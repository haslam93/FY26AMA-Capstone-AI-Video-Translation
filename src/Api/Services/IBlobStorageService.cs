namespace VideoTranslation.Api.Services;

/// <summary>
/// Service interface for Azure Blob Storage operations.
/// </summary>
public interface IBlobStorageService
{
    /// <summary>
    /// Generates a SAS URL for a blob that allows read access.
    /// </summary>
    /// <param name="containerName">Name of the container.</param>
    /// <param name="blobPath">Path to the blob within the container.</param>
    /// <param name="expiresIn">How long the SAS token is valid.</param>
    /// <returns>Full URL with SAS token.</returns>
    Task<string> GenerateSasUrlAsync(
        string containerName,
        string blobPath,
        TimeSpan expiresIn,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Copies a file from a URL to blob storage.
    /// </summary>
    /// <param name="sourceUrl">URL of the source file.</param>
    /// <param name="containerName">Destination container name.</param>
    /// <param name="blobPath">Destination blob path.</param>
    /// <returns>URL to the copied blob.</returns>
    Task<string> CopyFromUrlAsync(
        string sourceUrl,
        string containerName,
        string blobPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads content to blob storage.
    /// </summary>
    /// <param name="content">Content to upload.</param>
    /// <param name="containerName">Container name.</param>
    /// <param name="blobPath">Blob path.</param>
    /// <param name="contentType">Content type (MIME type).</param>
    /// <returns>URL to the uploaded blob.</returns>
    Task<string> UploadAsync(
        Stream content,
        string containerName,
        string blobPath,
        string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a blob exists.
    /// </summary>
    Task<bool> ExistsAsync(
        string containerName,
        string blobPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a blob if it exists.
    /// </summary>
    Task DeleteAsync(
        string containerName,
        string blobPath,
        CancellationToken cancellationToken = default);
}
