// ============================================================================
// AI Foundry (Azure AI Services) Module
// ============================================================================
// Azure AI Services account for AI capabilities including GPT-4o-mini
// deployment for multi-agent subtitle validation
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

// ============================================================================
// AI SERVICES ACCOUNT
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

@description('Name of the GPT-4o-mini deployment')
output gpt4oMiniDeploymentName string = gpt4oMiniDeployment.name

@description('GPT-4o-mini deployment ID')
output gpt4oMiniDeploymentId string = gpt4oMiniDeployment.id
