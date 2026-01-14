# Foundry Agent Service Implementation Progress

> **Branch**: `foundryagents`  
> **Started**: January 13, 2026  
> **Goal**: Replace direct GPT calls with Azure AI Foundry Agent Service

---

## Overview

Convert the current `SubtitleValidationAgent` (which uses direct Chat Completions) to use the Azure AI Foundry Agent Service with:
- **Agents** - Persistent AI assistant with instructions and tools
- **Threads** - Stateful conversation history
- **Runs** - Async execution with tool calling
- **Function Tools** - Agent can call our APIs to fetch data

---

## Phase 1: Infrastructure & Configuration

### 1.1 Update Bicep Templates
- [ ] Add/update AI Foundry account configuration to enable Agent Service
- [ ] Add model deployment for gpt-4o-mini (if not already present)
- [ ] Add role assignments for Function App to access Agent Service
- [ ] Deploy updated infrastructure

### 1.2 Add NuGet Package
- [x] Add `Azure.AI.Projects` NuGet package to `VideoTranslation.Api.csproj` ✅
- [x] Add `Azure.AI.Agents.Persistent` NuGet package (v1.2.0-beta.8) ✅
- [x] Add `Microsoft.Extensions.AI` NuGet package ✅
- [x] Verify package compatibility with .NET 8 ✅

### 1.3 Update App Configuration
- [x] Add `FOUNDRY_PROJECT_ENDPOINT` environment variable (for agent service endpoint) ✅
- [x] Add `FOUNDRY_MODEL_DEPLOYMENT` environment variable (defaults to gpt-4o-mini) ✅
- [x] Add `FOUNDRY_AGENT_NAME` environment variable ✅
- [x] Configure via `FoundryAgentOptions` class ✅

---

## Phase 2: Define Function Tools

### 2.1 Design Tool Schemas
- [x] Define `GetJobInfo` tool schema ✅
- [x] Define `GetSourceSubtitles` tool schema ✅
- [x] Define `GetTargetSubtitles` tool schema ✅

### 2.2 Implement Tool Handlers
- [x] Create `Services/FoundryToolHandler.cs` ✅
- [x] Implement `GetJobInfo()` method ✅
- [x] Implement `GetSourceSubtitles()` method ✅
- [x] Implement `GetTargetSubtitles()` method ✅
- [x] Add error handling for tool calls ✅

---

## Phase 3: Agent Service Implementation

### 3.1 Create FoundryAgentService
- [x] Create `Services/IFoundryAgentService.cs` interface ✅
- [x] Create `Services/FoundryAgentService.cs` implementation ✅
- [x] Implement `EnsureAgentExistsAsync()` - create agent on first run ✅
- [x] Implement `RunValidationAsync()` - execute validation with tool loop ✅
- [x] Implement `SendFollowUpMessageAsync()` - interactive review ✅
- [x] Implement `GetConversationHistoryAsync()` - get conversation history ✅

### 3.2 Tool Call Loop
- [x] Detect `RequiresAction` status from run ✅
- [x] Parse tool call requests (name + arguments) ✅
- [x] Route to appropriate tool handler ✅
- [x] Submit tool outputs back to agent ✅
- [x] Resume run until completion ✅
- [x] Handle errors and timeouts ✅

### 3.3 Agent Definition
- [x] Define agent instructions (system prompt for subtitle validation) ✅
- [x] Define tool definitions (JSON schema for each tool) ✅
- [x] Create agent via SDK on first run (if not exists) ✅
- [x] Cache agent ID for subsequent calls ✅

---

## Phase 4: Integrate with Existing Workflow

### 4.1 Update Models
- [ ] Add `ValidationThreadId` property to `TranslationJob.cs`
- [ ] Add `ThreadId` to `TranslationWorkflowState.cs` (if needed)

### 4.2 Modify RunValidationActivity
- [ ] Replace direct `SubtitleValidationAgent` call with `FoundryAgentService`
- [ ] Create thread for job
- [ ] Run validation agent
- [ ] Store thread ID in job state
- [ ] Parse validation results from agent response

### 4.3 Update Dependency Injection
- [ ] Register `IFoundryAgentService` in `Program.cs`
- [ ] Register `FoundryToolHandler` in `Program.cs`
- [ ] Configure any required options

---

## Phase 5: Interactive Review UI (Option 3)

### 5.1 Add Chat API Endpoints
- [ ] Add `POST /api/jobs/{jobId}/chat` - send message to agent
- [ ] Add `GET /api/jobs/{jobId}/chat` - get conversation history
- [ ] Handle cases where thread doesn't exist

### 5.2 Modify JobDetails.razor
- [ ] Add chat panel component below validation results
- [ ] Display conversation history (messages list)
- [ ] Add input field for reviewer questions
- [ ] Add send button with loading state
- [ ] Scroll to bottom on new messages
- [ ] Style to differentiate user vs assistant messages

### 5.3 Update UI Service
- [ ] Add `SendChatMessageAsync(jobId, message)` to `TranslationApiService.cs`
- [ ] Add `GetChatHistoryAsync(jobId)` to `TranslationApiService.cs`
- [ ] Add response models for chat API

---

## Phase 6: Testing & Debugging

### 6.1 Local Testing
- [ ] Test agent creation on first run
- [ ] Test tool calling flow
- [ ] Test validation with sample VTT files
- [ ] Test interactive chat in UI

### 6.2 Azure Deployment
- [ ] Deploy updated Bicep infrastructure
- [ ] Deploy updated Function App
- [ ] Deploy updated Static Web App
- [ ] Verify agent creation in Azure
- [ ] End-to-end test with real video

### 6.3 Error Handling
- [ ] Handle agent creation failures
- [ ] Handle tool execution failures
- [ ] Handle run timeouts
- [ ] Add appropriate logging

---

## Phase 7: Cleanup & Documentation

- [ ] Remove old `SubtitleValidationAgent.cs` (or keep as fallback)
- [ ] Update `architecture.md` with new agent flow
- [ ] Update `copilot-instructions.md` with new endpoints
- [ ] Update `README.md` with Foundry Agent info
- [ ] Add inline code documentation

---

## File Changes Summary

| File | Status | Description |
|------|--------|-------------|
| `infra/modules/ai-foundry.bicep` | ⬜ Pending | Update for Agent Service |
| `infra/main.bicep` | ⬜ Pending | Include updated module |
| `VideoTranslation.Api.csproj` | ✅ Done | Added Azure.AI.Projects, Azure.AI.Agents.Persistent, Microsoft.Extensions.AI |
| `Program.cs` | ✅ Done | Register FoundryAgentService, FoundryToolHandler, FoundryAgentOptions |
| `Models/TranslationJob.cs` | ⬜ Pending | Add ValidationThreadId |
| **NEW** `Services/IFoundryAgentService.cs` | ✅ Done | Interface for agent operations |
| **NEW** `Services/FoundryAgentService.cs` | ✅ Done | Full implementation with tool handling |
| **NEW** `Services/FoundryToolHandler.cs` | ✅ Done | Tool execution (GetJobInfo, GetSourceSubtitles, GetTargetSubtitles) |
| `Activities/RunValidationActivity.cs` | ⬜ Pending | Use new agent service |
| `Functions/TranslationFunctions.cs` | ⬜ Pending | Add chat endpoints |
| `UI/Pages/JobDetails.razor` | ⬜ Pending | Add chat panel |
| `UI/Services/TranslationApiService.cs` | ⬜ Pending | Add chat methods |

---

## Decisions Made

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Agent Setup Mode | Basic | Avoid Cosmos DB complexity; Azure manages threads |
| Agent Creation | Via SDK on first run | Bicep can't create agent definitions |
| Thread Lifecycle | Keep after approval | Audit trail for reviews |
| Initial Tools | 3 tools | `get_job_info`, `get_source_subtitles`, `get_target_subtitles` |

---

## Notes

- Agent definitions cannot be created via Bicep - they are data plane operations
- Bicep creates the infrastructure (AI Foundry account, project, model deployments)
- Agent is created on first validation run via SDK
- Thread ID is stored with job for later interactive review

---

## Estimated Time

| Phase | Estimate |
|-------|----------|
| Phase 1: Infrastructure | 1 hour |
| Phase 2: Tool Definitions | 1 hour |
| Phase 3: Agent Service | 2-3 hours |
| Phase 4: Workflow Integration | 1 hour |
| Phase 5: Interactive UI | 2-3 hours |
| Phase 6: Testing | 1-2 hours |
| Phase 7: Documentation | 30 min |
| **Total** | **8-12 hours** |

---

*Last Updated: January 14, 2026*

## Completed Work Summary

### Phase 1.2 ✅ NuGet Packages Added
- `Azure.AI.Projects` v1.2.0-beta.5
- `Azure.AI.Agents.Persistent` v1.2.0-beta.8 
- `Microsoft.Extensions.AI` v10.2.0

### Phase 2 ✅ Tool Definitions Complete
- `FoundryToolHandler.cs` with 3 tools: `GetJobInfo`, `GetSourceSubtitles`, `GetTargetSubtitles`
- Tools use job context passed from service for fetching data from blob storage

### Phase 3 ✅ Agent Service Implementation Complete
- `IFoundryAgentService.cs` interface with 4 methods
- `FoundryAgentService.cs` with full implementation:
  - Uses `PersistentAgentsClient` from Azure.AI.Agents.Persistent SDK
  - Creates agent on first run with instructions and tool definitions
  - Handles tool call loop (RequiresAction → execute tools → submit outputs)
  - Parses validation response from agent's JSON output
  - Supports interactive follow-up via threads
- `FoundryAgentOptions` class for configuration
- Registered in `Program.cs` with conditional loading

### Environment Variables Required
```
FOUNDRY_PROJECT_ENDPOINT=https://<account>.services.ai.azure.com/api/projects/<project-name>
FOUNDRY_MODEL_DEPLOYMENT=gpt-4o-mini
FOUNDRY_AGENT_NAME=SubtitleValidationAgent
```
