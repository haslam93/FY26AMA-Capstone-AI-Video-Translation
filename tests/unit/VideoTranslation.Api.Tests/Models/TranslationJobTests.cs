using VideoTranslation.Api.Models;
using Xunit;

namespace VideoTranslation.Api.Tests.Models;

public class TranslationJobTests
{
    [Fact]
    public void TranslationJob_DefaultStatus_IsSubmitted()
    {
        // Arrange & Act
        var job = new TranslationJob();

        // Assert
        Assert.Equal(JobStatus.Submitted, job.Status);
    }

    [Fact]
    public void TranslationJob_CanSetProperties()
    {
        // Arrange
        var jobId = Guid.NewGuid().ToString();
        var translationId = Guid.NewGuid().ToString();
        var iterationId = Guid.NewGuid().ToString();
        var request = new TranslationJobRequest
        {
            SourceLocale = "en-US",
            TargetLocale = "es-ES",
            BlobPath = "inputs/test.mp4"
        };

        // Act
        var job = new TranslationJob
        {
            JobId = jobId,
            TranslationId = translationId,
            IterationId = iterationId,
            Request = request,
            Status = JobStatus.Processing,
            CreatedAt = DateTime.UtcNow
        };

        // Assert
        Assert.Equal(jobId, job.JobId);
        Assert.Equal(translationId, job.TranslationId);
        Assert.Equal(iterationId, job.IterationId);
        Assert.Equal("en-US", job.Request.SourceLocale);
        Assert.Equal("es-ES", job.Request.TargetLocale);
        Assert.Equal("inputs/test.mp4", job.Request.BlobPath);
        Assert.Equal(JobStatus.Processing, job.Status);
    }

    [Fact]
    public void JobStatus_HasExpectedValues()
    {
        // Assert - verify key status values exist
        Assert.Equal(0, (int)JobStatus.Submitted);
        Assert.True(Enum.IsDefined(typeof(JobStatus), JobStatus.Validating));
        Assert.True(Enum.IsDefined(typeof(JobStatus), JobStatus.Processing));
        Assert.True(Enum.IsDefined(typeof(JobStatus), JobStatus.Completed));
        Assert.True(Enum.IsDefined(typeof(JobStatus), JobStatus.Failed));
    }

    [Fact]
    public void TranslationJobRequest_DefaultVoiceKind_IsPlatformVoice()
    {
        // Arrange & Act
        var request = new TranslationJobRequest();

        // Assert - VoiceKind should default to PlatformVoice
        Assert.Equal("PlatformVoice", request.VoiceKind);
    }

    [Fact]
    public void TranslationJobRequest_DefaultSpeakerCount_IsNull()
    {
        // Arrange & Act
        var request = new TranslationJobRequest();

        // Assert - SpeakerCount is nullable for auto-detection
        Assert.Null(request.SpeakerCount);
    }

    [Fact]
    public void TranslationJob_DefaultIterationNumber_IsZero()
    {
        // Arrange & Act
        var job = new TranslationJob();

        // Assert
        Assert.Equal(0, job.IterationNumber);
    }

    [Fact]
    public void TranslationJob_CreatedAt_DefaultsToUtcNow()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var job = new TranslationJob();

        // Assert
        var after = DateTime.UtcNow;
        Assert.True(job.CreatedAt >= before && job.CreatedAt <= after);
    }

    #region Multi-Agent Validation Tests

    [Fact]
    public void TranslationJob_MultiAgentValidation_DefaultsToNull()
    {
        // Arrange & Act
        var job = new TranslationJob();

        // Assert
        Assert.Null(job.MultiAgentValidation);
    }

    [Fact]
    public void TranslationJob_CanSetMultiAgentValidation()
    {
        // Arrange
        var validation = new MultiAgentValidationResult
        {
            OverallScore = 85,
            Recommendation = "Approve",
            IsValid = true,
            Summary = "High quality translation"
        };

        // Act
        var job = new TranslationJob
        {
            MultiAgentValidation = validation
        };

        // Assert
        Assert.NotNull(job.MultiAgentValidation);
        Assert.Equal(85, job.MultiAgentValidation.OverallScore);
        Assert.Equal("Approve", job.MultiAgentValidation.Recommendation);
        Assert.True(job.MultiAgentValidation.IsValid);
    }

    [Fact]
    public void JobStatus_HasRunningValidationStatus()
    {
        // Assert
        Assert.True(Enum.IsDefined(typeof(JobStatus), JobStatus.RunningValidation));
    }

    [Fact]
    public void JobStatus_HasPendingApprovalStatus()
    {
        // Assert
        Assert.True(Enum.IsDefined(typeof(JobStatus), JobStatus.PendingApproval));
    }

    [Fact]
    public void JobStatus_HasApprovedStatus()
    {
        // Assert
        Assert.True(Enum.IsDefined(typeof(JobStatus), JobStatus.Approved));
    }

    [Fact]
    public void JobStatus_HasRejectedStatus()
    {
        // Assert
        Assert.True(Enum.IsDefined(typeof(JobStatus), JobStatus.Rejected));
    }

    #endregion

    #region Approval Decision Tests

    [Fact]
    public void TranslationJob_ApprovalDecision_DefaultsToNull()
    {
        // Arrange & Act
        var job = new TranslationJob();

        // Assert
        Assert.Null(job.ApprovalDecision);
        Assert.Null(job.ApprovalRequestedAt);
        Assert.Null(job.ApprovalDecisionAt);
    }

    [Fact]
    public void ApprovalDecision_CanSetApproved()
    {
        // Arrange & Act
        var decision = new ApprovalDecision
        {
            Approved = true,
            ReviewedBy = "reviewer@example.com",
            Comments = "Looks good!"
        };

        // Assert
        Assert.True(decision.Approved);
        Assert.Equal("reviewer@example.com", decision.ReviewedBy);
        Assert.Equal("Looks good!", decision.Comments);
        Assert.Null(decision.Reason);
    }

    [Fact]
    public void ApprovalDecision_CanSetRejected()
    {
        // Arrange & Act
        var decision = new ApprovalDecision
        {
            Approved = false,
            ReviewedBy = "reviewer@example.com",
            Reason = "Poor translation quality",
            Comments = "Needs significant revision"
        };

        // Assert
        Assert.False(decision.Approved);
        Assert.Equal("reviewer@example.com", decision.ReviewedBy);
        Assert.Equal("Poor translation quality", decision.Reason);
        Assert.Equal("Needs significant revision", decision.Comments);
    }

    [Fact]
    public void TranslationJob_CanTrackApprovalTimestamps()
    {
        // Arrange
        var requestedAt = DateTime.UtcNow;
        var decisionAt = requestedAt.AddMinutes(30);

        // Act
        var job = new TranslationJob
        {
            Status = JobStatus.Approved,
            ApprovalRequestedAt = requestedAt,
            ApprovalDecisionAt = decisionAt,
            ApprovalDecision = new ApprovalDecision { Approved = true }
        };

        // Assert
        Assert.Equal(requestedAt, job.ApprovalRequestedAt);
        Assert.Equal(decisionAt, job.ApprovalDecisionAt);
        Assert.True(job.ApprovalDecision.Approved);
    }

    #endregion

    #region Translation Result Tests

    [Fact]
    public void TranslationResult_CanSetAllUrls()
    {
        // Arrange & Act
        var result = new TranslationResult
        {
            TranslatedVideoUrl = "https://example.com/video.mp4",
            SourceSubtitleUrl = "https://example.com/source.vtt",
            TargetSubtitleUrl = "https://example.com/target.vtt",
            MetadataUrl = "https://example.com/metadata.json"
        };

        // Assert
        Assert.Equal("https://example.com/video.mp4", result.TranslatedVideoUrl);
        Assert.Equal("https://example.com/source.vtt", result.SourceSubtitleUrl);
        Assert.Equal("https://example.com/target.vtt", result.TargetSubtitleUrl);
        Assert.Equal("https://example.com/metadata.json", result.MetadataUrl);
    }

    [Fact]
    public void StoredOutputs_CanSetAllUrls()
    {
        // Arrange & Act
        var stored = new StoredOutputs
        {
            VideoUrl = "https://storage.blob/outputs/video.mp4",
            SourceSubtitleUrl = "https://storage.blob/outputs/source.vtt",
            TargetSubtitleUrl = "https://storage.blob/outputs/target.vtt",
            MetadataUrl = "https://storage.blob/outputs/metadata.json"
        };

        // Assert
        Assert.Equal("https://storage.blob/outputs/video.mp4", stored.VideoUrl);
        Assert.Equal("https://storage.blob/outputs/source.vtt", stored.SourceSubtitleUrl);
        Assert.Equal("https://storage.blob/outputs/target.vtt", stored.TargetSubtitleUrl);
        Assert.Equal("https://storage.blob/outputs/metadata.json", stored.MetadataUrl);
    }

    #endregion
}
