// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using VideoTranslation.Api.Agents;
using VideoTranslation.Api.Models;
using VideoTranslation.Api.Services;

namespace VideoTranslation.Api.Activities;

/// <summary>
/// Activity that runs AI-powered subtitle validation.
/// Uses Azure AI Foundry Agent Service when available, falls back to direct chat.
/// </summary>
public class RunValidationActivity
{
    private readonly IFoundryAgentService? _foundryAgentService;
    private readonly ISubtitleValidationAgent _validationAgent;
    private readonly ILogger<RunValidationActivity> _logger;

    public RunValidationActivity(
        ISubtitleValidationAgent validationAgent,
        ILogger<RunValidationActivity> logger,
        IFoundryAgentService? foundryAgentService = null)
    {
        _foundryAgentService = foundryAgentService;
        _validationAgent = validationAgent;
        _logger = logger;
    }

    [Function(nameof(RunValidationActivity))]
    public async Task<RunValidationResult> RunAsync(
        [ActivityTrigger] RunValidationInput input)
    {
        _logger.LogInformation("Running AI validation for job {JobId}", input.JobId);

        try
        {
            if (string.IsNullOrEmpty(input.SourceSubtitleUrl) || 
                string.IsNullOrEmpty(input.TargetSubtitleUrl))
            {
                _logger.LogWarning("Job {JobId}: Missing subtitle URLs, skipping validation", input.JobId);
                return new RunValidationResult
                {
                    Success = false,
                    Error = "Missing source or target subtitle URLs"
                };
            }

            // Try Foundry Agent Service first (if available)
            if (_foundryAgentService != null && input.Job != null)
            {
                _logger.LogInformation("Job {JobId}: Using Foundry Agent Service for validation", input.JobId);
                
                var agentResult = await _foundryAgentService.RunValidationAsync(input.Job);
                
                if (agentResult.Success && agentResult.ValidationResult != null)
                {
                    _logger.LogInformation(
                        "Job {JobId}: Foundry validation complete. Score={Score}, ThreadId={ThreadId}",
                        input.JobId,
                        agentResult.ValidationResult.ConfidenceScore,
                        agentResult.ThreadId);

                    return new RunValidationResult
                    {
                        Success = true,
                        ValidationResult = agentResult.ValidationResult,
                        ThreadId = agentResult.ThreadId
                    };
                }
                else
                {
                    _logger.LogWarning(
                        "Job {JobId}: Foundry validation failed: {Error}. Falling back to legacy agent.",
                        input.JobId,
                        agentResult.Error);
                }
            }

            // Fallback to legacy direct chat validation
            _logger.LogInformation("Job {JobId}: Using legacy SubtitleValidationAgent", input.JobId);
            
            var validationResult = await _validationAgent.ValidateAsync(
                input.SourceSubtitleUrl,
                input.TargetSubtitleUrl,
                input.SourceLanguage,
                input.TargetLanguage);

            _logger.LogInformation(
                "Job {JobId}: Validation complete. Score={Score}, Issues={IssueCount}",
                input.JobId,
                validationResult.ConfidenceScore,
                validationResult.Issues.Count);

            return new RunValidationResult
            {
                Success = true,
                ValidationResult = validationResult
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Job {JobId}: Validation failed with exception", input.JobId);
            return new RunValidationResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }
}

/// <summary>
/// Input for the validation activity.
/// </summary>
public class RunValidationInput
{
    public string JobId { get; set; } = string.Empty;
    public string? SourceSubtitleUrl { get; set; }
    public string? TargetSubtitleUrl { get; set; }
    public string SourceLanguage { get; set; } = "en-US";
    public string TargetLanguage { get; set; } = "es-ES";
    
    /// <summary>
    /// The full job object for Foundry Agent Service (provides context for tools).
    /// </summary>
    public TranslationJob? Job { get; set; }
}

/// <summary>
/// Result of the validation activity.
/// </summary>
public class RunValidationResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public SubtitleValidationResult? ValidationResult { get; set; }
    
    /// <summary>
    /// Thread ID for follow-up conversations (from Foundry Agent Service).
    /// </summary>
    public string? ThreadId { get; set; }
}
