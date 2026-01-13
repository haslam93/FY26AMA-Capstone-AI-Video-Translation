# Multi-Agent Architecture Progress

## Overview
This document tracks the implementation progress of the multi-agent architecture for the Video Translation Service. The goal is to enhance the existing Durable Functions orchestration with AI-powered agents for improved subtitle validation and human-in-the-loop approval workflows.

---

## Target Architecture

```
┌──────────────────────────────────────────────────────────────────────────┐
│                        SUPERVISOR AGENT                                   │
│  (Coordinates workflow, delegates tasks, manages agent communication)    │
└──────────────────────────────────────────────────────────────────────────┘
         │                    │                    │
         ▼                    ▼                    ▼
┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐
│  TRANSLATION    │  │    SUBTITLE     │  │   HUMAN-IN-    │
│     AGENT       │  │   VALIDATION    │  │   THE-LOOP     │
│                 │  │     AGENT       │  │   COMPONENT    │
│ • Create job    │  │ • Analyze VTT   │  │ • Review queue │
│ • Monitor       │  │ • Check timing  │  │ • Approve/Reject│
│ • Copy outputs  │  │ • Validate sync │  │ • Edit subtitles│
└─────────────────┘  │ • Quality score │  │ • Final decision│
                     └─────────────────┘  └─────────────────┘
```

---

## Phase 1: Foundation & Infrastructure ✅ COMPLETE

### Tasks

| Task | Status | Date | Notes |
|------|--------|------|-------|
| Create `multiagent` branch | ✅ Complete | 2026-01-13 | Branch created and checked out |
| Update ai-foundry.bicep | ✅ Complete | 2026-01-13 | Added GPT-4o-mini deployment |
| Add Foundry Project | ✅ Complete | 2026-01-13 | `video-translation-agents` project |
| Update role-assignments.bicep | ✅ Complete | 2026-01-13 | Added OpenAI + Foundry Project roles |
| Update main.bicep outputs | ✅ Complete | 2026-01-13 | Added project endpoint outputs |
| Deploy infrastructure | ✅ Complete | 2026-01-14 | Deployed successfully (Exit Code 0) |
| Add Agent Framework packages | ✅ Complete | 2026-01-14 | Added NuGet packages |
| Configure Foundry connection | ✅ Complete | 2026-01-14 | AgentConfigurationService created |

### NuGet Packages Added
```xml
<PackageReference Include="Microsoft.Agents.AI.AzureAI" Version="1.0.0-preview.260108.1" />
<PackageReference Include="Microsoft.Agents.AI.Workflows" Version="1.0.0-preview.260108.1" />
<PackageReference Include="Azure.AI.Projects" Version="1.2.0-beta.5" />
<PackageReference Include="Azure.AI.OpenAI" Version="2.8.0-beta.1" />
<PackageReference Include="Azure.Storage.Blobs" Version="12.26.0" />
```

### Agent Infrastructure Files Created
- `src/Api/Agents/IAgentConfiguration.cs` - Interface for agent configuration
- `src/Api/Agents/AgentConfigurationService.cs` - Foundry connection with managed identity
- `src/Api/Models/TranslationWorkflowState.cs` - Multi-agent workflow state model

### Infrastructure Changes Made

#### ai-foundry.bicep
- Added GPT-4o-mini deployment (Standard SKU, 10K TPM capacity)
- Model: `gpt-4o-mini` version `2024-07-18`
- Auto-upgrade enabled for new versions
- **Added Foundry Project** `video-translation-agents` with system-assigned managed identity
- New outputs: `gpt4oMiniDeploymentName`, `gpt4oMiniDeploymentId`, `projectName`, `projectEndpoint`, `projectPrincipalId`

#### role-assignments.bicep
- Added `Cognitive Services OpenAI User` role for Function App
- Added `Cognitive Services OpenAI Contributor` role for Function App
- Added `Cognitive Services OpenAI User` role for deploying user
- **Added Foundry Project RBAC roles:**
  - `Cognitive Services OpenAI User` - access GPT-4o-mini
  - `Cognitive Services User` - access AI Services
  - `Storage Blob Data Contributor` - read/write subtitle files
  - `Key Vault Secrets User` - read secrets if needed

#### main.bicep
- Added `aiFoundryGpt4oMiniDeployment` output
- Added `aiFoundryProjectName` output
- Added `aiFoundryProjectEndpoint` output

---

## Phase 2: Supervisor Agent ⬜ NOT STARTED

### Tasks

| Task | Status | Date | Notes |
|------|--------|------|-------|
| Create SupervisorAgent class | ⬜ Not Started | - | Workflow coordinator |
| Define workflow graph | ⬜ Not Started | - | Agent edges and message flow |
| Integrate with Orchestrator | ⬜ Not Started | - | Wrap existing logic |
| Add state management | ⬜ Not Started | - | Multi-agent workflow state |

### Planned Files
- `src/Api/Agents/SupervisorAgent.cs`

### Completed Files (State Management)
- ✅ `src/Api/Models/TranslationWorkflowState.cs` - Workflow state with phases, validation results, human review

---

## Phase 3: Subtitle Validation Agent ⬜ NOT STARTED

### Tasks

| Task | Status | Date | Notes |
|------|--------|------|-------|
| Create SubtitleValidationAgent | ⬜ Not Started | - | GPT-4o-mini powered |
| Implement VTT parsing | ⬜ Not Started | - | Parse WebVTT files |
| Define validation prompts | ⬜ Not Started | - | Quality scoring prompts |
| Create ValidationResult model | ⬜ Not Started | - | Score, issues, recommendations |
| Add to workflow | ⬜ Not Started | - | After translation |

### Model Selection Rationale
- **Model**: GPT-4o-mini
- **Cost**: $0.26/1M tokens (economical for validation tasks)
- **Quality**: 0.7193 quality index (sufficient for text analysis)
- **Latency**: 0.89s TTFT (fast for UX)
- **Context**: 131K input (handles large subtitle files)

### Planned Files
- `src/Api/Agents/SubtitleValidationAgent.cs`
- `src/Api/Models/ValidationResult.cs`
- `src/Api/Services/VttParsingService.cs`

---

## Phase 4: Human-in-the-Loop ⬜ NOT STARTED

### Tasks

| Task | Status | Date | Notes |
|------|--------|------|-------|
| Create Review Queue API | ⬜ Not Started | - | GET/POST endpoints |
| Build Review UI page | ⬜ Not Started | - | Blazor review page |
| Implement approval workflow | ⬜ Not Started | - | Pause/resume orchestration |
| Add subtitle editing | ⬜ Not Started | - | In-browser VTT editor |
| Create notification system | ⬜ Not Started | - | Email/Teams notifications |

### Planned Files
- `src/Api/Functions/ReviewFunctions.cs`
- `src/Api/Models/ReviewDecision.cs`
- `src/ui/Pages/Reviews.razor`
- `src/ui/Pages/ReviewDetails.razor`
- `src/ui/Components/SubtitleEditor.razor`

---

## Phase 5: Integration & Testing ⬜ NOT STARTED

### Tasks

| Task | Status | Date | Notes |
|------|--------|------|-------|
| End-to-end workflow testing | ⬜ Not Started | - | Full multi-agent flow |
| Update existing tests | ⬜ Not Started | - | Modify unit tests |
| Add integration tests | ⬜ Not Started | - | Agent interaction tests |
| Update documentation | ⬜ Not Started | - | Architecture docs |

---

## Required NuGet Packages

```bash
# Agent Framework (preview)
dotnet add package Microsoft.Agents.AI.AzureAI --prerelease
dotnet add package Microsoft.Agents.AI.Workflows --prerelease

# Azure AI Projects SDK
dotnet add package Azure.AI.Projects --prerelease
```

---

## Environment Configuration

### Required App Settings (after deployment)

| Setting | Description |
|---------|-------------|
| `AIFoundry__ProjectEndpoint` | Foundry Project endpoint (e.g., `https://aiservices-ama-3.services.ai.azure.com/api/projects/video-translation-agents`) |
| `AIFoundry__OpenAIEndpoint` | Azure OpenAI endpoint (e.g., `https://aiservices-ama-3.openai.azure.com`) |
| `AIFoundry__ModelDeploymentName` | `gpt-4o-mini` |

### Foundry Project Details

| Property | Value |
|----------|-------|
| **Project Name** | `video-translation-agents` |
| **Display Name** | Video Translation Multi-Agent Project |
| **Identity Type** | System-assigned managed identity |
| **Endpoint Pattern** | `https://{account}.cognitiveservices.azure.com/agents/v1.0/projects/{project}` |

### Authentication
- **Method**: Managed Identity (DefaultAzureCredential)
- **Function App Roles**: Cognitive Services OpenAI User, Cognitive Services OpenAI Contributor
- **Foundry Project Roles**: Cognitive Services OpenAI User, Storage Blob Data Contributor, Key Vault Secrets User

---

## Effort Estimates

| Phase | Description | LoE |
|-------|-------------|-----|
| Phase 1 | Foundation & Setup | 2-3 days |
| Phase 2 | Supervisor Agent | 2-3 days |
| Phase 3 | Subtitle Validation Agent | 3-4 days |
| Phase 4 | Human-in-the-Loop | 4-5 days |
| Phase 5 | Integration & Testing | 2-3 days |
| **Total** | **Full Implementation** | **13-18 days** |

---

## Deployment Commands

### Deploy Infrastructure (Incremental)
```powershell
# From infra/ directory
az deployment sub create `
  --location eastus2 `
  --template-file main.bicep `
  --parameters deploymentNumber=3

# Or use the deploy script
.\deploy.ps1 -DeploymentNumber 3
```

### Verify GPT-4o-mini Deployment
```powershell
az cognitiveservices account deployment list `
  --resource-group AMAFY26-deployment-3 `
  --name AIServices-AMA-3 `
  --output table
```

---

## Change Log

| Date | Change | Author |
|------|--------|--------|
| 2026-01-13 | Created multiagent branch | Copilot |
| 2026-01-13 | Added GPT-4o-mini deployment to bicep | Copilot |
| 2026-01-13 | Added OpenAI RBAC roles for managed identity | Copilot |
| 2026-01-13 | Created progress tracking document | Copilot |
| 2026-01-13 | Added Foundry Project `video-translation-agents` | Copilot |
| 2026-01-13 | Added Foundry Project RBAC roles (OpenAI, Storage, KeyVault) | Copilot |
| 2026-01-13 | Updated main.bicep with project endpoint outputs | Copilot |
| 2026-01-14 | Fixed API version to 2025-04-01-preview for allowProjectManagement | Copilot |
| 2026-01-14 | Deployed infrastructure successfully | Copilot |
| 2026-01-14 | Added NuGet packages: Agents.AI.AzureAI, Agents.AI.Workflows, Azure.AI.Projects, Azure.AI.OpenAI | Copilot |
| 2026-01-14 | Created IAgentConfiguration interface | Copilot |
| 2026-01-14 | Created AgentConfigurationService with managed identity auth | Copilot |
| 2026-01-14 | Created TranslationWorkflowState model | Copilot |
| 2026-01-14 | Updated Program.cs with agent DI registration | Copilot |
| 2026-01-14 | Updated local.settings.json with Foundry config | Copilot |
