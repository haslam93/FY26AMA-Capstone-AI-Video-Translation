"""
Video Translation Service - Capstone Presentation Generator
Run this script to generate a PowerPoint presentation.

Requirements:
    pip install python-pptx

Usage:
    python create_presentation.py
"""

from pptx import Presentation
from pptx.util import Inches, Pt
from pptx.dml.color import RGBColor
from pptx.enum.text import PP_ALIGN, MSO_ANCHOR
from pptx.enum.shapes import MSO_SHAPE

# Color scheme (Microsoft Azure colors)
AZURE_BLUE = RGBColor(0, 120, 212)
AZURE_DARK = RGBColor(0, 78, 152)
AZURE_LIGHT = RGBColor(80, 230, 255)
WHITE = RGBColor(255, 255, 255)
DARK_GRAY = RGBColor(50, 50, 50)
LIGHT_GRAY = RGBColor(240, 240, 240)
SUCCESS_GREEN = RGBColor(16, 124, 16)
ACCENT_ORANGE = RGBColor(255, 140, 0)


def add_title_slide(prs, title, subtitle):
    """Add a title slide with gradient-like styling"""
    slide_layout = prs.slide_layouts[6]  # Blank layout
    slide = prs.slides.add_slide(slide_layout)
    
    # Add background shape
    shape = slide.shapes.add_shape(
        MSO_SHAPE.RECTANGLE, 0, 0, prs.slide_width, prs.slide_height
    )
    shape.fill.solid()
    shape.fill.fore_color.rgb = AZURE_BLUE
    shape.line.fill.background()
    
    # Add title
    title_box = slide.shapes.add_textbox(
        Inches(0.5), Inches(2.5), Inches(9), Inches(1.5)
    )
    tf = title_box.text_frame
    p = tf.paragraphs[0]
    p.text = title
    p.font.size = Pt(44)
    p.font.bold = True
    p.font.color.rgb = WHITE
    p.alignment = PP_ALIGN.CENTER
    
    # Add subtitle
    sub_box = slide.shapes.add_textbox(
        Inches(0.5), Inches(4), Inches(9), Inches(1)
    )
    tf = sub_box.text_frame
    p = tf.paragraphs[0]
    p.text = subtitle
    p.font.size = Pt(24)
    p.font.color.rgb = WHITE
    p.alignment = PP_ALIGN.CENTER
    
    return slide


def add_section_slide(prs, section_title, section_number=None):
    """Add a section divider slide"""
    slide_layout = prs.slide_layouts[6]
    slide = prs.slides.add_slide(slide_layout)
    
    # Add accent bar
    bar = slide.shapes.add_shape(
        MSO_SHAPE.RECTANGLE, 0, Inches(2.8), Inches(10), Inches(2)
    )
    bar.fill.solid()
    bar.fill.fore_color.rgb = AZURE_BLUE
    bar.line.fill.background()
    
    # Add section number if provided
    if section_number:
        num_box = slide.shapes.add_textbox(
            Inches(0.5), Inches(2.9), Inches(1), Inches(0.8)
        )
        tf = num_box.text_frame
        p = tf.paragraphs[0]
        p.text = f"{section_number:02d}"
        p.font.size = Pt(48)
        p.font.bold = True
        p.font.color.rgb = WHITE
    
    # Add section title
    title_box = slide.shapes.add_textbox(
        Inches(1.8) if section_number else Inches(0.5),
        Inches(3.0), Inches(8), Inches(1.5)
    )
    tf = title_box.text_frame
    p = tf.paragraphs[0]
    p.text = section_title
    p.font.size = Pt(40)
    p.font.bold = True
    p.font.color.rgb = WHITE
    
    return slide


def add_content_slide(prs, title, bullet_points, notes=None):
    """Add a content slide with bullet points"""
    slide_layout = prs.slide_layouts[6]
    slide = prs.slides.add_slide(slide_layout)
    
    # Add header bar
    header = slide.shapes.add_shape(
        MSO_SHAPE.RECTANGLE, 0, 0, prs.slide_width, Inches(1.2)
    )
    header.fill.solid()
    header.fill.fore_color.rgb = AZURE_BLUE
    header.line.fill.background()
    
    # Add title
    title_box = slide.shapes.add_textbox(
        Inches(0.5), Inches(0.3), Inches(9), Inches(0.7)
    )
    tf = title_box.text_frame
    p = tf.paragraphs[0]
    p.text = title
    p.font.size = Pt(32)
    p.font.bold = True
    p.font.color.rgb = WHITE
    
    # Add bullet points
    content_box = slide.shapes.add_textbox(
        Inches(0.5), Inches(1.5), Inches(9), Inches(5.5)
    )
    tf = content_box.text_frame
    tf.word_wrap = True
    
    for i, point in enumerate(bullet_points):
        if i == 0:
            p = tf.paragraphs[0]
        else:
            p = tf.add_paragraph()
        
        # Handle nested bullets
        if isinstance(point, tuple):
            text, level = point
            p.text = "• " + text
            p.level = level
            p.font.size = Pt(18 if level > 0 else 20)
            if level > 0:
                p.space_before = Pt(4)
        else:
            p.text = "• " + point
            p.font.size = Pt(20)
        
        p.font.color.rgb = DARK_GRAY
        p.space_after = Pt(8)
    
    # Add speaker notes if provided
    if notes:
        notes_slide = slide.notes_slide
        notes_slide.notes_text_frame.text = notes
    
    return slide


def add_two_column_slide(prs, title, left_content, right_content, left_title="", right_title=""):
    """Add a two-column content slide"""
    slide_layout = prs.slide_layouts[6]
    slide = prs.slides.add_slide(slide_layout)
    
    # Add header bar
    header = slide.shapes.add_shape(
        MSO_SHAPE.RECTANGLE, 0, 0, prs.slide_width, Inches(1.2)
    )
    header.fill.solid()
    header.fill.fore_color.rgb = AZURE_BLUE
    header.line.fill.background()
    
    # Add title
    title_box = slide.shapes.add_textbox(
        Inches(0.5), Inches(0.3), Inches(9), Inches(0.7)
    )
    tf = title_box.text_frame
    p = tf.paragraphs[0]
    p.text = title
    p.font.size = Pt(32)
    p.font.bold = True
    p.font.color.rgb = WHITE
    
    # Left column title
    if left_title:
        left_title_box = slide.shapes.add_textbox(
            Inches(0.3), Inches(1.4), Inches(4.5), Inches(0.5)
        )
        tf = left_title_box.text_frame
        p = tf.paragraphs[0]
        p.text = left_title
        p.font.size = Pt(22)
        p.font.bold = True
        p.font.color.rgb = AZURE_DARK
    
    # Left column content
    left_box = slide.shapes.add_textbox(
        Inches(0.3), Inches(1.9) if left_title else Inches(1.5), Inches(4.5), Inches(5)
    )
    tf = left_box.text_frame
    tf.word_wrap = True
    for i, point in enumerate(left_content):
        if i == 0:
            p = tf.paragraphs[0]
        else:
            p = tf.add_paragraph()
        p.text = "• " + point
        p.font.size = Pt(18)
        p.font.color.rgb = DARK_GRAY
        p.space_after = Pt(6)
    
    # Right column title
    if right_title:
        right_title_box = slide.shapes.add_textbox(
            Inches(5.2), Inches(1.4), Inches(4.5), Inches(0.5)
        )
        tf = right_title_box.text_frame
        p = tf.paragraphs[0]
        p.text = right_title
        p.font.size = Pt(22)
        p.font.bold = True
        p.font.color.rgb = AZURE_DARK
    
    # Right column content
    right_box = slide.shapes.add_textbox(
        Inches(5.2), Inches(1.9) if right_title else Inches(1.5), Inches(4.5), Inches(5)
    )
    tf = right_box.text_frame
    tf.word_wrap = True
    for i, point in enumerate(right_content):
        if i == 0:
            p = tf.paragraphs[0]
        else:
            p = tf.add_paragraph()
        p.text = "• " + point
        p.font.size = Pt(18)
        p.font.color.rgb = DARK_GRAY
        p.space_after = Pt(6)
    
    return slide


def add_architecture_slide(prs):
    """Add architecture diagram slide (simplified representation)"""
    slide_layout = prs.slide_layouts[6]
    slide = prs.slides.add_slide(slide_layout)
    
    # Add header
    header = slide.shapes.add_shape(
        MSO_SHAPE.RECTANGLE, 0, 0, prs.slide_width, Inches(1.2)
    )
    header.fill.solid()
    header.fill.fore_color.rgb = AZURE_BLUE
    header.line.fill.background()
    
    title_box = slide.shapes.add_textbox(
        Inches(0.5), Inches(0.3), Inches(9), Inches(0.7)
    )
    tf = title_box.text_frame
    p = tf.paragraphs[0]
    p.text = "System Architecture"
    p.font.size = Pt(32)
    p.font.bold = True
    p.font.color.rgb = WHITE
    
    # Component boxes
    components = [
        ("Blazor WebAssembly UI", Inches(0.5), Inches(1.5), Inches(2.5), Inches(1), RGBColor(0, 150, 136)),
        ("Azure Static Web App", Inches(0.5), Inches(2.7), Inches(2.5), Inches(0.8), RGBColor(0, 120, 212)),
        ("Azure Durable Functions", Inches(3.5), Inches(1.5), Inches(3), Inches(1), RGBColor(255, 140, 0)),
        ("Orchestrator", Inches(3.5), Inches(2.7), Inches(1.4), Inches(0.7), RGBColor(255, 193, 7)),
        ("Activities", Inches(5.1), Inches(2.7), Inches(1.4), Inches(0.7), RGBColor(255, 193, 7)),
        ("Azure Speech\nVideo Translation", Inches(7), Inches(1.5), Inches(2.5), Inches(1), RGBColor(156, 39, 176)),
        ("Azure Blob Storage", Inches(3.5), Inches(3.8), Inches(2), Inches(0.8), RGBColor(33, 150, 243)),
        ("Azure AI Foundry\n(Multi-Agent)", Inches(6), Inches(3.8), Inches(2.2), Inches(0.8), RGBColor(233, 30, 99)),
        ("Key Vault", Inches(0.5), Inches(4.8), Inches(1.8), Inches(0.7), RGBColor(76, 175, 80)),
        ("App Insights", Inches(2.5), Inches(4.8), Inches(1.8), Inches(0.7), RGBColor(103, 58, 183)),
        ("Human Review", Inches(8.2), Inches(3.8), Inches(1.5), Inches(0.8), RGBColor(96, 125, 139)),
    ]
    
    for name, left, top, width, height, color in components:
        shape = slide.shapes.add_shape(
            MSO_SHAPE.ROUNDED_RECTANGLE, left, top, width, height
        )
        shape.fill.solid()
        shape.fill.fore_color.rgb = color
        shape.line.fill.background()
        
        # Add text
        tf = shape.text_frame
        tf.word_wrap = True
        p = tf.paragraphs[0]
        p.text = name
        p.font.size = Pt(11)
        p.font.bold = True
        p.font.color.rgb = WHITE
        p.alignment = PP_ALIGN.CENTER
        tf.paragraphs[0].alignment = PP_ALIGN.CENTER
    
    # Add flow description
    desc_box = slide.shapes.add_textbox(
        Inches(0.5), Inches(5.7), Inches(9), Inches(0.8)
    )
    tf = desc_box.text_frame
    p = tf.paragraphs[0]
    p.text = "Flow: User → UI → Functions → Speech API → Blob Storage → Multi-Agent Validation → Human Review → Approved/Rejected"
    p.font.size = Pt(14)
    p.font.color.rgb = DARK_GRAY
    p.alignment = PP_ALIGN.CENTER
    
    return slide


def add_demo_flow_slide(prs):
    """Add a demo flow slide"""
    slide_layout = prs.slide_layouts[6]
    slide = prs.slides.add_slide(slide_layout)
    
    # Header
    header = slide.shapes.add_shape(
        MSO_SHAPE.RECTANGLE, 0, 0, prs.slide_width, Inches(1.2)
    )
    header.fill.solid()
    header.fill.fore_color.rgb = AZURE_BLUE
    header.line.fill.background()
    
    title_box = slide.shapes.add_textbox(
        Inches(0.5), Inches(0.3), Inches(9), Inches(0.7)
    )
    tf = title_box.text_frame
    p = tf.paragraphs[0]
    p.text = "Live Demo Flow"
    p.font.size = Pt(32)
    p.font.bold = True
    p.font.color.rgb = WHITE
    
    # Demo steps
    steps = [
        ("1", "Upload Video", "Select source video and configure translation settings"),
        ("2", "Create Job", "System creates translation with unique operation ID"),
        ("3", "Monitor Progress", "Real-time status updates via dashboard"),
        ("4", "AI Validation", "4-agent parallel validation scores the translation"),
        ("5", "Human Review", "Approve or reject with feedback"),
        ("6", "Download Results", "Get translated video + WebVTT subtitles"),
    ]
    
    y_pos = Inches(1.5)
    for num, title, desc in steps:
        # Number circle
        circle = slide.shapes.add_shape(
            MSO_SHAPE.OVAL, Inches(0.5), y_pos, Inches(0.5), Inches(0.5)
        )
        circle.fill.solid()
        circle.fill.fore_color.rgb = AZURE_BLUE
        circle.line.fill.background()
        
        tf = circle.text_frame
        p = tf.paragraphs[0]
        p.text = num
        p.font.size = Pt(16)
        p.font.bold = True
        p.font.color.rgb = WHITE
        p.alignment = PP_ALIGN.CENTER
        
        # Title and description
        text_box = slide.shapes.add_textbox(
            Inches(1.2), y_pos, Inches(8.5), Inches(0.7)
        )
        tf = text_box.text_frame
        p = tf.paragraphs[0]
        p.text = title
        p.font.size = Pt(18)
        p.font.bold = True
        p.font.color.rgb = DARK_GRAY
        
        p2 = tf.add_paragraph()
        p2.text = desc
        p2.font.size = Pt(14)
        p2.font.color.rgb = RGBColor(100, 100, 100)
        
        y_pos += Inches(0.85)
    
    return slide


def create_presentation():
    """Main function to create the presentation"""
    prs = Presentation()
    prs.slide_width = Inches(10)
    prs.slide_height = Inches(7.5)
    
    # =========================================================================
    # SLIDE 1: Title Slide
    # =========================================================================
    add_title_slide(
        prs,
        "Azure Video Translation Service",
        "AI-Powered Video Dubbing with Multi-Agent Validation\n\nCapstone Project | FY26 AMA"
    )
    
    # =========================================================================
    # SLIDE 2: Agenda
    # =========================================================================
    add_content_slide(prs, "Agenda", [
        "Business Problem & Solution Overview",
        "Architecture & Design Patterns",
        "Live Demo",
        "AI Integration & Multi-Agent System",
        "Testing & Quality Assurance",
        "DevOps & CI/CD Pipeline",
        "Monitoring & Operations",
        "Security & Compliance",
        "Recovery & Resilience",
        "Q&A"
    ])
    
    # =========================================================================
    # SLIDE 3: Business Problem
    # =========================================================================
    add_content_slide(prs, "The Business Problem", [
        "Global enterprises need to localize video content across 60+ languages",
        "Manual translation is expensive ($100-500 per video minute)",
        "Traditional dubbing takes weeks, not hours",
        "Quality varies significantly between vendors",
        "No standardized review/approval workflow",
        "Subtitles often out of sync or culturally inappropriate"
    ], notes="Emphasize the cost savings - Azure Speech API is ~$0.10/min vs $100+ for manual dubbing")
    
    # =========================================================================
    # SLIDE 4: Solution Overview
    # =========================================================================
    add_two_column_slide(
        prs,
        "Our Solution: Azure Video Translation Service",
        [
            "Automatic video dubbing with AI",
            "Voice cloning (Personal Voice) option",
            "WebVTT subtitle generation",
            "Burned-in subtitles option",
            "Real-time job tracking dashboard",
            "Multi-agent quality validation"
        ],
        [
            "Human-in-the-loop approval gate",
            "120+ source languages supported",
            "60+ target languages supported",
            "Pay-per-use Azure consumption",
            "Enterprise-grade security",
            "Full audit trail"
        ],
        "Features",
        "Benefits"
    )
    
    # =========================================================================
    # SLIDE 5: Section - Architecture
    # =========================================================================
    add_section_slide(prs, "Architecture & Design", 1)
    
    # =========================================================================
    # SLIDE 6: Architecture Diagram
    # =========================================================================
    add_architecture_slide(prs)
    
    # =========================================================================
    # SLIDE 7: Design Patterns
    # =========================================================================
    add_two_column_slide(
        prs,
        "Design Patterns & Modularity",
        [
            "Durable Functions Orchestration",
            "   - Saga pattern for long-running ops",
            "   - Automatic retry with backoff",
            "   - State persistence across failures",
            "Dependency Injection throughout",
            "Repository pattern for data access",
            "Strategy pattern for voice selection"
        ],
        [
            "Separation of Concerns",
            "   - Activities for single operations",
            "   - Services for business logic",
            "   - Functions for HTTP exposure",
            "Interface-based design (IService)",
            "Options pattern for configuration",
            "Async/await for scalability"
        ],
        "Orchestration",
        "Code Structure"
    )
    
    # =========================================================================
    # SLIDE 8: Scalability
    # =========================================================================
    add_content_slide(prs, "Scalability & Performance", [
        "Azure Functions Consumption Plan: Scale to zero, scale to thousands",
        "Durable Functions: Handle 100s of concurrent translations",
        "Blob Storage: Unlimited video storage with lifecycle policies",
        "Parallel multi-agent validation with Task.WhenAll()",
        "Stateless UI (Blazor WebAssembly) - no server affinity needed",
        "CDN-ready Static Web App hosting",
        ("Potential: Add Azure Front Door for global distribution", 1),
        ("Potential: Add Redis Cache for job status caching", 1)
    ])
    
    # =========================================================================
    # SLIDE 9: Section - Demo
    # =========================================================================
    add_section_slide(prs, "Live Demo", 2)
    
    # =========================================================================
    # SLIDE 10: Demo Flow
    # =========================================================================
    add_demo_flow_slide(prs)
    
    # =========================================================================
    # SLIDE 11: Demo URLs
    # =========================================================================
    add_content_slide(prs, "Demo Environment", [
        "Static Web App (UI): https://ashy-glacier-0400c0b0f.1.azurestaticapps.net",
        "Function App (API): https://funcapp-ama-3.azurewebsites.net",
        "Speech Service: https://speech-ama-3.cognitiveservices.azure.com",
        "",
        "Demo Scenario:",
        ("Upload a 30-second video in English", 1),
        ("Translate to Spanish with Platform Voice", 1),
        ("Watch real-time status updates", 1),
        ("View multi-agent validation scores", 1),
        ("Approve the translation", 1),
        ("Download translated video + subtitles", 1)
    ])
    
    # =========================================================================
    # SLIDE 12: Section - AI Integration
    # =========================================================================
    add_section_slide(prs, "AI Integration", 3)
    
    # =========================================================================
    # SLIDE 13: AI Technologies
    # =========================================================================
    add_two_column_slide(
        prs,
        "Azure AI Services Integration",
        [
            "Azure Speech Video Translation",
            "   - Neural voice synthesis",
            "   - Speaker diarization",
            "   - Automatic timing alignment",
            "   - Personal Voice (voice cloning)",
            "",
            "API Version: 2025-05-20",
            "Supported formats: MP4, WebM, MOV"
        ],
        [
            "Azure AI Foundry (OpenAI)",
            "   - GPT-4o-mini deployment",
            "   - Multi-agent orchestration",
            "   - Per-agent model config",
            "   - Managed Identity auth",
            "",
            "Purpose: Quality validation",
            "Pattern: Parallel execution"
        ],
        "Translation Engine",
        "Validation Engine"
    )
    
    # =========================================================================
    # SLIDE 14: Multi-Agent System
    # =========================================================================
    add_content_slide(prs, "Multi-Agent Validation Architecture", [
        "4-Agent Parallel Validation System:",
        ("Orchestrator Agent: Coordinates workflow, aggregates scores, provides summary", 1),
        ("Translation Agent (40%): Semantic accuracy, meaning preservation, fluency", 1),
        ("Technical Agent (30%): Timing sync, CPS rate, line length, WebVTT format", 1),
        ("Cultural Agent (30%): Cultural adaptation, idioms, regional appropriateness", 1),
        "",
        "Score Thresholds:",
        ("≥80: Auto-Approve (high quality)", 1),
        ("50-79: NeedsReview (human review required)", 1),
        ("<50: Auto-Reject (quality issues)", 1)
    ])
    
    # =========================================================================
    # SLIDE 15: Agent Coordination
    # =========================================================================
    add_content_slide(prs, "Agentic Behavior & Coordination", [
        "Autonomous Agent Behavior:",
        ("Each agent independently analyzes subtitles", 1),
        ("No inter-agent dependencies during analysis", 1),
        ("Parallel execution via Task.WhenAll() for performance", 1),
        "",
        "Orchestration Patterns:",
        ("State machine workflow in Durable Functions", 1),
        ("WaitForExternalEvent for human approval gate", 1),
        ("3-day timeout with automatic rejection", 1),
        "",
        "Agent Chat: Users can chat with specific agents for detailed feedback"
    ])
    
    # =========================================================================
    # SLIDE 16: Section - Testing
    # =========================================================================
    add_section_slide(prs, "Testing Strategy", 4)
    
    # =========================================================================
    # SLIDE 17: Testing Coverage
    # =========================================================================
    add_two_column_slide(
        prs,
        "Comprehensive Testing",
        [
            "Unit Tests (xUnit)",
            "   - Service layer tests",
            "   - Activity function tests",
            "   - Model validation tests",
            "   - Mock dependencies",
            "",
            "UI Tests (bUnit)",
            "   - Component rendering",
            "   - User interaction flows",
            "   - State management"
        ],
        [
            "Integration Tests",
            "   - API endpoint tests",
            "   - Durable Function orchestration",
            "   - Storage operations",
            "",
            "Test Data",
            "   - sample-high-quality.vtt",
            "   - sample-low-quality.vtt",
            "   - sample-malformed.vtt",
            "",
            "CI Pipeline: Tests run on every PR"
        ],
        "Unit & Component",
        "Integration & E2E"
    )
    
    # =========================================================================
    # SLIDE 18: Section - DevOps
    # =========================================================================
    add_section_slide(prs, "DevOps & CI/CD", 5)
    
    # =========================================================================
    # SLIDE 19: CI/CD Pipeline
    # =========================================================================
    add_content_slide(prs, "GitHub Actions CI/CD Pipeline", [
        "ci.yml - Continuous Integration:",
        ("Build .NET 8 API and .NET 9 UI projects", 1),
        ("Run unit tests with code coverage", 1),
        ("Validate Bicep templates", 1),
        ("Security scanning", 1),
        "",
        "cd-infra.yml - Infrastructure Deployment:",
        ("Bicep deployment to Azure (subscription scope)", 1),
        ("Managed Identity role assignments", 1),
        "",
        "cd-app.yml - Application Deployment:",
        ("Deploy Function App via zip deployment", 1),
        ("Deploy Static Web App via SWA CLI", 1)
    ])
    
    # =========================================================================
    # SLIDE 20: Infrastructure as Code
    # =========================================================================
    add_content_slide(prs, "Infrastructure as Code (Bicep)", [
        "Subscription-scoped deployment with modular design:",
        ("main.bicep - Entry point, parameter validation", 1),
        ("modules/function-app.bicep - Azure Functions", 1),
        ("modules/static-web-app.bicep - Blazor UI hosting", 1),
        ("modules/storage-account.bicep - Blob storage", 1),
        ("modules/speech-services.bicep - Speech API", 1),
        ("modules/ai-foundry.bicep - Azure OpenAI", 1),
        ("modules/keyvault.bicep - Secrets management", 1),
        ("modules/role-assignments.bicep - RBAC", 1),
        "",
        "Parameterized deployment numbers for multi-environment support"
    ])
    
    # =========================================================================
    # SLIDE 21: Section - Monitoring
    # =========================================================================
    add_section_slide(prs, "Monitoring & Operations", 6)
    
    # =========================================================================
    # SLIDE 22: Monitoring
    # =========================================================================
    add_two_column_slide(
        prs,
        "Observability Stack",
        [
            "Application Insights",
            "   - Request/response logging",
            "   - Dependency tracking",
            "   - Custom events & metrics",
            "   - Exception tracking",
            "",
            "Structured Logging",
            "   - ILogger<T> throughout",
            "   - Correlation IDs",
            "   - Job-specific context"
        ],
        [
            "Log Analytics Workspace",
            "   - Centralized log aggregation",
            "   - KQL query capabilities",
            "   - Long-term retention",
            "",
            "Alerting (Configurable)",
            "   - Failed translation alerts",
            "   - High latency warnings",
            "   - Error rate thresholds",
            "   - Agent validation failures"
        ],
        "Telemetry",
        "Analysis & Alerts"
    )
    
    # =========================================================================
    # SLIDE 23: Section - Security
    # =========================================================================
    add_section_slide(prs, "Security & Compliance", 7)
    
    # =========================================================================
    # SLIDE 24: Security
    # =========================================================================
    add_content_slide(prs, "Security Implementation", [
        "Identity & Access:",
        ("Managed Identity for all Azure service authentication", 1),
        ("No secrets in code - Key Vault integration", 1),
        ("RBAC role assignments via Bicep", 1),
        "",
        "Network Security:",
        ("HTTPS everywhere (TLS 1.2+)", 1),
        ("CORS configured for SWA domain only", 1),
        ("SAS tokens for blob access with expiration", 1),
        "",
        "Data Protection:",
        ("Encryption at rest (Azure-managed keys)", 1),
        ("Encryption in transit", 1),
        ("Audit logging for compliance", 1)
    ])
    
    # =========================================================================
    # SLIDE 25: Section - Recovery
    # =========================================================================
    add_section_slide(prs, "Recovery & Resilience", 8)
    
    # =========================================================================
    # SLIDE 26: Recovery
    # =========================================================================
    add_content_slide(prs, "Issue Recovery & Resilience", [
        "Durable Functions Built-in Recovery:",
        ("Automatic checkpointing - resume from any failure point", 1),
        ("Configurable retry policies with exponential backoff", 1),
        ("Dead-letter queue for failed operations", 1),
        "",
        "Operational Recovery:",
        ("409 Conflict fix: Unique operation IDs with random suffixes", 1),
        ("Storage auth fix: Public network access + RBAC roles", 1),
        ("Route conflict fix: /api/reviews/pending separate path", 1),
        "",
        "Human Intervention:",
        ("Pending approval dashboard for review", 1),
        ("3-day timeout with auto-rejection", 1),
        ("Re-translate option for failed jobs", 1)
    ])
    
    # =========================================================================
    # SLIDE 27: Lessons Learned
    # =========================================================================
    add_two_column_slide(
        prs,
        "Issues Found & Resolved",
        [
            "503 Service Unavailable",
            "   → Storage Account needed public",
            "   → access + RBAC roles",
            "",
            "409 Conflict on API calls",
            "   → Implemented unique operation",
            "   → IDs with GUID + random suffix",
            "",
            "SWA 403 Forbidden",
            "   → Removed IP restrictions",
            "   → from config"
        ],
        [
            "Reviews 404 Not Found",
            "   → Route /api/jobs/pending",
            "   → conflicted with /api/jobs/{id}",
            "   → Moved to /api/reviews/pending",
            "",
            "CORS Errors",
            "   → Added SWA hostname to",
            "   → Function App CORS settings",
            "",
            "Locale Validation Failures",
            "   → Expanded language list to 120+"
        ],
        "Infrastructure Issues",
        "Application Issues"
    )
    
    # =========================================================================
    # SLIDE 28: Future Enhancements
    # =========================================================================
    add_content_slide(prs, "Future Roadmap", [
        "Near-term Enhancements:",
        ("Real-time progress via SignalR instead of polling", 1),
        ("Batch processing for multiple videos", 1),
        ("Custom glossary support for domain-specific terms", 1),
        "",
        "Medium-term Features:",
        ("Speaker-specific voice cloning (multiple speakers)", 1),
        ("A/B testing for voice options", 1),
        ("Cost estimation before translation", 1),
        "",
        "Long-term Vision:",
        ("Integration with video CMS platforms", 1),
        ("Automated lip-sync with video manipulation", 1),
        ("Sentiment-aware voice modulation", 1)
    ])
    
    # =========================================================================
    # SLIDE 29: Summary
    # =========================================================================
    add_content_slide(prs, "Summary", [
        "✓ Enterprise-grade video translation with Azure Speech API",
        "✓ Multi-agent AI validation with GPT-4o-mini",
        "✓ Human-in-the-loop approval workflow",
        "✓ Durable Functions for reliable orchestration",
        "✓ Blazor WebAssembly modern UI",
        "✓ Infrastructure as Code with Bicep",
        "✓ CI/CD with GitHub Actions",
        "✓ Comprehensive monitoring & alerting",
        "✓ Security-first design with Managed Identity"
    ])
    
    # =========================================================================
    # SLIDE 30: Q&A
    # =========================================================================
    add_title_slide(
        prs,
        "Questions?",
        "Thank you for your time!\n\nRepository: github.com/haslam93/FY26AMA-Capstone-AI-Video-Translation"
    )
    
    # Save the presentation
    output_path = "VideoTranslationService_Presentation.pptx"
    prs.save(output_path)
    print(f"✅ Presentation saved to: {output_path}")
    print(f"   Total slides: {len(prs.slides)}")
    return output_path


if __name__ == "__main__":
    create_presentation()
