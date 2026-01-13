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
    public bool ExportSubtitleInVideo { get; set; } = false;
    public int? SubtitleMaxCharCountPerSegment { get; set; } = 50;
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
    public string? SourceSubtitleUrl { get; set; }
    public string? TargetSubtitleUrl { get; set; }
    public string? MetadataUrl { get; set; }
}

/// <summary>
/// Subtitle validation result from the AI agent.
/// </summary>
public class SubtitleValidationResult
{
    public bool IsValid { get; set; }
    public double ConfidenceScore { get; set; }
    public List<ValidationIssue> Issues { get; set; } = new();
    public string Reasoning { get; set; } = string.Empty;
    public DateTime ValidatedAt { get; set; }
    public int SourceCueCount { get; set; }
    public int TargetCueCount { get; set; }
    public int SourceWordCount { get; set; }
    public int TargetWordCount { get; set; }
    public ValidationCategoryScores? CategoryScores { get; set; }
}

/// <summary>
/// Validation scores by category.
/// </summary>
public class ValidationCategoryScores
{
    public double TimingScore { get; set; }
    public double TranslationAccuracyScore { get; set; }
    public double GrammarScore { get; set; }
    public double FormattingScore { get; set; }
    public double CulturalContextScore { get; set; }
}

/// <summary>
/// A validation issue found in subtitles.
/// </summary>
public class ValidationIssue
{
    public int Severity { get; set; }
    public int Category { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Timestamp { get; set; }
    public string? SuggestedFix { get; set; }
    
    /// <summary>
    /// Gets the severity as a display string.
    /// </summary>
    public string SeverityText => Severity switch
    {
        0 => "Low",
        1 => "Medium",
        2 => "High",
        3 => "Critical",
        _ => "Unknown"
    };
    
    /// <summary>
    /// Gets the category as a display string.
    /// </summary>
    public string CategoryText => Category switch
    {
        0 => "Timing",
        1 => "Grammar",
        2 => "TranslationAccuracy",
        3 => "CulturalContext",
        4 => "Formatting",
        5 => "ContentAppropriateness",
        6 => "MissingContent",
        _ => "Unknown"
    };
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

/// <summary>
/// Response from pending approvals endpoint.
/// </summary>
public class PendingApprovalsResponse
{
    public List<PendingApprovalJob> Jobs { get; set; } = new();
}

/// <summary>
/// Job pending human approval.
/// </summary>
public class PendingApprovalJob
{
    public string JobId { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string SourceLocale { get; set; } = string.Empty;
    public string TargetLocale { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public SubtitleValidationResult? ValidationResult { get; set; }
    public DateTime? ApprovalRequestedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Response from approve/reject endpoints.
/// </summary>
public class ApproveRejectResponse
{
    public string Message { get; set; } = string.Empty;
    public string JobId { get; set; } = string.Empty;
    public string? ReviewedBy { get; set; }
    public string? Reason { get; set; }
}

/// <summary>
/// Human approval decision.
/// </summary>
public class ApprovalDecision
{
    public bool Approved { get; set; }
    public string? ReviewedBy { get; set; }
    public string? Reason { get; set; }
    public string? Comments { get; set; }
}
