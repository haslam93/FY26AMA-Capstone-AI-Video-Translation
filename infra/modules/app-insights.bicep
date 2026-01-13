// ============================================================================
// Application Insights Module
// ============================================================================
// Application Performance Monitoring for the Function App
// ============================================================================

@description('Name of the Application Insights resource')
param name string

@description('Azure region for the resource')
param location string

@description('Tags to apply to the resource')
param tags object

@description('Resource ID of the Log Analytics workspace')
param logAnalyticsWorkspaceId string

@description('Application type')
@allowed([
  'web'
  'other'
])
param applicationType string = 'web'

// ============================================================================
// RESOURCE
// ============================================================================

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: name
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: applicationType
    WorkspaceResourceId: logAnalyticsWorkspaceId
    IngestionMode: 'LogAnalytics'
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
    RetentionInDays: 90
    DisableIpMasking: false
    DisableLocalAuth: false
    ForceCustomerStorageForProfiler: false
  }
}

// ============================================================================
// OUTPUTS
// ============================================================================

@description('Resource ID of the Application Insights resource')
output id string = appInsights.id

@description('Name of the Application Insights resource')
output name string = appInsights.name

@description('Instrumentation key for Application Insights')
output instrumentationKey string = appInsights.properties.InstrumentationKey

@description('Connection string for Application Insights')
output connectionString string = appInsights.properties.ConnectionString
