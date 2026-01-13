// ============================================================================
// Static Web App Module - Blazor WebAssembly Frontend
// ============================================================================
// Hosts the Blazor WebAssembly frontend for the video translation service.
// Configured with CORS to allow communication with the Function App API.
// ============================================================================

@description('Name of the Static Web App')
param name string

@description('Azure region for the resource')
param location string

@description('Tags to apply to the resource')
param tags object

@description('The Function App hostname for CORS and API backend linking')
param functionAppHostname string = ''

@description('SKU for the Static Web App')
@allowed(['Free', 'Standard'])
param sku string = 'Standard'

@description('IP addresses allowed to access the Static Web App (configured via staticwebapp.config.json networking.allowedIpRanges)')
param allowedIpAddresses array = []

// Note: IP restrictions for Static Web Apps are configured in staticwebapp.config.json
// The allowedIpAddresses parameter is for documentation and consistency with Function App

// ============================================================================
// STATIC WEB APP
// ============================================================================

resource staticWebApp 'Microsoft.Web/staticSites@2023-12-01' = {
  name: name
  location: location
  tags: tags
  sku: {
    name: sku
    tier: sku
  }
  properties: {
    // For manual deployment (not GitHub Actions), we leave these empty
    repositoryUrl: ''
    branch: ''
    buildProperties: {
      appLocation: '/src/ui'
      apiLocation: ''
      outputLocation: 'wwwroot'
      appBuildCommand: 'dotnet publish -c Release -o bin/publish'
    }
    stagingEnvironmentPolicy: 'Enabled'
    allowConfigFileUpdates: true
  }
}

// ============================================================================
// STATIC WEB APP CONFIGURATION
// ============================================================================

// Link to the Function App backend (if hostname provided)
resource backendLink 'Microsoft.Web/staticSites/linkedBackends@2023-12-01' = if (!empty(functionAppHostname)) {
  parent: staticWebApp
  name: 'api-backend'
  properties: {
    backendResourceId: resourceId('Microsoft.Web/sites', split(functionAppHostname, '.')[0])
    region: location
  }
}

// ============================================================================
// OUTPUTS
// ============================================================================

@description('The name of the Static Web App')
output name string = staticWebApp.name

@description('The default hostname of the Static Web App')
output defaultHostname string = staticWebApp.properties.defaultHostname

@description('The URL of the Static Web App')
output url string = 'https://${staticWebApp.properties.defaultHostname}'

@description('The resource ID of the Static Web App')
output id string = staticWebApp.id

@description('The deployment token for the Static Web App (use for CLI deployment)')
output deploymentTokenSecretName string = '${name}-deployment-token'
