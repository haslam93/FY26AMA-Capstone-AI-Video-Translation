using VideoTranslation.Api.Services;
using Xunit;

namespace VideoTranslation.Api.Tests.Services;

/// <summary>
/// Unit tests for BlobStorageOptions configuration model.
/// Since BlobStorageService requires actual Azure SDK client which can't be easily mocked,
/// we test the configuration options and validate the interface contract.
/// </summary>
public class BlobStorageOptionsTests
{
    [Fact]
    public void BlobStorageOptions_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new BlobStorageOptions();

        // Assert
        Assert.Equal(string.Empty, options.AccountName);
        Assert.Null(options.ConnectionString);
        Assert.Equal("videos", options.VideosContainer);
        Assert.Equal("outputs", options.OutputsContainer);
        Assert.Equal("subtitles", options.SubtitlesContainer);
    }

    [Fact]
    public void BlobStorageOptions_SectionName_IsCorrect()
    {
        // Assert
        Assert.Equal("BlobStorage", BlobStorageOptions.SectionName);
    }

    [Fact]
    public void BlobStorageOptions_CanSetAccountName()
    {
        // Arrange & Act
        var options = new BlobStorageOptions
        {
            AccountName = "mystorageaccount"
        };

        // Assert
        Assert.Equal("mystorageaccount", options.AccountName);
    }

    [Fact]
    public void BlobStorageOptions_CanSetConnectionString()
    {
        // Arrange
        var connectionString = "DefaultEndpointsProtocol=https;AccountName=test;AccountKey=key;EndpointSuffix=core.windows.net";

        // Act
        var options = new BlobStorageOptions
        {
            ConnectionString = connectionString
        };

        // Assert
        Assert.Equal(connectionString, options.ConnectionString);
    }

    [Fact]
    public void BlobStorageOptions_CanSetCustomContainerNames()
    {
        // Arrange & Act
        var options = new BlobStorageOptions
        {
            VideosContainer = "custom-videos",
            OutputsContainer = "custom-outputs",
            SubtitlesContainer = "custom-subtitles"
        };

        // Assert
        Assert.Equal("custom-videos", options.VideosContainer);
        Assert.Equal("custom-outputs", options.OutputsContainer);
        Assert.Equal("custom-subtitles", options.SubtitlesContainer);
    }

    [Fact]
    public void BlobStorageOptions_CanConfigureAllProperties()
    {
        // Arrange & Act
        var options = new BlobStorageOptions
        {
            AccountName = "storageama3",
            ConnectionString = null,
            VideosContainer = "videos",
            OutputsContainer = "outputs",
            SubtitlesContainer = "subtitles"
        };

        // Assert
        Assert.Equal("storageama3", options.AccountName);
        Assert.Null(options.ConnectionString);
        Assert.Equal("videos", options.VideosContainer);
        Assert.Equal("outputs", options.OutputsContainer);
        Assert.Equal("subtitles", options.SubtitlesContainer);
    }
}

/// <summary>
/// Interface contract tests for IBlobStorageService.
/// Validates that the interface defines all expected methods.
/// </summary>
public class BlobStorageServiceInterfaceTests
{
    [Fact]
    public void IBlobStorageService_DefinesGenerateSasUrlAsync()
    {
        // Arrange
        var interfaceType = typeof(IBlobStorageService);

        // Act
        var method = interfaceType.GetMethod("GenerateSasUrlAsync");

        // Assert
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<string>), method.ReturnType);
    }

    [Fact]
    public void IBlobStorageService_DefinesCopyFromUrlAsync()
    {
        // Arrange
        var interfaceType = typeof(IBlobStorageService);

        // Act
        var method = interfaceType.GetMethod("CopyFromUrlAsync");

        // Assert
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<string>), method.ReturnType);
    }

    [Fact]
    public void IBlobStorageService_DefinesUploadAsync()
    {
        // Arrange
        var interfaceType = typeof(IBlobStorageService);

        // Act
        var method = interfaceType.GetMethod("UploadAsync");

        // Assert
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<string>), method.ReturnType);
    }

    [Fact]
    public void IBlobStorageService_DefinesExistsAsync()
    {
        // Arrange
        var interfaceType = typeof(IBlobStorageService);

        // Act
        var method = interfaceType.GetMethod("ExistsAsync");

        // Assert
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<bool>), method.ReturnType);
    }

    [Fact]
    public void IBlobStorageService_DefinesReadAsStringAsync()
    {
        // Arrange
        var interfaceType = typeof(IBlobStorageService);

        // Act
        var method = interfaceType.GetMethod("ReadAsStringAsync");

        // Assert
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<string>), method.ReturnType);
    }

    [Fact]
    public void IBlobStorageService_DefinesDeleteAsync()
    {
        // Arrange
        var interfaceType = typeof(IBlobStorageService);

        // Act
        var method = interfaceType.GetMethod("DeleteAsync");

        // Assert
        Assert.NotNull(method);
        Assert.Equal(typeof(Task), method.ReturnType);
    }

    [Fact]
    public void BlobStorageService_ImplementsIBlobStorageService()
    {
        // Assert
        Assert.True(typeof(IBlobStorageService).IsAssignableFrom(typeof(BlobStorageService)));
    }
}
