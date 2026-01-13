using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using VideoTranslation.Api.Models;
using VideoTranslation.Api.Services;

namespace VideoTranslation.Api.Activities;

/// <summary>
/// Activity function to copy outputs from Speech API to our blob storage.
/// </summary>
public class CopyOutputsActivity
{
    private readonly IBlobStorageService _blobService;
    private readonly ILogger<CopyOutputsActivity> _logger;

    public CopyOutputsActivity(
        IBlobStorageService blobService,
        ILogger<CopyOutputsActivity> logger)
    {
        _blobService = blobService;
        _logger = logger;
    }

    [Function(nameof(CopyOutputsActivity))]
    public async Task<CopyOutputsResult> RunAsync(
        [ActivityTrigger] CopyOutputsInput input)
    {
        _logger.LogInformation("Copying outputs for job {JobId}, iteration {IterationNumber}", 
            input.JobId, input.IterationNumber);

        var result = new CopyOutputsResult { Success = true };
        var storedOutputs = new StoredOutputs();

        try
        {
            var basePath = $"{input.JobId}/iteration-{input.IterationNumber}";

            // Copy translated video
            if (!string.IsNullOrEmpty(input.TranslatedVideoUrl))
            {
                _logger.LogInformation("Copying translated video...");
                storedOutputs.VideoUrl = await _blobService.CopyFromUrlAsync(
                    input.TranslatedVideoUrl,
                    "outputs",
                    $"{basePath}/translated-video.mp4");
            }

            // Copy source subtitles
            if (!string.IsNullOrEmpty(input.SourceSubtitleUrl))
            {
                _logger.LogInformation("Copying source subtitles...");
                storedOutputs.SourceSubtitleUrl = await _blobService.CopyFromUrlAsync(
                    input.SourceSubtitleUrl,
                    "subtitles",
                    $"{basePath}/source-subtitles.vtt");
            }

            // Copy target subtitles
            if (!string.IsNullOrEmpty(input.TargetSubtitleUrl))
            {
                _logger.LogInformation("Copying target subtitles...");
                storedOutputs.TargetSubtitleUrl = await _blobService.CopyFromUrlAsync(
                    input.TargetSubtitleUrl,
                    "subtitles",
                    $"{basePath}/target-subtitles.vtt");
            }

            // Copy metadata
            if (!string.IsNullOrEmpty(input.MetadataUrl))
            {
                _logger.LogInformation("Copying metadata...");
                storedOutputs.MetadataUrl = await _blobService.CopyFromUrlAsync(
                    input.MetadataUrl,
                    "subtitles",
                    $"{basePath}/metadata.json");
            }

            result.StoredOutputs = storedOutputs;
            _logger.LogInformation("Successfully copied all outputs for job {JobId}", input.JobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to copy outputs for job {JobId}", input.JobId);
            result.Success = false;
            result.Error = ex.Message;
        }

        return result;
    }
}

public class CopyOutputsInput
{
    public string JobId { get; set; } = string.Empty;
    public int IterationNumber { get; set; }
    public string? TranslatedVideoUrl { get; set; }
    public string? SourceSubtitleUrl { get; set; }
    public string? TargetSubtitleUrl { get; set; }
    public string? MetadataUrl { get; set; }
}

public class CopyOutputsResult
{
    public bool Success { get; set; }
    public StoredOutputs? StoredOutputs { get; set; }
    public string? Error { get; set; }
}
