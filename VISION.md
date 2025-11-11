# RightClicks Project – Vision and Long-Term Goals

**Last Updated:** 2025-11-11

---

## Thesis

**Context guides both AI and human interaction.**

In Windows, context defines capability — the type of object we click on determines what we can do with it. A file's type, for example, provides that context. When you right-click a `.txt`, `.mp4`, or `.jpg`, Windows presents different menus based on what operations make sense for that file type. These contextual affordances are what the RightClicks project aims to enhance and unify.

---

## Concept Overview

The project explores how contextual right-click menus across Windows — from **File Explorer**, **Desktop**, **Clipboard**, and **Systray** — can evolve into intelligent tools.

Every environment offers unique context opportunities:

### **File Explorer**
Right-clicking a file exposes transformations:
- Extract MP3 from MP4
- Count Lines in Text File
- Convert Image formats
- Reverse video playback
- Extract video frames

### **Desktop**
Could offer context actions based on clipboard contents:
- Paste and Format Text
- Generate AI Image from Clipboard Prompt
- Reformat code snippets
- Translate text

### **Systray Menu**
Acts as a command hub that's always reachable, even when the desktop is buried under open windows:
- Quick access to features
- Job queue monitoring
- Configuration changes
- Clipboard-aware actions

---

## Roots in Old Projects

Your prior projects serve as functional prototypes and inspiration:

### **`.\OldProjects\TransformClipboard`**
Performed string transformations directly on clipboard text:
- Reverse strings
- Sort lists
- Count characters
- Format JSON/XML
- **Key Learning:** Simple, reflection-based extension pattern that made adding new features trivial

### **`.\OldProjects\RightClickApps`**
Added Explorer context items for operations:
- Line counting
- File analysis
- Quick transformations
- **Key Learning:** Shell integration patterns and registry management

### **`.\OldProjects\RightClickTray`**
Extended right-click menus for the application's systray icon:
- Quick access even when multitasking
- Always-available command palette
- **Key Learning:** Systray as a persistent, context-aware hub

### **`.\OldProjects\SystemTrayApp`**
Detected clipboard content like video URLs and triggered automated tasks:
- Automatically download videos from YouTube/Vimeo links
- Background processing without user intervention
- **Key Learning:** Clipboard monitoring and intelligent automation

**Together, these experiments show that context awareness — understanding where the click occurs and what's selected — can dramatically simplify user interaction.**

---

## Efficiency and Purpose

Small shortcuts add up to massive time savings. RightClicks is about **reducing mental load**:
- No need to recall long FFmpeg commands
- No need to remember SQL formatting scripts
- No need to look up regex rules
- No need to switch between multiple tools

Instead, the system **surfaces what's relevant** to the user's current focus — whether that's a file, text, or an application state.

**Example:** A simple right-click when your clipboard holds a YouTube or Vimeo URL could automatically trigger the correct video downloader, quietly handling the task in the background.

---

## Next-Generation Context: AI and MCP

Beyond traditional right-click menus, RightClicks will explore **AI integration** through **Model Context Protocols (MCP)** and **API connectors**. The goal: let context drive automation and reasoning.

### **Future AI-Powered Features:**

**Clipboard contains SQL:**
- Right-click → Reformat SQL Query (local reformatter + AI pass)
- Optimize query structure
- Add comments explaining query logic

**Clipboard contains GLSL shader code:**
- Right-click → Translate to Modern GLSL
- Route through AI syntax validator (Claude.ai)
- Explain shader functionality

**Clipboard contains Markdown text:**
- Summarize
- Rephrase (business tone, casual tone, technical tone)
- Convert to HTML
- Generate table of contents

**Clipboard contains a recognized video link:**
- Auto Download Video using pre-configured logic
- Extract audio only
- Download with subtitles

**Clipboard contains image:**
- Generate AI description
- Upscale with AI
- Remove background
- Generate variations

This combination of **local intelligence** and **cloud AI augmentation** transforms ordinary right-clicks into dynamic, situation-aware actions.

---

## Future Exploration

As the project evolves, the central questions become:

### **Where should context menus appear?**
- File Explorer (current focus)
- Desktop right-click
- Systray icon menu
- Clipboard monitoring (background)
- Text selection in any application
- Image selection in any application

### **What should they recognize?**
- File type (extension-based)
- Clipboard data type (text, image, URL, code)
- Current selection (highlighted text, selected files)
- Application state (what's running, what's focused)
- Content analysis (is this SQL? GLSL? JSON? A video URL?)

### **How can AI augment these actions securely and efficiently?**
- Local processing first (privacy, speed)
- Cloud AI for complex tasks (summarization, translation, generation)
- User control over API usage (opt-in, API key management)
- Transparent logging (what was sent, what was received)
- Cost awareness (token usage, API limits)

---

## The End Vision

A system that intelligently merges **local automation** with **AI-driven enhancements** — a Windows companion that knows the context of what you're doing and offers to help at exactly the right time.

**Characteristics:**
- **Context-aware:** Understands what you're working with
- **Unobtrusive:** Works in the background, surfaces when needed
- **Extensible:** Easy to add new features and integrations
- **Intelligent:** Combines local rules with AI reasoning
- **Efficient:** Reduces cognitive load and repetitive tasks
- **Trustworthy:** Transparent, secure, user-controlled

---

## The Path Forward

This new project, **RightClicks**, is a consolidation and evolution — a single, unified application that brings together the capabilities of your previous tools:
- TransformClipboard
- RightClickApps
- RightClickTray
- SystemTrayApp

**It's both a merging of past innovations and a clear path forward** — toward one cohesive, context-aware automation platform.

---

## Development Phases and Vision Alignment

### **Phase 1-2: Foundation (Current)**
- Establish core architecture
- Implement file-based context menus
- Prove the concept with video/audio features

### **Phase 3-4: Expansion**
- Add more file types (images, text, code)
- Implement job queue and background processing
- Polish UI and user experience

### **Phase 5-6: Integration**
- Shell integration complete
- Systray as command hub
- Clipboard monitoring foundation

### **Phase 7+: AI Evolution**
- Clipboard content analysis
- AI-powered transformations
- MCP integration for Claude.ai and other AI services
- Desktop and text selection context menus
- Intelligent automation based on user patterns

---

## Revisiting This Vision

**When to revisit:**
- After completing each major phase
- When considering new feature additions
- When architectural decisions need validation
- When prioritizing between competing features

**Questions to ask:**
- Does this feature align with the context-aware vision?
- Does it reduce mental load or add complexity?
- Is it the right balance of local vs. cloud processing?
- Does it fit naturally into the user's workflow?

---

## Notes for Future Development

**Keep in mind:**
- Start simple, evolve complexity
- Context is king — always ask "what does the user have in front of them?"
- Local first, cloud when beneficial
- User control and transparency are non-negotiable
- Small shortcuts compound into massive productivity gains

**This vision guides us, but doesn't constrain us. As we learn from building and using RightClicks, this document should evolve.**

---

**Remember: We're not just building a tool. We're building a context-aware companion that makes Windows work the way users think.**

