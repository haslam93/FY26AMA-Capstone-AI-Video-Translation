using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using VideoTranslation.Api.Models;
using VideoTranslation.Api.Models.SpeechApi;
using VideoTranslation.Api.Services;

namespace VideoTranslation.Api.Activities;

/// <summary>
/// Activity function to create a translation via Speech API.
/// </summary>
public class CreateTranslationActivity
{
    private readonly ISpeechTranslationService _speechService;
    private readonly ILogger<CreateTranslationActivity> _logger;

    public CreateTranslationActivity(
        ISpeechTranslationService speechService,
        ILogger<CreateTranslationActivity> logger)
    {
        _speechService = speechService;
        _logger = logger;
    }

    [Function(nameof(CreateTranslationActivity))]
    public async Task<CreateTranslationResult> RunAsync(
        [ActivityTrigger] CreateTranslationInput input)
    {
        _logger.LogInformation("Creating translation {TranslationId} for job {JobId}", 
            input.TranslationId, input.Job.JobId);

        try
        {
            var request = new CreateTranslationRequest
            {
                DisplayName = input.Job.DisplayName ?? $"Translation-{input.Job.JobId}",
                Description = $"Video translation job {input.Job.JobId}",
                Input = new TranslationInput
                {
                    SourceLocale = input.Job.Request.SourceLocale,
                    TargetLocale = input.Job.Request.TargetLocale,
                    VoiceKind = input.Job.Request.VoiceKind,
                    SpeakerCount = input.Job.Request.SpeakerCount,
                    SubtitleMaxCharCountPerSegment = input.Job.Request.SubtitleMaxCharCountPerSegment,
                    ExportSubtitleInVideo = input.Job.Request.ExportSubtitleInVideo,
                    EnableLipSync = input.Job.Request.EnableLipSync,
                    VideoFileUrl = input.VideoFileUrl
                }
            };

            // Operation ID must be globally unique - include translation ID and a unique suffix
            var fullOperationId = $"trans-{input.TranslationId}-{Guid.NewGuid():N}";
            var operationId = fullOperationId.Length > 64 ? fullOperationId.Substring(0, 64) : fullOperationId;
            var response = await _speechService.CreateTranslationAsync(
                input.TranslationId,
                operationId,
                request);

            _logger.LogInformation("Translation {TranslationId} created with status {Status}", 
                input.TranslationId, response.Status);

            return new CreateTranslationResult
            {
                Success = true,
                TranslationId = response.Id ?? input.TranslationId,
                OperationId = operationId,
                Status = response.Status
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create translation {TranslationId}", input.TranslationId);
            return new CreateTranslationResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }
}

public class CreateTranslationInput
{
    public TranslationJob Job { get; set; } = null!;
    public string TranslationId { get; set; } = string.Empty;
    public string VideoFileUrl { get; set; } = string.Empty;
}

public class CreateTranslationResult
{
    public bool Success { get; set; }
    public string? TranslationId { get; set; }
    public string? OperationId { get; set; }
    public string? Status { get; set; }
    public string? Error { get; set; }
}
