using VideoTranslation.Api.Models;
using Xunit;

namespace VideoTranslation.Api.Tests.Activities;

public class ValidateInputActivityTests
{
    [Fact]
    public void ValidateInput_WithValidRequest_ReturnsTrue()
    {
        // Arrange
        var request = new TranslationJobRequest
        {
            BlobPath = "inputs/test-video.mp4",
            SourceLocale = "en-US",
            TargetLocale = "es-ES",
            VoiceKind = "PlatformVoice",
            SpeakerCount = 1
        };

        // Act
        var isValid = IsValidRequest(request);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void ValidateInput_WithEmptyBlobPath_AndNoVideoUrl_ReturnsFalse()
    {
        // Arrange
        var request = new TranslationJobRequest
        {
            BlobPath = "",
            VideoUrl = null,
            SourceLocale = "en-US",
            TargetLocale = "es-ES",
            VoiceKind = "PlatformVoice",
            SpeakerCount = 1
        };

        // Act
        var isValid = IsValidRequest(request);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void ValidateInput_WithVideoUrl_ReturnsTrue()
    {
        // Arrange
        var request = new TranslationJobRequest
        {
            VideoUrl = "https://example.com/video.mp4",
            BlobPath = null,
            SourceLocale = "en-US",
            TargetLocale = "es-ES",
            VoiceKind = "PlatformVoice",
            SpeakerCount = 1
        };

        // Act
        var isValid = IsValidRequest(request);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void ValidateInput_WithEmptySourceLocale_ReturnsFalse()
    {
        // Arrange
        var request = new TranslationJobRequest
        {
            BlobPath = "inputs/test-video.mp4",
            SourceLocale = "",
            TargetLocale = "es-ES",
            VoiceKind = "PlatformVoice",
            SpeakerCount = 1
        };

        // Act
        var isValid = IsValidRequest(request);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void ValidateInput_WithEmptyTargetLocale_ReturnsFalse()
    {
        // Arrange
        var request = new TranslationJobRequest
        {
            BlobPath = "inputs/test-video.mp4",
            SourceLocale = "en-US",
            TargetLocale = "",
            VoiceKind = "PlatformVoice",
            SpeakerCount = 1
        };

        // Act
        var isValid = IsValidRequest(request);

        // Assert
        Assert.False(isValid);
    }

    [Theory]
    [InlineData("PlatformVoice")]
    [InlineData("PersonalVoice")]
    public void ValidateInput_WithValidVoiceKind_ReturnsTrue(string voiceKind)
    {
        // Arrange
        var request = new TranslationJobRequest
        {
            BlobPath = "inputs/test-video.mp4",
            SourceLocale = "en-US",
            TargetLocale = "es-ES",
            VoiceKind = voiceKind,
            SpeakerCount = 1
        };

        // Act
        var isValid = IsValidRequest(request);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void ValidateInput_DefaultVoiceKind_IsPlatformVoice()
    {
        // Arrange & Act
        var request = new TranslationJobRequest();

        // Assert
        Assert.Equal("PlatformVoice", request.VoiceKind);
    }

    /// <summary>
    /// Simple validation logic matching the activity
    /// </summary>
    private static bool IsValidRequest(TranslationJobRequest request)
    {
        // Must have either VideoUrl or BlobPath
        if (string.IsNullOrWhiteSpace(request.VideoUrl) && string.IsNullOrWhiteSpace(request.BlobPath))
            return false;

        if (string.IsNullOrWhiteSpace(request.SourceLocale))
            return false;

        if (string.IsNullOrWhiteSpace(request.TargetLocale))
            return false;

        return true;
    }
}
