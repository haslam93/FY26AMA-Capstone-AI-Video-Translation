using System.Text.Json;
using System.Text.RegularExpressions;
using Azure.AI.Agents.Persistent;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VideoTranslation.Api.Models;

namespace VideoTranslation.Api.Services;

/// <summary>
/// Configuration for the Foundry Agent Service.
/// </summary>
public class FoundryAgentOptions
{
    public const string SectionName = "FoundryAgent";

    /// <summary>
    /// Azure AI Foundry project endpoint.
    /// Format: "https://account.services.ai.azure.com/api/projects/project-name"
    /// Or: "https://account.cognitiveservices.azure.com"
    /// </summary>
    public string? ProjectEndpoint { get; set; }

    /// <summary>
    /// Model deployment name (e.g., "gpt-4o-mini").
    /// </summary>
    public string ModelDeploymentName { get; set; } = "gpt-4o-mini";

    /// <summary>
    /// Name of the validation agent.
    /// </summary>
    public string AgentName { get; set; } = "SubtitleValidationAgent";

    /// <summary>
    /// Agent ID (populated after creation).
    /// </summary>
    public string? AgentId { get; set; }
}

/// <summary>
/// Implementation of the Foundry Agent Service for subtitle validation.
/// Uses Azure AI Foundry Persistent Agents SDK.
/// </summary>
public partial class FoundryAgentService : IFoundryAgentService
{
    private readonly PersistentAgentsClient _agentsClient;
    private readonly FoundryAgentOptions _options;
    private readonly FoundryToolHandler _toolHandler;
    private readonly ILogger<FoundryAgentService> _logger;
    
    private string? _cachedAgentId;
    private readonly SemaphoreSlim _agentCreationLock = new(1, 1);

    // Agent instructions for subtitle validation
    private const string AgentInstructions = @"You are a professional subtitle quality validation agent for video translation services. Your role is to analyze translated subtitles and provide detailed quality assessments.

## Your Capabilities
You have access to tools that allow you to:
1. Get job information (metadata, languages, settings)
2. Get source language subtitles (original content)
3. Get translated target language subtitles (content to validate)

## Validation Process
When asked to validate a translation job:
1. First, use GetJobInfo to understand the translation context (source/target languages)
2. Then, use GetSourceSubtitles to get the original content
3. Finally, use GetTargetSubtitles to get the translated content
4. Analyze the translation quality across these dimensions:
   - Translation Accuracy: Does the translation convey the original meaning?
   - Grammar & Fluency: Is the target language grammatically correct and natural?
   - Timing & Sync: Are subtitle timings appropriate for reading speed?
   - Cultural Adaptation: Are idioms and cultural references appropriately localized?
   - Formatting: Are line breaks, character counts, and formatting appropriate?

## Response Format
After analysis, provide your assessment in this exact JSON format:
```json
{
  ""isValid"": true,
  ""confidenceScore"": 0.85,
  ""reasoning"": ""Overall assessment summary..."",
  ""categoryScores"": {
    ""translationAccuracyScore"": 0.90,
    ""grammarScore"": 0.85,
    ""timingScore"": 0.80,
    ""culturalContextScore"": 0.85,
    ""formattingScore"": 0.85
  },
  ""issues"": [
    { ""severity"": ""Medium"", ""category"": ""Translation"", ""description"": ""..."", ""timestamp"": ""00:01:23"" }
  ]
}
```

Scores are 0.0-1.0. isValid should be true if confidenceScore >= 0.7.

## Interactive Review
If a human reviewer asks follow-up questions, use your tools to provide specific information. You can:
- Explain why specific segments were flagged
- Provide alternative translation suggestions
- Compare specific segments between source and target
- Clarify cultural or linguistic issues";

    public FoundryAgentService(
        IOptions<FoundryAgentOptions> options,
        FoundryToolHandler toolHandler,
        ILogger<FoundryAgentService> logger)
    {
        _options = options.Value;
        _toolHandler = toolHandler;
        _logger = logger;

        if (string.IsNullOrEmpty(_options.ProjectEndpoint))
        {
            throw new InvalidOperationException("FoundryAgent:ProjectEndpoint must be configured");
        }

        // Create the Persistent Agents Client with managed identity
        _agentsClient = new PersistentAgentsClient(
            _options.ProjectEndpoint,
            new DefaultAzureCredential());
        
        _logger.LogInformation("Foundry Agent Service initialized with endpoint: {Endpoint}", 
            _options.ProjectEndpoint);
    }

    public async Task<string> EnsureAgentExistsAsync(CancellationToken cancellationToken = default)
    {
        // Return cached ID if available
        if (!string.IsNullOrEmpty(_cachedAgentId))
        {
            return _cachedAgentId;
        }

        await _agentCreationLock.WaitAsync(cancellationToken);
        try
        {
            // Double-check after acquiring lock
            if (!string.IsNullOrEmpty(_cachedAgentId))
            {
                return _cachedAgentId;
            }

            _logger.LogInformation("Checking for existing agent: {AgentName}", _options.AgentName);

            // Try to get existing agent by listing and finding by name
            try
            {
                await foreach (var agent in _agentsClient.Administration.GetAgentsAsync(cancellationToken: cancellationToken))
                {
                    if (agent.Name == _options.AgentName)
                    {
                        _cachedAgentId = agent.Id;
                        _logger.LogInformation("Found existing agent: {AgentId}", _cachedAgentId);
                        return _cachedAgentId;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to list existing agents, will create new one");
            }

            // Create new agent with function tools
            _logger.LogInformation("Creating new validation agent: {AgentName}", _options.AgentName);

            var functionTools = new List<FunctionToolDefinition>
            {
                CreateFunctionTool(
                    "GetJobInfo",
                    "Get translation job metadata including source and target languages, status, and creation time. Use this to understand the context of the translation being validated.",
                    new { type = "object", properties = new { jobId = new { type = "string", description = "The job ID to retrieve information for" } }, required = new[] { "jobId" } }),
                
                CreateFunctionTool(
                    "GetSourceSubtitles",
                    "Get the original source language subtitles in WebVTT format. Use this to compare the original content with the translation.",
                    new { type = "object", properties = new { jobId = new { type = "string", description = "The job ID to retrieve source subtitles for" } }, required = new[] { "jobId" } }),
                
                CreateFunctionTool(
                    "GetTargetSubtitles",
                    "Get the translated target language subtitles in WebVTT format. This is the primary content to validate for translation quality, grammar, and timing.",
                    new { type = "object", properties = new { jobId = new { type = "string", description = "The job ID to retrieve target subtitles for" } }, required = new[] { "jobId" } })
            };

            var createdAgent = await _agentsClient.Administration.CreateAgentAsync(
                model: _options.ModelDeploymentName,
                name: _options.AgentName,
                instructions: AgentInstructions,
                tools: functionTools.Cast<ToolDefinition>().ToList(),
                cancellationToken: cancellationToken);

            _cachedAgentId = createdAgent.Value.Id;
            _logger.LogInformation("Created new agent: {AgentId}", _cachedAgentId);
            
            return _cachedAgentId;
        }
        finally
        {
            _agentCreationLock.Release();
        }
    }

    private static FunctionToolDefinition CreateFunctionTool(string name, string description, object parameters)
    {
        var parametersJson = JsonSerializer.Serialize(parameters);
        return new FunctionToolDefinition(name, description, BinaryData.FromString(parametersJson));
    }

    public async Task<ValidationAgentResult> RunValidationAsync(
        TranslationJob job,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Running validation for job {JobId}", job.JobId);

        try
        {
            // Ensure agent exists
            var agentId = await EnsureAgentExistsAsync(cancellationToken);

            // Set job context for tool handler
            _toolHandler.SetJobContext(job);

            // Create a new thread
            var thread = await _agentsClient.Threads.CreateThreadAsync(cancellationToken: cancellationToken);
            var threadId = thread.Value.Id;
            _logger.LogInformation("Created thread {ThreadId} for job {JobId}", threadId, job.JobId);

            // Add the initial validation request message
            await _agentsClient.Messages.CreateMessageAsync(
                threadId: threadId,
                role: MessageRole.User,
                content: $"Please validate the subtitle translation for job {job.JobId}. " +
                         $"The video was translated from {job.Request.SourceLocale} to {job.Request.TargetLocale}. " +
                         "Use your tools to fetch the job info and subtitles, then provide a detailed quality assessment.",
                cancellationToken: cancellationToken);

            // Create and poll the run
            var run = await _agentsClient.Runs.CreateRunAsync(
                threadId: threadId,
                assistantId: agentId,
                cancellationToken: cancellationToken);

            // Poll for completion with tool handling
            var response = await PollRunWithToolHandlingAsync(threadId, run.Value.Id, cancellationToken);

            // Parse the response
            var validationResult = ParseValidationResponse(response);

            return new ValidationAgentResult
            {
                Success = true,
                ThreadId = threadId,
                ValidationResult = validationResult,
                RawResponse = response
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Validation failed for job {JobId}", job.JobId);
            return new ValidationAgentResult
            {
                Success = false,
                Error = ex.Message
            };
        }
        finally
        {
            _toolHandler.ClearJobContext();
        }
    }

    public async Task<string> SendFollowUpMessageAsync(
        string threadId,
        string message,
        TranslationJob job,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sending follow-up message to thread {ThreadId}", threadId);

        try
        {
            var agentId = await EnsureAgentExistsAsync(cancellationToken);
            _toolHandler.SetJobContext(job);

            // Add the user message
            await _agentsClient.Messages.CreateMessageAsync(
                threadId: threadId,
                role: MessageRole.User,
                content: message,
                cancellationToken: cancellationToken);

            // Create and poll the run
            var run = await _agentsClient.Runs.CreateRunAsync(
                threadId: threadId,
                assistantId: agentId,
                cancellationToken: cancellationToken);

            // Poll for completion with tool handling
            var response = await PollRunWithToolHandlingAsync(threadId, run.Value.Id, cancellationToken);

            return response;
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
        _logger.LogInformation("Getting conversation history for thread {ThreadId}", threadId);

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

        // Reverse to get chronological order (API returns newest first)
        messages.Reverse();
        return messages;
    }

    private async Task<string> PollRunWithToolHandlingAsync(
        string threadId,
        string runId,
        CancellationToken cancellationToken)
    {
        const int maxIterations = 60;
        var iteration = 0;

        while (iteration < maxIterations)
        {
            iteration++;
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);

            var runResponse = await _agentsClient.Runs.GetRunAsync(threadId, runId, cancellationToken);
            var run = runResponse.Value;
            var status = run.Status;

            _logger.LogDebug("Run {RunId} status: {Status} (iteration {Iteration})", 
                runId, status, iteration);

            // RunStatus is a struct with static readonly properties, use == comparison
            if (status == RunStatus.Completed)
            {
                // Get the assistant's response
                await foreach (var msg in _agentsClient.Messages.GetMessagesAsync(threadId, cancellationToken: cancellationToken))
                {
                    if (msg.Role == MessageRole.Agent)
                    {
                        var content = string.Join("", msg.ContentItems
                            .OfType<MessageTextContent>()
                            .Select(t => t.Text));
                        return content;
                    }
                }
                return "No response from agent";
            }
            else if (status == RunStatus.RequiresAction)
            {
                // Handle tool calls
                if (run.RequiredAction is SubmitToolOutputsAction toolOutputsAction)
                {
                    await HandleToolCallsAsync(threadId, runId, toolOutputsAction, cancellationToken);
                }
            }
            else if (status == RunStatus.Failed)
            {
                var error = run.LastError?.Message ?? "Unknown error";
                throw new Exception($"Agent run failed: {error}");
            }
            else if (status == RunStatus.Cancelled || status == RunStatus.Expired)
            {
                throw new Exception($"Agent run {status}");
            }
            // else: Queued, InProgress, Cancelling - continue polling
        }

        throw new TimeoutException($"Agent run did not complete within {maxIterations} iterations");
    }

    private async Task HandleToolCallsAsync(
        string threadId,
        string runId,
        SubmitToolOutputsAction toolOutputsAction,
        CancellationToken cancellationToken)
    {
        var toolOutputs = new List<ToolOutput>();

        foreach (var toolCall in toolOutputsAction.ToolCalls)
        {
            if (toolCall is RequiredFunctionToolCall functionCall)
            {
                var functionName = functionCall.Name;
                var arguments = functionCall.Arguments;

                _logger.LogInformation("Handling tool call: {Function} with args: {Args}", 
                    functionName, arguments);

                string output;
                try
                {
                    output = await ExecuteToolAsync(functionName, arguments, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Tool call {Function} failed", functionName);
                    output = $"Error executing {functionName}: {ex.Message}";
                }

                toolOutputs.Add(new ToolOutput(functionCall.Id, output));
            }
        }

        // Submit tool outputs
        await _agentsClient.Runs.SubmitToolOutputsToRunAsync(
            threadId: threadId,
            runId: runId,
            toolOutputs: toolOutputs,
            cancellationToken: cancellationToken);
    }

    private async Task<string> ExecuteToolAsync(
        string functionName,
        string arguments,
        CancellationToken cancellationToken)
    {
        // Parse arguments
        var args = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(arguments) 
            ?? new Dictionary<string, JsonElement>();

        return functionName switch
        {
            "GetJobInfo" => _toolHandler.GetJobInfo(
                args.TryGetValue("jobId", out var jobId) ? jobId.GetString() ?? "" : ""),
            
            "GetSourceSubtitles" => await _toolHandler.GetSourceSubtitles(
                args.TryGetValue("jobId", out var srcJobId) ? srcJobId.GetString() ?? "" : ""),
            
            "GetTargetSubtitles" => await _toolHandler.GetTargetSubtitles(
                args.TryGetValue("jobId", out var tgtJobId) ? tgtJobId.GetString() ?? "" : ""),
            
            _ => $"Unknown function: {functionName}"
        };
    }

    private SubtitleValidationResult? ParseValidationResponse(string response)
    {
        try
        {
            // Try to extract JSON from the response
            var jsonMatch = JsonRegex().Match(response);
            if (!jsonMatch.Success)
            {
                _logger.LogWarning("No JSON found in agent response");
                return CreateDefaultResult(response);
            }

            var json = jsonMatch.Value;
            var parsed = JsonSerializer.Deserialize<ValidationResponseJson>(json, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (parsed == null)
            {
                return CreateDefaultResult(response);
            }

            return new SubtitleValidationResult
            {
                IsValid = parsed.IsValid,
                ConfidenceScore = parsed.ConfidenceScore,
                Reasoning = parsed.Reasoning ?? response,
                CategoryScores = parsed.CategoryScores != null ? new ValidationCategoryScores
                {
                    TranslationAccuracyScore = parsed.CategoryScores.TranslationAccuracyScore,
                    GrammarScore = parsed.CategoryScores.GrammarScore,
                    TimingScore = parsed.CategoryScores.TimingScore,
                    CulturalContextScore = parsed.CategoryScores.CulturalContextScore,
                    FormattingScore = parsed.CategoryScores.FormattingScore
                } : null,
                Issues = parsed.Issues?.Select(i => new ValidationIssue
                {
                    Severity = Enum.TryParse<IssueSeverity>(i.Severity, true, out var sev) ? sev : IssueSeverity.Low,
                    Category = Enum.TryParse<IssueCategory>(i.Category, true, out var cat) ? cat : IssueCategory.Formatting,
                    Description = i.Description ?? "",
                    Timestamp = i.Timestamp
                }).ToList() ?? new List<ValidationIssue>(),
                ValidatedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse validation response");
            return CreateDefaultResult(response);
        }
    }

    private static SubtitleValidationResult CreateDefaultResult(string rawResponse)
    {
        return new SubtitleValidationResult
        {
            IsValid = false,
            ConfidenceScore = 0.5,
            Reasoning = rawResponse,
            ValidatedAt = DateTime.UtcNow
        };
    }

    [GeneratedRegex(@"\{[\s\S]*?\""isValid\""[\s\S]*?\}", RegexOptions.Multiline)]
    private static partial Regex JsonRegex();

    // JSON parsing helper classes
    private class ValidationResponseJson
    {
        public bool IsValid { get; set; }
        public double ConfidenceScore { get; set; }
        public string? Reasoning { get; set; }
        public CategoryScoresJson? CategoryScores { get; set; }
        public List<IssueJson>? Issues { get; set; }
    }

    private class CategoryScoresJson
    {
        public double TranslationAccuracyScore { get; set; }
        public double GrammarScore { get; set; }
        public double TimingScore { get; set; }
        public double CulturalContextScore { get; set; }
        public double FormattingScore { get; set; }
    }

    private class IssueJson
    {
        public string? Severity { get; set; }
        public string? Category { get; set; }
        public string? Description { get; set; }
        public string? Timestamp { get; set; }
    }
}
