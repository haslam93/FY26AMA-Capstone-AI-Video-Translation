using System.ComponentModel;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using VideoTranslation.Api.Models;

namespace VideoTranslation.Api.Services;

/// <summary>
/// Provides tool functions that the Foundry Agent can call.
/// These methods are decorated with descriptions for the AI to understand.
/// </summary>
public class FoundryToolHandler
{
    private readonly IBlobStorageService _blobStorageService;
    private readonly ILogger<FoundryToolHandler> _logger;
    
    // In-memory job cache for the current validation session
    // In production, this would come from durable storage
    private TranslationJob? _currentJob;

    public FoundryToolHandler(
        IBlobStorageService blobStorageService,
        ILogger<FoundryToolHandler> logger)
    {
        _blobStorageService = blobStorageService;
        _logger = logger;
    }

    /// <summary>
    /// Sets the current job context for tool calls.
    /// Must be called before the agent run.
    /// </summary>
    public void SetJobContext(TranslationJob job)
    {
        _currentJob = job;
        _logger.LogInformation("Job context set for job {JobId}", job.JobId);
    }

    /// <summary>
    /// Clears the current job context.
    /// </summary>
    public void ClearJobContext()
    {
        _currentJob = null;
    }

    /// <summary>
    /// Get job information including metadata, locales, and status.
    /// </summary>
    /// <param name="jobId">The job ID to get information for.</param>
    /// <returns>JSON string with job metadata.</returns>
    [Description("Get translation job metadata including source and target languages, status, and creation time. Use this to understand the context of the translation being validated.")]
    public string GetJobInfo(
        [Description("The job ID to retrieve information for")] string jobId)
    {
        _logger.LogInformation("Tool call: GetJobInfo for job {JobId}", jobId);

        if (_currentJob == null || _currentJob.JobId != jobId)
        {
            return JsonSerializer.Serialize(new { error = $"Job {jobId} not found in current context" });
        }

        var jobInfo = new
        {
            jobId = _currentJob.JobId,
            displayName = _currentJob.DisplayName,
            status = _currentJob.Status.ToString(),
            sourceLocale = _currentJob.Request.SourceLocale,
            targetLocale = _currentJob.Request.TargetLocale,
            voiceKind = _currentJob.Request.VoiceKind,
            speakerCount = _currentJob.Request.SpeakerCount,
            createdAt = _currentJob.CreatedAt,
            lastUpdatedAt = _currentJob.LastUpdatedAt,
            iterationNumber = _currentJob.IterationNumber,
            exportSubtitleInVideo = _currentJob.Request.ExportSubtitleInVideo
        };

        return JsonSerializer.Serialize(jobInfo, new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// Get the source language subtitles (VTT format) for a job.
    /// </summary>
    /// <param name="jobId">The job ID to get source subtitles for.</param>
    /// <returns>The source subtitle content in WebVTT format.</returns>
    [Description("Get the original source language subtitles in WebVTT format. Use this to compare the original content with the translation.")]
    public async Task<string> GetSourceSubtitles(
        [Description("The job ID to retrieve source subtitles for")] string jobId)
    {
        _logger.LogInformation("Tool call: GetSourceSubtitles for job {JobId}", jobId);

        if (_currentJob == null || _currentJob.JobId != jobId)
        {
            return $"Error: Job {jobId} not found in current context";
        }

        var sourceUrl = _currentJob.Result?.StoredOutputs?.SourceSubtitleUrl 
            ?? _currentJob.Result?.SourceSubtitleUrl;

        if (string.IsNullOrEmpty(sourceUrl))
        {
            return "Error: Source subtitles not available for this job. The job may not have completed translation yet.";
        }

        try
        {
            return await FetchSubtitleContentAsync(sourceUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch source subtitles for job {JobId}", jobId);
            return $"Error fetching source subtitles: {ex.Message}";
        }
    }

    /// <summary>
    /// Get the translated target language subtitles (VTT format) for a job.
    /// </summary>
    /// <param name="jobId">The job ID to get target subtitles for.</param>
    /// <returns>The translated subtitle content in WebVTT format.</returns>
    [Description("Get the translated target language subtitles in WebVTT format. This is the primary content to validate for translation quality, grammar, and timing.")]
    public async Task<string> GetTargetSubtitles(
        [Description("The job ID to retrieve target subtitles for")] string jobId)
    {
        _logger.LogInformation("Tool call: GetTargetSubtitles for job {JobId}", jobId);

        if (_currentJob == null || _currentJob.JobId != jobId)
        {
            return $"Error: Job {jobId} not found in current context";
        }

        var targetUrl = _currentJob.Result?.StoredOutputs?.TargetSubtitleUrl 
            ?? _currentJob.Result?.TargetSubtitleUrl;

        if (string.IsNullOrEmpty(targetUrl))
        {
            return "Error: Target subtitles not available for this job. The job may not have completed translation yet.";
        }

        try
        {
            return await FetchSubtitleContentAsync(targetUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch target subtitles for job {JobId}", jobId);
            return $"Error fetching target subtitles: {ex.Message}";
        }
    }

    /// <summary>
    /// Fetches subtitle content from a URL (with or without SAS token).
    /// </summary>
    private async Task<string> FetchSubtitleContentAsync(string url)
    {
        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(30);
        
        var response = await httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        
        var content = await response.Content.ReadAsStringAsync();
        
        // Truncate if too long to avoid token limits
        const int maxLength = 50000; // ~12k tokens
        if (content.Length > maxLength)
        {
            _logger.LogWarning("Subtitle content truncated from {Original} to {Max} characters", 
                content.Length, maxLength);
            content = content.Substring(0, maxLength) + "\n\n[Content truncated due to length...]";
        }

        return content;
    }
}
