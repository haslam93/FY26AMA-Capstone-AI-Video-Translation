// ============================================================================
// Speech Services Module
// ============================================================================
// Azure Cognitive Services - Speech for video translation
// ============================================================================

@description('Name of the Speech Services resource')
param name string

@description('Azure region for the resource')
param location string

@description('Tags to apply to the resource')
param tags object

@description('Resource ID of the Log Analytics workspace for diagnostics')
param logAnalyticsWorkspaceId string

@description('SKU for Speech Services (S0 = Standard)')
@allowed([
  'F0' // Free
  'S0' // Standard
])
param sku string = 'S0'

// ============================================================================
// RESOURCE
// ============================================================================

resource speechServices 'Microsoft.CognitiveServices/accounts@2024-10-01' = {
  name: name
  location: location
  tags: tags
  kind: 'SpeechServices'
  sku: {
    name: sku
  }
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    customSubDomainName: toLower(name)
    publicNetworkAccess: 'Enabled'
    networkAcls: {
      defaultAction: 'Allow'
    }
    disableLocalAuth: false // Allow key-based auth as fallback
  }
}

// Diagnostic settings for monitoring
resource diagnosticSettings 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: '${name}-diagnostics'
  scope: speechServices
  properties: {
    workspaceId: logAnalyticsWorkspaceId
    logs: [
      {
        categoryGroup: 'allLogs'
        enabled: true
        retentionPolicy: {
          enabled: false
          days: 0
        }
      }
    ]
    metrics: [
      {
        category: 'AllMetrics'
        enabled: true
        retentionPolicy: {
          enabled: false
          days: 0
        }
      }
    ]
  }
}

// ============================================================================
// OUTPUTS
// ============================================================================

@description('Resource ID of the Speech Services resource')
output id string = speechServices.id

@description('Name of the Speech Services resource')
output name string = speechServices.name

@description('Endpoint URL for the Speech Services')
output endpoint string = speechServices.properties.endpoint

@description('Principal ID of the system-assigned managed identity')
output principalId string = speechServices.identity.principalId
