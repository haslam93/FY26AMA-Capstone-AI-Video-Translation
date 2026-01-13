// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace VideoTranslation.Api.Models;

/// <summary>
/// Represents the state of a video translation workflow in the multi-agent system.
/// Tracks progress through supervisor, validation, and human review stages.
/// </summary>
public class TranslationWorkflowState
{
    /// <summary>
    /// Unique identifier for the workflow instance.
    /// </summary>
    public string WorkflowId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// The translation job ID from the Speech Video Translation API.
    /// </summary>
    public string TranslationJobId { get; set; } = string.Empty;

    /// <summary>
    /// Current phase of the multi-agent workflow.
    /// </summary>
    public WorkflowPhase CurrentPhase { get; set; } = WorkflowPhase.Pending;

    /// <summary>
    /// Overall status of the workflow.
    /// </summary>
    public WorkflowStatus Status { get; set; } = WorkflowStatus.NotStarted;

    /// <summary>
    /// Timestamp when the workflow was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when the workflow was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Input video URL for translation.
    /// </summary>
    public string InputVideoUrl { get; set; } = string.Empty;

    /// <summary>
    /// Source language code (e.g., "en-US").
    /// </summary>
    public string SourceLanguage { get; set; } = string.Empty;

    /// <summary>
    /// Target language code (e.g., "es-ES").
    /// </summary>
    public string TargetLanguage { get; set; } = string.Empty;

    /// <summary>
    /// Result from the subtitle validation agent.
    /// </summary>
    public SubtitleValidationResult? SubtitleValidation { get; set; }

    /// <summary>
    /// Result from the human review process.
    /// </summary>
    public HumanReviewResult? HumanReview { get; set; }

    /// <summary>
    /// Agent conversation history for auditability.
    /// </summary>
    public List<AgentMessage> ConversationHistory { get; set; } = new();

    /// <summary>
    /// Error message if the workflow failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Output URLs after successful translation.
    /// </summary>
    public TranslationOutputUrls? OutputUrls { get; set; }
}

/// <summary>
/// Phases of the multi-agent translation workflow.
/// </summary>
public enum WorkflowPhase
{
    /// <summary>Workflow not yet started.</summary>
    Pending,

    /// <summary>Supervisor agent is coordinating the translation.</summary>
    SupervisorInitialization,

    /// <summary>Video is being translated by Azure Speech API.</summary>
    Translation,

    /// <summary>Subtitle validation agent is checking quality.</summary>
    SubtitleValidation,

    /// <summary>Waiting for human review and approval.</summary>
    HumanReview,

    /// <summary>Finalizing output and cleanup.</summary>
    Finalization,

    /// <summary>Workflow completed successfully.</summary>
    Completed,

    /// <summary>Workflow failed with an error.</summary>
    Failed
}

/// <summary>
/// Overall workflow status.
/// </summary>
public enum WorkflowStatus
{
    /// <summary>Workflow has not started.</summary>
    NotStarted,

    /// <summary>Workflow is in progress.</summary>
    InProgress,

    /// <summary>Workflow is paused waiting for human input.</summary>
    AwaitingHumanReview,

    /// <summary>Workflow completed successfully.</summary>
    Completed,

    /// <summary>Workflow failed.</summary>
    Failed,

    /// <summary>Workflow was cancelled.</summary>
    Cancelled
}

/// <summary>
/// Results from the subtitle validation agent.
/// </summary>
public class SubtitleValidationResult
{
    /// <summary>Whether subtitles passed validation.</summary>
    public bool IsValid { get; set; }

    /// <summary>Confidence score from 0.0 to 1.0.</summary>
    public double ConfidenceScore { get; set; }

    /// <summary>List of validation issues found.</summary>
    public List<ValidationIssue> Issues { get; set; } = new();

    /// <summary>Agent's reasoning for the validation result.</summary>
    public string Reasoning { get; set; } = string.Empty;

    /// <summary>Timestamp of validation.</summary>
    public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Number of cues in the source subtitle file.</summary>
    public int SourceCueCount { get; set; }

    /// <summary>Number of cues in the target subtitle file.</summary>
    public int TargetCueCount { get; set; }

    /// <summary>Total word count in source subtitles.</summary>
    public int SourceWordCount { get; set; }

    /// <summary>Total word count in target subtitles.</summary>
    public int TargetWordCount { get; set; }

    /// <summary>Detailed scores by category.</summary>
    public ValidationCategoryScores? CategoryScores { get; set; }
}

/// <summary>
/// Detailed validation scores by category.
/// </summary>
public class ValidationCategoryScores
{
    /// <summary>Score for timing and synchronization (0.0-1.0).</summary>
    public double TimingScore { get; set; }

    /// <summary>Score for translation accuracy (0.0-1.0).</summary>
    public double TranslationAccuracyScore { get; set; }

    /// <summary>Score for grammar and spelling (0.0-1.0).</summary>
    public double GrammarScore { get; set; }

    /// <summary>Score for formatting and readability (0.0-1.0).</summary>
    public double FormattingScore { get; set; }

    /// <summary>Score for cultural context adaptation (0.0-1.0).</summary>
    public double CulturalContextScore { get; set; }
}

/// <summary>
/// A specific validation issue found in subtitles.
/// </summary>
public class ValidationIssue
{
    /// <summary>Severity of the issue.</summary>
    public IssueSeverity Severity { get; set; }

    /// <summary>Category of the issue.</summary>
    public IssueCategory Category { get; set; }

    /// <summary>Description of the issue.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Timestamp or cue where the issue was found.</summary>
    public string? Timestamp { get; set; }

    /// <summary>Suggested fix for the issue.</summary>
    public string? SuggestedFix { get; set; }
}

/// <summary>
/// Severity levels for validation issues.
/// </summary>
public enum IssueSeverity
{
    /// <summary>Minor issue that doesn't affect understanding.</summary>
    Low,

    /// <summary>Moderate issue that may affect clarity.</summary>
    Medium,

    /// <summary>Significant issue that affects meaning.</summary>
    High,

    /// <summary>Critical issue that makes content unusable.</summary>
    Critical
}

/// <summary>
/// Categories of subtitle validation issues.
/// </summary>
public enum IssueCategory
{
    /// <summary>Timing synchronization issues.</summary>
    Timing,

    /// <summary>Grammar or spelling errors.</summary>
    Grammar,

    /// <summary>Translation accuracy issues.</summary>
    TranslationAccuracy,

    /// <summary>Cultural context issues.</summary>
    CulturalContext,

    /// <summary>Formatting issues (line length, etc.).</summary>
    Formatting,

    /// <summary>Content that may be offensive or inappropriate.</summary>
    ContentAppropriatenss,

    /// <summary>Missing or truncated text.</summary>
    MissingContent
}

/// <summary>
/// Results from the human review process.
/// </summary>
public class HumanReviewResult
{
    /// <summary>Whether the human approved the translation.</summary>
    public bool Approved { get; set; }

    /// <summary>Name or ID of the reviewer.</summary>
    public string ReviewerName { get; set; } = string.Empty;

    /// <summary>Feedback or comments from the reviewer.</summary>
    public string Feedback { get; set; } = string.Empty;

    /// <summary>List of edits requested by the reviewer.</summary>
    public List<string> RequestedEdits { get; set; } = new();

    /// <summary>Timestamp of the review.</summary>
    public DateTime ReviewedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// A message in the agent conversation history.
/// </summary>
public class AgentMessage
{
    /// <summary>Name of the agent that sent the message.</summary>
    public string AgentName { get; set; } = string.Empty;

    /// <summary>Role of the message sender.</summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>Content of the message.</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>Timestamp of the message.</summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// URLs for translation outputs.
/// </summary>
public class TranslationOutputUrls
{
    /// <summary>URL to the translated video.</summary>
    public string? TranslatedVideoUrl { get; set; }

    /// <summary>URL to source language subtitles (WebVTT).</summary>
    public string? SourceSubtitlesUrl { get; set; }

    /// <summary>URL to target language subtitles (WebVTT).</summary>
    public string? TargetSubtitlesUrl { get; set; }

    /// <summary>URL to video with burned-in subtitles (if requested).</summary>
    public string? BurnedSubtitlesVideoUrl { get; set; }
}
