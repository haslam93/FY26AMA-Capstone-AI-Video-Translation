# Video Translation Service - Implementation Progress

## 📋 Project Overview

Building a video translation service with:
- **Backend**: Azure Durable Functions (.NET 8 Isolated)
- **Frontend**: Blazor WebAssembly on Azure Static Web App
- **API**: Azure Speech Video Translation API

## 🔑 Deployment Info

| Item | Value |
|------|-------|
| **Resource Group** | `AMAFY26-deployment-3` |
| **Function App** | `FuncApp-AMA-3` |
| **Static Web App** | `SWA-AMA-3` |
| **Speech Service** | `Speech-AMA-3` |
| **Storage Account** | `storageama3` |
| **SWA Deployment Token** | `bf8a6c644a2c8baf3ca9d7640974676f497a69912b437eb919404fa65426a34d01-bdf43284-203a-45d2-8fc2-e678ed0acb5000f00120400c0b0f` |

## 🎯 Implementation Phases

### Phase 1: Project Setup ✅ Complete
- [x] Create .NET 8 Isolated Function App project (`src/api/`)
- [x] Add required NuGet packages
- [x] Configure project settings (host.json, local.settings.json)
- [x] Create folder structure (Models, Services, Activities, Orchestration, Functions)
- [ ] Create Blazor WebAssembly project (`src/ui/`)

### Phase 2: Models & Services ✅ Complete
- [x] Create Speech API request/response models
- [x] Create `ISpeechTranslationService` interface and implementation
- [x] Create `IBlobStorageService` interface and implementation
- [x] Create job DTOs and domain models

### Phase 3: Activity Functions ✅ Complete
- [x] `ValidateInputActivity` - Validate video input
- [x] `CreateTranslationActivity` - Create translation via Speech API
- [x] `CreateIterationActivity` - Create/start iteration
- [x] `GetIterationStatusActivity` - Poll iteration status
- [x] `CopyOutputsActivity` - Copy outputs to storage

### Phase 4: Orchestrator ✅ Complete
- [x] Create `VideoTranslationOrchestrator`
- [x] Implement state machine (Submitted → Processing → Completed)
- [x] Add polling logic with 30-second intervals
- [x] Add 60-minute timeout
- [x] Add retry policies (exponential backoff)

### Phase 5: API Endpoints ✅ Complete
- [x] `POST /api/jobs` - Create new translation job
- [x] `GET /api/jobs/{jobId}` - Get job status
- [x] `GET /api/jobs` - List all jobs
- [x] `POST /api/jobs/{jobId}/iterate` - Start new iteration (placeholder)

### Phase 6: Blazor UI ✅ Complete
- [x] Create project structure and shared components
- [x] Dashboard page (list all jobs)
- [x] Create job page (video upload, language selection)
- [x] Job Details page (status, outputs, iterate)
- [x] Configure API client to call Function App
- [x] Add file upload feature (uploads to blob storage)
- [x] Add 120+ source languages and 60+ target languages (based on Azure Speech API docs)

### Phase 7: Azure Deployment ✅ Complete
- [x] Azure infrastructure deployed (AMAFY26-deployment-3)
- [x] Configure Function App settings in Azure (identity-based storage auth)
- [x] Deploy Function App to Azure
- [x] Configure Static Web App with Function App URL
- [x] Deploy Blazor UI to Static Web App
- [x] End-to-end testing with real Speech API

### Phase 8: Bug Fixes & Enhancements ✅ Complete
- [x] Fixed JobStatusResponse.Error type mismatch (string vs object)
- [x] Removed invalid "auto" source locale option
- [x] Added comprehensive language support (Arabic, Hindi, Thai, Vietnamese, etc.)
- [x] Updated API validation to support all Azure Speech Video Translation locales

---

## 📝 Current Task

**Status**: Adding Subtitle Features 🔄

### Phase 9: Subtitle Enhancements (In Progress)

**Goal**: Add subtitle support - both as downloadable WebVTT files and burned into video

**Tasks**:
- [x] A) Check logs to verify what subtitle URLs were returned from completed job ✅
  - **Finding**: Subtitles ARE being stored! Found in blob storage:
    - `subtitles/7a1b14e65615/iteration-1/source-subtitles.vtt` (408 bytes)
    - `subtitles/7a1b14e65615/iteration-1/target-subtitles.vtt` (568 bytes)
    - `subtitles/7a1b14e65615/iteration-1/metadata.json` (2162 bytes)
  - Issue is the **UI not displaying them**, not the API
- [x] B) Update UI to display subtitle download links (source & target WebVTT) ✅
  - Fixed property name mismatch: `SourceWebVttUrl` → `SourceSubtitleUrl`, `TargetWebVttUrl` → `TargetSubtitleUrl`
  - Updated JobDetails.razor to use correct property names
- [x] C) Enable `exportSubtitleInVideo` option to burn subtitles into video ✅
  - API already has `ExportSubtitleInVideo` in `TranslationJobRequest`
  - Orchestrator already passes it to `CreateIterationActivity`
  - Activity already passes it to Speech API
- [x] D) Add checkbox in Create page for "Burn subtitles into video" ✅
  - Added checkbox to Create.razor with label and helper text
  - Added "Max Characters Per Subtitle Line" input field
- [x] E) Deploy and test ✅
  - API deployed to FuncApp-AMA-3
  - UI deployed to https://ashy-glacier-0400c0b0f.1.azurestaticapps.net

### Phase 9: Subtitle Enhancements ✅ Complete

**Azure Resources**:
| Resource | Name | URL |
|----------|------|-----|
| Function App | FuncApp-AMA-3 | https://funcapp-ama-3.azurewebsites.net |
| Static Web App | SWA-AMA-3 | https://ashy-glacier-0400c0b0f.1.azurestaticapps.net |
| Speech Service | Speech-AMA-3 | https://speech-ama-3.cognitiveservices.azure.com |
| Storage Account | storageama3 | - |

### Completed Phases

#### Phase 8: DevOps Setup ✅ Complete
- [x] Created CI workflow (ci.yml) - validates Bicep, builds & tests API and UI
- [x] Created CD Infrastructure workflow (cd-infra.yml) - deploys Bicep templates
- [x] Created CD Application workflow (cd-app.yml) - deploys API & UI
- [x] Added unit tests for API (15 tests) and UI (11 tests)
- [x] Created devops.md documentation
- [x] Pushed to GitHub: https://github.com/haslam93/FY26AMA-Capstone-AI-Video-Translation
- [x] Fixed CI Bicep validation (use Bicep CLI directly instead of Azure CLI)

#### Previous Phases ✅
- Phase 1-7: Core application built and deployed
- First successful video translation completed! 🎉

**Next Action**: Check Application Insights logs for subtitle URLs

---

## 🔗 API Reference

**Base URL**: `https://eastus2.api.cognitive.microsoft.com/videotranslation`  
**API Version**: `2025-05-20`

### Authentication
- Header: `Ocp-Apim-Subscription-Key: {SpeechResourceKey}`
- Header: `Operation-Id: {unique-operation-id}` (for PUT operations)

### Endpoints

| Operation | Method | Path |
|-----------|--------|------|
| Create translation | PUT | `/translations/{translationId}?api-version=2025-05-20` |
| Get translation | GET | `/translations/{translationId}?api-version=2025-05-20` |
| List translations | GET | `/translations?api-version=2025-05-20` |
| Delete translation | DELETE | `/translations/{translationId}?api-version=2025-05-20` |
| Create iteration | PUT | `/translations/{translationId}/iterations/{iterationId}?api-version=2025-05-20` |
| Get iteration | GET | `/translations/{translationId}/iterations/{iterationId}?api-version=2025-05-20` |
| List iterations | GET | `/translations/{translationId}/iterations?api-version=2025-05-20` |
| Get operation status | GET | `/operations/{operationId}?api-version=2025-05-20` |

### Create Translation Request Body
```json
{
  "displayName": "My translation",
  "description": "Translation description",
  "input": {
    "sourceLocale": "es-ES",
    "targetLocale": "en-US",
    "voiceKind": "PlatformVoice",
    "speakerCount": 1,
    "subtitleMaxCharCountPerSegment": 50,
    "exportSubtitleInVideo": false,
    "enableLipSync": false,
    "videoFileUrl": "https://..."
  }
}
```

### Create Iteration Request Body (first)
```json
{
  "input": {
    "speakerCount": 1,
    "subtitleMaxCharCountPerSegment": 30,
    "exportSubtitleInVideo": true
  }
}
```

### Create Iteration Request Body (subsequent - with WebVTT)
```json
{
  "input": {
    "webvttFile": {
      "url": "https://your-storage/edited.vtt"
    }
  }
}
```

### Iteration Result (Succeeded)
```json
{
  "input": { ... },
  "result": {
    "translatedVideoFileUrl": "https://...",
    "sourceLocaleSubtitleWebvttFileUrl": "https://...",
    "targetLocaleSubtitleWebvttFileUrl": "https://...",
    "metadataJsonWebvttFileUrl": "https://..."
  },
  "status": "Succeeded",
  "id": "iteration-id",
  "createdDateTime": "2025-03-06T19:15:38.723Z"
}
```

### Status Values
- `NotStarted` → `Running` → `Succeeded` | `Failed`

---

## 📊 Progress Log

| Date | Phase | Task | Status |
|------|-------|------|--------|
| 2026-01-12 | Setup | Created progress.md | ✅ Done |
| 2026-01-12 | Setup | Deployed Azure infrastructure | ✅ Done |
| 2026-01-12 | Dev | Implemented Function App backend | ✅ Done |
| 2026-01-12 | Dev | Implemented Blazor WebAssembly UI | ✅ Done |
| 2026-01-12 | Dev | Added file upload feature | ✅ Done |
| 2026-01-12 | Deploy | Deploy Function App to Azure | ✅ Done |
| 2026-01-12 | Deploy | Deploy UI to Static Web App | ✅ Done |
| 2026-01-12 | Fix | Fixed Error type mismatch in JobStatusResponse | ✅ Done |
| 2026-01-12 | Fix | Removed invalid "auto" source locale | ✅ Done |
| 2026-01-12 | Enhancement | Added 120+ source and 60+ target languages | ✅ Done |

---

## ⚠️ Notes & Issues

- See `docs/issuesfoundandresolved.md` for debugging session details
- Local testing requires Azurite for storage emulation
- Speech API requires Azure Blob Storage SAS URLs (not public URLs)
- Function App uses Managed Identity for Azure authentication

