# Test Plan

## Overview

This document describes the testing strategy for the Video Translation Service, covering unit tests, integration tests, and end-to-end validation of the multi-agent subtitle validation system.

## Unit Tests

### API Tests (`tests/unit/VideoTranslation.Api.Tests`)

| Test Class | Coverage |
|------------|----------|
| `ValidateInputActivityTests` | Input validation logic, video URL/blob path validation |
| `TranslationJobTests` | Model properties, defaults, status transitions |
| `MultiAgentValidationServiceTests` | Agent coordination, score aggregation, weighted calculations |
| `BlobStorageServiceTests` | Blob operations, SAS token generation |

**Run locally:**
```bash
cd tests/unit/VideoTranslation.Api.Tests
dotnet test
```

### UI Tests (`tests/unit/VideoTranslation.UI.Tests`)

| Test Class | Coverage |
|------------|----------|
| `JobModelsTests` | UI model properties, multi-agent result mapping |
| `TranslationApiServiceTests` | API client methods, agent type parameter handling |

**Run locally:**
```bash
cd tests/unit/VideoTranslation.UI.Tests
dotnet test
```

## Integration Tests

### Multi-Agent Validation Tests

| Test Scenario | Description |
|---------------|-------------|
| Parallel Agent Execution | Verify all 3 specialist agents run in parallel |
| Score Aggregation | Verify weighted scoring (40/30/30) calculates correctly |
| Threshold Recommendations | Verify Approve/NeedsReview/Reject thresholds |
| Agent Chat Persistence | Verify thread IDs are stored and retrieved correctly |

### Speech API Integration

| Test Scenario | Description |
|---------------|-------------|
| Create Translation | Verify translation creation with Speech API |
| Create Iteration | Verify iteration processing |
| Status Polling | Verify polling returns correct status |
| Output Copy | Verify outputs are copied to storage correctly |

## End-to-End Tests

### Full Translation Workflow

1. **Upload Video** - Submit video via URL or blob path
2. **Translation Processing** - Monitor status through all states
3. **Multi-Agent Validation** - Verify 4 agents analyze subtitles
4. **Score Display** - Verify UI shows individual agent scores
5. **Human Approval** - Test approve/reject workflow
6. **Output Download** - Verify translated video and subtitles are accessible

### Multi-Agent Validation Flow

```
Input Video → Translation → Subtitles Generated → Multi-Agent Analysis
                                                        ↓
                                    ┌─────────────────────────────────────┐
                                    │      Parallel Agent Execution       │
                                    ├─────────────────────────────────────┤
                                    │ Translation Agent (40%) → Score     │
                                    │ Technical Agent (30%)   → Score     │
                                    │ Cultural Agent (30%)    → Score     │
                                    └─────────────────────────────────────┘
                                                        ↓
                                            Weighted Overall Score
                                                        ↓
                                    ┌─────────────────────────────────────┐
                                    │ ≥80: Approve | 50-79: NeedsReview   │
                                    │ <50: Reject                         │
                                    └─────────────────────────────────────┘
                                                        ↓
                                            Human Approval Gate
```

## Performance Tests

### Agent Response Time

| Metric | Target |
|--------|--------|
| Single Agent Response | < 10 seconds |
| Total Validation (Parallel) | < 15 seconds |
| Chat Response | < 5 seconds |

### Scalability

| Metric | Target |
|--------|--------|
| Concurrent Validations | 10+ |
| Agent Chat Sessions | 100+ concurrent |

## Test Data

### Sample VTT Files

- `tests/data/sample-source.vtt` - Source language WebVTT
- `tests/data/sample-target.vtt` - Target language WebVTT (for validation testing)
- `tests/data/sample-malformed.vtt` - Malformed WebVTT (for error handling)

### Expected Scores

| Scenario | Expected Score Range |
|----------|---------------------|
| High Quality Translation | 85-95 |
| Minor Issues | 60-79 |
| Major Issues | 30-49 |
| Poor Translation | 0-29 |

## Continuous Integration

All tests run automatically on:
- Push to `main` branch
- Pull requests to `main` branch

See `.github/workflows/ci.yml` for CI configuration.
