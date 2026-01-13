// ============================================================================
// AI Foundry (Azure AI Services) Module
// ============================================================================
// Azure AI Services account for AI capabilities
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
