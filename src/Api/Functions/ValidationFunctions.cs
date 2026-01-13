using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using VideoTranslation.Api.Agents;
using VideoTranslation.Api.Models;

namespace VideoTranslation.Api.Functions;

/// <summary>
/// HTTP trigger functions for subtitle validation operations.
/// </summary>
public class ValidationFunctions
{
    private readonly ILogger<ValidationFunctions> _logger;
    private readonly ISubtitleValidationAgent _validationAgent;
    private readonly IAgentConfiguration _agentConfig;
    private readonly JsonSerializerOptions _jsonOptions;

    public ValidationFunctions(
        ILogger<ValidationFunctions> logger,
        ISubtitleValidationAgent validationAgent,
        IAgentConfiguration agentConfig)
    {
        _logger = logger;
        _validationAgent = validationAgent;
        _agentConfig = agentConfig;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    /// <summary>
    /// GET /api/debug/gpt - Test GPT connection and see raw response.
    /// </summary>
    [Function("DebugGpt")]
    public async Task<HttpResponseData> DebugGptAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "debug/gpt")] HttpRequestData req,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("DebugGpt: Testing GPT-4o-mini connection");

        try
        {
            // Use the exact same prompt format as the validation agent
            var systemPrompt = @"You are an expert subtitle translation validator. Analyze the translation quality and provide scores.

You must respond with ONLY a valid JSON object (no markdown, no code fences) in this exact format:
{
    ""timingScore"": 0.85,
    ""translationScore"": 0.90,
    ""grammarScore"": 0.88,
    ""culturalScore"": 0.82,
    ""reasoning"": ""Test response"",
    ""issues"": []
}";

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage("This is a test. Return the sample JSON from the system prompt with your own reasonable scores.")
            };

            var completion = await _agentConfig.ChatClient.CompleteChatAsync(
                messages,
                new ChatCompletionOptions
                {
                    Temperature = 0.3f,
                    MaxOutputTokenCount = 500
                },
                cancellationToken);

            var responseText = completion.Value.Content[0].Text ?? "";
            
            // Test the extraction function
            var extracted = ExtractJson(responseText);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonSerializer.Serialize(new 
            { 
                success = true,
                rawResponse = responseText,
                extractedJson = extracted,
                startsWithBacktick = responseText.TrimStart().StartsWith("```"),
                model = "gpt-4o-mini"
            }));
            return response;
        }
        catch (Exception ex)
        {
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonSerializer.Serialize(new 
            { 
                success = false,
                error = ex.Message,
                errorType = ex.GetType().Name
            }));
            return response;
        }
    }

    private static string ExtractJson(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
            return "{}";

        var trimmed = response.Trim();

        // Check for ```json ... ``` or ``` ... ``` pattern
        if (trimmed.StartsWith("```"))
        {
            var firstNewline = trimmed.IndexOf('\n');
            if (firstNewline > 0)
            {
                var startIndex = firstNewline + 1;
                var endIndex = trimmed.IndexOf("\n```", startIndex, StringComparison.Ordinal);
                if (endIndex > startIndex)
                {
                    return trimmed.Substring(startIndex, endIndex - startIndex).Trim();
                }
                endIndex = trimmed.LastIndexOf("```", StringComparison.Ordinal);
                if (endIndex > startIndex)
                {
                    return trimmed.Substring(startIndex, endIndex - startIndex).Trim();
                }
            }
        }

        if (trimmed.StartsWith("{") && trimmed.EndsWith("}"))
            return trimmed;

        var jsonStart = trimmed.IndexOf('{');
        var jsonEnd = trimmed.LastIndexOf('}');
        if (jsonStart >= 0 && jsonEnd > jsonStart)
            return trimmed.Substring(jsonStart, jsonEnd - jsonStart + 1);

        return response;
    }

    /// <summary>
    /// POST /api/jobs/{jobId}/validate - Validate subtitles for a completed job.
    /// </summary>
    [Function("ValidateSubtitles")]
    public async Task<HttpResponseData> ValidateSubtitlesAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "jobs/{jobId}/validate")] HttpRequestData req,
        [DurableClient] DurableTaskClient durableClient,
        string jobId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("ValidateSubtitles: Starting validation for job {JobId}", jobId);

        try
        {
            // Get the job from orchestration
            var instance = await durableClient.GetInstanceAsync(jobId, getInputsAndOutputs: true);
            
            if (instance == null)
            {
                return await CreateErrorResponse(req, HttpStatusCode.NotFound, $"Job {jobId} not found");
            }

            // Parse the job state
            TranslationJob? job = null;
            if (instance.SerializedOutput != null)
            {
                job = JsonSerializer.Deserialize<TranslationJob>(instance.SerializedOutput, _jsonOptions);
            }
            else if (instance.SerializedInput != null)
            {
                job = JsonSerializer.Deserialize<TranslationJob>(instance.SerializedInput, _jsonOptions);
            }

            if (job == null)
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Unable to parse job state");
            }

            // Ensure job is completed and has results
            if (job.Status != JobStatus.Completed)
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, 
                    $"Job must be completed to validate. Current status: {job.Status}");
            }

            if (job.Result?.SourceSubtitleUrl == null || job.Result?.TargetSubtitleUrl == null)
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, 
                    "Job does not have subtitle outputs to validate");
            }

            // Get the subtitle URLs (prefer stored outputs if available)
            var sourceUrl = job.Result.StoredOutputs?.SourceSubtitleUrl ?? job.Result.SourceSubtitleUrl;
            var targetUrl = job.Result.StoredOutputs?.TargetSubtitleUrl ?? job.Result.TargetSubtitleUrl;

            _logger.LogInformation("ValidateSubtitles: Validating source={SourceUrl}, target={TargetUrl}", 
                sourceUrl, targetUrl);

            // Run validation
            var validationResult = await _validationAgent.ValidateAsync(
                sourceUrl,
                targetUrl,
                job.Request.SourceLocale,
                job.Request.TargetLocale,
                cancellationToken);

            // Create response
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");

            var responseBody = new ValidationResponse
            {
                JobId = jobId,
                ValidationResult = validationResult
            };

            await response.WriteStringAsync(JsonSerializer.Serialize(responseBody, _jsonOptions));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ValidateSubtitles: Error validating job {JobId}", jobId);
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    /// <summary>
    /// POST /api/validate - Validate subtitles directly from URLs.
    /// </summary>
    [Function("ValidateSubtitlesDirect")]
    public async Task<HttpResponseData> ValidateSubtitlesDirectAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "validate")] HttpRequestData req,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("ValidateSubtitlesDirect: Starting direct validation");

        try
        {
            var body = await req.ReadAsStringAsync();
            if (string.IsNullOrEmpty(body))
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Request body is required");
            }

            var request = JsonSerializer.Deserialize<DirectValidationRequest>(body, _jsonOptions);
            if (request == null)
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid request body");
            }

            // Validate required fields
            if (string.IsNullOrEmpty(request.SourceSubtitleUrl))
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "sourceSubtitleUrl is required");
            }
            if (string.IsNullOrEmpty(request.TargetSubtitleUrl))
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "targetSubtitleUrl is required");
            }

            // Run validation
            var validationResult = await _validationAgent.ValidateAsync(
                request.SourceSubtitleUrl,
                request.TargetSubtitleUrl,
                request.SourceLanguage ?? "en-US",
                request.TargetLanguage ?? "es-ES",
                cancellationToken);

            // Create response
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");

            await response.WriteStringAsync(JsonSerializer.Serialize(new
            {
                validationResult
            }, _jsonOptions));
            
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ValidateSubtitlesDirect: Error during validation");
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    private static async Task<HttpResponseData> CreateErrorResponse(
        HttpRequestData req,
        HttpStatusCode statusCode,
        string message)
    {
        var response = req.CreateResponse(statusCode);
        response.Headers.Add("Content-Type", "application/json");
        await response.WriteStringAsync(JsonSerializer.Serialize(new { error = message }));
        return response;
    }
}

/// <summary>
/// Response from validation endpoint.
/// </summary>
public class ValidationResponse
{
    public string JobId { get; set; } = string.Empty;
    public SubtitleValidationResult ValidationResult { get; set; } = new();
}

/// <summary>
/// Request for direct subtitle validation.
/// </summary>
public class DirectValidationRequest
{
    public string SourceSubtitleUrl { get; set; } = string.Empty;
    public string TargetSubtitleUrl { get; set; } = string.Empty;
    public string? SourceLanguage { get; set; }
    public string? TargetLanguage { get; set; }
}
