// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure.AI.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using System.ClientModel.Primitives;

namespace VideoTranslation.Api.Agents;

/// <summary>
/// Configures and manages connections to Azure AI Foundry for multi-agent orchestration.
/// Uses Managed Identity for authentication (no API keys required).
/// </summary>
public class AgentConfigurationService : IAgentConfiguration
{
    private readonly ILogger<AgentConfigurationService> _logger;
    private readonly AIProjectClient _projectClient;
    private readonly ChatClient _chatClient;
    private readonly string _modelDeploymentName;
    private readonly string _projectEndpoint;

    public AgentConfigurationService(
        IConfiguration configuration,
        ILogger<AgentConfigurationService> logger)
    {
        _logger = logger;

        // Get configuration from environment variables or app settings
        _projectEndpoint = configuration["AIFoundry:ProjectEndpoint"]
            ?? throw new InvalidOperationException("AIFoundry:ProjectEndpoint configuration is required");

        _modelDeploymentName = configuration["AIFoundry:ModelDeploymentName"]
            ?? "gpt-4o-mini";

        // Get the Azure OpenAI endpoint (the AI Services endpoint)
        var openAIEndpoint = configuration["AIFoundry:OpenAIEndpoint"]
            ?? ExtractOpenAIEndpoint(_projectEndpoint);

        _logger.LogInformation(
            "Initializing Agent Configuration Service with project endpoint: {Endpoint}, OpenAI endpoint: {OpenAIEndpoint}, model: {Model}",
            _projectEndpoint,
            openAIEndpoint,
            _modelDeploymentName);

        // Use DefaultAzureCredential for Managed Identity authentication
        // This works in Azure Functions with system-assigned managed identity
        var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            ExcludeEnvironmentCredential = false,
            ExcludeManagedIdentityCredential = false,
            ExcludeAzureCliCredential = false,
            ExcludeAzureDeveloperCliCredential = false,
            ExcludeWorkloadIdentityCredential = false,
            ExcludeVisualStudioCredential = true,
            ExcludeVisualStudioCodeCredential = true,
            ExcludeInteractiveBrowserCredential = true
        });

        // Initialize Azure AI Projects client
        _projectClient = new AIProjectClient(
            new Uri(_projectEndpoint),
            credential);

        _logger.LogInformation("Azure AI Projects client initialized successfully");

        // Initialize Azure OpenAI client for chat completions
        var azureOpenAIClient = new AzureOpenAIClient(
            new Uri(openAIEndpoint),
            credential);

        // Get ChatClient from Azure OpenAI client
        _chatClient = azureOpenAIClient.GetChatClient(_modelDeploymentName);

        _logger.LogInformation("Chat client initialized for model: {Model}", _modelDeploymentName);
    }

    /// <summary>
    /// Extracts the base Azure OpenAI endpoint from the project endpoint.
    /// Project endpoint: https://aiservices-ama-3.cognitiveservices.azure.com/api/projects/video-translation-agents
    /// OpenAI endpoint: https://aiservices-ama-3.openai.azure.com
    /// </summary>
    private static string ExtractOpenAIEndpoint(string projectEndpoint)
    {
        var uri = new Uri(projectEndpoint);
        var host = uri.Host;
        
        // Replace .cognitiveservices.azure.com with .openai.azure.com
        if (host.Contains(".cognitiveservices.azure.com"))
        {
            host = host.Replace(".cognitiveservices.azure.com", ".openai.azure.com");
        }
        else if (host.Contains(".services.ai.azure.com"))
        {
            // For AI Services unified endpoint, use the same host
            // The OpenAI endpoint is accessible at the same domain
        }

        return $"https://{host}";
    }

    /// <inheritdoc />
    public AIProjectClient ProjectClient => _projectClient;

    /// <inheritdoc />
    public ChatClient ChatClient => _chatClient;

    /// <inheritdoc />
    public string ModelDeploymentName => _modelDeploymentName;

    /// <inheritdoc />
    public string ProjectEndpoint => _projectEndpoint;
}
