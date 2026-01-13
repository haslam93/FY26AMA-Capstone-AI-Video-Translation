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
}
