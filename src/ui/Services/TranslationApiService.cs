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
    Task<PendingApprovalsResponse?> GetPendingApprovalsAsync();
    Task<ApproveRejectResponse?> ApproveJobAsync(string jobId, string? reviewedBy = null, string? comments = null);
    Task<ApproveRejectResponse?> RejectJobAsync(string jobId, string? reviewedBy = null, string? reason = null, string? comments = null);
    Task<UploadResponse?> UploadVideoAsync(Stream fileStream, string fileName, IProgress<int>? progress = null);
    Task<ValidationResponse?> ValidateSubtitlesAsync(string jobId);
    
    /// <summary>
    /// Send a chat message to a validation agent.
    /// </summary>
    /// <param name="jobId">The job ID.</param>
    /// <param name="message">The message to send.</param>
    /// <param name="agentType">Optional agent type: "orchestrator" (default), "translation", "technical", "cultural"</param>
    Task<ChatResponse?> SendChatMessageAsync(string jobId, string message, string? agentType = null);
    
    /// <summary>
    /// Get chat history for a job.
    /// </summary>
    /// <param name="jobId">The job ID.</param>
    /// <param name="agentType">Optional agent type for multi-agent systems.</param>
    Task<ChatHistoryResponse?> GetChatHistoryAsync(string jobId, string? agentType = null);
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

    public async Task<PendingApprovalsResponse?> GetPendingApprovalsAsync()
    {
        return await _httpClient.GetFromJsonAsync<PendingApprovalsResponse>("api/reviews/pending");
    }

    public async Task<ApproveRejectResponse?> ApproveJobAsync(string jobId, string? reviewedBy = null, string? comments = null)
    {
        var request = new { reviewedBy, comments };
        var response = await _httpClient.PostAsJsonAsync($"api/jobs/{jobId}/approve", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ApproveRejectResponse>();
    }

    public async Task<ApproveRejectResponse?> RejectJobAsync(string jobId, string? reviewedBy = null, string? reason = null, string? comments = null)
    {
        var request = new { reviewedBy, reason, comments };
        var response = await _httpClient.PostAsJsonAsync($"api/jobs/{jobId}/reject", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ApproveRejectResponse>();
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

    public async Task<ChatResponse?> SendChatMessageAsync(string jobId, string message, string? agentType = null)
    {
        var request = new ChatRequest { Message = message, AgentType = agentType };
        var response = await _httpClient.PostAsJsonAsync($"api/jobs/{jobId}/chat", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ChatResponse>();
    }

    public async Task<ChatHistoryResponse?> GetChatHistoryAsync(string jobId, string? agentType = null)
    {
        var url = $"api/jobs/{jobId}/chat/history";
        if (!string.IsNullOrEmpty(agentType))
        {
            url += $"?agentType={Uri.EscapeDataString(agentType)}";
        }
        return await _httpClient.GetFromJsonAsync<ChatHistoryResponse>(url);
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
