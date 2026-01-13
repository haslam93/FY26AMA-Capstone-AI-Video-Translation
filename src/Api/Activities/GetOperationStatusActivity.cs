using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using VideoTranslation.Api.Models.SpeechApi;
using VideoTranslation.Api.Services;

namespace VideoTranslation.Api.Activities;

/// <summary>
/// Activity function to get the status of a translation.
/// Uses GetTranslation API to check if translation is ready for iteration creation.
/// </summary>
public class GetOperationStatusActivity
{
    private readonly ISpeechTranslationService _speechService;
    private readonly ILogger<GetOperationStatusActivity> _logger;

    public GetOperationStatusActivity(
        ISpeechTranslationService speechService,
        ILogger<GetOperationStatusActivity> logger)
    {
        _speechService = speechService;
        _logger = logger;
    }

    [Function(nameof(GetOperationStatusActivity))]
    public async Task<GetOperationStatusResult> RunAsync(
        [ActivityTrigger] GetOperationStatusInput input)
    {
        _logger.LogInformation("Getting translation status for {TranslationId}", input.TranslationId);

        try
        {
            // Use GetTranslation to check translation status directly
            var response = await _speechService.GetTranslationAsync(input.TranslationId);

            if (response == null)
            {
                return new GetOperationStatusResult
                {
                    Success = false,
                    Error = $"Translation {input.TranslationId} not found"
                };
            }

            var isTerminal = SpeechApiStatus.IsTerminal(response.Status);
            var isSuccess = SpeechApiStatus.IsSuccess(response.Status);

            _logger.LogInformation("Translation {TranslationId} status: {Status}, IsTerminal: {IsTerminal}, IsSuccess: {IsSuccess}", 
                input.TranslationId, response.Status, isTerminal, isSuccess);

            return new GetOperationStatusResult
            {
                Success = true,
                OperationId = response.Id,
                Status = response.Status,
                IsTerminal = isTerminal,
                IsSuccess = isSuccess
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get translation status for {TranslationId}", input.TranslationId);
            return new GetOperationStatusResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }
}

public class GetOperationStatusInput
{
    public string TranslationId { get; set; } = string.Empty;
    // Keep OperationId for backwards compatibility but we won't use it
    public string OperationId { get; set; } = string.Empty;
}

public class GetOperationStatusResult
{
    public bool Success { get; set; }
    public string? OperationId { get; set; }
    public string? Status { get; set; }
    public bool IsTerminal { get; set; }
    public bool IsSuccess { get; set; }
    public string? Error { get; set; }
}
