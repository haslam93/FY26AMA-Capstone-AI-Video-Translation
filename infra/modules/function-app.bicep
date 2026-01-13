// ============================================================================
// Function App Module
// ============================================================================
// Azure Function App for Durable Functions orchestration
// ============================================================================

@description('Name of the Function App')
param functionAppName string

@description('Name of the App Service Plan')
param appServicePlanName string

@description('Azure region for the resource')
param location string

@description('Tags to apply to the resource')
param tags object

@description('Name of the storage account for Function App')
param storageAccountName string

@description('Application Insights instrumentation key')
param appInsightsInstrumentationKey string

@description('Application Insights connection string')
param appInsightsConnectionString string

@description('Function runtime')
@allowed([
  'dotnet'
  'dotnet-isolated'
  'node'
  'python'
  'java'
])
param functionRuntime string = 'dotnet-isolated'

@description('Function runtime version')
param functionRuntimeVersion string = '~4'

@description('.NET version')
param dotnetVersion string = 'v8.0'

@description('App Service Plan SKU')
param appServicePlanSku object = {
  name: 'S1'
  tier: 'Standard'
}

@description('CORS allowed origins')
param corsAllowedOrigins array = [
  'https://portal.azure.com'
]

@description('AI Foundry project endpoint for multi-agent orchestration')
param aiFoundryProjectEndpoint string = ''

@description('AI Foundry OpenAI endpoint')
param aiFoundryOpenAIEndpoint string = ''

@description('AI Foundry GPT-4o-mini deployment name')
param aiFoundryModelDeploymentName string = 'gpt-4o-mini'

// ============================================================================
// APP SERVICE PLAN
// ============================================================================

resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: appServicePlanName
  location: location
  tags: tags
  sku: appServicePlanSku
  properties: {
    reserved: false // false for Windows, true for Linux
  }
}

// ============================================================================
// FUNCTION APP
// ============================================================================

resource functionApp 'Microsoft.Web/sites@2023-12-01' = {
  name: functionAppName
  location: location
  tags: union(tags, {
    'azd-service-name': 'orchestrator'
  })
  kind: 'functionapp'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    publicNetworkAccess: 'Enabled'
    siteConfig: {
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      http20Enabled: true
      netFrameworkVersion: dotnetVersion
      use32BitWorkerProcess: false
      cors: {
        allowedOrigins: corsAllowedOrigins
        supportCredentials: false
      }
      appSettings: [
        // Identity-based connection for AzureWebJobsStorage (no keys required)
        {
          name: 'AzureWebJobsStorage__accountName'
          value: storageAccountName
        }
        {
          name: 'AzureWebJobsStorage__blobServiceUri'
          value: 'https://${storageAccountName}.blob.${environment().suffixes.storage}'
        }
        {
          name: 'AzureWebJobsStorage__queueServiceUri'
          value: 'https://${storageAccountName}.queue.${environment().suffixes.storage}'
        }
        {
          name: 'AzureWebJobsStorage__tableServiceUri'
          value: 'https://${storageAccountName}.table.${environment().suffixes.storage}'
        }
        // Note: WEBSITE_CONTENTAZUREFILECONNECTIONSTRING and WEBSITE_CONTENTSHARE are not needed
        // for dedicated plans (B1, S1, P1v2, etc.) - only for Consumption (Y1) and Elastic Premium (EP)
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: functionRuntimeVersion
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: functionRuntime
        }
        {
          name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
          value: appInsightsInstrumentationKey
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsightsConnectionString
        }
        {
          name: 'WEBSITE_RUN_FROM_PACKAGE'
          value: '1'
        }
        // AI Foundry settings for multi-agent architecture
        {
          name: 'AIFoundry__ProjectEndpoint'
          value: aiFoundryProjectEndpoint
        }
        {
          name: 'AIFoundry__OpenAIEndpoint'
          value: aiFoundryOpenAIEndpoint
        }
        {
          name: 'AIFoundry__ModelDeploymentName'
          value: aiFoundryModelDeploymentName
        }
      ]
    }
  }
}

// ============================================================================
// OUTPUTS
// ============================================================================

@description('Resource ID of the Function App')
output id string = functionApp.id

@description('Name of the Function App')
output name string = functionApp.name

@description('Default hostname of the Function App')
output defaultHostname string = functionApp.properties.defaultHostName

@description('Principal ID of the system-assigned managed identity')
output principalId string = functionApp.identity.principalId

@description('Resource ID of the App Service Plan')
output appServicePlanId string = appServicePlan.id
