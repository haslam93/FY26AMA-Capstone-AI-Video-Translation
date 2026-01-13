// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure.AI.Projects;
using OpenAI.Chat;

namespace VideoTranslation.Api.Agents;

/// <summary>
/// Interface for agent configuration and Azure AI Foundry connection management.
/// Provides access to AI clients for multi-agent orchestration.
/// </summary>
public interface IAgentConfiguration
{
    /// <summary>
    /// Gets the Azure AI Projects client for agent operations.
    /// </summary>
    AIProjectClient ProjectClient { get; }

    /// <summary>
    /// Gets the chat client for GPT-4o-mini model interactions.
    /// </summary>
    ChatClient ChatClient { get; }

    /// <summary>
    /// Gets the GPT-4o-mini deployment name.
    /// </summary>
    string ModelDeploymentName { get; }

    /// <summary>
    /// Gets the Azure AI Foundry project endpoint.
    /// </summary>
    string ProjectEndpoint { get; }
}
