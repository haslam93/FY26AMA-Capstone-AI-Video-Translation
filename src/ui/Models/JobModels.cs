namespace VideoTranslation.UI.Models;

/// <summary>
/// Request to create a new translation job.
/// </summary>
public class CreateJobRequest
{
    public string? DisplayName { get; set; }
    public string? VideoUrl { get; set; }
    public string? BlobPath { get; set; }
    public string SourceLocale { get; set; } = "en-US";
    public string TargetLocale { get; set; } = "es-ES";
    public string VoiceKind { get; set; } = "PlatformVoice";  // PersonalVoice requires special approval
    public int? SpeakerCount { get; set; }
}

/// <summary>
/// Response from video upload.
/// </summary>
public class UploadResponse
{
    public string UploadId { get; set; } = string.Empty;
    public string BlobPath { get; set; } = string.Empty;
    public string BlobUrl { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
}

/// <summary>
/// Response from job creation.
/// </summary>
public class CreateJobResponse
{
    public string JobId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string StatusUrl { get; set; } = string.Empty;
}

/// <summary>
/// Job status response.
/// </summary>
public class JobStatusResponse
{
    public string JobId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? StatusMessage { get; set; }
    public int IterationNumber { get; set; }
    public string SourceLocale { get; set; } = string.Empty;
    public string TargetLocale { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime LastUpdatedAt { get; set; }
    public JobResultDto? Result { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Job result containing output URLs.
/// </summary>
public class JobResultDto
{
    public string? TranslatedVideoUrl { get; set; }
    public string? SourceWebVttUrl { get; set; }
    public string? TargetWebVttUrl { get; set; }
}

/// <summary>
/// List of jobs response.
/// </summary>
public class JobListResponse
{
    public List<JobSummary> Jobs { get; set; } = new();
}

/// <summary>
/// Summary of a job for listing.
/// </summary>
public class JobSummary
{
    public string JobId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime LastUpdatedAt { get; set; }
}

/// <summary>
/// Supported locale information.
/// </summary>
public class SupportedLocale
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
