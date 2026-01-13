using System.Text.Json.Serialization;

namespace VideoTranslation.Api.Models;

/// <summary>
/// Represents the state of a translation job throughout its lifecycle.
/// Used as the orchestrator state.
/// </summary>
public class TranslationJob
{
    /// <summary>
    /// Unique job ID (used as orchestration instance ID).
    /// </summary>
    [JsonPropertyName("jobId")]
    public string JobId { get; set; } = string.Empty;

    /// <summary>
    /// Translation ID used with Speech API.
    /// </summary>
    [JsonPropertyName("translationId")]
    public string TranslationId { get; set; } = string.Empty;

    /// <summary>
    /// Current iteration ID.
    /// </summary>
    [JsonPropertyName("iterationId")]
    public string? IterationId { get; set; }

    /// <summary>
    /// Current iteration number (1-based).
    /// </summary>
    [JsonPropertyName("iterationNumber")]
    public int IterationNumber { get; set; } = 0;

    /// <summary>
    /// Display name for the job.
    /// </summary>
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    /// <summary>
    /// Current job status.
    /// </summary>
    [JsonPropertyName("status")]
    public JobStatus Status { get; set; } = JobStatus.Submitted;

    /// <summary>
    /// Detailed status message.
    /// </summary>
    [JsonPropertyName("statusMessage")]
    public string? StatusMessage { get; set; }

    /// <summary>
    /// Original request parameters.
    /// </summary>
    [JsonPropertyName("request")]
    public TranslationJobRequest Request { get; set; } = new();

    /// <summary>
    /// URL to the video file (resolved from URL or blob path).
    /// </summary>
    [JsonPropertyName("videoFileUrl")]
    public string? VideoFileUrl { get; set; }

    /// <summary>
    /// Result URLs when job completes successfully.
    /// </summary>
    [JsonPropertyName("result")]
    public TranslationResult? Result { get; set; }

    /// <summary>
    /// Job creation timestamp.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last update timestamp.
    /// </summary>
    [JsonPropertyName("lastUpdatedAt")]
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Error message if job failed.
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

/// <summary>
/// Job status enumeration matching the orchestrator state machine.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum JobStatus
{
    Submitted,
    Validating,
    Validated,
    CreatingTranslation,
    TranslationCreated,
    CreatingIteration,
    IterationCreated,
    Processing,
    CopyingOutputs,
    Completed,
    Failed
}

/// <summary>
/// Result URLs from a successful translation.
/// </summary>
public class TranslationResult
{
    [JsonPropertyName("translatedVideoUrl")]
    public string? TranslatedVideoUrl { get; set; }

    [JsonPropertyName("sourceSubtitleUrl")]
    public string? SourceSubtitleUrl { get; set; }

    [JsonPropertyName("targetSubtitleUrl")]
    public string? TargetSubtitleUrl { get; set; }

    [JsonPropertyName("metadataUrl")]
    public string? MetadataUrl { get; set; }

    /// <summary>
    /// URLs copied to our storage (for persistence beyond Speech API retention).
    /// </summary>
    [JsonPropertyName("storedOutputs")]
    public StoredOutputs? StoredOutputs { get; set; }
}

/// <summary>
/// URLs to outputs stored in our blob storage.
/// </summary>
public class StoredOutputs
{
    [JsonPropertyName("videoUrl")]
    public string? VideoUrl { get; set; }

    [JsonPropertyName("sourceSubtitleUrl")]
    public string? SourceSubtitleUrl { get; set; }

    [JsonPropertyName("targetSubtitleUrl")]
    public string? TargetSubtitleUrl { get; set; }

    [JsonPropertyName("metadataUrl")]
    public string? MetadataUrl { get; set; }
}
