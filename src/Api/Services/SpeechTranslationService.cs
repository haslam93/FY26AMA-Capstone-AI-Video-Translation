using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VideoTranslation.Api.Models.SpeechApi;

namespace VideoTranslation.Api.Services;

/// <summary>
/// Configuration options for the Speech Translation service.
/// </summary>
public class SpeechTranslationOptions
{
    public const string SectionName = "SpeechTranslation";

    /// <summary>
    /// Speech service endpoint (e.g., "https://speech-ama-3.cognitiveservices.azure.com").
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// Speech service region (e.g., "eastus2").
    /// </summary>
    public string Region { get; set; } = "eastus2";

    /// <summary>
    /// Speech service subscription key. Optional - if not provided, uses Azure AD auth.
    /// </summary>
    public string? SubscriptionKey { get; set; }

    /// <summary>
    /// API version for the Video Translation API.
    /// </summary>
    public string ApiVersion { get; set; } = "2025-05-20";
}

/// <summary>
/// Implementation of the Speech Video Translation API client.
/// Supports both API key and Azure AD (Managed Identity) authentication.
/// </summary>
public class SpeechTranslationService : ISpeechTranslationService
{
    private readonly HttpClient _httpClient;
    private readonly SpeechTranslationOptions _options;
    private readonly ILogger<SpeechTranslationService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly TokenCredential? _credential;
    private readonly bool _useAadAuth;

    private const string CognitiveServicesScope = "https://cognitiveservices.azure.com/.default";

    public SpeechTranslationService(
        HttpClient httpClient,
        IOptions<SpeechTranslationOptions> options,
        ILogger<SpeechTranslationService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        // Determine authentication method
        _useAadAuth = string.IsNullOrEmpty(_options.SubscriptionKey);
        
        if (_useAadAuth)
        {
            _credential = new DefaultAzureCredential();
            _logger.LogInformation("Using Azure AD authentication for Speech service");
        }
        else
        {
            _logger.LogInformation("Using API key authentication for Speech service");
        }

        // Configure base address - prefer custom endpoint, fall back to regional
        var baseUri = !string.IsNullOrEmpty(_options.Endpoint)
            ? new Uri($"{_options.Endpoint.TrimEnd('/')}/videotranslation/")
            : new Uri($"https://{_options.Region}.api.cognitive.microsoft.com/videotranslation/");
        
        _httpClient.BaseAddress = baseUri;

        // Only add API key header if using key-based auth
        if (!_useAadAuth)
        {
            _httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _options.SubscriptionKey);
        }
    }

    private async Task<HttpRequestMessage> CreateAuthenticatedRequestAsync(
        HttpMethod method, 
        string url,
        CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(method, url);

        if (_useAadAuth && _credential != null)
        {
            var tokenResult = await _credential.GetTokenAsync(
                new TokenRequestContext(new[] { CognitiveServicesScope }),
                cancellationToken);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult.Token);
        }

        return request;
    }

    public async Task<TranslationResponse> CreateTranslationAsync(
        string translationId,
        string operationId,
        CreateTranslationRequest request,
        CancellationToken cancellationToken = default)
    {
        var url = $"translations/{translationId}?api-version={_options.ApiVersion}";

        _logger.LogInformation("Creating translation {TranslationId}", translationId);
        
        // Log the request body for debugging
        var requestJson = JsonSerializer.Serialize(request, _jsonOptions);
        _logger.LogDebug("CreateTranslation request body: {RequestBody}", requestJson);

        using var httpRequest = await CreateAuthenticatedRequestAsync(HttpMethod.Put, url, cancellationToken);
        httpRequest.Headers.Add("Operation-Id", operationId);
        httpRequest.Content = JsonContent.Create(request, options: _jsonOptions);

        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("CreateTranslation failed with status {StatusCode}: {ErrorContent}", 
                response.StatusCode, errorContent);
            response.EnsureSuccessStatusCode(); // This will throw with the status code
        }

        var result = await response.Content.ReadFromJsonAsync<TranslationResponse>(_jsonOptions, cancellationToken);
        return result ?? throw new InvalidOperationException("Failed to deserialize translation response");
    }

    public async Task<TranslationResponse?> GetTranslationAsync(
        string translationId,
        CancellationToken cancellationToken = default)
    {
        var url = $"translations/{translationId}?api-version={_options.ApiVersion}";

        _logger.LogDebug("Getting translation {TranslationId}", translationId);

        using var httpRequest = await CreateAuthenticatedRequestAsync(HttpMethod.Get, url, cancellationToken);
        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TranslationResponse>(_jsonOptions, cancellationToken);
    }

    public async Task<IterationResponse> CreateIterationAsync(
        string translationId,
        string iterationId,
        string operationId,
        CreateIterationRequest request,
        CancellationToken cancellationToken = default)
    {
        var url = $"translations/{translationId}/iterations/{iterationId}?api-version={_options.ApiVersion}";

        _logger.LogInformation("Creating iteration {IterationId} for translation {TranslationId}", iterationId, translationId);
        
        // Log the request body for debugging
        var requestJson = JsonSerializer.Serialize(request, _jsonOptions);
        _logger.LogDebug("CreateIteration request body: {RequestBody}", requestJson);

        using var httpRequest = await CreateAuthenticatedRequestAsync(HttpMethod.Put, url, cancellationToken);
        httpRequest.Headers.Add("Operation-Id", operationId);
        httpRequest.Content = JsonContent.Create(request, options: _jsonOptions);

        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("CreateIteration failed with status {StatusCode}: {ErrorContent}", 
                response.StatusCode, errorContent);
            response.EnsureSuccessStatusCode();
        }

        var result = await response.Content.ReadFromJsonAsync<IterationResponse>(_jsonOptions, cancellationToken);
        return result ?? throw new InvalidOperationException("Failed to deserialize iteration response");
    }

    public async Task<IterationResponse?> GetIterationAsync(
        string translationId,
        string iterationId,
        CancellationToken cancellationToken = default)
    {
        var url = $"translations/{translationId}/iterations/{iterationId}?api-version={_options.ApiVersion}";

        _logger.LogDebug("Getting iteration {IterationId} for translation {TranslationId}", iterationId, translationId);

        using var httpRequest = await CreateAuthenticatedRequestAsync(HttpMethod.Get, url, cancellationToken);
        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IterationResponse>(_jsonOptions, cancellationToken);
    }

    public async Task<OperationResponse?> GetOperationAsync(
        string operationId,
        CancellationToken cancellationToken = default)
    {
        var url = $"operations/{operationId}?api-version={_options.ApiVersion}";

        _logger.LogInformation("Getting operation {OperationId} at URL: {Url}", operationId, url);

        using var httpRequest = await CreateAuthenticatedRequestAsync(HttpMethod.Get, url, cancellationToken);
        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Operation {OperationId} not found", operationId);
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("GetOperation failed with status {StatusCode}: {ErrorContent}", 
                response.StatusCode, errorContent);
            response.EnsureSuccessStatusCode();
        }

        return await response.Content.ReadFromJsonAsync<OperationResponse>(_jsonOptions, cancellationToken);
    }

    public async Task DeleteTranslationAsync(
        string translationId,
        CancellationToken cancellationToken = default)
    {
        var url = $"translations/{translationId}?api-version={_options.ApiVersion}";

        _logger.LogInformation("Deleting translation {TranslationId}", translationId);

        using var httpRequest = await CreateAuthenticatedRequestAsync(HttpMethod.Delete, url, cancellationToken);
        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        
        // 204 No Content is expected for successful delete
        if (response.StatusCode != System.Net.HttpStatusCode.NoContent && 
            response.StatusCode != System.Net.HttpStatusCode.NotFound)
        {
            response.EnsureSuccessStatusCode();
        }
    }
}
