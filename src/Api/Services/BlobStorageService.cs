using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace VideoTranslation.Api.Services;

/// <summary>
/// Configuration options for blob storage.
/// </summary>
public class BlobStorageOptions
{
    public const string SectionName = "BlobStorage";

    /// <summary>
    /// Storage account name.
    /// </summary>
    public string AccountName { get; set; } = string.Empty;

    /// <summary>
    /// Optional connection string (used for local development).
    /// If not provided, uses DefaultAzureCredential.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Container for input videos.
    /// </summary>
    public string VideosContainer { get; set; } = "videos";

    /// <summary>
    /// Container for translated outputs.
    /// </summary>
    public string OutputsContainer { get; set; } = "outputs";

    /// <summary>
    /// Container for subtitles.
    /// </summary>
    public string SubtitlesContainer { get; set; } = "subtitles";
}

/// <summary>
/// Implementation of blob storage operations using Azure.Storage.Blobs SDK.
/// </summary>
public class BlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly BlobStorageOptions _options;
    private readonly ILogger<BlobStorageService>? _logger;

    /// <summary>
    /// Constructor for DI with full options and logging.
    /// </summary>
    public BlobStorageService(
        IOptions<BlobStorageOptions> options,
        ILogger<BlobStorageService> logger)
    {
        _options = options.Value;
        _logger = logger;

        if (!string.IsNullOrEmpty(_options.ConnectionString))
        {
            _blobServiceClient = new BlobServiceClient(_options.ConnectionString);
        }
        else
        {
            var serviceUri = new Uri($"https://{_options.AccountName}.blob.core.windows.net");
            _blobServiceClient = new BlobServiceClient(serviceUri, new DefaultAzureCredential());
        }
    }

    /// <summary>
    /// Simple constructor for connection string only.
    /// </summary>
    public BlobStorageService(string connectionString)
    {
        _blobServiceClient = new BlobServiceClient(connectionString);
        _options = new BlobStorageOptions
        {
            ConnectionString = connectionString
        };
        _logger = null;
    }

    public async Task<string> GenerateSasUrlAsync(
        string containerName,
        string blobPath,
        TimeSpan expiresIn,
        CancellationToken cancellationToken = default)
    {
        _logger?.LogDebug("Generating SAS URL for {Container}/{BlobPath}", containerName, blobPath);

        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobPath);

        // Use user delegation key for better security (requires managed identity)
        var userDelegationKey = await _blobServiceClient.GetUserDelegationKeyAsync(
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.Add(expiresIn),
            cancellationToken);

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = containerName,
            BlobName = blobPath,
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.Add(expiresIn)
        };
        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        var sasToken = sasBuilder.ToSasQueryParameters(userDelegationKey.Value, _options.AccountName).ToString();
        return $"{blobClient.Uri}?{sasToken}";
    }

    public async Task<string> CopyFromUrlAsync(
        string sourceUrl,
        string containerName,
        string blobPath,
        CancellationToken cancellationToken = default)
    {
        _logger?.LogInformation("Copying from URL to {Container}/{BlobPath}", containerName, blobPath);

        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var blobClient = containerClient.GetBlobClient(blobPath);

        // Start the copy operation
        var copyOperation = await blobClient.StartCopyFromUriAsync(new Uri(sourceUrl), cancellationToken: cancellationToken);
        
        // Wait for copy to complete
        await copyOperation.WaitForCompletionAsync(cancellationToken);

        _logger?.LogInformation("Copy completed to {Container}/{BlobPath}", containerName, blobPath);
        return blobClient.Uri.ToString();
    }

    public async Task<string> UploadAsync(
        Stream content,
        string containerName,
        string blobPath,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        _logger?.LogInformation("Uploading to {Container}/{BlobPath}", containerName, blobPath);

        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var blobClient = containerClient.GetBlobClient(blobPath);

        await blobClient.UploadAsync(
            content,
            new BlobHttpHeaders { ContentType = contentType },
            cancellationToken: cancellationToken);

        _logger?.LogInformation("Upload completed to {Container}/{BlobPath}", containerName, blobPath);
        return blobClient.Uri.ToString();
    }

    public async Task<bool> ExistsAsync(
        string containerName,
        string blobPath,
        CancellationToken cancellationToken = default)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobPath);
        return await blobClient.ExistsAsync(cancellationToken);
    }

    public async Task<string> ReadAsStringAsync(
        string containerName,
        string blobPath,
        CancellationToken cancellationToken = default)
    {
        _logger?.LogInformation("Reading content from {Container}/{BlobPath}", containerName, blobPath);

        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobPath);
        
        var response = await blobClient.DownloadContentAsync(cancellationToken);
        return response.Value.Content.ToString();
    }

    public async Task DeleteAsync(
        string containerName,
        string blobPath,
        CancellationToken cancellationToken = default)
    {
        _logger?.LogInformation("Deleting {Container}/{BlobPath}", containerName, blobPath);

        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobPath);
        await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }
}
