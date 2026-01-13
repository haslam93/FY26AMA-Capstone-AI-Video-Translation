using System.Net.Http.Json;
using System.Net.Http.Headers;
using VideoTranslation.UI.Models;

namespace VideoTranslation.UI.Services;

/// <summary>
/// Service for communicating with the Video Translation API.
/// </summary>
public interface ITranslationApiService
{
    Task<CreateJobResponse?> CreateJobAsync(CreateJobRequest request);
    Task<JobStatusResponse?> GetJobStatusAsync(string jobId);
    Task<JobListResponse?> ListJobsAsync();
    Task<UploadResponse?> UploadVideoAsync(Stream fileStream, string fileName, IProgress<int>? progress = null);
    Task<ValidationResponse?> ValidateSubtitlesAsync(string jobId);
}

/// <summary>
/// Implementation of the Translation API service.
/// </summary>
public class TranslationApiService : ITranslationApiService
{
    private readonly HttpClient _httpClient;

    public TranslationApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<CreateJobResponse?> CreateJobAsync(CreateJobRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/jobs", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CreateJobResponse>();
    }

    public async Task<JobStatusResponse?> GetJobStatusAsync(string jobId)
    {
        return await _httpClient.GetFromJsonAsync<JobStatusResponse>($"api/jobs/{jobId}");
    }

    public async Task<JobListResponse?> ListJobsAsync()
    {
        return await _httpClient.GetFromJsonAsync<JobListResponse>("api/jobs");
    }

    public async Task<UploadResponse?> UploadVideoAsync(Stream fileStream, string fileName, IProgress<int>? progress = null)
    {
        using var content = new StreamContent(fileStream);
        content.Headers.ContentType = new MediaTypeHeaderValue(GetContentType(fileName));
        
        var response = await _httpClient.PostAsync($"api/upload?filename={Uri.EscapeDataString(fileName)}", content);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<UploadResponse>();
    }

    public async Task<ValidationResponse?> ValidateSubtitlesAsync(string jobId)
    {
        var response = await _httpClient.PostAsync($"api/jobs/{jobId}/validate", null);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ValidationResponse>();
    }

    private static string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".mp4" => "video/mp4",
            ".webm" => "video/webm",
            ".mov" => "video/quicktime",
            ".avi" => "video/x-msvideo",
            ".mkv" => "video/x-matroska",
            ".wmv" => "video/x-ms-wmv",
            _ => "application/octet-stream"
        };
    }
}
