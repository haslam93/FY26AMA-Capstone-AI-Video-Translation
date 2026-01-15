# Runbook

## Table of Contents
- [Infrastructure Overview](#infrastructure-overview)
- [Prerequisites](#prerequisites)
- [Deployment](#deployment)
- [Resource Naming Convention](#resource-naming-convention)
- [Monitoring](#monitoring)
- [Troubleshooting](#troubleshooting)
- [Incident Response](#incident-response)

---

## Infrastructure Overview

The Video Translation Service uses the following Azure resources, all deployed via Bicep Infrastructure as Code (IaC):

| Resource | Purpose | SKU |
|----------|---------|-----|
| **Resource Group** | Container for all resources | N/A |
| **Log Analytics Workspace** | Centralized logging and monitoring | PerGB2018 |
| **Application Insights** | Application performance monitoring | Web |
| **Storage Account** | Video files, outputs, subtitles, and Durable Functions state | Standard_LRS |
| **Key Vault** | Secrets management (RBAC enabled) | Standard |
| **Speech Services** | Video Translation API (with custom subdomain) | S0 (Standard) |
| **AI Services** | `AIServices-AMA-{N}` | S0 (Standard) |
| **Multi-Agent System** | 4 agents (Orchestrator, Translation, Technical, Cultural) | GPT-4o-mini |
| **Static Web App** | Blazor WebAssembly UI hosting | Free |
| **App Service Plan** | Hosting plan for Function App | S1 (Standard) |
| **Function App** | Durable Functions API backend | .NET 8 Isolated |

### Security Configuration

| Resource | Security Feature | Configuration |
|----------|-----------------|---------------|
| **Storage Account** | Managed Identity | Function App uses system-assigned identity |
| **Key Vault** | RBAC + Soft Delete | No access policies |
| **Function App** | HTTPS-only | TLS 1.2 minimum |
| **Static Web App** | HTTPS-only | Automatic SSL certificates |

### Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    AMAFY26-deployment-{N} Resource Group                │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ┌──────────────┐         ┌──────────────┐         ┌──────────────┐    │
│  │ Static Web   │  HTTP   │ Function App │  RBAC   │ App Insights │    │
│  │ App (Blazor) │────────►│  (Durable)   │────────►│              │    │
│  └──────────────┘         └──────┬───────┘         └──────┬───────┘    │
│                                  │                        │            │
│                       Managed Identity (RBAC)              ▼            │
│                                  │                  ┌──────────────┐    │
│         ┌──────────┬─────────┼──────────┬─────────┐  │ Log Analytics│    │
│         ▼          ▼          ▼          ▼          │  Workspace   │    │
│  ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐  └──────────────┘    │
│  │ Storage │ │Key Vault│ │ Speech  │ │   AI    │                       │
│  │ Account │ │  (RBAC) │ │Services │ │Services│                       │
│  └─────┬───┘ └─────────┘ └─────────┘ └─────────┘                       │
│        │                                                             │
│        ▼                                                             │
│  ┌──────────────────────────────────────┐                              │
│  │ Containers: videos | outputs | subtitles │                              │
│  └──────────────────────────────────────┘                              │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Prerequisites

### Required Tools

| Tool | Version | Installation |
|------|---------|--------------|
| Azure CLI | 2.50+ | [Install Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli) |
| Bicep CLI | 0.22+ | Included with Azure CLI or `az bicep install` |
| PowerShell | 7.0+ | [Install PowerShell](https://docs.microsoft.com/powershell/scripting/install/installing-powershell) |
| .NET SDK | 8.0+ | [Install .NET](https://dotnet.microsoft.com/download) |
| Azure Functions Core Tools | 4.x | `npm install -g azure-functions-core-tools@4` |
| Static Web Apps CLI | Latest | `npm install -g @azure/static-web-apps-cli` |

### Azure Permissions

You need the following permissions on the target Azure subscription:
- **Contributor** role on the subscription (to create resource groups)
- **User Access Administrator** role (to create RBAC role assignments)

Or use a custom role with:
- `Microsoft.Resources/subscriptions/resourceGroups/write`
- `Microsoft.Authorization/roleAssignments/write`
- Resource provider permissions for all services

### Login to Azure

```powershell
# Login to Azure
az login

# Set your subscription
az account set --subscription "<your-subscription-id>"

# Verify login
az account show
```

---

## Deployment

### Quick Start

```powershell
# Navigate to the infra directory
cd infra

# Deploy with deployment number 1
.\deploy.ps1 -DeploymentNumber 1

# Or preview changes first (What-If)
.\deploy.ps1 -DeploymentNumber 1 -WhatIf
```

### Deployment Parameters

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `deploymentNumber` | ✅ | - | Unique number (1-999) for resource naming |
| `location` | ❌ | `eastus2` | Azure region for all resources |
| `tags` | ❌ | See below | Tags applied to all resources |

Default tags:
```json
{
  "project": "VideoTranslation",
  "environment": "AMA-Capstone",
  "deploymentNumber": "{N}"
}
```

### Manual Deployment (Azure CLI)

```powershell
# Validate the template
az deployment sub validate `
    --location eastus2 `
    --template-file main.bicep `
    --parameters deploymentNumber=1

# What-If (preview changes)
az deployment sub what-if `
    --location eastus2 `
    --template-file main.bicep `
    --parameters deploymentNumber=1

# Deploy
az deployment sub create `
    --name "VideoTranslation-$(Get-Date -Format 'yyyyMMdd-HHmmss')" `
    --location eastus2 `
    --template-file main.bicep `
    --parameters deploymentNumber=1
```

### Using Parameters File

```powershell
az deployment sub create `
    --location eastus2 `
    --template-file main.bicep `
    --parameters @main.parameters.json
```

### Bash Deployment (Linux/macOS)

```bash
# Make script executable
chmod +x deploy.sh

# Deploy
./deploy.sh -n 1 -l eastus2

# What-If preview
./deploy.sh -n 1 --what-if
```

### Deployment Outputs

After successful deployment, you'll receive:

| Output | Description |
|--------|-------------|
| `resourceGroupName` | Name of the created resource group |
| `functionAppName` | Name of the Function App |
| `functionAppHostname` | Default hostname for the Function App |
| `staticWebAppName` | Name of the Static Web App |
| `staticWebAppUrl` | URL of the Static Web App (UI) |
| `staticWebAppHostname` | Hostname for the Static Web App |
| `speechServiceEndpoint` | Endpoint URL for Speech Services |
| `aiFoundryEndpoint` | Endpoint URL for AI Services |
| `keyVaultUri` | URI of the Key Vault |
| `storageAccountName` | Name of the Storage Account |
| `appInsightsName` | Name of Application Insights |
| `logAnalyticsWorkspaceId` | Resource ID of Log Analytics |

### Static Web App Deployment Token

The deployment script will output the Static Web App deployment token. Save this token for deploying the Blazor UI:

```powershell
# The token is output by deploy.ps1, or retrieve manually:
az staticwebapp secrets list --name SWA-AMA-{N} --resource-group AMAFY26-deployment-{N} --query "properties.apiKey" -o tsv
```

---

## Resource Naming Convention

All resources follow this naming pattern:

| Resource Type | Naming Pattern | Example |
|---------------|----------------|---------|
| Resource Group | `AMAFY26-deployment-{N}` | `AMAFY26-deployment-2` |
| Log Analytics | `LogAnalytics-AMA-{N}` | `LogAnalytics-AMA-2` |
| App Insights | `AppInsights-AMA-{N}` | `AppInsights-AMA-2` |
| Key Vault | `KeyVault-AMA-{N}` | `KeyVault-AMA-2` |
| Storage Account | `storageama{N}` | `storageama2` |
| Speech Services | `Speech-AMA-{N}` | `Speech-AMA-2` |
| AI Services | `AIServices-AMA-{N}` | `AIServices-AMA-2` |
| Static Web App | `SWA-AMA-{N}` | `SWA-AMA-2` |
| Function App | `FuncApp-AMA-{N}` | `FuncApp-AMA-2` |
| App Service Plan | `ASP-AMA-{N}` | `ASP-AMA-2` |

---

## Monitoring

### Application Insights

Access Application Insights in the Azure Portal:
1. Navigate to Resource Group `AMAFY26-deployment-{N}`
2. Open `AppInsights-AMA-{N}`
3. Use the following blades:
   - **Live Metrics**: Real-time performance
   - **Failures**: Error analysis
   - **Performance**: Request durations
   - **Logs**: Custom queries (KQL)

### Log Analytics Queries

```kusto
// Function App Errors (last 24 hours)
FunctionAppLogs
| where TimeGenerated > ago(24h)
| where Level == "Error"
| order by TimeGenerated desc

// Durable Functions Orchestration Status
traces
| where message contains "Orchestration"
| summarize count() by tostring(customDimensions.orchestrationStatus)

// Request Duration Percentiles
requests
| summarize 
    p50 = percentile(duration, 50),
    p90 = percentile(duration, 90),
    p99 = percentile(duration, 99)
    by bin(timestamp, 1h)
```

### Pre-Configured Alerts

| Alert | Condition | Severity |
|-------|-----------|----------|
| High Failure Rate | Failed requests > 10 in 15 min | 2 (Warning) |
| High Response Time | Avg duration > 5000ms in 15 min | 3 (Informational) |

---

## Troubleshooting

### Common Issues

#### 1. Deployment Fails - "AuthorizationFailed"

**Cause**: Insufficient permissions on the subscription.

**Solution**:
```powershell
# Check your role assignments
az role assignment list --assignee $(az ad signed-in-user show --query id -o tsv) --scope /subscriptions/<sub-id>

# Request Contributor + User Access Administrator roles
```

#### 2. Key Vault Access Denied

**Cause**: RBAC role assignment hasn't propagated yet.

#### 3. Reviews Page Returns 404

**Cause**: Route conflict between `/api/jobs/pending` and `/api/jobs/{jobId}`.

**Solution**: The pending endpoint was moved to `/api/reviews/pending`. Ensure you're using the latest deployed version:
```powershell
# Verify the endpoint is available
Invoke-RestMethod -Uri "https://funcapp-ama-{N}.azurewebsites.net/api/reviews/pending" -Method GET
```

#### 4. Jobs Stuck in PendingApproval

**Cause**: Orchestrator is waiting for human approval (WaitForExternalEvent).

**Solution**: 
- Approve/reject via the Reviews page in the UI
- Or use the API directly:
```powershell
# Approve a job
Invoke-RestMethod -Uri "https://funcapp-ama-{N}.azurewebsites.net/api/jobs/{jobId}/approve" -Method POST -ContentType "application/json" -Body '{"reviewedBy":"Admin","comments":"Looks good"}'

# Reject a job
Invoke-RestMethod -Uri "https://funcapp-ama-{N}.azurewebsites.net/api/jobs/{jobId}/reject" -Method POST -ContentType "application/json" -Body '{"reviewedBy":"Admin","reason":"Quality issues"}'
```
- Jobs will auto-reject after 3 days if no action is taken

#### 5. AI Validation Fails

**Cause**: GPT-4o-mini deployment not configured or AI Foundry endpoint unavailable.

**Solution**: Verify AI Foundry settings:
```powershell
# Check Function App settings
az functionapp config appsettings list --name FuncApp-AMA-{N} --resource-group AMAFY26-deployment-{N} --query "[?starts_with(name, 'FoundryAgent')]"

# Required settings:
# - FoundryAgent__Endpoint
# - FoundryAgent__ModelDeploymentName
# - FoundryAgent__ProjectName (optional)
```

#### 6. Multi-Agent Validation Returns Low Scores

**Cause**: Agents are correctly identifying quality issues.

**Solution**: 
- Review individual agent feedback in UI Job Details page
- Use agent chat to get more detailed feedback from specific agents
- Check if subtitles have formatting or timing issues
- Verify source video quality is adequate for translation

```powershell
# Test multi-agent validation endpoint directly
$body = @{sourceVtt = "..."; targetVtt = "..."; sourceLocale = "en-US"; targetLocale = "es-ES"} | ConvertTo-Json
Invoke-RestMethod -Uri "https://funcapp-ama-{N}.azurewebsites.net/api/jobs/{jobId}/validate" -Method POST -ContentType "application/json" -Body $body
```

#### 7. Agent Chat Not Working

**Cause**: Thread ID not persisted or agent service unavailable.

**Solution**:
```powershell
# Check agent chat history endpoint
Invoke-RestMethod -Uri "https://funcapp-ama-{N}.azurewebsites.net/api/jobs/{jobId}/chat?agentType=Translation" -Method GET
```

**Solution**: Wait 5-10 minutes for Azure RBAC propagation, or verify the role assignment:
```powershell
az role assignment list --scope /subscriptions/<sub-id>/resourceGroups/AMAFY26-deployment-{N}/providers/Microsoft.KeyVault/vaults/KeyVault-AMA-{N}
```

#### 3. Function App Cannot Access Storage

**Cause**: Missing RBAC role assignment for managed identity.

**Solution**: Verify role assignments:
```powershell
az role assignment list --assignee <function-app-principal-id> --scope /subscriptions/<sub-id>/resourceGroups/AMAFY26-deployment-{N}/providers/Microsoft.Storage/storageAccounts/storageama{N}
```

#### 4. Speech Services Quota Exceeded

**Cause**: S0 tier has rate limits.

**Solution**: 
- Check usage in Azure Portal → Speech Services → Metrics
- Request quota increase if needed
- Implement retry logic with exponential backoff

#### 5. AI Foundry Project Creation Fails

**Cause**: AI Foundry Hub not fully provisioned yet.

**Solution**: The Bicep template has dependencies configured correctly. If manual deployment, ensure Hub is created before Project.

### Viewing Deployment Logs

```powershell
# List recent deployments
az deployment sub list --query "[?starts_with(name, 'VideoTranslation')]" -o table

# View specific deployment details
az deployment sub show --name <deployment-name>

# View deployment operations (detailed)
az deployment operation sub list --name <deployment-name> -o table
```

---

## Incident Response

### Severity Levels

| Level | Description | Response Time | Examples |
|-------|-------------|---------------|----------|
| **P1** | Service down | 15 minutes | Function App not responding |
| **P2** | Major feature broken | 1 hour | Video translation failing |
| **P3** | Minor feature broken | 4 hours | Subtitles not generating |
| **P4** | Cosmetic/low impact | Next business day | Logging issues |

### Escalation Path

1. **First Response**: Check Application Insights Live Metrics
2. **Initial Triage**: Review recent deployments and changes
3. **Technical Investigation**: Analyze logs in Log Analytics
4. **Escalation**: Contact Azure Support if infrastructure issue

### Recovery Procedures

#### Redeploy Infrastructure
```powershell
# Redeploy with same deployment number (idempotent)
.\deploy.ps1 -DeploymentNumber 1
```

#### Rollback Function App
```powershell
# List deployment slots
az functionapp deployment slot list --name FuncApp-AMA-{N} --resource-group AMAFY26-deployment-{N}

# Swap to previous slot
az functionapp deployment slot swap --name FuncApp-AMA-{N} --resource-group AMAFY26-deployment-{N} --slot staging
```

#### Clear Durable Functions State
```powershell
# ⚠️ CAUTION: This will clear all orchestration history
az storage container delete --name azure-webjobs-durabletask --account-name storageama{N}
```

---

## Cleanup / Teardown

To delete all resources for a deployment:

```powershell
# Delete the entire resource group
az group delete --name AMAFY26-deployment-{N} --yes --no-wait

# Verify deletion
az group show --name AMAFY26-deployment-{N}
```

> ⚠️ **Warning**: This permanently deletes all resources including data in Storage and Key Vault. Ensure backups are taken if needed.
