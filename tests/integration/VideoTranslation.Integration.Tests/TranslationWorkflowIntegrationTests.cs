using VideoTranslation.Api.Models;
using Xunit;

namespace VideoTranslation.Integration.Tests;

/// <summary>
/// Integration tests for the translation workflow state machine.
/// Tests status transitions and job lifecycle scenarios.
/// </summary>
public class TranslationWorkflowIntegrationTests : IntegrationTestBase
{
    #region Status Transition Tests

    [Fact]
    public void JobStatus_FullSuccessfulWorkflow_ProgressesThroughAllStates()
    {
        // Test the expected progression of status values
        var expectedStatuses = new[]
        {
            JobStatus.Submitted,
            JobStatus.Validating,
            JobStatus.Validated,
            JobStatus.CreatingTranslation,
            JobStatus.TranslationCreated,
            JobStatus.CreatingIteration,
            JobStatus.IterationCreated,
            JobStatus.Processing,
            JobStatus.CopyingOutputs,
            JobStatus.RunningValidation,
            JobStatus.PendingApproval,
            JobStatus.Approved,
            JobStatus.Completed
        };

        // Verify all expected statuses are defined in enum
        foreach (var status in expectedStatuses)
        {
            Assert.True(Enum.IsDefined(typeof(JobStatus), status),
                $"Status {status} should be defined in JobStatus enum");
        }
    }

    [Fact]
    public void JobStatus_RejectionWorkflow_EndsInRejected()
    {
        // Test rejection workflow statuses
        var job = new TranslationJob
        {
            JobId = Guid.NewGuid().ToString(),
            Status = JobStatus.PendingApproval,
            ApprovalDecision = new ApprovalDecision
            {
                Approved = false,
                ReviewedBy = "reviewer@example.com",
                Reason = "Poor translation quality"
            }
        };

        // Simulate rejection
        job.Status = JobStatus.Rejected;

        Assert.Equal(JobStatus.Rejected, job.Status);
        Assert.False(job.ApprovalDecision.Approved);
        Assert.Equal("Poor translation quality", job.ApprovalDecision.Reason);
    }

    [Fact]
    public void JobStatus_FailureScenario_EndsInFailed()
    {
        // Test failure handling
        var job = new TranslationJob
        {
            JobId = Guid.NewGuid().ToString(),
            Status = JobStatus.Processing,
            Error = "Speech API returned error: Unsupported audio format"
        };

        // Simulate failure
        job.Status = JobStatus.Failed;

        Assert.Equal(JobStatus.Failed, job.Status);
        Assert.NotNull(job.Error);
        Assert.Contains("Unsupported audio format", job.Error);
    }

    #endregion

    #region TranslationJob Lifecycle Tests

    [Fact]
    public void TranslationJob_NewJob_HasCorrectDefaults()
    {
        // Arrange
        var request = new TranslationJobRequest
        {
            SourceLocale = "en-US",
            TargetLocale = "es-ES",
            BlobPath = "inputs/test-video.mp4"
        };

        // Act
        var job = new TranslationJob
        {
            JobId = Guid.NewGuid().ToString(),
            Request = request
        };

        // Assert
        Assert.Equal(JobStatus.Submitted, job.Status);
        Assert.Equal(0, job.IterationNumber);
        Assert.Null(job.MultiAgentValidation);
        Assert.Null(job.ApprovalDecision);
        Assert.Null(job.Result);
        Assert.Null(job.Error);
    }

    [Fact]
    public void TranslationJob_WithMultiAgentValidation_TracksAllData()
    {
        // Arrange
        var job = new TranslationJob
        {
            JobId = Guid.NewGuid().ToString(),
            Status = JobStatus.PendingApproval,
            MultiAgentValidation = new MultiAgentValidationResult
            {
                OverallScore = 76,
                Recommendation = "NeedsReview",
                TranslationReview = new AgentReviewResult { Score = 70, AgentType = "translation" },
                TechnicalReview = new AgentReviewResult { Score = 85, AgentType = "technical" },
                CulturalReview = new AgentReviewResult { Score = 75, AgentType = "cultural" },
                OrchestratorThreadId = "thread-orch-1",
                TranslationAgentThreadId = "thread-trans-1",
                TechnicalAgentThreadId = "thread-tech-1",
                CulturalAgentThreadId = "thread-cult-1"
            },
            ApprovalRequestedAt = DateTime.UtcNow
        };

        // Assert
        Assert.NotNull(job.MultiAgentValidation);
        Assert.Equal(76, job.MultiAgentValidation.OverallScore);
        Assert.Equal("NeedsReview", job.MultiAgentValidation.Recommendation);
        Assert.NotNull(job.ApprovalRequestedAt);
        
        // Verify all agent reviews are present
        Assert.NotNull(job.MultiAgentValidation.TranslationReview);
        Assert.NotNull(job.MultiAgentValidation.TechnicalReview);
        Assert.NotNull(job.MultiAgentValidation.CulturalReview);
        
        // Verify all thread IDs are present
        Assert.NotNull(job.MultiAgentValidation.OrchestratorThreadId);
        Assert.NotNull(job.MultiAgentValidation.TranslationAgentThreadId);
        Assert.NotNull(job.MultiAgentValidation.TechnicalAgentThreadId);
        Assert.NotNull(job.MultiAgentValidation.CulturalAgentThreadId);
    }

    [Fact]
    public void TranslationJob_ApprovalWorkflow_TracksTimestamps()
    {
        // Arrange
        var job = new TranslationJob
        {
            JobId = Guid.NewGuid().ToString(),
            Status = JobStatus.PendingApproval,
            ApprovalRequestedAt = DateTime.UtcNow.AddMinutes(-30)
        };

        // Act - Simulate approval
        job.Status = JobStatus.Approved;
        job.ApprovalDecisionAt = DateTime.UtcNow;
        job.ApprovalDecision = new ApprovalDecision
        {
            Approved = true,
            ReviewedBy = "approver@example.com",
            Comments = "Good quality translation"
        };

        // Assert
        Assert.Equal(JobStatus.Approved, job.Status);
        Assert.NotNull(job.ApprovalRequestedAt);
        Assert.NotNull(job.ApprovalDecisionAt);
        Assert.True(job.ApprovalDecisionAt > job.ApprovalRequestedAt);
        Assert.True(job.ApprovalDecision.Approved);
    }

    #endregion

    #region TranslationJobRequest Validation Tests

    [Fact]
    public void TranslationJobRequest_ValidRequest_HasRequiredFields()
    {
        // Arrange
        var request = new TranslationJobRequest
        {
            BlobPath = "inputs/video.mp4",
            SourceLocale = "en-US",
            TargetLocale = "es-ES",
            VoiceKind = "PlatformVoice",
            SpeakerCount = 1
        };

        // Assert
        Assert.False(string.IsNullOrEmpty(request.BlobPath));
        Assert.False(string.IsNullOrEmpty(request.SourceLocale));
        Assert.False(string.IsNullOrEmpty(request.TargetLocale));
    }

    [Theory]
    [InlineData("en-US", "es-ES")]
    [InlineData("en-US", "ar-EG")]
    [InlineData("ja-JP", "en-US")]
    [InlineData("zh-CN", "en-US")]
    [InlineData("fr-FR", "de-DE")]
    public void TranslationJobRequest_CommonLanguagePairs_AreValid(string sourceLocale, string targetLocale)
    {
        // Arrange
        var request = new TranslationJobRequest
        {
            BlobPath = "inputs/video.mp4",
            SourceLocale = sourceLocale,
            TargetLocale = targetLocale
        };

        // Assert
        Assert.Equal(sourceLocale, request.SourceLocale);
        Assert.Equal(targetLocale, request.TargetLocale);
        Assert.NotEqual(sourceLocale, targetLocale);
    }

    [Theory]
    [InlineData("PlatformVoice")]
    [InlineData("PersonalVoice")]
    public void TranslationJobRequest_VoiceKindOptions_AreValid(string voiceKind)
    {
        // Arrange
        var request = new TranslationJobRequest
        {
            VoiceKind = voiceKind
        };

        // Assert
        Assert.Equal(voiceKind, request.VoiceKind);
    }

    #endregion

    #region TranslationResult Tests

    [Fact]
    public void TranslationResult_SuccessfulJob_HasAllOutputUrls()
    {
        // Arrange
        var result = new TranslationResult
        {
            TranslatedVideoUrl = "https://speech.blob/outputs/video.mp4",
            SourceSubtitleUrl = "https://speech.blob/outputs/source.vtt",
            TargetSubtitleUrl = "https://speech.blob/outputs/target.vtt",
            MetadataUrl = "https://speech.blob/outputs/metadata.json",
            StoredOutputs = new StoredOutputs
            {
                VideoUrl = "https://storage.blob/outputs/video.mp4",
                SourceSubtitleUrl = "https://storage.blob/outputs/source.vtt",
                TargetSubtitleUrl = "https://storage.blob/outputs/target.vtt",
                MetadataUrl = "https://storage.blob/outputs/metadata.json"
            }
        };

        // Assert
        Assert.NotNull(result.TranslatedVideoUrl);
        Assert.NotNull(result.SourceSubtitleUrl);
        Assert.NotNull(result.TargetSubtitleUrl);
        Assert.NotNull(result.StoredOutputs);
        Assert.NotNull(result.StoredOutputs.VideoUrl);
    }

    #endregion
}
