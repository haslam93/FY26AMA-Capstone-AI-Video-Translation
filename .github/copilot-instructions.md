# Custom Instructions for GitHub Copilot

## Project Overview

This is a **Video Translation Service** built as a capstone project for Azure. The system translates video content from one language to another using Azure Speech Video Translation API, featuring:

- **Automatic video dubbing** with voice cloning (Personal Voice) or platform voices
- **Subtitle generation** in both source and target languages (WebVTT format)
- **Burned-in subtitles** option to embed translated subtitles directly in the video
- **Real-time job tracking** with status updates via a web dashboard
- **Durable Functions orchestration** for reliable long-running operations

## Technology Stack

| Component | Technology |
|-----------|------------|
| **Backend API** | Azure Durable Functions (.NET 8 Isolated Worker) |
| **Frontend UI** | Blazor WebAssembly (.NET 9) |
| **Translation API** | Azure Speech Video Translation API (version 2025-05-20) |
| **Storage** | Azure Blob Storage with Managed Identity |
| **Monitoring** | Application Insights + Log Analytics |
| **Infrastructure** | Azure Bicep (subscription-scoped) |
| **CI/CD** | GitHub Actions |
| **Hosting** | Azure Static Web App (UI) + Azure Function App (API) |

## Azure Resources (Deployment 3)

| Resource | Name | URL |
|----------|------|-----|
| Resource Group | AMAFY26-deployment-3 | - |
| Function App | FuncApp-AMA-3 | https://funcapp-ama-3.azurewebsites.net |
| Static Web App | SWA-AMA-3 | https://ashy-glacier-0400c0b0f.1.azurestaticapps.net |
| Speech Service | Speech-AMA-3 | https://speech-ama-3.cognitiveservices.azure.com |
| Storage Account | storageama3 | - |
| Application Insights | AppInsights-AMA-3 | - |
| Key Vault | KeyVault-AMA-3 | - |

---

## Directory Structure

```
Capstone/
├── src/
│   ├── Api/                        # Azure Durable Functions backend
│   │   ├── Activities/             # Durable activity functions
│   │   │   ├── CopyOutputsActivity.cs
│   │   │   ├── CreateIterationActivity.cs
│   │   │   ├── CreateTranslationActivity.cs
│   │   │   ├── GetIterationStatusActivity.cs
│   │   │   ├── RunValidationActivity.cs    # AI validation activity
│   │   │   └── ValidateInputActivity.cs
│   │   ├── Agents/                 # AI-powered agents
│   │   │   ├── SubtitleValidationAgent.cs  # GPT-4o-mini validation
│   │   │   ├── VttParsingService.cs        # WebVTT parser
│   │   │   └── AgentConfigurationService.cs
│   │   ├── Functions/              # HTTP trigger functions
│   │   │   ├── TranslationFunctions.cs
│   │   │   └── ValidationFunctions.cs
│   │   ├── Models/                 # Data models and DTOs
│   │   │   ├── TranslationJob.cs
│   │   │   ├── TranslationJobRequest.cs
│   │   │   ├── TranslationWorkflowState.cs # Validation results
│   │   │   └── SpeechApi/          # Speech API response models
│   │   ├── Orchestration/          # Durable orchestrator
│   │   │   └── VideoTranslationOrchestrator.cs
│   │   ├── Services/               # Business logic services
│   │   │   ├── BlobStorageService.cs
│   │   │   └── SpeechTranslationService.cs
│   │   ├── Program.cs              # DI configuration
│   │   └── local.settings.json     # Local dev settings
│   │
│   └── ui/                         # Blazor WebAssembly frontend
│       ├── Pages/                  # Razor pages
│       │   ├── Create.razor        # New job form
│       │   ├── Index.razor         # Job dashboard
│       │   ├── JobDetails.razor    # Job status/results/approve/reject
│       │   └── Reviews.razor       # Pending approvals dashboard
│       ├── Models/                 # Client-side models
│       │   └── JobModels.cs
│       ├── Services/               # API client services
│       │   └── TranslationService.cs
│       └── wwwroot/
│           └── staticwebapp.config.json  # SWA routing config
│
├── infra/                          # Bicep IaC templates
│   ├── main.bicep                  # Entry point (subscription scope)
│   ├── main.parameters.json        # Deployment parameters
│   ├── deploy.ps1                  # PowerShell deployment script
│   └── modules/                    # Resource modules
│       ├── function-app.bicep
│       ├── static-web-app.bicep
│       ├── storage-account.bicep
│       ├── speech-services.bicep
│       ├── keyvault.bicep
│       ├── app-insights.bicep
│       ├── log-analytics.bicep
│       └── role-assignments.bicep
│
├── tests/
│   ├── unit/                       # xUnit + bUnit tests
│   └── integration/                # Integration tests
│
├── docs/
│   ├── architecture.md             # System design
│   ├── runbook.md                  # Operations guide
│   ├── devops.md                   # CI/CD documentation
│   └── test-plan.md                # Testing strategy
│
├── .github/
│   ├── workflows/                  # CI/CD pipelines
│   │   ├── ci.yml                  # Build, test, validate
│   │   ├── cd-infra.yml            # Infrastructure deployment
│   │   └── cd-app.yml              # Application deployment
│   └── custom-instructions.md      # This file
│
├── progress.md                     # Project progress tracker
└── README.md                       # Project overview
```

---

## Key API Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/jobs` | GET | List all translation jobs |
| `/api/jobs` | POST | Create a new translation job |
| `/api/jobs/{jobId}` | GET | Get job status and results |
| `/api/jobs/{jobId}/iterate` | POST | Create a new iteration (re-translate) |
| `/api/jobs/{jobId}/validate` | POST | Run AI validation on subtitles |
| `/api/jobs/{jobId}/approve` | POST | Approve a translation job |
| `/api/jobs/{jobId}/reject` | POST | Reject a translation job |
| `/api/reviews/pending` | GET | List jobs pending approval |
| `/api/upload` | POST | Upload video file directly |

---

## How to Deploy to Azure

### Prerequisites
- Azure CLI installed and logged in (`az login`)
- .NET 8 SDK
- Azure Functions Core Tools v4
- Node.js 18+ (for SWA CLI)

### Step 1: Deploy Infrastructure (Bicep)

```powershell
# Set subscription
az account set --subscription "YOUR_SUBSCRIPTION_ID"

# Deploy infrastructure
az deployment sub create `
  --location eastus2 `
  --template-file infra/main.bicep `
  --parameters infra/main.parameters.json `
  --parameters deploymentNumber=3
```

### Step 2: Deploy Function App

```powershell
cd src/Api

# Build and publish
dotnet publish -c Release -o ./bin/publish

# Create zip package
Compress-Archive -Path ./bin/publish/* -DestinationPath ./bin/deploy.zip -Force

# Deploy to Azure
az functionapp deployment source config-zip `
  --resource-group "AMAFY26-deployment-3" `
  --name "FuncApp-AMA-3" `
  --src ./bin/deploy.zip `
  --build-remote true
```

### Step 3: Deploy Static Web App (UI)

```powershell
cd src/ui

# Build and publish
dotnet publish -c Release -o ./publish

# Get deployment token
$token = az staticwebapp secrets list `
  --name "SWA-AMA-3" `
  --resource-group "AMAFY26-deployment-3" `
  --query "properties.apiKey" -o tsv

# Deploy
swa deploy ./publish/wwwroot --deployment-token $token --env production
```

### Step 4: Configure Function App Settings

After initial deployment, ensure these settings are configured in the Function App:

```powershell
az functionapp config appsettings set `
  --name "FuncApp-AMA-3" `
  --resource-group "AMAFY26-deployment-3" `
  --settings `
    "SpeechServiceEndpoint=https://speech-ama-3.cognitiveservices.azure.com" `
    "SpeechServiceRegion=eastus2" `
    "BlobStorageEndpoint=https://storageama3.blob.core.windows.net"
```

---

## What Has Been Completed

### Phase 1-5: Core Application ✅
- Durable Functions orchestration with activity functions
- Azure Speech Video Translation API integration
- Blazor WebAssembly UI with job dashboard
- Azure Blob Storage for video/output management
- Managed Identity authentication

### Phase 6-7: DevOps & Testing ✅
- GitHub Actions CI/CD pipelines (ci.yml, cd-infra.yml, cd-app.yml)
- Bicep IaC templates for all Azure resources
- Unit tests with xUnit and bUnit
- Integration tests

### Phase 8: Subtitle Enhancements ✅
- WebVTT subtitle download links (source and target languages)
- "Burn subtitles into video" checkbox option
- "Max Characters Per Subtitle Line" configuration
- Fixed 409 Conflict errors with unique operation IDs

### Phase 9: Language Support ✅
- Expanded to 120+ source languages and 60+ target languages
- Fixed locale validation for all Azure Speech supported languages

### Phase 10: Troubleshooting & Stability ✅
- Fixed Storage Account authorization (enabled public network access)
- Added Storage Account Contributor role to Function App
- Removed IP restrictions (reverted for stability)

### Phase 11: Multi-Agent Architecture ✅
- GPT-4o-mini deployment to Azure AI Foundry
- SubtitleValidationAgent for AI-powered quality analysis
- 5-category scoring: Translation Accuracy, Grammar, Timing, Cultural Context, Formatting
- RunValidationActivity for automatic validation after translation
- Human-in-the-Loop approval gate with 3-day timeout
- Reviews.razor page for pending approvals dashboard
- Approve/reject buttons with reviewer info capture
- New job statuses: RunningValidation, PendingApproval, Approved, Rejected

---

## Common Issues & Fixes

### Function App 503 Error
**Cause**: Storage Account has `publicNetworkAccess: Disabled` or missing role assignments
**Fix**:
```powershell
az storage account update --name "storageama3" --resource-group "AMAFY26-deployment-3" --public-network-access Enabled

# Add role if needed
az role assignment create --assignee "<function-app-principal-id>" --role "Storage Account Contributor" --scope "<storage-account-resource-id>"

az functionapp restart --name "FuncApp-AMA-3" --resource-group "AMAFY26-deployment-3"
```

### Reviews Page 404 Error
**Cause**: Route conflict - `/api/jobs/pending` was matched by `/api/jobs/{jobId}`
**Fix**: Endpoint moved to `/api/reviews/pending`. Redeploy latest Function App.

### Jobs Stuck in PendingApproval
**Cause**: Orchestrator waiting for human approval via WaitForExternalEvent
**Fix**: Approve/reject via Reviews page or API. Jobs auto-reject after 3 days.

### SWA 403 Forbidden
**Cause**: IP restrictions in `staticwebapp.config.json` blocking access
**Fix**: Remove `networking.allowedIpRanges` from config and redeploy

### 409 Conflict Error
**Cause**: Duplicate operation IDs in Speech API calls
**Fix**: Ensure unique operation IDs by using truncated GUIDs with random suffixes

### CORS Errors
**Cause**: Function App CORS not configured for SWA domain
**Fix**: Add SWA hostname to `corsAllowedOrigins` in Bicep or Azure Portal

---

## Local Development

### Start Azurite (Local Storage Emulator)
```powershell
azurite --silent --location "$env:TEMP\azurite" --blobPort 10000 --queuePort 10001 --tablePort 10002
```

### Start Function App Locally
```powershell
cd src/Api
func start
```

### Start UI Locally
```powershell
cd src/ui
dotnet run
```

### Required local.settings.json for API
```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "SpeechServiceEndpoint": "https://speech-ama-3.cognitiveservices.azure.com",
    "SpeechServiceRegion": "eastus2",
    "SpeechServiceKey": "<your-key>",
    "BlobStorageConnectionString": "<your-connection-string>"
  }
}
```

---

## Speech Video Translation API Notes

- **API Version**: 2025-05-20
- **Endpoint Pattern**: `{endpoint}/videotranslation/translations/{translationId}`
- **Max Video Length**: 4 hours
- **Supported Formats**: MP4, WebM, MOV, AVI
- **Voice Options**: 
  - `PlatformVoice` - Azure neural voice
  - `PersonalVoice` - Clone speaker's voice (requires consent)

### Translation Flow
1. **Create Translation** → Returns translation ID
2. **Create Iteration** → Starts the actual processing
3. **Poll Status** → Check until `Succeeded` or `Failed`
4. **Get Results** → Download translated video and subtitles

---

## Coding Guidelines

### Azure Functions Best Practices
- Keep functions small and focused
- Use dependency injection for services
- Log extensively with structured logging
- Handle exceptions gracefully with proper error responses

### Durable Functions Specific
- Orchestrator functions must be deterministic
- Never use `DateTime.Now` in orchestrators (use `context.CurrentUtcDateTime`)
- Avoid I/O operations directly in orchestrators
- Use activity functions for all external calls

### Error Handling
- Use the error models from shared code
- Include correlation IDs in all error responses
- Log errors with full context before returning

### Telemetry
- Track custom events for business metrics
- Use dependency tracking for external calls
- Include operation context for distributed tracing

---

## Quick Reference Commands

```powershell
# Check Function App logs
az webapp log tail --name "FuncApp-AMA-3" --resource-group "AMAFY26-deployment-3"

# Test health endpoint
Invoke-WebRequest -Uri "https://funcapp-ama-3.azurewebsites.net/api/health" -Method GET

# List functions
az functionapp function list --name "FuncApp-AMA-3" --resource-group "AMAFY26-deployment-3" -o table

# Restart Function App
az functionapp restart --name "FuncApp-AMA-3" --resource-group "AMAFY26-deployment-3"

# Validate Bicep
az bicep build --file infra/main.bicep
```

---

## GitHub Repository
https://github.com/haslam93/FY26AMA-Capstone-AI-Video-Translation
