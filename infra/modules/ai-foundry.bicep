// ============================================================================
// AI Foundry (Azure AI Services) Module
// ============================================================================
// Azure AI Services account with Foundry Project for multi-agent architecture
// Includes GPT-4o-mini deployment for subtitle validation agents
// ============================================================================

@description('Name of the AI Foundry account')
param accountName string

@description('Azure region for the resource')
param location string

@description('Tags to apply to the resource')
param tags object

@description('SKU for AI Services')
@allowed([
  'S0'
])
param sku string = 'S0'

@description('Name of the GPT-4o-mini deployment')
param gpt4oMiniDeploymentName string = 'gpt-4o-mini'

@description('Capacity (TPM in thousands) for GPT-4o-mini deployment')
@minValue(1)
@maxValue(120)
param gpt4oMiniCapacity int = 10

@description('Name of the Foundry Project for multi-agent orchestration')
param projectName string = 'video-translation-agents'

// ============================================================================
// AI SERVICES ACCOUNT (Hub)
// ============================================================================

resource aiServices 'Microsoft.CognitiveServices/accounts@2024-10-01' = {
  name: accountName
  location: location
  tags: tags
  kind: 'AIServices'
  sku: {
    name: sku
  }
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    customSubDomainName: toLower(accountName)
    publicNetworkAccess: 'Enabled'
    networkAcls: {
      defaultAction: 'Allow'
    }
    disableLocalAuth: false
    allowProjectManagement: true  // Required for Foundry Project creation
  }
}

// ============================================================================
// AI FOUNDRY PROJECT
// ============================================================================
// Project for multi-agent orchestration with supervisor, validation, and
// human-in-the-loop agents. Uses system-assigned managed identity for
// secure access to AI Services resources.
// ============================================================================

resource foundryProject 'Microsoft.CognitiveServices/accounts/projects@2025-04-01-preview' = {
  parent: aiServices
  name: projectName
  location: location
  tags: tags
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    displayName: 'Video Translation Multi-Agent Project'
    description: 'Foundry project for multi-agent video translation with supervisor, subtitle validation, and human-in-the-loop agents'
  }
}

// ============================================================================
// GPT-4o-mini DEPLOYMENT
// ============================================================================
// Deploying GPT-4o-mini for multi-agent subtitle validation
// Cost-effective model with good quality for text analysis tasks
// ============================================================================

resource gpt4oMiniDeployment 'Microsoft.CognitiveServices/accounts/deployments@2024-10-01' = {
  parent: aiServices
  name: gpt4oMiniDeploymentName
  tags: tags
  sku: {
    name: 'Standard'
    capacity: gpt4oMiniCapacity
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: 'gpt-4o-mini'
      version: '2024-07-18'
    }
    versionUpgradeOption: 'OnceNewDefaultVersionAvailable'
  }
}

// ============================================================================
// OUTPUTS
// ============================================================================

@description('Resource ID of the AI Services account')
output id string = aiServices.id

@description('Name of the AI Services account')
output accountName string = aiServices.name

@description('Endpoint URL for the AI Services')
output endpoint string = aiServices.properties.endpoint

@description('Principal ID of the AI Services system-assigned managed identity')
output aiServicesPrincipalId string = aiServices.identity.principalId

@description('Name of the Foundry Project')
output projectName string = foundryProject.name

@description('Resource ID of the Foundry Project')
output projectId string = foundryProject.id

@description('Principal ID of the Foundry Project managed identity')
output projectPrincipalId string = foundryProject.identity.principalId

@description('Foundry Project endpoint (for agent SDK)')
output projectEndpoint string = 'https://${toLower(accountName)}.cognitiveservices.azure.com/agents/v1.0/projects/${projectName}'

@description('Name of the GPT-4o-mini deployment')
output gpt4oMiniDeploymentName string = gpt4oMiniDeployment.name

@description('GPT-4o-mini deployment ID')
output gpt4oMiniDeploymentId string = gpt4oMiniDeployment.id
