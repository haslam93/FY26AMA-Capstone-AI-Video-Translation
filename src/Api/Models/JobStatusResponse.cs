using System.Text.Json.Serialization;

namespace VideoTranslation.Api.Models;

/// <summary>
/// API response model for job status.
/// </summary>
public class JobStatusResponse
{
    [JsonPropertyName("jobId")]
    public string JobId { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("statusMessage")]
    public string? StatusMessage { get; set; }

    [JsonPropertyName("iterationNumber")]
    public int IterationNumber { get; set; }

    [JsonPropertyName("sourceLocale")]
    public string? SourceLocale { get; set; }

    [JsonPropertyName("targetLocale")]
    public string? TargetLocale { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("lastUpdatedAt")]
    public DateTime LastUpdatedAt { get; set; }

    [JsonPropertyName("result")]
    public TranslationResult? Result { get; set; }

    [JsonPropertyName("validationResult")]
    public SubtitleValidationResult? ValidationResult { get; set; }

    [JsonPropertyName("validationThreadId")]
    public string? ValidationThreadId { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>
    /// Creates a JobStatusResponse from a TranslationJob.
    /// </summary>
    public static JobStatusResponse FromJob(TranslationJob job)
    {
        return new JobStatusResponse
        {
            JobId = job.JobId,
            DisplayName = job.DisplayName,
            Status = job.Status.ToString(),
            StatusMessage = job.StatusMessage,
            IterationNumber = job.IterationNumber,
            SourceLocale = job.Request.SourceLocale,
            TargetLocale = job.Request.TargetLocale,
            CreatedAt = job.CreatedAt,
            LastUpdatedAt = job.LastUpdatedAt,
            Result = job.Result,
            ValidationResult = job.ValidationResult,
            ValidationThreadId = job.ValidationThreadId,
            Error = job.Error
        };
    }
}

/// <summary>
/// API response for job creation.
/// </summary>
public class CreateJobResponse
{
    [JsonPropertyName("jobId")]
    public string JobId { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("statusUrl")]
    public string StatusUrl { get; set; } = string.Empty;
}
