# Video Translation Service - Capstone Project

## Overview

A cloud-native video translation service built on Azure that automatically translates video content using Azure Speech Services Video Translation API. The system features:

- **Automatic video dubbing** with voice cloning (Personal Voice) or platform voices
- **Subtitle generation** in both source and target languages (WebVTT format)
- **Burned-in subtitles** option to embed translated subtitles directly in the video
- **Real-time job tracking** with status updates and progress monitoring

## Architecture

```
┌─────────────────────────────────────────────────────────────────────────┐
│                     Video Translation Service                            │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│   User ──────────────────► Static Web App (Blazor WASM)                │
│                                    │                                    │
│                                    ▼                                    │
│                            Function App (Durable Functions)             │
│                                    │                                    │
│         ┌──────────────────────────┼──────────────────────────┐         │
│         ▼                          ▼                          ▼         │
│   Speech Services          Storage Account             App Insights     │
│   (Video Translation)     (Videos, Outputs)          (Monitoring)       │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

## Features

| Feature | Description |
|---------|-------------|
| **Video Upload** | Upload via URL, blob path, or direct file upload |
| **120+ Source Languages** | Extensive language support for source audio |
| **60+ Target Languages** | Wide range of translation targets |
| **Voice Cloning** | Personal Voice option to clone speaker's voice |
| **Subtitle Generation** | Automatic WebVTT subtitle creation |
| **Burned-in Subtitles** | Option to embed subtitles in output video |
| **Job Dashboard** | Track all translation jobs with status updates |
| **Multi-Agent Validation** | 4 specialized AI agents analyze subtitle quality in parallel |
| **Weighted Quality Scoring** | Translation (40%), Technical (30%), Cultural (30%) |
| **Human Approval Gate** | Review and approve/reject translations |
| **Agent Chat** | Chat with specific agents for detailed feedback |
| **Auto-Rejection** | Jobs auto-rejected after 3-day timeout |

## Project Structure

```
├── src/
│   ├── Api/                    # Azure Durable Functions backend
│   │   ├── Activities/         # Workflow activity functions
│   │   ├── Functions/          # HTTP trigger functions
│   │   ├── Models/             # Data models and DTOs
│   │   ├── Orchestration/      # Durable orchestrator
│   │   └── Services/           # Business logic & multi-agent services
│   │       ├── MultiAgentValidationService.cs  # 4-agent parallel validation
│   │       ├── FoundryAgentService.cs          # Azure AI Foundry integration
│   │       ├── SpeechTranslationService.cs     # Speech API client
│   │       └── BlobStorageService.cs           # Blob storage operations
│   └── ui/                     # Blazor WebAssembly frontend
│       ├── Pages/              # Razor pages (Dashboard, Create, Details, Reviews)
│       ├── Models/             # Client-side models
│       ├── Services/           # API client services
│       └── wwwroot/            # Static assets & config
├── tests/
│   ├── unit/                   # Unit tests (xUnit, bUnit)
│   └── integration/            # Integration tests
├── infra/                      # Bicep IaC templates
│   ├── main.bicep              # Main deployment template
│   ├── main.parameters.json    # Deployment parameters
│   └── modules/                # Resource modules
├── docs/                       # Documentation
│   ├── architecture.md         # System architecture
│   ├── runbook.md              # Operations runbook
│   ├── devops.md               # CI/CD documentation
│   └── test-plan.md            # Testing strategy
└── .github/workflows/          # CI/CD pipelines
    ├── ci.yml                  # Continuous Integration
    ├── cd-infra.yml            # Infrastructure deployment
    └── cd-app.yml              # Application deployment
```

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli)
- [Azure Functions Core Tools v4](https://docs.microsoft.com/azure/azure-functions/functions-run-local)
- [Node.js 18+](https://nodejs.org/) (for SWA CLI)
- Azure Subscription with Contributor access

### Local Development

```powershell
# Clone the repository
git clone https://github.com/haslam93/FY26AMA-Capstone-AI-Video-Translation.git
cd FY26AMA-Capstone-AI-Video-Translation

# Start Azurite (local storage emulator)
azurite --silent --location "$env:TEMP\azurite"

# Run the API locally
cd src/Api
func start

# Run the UI locally (in another terminal)
cd src/ui
dotnet run
```

### Deploy to Azure

```powershell
# Login to Azure
az login
az account set --subscription "<your-subscription-id>"

# Deploy infrastructure
az deployment sub create \
  --location eastus2 \
  --template-file infra/main.bicep \
  --parameters infra/main.parameters.json \
  --parameters deploymentNumber=3

# Deploy Function App
cd src/Api
dotnet publish -c Release -o ./publish
Compress-Archive -Path ./publish/* -DestinationPath ./publish.zip -Force
az functionapp deployment source config-zip \
  --resource-group "AMAFY26-deployment-3" \
  --name "FuncApp-AMA-3" \
  --src ./publish.zip

# Deploy Static Web App
cd src/ui
dotnet publish -c Release -o ./publish
swa deploy ./publish/wwwroot --deployment-token "<your-token>" --env production
```

## Security

The application uses Azure-native security features:

| Resource | Protection |
|----------|------------|
| **Storage Account** | Managed Identity (no public access) |
| **Key Vault** | RBAC (no access policies) |
| **Function App** | HTTPS-only, TLS 1.2 minimum |
| **Static Web App** | HTTPS-only |

## CI/CD

| Workflow | Trigger | Purpose |
|----------|---------|---------|
| `ci.yml` | Push/PR to main | Validate Bicep, build & test |
| `cd-infra.yml` | Manual | Deploy Azure infrastructure |
| `cd-app.yml` | Manual | Deploy API & UI |

## Documentation

- [Architecture](docs/architecture.md) - System design and components
- [Runbook](docs/runbook.md) - Deployment and operations guide
- [DevOps](docs/devops.md) - CI/CD pipeline documentation
- [Test Plan](docs/test-plan.md) - Testing strategy

## Azure Resources

| Resource | Name | Purpose |
|----------|------|---------|
| Resource Group | AMAFY26-deployment-3 | Container for all resources |
| Function App | FuncApp-AMA-3 | Durable Functions API |
| Static Web App | SWA-AMA-3 | Blazor WebAssembly UI |
| Speech Service | Speech-AMA-3 | Video Translation API |
| Storage Account | storageama3 | Videos, outputs, subtitles |
| Application Insights | AppInsights-AMA-3 | Monitoring and logging |

## License

This project is part of the Microsoft AMA FY26 Capstone program.
