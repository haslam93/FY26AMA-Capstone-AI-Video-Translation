using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using VideoTranslation.Api.Models.SpeechApi;
using VideoTranslation.Api.Services;

namespace VideoTranslation.Api.Activities;

/// <summary>
/// Activity function to check the status of an iteration.
/// </summary>
public class GetIterationStatusActivity
{
    private readonly ISpeechTranslationService _speechService;
    private readonly ILogger<GetIterationStatusActivity> _logger;

    public GetIterationStatusActivity(
        ISpeechTranslationService speechService,
        ILogger<GetIterationStatusActivity> logger)
    {
        _speechService = speechService;
        _logger = logger;
    }

    [Function(nameof(GetIterationStatusActivity))]
    public async Task<GetIterationStatusResult> RunAsync(
        [ActivityTrigger] GetIterationStatusInput input)
    {
        _logger.LogDebug("Checking iteration status: {TranslationId}/{IterationId}", 
            input.TranslationId, input.IterationId);

        try
        {
            var iteration = await _speechService.GetIterationAsync(
                input.TranslationId,
                input.IterationId);

            if (iteration == null)
            {
                return new GetIterationStatusResult
                {
                    Success = false,
                    Error = "Iteration not found"
                };
            }

            var result = new GetIterationStatusResult
            {
                Success = true,
                Status = iteration.Status,
                IsTerminal = SpeechApiStatus.IsTerminal(iteration.Status),
                IsSuccess = SpeechApiStatus.IsSuccess(iteration.Status)
            };

            // If completed, include the result URLs
            if (result.IsSuccess && iteration.Result != null)
            {
                result.TranslatedVideoUrl = iteration.Result.TranslatedVideoFileUrl;
                result.SourceSubtitleUrl = iteration.Result.SourceLocaleSubtitleWebvttFileUrl;
                result.TargetSubtitleUrl = iteration.Result.TargetLocaleSubtitleWebvttFileUrl;
                result.MetadataUrl = iteration.Result.MetadataJsonWebvttFileUrl;
            }

            _logger.LogInformation("Iteration {IterationId} status: {Status}", 
                input.IterationId, iteration.Status);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get iteration status: {IterationId}", input.IterationId);
            return new GetIterationStatusResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }
}

public class GetIterationStatusInput
{
    public string TranslationId { get; set; } = string.Empty;
    public string IterationId { get; set; } = string.Empty;
}

public class GetIterationStatusResult
{
    public bool Success { get; set; }
    public string? Status { get; set; }
    public bool IsTerminal { get; set; }
    public bool IsSuccess { get; set; }
    public string? Error { get; set; }

    // Result URLs (populated when IsSuccess is true)
    public string? TranslatedVideoUrl { get; set; }
    public string? SourceSubtitleUrl { get; set; }
    public string? TargetSubtitleUrl { get; set; }
    public string? MetadataUrl { get; set; }
}
