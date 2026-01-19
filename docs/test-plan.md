# Test Plan

## Overview

This document describes the comprehensive testing strategy for the Video Translation Service, covering unit tests, integration tests, and end-to-end validation of the multi-agent subtitle validation system.

## Table of Contents

- [Test Architecture](#test-architecture)
- [Unit Tests](#unit-tests)
- [Integration Tests](#integration-tests)
- [End-to-End Tests](#end-to-end-tests)
- [Performance Tests](#performance-tests)
- [Test Data](#test-data)
- [Running Tests](#running-tests)
- [Continuous Integration](#continuous-integration)
- [Test Coverage Goals](#test-coverage-goals)

---

## Test Architecture

```
tests/
├── unit/
│   ├── VideoTranslation.Api.Tests/      # Backend unit tests
│   │   ├── Activities/                   # Activity function tests
│   │   ├── Models/                       # Model/DTO tests
│   │   └── Services/                     # Service tests
│   └── VideoTranslation.UI.Tests/       # Frontend unit tests
│       └── Models/                       # UI model tests
├── integration/
│   └── VideoTranslation.Integration.Tests/  # Integration tests
└── data/                                 # Test data files
    ├── sample-source.vtt
    ├── sample-target.vtt
    ├── sample-high-quality.vtt
    ├── sample-low-quality.vtt
    └── sample-malformed.vtt
```

---

## Unit Tests

### API Tests (`tests/unit/VideoTranslation.Api.Tests`)

| Test Class | Test Count | Coverage |
|------------|------------|----------|
| `ValidateInputActivityTests` | 7 | Input validation, URL/blob path, locale validation |
| `TranslationJobTests` | 16 | Model properties, status transitions, approval workflow |
| `MultiAgentValidationResultTests` | 25 | Score calculation, thresholds, issue merging |
| `BlobStorageServiceTests` | 12 | Options config, interface contracts |

#### MultiAgentValidationResultTests Details

| Test Category | Tests |
|---------------|-------|
| **Score Calculation** | Weighted average (40/30/30), null handling, mixed values |
| **Threshold Logic** | Approve (≥80), NeedsReview (50-79), Reject (<50), boundary cases |
| **Issue Merging** | Combine from all agents, clear before merge, handle nulls |
| **Agent Review** | Default values, property setting, thread IDs |
| **Multi-Agent Issue** | Severity levels, categories, location/suggestion |

#### TranslationJobTests (Enhanced)

| Test Category | Tests |
|---------------|-------|
| **Basic Properties** | Default status, timestamps, iteration number |
| **Multi-Agent** | Validation result attachment, status values |
| **Approval Workflow** | Decision tracking, timestamp management |
| **Result Objects** | TranslationResult, StoredOutputs URLs |

### UI Tests (`tests/unit/VideoTranslation.UI.Tests`)

| Test Class | Test Count | Coverage |
|------------|------------|----------|
| `JobModelsTests` | 24 | UI models, multi-agent results, chat, approval |

#### JobModelsTests Details

| Test Category | Tests |
|---------------|-------|
| **CreateJobRequest** | Defaults, property setting |
| **JobStatusResponse** | HasMultiAgentValidation, all properties |
| **MultiAgentValidationResult** | Defaults, agent reviews, thread IDs |
| **AgentReviewResult** | Properties, issues list |
| **Chat/Approval** | ChatRequest, ConversationMessage, ApprovalDecision |
| **ValidationIssue** | SeverityText, CategoryText display helpers |

---

## Integration Tests

### Project: `tests/integration/VideoTranslation.Integration.Tests`

| Test Class | Test Count | Coverage |
|------------|------------|----------|
| `MultiAgentValidationIntegrationTests` | 12 | Full validation pipeline scenarios |
| `TranslationWorkflowIntegrationTests` | 14 | Job lifecycle, status transitions |

#### MultiAgentValidationIntegrationTests

| Test Scenario | Description |
|---------------|-------------|
| **High Quality Pipeline** | Translation 92%, Technical 88%, Cultural 85% → Approve (88.7) |
| **Medium Quality Pipeline** | Translation 70%, Technical 85%, Cultural 65% → NeedsReview (73) |
| **Low Quality Pipeline** | Translation 35%, Technical 60%, Cultural 40% → Reject (44) |
| **Boundary Thresholds** | Test exact 80, 79.9, 50, 49.9 boundaries |
| **Issue Aggregation** | Category counts, severity counts, merge behavior |
| **Thread ID Management** | Unique thread IDs for all 4 agents |

#### TranslationWorkflowIntegrationTests

| Test Scenario | Description |
|---------------|-------------|
| **Full Workflow** | Submitted → Validating → Processing → Validation → Approval → Complete |
| **Rejection Workflow** | PendingApproval → Rejected with reason |
| **Failure Scenario** | Processing → Failed with error message |
| **Job Lifecycle** | New job defaults, multi-agent attachment, timestamps |
| **Request Validation** | Required fields, language pairs, voice kinds |
| **Result Objects** | Output URLs, stored outputs |

---

## End-to-End Tests

### Full Translation Workflow

```
Upload Video → Create Job → Translation Processing → Subtitles Generated
                                                           ↓
                                                  Multi-Agent Validation
                                                           ↓
                                                    Human Approval
                                                           ↓
                                                  Completed/Rejected
```

### Test Scenarios

| Scenario | Steps | Expected Outcome |
|----------|-------|------------------|
| **Happy Path** | Upload → Translate → Validate → Approve | Status: Completed |
| **Rejection Flow** | Upload → Translate → Validate → Reject | Status: Rejected |
| **Re-Iteration** | Completed job → New iteration | New translation with incremented iteration |
| **Agent Chat** | Validation complete → Chat with Translation agent | Contextual response |
| **Timeout** | Job in PendingApproval for 3+ days | Auto-reject |

### Multi-Agent Validation Flow

```
Input Video → Translation → Subtitles Generated → Multi-Agent Analysis
                                                        ↓
                                    ┌─────────────────────────────────────┐
                                    │      Parallel Agent Execution       │
                                    │      (Task.WhenAll)                 │
                                    ├─────────────────────────────────────┤
                                    │ Translation Agent (40%) → Score     │
                                    │ Technical Agent (30%)   → Score     │
                                    │ Cultural Agent (30%)    → Score     │
                                    └─────────────────────────────────────┘
                                                        ↓
                                            Orchestrator Agent
                                            (Aggregate & Summarize)
                                                        ↓
                                    ┌─────────────────────────────────────┐
                                    │ ≥80: Approve | 50-79: NeedsReview   │
                                    │ <50: Reject                         │
                                    └─────────────────────────────────────┘
                                                        ↓
                                            Human Approval Gate
                                            (3-day timeout)
```

---

## Performance Tests

### Agent Response Time

| Metric | Target | Measurement |
|--------|--------|-------------|
| Single Agent Response | < 10 seconds | Time from prompt to score |
| Total Validation (Parallel) | < 15 seconds | All 3 agents + orchestrator |
| Chat Response | < 5 seconds | Follow-up question response |
| Score Calculation | < 100ms | Local weighted average |

### Scalability

| Metric | Target | Notes |
|--------|--------|-------|
| Concurrent Validations | 10+ | Limited by AI Foundry quota |
| Agent Chat Sessions | 100+ | Thread-based, stateless |
| Storage Operations | 50 concurrent | SAS token generation |

---

## Test Data

### Sample VTT Files

| File | Purpose | Language |
|------|---------|----------|
| `sample-source.vtt` | Source subtitle baseline | English (en-US) |
| `sample-target.vtt` | Valid target translation | Spanish (es-ES) |
| `sample-high-quality.vtt` | High score scenario (85+) | Arabic (ar-EG) |
| `sample-low-quality.vtt` | Low score scenario (<50) | Poor English |
| `sample-malformed.vtt` | Error handling tests | Invalid format |

### Expected Scores by Scenario

| Scenario | Translation | Technical | Cultural | Overall | Recommendation |
|----------|-------------|-----------|----------|---------|----------------|
| Excellent | 92 | 88 | 85 | 88.7 | Approve |
| Good | 80 | 80 | 80 | 80.0 | Approve |
| Needs Review | 70 | 85 | 65 | 73.0 | NeedsReview |
| Poor | 35 | 60 | 40 | 44.0 | Reject |

### Test Language Pairs

| Source | Target | Notes |
|--------|--------|-------|
| en-US | es-ES | Common European |
| en-US | ar-EG | RTL language |
| ja-JP | en-US | Asian to English |
| zh-CN | en-US | Complex characters |
| fr-FR | de-DE | European pair |

---

## Running Tests

### All Tests

```powershell
# From solution root
dotnet test Capstone.sln
```

### Unit Tests Only

```powershell
# API tests
cd tests/unit/VideoTranslation.Api.Tests
dotnet test

# UI tests
cd tests/unit/VideoTranslation.UI.Tests
dotnet test
```

### Integration Tests Only

```powershell
cd tests/integration/VideoTranslation.Integration.Tests
dotnet test
```

### With Coverage

```powershell
dotnet test --collect:"XPlat Code Coverage"
```

### Filter by Category

```powershell
# Run only multi-agent tests
dotnet test --filter "FullyQualifiedName~MultiAgent"

# Run only approval workflow tests
dotnet test --filter "FullyQualifiedName~Approval"
```

### Live Tests (Requires Azure Resources)

```powershell
# Enable live tests by setting environment variable
$env:TestSettings__SkipLiveTests = "false"
$env:SpeechService__Endpoint = "https://speech-ama-3.cognitiveservices.azure.com"
$env:BlobStorage__AccountName = "storageama3"

dotnet test --filter "Category=Live"
```

---

## Continuous Integration

### GitHub Actions Workflow

Tests run automatically on:
- ✅ Push to `main` branch
- ✅ Pull requests to `main` branch
- ✅ Manual workflow dispatch

### CI Pipeline Steps

```yaml
- name: Run Unit Tests
  run: dotnet test tests/unit/**/*.csproj --no-build

- name: Run Integration Tests
  run: dotnet test tests/integration/**/*.csproj --no-build

- name: Upload Coverage
  uses: codecov/codecov-action@v3
```

See `.github/workflows/ci.yml` for full configuration.

---

## Test Coverage Goals

### Target Coverage

| Component | Target | Current |
|-----------|--------|---------|
| API Models | 90% | ~85% |
| API Services | 70% | ~60% |
| API Activities | 80% | ~75% |
| UI Models | 90% | ~90% |
| Overall | 75% | ~70% |

### Coverage by Feature

| Feature | Unit Tests | Integration Tests |
|---------|------------|-------------------|
| Multi-Agent Validation | ✅ 25 tests | ✅ 12 tests |
| Score Calculation | ✅ 10 tests | ✅ 6 tests |
| Approval Workflow | ✅ 8 tests | ✅ 4 tests |
| Job Lifecycle | ✅ 16 tests | ✅ 14 tests |
| Blob Storage | ✅ 12 tests | 🔄 Planned |

### Excluded from Coverage

- External SDK interactions (Azure.AI.Agents.Persistent)
- Azure Storage SDK direct calls
- HTTP trigger functions (tested via integration)

---

## Test Maintenance

### Adding New Tests

1. Create test class in appropriate folder
2. Follow naming convention: `{ClassName}Tests.cs`
3. Use `[Fact]` for single tests, `[Theory]` for parameterized
4. Include Arrange/Act/Assert comments
5. Update this document with new test counts

### Test Data Updates

When adding new VTT test files:
1. Add to `tests/data/` folder
2. Ensure proper WebVTT format
3. Document expected scores in this plan
4. Update `.csproj` to copy files to output

---

*Last Updated: January 2026*
*Test Count: ~80 unit tests, ~26 integration tests*
