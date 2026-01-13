using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using VideoTranslation.Api.Models;
using VideoTranslation.Api.Models.SpeechApi;
using VideoTranslation.Api.Services;

namespace VideoTranslation.Api.Activities;

/// <summary>
/// Activity function to create an iteration for a translation.
/// </summary>
public class CreateIterationActivity
{
    private readonly ISpeechTranslationService _speechService;
    private readonly IBlobStorageService _blobService;
    private readonly ILogger<CreateIterationActivity> _logger;

    public CreateIterationActivity(
        ISpeechTranslationService speechService,
        IBlobStorageService blobService,
        ILogger<CreateIterationActivity> logger)
    {
        _speechService = speechService;
        _blobService = blobService;
        _logger = logger;
    }

    [Function(nameof(CreateIterationActivity))]
    public async Task<CreateIterationResult> RunAsync(
        [ActivityTrigger] CreateIterationInput input)
    {
        _logger.LogInformation("Creating iteration {IterationId} for translation {TranslationId}", 
            input.IterationId, input.TranslationId);

        try
        {
            var iterationInput = new IterationInput
            {
                SpeakerCount = input.SpeakerCount,
                SubtitleMaxCharCountPerSegment = input.SubtitleMaxCharCountPerSegment,
                ExportSubtitleInVideo = input.ExportSubtitleInVideo
            };

            // If WebVTT file is provided (for subsequent iterations)
            if (!string.IsNullOrEmpty(input.WebvttUrl))
            {
                iterationInput.WebvttFile = new WebvttFileReference
                {
                    Url = input.WebvttUrl
                };
            }
            else if (!string.IsNullOrEmpty(input.WebvttBlobPath))
            {
                // Generate SAS URL for the WebVTT file
                var webvttSasUrl = await _blobService.GenerateSasUrlAsync(
                    "subtitles",
                    input.WebvttBlobPath,
                    TimeSpan.FromHours(2));

                iterationInput.WebvttFile = new WebvttFileReference
                {
                    Url = webvttSasUrl
                };
            }

            var request = new CreateIterationRequest
            {
                Input = iterationInput
            };

            var operationId = $"create-iteration-{input.IterationId}";
            var response = await _speechService.CreateIterationAsync(
                input.TranslationId,
                input.IterationId,
                operationId,
                request);

            _logger.LogInformation("Iteration {IterationId} created with status {Status}", 
                input.IterationId, response.Status);

            return new CreateIterationResult
            {
                Success = true,
                IterationId = response.Id ?? input.IterationId,
                OperationId = operationId,
                Status = response.Status
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create iteration {IterationId}", input.IterationId);
            return new CreateIterationResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }
}

public class CreateIterationInput
{
    public string TranslationId { get; set; } = string.Empty;
    public string IterationId { get; set; } = string.Empty;
    public int? SpeakerCount { get; set; }
    public int? SubtitleMaxCharCountPerSegment { get; set; }
    public bool? ExportSubtitleInVideo { get; set; }
    public string? WebvttUrl { get; set; }
    public string? WebvttBlobPath { get; set; }
}

public class CreateIterationResult
{
    public bool Success { get; set; }
    public string? IterationId { get; set; }
    public string? OperationId { get; set; }
    public string? Status { get; set; }
    public string? Error { get; set; }
}
