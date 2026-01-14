using Azure.Identity;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VideoTranslation.Api.Agents;
using VideoTranslation.Api.Services;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Get configuration values
var speechEndpoint = Environment.GetEnvironmentVariable("SPEECH_ENDPOINT") 
    ?? "https://eastus2.api.cognitive.microsoft.com";
var speechApiKey = Environment.GetEnvironmentVariable("SPEECH_API_KEY"); // Can be null for AAD auth
var speechRegion = Environment.GetEnvironmentVariable("SPEECH_REGION") ?? "eastus2";

// Get storage account name from identity-based settings or fall back to connection string for local dev
var storageAccountName = Environment.GetEnvironmentVariable("AzureWebJobsStorage__accountName") ?? "storageama3";
var storageConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");

// Configure Speech Translation Options
builder.Services.Configure<SpeechTranslationOptions>(options =>
{
    options.Endpoint = speechEndpoint;
    options.Region = speechRegion;
    options.SubscriptionKey = speechApiKey; // Null = use Azure AD auth
});

// Configure Blob Storage Options
builder.Services.Configure<BlobStorageOptions>(options =>
{
    options.AccountName = storageAccountName;
    options.ConnectionString = storageConnectionString; // Will be null in Azure (identity-based)
    options.VideosContainer = "videos";
    options.OutputsContainer = "outputs";
    options.SubtitlesContainer = "subtitles";
});

// Register HttpClient for Speech Translation Service
builder.Services.AddHttpClient<ISpeechTranslationService, SpeechTranslationService>();

// Register Blob Storage Service with proper DI
builder.Services.AddSingleton<IBlobStorageService>(sp =>
{
    var options = sp.GetRequiredService<IOptions<BlobStorageOptions>>();
    var logger = sp.GetRequiredService<ILogger<BlobStorageService>>();
    return new BlobStorageService(options, logger);
});

// Add Azure clients (for DefaultAzureCredential if needed)
builder.Services.AddAzureClients(clientBuilder =>
{
    clientBuilder.UseCredential(new DefaultAzureCredential());
});

// Register Agent Configuration Service for multi-agent orchestration
// Uses Azure AI Foundry with GPT-4o-mini
builder.Services.AddSingleton<IAgentConfiguration, AgentConfigurationService>();

// Register VTT Parsing Service for subtitle analysis
builder.Services.AddHttpClient<IVttParsingService, VttParsingService>();

// Register Subtitle Validation Agent (legacy direct chat approach)
builder.Services.AddScoped<ISubtitleValidationAgent, SubtitleValidationAgent>();

// Configure Foundry Agent Service Options
builder.Services.Configure<FoundryAgentOptions>(options =>
{
    options.ProjectEndpoint = Environment.GetEnvironmentVariable("FOUNDRY_PROJECT_ENDPOINT");
    options.ModelDeploymentName = Environment.GetEnvironmentVariable("FOUNDRY_MODEL_DEPLOYMENT") ?? "gpt-4o-mini";
    options.AgentName = Environment.GetEnvironmentVariable("FOUNDRY_AGENT_NAME") ?? "SubtitleValidationAgent";
});

// Register Foundry Agent Tool Handler (provides tools for the agent)
builder.Services.AddScoped<FoundryToolHandler>();

// Register Foundry Agent Service (uses Azure AI Foundry Persistent Agents SDK)
// Only register if the endpoint is configured
var foundryEndpoint = Environment.GetEnvironmentVariable("FOUNDRY_PROJECT_ENDPOINT");
if (!string.IsNullOrEmpty(foundryEndpoint))
{
    builder.Services.AddScoped<IFoundryAgentService, FoundryAgentService>();
}

// Add Application Insights
builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Build().Run();
