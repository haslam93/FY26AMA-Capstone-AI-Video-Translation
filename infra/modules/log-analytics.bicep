// ============================================================================
// Log Analytics Workspace Module
// ============================================================================
// Centralized logging and monitoring for all Azure resources
// ============================================================================

@description('Name of the Log Analytics workspace')
param name string

@description('Azure region for the resource')
param location string

@description('Tags to apply to the resource')
param tags object

@description('Retention period in days (30-730)')
@minValue(30)
@maxValue(730)
param retentionInDays int = 30

@description('SKU for the workspace')
@allowed([
  'PerGB2018'
  'Free'
  'Standalone'
  'PerNode'
])
param sku string = 'PerGB2018'

// ============================================================================
// RESOURCE
// ============================================================================

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: name
  location: location
  tags: tags
  properties: {
    sku: {
      name: sku
    }
    retentionInDays: retentionInDays
    features: {
      enableLogAccessUsingOnlyResourcePermissions: true
    }
    workspaceCapping: {
      dailyQuotaGb: -1 // No cap
    }
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

// ============================================================================
// OUTPUTS
// ============================================================================

@description('Resource ID of the Log Analytics workspace')
output id string = logAnalyticsWorkspace.id

@description('Name of the Log Analytics workspace')
output name string = logAnalyticsWorkspace.name

@description('Workspace ID (GUID) for the Log Analytics workspace')
output workspaceId string = logAnalyticsWorkspace.properties.customerId
