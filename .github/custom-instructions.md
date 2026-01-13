# Custom Instructions for GitHub Copilot

## Project Overview
This is a video translation service built on Azure Durable Functions. The system accepts video files, translates speech using Azure Speech API, generates/cleans subtitles, and returns translated video content.

## Technology Stack
- **Runtime**: Azure Functions (Durable Functions)
- **Language**: C# / .NET
- **Infrastructure**: Azure Bicep
- **CI/CD**: GitHub Actions

---

## Directory Structure

### `/src/Api`
HTTP-triggered Azure Functions that serve as the public API layer.
- **Submit endpoint**: Accepts video upload requests, validates input, and starts the orchestration
- **Status endpoint**: Returns the current state of a translation job by orchestration ID
- All API responses should follow consistent error models from `/src/Shared`

### `/src/Orchestrator`
Durable Functions orchestrations that manage the workflow state machine.
- Coordinates the entire translation pipeline
- Handles retries, timeouts, and compensation logic
- Implements the saga pattern for long-running operations
- Should be idempotent and deterministic (Durable Functions requirement)

### `/src/Workers`
Activity functions that perform the actual work. Each subfolder is a logical grouping:

#### `/src/Workers/SpeechVideoTranslation`
- Wrapper client for Azure Speech Video Translation API
- Handles authentication and API calls to Azure Speech services
- Manages translation job submission and polling

#### `/src/Workers/Subtitles`
- WebVTT file parsing and generation
- Subtitle cleanup and formatting
- Glossary/terminology handling for domain-specific translations
- Timing adjustments and synchronization

#### `/src/Workers/Storage`
- Azure Blob Storage operations
- SAS token generation for secure client uploads/downloads
- File management (upload, download, delete, copy)
- Container management

### `/src/Shared`
Cross-cutting concerns and shared code:
- **Contracts**: Request/response DTOs and API models
- **Telemetry**: Application Insights integration, custom metrics, and tracing
- **Error Models**: Standardized error responses and exception types
- **Constants**: Configuration keys, magic strings, and enums

### `/tests/unit`
Unit tests for individual components:
- Mock external dependencies
- Test business logic in isolation
- Follow naming convention: `{ClassName}Tests.cs`

### `/tests/integration`
Integration tests that verify component interactions:
- Test against real or emulated Azure services
- Verify end-to-end workflows
- May require test configuration/secrets

### `/infra`
Infrastructure as Code using Azure Bicep:
- **main.bicep**: Root template that deploys all resources
- **monitoring.bicep**: Application Insights, Log Analytics, alerts, and dashboards
- Follow Azure naming conventions and use parameters for environment-specific values

### `/docs`
Project documentation:
- **architecture.md**: System design, component diagrams, data flow
- **adr/**: Architecture Decision Records (use template: `NNNN-title.md`)
- **runbook.md**: Operational procedures, deployment, troubleshooting
- **test-plan.md**: Testing strategy and test case documentation
- **demo-script.md**: Step-by-step demo instructions

### `/.github/workflows`
GitHub Actions CI/CD pipelines:
- **ci.yml**: Build, test, and validate on PR/push
- **cd.yml**: Deploy to Azure environments

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
- Use the error models from `/src/Shared`
- Include correlation IDs in all error responses
- Log errors with full context before returning

### Telemetry
- Track custom events for business metrics
- Use dependency tracking for external calls
- Include operation context for distributed tracing
