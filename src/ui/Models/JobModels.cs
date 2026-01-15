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
    public SubtitleValidationResult? ValidationResult { get; set; }
    public string? ValidationThreadId { get; set; }
    
    /// <summary>
    /// Multi-agent validation result with per-agent scores.
    /// </summary>
    public MultiAgentValidationResult? MultiAgentValidation { get; set; }
    
    public string? Error { get; set; }
    
    /// <summary>
    /// Checks if this job has multi-agent validation results.
    /// </summary>
    public bool HasMultiAgentValidation => MultiAgentValidation != null;
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

/// <summary>
/// Request to send a chat message to the validation agent.
/// </summary>
public class ChatRequest
{
    /// <summary>
    /// The message to send to the agent.
    /// </summary>
    public string? Message { get; set; }
    
    /// <summary>
    /// The type of agent to route the message to.
    /// Values: "orchestrator" (default), "translation", "technical", "cultural"
    /// </summary>
    public string? AgentType { get; set; }
}

/// <summary>
/// Response from the validation agent chat.
/// </summary>
public class ChatResponse
{
    public string? Message { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Conversation history response.
/// </summary>
public class ChatHistoryResponse
{
    public List<ConversationMessage> Messages { get; set; } = new();
    public string? ThreadId { get; set; }
}

/// <summary>
/// A message in the conversation.
/// </summary>
public class ConversationMessage
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    
    /// <summary>
    /// The type of agent that sent/received this message (for multi-agent systems).
    /// Values: "Orchestrator", "Translation", "Technical", "Cultural", or null for single-agent.
    /// </summary>
    public string? AgentType { get; set; }
}

#region Multi-Agent Validation Models

/// <summary>
/// Result from the multi-agent validation pipeline.
/// Contains aggregated results from all specialist agents.
/// </summary>
public class MultiAgentValidationResult
{
    /// <summary>
    /// Whether the validation passed overall.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Weighted average score from all agents (0-100).
    /// Weights: Translation 40%, Technical 30%, Cultural 30%
    /// </summary>
    public double OverallScore { get; set; }

    /// <summary>
    /// Final recommendation: "Approve", "NeedsReview", or "Reject"
    /// </summary>
    public string Recommendation { get; set; } = "NeedsReview";

    /// <summary>
    /// Human-readable summary from the orchestrator agent.
    /// </summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// Results from the Translation Review Agent.
    /// </summary>
    public AgentReviewResult? TranslationReview { get; set; }

    /// <summary>
    /// Results from the Technical Review Agent.
    /// </summary>
    public AgentReviewResult? TechnicalReview { get; set; }

    /// <summary>
    /// Results from the Cultural Review Agent.
    /// </summary>
    public AgentReviewResult? CulturalReview { get; set; }

    /// <summary>
    /// Thread ID for the Orchestrator Agent (for follow-up chat).
    /// </summary>
    public string? OrchestratorThreadId { get; set; }

    /// <summary>
    /// Thread ID for the Translation Review Agent.
    /// </summary>
    public string? TranslationAgentThreadId { get; set; }

    /// <summary>
    /// Thread ID for the Technical Review Agent.
    /// </summary>
    public string? TechnicalAgentThreadId { get; set; }

    /// <summary>
    /// Thread ID for the Cultural Review Agent.
    /// </summary>
    public string? CulturalAgentThreadId { get; set; }

    /// <summary>
    /// All issues found by all agents.
    /// </summary>
    public List<MultiAgentIssue> AllIssues { get; set; } = new();

    /// <summary>
    /// When the validation was completed.
    /// </summary>
    public DateTime ValidatedAt { get; set; }

    // Convenience alias properties for easier Razor binding
    public AgentReviewResult? TranslationAgent => TranslationReview;
    public AgentReviewResult? TechnicalAgent => TechnicalReview;
    public AgentReviewResult? CulturalAgent => CulturalReview;
    public string? OrchestratorSummary => Summary;

    /// <summary>
    /// Gets the recommendation badge color class.
    /// </summary>
    public string RecommendationBadgeClass => Recommendation switch
    {
        "Approve" => "bg-success",
        "NeedsReview" => "bg-warning text-dark",
        "Reject" => "bg-danger",
        _ => "bg-secondary"
    };

    /// <summary>
    /// Gets the overall score color class.
    /// </summary>
    public string ScoreColorClass => OverallScore switch
    {
        >= 80 => "text-success",
        >= 50 => "text-warning",
        _ => "text-danger"
    };

    /// <summary>
    /// Gets the recommendation icon.
    /// </summary>
    public string RecommendationIcon => Recommendation switch
    {
        "Approve" => "✅",
        "NeedsReview" => "⚠️",
        "Reject" => "❌",
        _ => "❓"
    };
}

/// <summary>
/// Result from a single specialist agent's review.
/// </summary>
public class AgentReviewResult
{
    /// <summary>
    /// Name of the agent.
    /// </summary>
    public string AgentName { get; set; } = string.Empty;

    /// <summary>
    /// Type: "translation", "technical", "cultural"
    /// </summary>
    public string AgentType { get; set; } = string.Empty;

    /// <summary>
    /// Score assigned by this agent (0-100).
    /// </summary>
    public double Score { get; set; }

    /// <summary>
    /// Detailed reasoning for the score.
    /// </summary>
    public string Reasoning { get; set; } = string.Empty;

    /// <summary>
    /// Issues found by this agent.
    /// </summary>
    public List<MultiAgentIssue> Issues { get; set; } = new();

    /// <summary>
    /// When this review was completed.
    /// </summary>
    public DateTime ReviewedAt { get; set; }

    /// <summary>
    /// Thread ID for follow-up chat with this agent.
    /// </summary>
    public string? ThreadId { get; set; }

    /// <summary>
    /// Gets the score color class for display.
    /// </summary>
    public string ScoreColorClass => Score switch
    {
        >= 80 => "text-success",
        >= 50 => "text-warning",
        _ => "text-danger"
    };

    /// <summary>
    /// Gets the icon for this agent type.
    /// </summary>
    public string AgentIcon => AgentType switch
    {
        "translation" => "📝",
        "technical" => "⚙️",
        "cultural" => "🌍",
        _ => "🤖"
    };
}

/// <summary>
/// A specific issue found by a multi-agent validator.
/// </summary>
public class MultiAgentIssue
{
    /// <summary>
    /// Severity: "critical", "major", "minor", "suggestion"
    /// </summary>
    public string Severity { get; set; } = "minor";

    /// <summary>
    /// Category/source: "translation", "technical", "cultural"
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Description of the issue.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Location in the subtitle file.
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// Suggested fix.
    /// </summary>
    public string? Suggestion { get; set; }

    /// <summary>
    /// Gets the severity badge class.
    /// </summary>
    public string SeverityBadgeClass => Severity?.ToLower() switch
    {
        "critical" => "badge-danger",
        "major" => "badge-warning",
        "minor" => "badge-info",
        "suggestion" => "badge-secondary",
        _ => "badge-secondary"
    };

    /// <summary>
    /// Gets the category icon.
    /// </summary>
    public string CategoryIcon => Category?.ToLower() switch
    {
        "translation" => "📝",
        "technical" => "⚙️",
        "cultural" => "🌍",
        _ => "❓"
    };
}

#endregion
