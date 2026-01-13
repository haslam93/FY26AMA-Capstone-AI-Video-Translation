using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using VideoTranslation.Api.Models;
using VideoTranslation.Api.Orchestration;
using VideoTranslation.Api.Services;

namespace VideoTranslation.Api.Functions;

/// <summary>
/// HTTP trigger functions for the Video Translation API.
/// </summary>
public class TranslationFunctions
{
    private readonly ILogger<TranslationFunctions> _logger;
    private readonly IBlobStorageService _blobService;
    private readonly JsonSerializerOptions _jsonOptions;

    public TranslationFunctions(
        ILogger<TranslationFunctions> logger,
        IBlobStorageService blobService)
    {
        _logger = logger;
        _blobService = blobService;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    /// <summary>
    /// POST /api/jobs - Create a new translation job.
    /// </summary>
    [Function("CreateJob")]
    public async Task<HttpResponseData> CreateJobAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "jobs")] HttpRequestData req,
        [DurableClient] DurableTaskClient durableClient)
    {
        _logger.LogInformation("CreateJob: Received request");

        try
        {
            // Parse request body
            var body = await req.ReadAsStringAsync();
            if (string.IsNullOrEmpty(body))
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Request body is required");
            }

            var request = JsonSerializer.Deserialize<TranslationJobRequest>(body, _jsonOptions);
            if (request == null)
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid request body");
            }

            // Validate required fields
            if (string.IsNullOrEmpty(request.SourceLocale))
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "sourceLocale is required");
            }
            if (string.IsNullOrEmpty(request.TargetLocale))
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "targetLocale is required");
            }
            if (string.IsNullOrEmpty(request.VideoUrl) && string.IsNullOrEmpty(request.BlobPath))
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Either videoUrl or blobPath is required");
            }

            // Generate unique IDs
            var jobId = Guid.NewGuid().ToString("N")[..12];
            var translationId = $"vt-{jobId}";

            // Create the job state
            var job = new TranslationJob
            {
                JobId = jobId,
                TranslationId = translationId,
                DisplayName = request.DisplayName ?? $"Translation-{jobId}",
                Request = request,
                Status = JobStatus.Submitted,
                StatusMessage = "Job submitted"
            };

            // Start the orchestration with the jobId as instance ID
            var instanceId = await durableClient.ScheduleNewOrchestrationInstanceAsync(
                nameof(VideoTranslationOrchestrator),
                job,
                new StartOrchestrationOptions { InstanceId = jobId });

            _logger.LogInformation("CreateJob: Started orchestration {InstanceId}", instanceId);

            // Create response
            var response = req.CreateResponse(HttpStatusCode.Accepted);
            response.Headers.Add("Content-Type", "application/json");

            var responseBody = new CreateJobResponse
            {
                JobId = jobId,
                Status = "Submitted",
                StatusUrl = $"{GetBaseUrl(req)}/api/jobs/{jobId}"
            };

            await response.WriteStringAsync(JsonSerializer.Serialize(responseBody, _jsonOptions));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateJob: Error processing request");
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    /// <summary>
    /// GET /api/jobs/{jobId} - Get job status.
    /// </summary>
    [Function("GetJobStatus")]
    public async Task<HttpResponseData> GetJobStatusAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "jobs/{jobId}")] HttpRequestData req,
        [DurableClient] DurableTaskClient durableClient,
        string jobId)
    {
        _logger.LogInformation("GetJobStatus: Getting status for job {JobId}", jobId);

        try
        {
            var instance = await durableClient.GetInstanceAsync(jobId, getInputsAndOutputs: true);

            if (instance == null)
            {
                return await CreateErrorResponse(req, HttpStatusCode.NotFound, $"Job {jobId} not found");
            }

            _logger.LogInformation("GetJobStatus: Instance {JobId} - RuntimeStatus={RuntimeStatus}, HasOutput={HasOutput}, HasInput={HasInput}", 
                jobId, instance.RuntimeStatus, instance.SerializedOutput != null, instance.SerializedInput != null);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");

            // Try to get the job state from the orchestration output
            if (instance.SerializedOutput != null)
            {
                _logger.LogDebug("GetJobStatus: SerializedOutput = {Output}", instance.SerializedOutput);
                try
                {
                    var job = JsonSerializer.Deserialize<TranslationJob>(instance.SerializedOutput, _jsonOptions);
                    if (job != null)
                    {
                        var statusResponse = JobStatusResponse.FromJob(job);
                        await response.WriteStringAsync(JsonSerializer.Serialize(statusResponse, _jsonOptions));
                        return response;
                    }
                    _logger.LogWarning("GetJobStatus: Deserialized output was null");
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "GetJobStatus: Failed to deserialize output: {Output}", instance.SerializedOutput);
                }
            }

            // Fallback to basic status from orchestration input
            if (instance.SerializedInput != null)
            {
                _logger.LogDebug("GetJobStatus: SerializedInput = {Input}", instance.SerializedInput);
                try
                {
                    var job = JsonSerializer.Deserialize<TranslationJob>(instance.SerializedInput, _jsonOptions);
                    if (job != null)
                    {
                        // Update status based on orchestration runtime status
                        job.Status = MapOrchestrationStatus(instance.RuntimeStatus);
                        var statusResponse = JobStatusResponse.FromJob(job);
                        await response.WriteStringAsync(JsonSerializer.Serialize(statusResponse, _jsonOptions));
                        return response;
                    }
                    _logger.LogWarning("GetJobStatus: Deserialized input was null");
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "GetJobStatus: Failed to deserialize input: {Input}", instance.SerializedInput);
                }
            }

            // Last resort - return basic orchestration info
            var basicResponse = new
            {
                jobId,
                status = instance.RuntimeStatus.ToString(),
                createdAt = instance.CreatedAt,
                lastUpdatedAt = instance.LastUpdatedAt
            };
            await response.WriteStringAsync(JsonSerializer.Serialize(basicResponse, _jsonOptions));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetJobStatus: Error getting status for job {JobId}", jobId);
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    /// <summary>
    /// GET /api/jobs/{jobId}/debug - Get raw orchestration data for debugging.
    /// </summary>
    [Function("GetJobDebug")]
    public async Task<HttpResponseData> GetJobDebugAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "jobs/{jobId}/debug")] HttpRequestData req,
        [DurableClient] DurableTaskClient durableClient,
        string jobId)
    {
        try
        {
            var instance = await durableClient.GetInstanceAsync(jobId, getInputsAndOutputs: true);

            if (instance == null)
            {
                return await CreateErrorResponse(req, HttpStatusCode.NotFound, $"Job {jobId} not found");
            }

            var debugInfo = new
            {
                instanceId = instance.InstanceId,
                runtimeStatus = instance.RuntimeStatus.ToString(),
                createdAt = instance.CreatedAt,
                lastUpdatedAt = instance.LastUpdatedAt,
                serializedInput = instance.SerializedInput,
                serializedOutput = instance.SerializedOutput
            };

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonSerializer.Serialize(debugInfo, _jsonOptions));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetJobDebug: Error getting debug info for job {JobId}", jobId);
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    /// <summary>
    /// POST /api/jobs/{jobId}/iterate - Start a new iteration with edited WebVTT.
    /// </summary>
    [Function("IterateJob")]
    public async Task<HttpResponseData> IterateJobAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "jobs/{jobId}/iterate")] HttpRequestData req,
        [DurableClient] DurableTaskClient durableClient,
        string jobId)
    {
        _logger.LogInformation("IterateJob: Processing iteration request for job {JobId}", jobId);

        try
        {
            // Parse request body
            var body = await req.ReadAsStringAsync();
            if (string.IsNullOrEmpty(body))
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Request body is required");
            }

            var iterateRequest = JsonSerializer.Deserialize<IterateRequest>(body, _jsonOptions);
            if (iterateRequest == null)
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid request body");
            }

            if (string.IsNullOrEmpty(iterateRequest.WebvttUrl) && string.IsNullOrEmpty(iterateRequest.WebvttBlobPath))
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Either webvttUrl or webvttBlobPath is required");
            }

            // Get the current job state
            var instance = await durableClient.GetInstanceAsync(jobId);
            if (instance == null)
            {
                return await CreateErrorResponse(req, HttpStatusCode.NotFound, $"Job {jobId} not found");
            }

            if (instance.RuntimeStatus != OrchestrationRuntimeStatus.Completed)
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, 
                    $"Cannot iterate on job with status: {instance.RuntimeStatus}. Job must be completed first.");
            }

            // Get the completed job
            TranslationJob? existingJob = null;
            if (instance.SerializedOutput != null)
            {
                existingJob = JsonSerializer.Deserialize<TranslationJob>(instance.SerializedOutput, _jsonOptions);
            }

            if (existingJob == null)
            {
                return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Could not retrieve job state");
            }

            if (existingJob.Status != JobStatus.Completed)
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, 
                    $"Cannot iterate on job with status: {existingJob.Status}. Job must be completed first.");
            }

            // Create a new job for the iteration (same translation, new iteration)
            var newIterationNumber = existingJob.IterationNumber + 1;
            var newJobId = $"{jobId}-iter{newIterationNumber}";

            var newJob = new TranslationJob
            {
                JobId = newJobId,
                TranslationId = existingJob.TranslationId,
                IterationId = $"iteration-{newIterationNumber}",
                IterationNumber = newIterationNumber - 1, // Will be incremented in orchestrator
                DisplayName = $"{existingJob.DisplayName} - Iteration {newIterationNumber}",
                Request = existingJob.Request,
                VideoFileUrl = existingJob.VideoFileUrl,
                Status = JobStatus.Submitted,
                StatusMessage = "Iteration job submitted"
            };

            // Store iteration request info for the orchestrator
            // We'll need to create a specialized orchestrator for iterations
            // For now, we'll use a simplified approach

            // TODO: Implement iteration-specific orchestrator
            // For now, return not implemented
            return await CreateErrorResponse(req, HttpStatusCode.NotImplemented, 
                "Iteration feature is in development. Please create a new job with the modified video.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "IterateJob: Error processing iteration for job {JobId}", jobId);
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    /// <summary>
    /// POST /api/upload - Upload a video file to blob storage.
    /// </summary>
    [Function("UploadVideo")]
    public async Task<HttpResponseData> UploadVideoAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "upload")] HttpRequestData req)
    {
        _logger.LogInformation("UploadVideo: Received upload request");

        try
        {
            // Get filename from query string or header
            var fileName = req.Query["filename"];
            if (string.IsNullOrEmpty(fileName))
            {
                fileName = req.Headers.TryGetValues("X-Filename", out var values) 
                    ? values.FirstOrDefault() 
                    : null;
            }

            if (string.IsNullOrEmpty(fileName))
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "filename query parameter or X-Filename header is required");
            }

            // Validate file extension
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            var allowedExtensions = new[] { ".mp4", ".webm", ".mov", ".avi", ".mkv", ".wmv" };
            if (!allowedExtensions.Contains(extension))
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, 
                    $"Invalid file type. Allowed: {string.Join(", ", allowedExtensions)}");
            }

            // Generate unique blob path
            var uploadId = Guid.NewGuid().ToString("N")[..12];
            var blobPath = $"{uploadId}/{fileName}";

            // Get content type
            var contentType = extension switch
            {
                ".mp4" => "video/mp4",
                ".webm" => "video/webm",
                ".mov" => "video/quicktime",
                ".avi" => "video/x-msvideo",
                ".mkv" => "video/x-matroska",
                ".wmv" => "video/x-ms-wmv",
                _ => "application/octet-stream"
            };

            // Upload to blob storage
            using var bodyStream = req.Body;
            var blobUrl = await _blobService.UploadAsync(
                bodyStream,
                "videos",
                blobPath,
                contentType);

            _logger.LogInformation("UploadVideo: Uploaded {FileName} to {BlobPath}", fileName, blobPath);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonSerializer.Serialize(new
            {
                uploadId,
                blobPath,  // Just the path within the container, not including container name
                blobUrl,
                fileName,
                contentType
            }, _jsonOptions));

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UploadVideo: Error uploading video");
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    /// <summary>
    /// GET /api/jobs - List all jobs.
    /// </summary>
    [Function("ListJobs")]
    public async Task<HttpResponseData> ListJobsAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "jobs")] HttpRequestData req,
        [DurableClient] DurableTaskClient durableClient)
    {
        _logger.LogInformation("ListJobs: Listing all jobs");

        try
        {
            var instances = durableClient.GetAllInstancesAsync(new OrchestrationQuery
            {
                PageSize = 100,
                FetchInputsAndOutputs = true  // Need outputs to get actual job status
            });

            var jobs = new List<object>();
            await foreach (var instance in instances)
            {
                // Try to get the actual job status from the output
                var jobStatus = instance.RuntimeStatus.ToString();
                
                if (instance.RuntimeStatus == OrchestrationRuntimeStatus.Completed && 
                    instance.SerializedOutput != null)
                {
                    try
                    {
                        var job = JsonSerializer.Deserialize<TranslationJob>(instance.SerializedOutput, _jsonOptions);
                        if (job != null)
                        {
                            // Use the actual job status, not the orchestration status
                            jobStatus = job.Status.ToString();
                        }
                    }
                    catch (JsonException)
                    {
                        // If we can't deserialize, fall back to orchestration status
                    }
                }
                
                jobs.Add(new
                {
                    jobId = instance.InstanceId,
                    status = jobStatus,
                    createdAt = instance.CreatedAt,
                    lastUpdatedAt = instance.LastUpdatedAt
                });
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonSerializer.Serialize(new { jobs }, _jsonOptions));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ListJobs: Error listing jobs");
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    private static JobStatus MapOrchestrationStatus(OrchestrationRuntimeStatus status)
    {
        return status switch
        {
            OrchestrationRuntimeStatus.Pending => JobStatus.Submitted,
            OrchestrationRuntimeStatus.Running => JobStatus.Processing,
            OrchestrationRuntimeStatus.Completed => JobStatus.Completed,
            OrchestrationRuntimeStatus.Failed => JobStatus.Failed,
            OrchestrationRuntimeStatus.Terminated => JobStatus.Failed,
            OrchestrationRuntimeStatus.Suspended => JobStatus.Processing,
            _ => JobStatus.Processing
        };
    }

    private static string GetBaseUrl(HttpRequestData req)
    {
        return $"{req.Url.Scheme}://{req.Url.Host}{(req.Url.Port != 80 && req.Url.Port != 443 ? $":{req.Url.Port}" : "")}";
    }

    private static async Task<HttpResponseData> CreateErrorResponse(
        HttpRequestData req, 
        HttpStatusCode statusCode, 
        string message)
    {
        var response = req.CreateResponse(statusCode);
        response.Headers.Add("Content-Type", "application/json");
        await response.WriteStringAsync(JsonSerializer.Serialize(new { error = message }));
        return response;
    }
}
