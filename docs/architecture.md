# Architecture

## Table of Contents
- [Overview](#overview)
- [High-Level Architecture](#high-level-architecture)
- [Components](#components)
- [API Reference](#api-reference)
- [Data Flow](#data-flow)
- [Azure Services](#azure-services)
- [Infrastructure as Code](#infrastructure-as-code)
- [Security Architecture](#security-architecture)
- [Scalability Considerations](#scalability-considerations)

---

## Overview

The Video Translation Service is a cloud-native application built on Azure that automatically translates video content using Azure Speech Services Video Translation API. The system uses Azure Durable Functions to orchestrate a multi-step workflow that:

1. Accepts video files via URL or Azure Blob Storage path
2. Creates a translation job via Azure Speech Video Translation API
3. Creates an iteration to process the translation
4. Polls for completion with configurable timeout
5. Copies translated outputs to your storage account
6. Provides a Blazor WebAssembly UI for job management

### Key Design Principles

| Principle | Implementation |
|-----------|---------------|
| **Serverless** | Azure Functions with Standard (S1) plan for reliable performance |
| **Event-Driven** | HTTP triggers and Durable Functions orchestration |
| **Secure** | Managed identities, RBAC, Key Vault for secrets |
| **Observable** | Application Insights, Log Analytics, alerts |
| **Scalable** | App Service Plan scaling, stateless workers |
| **Idempotent** | Durable Functions replay-safe activities |
| **User-Friendly** | Blazor WebAssembly UI hosted on Static Web App |

---

## High-Level Architecture

```mermaid
graph TB
    subgraph "User Layer"
        Browser[User Browser]
    end

    subgraph "Azure Static Web App"
        BlazorUI[Blazor WebAssembly UI<br/>• Dashboard<br/>• Create Job<br/>• Job Details<br/>• Reviews]
    end

    subgraph "Azure Function App"
        HTTP[HTTP Triggers<br/>TranslationFunctions.cs]
        Orch[Orchestrator<br/>VideoTranslationOrchestrator.cs]
        
        subgraph "Activities"
            A1[ValidateInputActivity]
            A2[CreateTranslationActivity]
            A3[CreateIterationActivity]
            A4[GetIterationStatusActivity]
            A5[CopyOutputsActivity]
            A6[RunValidationActivity]
        end
        
        subgraph "Multi-Agent System"
            ORCH_AGENT[Orchestrator Agent<br/>Coordinates & Aggregates]
            TRANS_AGENT[Translation Agent<br/>40% Weight]
            TECH_AGENT[Technical Agent<br/>30% Weight]
            CULT_AGENT[Cultural Agent<br/>30% Weight]
        end
    end

    subgraph "Azure Services"
        Speech[Speech Services<br/>Video Translation API]
        Storage[Storage Account<br/>• videos/<br/>• outputs/<br/>• subtitles/]
        KV[Key Vault]
        AI[AI Services<br/>GPT-4o-mini]
    end

    subgraph "Monitoring"
        AppIns[Application Insights]
        LogAna[Log Analytics]
    end

    Browser --> BlazorUI
    BlazorUI -->|REST API| HTTP
    HTTP --> Orch
    Orch --> A1
    Orch --> A2
    Orch --> A3
    Orch --> A4
    Orch --> A5
    Orch --> A6
    A6 --> ORCH_AGENT
    ORCH_AGENT --> TRANS_AGENT
    ORCH_AGENT --> TECH_AGENT
    ORCH_AGENT --> CULT_AGENT
    TRANS_AGENT --> AI
    TECH_AGENT --> AI
    CULT_AGENT --> AI
    
    A1 --> Storage
    A2 --> Speech
    A3 --> Speech
    A4 --> Speech
    A5 --> Storage
    
    HTTP -.-> AppIns
    Orch -.-> AppIns
    AppIns --> LogAna
```

---

## Components

### 1. Blazor WebAssembly UI (`src/ui/`)

**Purpose**: User-friendly web interface for video translation management.

```mermaid
graph LR
    subgraph "Blazor WebAssembly App"
        Home[Home.razor<br/>Dashboard]
        Create[Create.razor<br/>New Job Form]
        Details[JobDetails.razor<br/>Status & Downloads]
        Reviews[Reviews.razor<br/>Pending Approvals]
    end
    
    subgraph "Services"
        API[TranslationApiService.cs]
    end
    
    subgraph "Models"
        Models[JobModels.cs]
    end
    
    Home --> API
    Create --> API
    Details --> API
    Reviews --> API
    API --> Models
```

| Page | Route | Purpose |
|------|-------|---------|
| **Dashboard** | `/` | View all translation jobs with status badges |
| **Create Job** | `/create` | Form for video URL/blob, language selection, voice options |
| **Job Details** | `/jobs/{jobId}` | View progress, download outputs, approve/reject, AI validation |
| **Reviews** | `/reviews` | View and manage jobs pending human approval |
| **Create Job** | `/create` | Form for video URL/blob, language selection, voice options |
| **Job Details** | `/jobs/{jobId}` | View progress, download outputs, auto-refresh status |

**Technology Stack**:
- .NET 9 Blazor WebAssembly
- Bootstrap 5 with Bootstrap Icons
- Hosted on Azure Static Web App (Free tier)

### 2. API Layer (`src/api/Functions/`)

**Purpose**: HTTP entry points for the video translation service.

**File**: `TranslationFunctions.cs`

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/jobs` | POST | Start a new video translation job |
| `/api/jobs` | GET | List all translation jobs |
| `/api/jobs/{jobId}` | GET | Check job status and get output URLs |
| `/api/jobs/{jobId}/iterate` | POST | Start a new iteration with edited WebVTT |
| `/api/jobs/{jobId}/validate` | POST | Run AI validation on subtitles |
| `/api/reviews/pending` | GET | List jobs pending human approval |
| `/api/jobs/{jobId}/approve` | POST | Approve a translation job |
| `/api/jobs/{jobId}/reject` | POST | Reject a translation job |

**Technology**: Azure Functions HTTP triggers with Durable Client (.NET 8 Isolated)

### 3. Orchestrator (`src/api/Orchestration/`)

**Purpose**: Coordinates the multi-step translation workflow using Durable Functions.

**File**: `VideoTranslationOrchestrator.cs`

**Pattern**: Polling-based state machine with 60-minute timeout and human approval gate

```mermaid
stateDiagram-v2
    [*] --> Validating: Job Submitted
    
    Validating --> CreatingTranslation: Input Valid
    Validating --> Failed: Invalid Input
    
    CreatingTranslation --> CreatingIteration: Translation Created
    CreatingTranslation --> Failed: API Error
    
    CreatingIteration --> Processing: Iteration Started
    CreatingIteration --> Failed: API Error
    
    Processing --> Processing: Poll (30s interval)
    Processing --> CopyingOutputs: Succeeded
    Processing --> Failed: API Failed
    Processing --> Failed: Timeout (60min)
    
    CopyingOutputs --> RunningValidation: Outputs Copied
    CopyingOutputs --> Failed: Copy Error
    
    RunningValidation --> PendingApproval: AI Validation Complete
    RunningValidation --> PendingApproval: Validation Skipped
    
    PendingApproval --> Approved: Human Approves
    PendingApproval --> Rejected: Human Rejects
    PendingApproval --> Rejected: Timeout (3 days)
    
    Approved --> [*]
    Rejected --> [*]
    Failed --> [*]
```

**Configuration**:
| Setting | Value |
|---------|-------|
| Polling Interval | 30 seconds |
| Max Duration | 60 minutes |
| Approval Timeout | 3 days |
| Retry Attempts | 3 |
| Retry Backoff | Exponential (5s base) |

### 4. Activity Functions (`src/api/Activities/`)

**Purpose**: Individual activity functions for specific tasks.

```mermaid
graph LR
    subgraph "Activity Functions"
        V[ValidateInputActivity]
        CT[CreateTranslationActivity]
        CI[CreateIterationActivity]
        GS[GetIterationStatusActivity]
        CO[CopyOutputsActivity]
        RV[RunValidationActivity]
    end
    
    subgraph "External Services"
        Speech[Speech API]
        Blob[Blob Storage]
        GPT[GPT-4o-mini]
    end
    
    V --> Blob
    CT --> Speech
    CI --> Speech
    GS --> Speech
    CO --> Blob
    RV --> GPT
```

| Activity | File | Responsibility |
|----------|------|----------------|
| `ValidateInputActivity` | `ValidateInputActivity.cs` | Validate video URL/blob path, generate SAS URL if needed |
| `CreateTranslationActivity` | `CreateTranslationActivity.cs` | Create translation via `PUT /translations/{id}` |
| `CreateIterationActivity` | `CreateIterationActivity.cs` | Create iteration via `PUT /translations/{id}/iterations/{id}` |
| `GetIterationStatusActivity` | `GetIterationStatusActivity.cs` | Poll iteration status via `GET /translations/{id}/iterations/{id}` |
| `CopyOutputsActivity` | `CopyOutputsActivity.cs` | Copy translated video/subtitles from Speech API URLs to your storage |
| `RunValidationActivity` | `RunValidationActivity.cs` | Run AI-powered subtitle validation using GPT-4o-mini |

### 5. Multi-Agent Validation System (`src/api/Services/`)

**Purpose**: AI-powered multi-agent system for comprehensive subtitle quality validation.

**Architecture**: 4 specialized agents running in **parallel** for optimal performance.

```mermaid
graph TB
    subgraph "Multi-Agent Orchestration"
        Input[Subtitles Input] --> ORCH[Orchestrator Agent]
        ORCH --> |Parallel Execution| TRANS[Translation Agent]
        ORCH --> |Parallel Execution| TECH[Technical Agent]
        ORCH --> |Parallel Execution| CULT[Cultural Agent]
        TRANS --> AGG[Score Aggregation]
        TECH --> AGG
        CULT --> AGG
        AGG --> |Weighted Score| Output[Final Result]
    end
```

| Agent | Weight | Responsibility |
|-------|--------|----------------|
| **Orchestrator Agent** | - | Coordinates workflow, aggregates scores, provides summary |
| **Translation Agent** | 40% | Semantic accuracy, meaning preservation, natural phrasing |
| **Technical Agent** | 30% | Timing sync, CPS rate, formatting compliance |
| **Cultural Agent** | 30% | Cultural adaptation, idioms, regional appropriateness |

**Services & Files**:
| Service | File | Purpose |
|---------|------|---------||
| `IMultiAgentValidationService` | `Services/IMultiAgentValidationService.cs` | Interface for multi-agent validation |
| `MultiAgentValidationService` | `Services/MultiAgentValidationService.cs` | 4-agent parallel validation implementation |
| `FoundryAgentService` | `Services/FoundryAgentService.cs` | Azure AI Foundry Agent SDK integration |
| `MultiAgentValidationResult` | `Models/MultiAgentValidationResult.cs` | Result models with weighted scoring |

**Score Thresholds**:
| Score Range | Recommendation | Action |
|-------------|----------------|--------|
| ≥ 80 | **Approve** | Auto-proceed to approval |
| 50-79 | **NeedsReview** | Human review recommended |
| < 50 | **Reject** | Translation quality insufficient |

**Per-Agent Model Configuration**:
```json
{
  "FoundryAgent": {
    "ModelDeploymentName": "gpt-4o-mini",
    "AgentModels": {
      "Orchestrator": "gpt-4o",
      "Translation": "gpt-4o-mini",
      "Technical": "gpt-4o-mini",
      "Cultural": "gpt-4o-mini"
    }
  }
}
```

### 6. Services (`src/api/Services/`)

**Purpose**: Shared services and API clients.

| Service | Interface | Implementation | Responsibility |
|---------|-----------|----------------|----------------|
| Speech Translation | `ISpeechTranslationService` | `SpeechTranslationService.cs` | HTTP client for Speech Video Translation REST API |
| Blob Storage | `IBlobStorageService` | `BlobStorageService.cs` | Blob upload/download, SAS token generation, copy operations |

### 6. Models (`src/api/Models/`)

**Purpose**: Data transfer objects and domain models.

```mermaid
classDiagram
    class TranslationJobRequest {
        +string DisplayName
        +string VideoUrl
        +string BlobPath
        +string SourceLocale
        +string TargetLocale
        +string VoiceKind
        +int SpeakerCount
    }
    
    class TranslationJob {
        +string JobId
        +string TranslationId
        +string IterationId
        +int IterationNumber
        +JobStatus Status
        +string StatusMessage
        +TranslationJobRequest Request
        +JobResult Result
        +DateTime CreatedAt
    }
    
    class JobStatus {
        <<enumeration>>
        Submitted
        Validating
        CreatingTranslation
        CreatingIteration
        Processing
        CopyingOutputs
        RunningValidation
        PendingApproval
        Approved
        Rejected
        Completed
        Failed
    }
    
    class JobResult {
        +string TranslatedVideoUrl
        +string SourceWebVttUrl
        +string TargetWebVttUrl
    }
    
    class ApprovalDecision {
        +bool Approved
        +string ReviewedBy
        +string Reason
        +string Comments
    }
    
    class SubtitleValidationResult {
        +bool IsValid
        +double ConfidenceScore
        +List~ValidationIssue~ Issues
        +ValidationCategoryScores CategoryScores
    }
    
    class MultiAgentValidationResult {
        +double OverallScore
        +string Recommendation
        +AgentReviewResult TranslationReview
        +AgentReviewResult TechnicalReview
        +AgentReviewResult CulturalReview
        +string OrchestratorSummary
        +DateTime ValidatedAt
    }
    
    class AgentReviewResult {
        +string AgentType
        +double Score
        +string Reasoning
        +List~MultiAgentIssue~ Issues
        +string ThreadId
    }
    
    TranslationJob --> TranslationJobRequest
    TranslationJob --> JobStatus
    TranslationJob --> JobResult
    TranslationJob --> ApprovalDecision
    TranslationJob --> SubtitleValidationResult
    TranslationJob --> MultiAgentValidationResult
```

| Model | Location | Purpose |
|-------|----------|---------|
| `TranslationJobRequest` | `Models/TranslationJobRequest.cs` | API input from client |
| `TranslationJob` | `Models/TranslationJob.cs` | Orchestrator state object |
| `JobStatusResponse` | `Models/JobStatusResponse.cs` | API response DTO |
| `IterateRequest` | `Models/IterateRequest.cs` | Request for new iteration |
| `ApprovalDecision` | `Models/TranslationJob.cs` | Human approval/rejection decision |
| `SubtitleValidationResult` | `Models/TranslationWorkflowState.cs` | Single-agent validation result |
| `MultiAgentValidationResult` | `Models/MultiAgentValidationResult.cs` | Multi-agent validation with weighted scoring |
| `AgentReviewResult` | `Models/MultiAgentValidationResult.cs` | Per-agent review result |
| `MultiAgentIssue` | `Models/MultiAgentValidationResult.cs` | Validation issue from agents |
| Speech API Models | `Models/SpeechApi/*.cs` | Request/response types for Speech API |

---

## API Reference

### Base URL

- **Local Development**: `http://localhost:7071`
- **Production**: `https://funcapp-ama-{N}.azurewebsites.net`

### Endpoints

#### Create Translation Job

```http
POST /api/jobs
Content-Type: application/json

{
  "displayName": "My Video Translation",
  "videoUrl": "https://example.com/video.mp4",
  "sourceLocale": "en-US",
  "targetLocale": "es-ES",
  "voiceKind": "PersonalVoice",
  "speakerCount": 1
}
```

**Alternative with Blob Storage**:
```json
{
  "displayName": "My Video Translation",
  "blobPath": "videos/my-video.mp4",
  "sourceLocale": "en-US",
  "targetLocale": "fr-FR",
  "voiceKind": "PlatformVoice"
}
```

**Response** (202 Accepted):
```json
{
  "jobId": "abc123def456",
  "status": "Submitted",
  "statusUrl": "https://funcapp-ama-3.azurewebsites.net/api/jobs/abc123def456"
}
```

#### Get Job Status

```http
GET /api/jobs/{jobId}
```

**Response** (200 OK):
```json
{
  "jobId": "abc123def456",
  "displayName": "My Video Translation",
  "status": "Completed",
  "statusMessage": "Translation completed successfully",
  "iterationNumber": 1,
  "sourceLocale": "en-US",
  "targetLocale": "es-ES",
  "createdAt": "2026-01-12T10:30:00Z",
  "lastUpdatedAt": "2026-01-12T10:45:00Z",
  "result": {
    "translatedVideoUrl": "https://storageama3.blob.core.windows.net/outputs/abc123def456/translated.mp4",
    "sourceWebVttUrl": "https://storageama3.blob.core.windows.net/subtitles/abc123def456/source.vtt",
    "targetWebVttUrl": "https://storageama3.blob.core.windows.net/subtitles/abc123def456/target.vtt"
  }
}
```

#### List All Jobs

```http
GET /api/jobs
```

**Response** (200 OK):
```json
{
  "jobs": [
    {
      "jobId": "abc123def456",
      "status": "Completed",
      "createdAt": "2026-01-12T10:30:00Z",
      "lastUpdatedAt": "2026-01-12T10:45:00Z"
    },
    {
      "jobId": "xyz789ghi012",
      "status": "Processing",
      "createdAt": "2026-01-12T11:00:00Z",
      "lastUpdatedAt": "2026-01-12T11:05:00Z"
    }
  ]
}
```

### Status Values

| Status | Description |
|--------|-------------|
| `Submitted` | Job received, waiting to start |
| `Validating` | Validating input video and parameters |
| `CreatingTranslation` | Creating translation in Speech API |
| `CreatingIteration` | Creating iteration for processing |
| `Processing` | Translation in progress (polling) |
| `CopyingOutputs` | Copying results to storage |
| `Completed` | Translation complete, outputs available |
| `Failed` | Translation failed (see error details) |

### Supported Languages

| Code | Language |
|------|----------|
| `en-US` | English (US) |
| `en-GB` | English (UK) |
| `es-ES` | Spanish (Spain) |
| `es-MX` | Spanish (Mexico) |
| `fr-FR` | French |
| `de-DE` | German |
| `it-IT` | Italian |
| `pt-BR` | Portuguese (Brazil) |
| `zh-CN` | Chinese (Simplified) |
| `zh-TW` | Chinese (Traditional) |
| `ja-JP` | Japanese |
| `ko-KR` | Korean |
| `ar-SA` | Arabic |
| `hi-IN` | Hindi |

---

## Data Flow

### Video Translation Flow

```mermaid
sequenceDiagram
    autonumber
    participant User as User Browser
    participant UI as Blazor UI
    participant API as Function App
    participant Orch as Orchestrator
    participant Speech as Speech API
    participant Storage as Blob Storage

    User->>UI: Submit translation request
    UI->>API: POST /api/jobs
    API->>Orch: ScheduleNewOrchestrationInstanceAsync()
    API-->>UI: 202 Accepted (jobId)
    UI-->>User: Show "Job Submitted"

    Note over Orch: Orchestration Starts
    
    Orch->>Orch: ValidateInputActivity
    alt Blob Path provided
        Orch->>Storage: Generate SAS URL
        Storage-->>Orch: Video SAS URL
    end
    
    Orch->>Speech: PUT /translations/{id}
    Speech-->>Orch: Translation Created
    
    Orch->>Speech: PUT /translations/{id}/iterations/{id}
    Speech-->>Orch: Iteration Started
    
    loop Poll every 30 seconds (max 60 min)
        Orch->>Speech: GET /translations/{id}/iterations/{id}
        Speech-->>Orch: Status (Running/Succeeded/Failed)
    end
    
    Note over Orch: Status = Succeeded
    
    Orch->>Storage: Copy translated video
    Orch->>Storage: Copy source WebVTT
    Orch->>Storage: Copy target WebVTT
    
    Note over Orch: Orchestration Complete
    
    User->>UI: Check status
    UI->>API: GET /api/jobs/{jobId}
    API-->>UI: Job with output URLs
    UI-->>User: Show downloads
```

### Iteration Flow (Refinement)

```mermaid
sequenceDiagram
    participant User as User Browser
    participant UI as Blazor UI
    participant API as Function App
    participant Speech as Speech API

    User->>UI: Download source WebVTT
    User->>User: Edit WebVTT (fix translations)
    User->>UI: Upload edited WebVTT
    UI->>API: POST /api/jobs/{jobId}/iterate
    API->>Speech: PUT /translations/{id}/iterations/{id+1}
    Note right of Speech: With edited WebVTT
    Speech-->>API: New iteration started
    API-->>UI: New job ID
    Note over UI: Same polling flow as above
```

### Data Storage Layout

```mermaid
graph TD
    subgraph "Storage Account: storageama{N}"
        subgraph "videos container"
            V1["{jobId}/original.mp4"]
        end
        
        subgraph "outputs container"
            O1["{jobId}/translated-video.mp4"]
        end
        
        subgraph "subtitles container"
            S1["{jobId}/source.vtt"]
            S2["{jobId}/target.vtt"]
        end
    end
```

```
Storage Account: storageama{N}
├── videos/                      # Input video files
│   └── {jobId}/
│       └── original.mp4
├── outputs/                     # Translated outputs  
│   └── {jobId}/
│       └── translated-video.mp4
└── subtitles/                   # Generated subtitles
    └── {jobId}/
        ├── source.vtt           # Original language WebVTT
        └── target.vtt           # Translated language WebVTT
```

---

## Azure Services

### Service Architecture

```mermaid
graph TB
    subgraph "Resource Group: AMAFY26-deployment-{N}"
        subgraph "Frontend"
            SWA[Static Web App<br/>SWA-AMA-{N}<br/>Free Tier]
        end
        
        subgraph "Backend"
            ASP[App Service Plan<br/>ASP-AMA-{N}<br/>Standard S1]
            FUNC[Function App<br/>FuncApp-AMA-{N}<br/>.NET 8 Isolated]
        end
        
        subgraph "Storage"
            STORAGE[Storage Account<br/>storageama{N}<br/>Standard LRS]
        end
        
        subgraph "AI Services"
            SPEECH[Speech Services<br/>Speech-AMA-{N}<br/>S0 Tier]
            AI[AI Services<br/>AIServices-AMA-{N}<br/>S0 Tier]
            PROJ[Foundry Project<br/>video-translation-agents<br/>Multi-Agent]
            GPT[GPT-4o-mini<br/>Deployment]
        end
        
        subgraph "Security"
            KV[Key Vault<br/>KeyVault-AMA-{N}<br/>RBAC Enabled]
        end
        
        subgraph "Monitoring"
            APPINS[Application Insights<br/>AppInsights-AMA-{N}]
            LOG[Log Analytics<br/>LogAnalytics-AMA-{N}]
        end
    end
    
    SWA -->|CORS| FUNC
    FUNC --> ASP
    FUNC -->|Managed Identity| STORAGE
    FUNC -->|Managed Identity| SPEECH
    FUNC -->|Managed Identity| KV
    FUNC -->|Managed Identity| PROJ
    FUNC -.->|Telemetry| APPINS
    APPINS --> LOG
    SPEECH --> AI
    PROJ --> GPT
    AI --> GPT
```

### Core Services

| Service | Resource Name | SKU | Purpose |
|---------|---------------|-----|---------|
| **Static Web App** | `SWA-AMA-{N}` | Free | Blazor WebAssembly UI hosting |
| **Azure Functions** | `FuncApp-AMA-{N}` | S1 | Durable Functions API backend |
| **App Service Plan** | `ASP-AMA-{N}` | Standard S1 | Hosting plan for Functions |
| **Storage Account** | `storageama{N}` | Standard LRS | Blob storage (videos, outputs, subtitles) |
| **Speech Services** | `Speech-AMA-{N}` | S0 | Video Translation API |
| **AI Services** | `AIServices-AMA-{N}` | S0 | Cognitive Services (hub for AI) |
| **Foundry Project** | `video-translation-agents` | - | Multi-agent orchestration project |
| **GPT-4o-mini** | `gpt-4o-mini` | Standard (10K TPM) | Subtitle validation agent model |

### Supporting Services

| Service | Resource Name | Purpose |
|---------|---------------|---------|
| **Key Vault** | `KeyVault-AMA-{N}` | Secrets and certificate management |
| **Application Insights** | `AppInsights-AMA-{N}` | APM and distributed tracing |
| **Log Analytics** | `LogAnalytics-AMA-{N}` | Centralized logging and queries |

---

## Infrastructure as Code

### Bicep Structure

```mermaid
graph TD
    subgraph "infra/"
        MAIN[main.bicep<br/>Subscription Scope]
        PARAMS[main.parameters.json]
        DEPLOY_PS[deploy.ps1]
        DEPLOY_SH[deploy.sh]
        
        subgraph "modules/"
            LOG[log-analytics.bicep]
            APPINS[app-insights.bicep]
            STORAGE[storage-account.bicep]
            KV[keyvault.bicep]
            SPEECH[speech-services.bicep]
            AI[ai-foundry.bicep]
            SWA[static-web-app.bicep]
            FUNC[function-app.bicep]
            RBAC[role-assignments.bicep]
        end
    end
    
    MAIN --> LOG
    MAIN --> APPINS
    MAIN --> STORAGE
    MAIN --> KV
    MAIN --> SPEECH
    MAIN --> AI
    MAIN --> SWA
    MAIN --> FUNC
    MAIN --> RBAC
    
    APPINS -.-> LOG
    FUNC -.-> STORAGE
    FUNC -.-> APPINS
    FUNC -.-> SWA
    RBAC -.-> FUNC
    RBAC -.-> STORAGE
    RBAC -.-> KV
    RBAC -.-> SPEECH
```

```
infra/
├── main.bicep                    # Entry point (subscription scope)
├── main.parameters.json          # Parameter values (optional)
├── deploy.ps1                    # PowerShell deployment script
├── deploy.sh                     # Bash deployment script
└── modules/
    ├── log-analytics.bicep       # Log Analytics Workspace
    ├── app-insights.bicep        # Application Insights
    ├── storage-account.bicep     # Storage + containers (videos, outputs, subtitles)
    ├── keyvault.bicep            # Key Vault (RBAC enabled)
    ├── speech-services.bicep     # Speech Services with custom subdomain
    ├── ai-foundry.bicep          # AI Services (Cognitive Services)
    ├── static-web-app.bicep      # Static Web App for Blazor UI
    ├── function-app.bicep        # Function App + S1 App Service Plan
    └── role-assignments.bicep    # RBAC for managed identities + user
```

### Module Dependencies

```mermaid
graph LR
    MAIN[main.bicep] --> LOG[log-analytics]
    LOG --> APPINS[app-insights]
    MAIN --> STORAGE[storage-account]
    LOG --> KV[keyvault]
    LOG --> SPEECH[speech-services]
    MAIN --> AI[ai-foundry]
    MAIN --> SWA[static-web-app]
    
    STORAGE --> FUNC[function-app]
    APPINS --> FUNC
    SWA --> FUNC
    
    FUNC --> RBAC[role-assignments]
    STORAGE --> RBAC
    KV --> RBAC
    SPEECH --> RBAC
    AI --> RBAC
```

### Deployment

See [runbook.md](runbook.md) for detailed deployment instructions.

Quick start:
```powershell
cd infra
.\deploy.ps1 -DeploymentNumber 3
```

---

## Security Architecture

### Authentication & Authorization

```mermaid
graph TB
    subgraph "Function App"
        MI[System-Assigned<br/>Managed Identity]
    end
    
    subgraph "RBAC Role Assignments"
        R1[Storage Blob Data Contributor]
        R2[Storage Queue Data Contributor]
        R3[Storage Table Data Contributor]
        R4[Key Vault Secrets User]
        R5[Cognitive Services User]
        R6[Cognitive Services Contributor]
    end
    
    subgraph "Resources"
        STORAGE[Storage Account]
        KV[Key Vault]
        SPEECH[Speech Services]
        AI[AI Services]
    end
    
    MI --> R1 --> STORAGE
    MI --> R2 --> STORAGE
    MI --> R3 --> STORAGE
    MI --> R4 --> KV
    MI --> R5 --> SPEECH
    MI --> R5 --> AI
    MI --> R6 --> AI
```

### RBAC Role Assignments

| Principal | Resource | Role |
|-----------|----------|------|
| Function App MI | Storage Account | Storage Blob Data Contributor |
| Function App MI | Storage Account | Storage Queue Data Contributor |
| Function App MI | Storage Account | Storage Table Data Contributor |
| Function App MI | Key Vault | Key Vault Secrets User |
| Function App MI | Speech Services | Cognitive Services User |
| Function App MI | AI Services | Cognitive Services User |
| Function App MI | AI Services | Cognitive Services Contributor |
| Function App MI | AI Services | Cognitive Services OpenAI User |
| Function App MI | AI Services | Cognitive Services OpenAI Contributor |
| Foundry Project MI | AI Services | Cognitive Services OpenAI User |
| Foundry Project MI | AI Services | Cognitive Services User |
| Foundry Project MI | Storage Account | Storage Blob Data Contributor |
| Foundry Project MI | Key Vault | Key Vault Secrets User |
| User Principal | Speech Services | Cognitive Services User |
| User Principal | AI Services | Cognitive Services User |
| User Principal | AI Services | Cognitive Services OpenAI User |

**User Principal ID**: `716e5244-7a36-4bef-9fe6-18f8b62f3cce`

### Multi-Agent Architecture (Implemented)

The system uses a **multi-agent architecture** for comprehensive subtitle quality validation with 4 specialized agents running in **parallel**:

```mermaid
graph TB
    subgraph "Multi-Agent Validation System"
        SUP[Orchestrator Agent<br/>Coordinates & Summarizes]
        TRANS[Translation Agent<br/>Semantic Accuracy - 40%]
        TECH[Technical Agent<br/>Timing & Format - 30%]
        CULT[Cultural Agent<br/>Localization - 30%]
        HUMAN[Human-in-the-Loop<br/>Final Approval]
    end
    
    SUP -->|Parallel| TRANS
    SUP -->|Parallel| TECH
    SUP -->|Parallel| CULT
    TRANS -->|Score| SUP
    TECH -->|Score| SUP
    CULT -->|Score| SUP
    SUP -->|Weighted Score| HUMAN
    HUMAN -->|Approve/Reject| SUP
```

| Agent | Model | Weight | Purpose |
|-------|-------|--------|---------|
| **Orchestrator** | GPT-4o-mini | - | Coordinates workflow, aggregates scores, provides summary |
| **Translation Agent** | GPT-4o-mini | 40% | Semantic accuracy, meaning preservation, fluency |
| **Technical Agent** | GPT-4o-mini | 30% | Timing sync, CPS, line length, WebVTT format |
| **Cultural Agent** | GPT-4o-mini | 30% | Cultural adaptation, idioms, appropriateness |
| **Human-in-the-Loop** | - | - | Final review and approval (3-day timeout) |

**Score Thresholds**:
| Score | Recommendation | Action |
|-------|----------------|--------|
| ≥ 80 | Approve | Translation meets quality standards |
| 50-79 | NeedsReview | Human review recommended |
| < 50 | Reject | Translation quality insufficient |

### Key Security Features

- ✅ **No hardcoded secrets** - All access via managed identity
- ✅ **RBAC on Key Vault** - No access policies, pure RBAC
- ✅ **TLS 1.2 minimum** - All services enforce TLS 1.2+
- ✅ **HTTPS only** - Storage and Function App HTTPS-only
- ✅ **Soft delete enabled** - Key Vault with purge protection
- ✅ **Diagnostic logging** - All services log to Log Analytics

### Network Security

The application uses Azure-native security features:

| Resource | Protection | Configuration |
|----------|-----------|---------------|
| **Static Web App** | HTTPS-only | Automatic SSL certificates |
| **Function App** | HTTPS-only | TLS 1.2 minimum |
| **Storage Account** | Managed Identity | Only Function App can access via RBAC |
| **Speech Service** | API Key + MI | Protected by Key Vault, called only by Function App |

```mermaid
graph TB
    subgraph "Internet"
        User[User]
    end
    
    subgraph "Azure Edge"
        SWA[Static Web App<br/>HTTPS Only]
        FA[Function App<br/>HTTPS Only]
    end
    
    subgraph "Internal Services"
        Storage[Storage Account]
        Speech[Speech Services]
    end
    
    User -->|HTTPS| SWA
    SWA -->|API Call| FA
    FA -->|Managed Identity| Storage
    FA -->|API Key| Speech
```

---

## Scalability Considerations

### Azure Functions Scaling

| Plan | Max Instances | Scaling Behavior |
|------|---------------|------------------|
| **Standard (S1)** ✓ | Plan-based | Always-on, manual/auto scale |
| Consumption (Y1) | 200 | Event-driven, scale to zero |
| Premium | 100 | Pre-warmed instances |

**Current Configuration**: Standard S1 (selected due to quota availability)

### Durable Functions Considerations

- **Task Hub Partitioning**: Default 4 partitions, increase for high throughput
- **Entity Concurrency**: Orchestrations are single-threaded per instance
- **History Size**: Implement `ContinueAsNew` for long-running orchestrations
- **Storage Throttling**: Use Premium storage for high-volume scenarios

### Speech Services Limits (S0 Tier)

| Feature | Limit |
|---------|-------|
| Concurrent requests | 100 |
| Audio length | 10 hours per request |
| Real-time factor | Varies by language |

### Recommendations for Production

1. **Use Premium Functions Plan** for predictable latency
2. **Enable auto-scaling** with minimum instance count
3. **Implement circuit breakers** for external service calls
4. **Use CDN** for output file distribution
5. **Consider regional redundancy** for high availability

---

## Future Enhancements

### Network Isolation Options

For production environments requiring enhanced network security, consider these options:

| Option | Complexity | Estimated Effort | Description |
|--------|------------|------------------|-------------|
| **Hybrid (Recommended)** | Medium | ~6 hours | Private endpoints for Storage, Speech, AI Foundry; Function App public with auth |
| **API Management Gateway** | High | 1-2 days | Full private network with APIM as single entry point |
| **App Service Replacement** | Medium | 1 day | Replace SWA with App Service for VNet integration |
| **Front Door + Private Link** | High | 1-2 days | CDN + WAF with private backend connectivity |

**Recommended Approach (Hybrid):**
- Static Web Apps cannot connect to Function Apps via private endpoints (SWA limitation)
- Function App uses VNet Integration to reach private services
- All backend services (Storage, Speech, AI Foundry) accessible only via private endpoints
- DNS zones required for each private endpoint

### Additional Enhancements

| Enhancement | Priority | Description |
|-------------|----------|-------------|
| Agent Consensus Visualization | Medium | Show agreement/disagreement between agents |
| PDF Export | Low | Export validation reports as PDF |
| Agent Performance Dashboard | Medium | Track agent response times and accuracy |
| Webhook Notifications | Medium | Notify external systems on job completion |
| Cost Tracking | Low | Track Azure AI Foundry token usage per job |
| Re-Validation After Edit | High | Re-run validation when subtitles are manually edited |
