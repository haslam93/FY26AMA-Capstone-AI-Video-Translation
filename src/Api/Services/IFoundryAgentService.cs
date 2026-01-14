using VideoTranslation.Api.Models;

namespace VideoTranslation.Api.Services;

/// <summary>
/// Service interface for Azure AI Foundry Agent operations.
/// </summary>
public interface IFoundryAgentService
{
    /// <summary>
    /// Ensures the validation agent exists, creating it if necessary.
    /// </summary>
    /// <returns>The agent ID.</returns>
    Task<string> EnsureAgentExistsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs the subtitle validation agent for a job.
    /// Creates a thread, runs the agent with tools, and returns the validation result.
    /// </summary>
    /// <param name="job">The translation job to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The validation result and thread ID for future interactions.</returns>
    Task<ValidationAgentResult> RunValidationAsync(
        TranslationJob job,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a follow-up message to an existing validation thread.
    /// Used for interactive review where a human reviewer can ask questions.
    /// </summary>
    /// <param name="threadId">The thread ID from a previous validation run.</param>
    /// <param name="message">The reviewer's question or comment.</param>
    /// <param name="job">The translation job (for tool context).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The agent's response.</returns>
    Task<string> SendFollowUpMessageAsync(
        string threadId,
        string message,
        TranslationJob job,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the conversation history for a thread.
    /// </summary>
    /// <param name="threadId">The thread ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of messages in the conversation.</returns>
    Task<IReadOnlyList<ConversationMessage>> GetConversationHistoryAsync(
        string threadId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result from running the validation agent.
/// </summary>
public class ValidationAgentResult
{
    /// <summary>
    /// Whether the validation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The thread ID for follow-up interactions.
    /// </summary>
    public string? ThreadId { get; set; }

    /// <summary>
    /// The parsed validation result.
    /// </summary>
    public SubtitleValidationResult? ValidationResult { get; set; }

    /// <summary>
    /// Raw agent response text.
    /// </summary>
    public string? RawResponse { get; set; }

    /// <summary>
    /// Error message if validation failed.
    /// </summary>
    public string? Error { get; set; }
}

/// <summary>
/// A message in the conversation thread.
/// </summary>
public class ConversationMessage
{
    /// <summary>
    /// Role of the message sender (user or assistant).
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Content of the message.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp of the message.
    /// </summary>
    public DateTime Timestamp { get; set; }
}
