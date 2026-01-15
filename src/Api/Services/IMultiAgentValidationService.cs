using VideoTranslation.Api.Models;

namespace VideoTranslation.Api.Services;

/// <summary>
/// Interface for the multi-agent subtitle validation service.
/// 
/// Architecture Overview:
/// ----------------------
/// The multi-agent system consists of 4 specialized agents:
/// 1. OrchestratorAgent - Coordinates workflow, aggregates results
/// 2. TranslationReviewAgent - Analyzes semantic accuracy, grammar, fluency
/// 3. TechnicalReviewAgent - Analyzes timing, sync, formatting
/// 4. CulturalReviewAgent - Analyzes cultural adaptation, idioms, tone
/// 
/// All agents run in PARALLEL for faster execution.
/// 
/// Model Configuration:
/// -------------------
/// By default, all agents use the same model deployment (e.g., gpt-4o-mini).
/// To use different models for different agents, configure in appsettings.json:
/// 
/// {
///   "AIFoundry": {
///     "ProjectEndpoint": "https://your-project.ai.azure.com",
///     "DefaultModelDeployment": "gpt-4o-mini",
///     "AgentModels": {
///       "Orchestrator": "gpt-4o",        // Optional: Use more powerful model
///       "Translation": "gpt-4o-mini",    // Default
///       "Technical": "gpt-4o-mini",      // Default
///       "Cultural": "gpt-4o-mini"        // Default
///     }
///   }
/// }
/// 
/// If AgentModels is not specified, DefaultModelDeployment is used for all agents.
/// </summary>
public interface IMultiAgentValidationService
{
    /// <summary>
    /// Ensures all specialist agents exist in Azure AI Foundry.
    /// Creates agents if they don't exist.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dictionary of agent type to agent ID.</returns>
    Task<Dictionary<string, string>> EnsureAgentsExistAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs the multi-agent validation pipeline on a translation job.
    /// All specialist agents run in PARALLEL for performance.
    /// </summary>
    /// <param name="job">The translation job to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Aggregated validation result from all agents.</returns>
    Task<MultiAgentValidationResult> RunValidationAsync(
        TranslationJob job,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a follow-up message to a specific agent.
    /// </summary>
    /// <param name="agentType">Type of agent: "orchestrator", "translation", "technical", "cultural"</param>
    /// <param name="threadId">Thread ID for the conversation.</param>
    /// <param name="message">User message.</param>
    /// <param name="job">Job context for tool calls.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Agent's response.</returns>
    Task<string> SendMessageToAgentAsync(
        string agentType,
        string threadId,
        string message,
        TranslationJob job,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets conversation history for a specific agent thread.
    /// </summary>
    /// <param name="threadId">Thread ID for the conversation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of messages in chronological order.</returns>
    Task<IReadOnlyList<ConversationMessage>> GetConversationHistoryAsync(
        string threadId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the thread ID for a specific agent type from the validation result.
    /// </summary>
    /// <param name="validation">The multi-agent validation result.</param>
    /// <param name="agentType">Type of agent: "orchestrator", "translation", "technical", "cultural"</param>
    /// <returns>Thread ID or null if not found.</returns>
    string? GetThreadIdForAgent(MultiAgentValidationResult? validation, string agentType);
}
