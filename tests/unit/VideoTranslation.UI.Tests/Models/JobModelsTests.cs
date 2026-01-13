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
}
