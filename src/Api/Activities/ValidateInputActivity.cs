using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;
using VideoTranslation.Api.Models;
using VideoTranslation.Api.Services;

namespace VideoTranslation.Api.Activities;

/// <summary>
/// Activity function to validate input and resolve video URL.
/// </summary>
public class ValidateInputActivity
{
    private readonly IBlobStorageService _blobService;
    private readonly ILogger<ValidateInputActivity> _logger;

    // Supported source and target locales based on Azure Speech Video Translation API documentation
    private static readonly HashSet<string> SupportedLocales = new(StringComparer.OrdinalIgnoreCase)
    {
        // English
        "en-US", "en-GB", "en-AU", "en-CA", "en-IN", "en-IE", "en-NZ", "en-SG", "en-ZA", 
        "en-HK", "en-KE", "en-NG", "en-PH", "en-TZ",
        // Arabic
        "ar-SA", "ar-EG", "ar-AE", "ar-BH", "ar-DZ", "ar-IQ", "ar-JO", "ar-KW", "ar-LB", 
        "ar-LY", "ar-MA", "ar-OM", "ar-QA", "ar-SY", "ar-TN", "ar-YE",
        // Chinese
        "zh-CN", "zh-TW", "zh-HK", "yue-CN",
        // European Languages
        "de-DE", "de-AT", "de-CH", "fr-FR", "fr-CA", "fr-BE", "fr-CH",
        "es-ES", "es-MX", "es-AR", "es-BO", "es-CL", "es-CO", "es-CR", "es-CU", "es-DO",
        "es-EC", "es-GQ", "es-GT", "es-HN", "es-NI", "es-PA", "es-PE", "es-PR", "es-PY",
        "es-SV", "es-US", "es-UY", "es-VE",
        "it-IT", "pt-BR", "pt-PT", "nl-NL", "nl-BE", "pl-PL", "ru-RU", "uk-UA",
        "cs-CZ", "da-DK", "fi-FI", "el-GR", "hu-HU", "nb-NO", "ro-RO", "sk-SK", "sl-SI",
        "sv-SE", "bg-BG", "hr-HR", "et-EE", "lv-LV", "lt-LT", "sr-RS",
        "ca-ES", "eu-ES", "gl-ES", "cy-GB", "ga-IE", "is-IS", "mt-MT", "sq-AL", "bs-BA", "mk-MK",
        // Asian Languages
        "ja-JP", "ko-KR", "hi-IN", "th-TH", "vi-VN", "id-ID", "ms-MY", "fil-PH",
        "ta-IN", "te-IN", "bn-IN", "gu-IN", "kn-IN", "ml-IN", "mr-IN",
        "jv-ID", "km-KH", "lo-LA", "my-MM", "ne-NP", "si-LK", "mn-MN",
        "kk-KZ", "uz-UZ", "az-AZ", "hy-AM", "ka-GE",
        // Middle Eastern & African
        "he-IL", "tr-TR", "fa-IR", "ps-AF", "af-ZA", "am-ET", "sw-KE", "sw-TZ", "so-SO", "su-ID", "zu-ZA"
    };

    public ValidateInputActivity(
        IBlobStorageService blobService,
        ILogger<ValidateInputActivity> logger)
    {
        _blobService = blobService;
        _logger = logger;
    }

    [Function(nameof(ValidateInputActivity))]
    public async Task<ValidateInputResult> RunAsync(
        [ActivityTrigger] TranslationJob job)
    {
        _logger.LogInformation("Validating input for job {JobId}", job.JobId);

        var result = new ValidateInputResult { IsValid = true };

        // Validate locales
        if (!SupportedLocales.Contains(job.Request.SourceLocale))
        {
            result.IsValid = false;
            result.Error = $"Unsupported source locale: {job.Request.SourceLocale}";
            return result;
        }

        if (!SupportedLocales.Contains(job.Request.TargetLocale))
        {
            result.IsValid = false;
            result.Error = $"Unsupported target locale: {job.Request.TargetLocale}";
            return result;
        }

        if (job.Request.SourceLocale == job.Request.TargetLocale)
        {
            result.IsValid = false;
            result.Error = "Source and target locales must be different";
            return result;
        }

        // Validate voice kind
        if (job.Request.VoiceKind != "PlatformVoice" && job.Request.VoiceKind != "PersonalVoice")
        {
            result.IsValid = false;
            result.Error = $"Invalid voice kind: {job.Request.VoiceKind}. Must be 'PlatformVoice' or 'PersonalVoice'";
            return result;
        }

        // Resolve video URL
        // The Speech Video Translation API requires an Azure Blob Storage SAS URL
        // If a direct URL is provided, we need to copy it to our blob storage first
        if (!string.IsNullOrEmpty(job.Request.VideoUrl))
        {
            try
            {
                // Check if it's already an Azure Blob Storage URL
                var videoUri = new Uri(job.Request.VideoUrl);
                var isBlobUrl = videoUri.Host.EndsWith(".blob.core.windows.net", StringComparison.OrdinalIgnoreCase);
                
                if (isBlobUrl && job.Request.VideoUrl.Contains("sig=", StringComparison.OrdinalIgnoreCase))
                {
                    // Already a SAS URL, use as-is
                    result.VideoFileUrl = job.Request.VideoUrl;
                    _logger.LogInformation("Using existing blob SAS URL");
                }
                else
                {
                    // External URL or blob without SAS - copy to our storage
                    var blobPath = $"{job.JobId}/{Path.GetFileName(videoUri.LocalPath)}";
                    
                    _logger.LogInformation("Copying external video to blob storage: {BlobPath}", blobPath);
                    await _blobService.CopyFromUrlAsync(job.Request.VideoUrl, "videos", blobPath);
                    
                    // Generate SAS URL valid for 4 hours (enough for Speech API to process)
                    result.VideoFileUrl = await _blobService.GenerateSasUrlAsync(
                        "videos",
                        blobPath,
                        TimeSpan.FromHours(4));
                    
                    _logger.LogInformation("Generated SAS URL for copied video");
                }
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Error = $"Failed to process video URL: {ex.Message}";
                return result;
            }
        }
        else if (!string.IsNullOrEmpty(job.Request.BlobPath))
        {
            // Blob path provided - generate SAS URL
            var exists = await _blobService.ExistsAsync("videos", job.Request.BlobPath);
            if (!exists)
            {
                result.IsValid = false;
                result.Error = $"Video file not found in storage: {job.Request.BlobPath}";
                return result;
            }

            // Generate SAS URL valid for 2 hours (enough for Speech API to download)
            result.VideoFileUrl = await _blobService.GenerateSasUrlAsync(
                "videos",
                job.Request.BlobPath,
                TimeSpan.FromHours(2));
            
            _logger.LogInformation("Generated SAS URL for blob: {BlobPath}", job.Request.BlobPath);
        }
        else
        {
            result.IsValid = false;
            result.Error = "Either videoUrl or blobPath must be provided";
            return result;
        }

        _logger.LogInformation("Input validation successful for job {JobId}", job.JobId);
        return result;
    }
}

public class ValidateInputResult
{
    public bool IsValid { get; set; }
    public string? Error { get; set; }
    public string? VideoFileUrl { get; set; }
}
