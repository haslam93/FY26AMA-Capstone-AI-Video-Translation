# Multi-Agent Subtitle Validation System

## Overview

This document outlines the implementation plan for transforming the single-agent validation system into a true multi-agent architecture with specialized agents that collaborate to provide comprehensive subtitle quality validation.

---

## Phase 1: Agent Definitions & Creation

### 1.1 Agent Specifications

| Agent | Name in Foundry | Model | Purpose |
|-------|-----------------|-------|---------|
| **OrchestratorAgent** | `ValidationOrchestratorAgent` | gpt-4o-mini | Coordinates workflow, delegates to specialists, aggregates results |
| **TranslationReviewAgent** | `TranslationReviewAgent` | gpt-4o-mini | Analyzes semantic accuracy, grammar, fluency |
| **TechnicalReviewAgent** | `TechnicalReviewAgent` | gpt-4o-mini | Analyzes timing, sync, formatting, reading speed |
| **CulturalReviewAgent** | `CulturalReviewAgent` | gpt-4o-mini | Analyzes cultural adaptation, idioms, tone |

### 1.2 Agent Instructions (System Prompts)

**OrchestratorAgent:**
```
You are the lead validator coordinating a team of specialist agents.
Your job is to:
1. Receive subtitle validation requests
2. Prepare context for specialist agents
3. Aggregate their findings into a unified report
4. Provide final recommendation (Approve/Reject/NeedsReview)
5. Answer human reviewer questions using insights from all specialists
```

**TranslationReviewAgent:**
```
You are a professional translator and linguist.
Analyze subtitles for:
- Semantic accuracy (does translation preserve meaning?)
- Grammar and syntax correctness
- Natural fluency in target language
- Completeness (nothing omitted or added?)
Score: 0-100 with detailed reasoning
```

**TechnicalReviewAgent:**
```
You are a subtitle technical specialist.
Analyze subtitles for:
- Timing synchronization with source
- Reading speed (characters per second)
- Line length and breaks
- Duration appropriateness
- Format consistency
Score: 0-100 with detailed reasoning
```

**CulturalReviewAgent:**
```
You are a cultural localization expert.
Analyze subtitles for:
- Idiom and expression adaptation
- Cultural reference handling
- Tone and register appropriateness
- Audience suitability
- Sensitive content handling
Score: 0-100 with detailed reasoning
```

---

## Phase 2: Code Changes

### 2.1 New Files to Create

| File | Purpose |
|------|---------|
| `Services/MultiAgentValidationService.cs` | Main service orchestrating multiple agents |
| `Services/IMultiAgentValidationService.cs` | Interface for the service |
| `Models/MultiAgentValidationResult.cs` | Result model with per-agent scores |

### 2.2 Files to Modify

| File | Changes |
|------|---------|
| `Program.cs` | Register new service |
| `Activities/RunValidationActivity.cs` | Use multi-agent service instead of single agent |
| `Models/TranslationJob.cs` | Store per-agent thread IDs |
| `Functions/TranslationFunctions.cs` | Update chat to route to appropriate agent |

---

## Phase 3: Multi-Agent Workflow

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         VALIDATION WORKFLOW                              │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  Step 1: PREPARATION                                                     │
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │ OrchestratorAgent creates shared context:                          │ │
│  │ • Fetches job info, source subtitles, target subtitles             │ │
│  │ • Prepares validation package for specialists                      │ │
│  └────────────────────────────────────────────────────────────────────┘ │
│                                    │                                     │
│  Step 2: PARALLEL SPECIALIST REVIEW                                      │
│  ┌──────────────────┬──────────────────┬──────────────────┐             │
│  │ TranslationAgent │ TechnicalAgent   │ CulturalAgent    │             │
│  │                  │                  │                  │             │
│  │ Input:           │ Input:           │ Input:           │             │
│  │ • Source VTT     │ • Source VTT     │ • Source VTT     │             │
│  │ • Target VTT     │ • Target VTT     │ • Target VTT     │             │
│  │ • Languages      │ • Languages      │ • Languages      │             │
│  │                  │                  │                  │             │
│  │ Output:          │ Output:          │ Output:          │             │
│  │ • Score (0-100)  │ • Score (0-100)  │ • Score (0-100)  │             │
│  │ • Issues[]       │ • Issues[]       │ • Issues[]       │             │
│  │ • Reasoning      │ • Reasoning      │ • Reasoning      │             │
│  └──────────────────┴──────────────────┴──────────────────┘             │
│                                    │                                     │
│  Step 3: AGGREGATION                                                     │
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │ OrchestratorAgent aggregates results:                              │ │
│  │ • Combines scores (weighted average)                               │ │
│  │ • Merges and deduplicates issues                                   │ │
│  │ • Determines final recommendation                                  │ │
│  │ • Generates human-readable summary                                 │ │
│  └────────────────────────────────────────────────────────────────────┘ │
│                                    │                                     │
│  Step 4: INTERACTIVE REVIEW                                              │
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │ Human asks questions → OrchestratorAgent responds                  │ │
│  │ • Can consult specialist agents for detailed answers               │ │
│  │ • Maintains conversation context                                   │ │
│  └────────────────────────────────────────────────────────────────────┘ │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Phase 4: Data Models

### 4.1 MultiAgentValidationResult

```csharp
public class MultiAgentValidationResult
{
    // Overall aggregated result
    public bool IsValid { get; set; }
    public double OverallScore { get; set; }
    public string FinalRecommendation { get; set; } // Approve/Reject/NeedsReview
    public string Summary { get; set; }
    
    // Per-agent results
    public AgentReviewResult TranslationReview { get; set; }
    public AgentReviewResult TechnicalReview { get; set; }
    public AgentReviewResult CulturalReview { get; set; }
    
    // Thread IDs for follow-up chat
    public string OrchestratorThreadId { get; set; }
    public string TranslationAgentThreadId { get; set; }
    public string TechnicalAgentThreadId { get; set; }
    public string CulturalAgentThreadId { get; set; }
    
    // Combined issues from all agents
    public List<ValidationIssue> AllIssues { get; set; }
}

public class AgentReviewResult
{
    public string AgentName { get; set; }
    public double Score { get; set; }
    public string Reasoning { get; set; }
    public List<ValidationIssue> Issues { get; set; }
    public DateTime ReviewedAt { get; set; }
}
```

---

## Phase 5: UI Updates

### 5.1 JobDetails.razor Changes

```
┌─────────────────────────────────────────────────────────────────────────┐
│ 🤖 Multi-Agent Quality Validation                              [78%]   │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  Agent Scores                                                            │
│  ┌──────────────────┬──────────────────┬──────────────────┐             │
│  │ 📝 Translation   │ ⚙️ Technical      │ 🌍 Cultural      │             │
│  │     75%          │     85%          │     72%          │             │
│  │ "Good accuracy   │ "Timing well     │ "Some idioms     │             │
│  │  but some        │  synchronized,   │  need local      │             │
│  │  nuances lost"   │  good pacing"    │  adaptation"     │             │
│  └──────────────────┴──────────────────┴──────────────────┘             │
│                                                                          │
│  Final Recommendation: ⚠️ NEEDS REVIEW                                   │
│                                                                          │
│  Issues Found (12)                                                       │
│  ├─ 📝 Translation: 5 issues                                            │
│  ├─ ⚙️ Technical: 3 issues                                              │
│  └─ 🌍 Cultural: 4 issues                                               │
│                                                                          │
├─────────────────────────────────────────────────────────────────────────┤
│ 💬 Ask the Agents                                                        │
│  ┌─────────────────────────────────────────────────────────────────┐    │
│  │ [Orchestrator ▼]  "Why did the cultural score drop?"            │    │
│  │                                                            [Send]│    │
│  └─────────────────────────────────────────────────────────────────┘    │
│  Agent selector: Orchestrator | Translation | Technical | Cultural      │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Phase 6: Implementation Steps

| Step | Task | Estimated Effort |
|------|------|------------------|
| 1 | Create `MultiAgentValidationResult` model | 10 min |
| 2 | Create `IMultiAgentValidationService` interface | 5 min |
| 3 | Create `MultiAgentValidationService` implementation | 45 min |
| 4 | Update `TranslationJob` with multi-agent thread IDs | 5 min |
| 5 | Update `RunValidationActivity` to use multi-agent service | 15 min |
| 6 | Update `Program.cs` to register new service | 5 min |
| 7 | Update chat endpoints for agent selection | 20 min |
| 8 | Update UI models for multi-agent results | 10 min |
| 9 | Update `JobDetails.razor` with multi-agent display | 30 min |
| 10 | Build and deploy | 10 min |
| 11 | Test end-to-end | 15 min |

**Total: ~2.5 hours**

---

## Phase 7: API Endpoint Changes

| Endpoint | Change |
|----------|--------|
| `POST /api/jobs/{id}/chat` | Add `agentType` parameter (orchestrator/translation/technical/cultural) |
| `GET /api/jobs/{id}/chat/history` | Add `agentType` parameter |
| `GET /api/jobs/{id}` | Return multi-agent validation results |

---

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        MULTI-AGENT VALIDATION PIPELINE                       │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │                      ORCHESTRATOR AGENT (Supervisor)                   │  │
│  │  • Coordinates the validation pipeline                                 │  │
│  │  • Delegates to specialist agents                                      │  │
│  │  • Aggregates results and makes final recommendation                   │  │
│  │  • Handles conversation context for human reviewer                     │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                                    │                                         │
│              ┌─────────────────────┼─────────────────────┐                  │
│              ▼                     ▼                     ▼                  │
│  ┌───────────────────┐ ┌───────────────────┐ ┌───────────────────┐         │
│  │  TRANSLATION      │ │   TECHNICAL       │ │    CULTURAL       │         │
│  │  REVIEW AGENT     │ │   REVIEW AGENT    │ │    REVIEW AGENT   │         │
│  │                   │ │                   │ │                   │         │
│  │  Expertise:       │ │  Expertise:       │ │  Expertise:       │         │
│  │  • Semantic       │ │  • Timing sync    │ │  • Idioms         │         │
│  │    accuracy       │ │  • Reading speed  │ │  • Local refs     │         │
│  │  • Grammar        │ │  • Line breaks    │ │  • Tone/register  │         │
│  │  • Fluency        │ │  • Char counts    │ │  • Audience fit   │         │
│  │  • Completeness   │ │  • Format/style   │ │  • Sensitivity    │         │
│  └───────────────────┘ └───────────────────┘ └───────────────────┘         │
│              │                     │                     │                  │
│              └─────────────────────┼─────────────────────┘                  │
│                                    ▼                                         │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │                         AGGREGATED RESULTS                             │  │
│  │  • Combined score from all agents (weighted: 40/30/30)                 │  │
│  │  • Categorized issues by specialty                                     │  │
│  │  • Consensus recommendation (Approve/Reject/NeedsReview)               │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Design Decisions

### 1. Parallel vs Sequential Execution
**Decision: Parallel** with `Task.WhenAll()`
- Faster execution (all agents run simultaneously)
- Better user experience (reduced wait time)
- Independent analysis (agents don't influence each other)

### 2. Score Weighting
| Agent | Weight | Rationale |
|-------|--------|-----------|
| Translation | 40% | Core purpose - accuracy is most important |
| Technical | 30% | Ensures watchability and sync |
| Cultural | 30% | Important for localization quality |

### 3. Chat Routing
**Decision: Both options**
- Default to Orchestrator (holistic answers)
- Allow direct agent chat (deep-dive into specialties)

### 4. Data Sharing
**Decision: Orchestrator fetches once, passes to specialists**
- More efficient (single API call for subtitles)
- Consistent context across all agents
- Reduces API costs and latency

---

## Benefits of Multi-Agent Approach

| Benefit | Description |
|---------|-------------|
| **Specialization** | Each agent is an expert in its domain |
| **Parallel Processing** | Agents can run concurrently for faster results |
| **Better Coverage** | Different perspectives catch more issues |
| **Explainability** | Clear which agent found which issue |
| **Scalability** | Easy to add new specialist agents |
| **Maintainability** | Simpler prompts per agent vs one complex prompt |

---

## Future Enhancements

1. **Additional Specialist Agents**
   - `AccessibilityAgent` - SDH compliance, audio descriptions
   - `LegalComplianceAgent` - Censorship, regional regulations
   - `BrandVoiceAgent` - Company style guide adherence

2. **Agent Learning**
   - Store reviewer feedback to improve agent prompts
   - A/B test different agent configurations

3. **Confidence Voting**
   - Agents vote on approval/rejection
   - Weighted consensus for final decision

4. **Agent Disagreement Resolution**
   - Detect when agents disagree significantly
   - Escalate to human reviewer with context
