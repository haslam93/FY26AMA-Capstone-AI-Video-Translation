using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;
using VideoTranslation.Api.Activities;
using VideoTranslation.Api.Models;

namespace VideoTranslation.Api.Orchestration;

/// <summary>
/// Durable Functions orchestrator for video translation workflow.
/// 
/// Workflow:
/// 1. Validate input and resolve video URL
/// 2. Create translation via Speech API
/// 3. Create iteration to start translation
/// 4. Poll for iteration completion (30-second intervals, 60-minute timeout)
/// 5. Copy outputs to blob storage
/// 6. Run AI subtitle validation
/// 7. Wait for human approval (3-day timeout)
/// 8. Complete (Approved) or Reject
/// </summary>
public static class VideoTranslationOrchestrator
{
    // Polling configuration
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan MaxDuration = TimeSpan.FromMinutes(60);
    private static readonly TimeSpan ApprovalTimeout = TimeSpan.FromDays(3);
    private static readonly int MaxRetries = 3;
    private static readonly TimeSpan InitialRetryDelay = TimeSpan.FromSeconds(5);

    [Function(nameof(VideoTranslationOrchestrator))]
    public static async Task<TranslationJob> RunAsync(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var logger = context.CreateReplaySafeLogger(nameof(VideoTranslationOrchestrator));
        var job = context.GetInput<TranslationJob>()!;

        try
        {
            // Update status
            job.Status = JobStatus.Validating;
            job.StatusMessage = "Validating input...";
            job.LastUpdatedAt = context.CurrentUtcDateTime;

            // Step 1: Validate input
            logger.LogInformation("Job {JobId}: Validating input", job.JobId);
            var validationResult = await context.CallActivityAsync<ValidateInputResult>(
                nameof(ValidateInputActivity),
                job,
                CreateRetryOptions());

            if (!validationResult.IsValid)
            {
                job.Status = JobStatus.Failed;
                job.Error = validationResult.Error;
                job.StatusMessage = "Validation failed";
                job.LastUpdatedAt = context.CurrentUtcDateTime;
                return job;
            }

            job.VideoFileUrl = validationResult.VideoFileUrl;
            job.Status = JobStatus.Validated;
            job.StatusMessage = "Input validated";
            job.LastUpdatedAt = context.CurrentUtcDateTime;

            // Step 2: Create translation
            job.Status = JobStatus.CreatingTranslation;
            job.StatusMessage = "Creating translation...";
            job.LastUpdatedAt = context.CurrentUtcDateTime;

            logger.LogInformation("Job {JobId}: Creating translation", job.JobId);
            var translationResult = await context.CallActivityAsync<CreateTranslationResult>(
                nameof(CreateTranslationActivity),
                new CreateTranslationInput
                {
                    Job = job,
                    TranslationId = job.TranslationId,
                    VideoFileUrl = job.VideoFileUrl!
                },
                CreateRetryOptions());

            if (!translationResult.Success)
            {
                logger.LogError("Job {JobId}: Translation creation failed: {Error}", job.JobId, translationResult.Error);
                job.Status = JobStatus.Failed;
                job.Error = translationResult.Error;
                job.StatusMessage = "Failed to create translation";
                job.LastUpdatedAt = context.CurrentUtcDateTime;
                return job;
            }

            logger.LogInformation("Job {JobId}: Translation created successfully with operation {OperationId}", 
                job.JobId, translationResult.OperationId);
            job.Status = JobStatus.TranslationCreated;
            job.StatusMessage = "Translation created, waiting for completion...";
            job.LastUpdatedAt = context.CurrentUtcDateTime;

            // Step 2b: Wait for translation operation to complete
            // The Speech API requires the translation to be in "Succeeded" state before creating an iteration
            logger.LogInformation("Job {JobId}: Polling for translation operation completion", job.JobId);
            var translationStartTime = context.CurrentUtcDateTime;
            
            while (true)
            {
                var operationStatus = await context.CallActivityAsync<GetOperationStatusResult>(
                    nameof(GetOperationStatusActivity),
                    new GetOperationStatusInput
                    {
                        TranslationId = job.TranslationId
                    },
                    CreateRetryOptions());

                if (!operationStatus.Success)
                {
                    job.Status = JobStatus.Failed;
                    job.Error = operationStatus.Error;
                    job.StatusMessage = "Failed to get translation status";
                    job.LastUpdatedAt = context.CurrentUtcDateTime;
                    return job;
                }

                job.StatusMessage = $"Translation status: {operationStatus.Status}";
                job.LastUpdatedAt = context.CurrentUtcDateTime;

                if (operationStatus.IsTerminal)
                {
                    if (!operationStatus.IsSuccess)
                    {
                        job.Status = JobStatus.Failed;
                        job.Error = $"Translation failed with status: {operationStatus.Status}";
                        job.StatusMessage = "Translation failed";
                        job.LastUpdatedAt = context.CurrentUtcDateTime;
                        return job;
                    }
                    logger.LogInformation("Job {JobId}: Translation completed successfully", job.JobId);
                    break;
                }

                // Check timeout for translation (15 minutes for longer videos)
                var translationElapsed = context.CurrentUtcDateTime - translationStartTime;
                if (translationElapsed > TimeSpan.FromMinutes(15))
                {
                    job.Status = JobStatus.Failed;
                    job.Error = "Translation operation timed out after 15 minutes";
                    job.StatusMessage = "Translation operation timeout";
                    job.LastUpdatedAt = context.CurrentUtcDateTime;
                    return job;
                }

                // Wait before polling again
                await context.CreateTimer(context.CurrentUtcDateTime.Add(TimeSpan.FromSeconds(5)), CancellationToken.None);
            }

            // Step 3: Create iteration
            job.Status = JobStatus.CreatingIteration;
            job.StatusMessage = "Creating iteration...";
            job.IterationNumber = 1;
            job.IterationId = $"iteration-{job.IterationNumber}";
            job.LastUpdatedAt = context.CurrentUtcDateTime;

            logger.LogInformation("Job {JobId}: Creating iteration {IterationNumber} with id {IterationId}", job.JobId, job.IterationNumber, job.IterationId);
            var iterationResult = await context.CallActivityAsync<CreateIterationResult>(
                nameof(CreateIterationActivity),
                new CreateIterationInput
                {
                    TranslationId = job.TranslationId,
                    IterationId = job.IterationId,
                    SpeakerCount = job.Request.SpeakerCount,
                    SubtitleMaxCharCountPerSegment = job.Request.SubtitleMaxCharCountPerSegment,
                    ExportSubtitleInVideo = job.Request.ExportSubtitleInVideo
                },
                CreateRetryOptions());

            if (!iterationResult.Success)
            {
                job.Status = JobStatus.Failed;
                job.Error = iterationResult.Error;
                job.StatusMessage = "Failed to create iteration";
                job.LastUpdatedAt = context.CurrentUtcDateTime;
                return job;
            }

            job.Status = JobStatus.Processing;
            job.StatusMessage = "Translation in progress...";
            job.LastUpdatedAt = context.CurrentUtcDateTime;

            // Step 4: Poll for completion
            logger.LogInformation("Job {JobId}: Polling for iteration completion", job.JobId);
            var startTime = context.CurrentUtcDateTime;
            GetIterationStatusResult statusResult;

            while (true)
            {
                statusResult = await context.CallActivityAsync<GetIterationStatusResult>(
                    nameof(GetIterationStatusActivity),
                    new GetIterationStatusInput
                    {
                        TranslationId = job.TranslationId,
                        IterationId = job.IterationId!
                    },
                    CreateRetryOptions());

                if (!statusResult.Success)
                {
                    job.Status = JobStatus.Failed;
                    job.Error = statusResult.Error;
                    job.StatusMessage = "Failed to get iteration status";
                    job.LastUpdatedAt = context.CurrentUtcDateTime;
                    return job;
                }

                job.StatusMessage = $"Processing: {statusResult.Status}";
                job.LastUpdatedAt = context.CurrentUtcDateTime;

                if (statusResult.IsTerminal)
                {
                    break;
                }

                // Check timeout
                var elapsed = context.CurrentUtcDateTime - startTime;
                if (elapsed > MaxDuration)
                {
                    job.Status = JobStatus.Failed;
                    job.Error = $"Translation timed out after {MaxDuration.TotalMinutes} minutes";
                    job.StatusMessage = "Timeout";
                    job.LastUpdatedAt = context.CurrentUtcDateTime;
                    return job;
                }

                // Wait before next poll
                logger.LogDebug("Job {JobId}: Waiting {Interval} before next poll", job.JobId, PollingInterval);
                await context.CreateTimer(context.CurrentUtcDateTime.Add(PollingInterval), CancellationToken.None);
            }

            // Check if iteration succeeded
            if (!statusResult.IsSuccess)
            {
                job.Status = JobStatus.Failed;
                job.Error = $"Translation failed with status: {statusResult.Status}";
                job.StatusMessage = "Translation failed";
                job.LastUpdatedAt = context.CurrentUtcDateTime;
                return job;
            }

            // Store original result URLs
            job.Result = new TranslationResult
            {
                TranslatedVideoUrl = statusResult.TranslatedVideoUrl,
                SourceSubtitleUrl = statusResult.SourceSubtitleUrl,
                TargetSubtitleUrl = statusResult.TargetSubtitleUrl,
                MetadataUrl = statusResult.MetadataUrl
            };

            // Step 5: Copy outputs to our storage
            job.Status = JobStatus.CopyingOutputs;
            job.StatusMessage = "Copying outputs to storage...";
            job.LastUpdatedAt = context.CurrentUtcDateTime;

            logger.LogInformation("Job {JobId}: Copying outputs to storage", job.JobId);
            var copyResult = await context.CallActivityAsync<CopyOutputsResult>(
                nameof(CopyOutputsActivity),
                new CopyOutputsInput
                {
                    JobId = job.JobId,
                    IterationNumber = job.IterationNumber,
                    TranslatedVideoUrl = statusResult.TranslatedVideoUrl,
                    SourceSubtitleUrl = statusResult.SourceSubtitleUrl,
                    TargetSubtitleUrl = statusResult.TargetSubtitleUrl,
                    MetadataUrl = statusResult.MetadataUrl
                },
                CreateRetryOptions());

            if (!copyResult.Success)
            {
                // Log warning but don't fail - we still have the original URLs
                logger.LogWarning("Job {JobId}: Failed to copy outputs: {Error}", job.JobId, copyResult.Error);
            }
            else
            {
                job.Result.StoredOutputs = copyResult.StoredOutputs;
            }

            // Step 6: Run AI subtitle validation
            job.Status = JobStatus.RunningValidation;
            job.StatusMessage = "Running AI subtitle validation...";
            job.LastUpdatedAt = context.CurrentUtcDateTime;

            logger.LogInformation("Job {JobId}: Running AI validation", job.JobId);

            // Get subtitle URLs (prefer stored, fallback to original)
            var sourceSubtitleUrl = job.Result.StoredOutputs?.SourceSubtitleUrl ?? job.Result.SourceSubtitleUrl;
            var targetSubtitleUrl = job.Result.StoredOutputs?.TargetSubtitleUrl ?? job.Result.TargetSubtitleUrl;

            var subtitleValidation = await context.CallActivityAsync<RunValidationResult>(
                nameof(RunValidationActivity),
                new RunValidationInput
                {
                    JobId = job.JobId,
                    SourceSubtitleUrl = sourceSubtitleUrl,
                    TargetSubtitleUrl = targetSubtitleUrl,
                    SourceLanguage = job.Request.SourceLocale,
                    TargetLanguage = job.Request.TargetLocale,
                    Job = job  // Pass full job for Foundry Agent Service context
                },
                CreateRetryOptions());

            if (subtitleValidation.Success && subtitleValidation.ValidationResult != null)
            {
                job.ValidationResult = subtitleValidation.ValidationResult;
                
                // Store thread ID for interactive chat (if using Foundry Agent Service)
                if (!string.IsNullOrEmpty(subtitleValidation.ThreadId))
                {
                    job.ValidationThreadId = subtitleValidation.ThreadId;
                    logger.LogInformation("Job {JobId}: Foundry thread created: {ThreadId}", 
                        job.JobId, subtitleValidation.ThreadId);
                }
                
                logger.LogInformation(
                    "Job {JobId}: Validation complete. Score={Score}, Valid={IsValid}",
                    job.JobId,
                    subtitleValidation.ValidationResult.ConfidenceScore,
                    subtitleValidation.ValidationResult.IsValid);
            }
            else if (subtitleValidation.Success && subtitleValidation.MultiAgentResult != null)
            {
                // Multi-Agent validation result (4 specialist agents)
                job.MultiAgentValidation = subtitleValidation.MultiAgentResult;
                
                // Also set legacy fields for backward compatibility
                job.ValidationThreadId = subtitleValidation.MultiAgentResult.OrchestratorThreadId;
                
                logger.LogInformation(
                    "Job {JobId}: Multi-agent validation complete. " +
                    "Overall={Overall:F1}, Recommendation={Rec}, Issues={Issues}",
                    job.JobId,
                    subtitleValidation.MultiAgentResult.OverallScore,
                    subtitleValidation.MultiAgentResult.Recommendation,
                    subtitleValidation.MultiAgentResult.AllIssues?.Count ?? 0);
            }
            else
            {
                logger.LogWarning("Job {JobId}: Validation failed: {Error}", job.JobId, subtitleValidation.Error);
                // Continue without validation result - don't block the workflow
            }

            // Step 7: Wait for human approval
            job.Status = JobStatus.PendingApproval;
            job.StatusMessage = "Awaiting human approval...";
            job.ApprovalRequestedAt = context.CurrentUtcDateTime;
            job.LastUpdatedAt = context.CurrentUtcDateTime;

            // IMPORTANT: Set custom status so the UI can read the current state while waiting for approval
            // Without this, GetJobStatus only sees the initial input since output is null until orchestration completes
            context.SetCustomStatus(job);

            logger.LogInformation("Job {JobId}: Waiting for human approval (timeout: {Timeout})", job.JobId, ApprovalTimeout);

            // Wait for external event with timeout - use CancellationTokenSource to properly cancel timer
            using var timeoutCts = new CancellationTokenSource();
            var approvalTask = context.WaitForExternalEvent<ApprovalDecision>("ApprovalDecision");
            var timeoutTask = context.CreateTimer(context.CurrentUtcDateTime.Add(ApprovalTimeout), timeoutCts.Token);

            var winner = await Task.WhenAny(approvalTask, timeoutTask);

            if (winner == timeoutTask && !timeoutCts.IsCancellationRequested)
            {
                // Auto-reject after timeout
                logger.LogWarning("Job {JobId}: Approval timed out after {Days} days", job.JobId, ApprovalTimeout.TotalDays);
                job.ApprovalDecision = new ApprovalDecision
                {
                    Approved = false,
                    Reason = $"Auto-rejected: No response within {ApprovalTimeout.TotalDays} days",
                    ReviewedBy = "System"
                };
                job.Status = JobStatus.Rejected;
                job.StatusMessage = "Auto-rejected due to approval timeout";
            }
            else
            {
                // Human decision received - cancel the timeout timer
                timeoutCts.Cancel();
                
                var decision = await approvalTask;
                job.ApprovalDecision = decision;
                job.ApprovalDecisionAt = context.CurrentUtcDateTime;

                if (decision.Approved)
                {
                    job.Status = JobStatus.Approved;
                    job.StatusMessage = $"Approved by {decision.ReviewedBy ?? "reviewer"}";
                    logger.LogInformation("Job {JobId}: Approved by {Reviewer}", job.JobId, decision.ReviewedBy);
                }
                else
                {
                    job.Status = JobStatus.Rejected;
                    job.StatusMessage = $"Rejected by {decision.ReviewedBy ?? "reviewer"}: {decision.Reason ?? "No reason provided"}";
                    logger.LogInformation("Job {JobId}: Rejected by {Reviewer}: {Reason}", 
                        job.JobId, decision.ReviewedBy, decision.Reason);
                }
            }

            job.LastUpdatedAt = context.CurrentUtcDateTime;
            logger.LogInformation("Job {JobId}: Final status: {Status}", job.JobId, job.Status);
            return job;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Job {JobId}: Unhandled exception", job.JobId);
            job.Status = JobStatus.Failed;
            job.Error = ex.Message;
            job.StatusMessage = "Unhandled error";
            job.LastUpdatedAt = context.CurrentUtcDateTime;
            return job;
        }
    }

    private static TaskOptions CreateRetryOptions()
    {
        return TaskOptions.FromRetryPolicy(new RetryPolicy(
            maxNumberOfAttempts: MaxRetries,
            firstRetryInterval: InitialRetryDelay,
            backoffCoefficient: 2.0));
    }
}
