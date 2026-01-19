using Microsoft.Extensions.Configuration;

namespace VideoTranslation.Integration.Tests;

/// <summary>
/// Base class for integration tests providing shared configuration and utilities.
/// </summary>
public abstract class IntegrationTestBase : IDisposable
{
    protected readonly IConfiguration Configuration;
    protected readonly bool SkipLiveTests;

    protected IntegrationTestBase()
    {
        Configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.test.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        SkipLiveTests = Configuration.GetValue<bool>("TestSettings:SkipLiveTests", true);
    }

    /// <summary>
    /// Skip test if live tests are disabled.
    /// </summary>
    protected void SkipIfLiveTestsDisabled()
    {
        if (SkipLiveTests)
        {
            throw new InvalidOperationException("Live tests are disabled. Set TestSettings:SkipLiveTests=false to enable.");
        }
    }

    /// <summary>
    /// Get configuration value with validation.
    /// </summary>
    protected string GetRequiredConfig(string key)
    {
        var value = Configuration[key];
        if (string.IsNullOrEmpty(value))
        {
            throw new InvalidOperationException($"Required configuration '{key}' is not set.");
        }
        return value;
    }

    public virtual void Dispose()
    {
        // Cleanup resources if needed
        GC.SuppressFinalize(this);
    }
}
