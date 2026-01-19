"""
Video Translation Service - Professional Capstone Presentation
45-minute presentation with ~15 slides

Structure:
  1. Title
  2. The Problem
  3. Solution Overview (brief) → DEMO HAPPENS AFTER THIS
  4. Demo Recap / UI Mockups
  5. High-Level Architecture (DIAGRAM PLACEHOLDER)
  6. Why Multi-Agent? (The Key Differentiator)
  7. Multi-Agent Architecture (DIAGRAM PLACEHOLDER)
  8. The 4 Agents Deep Dive
  9. Scoring & Human-in-the-Loop
  10. Technology Stack (Visual)
  11. DevOps & CI/CD
  12. Enterprise Readiness - Security
  13. Enterprise Readiness - Roadmap
  14. How I Built This with AI
  15. Q&A

Requirements: pip install python-pptx
Usage: python create_presentation_v2.py
"""

from pptx import Presentation
from pptx.util import Inches, Pt, Emu
from pptx.dml.color import RGBColor
from pptx.enum.text import PP_ALIGN, MSO_ANCHOR
from pptx.enum.shapes import MSO_SHAPE
from pptx.oxml.ns import qn

# =============================================================================
# PROFESSIONAL COLOR SCHEME (Microsoft/Azure)
# =============================================================================
COLORS = {
    # Primary
    "azure_blue": RGBColor(0, 120, 212),      # Microsoft Blue
    "azure_dark": RGBColor(0, 51, 102),       # Dark Navy
    "azure_light": RGBColor(0, 164, 239),     # Light Azure
    
    # Neutrals
    "white": RGBColor(255, 255, 255),
    "off_white": RGBColor(250, 250, 250),
    "light_gray": RGBColor(243, 243, 243),
    "medium_gray": RGBColor(128, 128, 128),
    "dark_gray": RGBColor(51, 51, 51),
    "black": RGBColor(23, 23, 23),
    
    # Accents
    "success_green": RGBColor(16, 124, 16),
    "warning_orange": RGBColor(255, 140, 0),
    "error_red": RGBColor(209, 52, 56),
    "purple": RGBColor(107, 76, 154),
    "teal": RGBColor(0, 183, 195),
    
    # Agent Colors
    "agent_orch": RGBColor(0, 120, 212),      # Blue - Orchestrator
    "agent_trans": RGBColor(16, 124, 16),     # Green - Translation
    "agent_tech": RGBColor(255, 140, 0),      # Orange - Technical
    "agent_cult": RGBColor(107, 76, 154),     # Purple - Cultural
}


def set_shape_shadow(shape, blur=4, offset=2):
    """Add subtle shadow to shape (simplified approach)"""
    # Note: python-pptx has limited shadow support, skip for now
    pass


def add_gradient_header(slide, prs, height=Inches(1.3)):
    """Add a professional gradient-style header bar"""
    # Main header bar
    header = slide.shapes.add_shape(
        MSO_SHAPE.RECTANGLE, 0, 0, prs.slide_width, height
    )
    header.fill.solid()
    header.fill.fore_color.rgb = COLORS["azure_dark"]
    header.line.fill.background()
    
    # Accent line at bottom of header
    accent = slide.shapes.add_shape(
        MSO_SHAPE.RECTANGLE, 0, height - Inches(0.05), prs.slide_width, Inches(0.05)
    )
    accent.fill.solid()
    accent.fill.fore_color.rgb = COLORS["azure_light"]
    accent.line.fill.background()


def add_title_text(slide, title, top=Inches(0.35), size=Pt(36)):
    """Add title text to slide"""
    title_box = slide.shapes.add_textbox(
        Inches(0.6), top, Inches(8.8), Inches(0.9)
    )
    tf = title_box.text_frame
    p = tf.paragraphs[0]
    p.text = title
    p.font.size = size
    p.font.bold = True
    p.font.color.rgb = COLORS["white"]
    p.font.name = "Segoe UI"


def add_slide_number(slide, prs, number, total=15):
    """Add slide number to bottom right"""
    num_box = slide.shapes.add_textbox(
        prs.slide_width - Inches(1), prs.slide_height - Inches(0.4),
        Inches(0.8), Inches(0.3)
    )
    tf = num_box.text_frame
    p = tf.paragraphs[0]
    p.text = f"{number} / {total}"
    p.font.size = Pt(10)
    p.font.color.rgb = COLORS["medium_gray"]
    p.alignment = PP_ALIGN.RIGHT


def add_bullet_content(slide, bullets, left=Inches(0.6), top=Inches(1.6), width=Inches(8.8)):
    """Add bullet point content"""
    content_box = slide.shapes.add_textbox(left, top, width, Inches(5))
    tf = content_box.text_frame
    tf.word_wrap = True
    
    for i, bullet in enumerate(bullets):
        if i == 0:
            p = tf.paragraphs[0]
        else:
            p = tf.add_paragraph()
        
        if isinstance(bullet, tuple):
            text, level = bullet
            p.text = "  " * level + "• " + text
            p.font.size = Pt(16 if level > 0 else 18)
            p.space_before = Pt(4 if level > 0 else 8)
        else:
            p.text = "• " + bullet
            p.font.size = Pt(18)
            p.space_before = Pt(8)
        
        p.font.color.rgb = COLORS["dark_gray"]
        p.font.name = "Segoe UI"
        p.space_after = Pt(4)
    
    return content_box


def add_diagram_placeholder(slide, instruction_text, top=Inches(1.6), height=Inches(4.5)):
    """Add a placeholder box for where to paste diagrams"""
    # Dashed border rectangle
    placeholder = slide.shapes.add_shape(
        MSO_SHAPE.ROUNDED_RECTANGLE, Inches(0.5), top, Inches(9), height
    )
    placeholder.fill.solid()
    placeholder.fill.fore_color.rgb = COLORS["light_gray"]
    placeholder.line.color.rgb = COLORS["azure_blue"]
    placeholder.line.width = Pt(2)
    placeholder.line.dash_style = 3  # Dash style
    
    # Instruction text
    tf = placeholder.text_frame
    tf.word_wrap = True
    p = tf.paragraphs[0]
    p.text = "📊 DIAGRAM PLACEHOLDER"
    p.font.size = Pt(20)
    p.font.bold = True
    p.font.color.rgb = COLORS["azure_blue"]
    p.alignment = PP_ALIGN.CENTER
    
    p2 = tf.add_paragraph()
    p2.text = instruction_text
    p2.font.size = Pt(14)
    p2.font.color.rgb = COLORS["medium_gray"]
    p2.alignment = PP_ALIGN.CENTER
    
    return placeholder


def add_icon_box(slide, icon_text, label, color, left, top, width=Inches(2), height=Inches(1.2)):
    """Add an icon-style box"""
    shape = slide.shapes.add_shape(
        MSO_SHAPE.ROUNDED_RECTANGLE, left, top, width, height
    )
    shape.fill.solid()
    shape.fill.fore_color.rgb = color
    shape.line.fill.background()
    
    tf = shape.text_frame
    tf.word_wrap = True
    p = tf.paragraphs[0]
    p.text = icon_text
    p.font.size = Pt(24)
    p.alignment = PP_ALIGN.CENTER
    
    p2 = tf.add_paragraph()
    p2.text = label
    p2.font.size = Pt(11)
    p2.font.bold = True
    p2.font.color.rgb = COLORS["white"]
    p2.alignment = PP_ALIGN.CENTER


# =============================================================================
# SLIDE CREATION FUNCTIONS
# =============================================================================

def create_slide_1_title(prs):
    """Slide 1: Title Slide"""
    slide = prs.slides.add_slide(prs.slide_layouts[6])
    
    # Full background
    bg = slide.shapes.add_shape(
        MSO_SHAPE.RECTANGLE, 0, 0, prs.slide_width, prs.slide_height
    )
    bg.fill.solid()
    bg.fill.fore_color.rgb = COLORS["azure_dark"]
    bg.line.fill.background()
    
    # Accent geometric shapes (decorative)
    accent1 = slide.shapes.add_shape(
        MSO_SHAPE.PARALLELOGRAM, Inches(7), Inches(0), Inches(4), prs.slide_height
    )
    accent1.fill.solid()
    accent1.fill.fore_color.rgb = COLORS["azure_blue"]
    accent1.fill.fore_color.brightness = -0.1
    accent1.line.fill.background()
    
    # Main title
    title_box = slide.shapes.add_textbox(
        Inches(0.8), Inches(2.2), Inches(8), Inches(1.2)
    )
    tf = title_box.text_frame
    p = tf.paragraphs[0]
    p.text = "Azure Video Translation Service"
    p.font.size = Pt(44)
    p.font.bold = True
    p.font.color.rgb = COLORS["white"]
    p.font.name = "Segoe UI Light"
    
    # Subtitle
    sub_box = slide.shapes.add_textbox(
        Inches(0.8), Inches(3.5), Inches(8), Inches(1)
    )
    tf = sub_box.text_frame
    p = tf.paragraphs[0]
    p.text = "AI-Powered Video Dubbing with Multi-Agent Validation"
    p.font.size = Pt(22)
    p.font.color.rgb = COLORS["azure_light"]
    p.font.name = "Segoe UI"
    
    # Author info
    author_box = slide.shapes.add_textbox(
        Inches(0.8), Inches(5.5), Inches(6), Inches(1)
    )
    tf = author_box.text_frame
    p = tf.paragraphs[0]
    p.text = "Capstone Project | FY26 AMA"
    p.font.size = Pt(16)
    p.font.color.rgb = COLORS["white"]
    p.font.name = "Segoe UI"
    
    p2 = tf.add_paragraph()
    p2.text = "Hammad Aslam"
    p2.font.size = Pt(14)
    p2.font.color.rgb = COLORS["medium_gray"]


def create_slide_2_problem(prs):
    """Slide 2: The Business Problem"""
    slide = prs.slides.add_slide(prs.slide_layouts[6])
    add_gradient_header(slide, prs)
    add_title_text(slide, "The Business Problem")
    add_slide_number(slide, prs, 2)
    
    # Left side - pain points with icons
    problems = [
        ("💰", "$100-500 per minute", "Manual video dubbing cost", Inches(0.5)),
        ("⏱️", "Weeks, not hours", "Traditional turnaround time", Inches(2.6)),
        ("🌍", "60+ languages needed", "Global content demand", Inches(4.7)),
        ("❌", "Inconsistent quality", "Variable vendor results", Inches(6.8)),
    ]
    
    for icon, stat, desc, left in problems:
        # Stat box
        box = slide.shapes.add_shape(
            MSO_SHAPE.ROUNDED_RECTANGLE, left, Inches(1.8), Inches(1.9), Inches(2.2)
        )
        box.fill.solid()
        box.fill.fore_color.rgb = COLORS["light_gray"]
        box.line.fill.background()
        
        tf = box.text_frame
        tf.word_wrap = True
        p = tf.paragraphs[0]
        p.text = icon
        p.font.size = Pt(36)
        p.alignment = PP_ALIGN.CENTER
        
        p2 = tf.add_paragraph()
        p2.text = stat
        p2.font.size = Pt(16)
        p2.font.bold = True
        p2.font.color.rgb = COLORS["azure_dark"]
        p2.alignment = PP_ALIGN.CENTER
        
        p3 = tf.add_paragraph()
        p3.text = desc
        p3.font.size = Pt(11)
        p3.font.color.rgb = COLORS["medium_gray"]
        p3.alignment = PP_ALIGN.CENTER
    
    # Bottom callout
    callout = slide.shapes.add_shape(
        MSO_SHAPE.ROUNDED_RECTANGLE, Inches(0.5), Inches(4.5), Inches(9), Inches(1.2)
    )
    callout.fill.solid()
    callout.fill.fore_color.rgb = COLORS["azure_blue"]
    callout.line.fill.background()
    
    tf = callout.text_frame
    p = tf.paragraphs[0]
    p.text = "🎯 Challenge: Build an automated, AI-validated video translation platform"
    p.font.size = Pt(20)
    p.font.bold = True
    p.font.color.rgb = COLORS["white"]
    p.alignment = PP_ALIGN.CENTER


def create_slide_3_solution(prs):
    """Slide 3: Solution Overview (brief before demo)"""
    slide = prs.slides.add_slide(prs.slide_layouts[6])
    add_gradient_header(slide, prs)
    add_title_text(slide, "The Solution: Let Me Show You")
    add_slide_number(slide, prs, 3)
    
    # Simple 3-step visual
    steps = [
        ("1", "Upload", "Video file or URL", COLORS["azure_blue"]),
        ("2", "Translate", "Azure Speech AI", COLORS["success_green"]),
        ("3", "Validate", "Multi-Agent AI", COLORS["purple"]),
    ]
    
    x_positions = [Inches(1), Inches(4), Inches(7)]
    
    for i, (num, title, desc, color) in enumerate(steps):
        # Circle with number
        circle = slide.shapes.add_shape(
            MSO_SHAPE.OVAL, x_positions[i], Inches(2.2), Inches(1.2), Inches(1.2)
        )
        circle.fill.solid()
        circle.fill.fore_color.rgb = color
        circle.line.fill.background()
        
        tf = circle.text_frame
        p = tf.paragraphs[0]
        p.text = num
        p.font.size = Pt(40)
        p.font.bold = True
        p.font.color.rgb = COLORS["white"]
        p.alignment = PP_ALIGN.CENTER
        
        # Title below
        title_box = slide.shapes.add_textbox(
            x_positions[i] - Inches(0.3), Inches(3.5), Inches(1.8), Inches(0.5)
        )
        tf = title_box.text_frame
        p = tf.paragraphs[0]
        p.text = title
        p.font.size = Pt(22)
        p.font.bold = True
        p.font.color.rgb = COLORS["dark_gray"]
        p.alignment = PP_ALIGN.CENTER
        
        # Description
        desc_box = slide.shapes.add_textbox(
            x_positions[i] - Inches(0.5), Inches(4.0), Inches(2.2), Inches(0.5)
        )
        tf = desc_box.text_frame
        p = tf.paragraphs[0]
        p.text = desc
        p.font.size = Pt(14)
        p.font.color.rgb = COLORS["medium_gray"]
        p.alignment = PP_ALIGN.CENTER
        
        # Arrow between steps (except last)
        if i < 2:
            arrow = slide.shapes.add_shape(
                MSO_SHAPE.RIGHT_ARROW, x_positions[i] + Inches(1.4), Inches(2.6), Inches(1.2), Inches(0.4)
            )
            arrow.fill.solid()
            arrow.fill.fore_color.rgb = COLORS["light_gray"]
            arrow.line.fill.background()
    
    # Demo prompt box
    demo_box = slide.shapes.add_shape(
        MSO_SHAPE.ROUNDED_RECTANGLE, Inches(2), Inches(5), Inches(6), Inches(1)
    )
    demo_box.fill.solid()
    demo_box.fill.fore_color.rgb = COLORS["warning_orange"]
    demo_box.line.fill.background()
    
    tf = demo_box.text_frame
    p = tf.paragraphs[0]
    p.text = "🎬 LIVE DEMO"
    p.font.size = Pt(28)
    p.font.bold = True
    p.font.color.rgb = COLORS["white"]
    p.alignment = PP_ALIGN.CENTER


def create_slide_4_demo_recap(prs):
    """Slide 4: Demo Recap / UI Mockups (after live demo)"""
    slide = prs.slides.add_slide(prs.slide_layouts[6])
    add_gradient_header(slide, prs)
    add_title_text(slide, "What You Just Saw")
    add_slide_number(slide, prs, 4)
    
    # UI Screen mockups - simplified boxes representing screens
    screens = [
        ("Dashboard", "All jobs at a glance\nStatus badges\nQuick actions", Inches(0.4)),
        ("Create Job", "Video URL/upload\nLanguage selection\nVoice options", Inches(3.4)),
        ("Job Details", "Real-time progress\nAI validation scores\nDownload outputs", Inches(6.4)),
    ]
    
    for title, features, left in screens:
        # Screen frame
        frame = slide.shapes.add_shape(
            MSO_SHAPE.RECTANGLE, left, Inches(1.7), Inches(3), Inches(3.5)
        )
        frame.fill.solid()
        frame.fill.fore_color.rgb = COLORS["white"]
        frame.line.color.rgb = COLORS["azure_blue"]
        frame.line.width = Pt(2)
        
        # Screen header bar
        header = slide.shapes.add_shape(
            MSO_SHAPE.RECTANGLE, left, Inches(1.7), Inches(3), Inches(0.4)
        )
        header.fill.solid()
        header.fill.fore_color.rgb = COLORS["azure_blue"]
        header.line.fill.background()
        
        # Screen title
        title_box = slide.shapes.add_textbox(left + Inches(0.1), Inches(1.75), Inches(2.8), Inches(0.3))
        tf = title_box.text_frame
        p = tf.paragraphs[0]
        p.text = title
        p.font.size = Pt(12)
        p.font.bold = True
        p.font.color.rgb = COLORS["white"]
        
        # Features inside screen
        feat_box = slide.shapes.add_textbox(left + Inches(0.2), Inches(2.2), Inches(2.6), Inches(2.8))
        tf = feat_box.text_frame
        tf.word_wrap = True
        p = tf.paragraphs[0]
        p.text = features
        p.font.size = Pt(12)
        p.font.color.rgb = COLORS["dark_gray"]
    
    # Key outcomes box
    outcome_box = slide.shapes.add_textbox(Inches(0.5), Inches(5.4), Inches(9), Inches(0.8))
    tf = outcome_box.text_frame
    p = tf.paragraphs[0]
    p.text = "✅ 120+ source languages  •  60+ target languages  •  <$0.10/min vs $100+/min manual"
    p.font.size = Pt(16)
    p.font.bold = True
    p.font.color.rgb = COLORS["success_green"]
    p.alignment = PP_ALIGN.CENTER


def create_slide_5_architecture(prs):
    """Slide 5: High-Level Architecture (DIAGRAM PLACEHOLDER)"""
    slide = prs.slides.add_slide(prs.slide_layouts[6])
    add_gradient_header(slide, prs)
    add_title_text(slide, "High-Level Architecture")
    add_slide_number(slide, prs, 5)
    
    add_diagram_placeholder(
        slide,
        """
Paste the HIGH-LEVEL ARCHITECTURE diagram here.

From: docs/architecture.md → "High-Level Architecture" section
Mermaid code starts with: graph TB

Use https://mermaid.live to export as PNG/SVG:
1. Copy the mermaid code block from architecture.md
2. Paste into mermaid.live
3. Export as PNG (recommended) or SVG
4. Delete this placeholder shape
5. Insert → Pictures → paste the exported image
        """,
        top=Inches(1.5),
        height=Inches(5)
    )


def create_slide_6_why_multiagent(prs):
    """Slide 6: Why Multi-Agent? (The Key Differentiator)"""
    slide = prs.slides.add_slide(prs.slide_layouts[6])
    add_gradient_header(slide, prs)
    add_title_text(slide, "Why Multi-Agent Architecture?")
    add_slide_number(slide, prs, 6)
    
    # Two columns: Single LLM vs Multi-Agent
    
    # Left column - Single LLM (crossed out)
    left_box = slide.shapes.add_shape(
        MSO_SHAPE.ROUNDED_RECTANGLE, Inches(0.4), Inches(1.6), Inches(4.3), Inches(3.2)
    )
    left_box.fill.solid()
    left_box.fill.fore_color.rgb = COLORS["light_gray"]
    left_box.line.color.rgb = COLORS["error_red"]
    left_box.line.width = Pt(2)
    
    left_title = slide.shapes.add_textbox(Inches(0.6), Inches(1.7), Inches(4), Inches(0.5))
    tf = left_title.text_frame
    p = tf.paragraphs[0]
    p.text = "❌ Single LLM Approach"
    p.font.size = Pt(18)
    p.font.bold = True
    p.font.color.rgb = COLORS["error_red"]
    
    left_content = slide.shapes.add_textbox(Inches(0.6), Inches(2.3), Inches(4), Inches(2.3))
    tf = left_content.text_frame
    tf.word_wrap = True
    issues = [
        "• One prompt does everything",
        "• No specialization",
        "• Hard to debug which aspect failed",
        "• Single point of failure",
        "• Can't weight different concerns"
    ]
    for i, issue in enumerate(issues):
        if i == 0:
            p = tf.paragraphs[0]
        else:
            p = tf.add_paragraph()
        p.text = issue
        p.font.size = Pt(14)
        p.font.color.rgb = COLORS["dark_gray"]
        p.space_after = Pt(6)
    
    # Right column - Multi-Agent (highlighted)
    right_box = slide.shapes.add_shape(
        MSO_SHAPE.ROUNDED_RECTANGLE, Inches(5.3), Inches(1.6), Inches(4.3), Inches(3.2)
    )
    right_box.fill.solid()
    right_box.fill.fore_color.rgb = COLORS["azure_blue"]
    right_box.line.fill.background()
    
    right_title = slide.shapes.add_textbox(Inches(5.5), Inches(1.7), Inches(4), Inches(0.5))
    tf = right_title.text_frame
    p = tf.paragraphs[0]
    p.text = "✅ Multi-Agent Approach"
    p.font.size = Pt(18)
    p.font.bold = True
    p.font.color.rgb = COLORS["white"]
    
    right_content = slide.shapes.add_textbox(Inches(5.5), Inches(2.3), Inches(4), Inches(2.3))
    tf = right_content.text_frame
    tf.word_wrap = True
    benefits = [
        "• Specialized agents per concern",
        "• Parallel execution (fast)",
        "• Weighted scoring (40/30/30)",
        "• Clear accountability",
        "• Extensible architecture"
    ]
    for i, benefit in enumerate(benefits):
        if i == 0:
            p = tf.paragraphs[0]
        else:
            p = tf.add_paragraph()
        p.text = benefit
        p.font.size = Pt(14)
        p.font.color.rgb = COLORS["white"]
        p.space_after = Pt(6)
    
    # Bottom insight
    insight = slide.shapes.add_shape(
        MSO_SHAPE.ROUNDED_RECTANGLE, Inches(0.4), Inches(5), Inches(9.2), Inches(0.9)
    )
    insight.fill.solid()
    insight.fill.fore_color.rgb = COLORS["azure_dark"]
    insight.line.fill.background()
    
    tf = insight.text_frame
    p = tf.paragraphs[0]
    p.text = "💡 Key Insight: Translation quality requires multiple perspectives - linguistic, technical, and cultural"
    p.font.size = Pt(16)
    p.font.color.rgb = COLORS["white"]
    p.alignment = PP_ALIGN.CENTER


def create_slide_7_multiagent_arch(prs):
    """Slide 7: Multi-Agent Architecture (DIAGRAM PLACEHOLDER)"""
    slide = prs.slides.add_slide(prs.slide_layouts[6])
    add_gradient_header(slide, prs)
    add_title_text(slide, "Multi-Agent Orchestration")
    add_slide_number(slide, prs, 7)
    
    add_diagram_placeholder(
        slide,
        """
Paste the MULTI-AGENT ORCHESTRATION diagram here.

From: docs/architecture.md → "Multi-Agent Validation System" section
Mermaid code starts with: graph TB

Shows: Orchestrator Agent → Parallel execution to 3 specialist agents → Aggregation

Same process:
1. Copy mermaid code from architecture.md (search "Multi-Agent Orchestration")
2. Export from mermaid.live as PNG
3. Replace this placeholder
        """,
        top=Inches(1.5),
        height=Inches(5)
    )


def create_slide_8_agents_deep_dive(prs):
    """Slide 8: The 4 Agents Deep Dive"""
    slide = prs.slides.add_slide(prs.slide_layouts[6])
    add_gradient_header(slide, prs)
    add_title_text(slide, "The 4 Specialized Agents")
    add_slide_number(slide, prs, 8)
    
    # Four agent cards
    agents = [
        ("🎯", "Orchestrator", "Coordinates workflow\nAggregates scores\nProvides summary", COLORS["agent_orch"], "—"),
        ("📝", "Translation", "Semantic accuracy\nMeaning preservation\nNatural phrasing", COLORS["agent_trans"], "40%"),
        ("⚙️", "Technical", "Timing sync\nCPS rate (<15)\nWebVTT format", COLORS["agent_tech"], "30%"),
        ("🌍", "Cultural", "Cultural adaptation\nIdiom handling\nRegional fit", COLORS["agent_cult"], "30%"),
    ]
    
    x_positions = [Inches(0.3), Inches(2.6), Inches(4.9), Inches(7.2)]
    
    for i, (icon, name, details, color, weight) in enumerate(agents):
        # Card background
        card = slide.shapes.add_shape(
            MSO_SHAPE.ROUNDED_RECTANGLE, x_positions[i], Inches(1.7), Inches(2.2), Inches(3.2)
        )
        card.fill.solid()
        card.fill.fore_color.rgb = color
        card.line.fill.background()
        
        # Icon
        icon_box = slide.shapes.add_textbox(x_positions[i], Inches(1.85), Inches(2.2), Inches(0.6))
        tf = icon_box.text_frame
        p = tf.paragraphs[0]
        p.text = icon
        p.font.size = Pt(32)
        p.alignment = PP_ALIGN.CENTER
        
        # Agent name
        name_box = slide.shapes.add_textbox(x_positions[i], Inches(2.45), Inches(2.2), Inches(0.4))
        tf = name_box.text_frame
        p = tf.paragraphs[0]
        p.text = name
        p.font.size = Pt(16)
        p.font.bold = True
        p.font.color.rgb = COLORS["white"]
        p.alignment = PP_ALIGN.CENTER
        
        # Weight badge
        weight_box = slide.shapes.add_textbox(x_positions[i], Inches(2.85), Inches(2.2), Inches(0.35))
        tf = weight_box.text_frame
        p = tf.paragraphs[0]
        p.text = f"Weight: {weight}"
        p.font.size = Pt(12)
        p.font.color.rgb = COLORS["white"]
        p.alignment = PP_ALIGN.CENTER
        
        # Details
        detail_box = slide.shapes.add_textbox(x_positions[i] + Inches(0.1), Inches(3.3), Inches(2), Inches(1.5))
        tf = detail_box.text_frame
        tf.word_wrap = True
        p = tf.paragraphs[0]
        p.text = details
        p.font.size = Pt(11)
        p.font.color.rgb = COLORS["white"]
        p.alignment = PP_ALIGN.CENTER
    
    # Technical note
    note = slide.shapes.add_textbox(Inches(0.5), Inches(5.2), Inches(9), Inches(0.6))
    tf = note.text_frame
    p = tf.paragraphs[0]
    p.text = "⚡ All 3 specialist agents run in parallel via Task.WhenAll() • Powered by GPT-4o-mini"
    p.font.size = Pt(14)
    p.font.color.rgb = COLORS["medium_gray"]
    p.alignment = PP_ALIGN.CENTER


def create_slide_9_scoring_hitl(prs):
    """Slide 9: Scoring & Human-in-the-Loop"""
    slide = prs.slides.add_slide(prs.slide_layouts[6])
    add_gradient_header(slide, prs)
    add_title_text(slide, "Scoring & Human-in-the-Loop")
    add_slide_number(slide, prs, 9)
    
    # Scoring thresholds visual
    thresholds = [
        ("≥ 80", "Auto-Approve", "High quality - no review needed", COLORS["success_green"]),
        ("50-79", "Needs Review", "Human reviewer must approve", COLORS["warning_orange"]),
        ("< 50", "Auto-Reject", "Quality issues - re-translate", COLORS["error_red"]),
    ]
    
    y_positions = [Inches(1.7), Inches(2.6), Inches(3.5)]
    
    for i, (score, action, desc, color) in enumerate(thresholds):
        # Score badge
        badge = slide.shapes.add_shape(
            MSO_SHAPE.ROUNDED_RECTANGLE, Inches(0.5), y_positions[i], Inches(1.2), Inches(0.7)
        )
        badge.fill.solid()
        badge.fill.fore_color.rgb = color
        badge.line.fill.background()
        
        tf = badge.text_frame
        p = tf.paragraphs[0]
        p.text = score
        p.font.size = Pt(18)
        p.font.bold = True
        p.font.color.rgb = COLORS["white"]
        p.alignment = PP_ALIGN.CENTER
        
        # Action and description
        text_box = slide.shapes.add_textbox(Inches(1.9), y_positions[i], Inches(4), Inches(0.7))
        tf = text_box.text_frame
        p = tf.paragraphs[0]
        p.text = action
        p.font.size = Pt(16)
        p.font.bold = True
        p.font.color.rgb = color
        
        p2 = tf.add_paragraph()
        p2.text = desc
        p2.font.size = Pt(12)
        p2.font.color.rgb = COLORS["medium_gray"]
    
    # Human-in-the-Loop section
    hitl_box = slide.shapes.add_shape(
        MSO_SHAPE.ROUNDED_RECTANGLE, Inches(5.5), Inches(1.7), Inches(4), Inches(2.8)
    )
    hitl_box.fill.solid()
    hitl_box.fill.fore_color.rgb = COLORS["azure_dark"]
    hitl_box.line.fill.background()
    
    hitl_title = slide.shapes.add_textbox(Inches(5.7), Inches(1.85), Inches(3.6), Inches(0.4))
    tf = hitl_title.text_frame
    p = tf.paragraphs[0]
    p.text = "🧑‍💼 Human-in-the-Loop"
    p.font.size = Pt(16)
    p.font.bold = True
    p.font.color.rgb = COLORS["white"]
    
    hitl_content = slide.shapes.add_textbox(Inches(5.7), Inches(2.4), Inches(3.6), Inches(2))
    tf = hitl_content.text_frame
    tf.word_wrap = True
    items = [
        "• Reviews dashboard",
        "• Approve / Reject buttons",
        "• Reviewer info captured",
        "• 3-day timeout → auto-reject",
        "• Agent chat for questions"
    ]
    for i, item in enumerate(items):
        if i == 0:
            p = tf.paragraphs[0]
        else:
            p = tf.add_paragraph()
        p.text = item
        p.font.size = Pt(13)
        p.font.color.rgb = COLORS["white"]
        p.space_after = Pt(4)
    
    # State machine note
    state_box = slide.shapes.add_textbox(Inches(0.5), Inches(4.6), Inches(9), Inches(1.2))
    tf = state_box.text_frame
    p = tf.paragraphs[0]
    p.text = "📊 Workflow State Machine"
    p.font.size = Pt(14)
    p.font.bold = True
    p.font.color.rgb = COLORS["dark_gray"]
    
    p2 = tf.add_paragraph()
    p2.text = "Created → Validating → Processing → CopyingOutputs → RunningValidation → PendingApproval → Approved/Rejected"
    p2.font.size = Pt(12)
    p2.font.color.rgb = COLORS["medium_gray"]


def create_slide_10_tech_stack(prs):
    """Slide 10: Technology Stack (Visual)"""
    slide = prs.slides.add_slide(prs.slide_layouts[6])
    add_gradient_header(slide, prs)
    add_title_text(slide, "Technology Stack")
    add_slide_number(slide, prs, 10)
    
    # Technology icons/boxes in a grid
    tech_items = [
        # Row 1 - Frontend
        ("Blazor WASM", ".NET 9", COLORS["purple"], Inches(0.4), Inches(1.7)),
        ("Static Web App", "Hosting", COLORS["azure_blue"], Inches(2.5), Inches(1.7)),
        
        # Row 2 - Backend
        ("Durable Functions", ".NET 8 Isolated", COLORS["warning_orange"], Inches(0.4), Inches(2.9)),
        ("Function App", "Consumption Plan", COLORS["azure_blue"], Inches(2.5), Inches(2.9)),
        
        # Row 3 - AI Services
        ("Speech Services", "Video Translation", COLORS["teal"], Inches(4.6), Inches(1.7)),
        ("AI Foundry", "GPT-4o-mini", COLORS["purple"], Inches(4.6), Inches(2.9)),
        
        # Row 4 - Infrastructure
        ("Blob Storage", "Videos & Outputs", COLORS["azure_blue"], Inches(6.7), Inches(1.7)),
        ("Key Vault", "Secrets", COLORS["success_green"], Inches(6.7), Inches(2.9)),
        
        # Row 5 - Monitoring & DevOps
        ("App Insights", "Monitoring", COLORS["azure_light"], Inches(0.4), Inches(4.1)),
        ("Log Analytics", "Queries", COLORS["azure_light"], Inches(2.5), Inches(4.1)),
        ("GitHub Actions", "CI/CD", COLORS["dark_gray"], Inches(4.6), Inches(4.1)),
        ("Bicep", "IaC", COLORS["warning_orange"], Inches(6.7), Inches(4.1)),
    ]
    
    for name, desc, color, left, top in tech_items:
        box = slide.shapes.add_shape(
            MSO_SHAPE.ROUNDED_RECTANGLE, left, top, Inches(1.95), Inches(1)
        )
        box.fill.solid()
        box.fill.fore_color.rgb = color
        box.line.fill.background()
        
        tf = box.text_frame
        tf.word_wrap = True
        p = tf.paragraphs[0]
        p.text = name
        p.font.size = Pt(13)
        p.font.bold = True
        p.font.color.rgb = COLORS["white"]
        p.alignment = PP_ALIGN.CENTER
        
        p2 = tf.add_paragraph()
        p2.text = desc
        p2.font.size = Pt(10)
        p2.font.color.rgb = COLORS["white"]
        p2.alignment = PP_ALIGN.CENTER
    
    # Layer labels
    layers = [
        ("Frontend", Inches(1.2), Inches(1.45)),
        ("Backend", Inches(1.2), Inches(2.65)),
        ("AI", Inches(5), Inches(1.45)),
        ("Storage", Inches(7.3), Inches(1.45)),
        ("Ops", Inches(4), Inches(3.85)),
    ]
    
    for label, x, y in layers:
        label_box = slide.shapes.add_textbox(x, y, Inches(1), Inches(0.25))
        tf = label_box.text_frame
        p = tf.paragraphs[0]
        p.text = label
        p.font.size = Pt(9)
        p.font.color.rgb = COLORS["medium_gray"]
        p.alignment = PP_ALIGN.CENTER


def create_slide_11_devops(prs):
    """Slide 11: DevOps & CI/CD"""
    slide = prs.slides.add_slide(prs.slide_layouts[6])
    add_gradient_header(slide, prs)
    add_title_text(slide, "DevOps & CI/CD")
    add_slide_number(slide, prs, 11)
    
    # Pipeline visualization
    pipeline_steps = [
        ("📝", "Push", COLORS["dark_gray"]),
        ("🔨", "Build", COLORS["warning_orange"]),
        ("🧪", "Test", COLORS["purple"]),
        ("✅", "Validate", COLORS["azure_blue"]),
        ("🚀", "Deploy", COLORS["success_green"]),
    ]
    
    x_start = Inches(0.5)
    for i, (icon, label, color) in enumerate(pipeline_steps):
        # Step circle
        x = x_start + (i * Inches(1.8))
        circle = slide.shapes.add_shape(
            MSO_SHAPE.OVAL, x, Inches(1.8), Inches(0.9), Inches(0.9)
        )
        circle.fill.solid()
        circle.fill.fore_color.rgb = color
        circle.line.fill.background()
        
        tf = circle.text_frame
        p = tf.paragraphs[0]
        p.text = icon
        p.font.size = Pt(28)
        p.alignment = PP_ALIGN.CENTER
        
        # Label
        label_box = slide.shapes.add_textbox(x - Inches(0.2), Inches(2.8), Inches(1.3), Inches(0.4))
        tf = label_box.text_frame
        p = tf.paragraphs[0]
        p.text = label
        p.font.size = Pt(14)
        p.font.bold = True
        p.font.color.rgb = color
        p.alignment = PP_ALIGN.CENTER
        
        # Arrow
        if i < 4:
            arrow = slide.shapes.add_shape(
                MSO_SHAPE.RIGHT_ARROW, x + Inches(1), Inches(2.1), Inches(0.6), Inches(0.3)
            )
            arrow.fill.solid()
            arrow.fill.fore_color.rgb = COLORS["light_gray"]
            arrow.line.fill.background()
    
    # GitHub Actions files
    files_box = slide.shapes.add_shape(
        MSO_SHAPE.ROUNDED_RECTANGLE, Inches(0.5), Inches(3.5), Inches(4.5), Inches(2.2)
    )
    files_box.fill.solid()
    files_box.fill.fore_color.rgb = COLORS["light_gray"]
    files_box.line.fill.background()
    
    files_content = slide.shapes.add_textbox(Inches(0.7), Inches(3.6), Inches(4.2), Inches(2))
    tf = files_content.text_frame
    tf.word_wrap = True
    p = tf.paragraphs[0]
    p.text = "📁 .github/workflows/"
    p.font.size = Pt(14)
    p.font.bold = True
    p.font.color.rgb = COLORS["dark_gray"]
    
    files = ["ci.yml - Build, test, validate", "cd-infra.yml - Bicep deployment", "cd-app.yml - App deployment"]
    for f in files:
        p = tf.add_paragraph()
        p.text = "   • " + f
        p.font.size = Pt(12)
        p.font.color.rgb = COLORS["medium_gray"]
    
    # IaC section
    iac_box = slide.shapes.add_shape(
        MSO_SHAPE.ROUNDED_RECTANGLE, Inches(5.2), Inches(3.5), Inches(4.4), Inches(2.2)
    )
    iac_box.fill.solid()
    iac_box.fill.fore_color.rgb = COLORS["azure_dark"]
    iac_box.line.fill.background()
    
    iac_content = slide.shapes.add_textbox(Inches(5.4), Inches(3.6), Inches(4.1), Inches(2))
    tf = iac_content.text_frame
    tf.word_wrap = True
    p = tf.paragraphs[0]
    p.text = "📐 Infrastructure as Code (Bicep)"
    p.font.size = Pt(14)
    p.font.bold = True
    p.font.color.rgb = COLORS["white"]
    
    modules = ["Modular design (8 modules)", "Subscription-scoped", "Parameterized environments", "RBAC via Managed Identity"]
    for m in modules:
        p = tf.add_paragraph()
        p.text = "• " + m
        p.font.size = Pt(12)
        p.font.color.rgb = COLORS["white"]


def create_slide_12_security(prs):
    """Slide 12: Enterprise Readiness - Security"""
    slide = prs.slides.add_slide(prs.slide_layouts[6])
    add_gradient_header(slide, prs)
    add_title_text(slide, "Enterprise Readiness: Security")
    add_slide_number(slide, prs, 12)
    
    # Security pillars
    pillars = [
        ("🔐", "Identity", ["Managed Identity", "No secrets in code", "RBAC assignments", "Key Vault integration"], COLORS["azure_blue"]),
        ("🌐", "Network", ["HTTPS everywhere", "TLS 1.2+", "CORS configured", "SAS token expiration"], COLORS["success_green"]),
        ("🛡️", "Data", ["Encryption at rest", "Encryption in transit", "Audit logging", "Blob lifecycle"], COLORS["purple"]),
    ]
    
    x_positions = [Inches(0.4), Inches(3.4), Inches(6.4)]
    
    for i, (icon, title, items, color) in enumerate(pillars):
        # Pillar box
        box = slide.shapes.add_shape(
            MSO_SHAPE.ROUNDED_RECTANGLE, x_positions[i], Inches(1.7), Inches(2.9), Inches(3)
        )
        box.fill.solid()
        box.fill.fore_color.rgb = color
        box.line.fill.background()
        
        # Icon and title
        header_box = slide.shapes.add_textbox(x_positions[i], Inches(1.85), Inches(2.9), Inches(0.7))
        tf = header_box.text_frame
        p = tf.paragraphs[0]
        p.text = f"{icon} {title}"
        p.font.size = Pt(18)
        p.font.bold = True
        p.font.color.rgb = COLORS["white"]
        p.alignment = PP_ALIGN.CENTER
        
        # Items
        items_box = slide.shapes.add_textbox(x_positions[i] + Inches(0.2), Inches(2.6), Inches(2.6), Inches(2))
        tf = items_box.text_frame
        tf.word_wrap = True
        for j, item in enumerate(items):
            if j == 0:
                p = tf.paragraphs[0]
            else:
                p = tf.add_paragraph()
            p.text = "✓ " + item
            p.font.size = Pt(13)
            p.font.color.rgb = COLORS["white"]
            p.space_after = Pt(4)
    
    # Future improvements
    future_box = slide.shapes.add_shape(
        MSO_SHAPE.ROUNDED_RECTANGLE, Inches(0.4), Inches(4.9), Inches(9.2), Inches(0.9)
    )
    future_box.fill.solid()
    future_box.fill.fore_color.rgb = COLORS["light_gray"]
    future_box.line.color.rgb = COLORS["warning_orange"]
    future_box.line.width = Pt(2)
    
    future_text = slide.shapes.add_textbox(Inches(0.6), Inches(5.05), Inches(8.8), Inches(0.7))
    tf = future_text.text_frame
    p = tf.paragraphs[0]
    p.text = "🔮 Future: Private Endpoints • VNet Integration • Azure Front Door • WAF"
    p.font.size = Pt(14)
    p.font.color.rgb = COLORS["warning_orange"]
    p.alignment = PP_ALIGN.CENTER


def create_slide_13_roadmap(prs):
    """Slide 13: Enterprise Readiness - Roadmap"""
    slide = prs.slides.add_slide(prs.slide_layouts[6])
    add_gradient_header(slide, prs)
    add_title_text(slide, "Enterprise Readiness: Roadmap")
    add_slide_number(slide, prs, 13)
    
    # Three columns: Now, Next, Future
    columns = [
        ("✅ Implemented", [
            "Managed Identity auth",
            "Key Vault secrets",
            "App Insights monitoring",
            "CI/CD pipelines",
            "Multi-agent validation",
            "Human approval gate"
        ], COLORS["success_green"]),
        ("🔨 Near-term", [
            "Private Endpoints",
            "VNet Integration",
            "Cost calculator / estimates",
            "Rate limiting / quotas",
            "SignalR real-time updates",
            "Batch processing"
        ], COLORS["warning_orange"]),
        ("🔮 Future Vision", [
            "Geo-redundancy (DR)",
            "Multi-tenant support",
            "Custom glossaries",
            "A/B testing voices",
            "Lip-sync enhancement",
            "CMS integrations"
        ], COLORS["purple"]),
    ]
    
    x_positions = [Inches(0.3), Inches(3.35), Inches(6.4)]
    
    for i, (title, items, color) in enumerate(columns):
        # Header
        header = slide.shapes.add_shape(
            MSO_SHAPE.ROUNDED_RECTANGLE, x_positions[i], Inches(1.7), Inches(3), Inches(0.6)
        )
        header.fill.solid()
        header.fill.fore_color.rgb = color
        header.line.fill.background()
        
        tf = header.text_frame
        p = tf.paragraphs[0]
        p.text = title
        p.font.size = Pt(16)
        p.font.bold = True
        p.font.color.rgb = COLORS["white"]
        p.alignment = PP_ALIGN.CENTER
        
        # Items box
        items_box = slide.shapes.add_shape(
            MSO_SHAPE.ROUNDED_RECTANGLE, x_positions[i], Inches(2.4), Inches(3), Inches(3.3)
        )
        items_box.fill.solid()
        items_box.fill.fore_color.rgb = COLORS["light_gray"]
        items_box.line.fill.background()
        
        # Items text
        text_box = slide.shapes.add_textbox(x_positions[i] + Inches(0.15), Inches(2.55), Inches(2.7), Inches(3.1))
        tf = text_box.text_frame
        tf.word_wrap = True
        for j, item in enumerate(items):
            if j == 0:
                p = tf.paragraphs[0]
            else:
                p = tf.add_paragraph()
            p.text = "• " + item
            p.font.size = Pt(12)
            p.font.color.rgb = COLORS["dark_gray"]
            p.space_after = Pt(6)


def create_slide_14_ai_journey(prs):
    """Slide 14: How I Built This with AI"""
    slide = prs.slides.add_slide(prs.slide_layouts[6])
    add_gradient_header(slide, prs)
    add_title_text(slide, "How I Built This with AI")
    add_slide_number(slide, prs, 14)
    
    # Journey flow - left to right with arrows
    steps = [
        ("📄", "Paper → Digital", "M365 Copilot\nDigitized capstone\nproject letter", COLORS["azure_blue"], Inches(0.2)),
        ("📓", "Plan & Research", "M365 Copilot +\nNotebook LM\nArchitecture planning", COLORS["success_green"], Inches(2.3)),
        ("🛠️", "Build & Iterate", "GitHub Copilot\n(Claude Opus 4.5)\nCode + debug", COLORS["warning_orange"], Inches(4.4)),
        ("📊", "Present", "GitHub Copilot\n(Claude Opus 4.5)\nThis presentation!", COLORS["purple"], Inches(6.5)),
    ]
    
    for i, (icon, title, desc, color, left) in enumerate(steps):
        # Step card
        card = slide.shapes.add_shape(
            MSO_SHAPE.ROUNDED_RECTANGLE, left, Inches(2.2), Inches(2), Inches(2.8)
        )
        card.fill.solid()
        card.fill.fore_color.rgb = color
        card.line.fill.background()
        
        # Icon
        icon_box = slide.shapes.add_textbox(left, Inches(2.35), Inches(2), Inches(0.6))
        tf = icon_box.text_frame
        p = tf.paragraphs[0]
        p.text = icon
        p.font.size = Pt(36)
        p.alignment = PP_ALIGN.CENTER
        
        # Title
        title_box = slide.shapes.add_textbox(left, Inches(2.95), Inches(2), Inches(0.5))
        tf = title_box.text_frame
        p = tf.paragraphs[0]
        p.text = title
        p.font.size = Pt(13)
        p.font.bold = True
        p.font.color.rgb = COLORS["white"]
        p.alignment = PP_ALIGN.CENTER
        
        # Description
        desc_box = slide.shapes.add_textbox(left + Inches(0.1), Inches(3.5), Inches(1.8), Inches(1.4))
        tf = desc_box.text_frame
        tf.word_wrap = True
        p = tf.paragraphs[0]
        p.text = desc
        p.font.size = Pt(10)
        p.font.color.rgb = COLORS["white"]
        p.alignment = PP_ALIGN.CENTER
        
        # Arrow between steps
        if i < 3:
            arrow = slide.shapes.add_shape(
                MSO_SHAPE.RIGHT_ARROW, left + Inches(2.05), Inches(3.4), Inches(0.22), Inches(0.25)
            )
            arrow.fill.solid()
            arrow.fill.fore_color.rgb = COLORS["light_gray"]
            arrow.line.fill.background()
    
    # Meta insight box
    insight = slide.shapes.add_shape(
        MSO_SHAPE.ROUNDED_RECTANGLE, Inches(0.4), Inches(5.2), Inches(9.2), Inches(0.7)
    )
    insight.fill.solid()
    insight.fill.fore_color.rgb = COLORS["azure_dark"]
    insight.line.fill.background()
    
    tf = insight.text_frame
    p = tf.paragraphs[0]
    p.text = "💡 AI as a force multiplier: Focus on architecture decisions, let AI handle implementation details"
    p.font.size = Pt(14)
    p.font.color.rgb = COLORS["white"]
    p.alignment = PP_ALIGN.CENTER


def create_slide_15_qanda(prs):
    """Slide 15: Q&A"""
    slide = prs.slides.add_slide(prs.slide_layouts[6])
    
    # Full background
    bg = slide.shapes.add_shape(
        MSO_SHAPE.RECTANGLE, 0, 0, prs.slide_width, prs.slide_height
    )
    bg.fill.solid()
    bg.fill.fore_color.rgb = COLORS["azure_dark"]
    bg.line.fill.background()
    
    # Accent shape
    accent = slide.shapes.add_shape(
        MSO_SHAPE.PARALLELOGRAM, Inches(7), Inches(0), Inches(4), prs.slide_height
    )
    accent.fill.solid()
    accent.fill.fore_color.rgb = COLORS["azure_blue"]
    accent.line.fill.background()
    
    # Q&A text
    title_box = slide.shapes.add_textbox(
        Inches(0.8), Inches(2.5), Inches(7), Inches(1.5)
    )
    tf = title_box.text_frame
    p = tf.paragraphs[0]
    p.text = "Questions?"
    p.font.size = Pt(60)
    p.font.bold = True
    p.font.color.rgb = COLORS["white"]
    p.font.name = "Segoe UI Light"
    
    # Thank you
    thanks_box = slide.shapes.add_textbox(
        Inches(0.8), Inches(4), Inches(7), Inches(0.8)
    )
    tf = thanks_box.text_frame
    p = tf.paragraphs[0]
    p.text = "Thank you for your time!"
    p.font.size = Pt(24)
    p.font.color.rgb = COLORS["azure_light"]
    
    # Contact/repo info
    info_box = slide.shapes.add_textbox(
        Inches(0.8), Inches(5.3), Inches(7), Inches(1)
    )
    tf = info_box.text_frame
    p = tf.paragraphs[0]
    p.text = "📧 Hammad Aslam"
    p.font.size = Pt(14)
    p.font.color.rgb = COLORS["white"]
    
    p2 = tf.add_paragraph()
    p2.text = "🔗 github.com/haslam93/FY26AMA-Capstone-AI-Video-Translation"
    p2.font.size = Pt(12)
    p2.font.color.rgb = COLORS["medium_gray"]


# =============================================================================
# MAIN FUNCTION
# =============================================================================

def create_presentation():
    """Generate the complete presentation"""
    prs = Presentation()
    prs.slide_width = Inches(10)
    prs.slide_height = Inches(7.5)
    
    print("🎨 Creating professional presentation...")
    
    # Create all slides
    create_slide_1_title(prs)
    print("  ✓ Slide 1: Title")
    
    create_slide_2_problem(prs)
    print("  ✓ Slide 2: The Problem")
    
    create_slide_3_solution(prs)
    print("  ✓ Slide 3: Solution Overview (pre-demo)")
    
    # === LIVE DEMO HAPPENS HERE ===
    
    create_slide_4_demo_recap(prs)
    print("  ✓ Slide 4: Demo Recap (post-demo)")
    
    create_slide_5_architecture(prs)
    print("  ✓ Slide 5: Architecture [DIAGRAM PLACEHOLDER]")
    
    create_slide_6_why_multiagent(prs)
    print("  ✓ Slide 6: Why Multi-Agent?")
    
    create_slide_7_multiagent_arch(prs)
    print("  ✓ Slide 7: Multi-Agent Architecture [DIAGRAM PLACEHOLDER]")
    
    create_slide_8_agents_deep_dive(prs)
    print("  ✓ Slide 8: 4 Agents Deep Dive")
    
    create_slide_9_scoring_hitl(prs)
    print("  ✓ Slide 9: Scoring & Human-in-the-Loop")
    
    create_slide_10_tech_stack(prs)
    print("  ✓ Slide 10: Technology Stack")
    
    create_slide_11_devops(prs)
    print("  ✓ Slide 11: DevOps & CI/CD")
    
    create_slide_12_security(prs)
    print("  ✓ Slide 12: Enterprise Security")
    
    create_slide_13_roadmap(prs)
    print("  ✓ Slide 13: Enterprise Roadmap")
    
    create_slide_14_ai_journey(prs)
    print("  ✓ Slide 14: How I Built This with AI")
    
    create_slide_15_qanda(prs)
    print("  ✓ Slide 15: Q&A")
    
    # Save
    output_path = "VideoTranslation_Capstone_Presentation.pptx"
    prs.save(output_path)
    
    print(f"\n✅ Presentation saved: {output_path}")
    print(f"   Total slides: {len(prs.slides)}")
    print("\n" + "="*60)
    print("📋 DIAGRAM INSTRUCTIONS:")
    print("="*60)
    print("""
Slide 5 - High-Level Architecture:
  1. Open docs/architecture.md
  2. Find the "High-Level Architecture" section
  3. Copy the mermaid code block (starts with "graph TB")
  4. Go to https://mermaid.live
  5. Paste and export as PNG
  6. In PowerPoint: Delete placeholder shape, Insert → Pictures

Slide 7 - Multi-Agent Orchestration:
  1. In architecture.md, find "Multi-Agent Validation System"
  2. Copy the mermaid code (shows Orchestrator → 3 agents)
  3. Same process: mermaid.live → Export PNG → Insert
""")
    
    return output_path


if __name__ == "__main__":
    create_presentation()
