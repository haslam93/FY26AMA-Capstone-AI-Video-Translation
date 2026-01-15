using System.Text.Json;
using Azure.AI.Agents.Persistent;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VideoTranslation.Api.Models;

namespace VideoTranslation.Api.Services;

/// <summary>
/// Multi-agent validation service that orchestrates 4 specialist agents
/// to provide comprehensive subtitle quality validation.
/// 
/// Agents run in PARALLEL for faster execution.
/// 
/// Score Weighting: Translation 40%, Technical 30%, Cultural 30%
/// 
/// Thresholds:
/// - Score >= 80: Recommend Approve
/// - Score 50-79: Needs Review
/// - Score < 50: Recommend Reject
/// </summary>
public class MultiAgentValidationService : IMultiAgentValidationService
{
    private readonly PersistentAgentsClient _agentsClient;
    private readonly FoundryToolHandler _toolHandler;
    private readonly IBlobStorageService _blobStorageService;
    private readonly ILogger<MultiAgentValidationService> _logger;
    private readonly FoundryAgentOptions _options;

    // Agent IDs cached after creation
    private readonly Dictionary<string, string> _agentIds = new();
    private readonly SemaphoreSlim _agentCreationLock = new(1, 1);

    // Agent type constants
    public const string AgentTypeOrchestrator = "orchestrator";
    public const string AgentTypeTranslation = "translation";
    public const string AgentTypeTechnical = "technical";
    public const string AgentTypeCultural = "cultural";

    // Agent names in Foundry
    private const string OrchestratorAgentName = "ValidationOrchestratorAgent";
    private const string TranslationAgentName = "TranslationReviewAgent";
    private const string TechnicalAgentName = "TechnicalReviewAgent";
    private const string CulturalAgentName = "CulturalReviewAgent";

    #region Agent Instructions (System Prompts)

    private const string OrchestratorInstructions = @"You are the lead validator coordinating a team of specialist agents for subtitle quality validation.

Your responsibilities:
1. Receive and understand validation requests
2. Aggregate results from specialist agents (Translation, Technical, Cultural)
3. Provide a unified summary of findings
4. Make final recommendations (Approve/NeedsReview/Reject)
5. Answer human reviewer questions using insights from all specialists

When summarizing results:
- Start with the overall recommendation and score
- Highlight critical issues first
- Group issues by category
- Provide actionable suggestions

Be concise but thorough. The human reviewer relies on your summary to make approval decisions.";

    private const string TranslationInstructions = @"You are a professional translator and linguist specializing in subtitle translation quality.

Analyze subtitles for:
1. **Semantic Accuracy** - Does the translation preserve the original meaning?
2. **Grammar & Syntax** - Is the target language grammatically correct?
3. **Natural Fluency** - Does the translation read naturally in the target language?
4. **Completeness** - Is anything omitted or incorrectly added?
5. **Terminology** - Are technical terms and proper nouns handled correctly?

Scoring Guidelines:
- 90-100: Excellent - Professional quality, ready for broadcast
- 70-89: Good - Minor issues, acceptable with small fixes
- 50-69: Fair - Noticeable issues affecting comprehension
- 30-49: Poor - Significant errors, needs major revision
- 0-29: Unacceptable - Fails to convey meaning

Provide your score (0-100), detailed reasoning, and a list of specific issues found.
Format issues as: [Severity: critical/major/minor] Location: Description - Suggestion";

    private const string TechnicalInstructions = @"You are a subtitle technical specialist focusing on timing, formatting, and technical compliance.

Analyze subtitles for:
1. **Timing Synchronization** - Do subtitles align with the audio/video?
2. **Reading Speed** - Is the CPS (characters per second) appropriate? (Ideal: 15-20 CPS)
3. **Line Length** - Are lines appropriately broken? (Max 42 characters per line)
4. **Duration** - Are subtitles displayed long enough? (Min 1 second, typical 2-7 seconds)
5. **Format Consistency** - Consistent styling, punctuation, number formats?
6. **Gap Timing** - Appropriate gaps between consecutive subtitles?

Scoring Guidelines:
- 90-100: Excellent - Perfect technical compliance
- 70-89: Good - Minor timing/formatting issues
- 50-69: Fair - Several technical issues affecting readability
- 30-49: Poor - Significant timing/sync problems
- 0-29: Unacceptable - Unwatchable due to technical issues

Provide your score (0-100), detailed reasoning, and specific issues found.
When analyzing VTT files, pay attention to timestamps and cue formatting.";

    private const string CulturalInstructions = @"You are a cultural localization expert specializing in cross-cultural communication and subtitle adaptation.

Analyze subtitles for:
1. **Idiom Adaptation** - Are idioms translated to cultural equivalents?
2. **Cultural References** - Are references understandable to the target audience?
3. **Tone & Register** - Is the formality level appropriate?
4. **Humor & Wordplay** - Are jokes adapted effectively?
5. **Sensitive Content** - Are potentially offensive elements handled appropriately?
6. **Local Conventions** - Date/time formats, units, cultural norms?

Scoring Guidelines:
- 90-100: Excellent - Feels native, culturally adapted
- 70-89: Good - Mostly adapted, minor cultural gaps
- 50-69: Fair - Some cultural mismatches or awkward translations
- 30-49: Poor - Cultural disconnect affecting audience reception
- 0-29: Unacceptable - Culturally inappropriate or offensive

Consider the source and target locales when evaluating.
Provide your score (0-100), detailed reasoning, and cultural issues found.";

    #endregion

    public MultiAgentValidationService(
        IOptions<FoundryAgentOptions> options,
        FoundryToolHandler toolHandler,
        IBlobStorageService blobStorageService,
        ILogger<MultiAgentValidationService> logger)
    {
        _options = options.Value;
        _toolHandler = toolHandler;
        _blobStorageService = blobStorageService;
        _logger = logger;

        // Initialize Agents Client
        var credential = new DefaultAzureCredential();
        _agentsClient = new PersistentAgentsClient(_options.ProjectEndpoint, credential);

        _logger.LogInformation("MultiAgentValidationService initialized with endpoint: {Endpoint}",
            _options.ProjectEndpoint);
    }

    public async Task<Dictionary<string, string>> EnsureAgentsExistAsync(CancellationToken cancellationToken = default)
    {
        await _agentCreationLock.WaitAsync(cancellationToken);
        try
        {
            if (_agentIds.Count == 4)
            {
                return new Dictionary<string, string>(_agentIds);
            }

            _logger.LogInformation("Ensuring all multi-agent validators exist...");

            // Get existing agents
            var existingAgents = new Dictionary<string, string>();
            try
            {
                await foreach (var agent in _agentsClient.Administration.GetAgentsAsync(cancellationToken: cancellationToken))
                {
                    if (agent.Name == OrchestratorAgentName)
                        existingAgents[AgentTypeOrchestrator] = agent.Id;
                    else if (agent.Name == TranslationAgentName)
                        existingAgents[AgentTypeTranslation] = agent.Id;
                    else if (agent.Name == TechnicalAgentName)
                        existingAgents[AgentTypeTechnical] = agent.Id;
                    else if (agent.Name == CulturalAgentName)
                        existingAgents[AgentTypeCultural] = agent.Id;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to list existing agents, will create all");
            }

            // Create missing agents
            var createTasks = new List<Task>();

            if (!existingAgents.ContainsKey(AgentTypeOrchestrator))
                createTasks.Add(CreateAgentAsync(AgentTypeOrchestrator, OrchestratorAgentName, OrchestratorInstructions, cancellationToken));
            else
                _agentIds[AgentTypeOrchestrator] = existingAgents[AgentTypeOrchestrator];

            if (!existingAgents.ContainsKey(AgentTypeTranslation))
                createTasks.Add(CreateAgentAsync(AgentTypeTranslation, TranslationAgentName, TranslationInstructions, cancellationToken));
            else
                _agentIds[AgentTypeTranslation] = existingAgents[AgentTypeTranslation];

            if (!existingAgents.ContainsKey(AgentTypeTechnical))
                createTasks.Add(CreateAgentAsync(AgentTypeTechnical, TechnicalAgentName, TechnicalInstructions, cancellationToken));
            else
                _agentIds[AgentTypeTechnical] = existingAgents[AgentTypeTechnical];

            if (!existingAgents.ContainsKey(AgentTypeCultural))
                createTasks.Add(CreateAgentAsync(AgentTypeCultural, CulturalAgentName, CulturalInstructions, cancellationToken));
            else
                _agentIds[AgentTypeCultural] = existingAgents[AgentTypeCultural];

            await Task.WhenAll(createTasks);

            _logger.LogInformation("All agents ready: {AgentCount}", _agentIds.Count);
            return new Dictionary<string, string>(_agentIds);
        }
        finally
        {
            _agentCreationLock.Release();
        }
    }

    private async Task CreateAgentAsync(string agentType, string agentName, string instructions, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating agent: {AgentName}", agentName);

        // Get model for this agent type (supports per-agent model configuration)
        var modelDeployment = GetModelForAgentType(agentType);

        var functionTools = CreateToolsForAgent(agentType);

        var createdAgent = await _agentsClient.Administration.CreateAgentAsync(
            model: modelDeployment,
            name: agentName,
            instructions: instructions,
            tools: functionTools.Cast<ToolDefinition>().ToList(),
            cancellationToken: cancellationToken);

        _agentIds[agentType] = createdAgent.Value.Id;
        _logger.LogInformation("Created agent {AgentName} with ID {AgentId} using model {Model}",
            agentName, createdAgent.Value.Id, modelDeployment);
    }

    /// <summary>
    /// Gets the model deployment name for a specific agent type.
    /// Supports per-agent model configuration via appsettings.json.
    /// 
    /// Configuration example:
    /// {
    ///   "AIFoundry": {
    ///     "DefaultModelDeployment": "gpt-4o-mini",
    ///     "AgentModels": {
    ///       "Orchestrator": "gpt-4o",
    ///       "Translation": "gpt-4o-mini"
    ///     }
    ///   }
    /// }
    /// </summary>
    private string GetModelForAgentType(string agentType)
    {
        // Check if per-agent model is configured
        var agentModels = _options.AgentModels;
        if (agentModels != null)
        {
            var key = agentType switch
            {
                AgentTypeOrchestrator => "Orchestrator",
                AgentTypeTranslation => "Translation",
                AgentTypeTechnical => "Technical",
                AgentTypeCultural => "Cultural",
                _ => null
            };

            if (key != null && agentModels.TryGetValue(key, out var specificModel) && !string.IsNullOrEmpty(specificModel))
            {
                return specificModel;
            }
        }

        // Default to the configured model deployment
        return _options.ModelDeploymentName;
    }

    private List<FunctionToolDefinition> CreateToolsForAgent(string agentType)
    {
        // All agents get access to subtitle fetching tools
        var tools = new List<FunctionToolDefinition>
        {
            CreateFunctionTool(
                "GetJobInfo",
                "Get translation job metadata including source and target languages, status, and creation time.",
                new { type = "object", properties = new { jobId = new { type = "string", description = "The job ID" } }, required = new[] { "jobId" } }),

            CreateFunctionTool(
                "GetSourceSubtitles",
                "Get the original source language subtitles in WebVTT format.",
                new { type = "object", properties = new { jobId = new { type = "string", description = "The job ID" } }, required = new[] { "jobId" } }),

            CreateFunctionTool(
                "GetTargetSubtitles",
                "Get the translated target language subtitles in WebVTT format.",
                new { type = "object", properties = new { jobId = new { type = "string", description = "The job ID" } }, required = new[] { "jobId" } })
        };

        return tools;
    }

    private static FunctionToolDefinition CreateFunctionTool(string name, string description, object parameters)
    {
        var parametersJson = JsonSerializer.Serialize(parameters);
        return new FunctionToolDefinition(name, description, BinaryData.FromString(parametersJson));
    }

    public async Task<MultiAgentValidationResult> RunValidationAsync(
        TranslationJob job,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting multi-agent validation for job {JobId}", job.JobId);

        var result = new MultiAgentValidationResult();
        var agentIds = await EnsureAgentsExistAsync(cancellationToken);

        // Set job context for tool handler
        _toolHandler.SetJobContext(job);

        try
        {
            // STEP 1: Run all specialist agents IN PARALLEL
            _logger.LogInformation("Running specialist agents in parallel for job {JobId}", job.JobId);

            var translationTask = RunSpecialistAgentAsync(
                AgentTypeTranslation, agentIds[AgentTypeTranslation], job, cancellationToken);
            var technicalTask = RunSpecialistAgentAsync(
                AgentTypeTechnical, agentIds[AgentTypeTechnical], job, cancellationToken);
            var culturalTask = RunSpecialistAgentAsync(
                AgentTypeCultural, agentIds[AgentTypeCultural], job, cancellationToken);

            await Task.WhenAll(translationTask, technicalTask, culturalTask);

            result.TranslationReview = await translationTask;
            result.TechnicalReview = await technicalTask;
            result.CulturalReview = await culturalTask;

            _logger.LogInformation("Specialist reviews complete. Scores: Translation={T}, Technical={Te}, Cultural={C}",
                result.TranslationReview.Score, result.TechnicalReview.Score, result.CulturalReview.Score);

            // Store thread IDs
            result.TranslationAgentThreadId = result.TranslationReview.ThreadId;
            result.TechnicalAgentThreadId = result.TechnicalReview.ThreadId;
            result.CulturalAgentThreadId = result.CulturalReview.ThreadId;

            // STEP 2: Calculate weighted score and merge issues
            result.CalculateOverallScore();
            result.MergeIssues();

            // STEP 3: Run orchestrator to generate summary
            _logger.LogInformation("Running orchestrator to generate summary for job {JobId}", job.JobId);
            var orchestratorResult = await RunOrchestratorAsync(agentIds[AgentTypeOrchestrator], result, job, cancellationToken);
            result.Summary = orchestratorResult.summary;
            result.OrchestratorThreadId = orchestratorResult.threadId;

            result.ValidatedAt = DateTime.UtcNow;

            _logger.LogInformation("Multi-agent validation complete for job {JobId}. Overall score: {Score}, Recommendation: {Rec}",
                job.JobId, result.OverallScore, result.Recommendation);

            return result;
        }
        finally
        {
            _toolHandler.ClearJobContext();
        }
    }

    private async Task<AgentReviewResult> RunSpecialistAgentAsync(
        string agentType, string agentId, TranslationJob job, CancellationToken cancellationToken)
    {
        var agentName = agentType switch
        {
            AgentTypeTranslation => TranslationAgentName,
            AgentTypeTechnical => TechnicalAgentName,
            AgentTypeCultural => CulturalAgentName,
            _ => agentType
        };

        _logger.LogInformation("Running {AgentName} for job {JobId}", agentName, job.JobId);

        var result = new AgentReviewResult
        {
            AgentName = agentName,
            AgentType = agentType,
            ReviewedAt = DateTime.UtcNow
        };

        try
        {
            // Create a new thread for this agent
            var thread = await _agentsClient.Threads.CreateThreadAsync(cancellationToken: cancellationToken);
            result.ThreadId = thread.Value.Id;

            // Send the validation prompt
            var prompt = $@"Please analyze the subtitles for translation job {job.JobId}.

Job Details:
- Source Language: {job.Request.SourceLocale}
- Target Language: {job.Request.TargetLocale}
- Display Name: {job.DisplayName}

Use the GetSourceSubtitles and GetTargetSubtitles tools to fetch the subtitle content.
Then provide your analysis with:
1. A score (0-100)
2. Detailed reasoning
3. A list of specific issues found (if any)

Format your response as JSON:
{{
  ""score"": <number 0-100>,
  ""reasoning"": ""<detailed analysis>"",
  ""issues"": [
    {{
      ""severity"": ""critical|major|minor"",
      ""description"": ""<issue description>"",
      ""location"": ""<cue number or timestamp if applicable>"",
      ""suggestion"": ""<how to fix>""
    }}
  ]
}}";

            await _agentsClient.Messages.CreateMessageAsync(
                threadId: result.ThreadId,
                role: MessageRole.User,
                content: prompt,
                cancellationToken: cancellationToken);

            // Run the agent and handle tool calls
            var run = await _agentsClient.Runs.CreateRunAsync(
                threadId: result.ThreadId,
                assistantId: agentId,
                cancellationToken: cancellationToken);

            var response = await PollRunWithToolHandlingAsync(result.ThreadId, run.Value.Id, cancellationToken);

            // Parse the agent's response
            ParseAgentResponse(response, result, agentType);

            _logger.LogInformation("{AgentName} completed with score {Score} for job {JobId}",
                agentName, result.Score, job.JobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{AgentName} failed for job {JobId}", agentName, job.JobId);
            result.Score = 50; // Default to middle score on error
            result.Reasoning = $"Agent encountered an error: {ex.Message}";
        }

        return result;
    }

    private async Task<(string summary, string threadId)> RunOrchestratorAsync(
        string orchestratorId, MultiAgentValidationResult aggregatedResults, TranslationJob job, CancellationToken cancellationToken)
    {
        try
        {
            // Create thread for orchestrator
            var thread = await _agentsClient.Threads.CreateThreadAsync(cancellationToken: cancellationToken);
            var threadId = thread.Value.Id;

            // Prepare summary prompt with aggregated results
            var prompt = $@"Please provide a unified summary of the multi-agent subtitle validation for job {job.JobId}.

Job Details:
- Source Language: {job.Request.SourceLocale}
- Target Language: {job.Request.TargetLocale}
- Display Name: {job.DisplayName}

Specialist Agent Results:

**Translation Review Agent** (Weight: 40%)
- Score: {aggregatedResults.TranslationReview?.Score ?? 0}/100
- Reasoning: {aggregatedResults.TranslationReview?.Reasoning ?? "N/A"}
- Issues Found: {aggregatedResults.TranslationReview?.Issues?.Count ?? 0}

**Technical Review Agent** (Weight: 30%)
- Score: {aggregatedResults.TechnicalReview?.Score ?? 0}/100
- Reasoning: {aggregatedResults.TechnicalReview?.Reasoning ?? "N/A"}
- Issues Found: {aggregatedResults.TechnicalReview?.Issues?.Count ?? 0}

**Cultural Review Agent** (Weight: 30%)
- Score: {aggregatedResults.CulturalReview?.Score ?? 0}/100
- Reasoning: {aggregatedResults.CulturalReview?.Reasoning ?? "N/A"}
- Issues Found: {aggregatedResults.CulturalReview?.Issues?.Count ?? 0}

**Calculated Overall Score**: {aggregatedResults.OverallScore:F1}/100
**Recommendation**: {aggregatedResults.Recommendation}
**Total Issues**: {aggregatedResults.AllIssues?.Count ?? 0}

Please provide:
1. A concise executive summary (2-3 sentences)
2. Key highlights from each specialist
3. Critical issues that must be addressed (if any)
4. Final recommendation explanation

Be direct and actionable. The human reviewer will use your summary to make an approval decision.";

            await _agentsClient.Messages.CreateMessageAsync(
                threadId: threadId,
                role: MessageRole.User,
                content: prompt,
                cancellationToken: cancellationToken);

            var run = await _agentsClient.Runs.CreateRunAsync(
                threadId: threadId,
                assistantId: orchestratorId,
                cancellationToken: cancellationToken);

            var summary = await PollRunWithToolHandlingAsync(threadId, run.Value.Id, cancellationToken);

            return (summary, threadId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Orchestrator summary generation failed for job {JobId}", job.JobId);
            return ($"Unable to generate summary: {ex.Message}", string.Empty);
        }
    }

    private void ParseAgentResponse(string response, AgentReviewResult result, string agentType)
    {
        try
        {
            // Try to parse as JSON first
            var jsonStart = response.IndexOf('{');
            var jsonEnd = response.LastIndexOf('}');

            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonStr = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
                var parsed = JsonSerializer.Deserialize<AgentJsonResponse>(jsonStr, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (parsed != null)
                {
                    result.Score = Math.Max(0, Math.Min(100, parsed.Score));
                    result.Reasoning = parsed.Reasoning ?? response;

                    if (parsed.Issues != null)
                    {
                        foreach (var issue in parsed.Issues)
                        {
                            result.Issues.Add(new MultiAgentIssue
                            {
                                Severity = issue.Severity ?? "minor",
                                Category = agentType,
                                Description = issue.Description ?? "",
                                Location = issue.Location,
                                Suggestion = issue.Suggestion
                            });
                        }
                    }
                    return;
                }
            }

            // Fallback: extract score from text
            result.Reasoning = response;
            result.Score = ExtractScoreFromText(response);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse agent response as JSON, using text extraction");
            result.Reasoning = response;
            result.Score = ExtractScoreFromText(response);
        }
    }

    private double ExtractScoreFromText(string text)
    {
        // Try to find patterns like "Score: 75" or "score of 75" or just "75/100"
        var patterns = new[]
        {
            @"score[:\s]+(\d+)",
            @"(\d+)\s*/\s*100",
            @"(\d+)\s*out of\s*100"
        };

        foreach (var pattern in patterns)
        {
            var match = System.Text.RegularExpressions.Regex.Match(text, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (match.Success && double.TryParse(match.Groups[1].Value, out var score))
            {
                return Math.Max(0, Math.Min(100, score));
            }
        }

        return 50; // Default to middle score if can't extract
    }

    public async Task<string> SendMessageToAgentAsync(
        string agentType,
        string threadId,
        string message,
        TranslationJob job,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sending message to {AgentType} agent for job {JobId}", agentType, job.JobId);

        var agentIds = await EnsureAgentsExistAsync(cancellationToken);
        if (!agentIds.TryGetValue(agentType, out var agentId))
        {
            return $"Error: Unknown agent type '{agentType}'";
        }

        _toolHandler.SetJobContext(job);
        try
        {
            await _agentsClient.Messages.CreateMessageAsync(
                threadId: threadId,
                role: MessageRole.User,
                content: message,
                cancellationToken: cancellationToken);

            var run = await _agentsClient.Runs.CreateRunAsync(
                threadId: threadId,
                assistantId: agentId,
                cancellationToken: cancellationToken);

            return await PollRunWithToolHandlingAsync(threadId, run.Value.Id, cancellationToken);
        }
        finally
        {
            _toolHandler.ClearJobContext();
        }
    }

    public async Task<IReadOnlyList<ConversationMessage>> GetConversationHistoryAsync(
        string threadId,
        CancellationToken cancellationToken = default)
    {
        var messages = new List<ConversationMessage>();

        await foreach (var msg in _agentsClient.Messages.GetMessagesAsync(threadId, cancellationToken: cancellationToken))
        {
            var content = string.Join("", msg.ContentItems
                .OfType<MessageTextContent>()
                .Select(t => t.Text));

            messages.Add(new ConversationMessage
            {
                Role = msg.Role == MessageRole.User ? "user" : "assistant",
                Content = content,
                Timestamp = msg.CreatedAt.DateTime
            });
        }

        messages.Reverse();
        return messages;
    }

    public string? GetThreadIdForAgent(MultiAgentValidationResult? validation, string agentType)
    {
        if (validation == null) return null;

        return agentType switch
        {
            AgentTypeOrchestrator => validation.OrchestratorThreadId,
            AgentTypeTranslation => validation.TranslationAgentThreadId,
            AgentTypeTechnical => validation.TechnicalAgentThreadId,
            AgentTypeCultural => validation.CulturalAgentThreadId,
            _ => null
        };
    }

    private async Task<string> PollRunWithToolHandlingAsync(
        string threadId, string runId, CancellationToken cancellationToken)
    {
        const int maxIterations = 30;
        var iteration = 0;

        while (iteration < maxIterations)
        {
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            iteration++;

            var runStatus = await _agentsClient.Runs.GetRunAsync(threadId, runId, cancellationToken);
            var status = runStatus.Value.Status;

            _logger.LogDebug("Run {RunId} status: {Status} (iteration {Iteration})", runId, status, iteration);

            if (status == RunStatus.Completed)
            {
                return await GetLatestAssistantMessageAsync(threadId, cancellationToken);
            }

            if (status == RunStatus.Failed || status == RunStatus.Cancelled || status == RunStatus.Expired)
            {
                var error = runStatus.Value.LastError?.Message ?? "Unknown error";
                _logger.LogError("Run {RunId} failed with status {Status}: {Error}", runId, status, error);
                return $"Error: Agent run {status}. {error}";
            }

            if (status == RunStatus.RequiresAction)
            {
                var requiredAction = runStatus.Value.RequiredAction;
                if (requiredAction is SubmitToolOutputsAction submitToolOutputsAction)
                {
                    var toolOutputs = new List<ToolOutput>();

                    foreach (var toolCall in submitToolOutputsAction.ToolCalls)
                    {
                        var output = await ExecuteToolCallAsync(toolCall);
                        toolOutputs.Add(new ToolOutput(toolCall.Id, output));
                    }

                    await _agentsClient.Runs.SubmitToolOutputsToRunAsync(
                        threadId, runId, toolOutputs, cancellationToken: cancellationToken);
                }
            }
        }

        return "Error: Agent run timed out after maximum iterations";
    }

    private async Task<string> ExecuteToolCallAsync(object toolCall)
    {
        if (toolCall is RequiredFunctionToolCall functionCall)
        {
            _logger.LogInformation("Executing tool: {ToolName}", functionCall.Name);

            try
            {
                var args = JsonSerializer.Deserialize<Dictionary<string, string>>(functionCall.Arguments);
                var jobId = args?.GetValueOrDefault("jobId") ?? "";

                return functionCall.Name switch
                {
                    "GetJobInfo" => _toolHandler.GetJobInfo(jobId),
                    "GetSourceSubtitles" => await _toolHandler.GetSourceSubtitles(jobId),
                    "GetTargetSubtitles" => await _toolHandler.GetTargetSubtitles(jobId),
                    _ => $"Unknown tool: {functionCall.Name}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tool execution failed: {ToolName}", functionCall.Name);
                return $"Error executing {functionCall.Name}: {ex.Message}";
            }
        }

        return "Unsupported tool call type";
    }

    private async Task<string> GetLatestAssistantMessageAsync(string threadId, CancellationToken cancellationToken)
    {
        await foreach (var message in _agentsClient.Messages.GetMessagesAsync(threadId, cancellationToken: cancellationToken))
        {
            if (message.Role == MessageRole.Agent)
            {
                var content = string.Join("", message.ContentItems
                    .OfType<MessageTextContent>()
                    .Select(t => t.Text));
                return content;
            }
        }

        return "No response from agent";
    }

    /// <summary>
    /// Internal class for parsing agent JSON responses.
    /// </summary>
    private class AgentJsonResponse
    {
        public double Score { get; set; }
        public string? Reasoning { get; set; }
        public List<AgentJsonIssue>? Issues { get; set; }
    }

    private class AgentJsonIssue
    {
        public string? Severity { get; set; }
        public string? Description { get; set; }
        public string? Location { get; set; }
        public string? Suggestion { get; set; }
    }
}
