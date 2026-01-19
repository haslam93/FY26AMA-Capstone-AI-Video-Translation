using VideoTranslation.UI.Models;
using Xunit;

namespace VideoTranslation.UI.Tests.Models;

public class JobModelsTests
{
    [Fact]
    public void CreateJobRequest_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var request = new CreateJobRequest();

        // Assert
        Assert.Equal("en-US", request.SourceLocale);
        Assert.Equal("es-ES", request.TargetLocale);
        Assert.Equal("PlatformVoice", request.VoiceKind);
        Assert.Null(request.SpeakerCount);
        Assert.Null(request.VideoUrl);
        Assert.Null(request.BlobPath);
    }

    [Fact]
    public void CreateJobRequest_CanSetProperties()
    {
        // Arrange & Act
        var request = new CreateJobRequest
        {
            SourceLocale = "fr-FR",
            TargetLocale = "de-DE",
            VoiceKind = "PersonalVoice",
            SpeakerCount = 3,
            DisplayName = "Test Job",
            BlobPath = "inputs/video.mp4"
        };

        // Assert
        Assert.Equal("fr-FR", request.SourceLocale);
        Assert.Equal("de-DE", request.TargetLocale);
        Assert.Equal("PersonalVoice", request.VoiceKind);
        Assert.Equal(3, request.SpeakerCount);
        Assert.Equal("Test Job", request.DisplayName);
        Assert.Equal("inputs/video.mp4", request.BlobPath);
    }

    [Fact]
    public void JobStatusResponse_CanSetAllProperties()
    {
        // Arrange
        var createdAt = DateTime.UtcNow;
        var lastUpdatedAt = DateTime.UtcNow.AddMinutes(5);
        var result = new JobResultDto
        {
            TranslatedVideoUrl = "https://example.com/translated.mp4",
            SourceSubtitleUrl = "https://example.com/source.vtt",
            TargetSubtitleUrl = "https://example.com/target.vtt"
        };

        // Act
        var response = new JobStatusResponse
        {
            JobId = "job-123",
            Status = "Completed",
            DisplayName = "Test Translation",
            SourceLocale = "en-US",
            TargetLocale = "ja-JP",
            CreatedAt = createdAt,
            LastUpdatedAt = lastUpdatedAt,
            IterationNumber = 1,
            Result = result,
            Error = null
        };

        // Assert
        Assert.Equal("job-123", response.JobId);
        Assert.Equal("Completed", response.Status);
        Assert.Equal("Test Translation", response.DisplayName);
        Assert.Equal("en-US", response.SourceLocale);
        Assert.Equal("ja-JP", response.TargetLocale);
        Assert.Equal(createdAt, response.CreatedAt);
        Assert.Equal(lastUpdatedAt, response.LastUpdatedAt);
        Assert.Equal(1, response.IterationNumber);
        Assert.NotNull(response.Result);
        Assert.Equal("https://example.com/translated.mp4", response.Result.TranslatedVideoUrl);
        Assert.Null(response.Error);
    }

    [Fact]
    public void JobSummary_CanSetProperties()
    {
        // Arrange & Act
        var item = new JobSummary
        {
            JobId = "job-456",
            Status = "Processing",
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow
        };

        // Assert
        Assert.Equal("job-456", item.JobId);
        Assert.Equal("Processing", item.Status);
    }

    [Fact]
    public void UploadResponse_CanSetProperties()
    {
        // Arrange & Act
        var response = new UploadResponse
        {
            UploadId = "upload-123",
            FileName = "uploaded-video.mp4",
            BlobPath = "inputs/uploaded-video.mp4",
            BlobUrl = "https://storage.blob.core.windows.net/inputs/uploaded-video.mp4",
            ContentType = "video/mp4"
        };

        // Assert
        Assert.Equal("upload-123", response.UploadId);
        Assert.Equal("uploaded-video.mp4", response.FileName);
        Assert.Equal("inputs/uploaded-video.mp4", response.BlobPath);
        Assert.Equal("https://storage.blob.core.windows.net/inputs/uploaded-video.mp4", response.BlobUrl);
        Assert.Equal("video/mp4", response.ContentType);
    }

    [Fact]
    public void CreateJobResponse_CanSetProperties()
    {
        // Arrange & Act
        var response = new CreateJobResponse
        {
            JobId = "job-789",
            Status = "Submitted",
            StatusUrl = "/api/jobs/job-789/status"
        };

        // Assert
        Assert.Equal("job-789", response.JobId);
        Assert.Equal("Submitted", response.Status);
        Assert.Equal("/api/jobs/job-789/status", response.StatusUrl);
    }

    [Theory]
    [InlineData("Submitted", false)]
    [InlineData("Processing", false)]
    [InlineData("Completed", true)]
    [InlineData("Failed", true)]
    public void JobStatus_IsTerminalState(string status, bool expectedTerminal)
    {
        // Arrange & Act
        var isTerminal = status is "Completed" or "Failed";

        // Assert
        Assert.Equal(expectedTerminal, isTerminal);
    }

    [Fact]
    public void SupportedLocale_CanSetProperties()
    {
        // Arrange & Act
        var locale = new SupportedLocale
        {
            Code = "en-US",
            Name = "English (United States)"
        };

        // Assert
        Assert.Equal("en-US", locale.Code);
        Assert.Equal("English (United States)", locale.Name);
    }

    #region Multi-Agent Validation Tests

    [Fact]
    public void JobStatusResponse_HasMultiAgentValidation_ReturnsFalse_WhenNull()
    {
        // Arrange
        var response = new JobStatusResponse
        {
            JobId = "job-123",
            MultiAgentValidation = null
        };

        // Assert
        Assert.False(response.HasMultiAgentValidation);
    }

    [Fact]
    public void JobStatusResponse_HasMultiAgentValidation_ReturnsTrue_WhenSet()
    {
        // Arrange
        var response = new JobStatusResponse
        {
            JobId = "job-123",
            MultiAgentValidation = new MultiAgentValidationResult
            {
                OverallScore = 85,
                Recommendation = "Approve"
            }
        };

        // Assert
        Assert.True(response.HasMultiAgentValidation);
    }

    [Fact]
    public void MultiAgentValidationResult_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var result = new MultiAgentValidationResult();

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(0, result.OverallScore);
        Assert.Equal("NeedsReview", result.Recommendation);
        Assert.Equal(string.Empty, result.Summary);
        Assert.Null(result.TranslationReview);
        Assert.Null(result.TechnicalReview);
        Assert.Null(result.CulturalReview);
    }

    [Fact]
    public void MultiAgentValidationResult_CanSetAllAgentReviews()
    {
        // Arrange & Act
        var result = new MultiAgentValidationResult
        {
            IsValid = true,
            OverallScore = 85,
            Recommendation = "Approve",
            Summary = "High quality translation",
            TranslationReview = new AgentReviewResult { Score = 90, AgentType = "translation" },
            TechnicalReview = new AgentReviewResult { Score = 80, AgentType = "technical" },
            CulturalReview = new AgentReviewResult { Score = 85, AgentType = "cultural" }
        };

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(85, result.OverallScore);
        Assert.Equal("Approve", result.Recommendation);
        Assert.NotNull(result.TranslationReview);
        Assert.NotNull(result.TechnicalReview);
        Assert.NotNull(result.CulturalReview);
        Assert.Equal(90, result.TranslationReview.Score);
        Assert.Equal(80, result.TechnicalReview.Score);
        Assert.Equal(85, result.CulturalReview.Score);
    }

    [Fact]
    public void AgentReviewResult_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var review = new AgentReviewResult();

        // Assert
        Assert.Equal(string.Empty, review.AgentName);
        Assert.Equal(string.Empty, review.AgentType);
        Assert.Equal(0, review.Score);
        Assert.Equal(string.Empty, review.Reasoning);
        Assert.NotNull(review.Issues);
        Assert.Empty(review.Issues);
    }

    [Fact]
    public void AgentReviewResult_CanSetAllProperties()
    {
        // Arrange
        var issues = new List<MultiAgentIssue>
        {
            new() { Description = "Test issue", Severity = "minor", Category = "translation" }
        };

        // Act
        var review = new AgentReviewResult
        {
            AgentName = "TranslationReviewAgent",
            AgentType = "translation",
            Score = 85.5,
            Reasoning = "Good translation quality",
            Issues = issues,
            ThreadId = "thread-123"
        };

        // Assert
        Assert.Equal("TranslationReviewAgent", review.AgentName);
        Assert.Equal("translation", review.AgentType);
        Assert.Equal(85.5, review.Score);
        Assert.Equal("Good translation quality", review.Reasoning);
        Assert.Single(review.Issues);
        Assert.Equal("thread-123", review.ThreadId);
    }

    [Fact]
    public void MultiAgentIssue_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var issue = new MultiAgentIssue();

        // Assert
        Assert.Equal("minor", issue.Severity);
        Assert.Equal(string.Empty, issue.Category);
        Assert.Equal(string.Empty, issue.Description);
        Assert.Null(issue.Location);
        Assert.Null(issue.Suggestion);
    }

    [Fact]
    public void MultiAgentIssue_CanSetAllProperties()
    {
        // Act
        var issue = new MultiAgentIssue
        {
            Severity = "critical",
            Category = "translation",
            Description = "Meaning lost in translation",
            Location = "Cue 5, 00:01:23.000",
            Suggestion = "Use 'greeting' instead of 'hello'"
        };

        // Assert
        Assert.Equal("critical", issue.Severity);
        Assert.Equal("translation", issue.Category);
        Assert.Equal("Meaning lost in translation", issue.Description);
        Assert.Equal("Cue 5, 00:01:23.000", issue.Location);
        Assert.Equal("Use 'greeting' instead of 'hello'", issue.Suggestion);
    }

    [Fact]
    public void MultiAgentValidationResult_CanSetThreadIds()
    {
        // Act
        var result = new MultiAgentValidationResult
        {
            OrchestratorThreadId = "orch-thread-1",
            TranslationAgentThreadId = "trans-thread-1",
            TechnicalAgentThreadId = "tech-thread-1",
            CulturalAgentThreadId = "cult-thread-1"
        };

        // Assert
        Assert.Equal("orch-thread-1", result.OrchestratorThreadId);
        Assert.Equal("trans-thread-1", result.TranslationAgentThreadId);
        Assert.Equal("tech-thread-1", result.TechnicalAgentThreadId);
        Assert.Equal("cult-thread-1", result.CulturalAgentThreadId);
    }

    #endregion

    #region Chat and Approval Tests

    [Fact]
    public void ChatRequest_CanSetProperties()
    {
        // Act
        var request = new ChatRequest
        {
            Message = "What issues were found?",
            AgentType = "translation"
        };

        // Assert
        Assert.Equal("What issues were found?", request.Message);
        Assert.Equal("translation", request.AgentType);
    }

    [Fact]
    public void ChatResponse_CanSetProperties()
    {
        // Act
        var response = new ChatResponse
        {
            Message = "I found 3 translation issues...",
            Timestamp = DateTime.UtcNow
        };

        // Assert
        Assert.Equal("I found 3 translation issues...", response.Message);
        Assert.True(response.Timestamp <= DateTime.UtcNow);
    }

    [Fact]
    public void ConversationMessage_CanSetAgentType()
    {
        // Act
        var message = new ConversationMessage
        {
            Role = "assistant",
            Content = "Analysis complete",
            AgentType = "orchestrator"
        };

        // Assert
        Assert.Equal("assistant", message.Role);
        Assert.Equal("Analysis complete", message.Content);
        Assert.Equal("orchestrator", message.AgentType);
    }

    [Fact]
    public void ApprovalDecision_CanSetApproved()
    {
        // Act
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
    }

    [Fact]
    public void ApprovalDecision_CanSetRejected()
    {
        // Act
        var decision = new ApprovalDecision
        {
            Approved = false,
            ReviewedBy = "reviewer@example.com",
            Reason = "Poor translation quality"
        };

        // Assert
        Assert.False(decision.Approved);
        Assert.Equal("reviewer@example.com", decision.ReviewedBy);
        Assert.Equal("Poor translation quality", decision.Reason);
    }

    [Fact]
    public void PendingApprovalJob_CanSetProperties()
    {
        // Act
        var job = new PendingApprovalJob
        {
            JobId = "job-123",
            DisplayName = "Test Job",
            SourceLocale = "en-US",
            TargetLocale = "es-ES",
            Status = "PendingApproval",
            ApprovalRequestedAt = DateTime.UtcNow
        };

        // Assert
        Assert.Equal("job-123", job.JobId);
        Assert.Equal("Test Job", job.DisplayName);
        Assert.Equal("en-US", job.SourceLocale);
        Assert.Equal("es-ES", job.TargetLocale);
        Assert.Equal("PendingApproval", job.Status);
    }

    #endregion

    #region ValidationIssue Display Tests

    [Theory]
    [InlineData(0, "Low")]
    [InlineData(1, "Medium")]
    [InlineData(2, "High")]
    [InlineData(3, "Critical")]
    [InlineData(99, "Unknown")]
    public void ValidationIssue_SeverityText_ReturnsCorrectValue(int severity, string expected)
    {
        // Arrange
        var issue = new ValidationIssue { Severity = severity };

        // Assert
        Assert.Equal(expected, issue.SeverityText);
    }

    [Theory]
    [InlineData(0, "Timing")]
    [InlineData(1, "Grammar")]
    [InlineData(2, "TranslationAccuracy")]
    [InlineData(3, "CulturalContext")]
    [InlineData(4, "Formatting")]
    [InlineData(5, "ContentAppropriateness")]
    [InlineData(6, "MissingContent")]
    [InlineData(99, "Unknown")]
    public void ValidationIssue_CategoryText_ReturnsCorrectValue(int category, string expected)
    {
        // Arrange
        var issue = new ValidationIssue { Category = category };

        // Assert
        Assert.Equal(expected, issue.CategoryText);
    }

    #endregion
}
