# DevOps Process Documentation

This document describes the CI/CD pipeline setup for the Video Translation Service.

## Overview

The project uses GitHub Actions for continuous integration and deployment with three workflows:

| Workflow | Trigger | Purpose |
|----------|---------|---------|
| `ci.yml` | Push to main, PRs to main | Build validation and testing |
| `cd-infra.yml` | Manual (workflow_dispatch) | Deploy Azure infrastructure |
| `cd-app.yml` | Manual (workflow_dispatch) | Deploy application code |

## Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                        GitHub Repository                             │
├─────────────────────────────────────────────────────────────────────┤
│  Push/PR to main                                                     │
│       │                                                              │
│       ▼                                                              │
│  ┌─────────────────────────────────────────────────────────────┐    │
│  │                    CI Workflow                                │    │
│  │  ┌──────────────┐ ┌──────────────┐ ┌──────────────────┐     │    │
│  │  │ Validate     │ │ Build & Test │ │ Build & Test     │     │    │
│  │  │ Bicep        │ │ API          │ │ UI               │     │    │
│  │  └──────────────┘ └──────────────┘ └──────────────────┘     │    │
│  └─────────────────────────────────────────────────────────────┘    │
│                                                                      │
│  Manual Trigger (workflow_dispatch)                                  │
│       │                                                              │
│       ▼                                                              │
│  ┌─────────────────────────────────────────────────────────────┐    │
│  │               CD Infrastructure Workflow                      │    │
│  │  ┌──────────────────────────────────────────────────────┐   │    │
│  │  │ Deploy Bicep templates to Azure                       │   │    │
│  │  │ - Creates Resource Group                              │   │    │
│  │  │ - Deploys all Azure resources                         │   │    │
│  │  └──────────────────────────────────────────────────────┘   │    │
│  └─────────────────────────────────────────────────────────────┘    │
│                                                                      │
│  Manual Trigger (workflow_dispatch)                                  │
│       │                                                              │
│       ▼                                                              │
│  ┌─────────────────────────────────────────────────────────────┐    │
│  │                 CD Application Workflow                       │    │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐  │    │
│  │  │ (Optional)  │  │ Build API   │  │ Build UI            │  │    │
│  │  │ Run Infra   │  │ .NET 8      │  │ .NET 9 Blazor       │  │    │
│  │  └──────┬──────┘  └──────┬──────┘  └──────────┬──────────┘  │    │
│  │         │                │                     │             │    │
│  │         ▼                ▼                     ▼             │    │
│  │  ┌─────────────────────────────────────────────────────┐    │    │
│  │  │ Deploy to Azure                                      │    │    │
│  │  │ - API → Function App                                 │    │    │
│  │  │ - UI → Static Web App                                │    │    │
│  │  └─────────────────────────────────────────────────────┘    │    │
│  └─────────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────────┐
│                            Azure                                     │
│  ┌─────────────────────────────────────────────────────────────┐    │
│  │           Resource Group: AMAFY26-deployment-{N}             │    │
│  │                                                              │    │
│  │  ┌────────────────┐  ┌────────────────┐  ┌──────────────┐   │    │
│  │  │ FuncApp-AMA-N  │  │ SWA-AMA-N      │  │ Speech-AMA-N │   │    │
│  │  │ (API)          │  │ (UI)           │  │              │   │    │
│  │  └────────────────┘  └────────────────┘  └──────────────┘   │    │
│  │                                                              │    │
│  │  ┌────────────────┐  ┌────────────────┐  ┌──────────────┐   │    │
│  │  │ storageamaN    │  │ KeyVault-AMA-N │  │ AppInsights  │   │    │
│  │  └────────────────┘  └────────────────┘  └──────────────┘   │    │
│  └─────────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────────┘
```

## Prerequisites

### 1. Azure Subscription

You need an Azure subscription with permissions to:
- Create resource groups
- Create and manage Azure resources (Functions, Storage, Key Vault, etc.)

### 2. Create Azure Service Principal

The GitHub Actions workflows need credentials to deploy to Azure. Create a service principal with the following steps:

```bash
# Login to Azure CLI
az login

# Set your subscription
az account set --subscription "<YOUR_SUBSCRIPTION_ID>"

# Create a service principal with Contributor role
az ad sp create-for-rbac \
  --name "github-actions-video-translation" \
  --role Contributor \
  --scopes /subscriptions/<YOUR_SUBSCRIPTION_ID> \
  --sdk-auth
```

This command outputs a JSON object like:

```json
{
  "clientId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "clientSecret": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "subscriptionId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "tenantId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "activeDirectoryEndpointUrl": "https://login.microsoftonline.com",
  "resourceManagerEndpointUrl": "https://management.azure.com/",
  ...
}
```

**Save this entire JSON output** - you'll need it for the `AZURE_CREDENTIALS` secret.

### 3. Configure GitHub Secrets

Navigate to your GitHub repository → Settings → Secrets and variables → Actions → New repository secret.

Create the following secrets:

| Secret Name | Value | Description |
|-------------|-------|-------------|
| `AZURE_CREDENTIALS` | The entire JSON output from `az ad sp create-for-rbac` | Service principal credentials |
| `AZURE_SUBSCRIPTION_ID` | Your Azure subscription ID (GUID) | Used for deployment scope |

### 4. Verify Service Principal Permissions

Ensure the service principal has sufficient permissions:

```bash
# Verify the service principal can access the subscription
az login --service-principal \
  -u <clientId> \
  -p <clientSecret> \
  --tenant <tenantId>

# Test creating a resource group
az group create --name test-permissions --location eastus2
az group delete --name test-permissions --yes
```

## Workflows

### CI Workflow (`ci.yml`)

**Trigger:** Automatically on push to `main` and pull requests to `main`

**Jobs:**
1. **validate-infra** - Validates all Bicep files syntax
2. **build-api** - Builds .NET 8 API and runs unit tests
3. **build-ui** - Builds .NET 9 Blazor UI and runs unit tests
4. **ci-summary** - Reports overall CI status

**What it validates:**
- Bicep template syntax (main.bicep and all modules)
- API compilation and unit tests
- UI compilation and unit tests

### CD Infrastructure Workflow (`cd-infra.yml`)

**Trigger:** Manual only (workflow_dispatch)

**Inputs:**
| Input | Required | Description |
|-------|----------|-------------|
| `deployment_number` | Yes | Number 1-999, determines resource names |
| `location` | No | Azure region (default: eastus2) |

**What it deploys:**
- Resource Group: `AMAFY26-deployment-{N}`
- Function App: `FuncApp-AMA-{N}`
- Static Web App: `SWA-AMA-{N}`
- Storage Account: `storageama{N}`
- Key Vault: `KeyVault-AMA-{N}`
- Speech Service: `Speech-AMA-{N}`
- Application Insights: `AppInsights-AMA-{N}`
- Log Analytics: `LogAnalytics-AMA-{N}`

### CD Application Workflow (`cd-app.yml`)

**Trigger:** Manual only (workflow_dispatch)

**Inputs:**
| Input | Required | Default | Description |
|-------|----------|---------|-------------|
| `deployment_number` | Yes | - | Target deployment number |
| `run_infrastructure` | No | false | Deploy infrastructure first |
| `location` | No | eastus2 | Azure region (if deploying infra) |

**What it deploys:**
- API code to Azure Function App
- UI code to Azure Static Web App

**Optional Infrastructure:** If `run_infrastructure` is true, it first runs the infrastructure deployment, then deploys the application.

## Usage Examples

### Scenario 1: New Deployment (Full Stack)

For a completely new environment:

1. Go to Actions → "CD - Deploy Application"
2. Click "Run workflow"
3. Enter:
   - `deployment_number`: `4` (new number)
   - `run_infrastructure`: ✅ checked
   - `location`: `eastus2`
4. Click "Run workflow"

This will:
1. Create all Azure infrastructure
2. Build and deploy API
3. Build and deploy UI

### Scenario 2: Code Fix (Redeploy to Existing)

For deploying code changes to an existing environment:

1. Go to Actions → "CD - Deploy Application"
2. Click "Run workflow"
3. Enter:
   - `deployment_number`: `3` (existing number)
   - `run_infrastructure`: ❌ unchecked
4. Click "Run workflow"

This will only redeploy the application code, skipping infrastructure.

### Scenario 3: Infrastructure Only

To update or recreate infrastructure without redeploying code:

1. Go to Actions → "CD - Deploy Infrastructure"
2. Click "Run workflow"
3. Enter:
   - `deployment_number`: `3`
   - `location`: `eastus2`
4. Click "Run workflow"

## Resource Naming Convention

All resources follow the pattern: `{ResourceType}-AMA-{DeploymentNumber}`

| Resource | Naming Pattern | Example |
|----------|---------------|---------|
| Resource Group | `AMAFY26-deployment-{N}` | `AMAFY26-deployment-3` |
| Function App | `FuncApp-AMA-{N}` | `FuncApp-AMA-3` |
| Static Web App | `SWA-AMA-{N}` | `SWA-AMA-3` |
| Storage Account | `storageama{N}` | `storageama3` |
| Key Vault | `KeyVault-AMA-{N}` | `KeyVault-AMA-3` |
| Speech Service | `Speech-AMA-{N}` | `Speech-AMA-3` |
| App Insights | `AppInsights-AMA-{N}` | `AppInsights-AMA-3` |

## Unit Tests

### API Tests (`tests/unit/VideoTranslation.Api.Tests`)

- `ValidateInputActivityTests` - Tests input validation logic
- `TranslationJobTests` - Tests model properties and defaults

Run locally:
```bash
cd tests/unit/VideoTranslation.Api.Tests
dotnet test
```

### UI Tests (`tests/unit/VideoTranslation.UI.Tests`)

- `JobModelsTests` - Tests UI model properties and defaults

Run locally:
```bash
cd tests/unit/VideoTranslation.UI.Tests
dotnet test
```

## Troubleshooting

### Common Issues

#### 1. "Login failed" in CD workflows

**Cause:** Invalid or expired Azure credentials

**Solution:**
- Regenerate the service principal: `az ad sp create-for-rbac ...`
- Update the `AZURE_CREDENTIALS` secret in GitHub

#### 2. "Deployment failed - resource already exists"

**Cause:** Trying to create resources that already exist

**Solution:** Bicep deployments are idempotent - this usually resolves itself. If persists, manually delete the conflicting resource.

#### 3. "SWA deployment token retrieval failed"

**Cause:** Service principal doesn't have access to the Static Web App

**Solution:** Ensure the service principal has Contributor role on the subscription or resource group.

#### 4. "Bicep validation failed"

**Cause:** Syntax error in Bicep files

**Solution:** Run locally to see detailed errors:
```bash
az bicep build --file infra/main.bicep
```

### Viewing Logs

- **GitHub Actions:** Actions tab → Select workflow run → Click on job → View step logs
- **Azure Deployment:** Azure Portal → Resource Group → Deployments → Select deployment

## Security Considerations

1. **Service Principal Scope:** The service principal is scoped to the entire subscription. For production, consider scoping to specific resource groups.

2. **Secret Rotation:** Rotate the service principal credentials periodically:
   ```bash
   az ad sp credential reset --name "github-actions-video-translation"
   ```

3. **Branch Protection:** Enable branch protection rules for `main` to require CI to pass before merging.

4. **IP Whitelisting:** The application restricts access to authorized IP addresses:
   - **Static Web App:** Configured in `src/ui/wwwroot/staticwebapp.config.json` via `networking.allowedIpRanges`
   - **Function App:** Configured via Bicep `allowedIpAddresses` parameter in `infra/main.parameters.json`
   - When deploying, ensure your IP is in the whitelist or you won't be able to access the application

5. **Updating Allowed IPs:** To add/remove IP addresses:
   ```bash
   # For Function App (immediate effect)
   az functionapp config access-restriction add \
     --resource-group "AMAFY26-deployment-3" \
     --name "FuncApp-AMA-3" \
     --rule-name "AllowNewIP" \
     --action Allow \
     --ip-address "NEW.IP.ADDRESS/32" \
     --priority 101
   
   # For Static Web App (requires redeployment)
   # Edit staticwebapp.config.json and redeploy
   ```

## Future Improvements

- [ ] Add integration tests with Azure resources
- [ ] Add staging environment support
- [ ] Implement approval gates for production deployments
- [ ] Add Slack/Teams notifications for deployment status
- [ ] Add performance/load tests in CI
- [ ] Implement multi-agent subtitle validation
