using VideoTranslation.Api.Models.SpeechApi;

namespace VideoTranslation.Api.Services;

/// <summary>
/// Service interface for interacting with the Azure Speech Video Translation API.
/// </summary>
public interface ISpeechTranslationService
{
    /// <summary>
    /// Creates a new translation.
    /// </summary>
    /// <param name="translationId">Unique ID for the translation.</param>
    /// <param name="operationId">Unique ID for tracking the operation.</param>
    /// <param name="request">Translation request parameters.</param>
    /// <returns>Translation response.</returns>
    Task<TranslationResponse> CreateTranslationAsync(
        string translationId,
        string operationId,
        CreateTranslationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a translation by ID.
    /// </summary>
    Task<TranslationResponse?> GetTranslationAsync(
        string translationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates an iteration for a translation.
    /// </summary>
    /// <param name="translationId">Translation ID.</param>
    /// <param name="iterationId">Unique ID for the iteration.</param>
    /// <param name="operationId">Unique ID for tracking the operation.</param>
    /// <param name="request">Iteration request parameters.</param>
    /// <returns>Iteration response.</returns>
    Task<IterationResponse> CreateIterationAsync(
        string translationId,
        string iterationId,
        string operationId,
        CreateIterationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an iteration by ID.
    /// </summary>
    Task<IterationResponse?> GetIterationAsync(
        string translationId,
        string iterationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets operation status by operation ID.
    /// </summary>
    Task<OperationResponse?> GetOperationAsync(
        string operationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a translation and all its iterations.
    /// </summary>
    Task DeleteTranslationAsync(
        string translationId,
        CancellationToken cancellationToken = default);
}
