# RightClicks

**Context-aware Windows automation through intelligent right-click menus**

---

## What is RightClicks?

RightClicks transforms Windows Explorer's context menu into a powerful automation hub. Right-click any file and instantly access relevant operations — no need to remember complex commands, switch between tools, or hunt for utilities.

**The core idea:** *Context guides capability.* When you right-click a `.mp4`, you see video operations. Right-click a `.jpg`, you see image operations. Right-click a `.glsl` shader file, you see code operations. The system surfaces what's relevant to your current focus.

### Why This Matters

Small shortcuts compound into massive time savings:
- ✅ No need to recall FFmpeg commands
- ✅ No need to remember image conversion syntax
- ✅ No need to switch between multiple tools
- ✅ No need to look up formatting scripts

**Example:** Right-click `video.mp4` → Extract MP3. Done. The job runs in the background, you get a notification when it's complete, and `video.mp3` appears next to the original file.

---

## Current Status

**🚧 Active Development** — Core functionality working, expanding feature set.

RightClicks currently includes:
- ✅ **12 working features** (video, audio, image, text operations)
- ✅ **Windows Explorer integration** (right-click context menu)
- ✅ **Background job queue** with configurable concurrency
- ✅ **System tray application** with configuration UI
- ✅ **Windows notifications** for job completion

See **[TASKS.md](TASKS.md)** for detailed development progress and roadmap.

---

## The Vision: AI-Augmented Context

This is where it gets interesting.

RightClicks is exploring **AI integration** as a research and development initiative. The thesis: *context-aware AI can transform ordinary right-clicks into intelligent, situation-aware actions.*

### AI Touchpoints (Research Phase)

**Clipboard contains SQL:**
- Right-click → Reformat + Optimize + Explain (via AI)

**Clipboard contains GLSL shader:**
- Right-click → Translate to Modern GLSL + Validate Syntax

**Clipboard contains image:**
- Right-click → AI Upscale / Remove Background / Generate Variations

**Clipboard contains video URL:**
- Right-click → Auto-download with intelligent format selection

### The Research Challenge

This is where **community input becomes critical**. Questions I'm exploring:

- **Which AI services should we integrate?** (fal.ai, Replicate, HuggingFace, others?)
- **How do we handle API authentication?** (Some services like HailuoAI require CCP accounts — easy for me personally, but a big ask for general users)
- **What's the right balance between local processing and cloud AI?**
- **Which features provide the most value?**

**Personal Note:** I've worked extensively with [HailuoAI](https://hailuoai.com/) and found their API integration straightforward, but requiring a CCP (China) account creates friction for Western users. This highlights the research challenge: finding the right AI providers that balance capability, accessibility, and ease of integration.

**This is R&D territory.** It might slow development, but the ideas in **[VISION.md](VISION.md)** are what could make RightClicks a truly transformative application.

---

## Why Contribute?

This project becomes exponentially more valuable with diverse perspectives:

- **Feature Ideas:** What operations do *you* perform repeatedly?
- **AI Integration:** Which AI services do *you* use and trust?
- **Use Cases:** What workflows could context-aware automation improve?
- **Technical Expertise:** Shell integration, AI APIs, Windows internals, UX design

The foundation is solid. The architecture is extensible. The vision is ambitious. **Your ideas and contributions can shape where this goes.**

---

## Documentation

- **[VISION.md](VISION.md)** — Long-term goals, AI integration thesis, and conceptual framework
- **[TASKS.md](TASKS.md)** — Development roadmap and current progress
- **[ARCHITECTURE.md](ARCHITECTURE.md)** — Technical decisions and implementation details
- **[RightClicks.md](RightClicks.md)** — Feature specifications and exact behaviors

---

## Quick Start (For Developers)

**Requirements:**
- Windows 10/11
- .NET 8 SDK
- Visual Studio 2022 or VS Code

**Build:**
```bash
git clone https://github.com/hughesdo/RightClicks.git
cd RightClicks
dotnet build
```

**Install Shell Extension (requires admin):**
```bash
RightClicksShellInstaller.exe /install
```

**Run:**
```bash
RightClicks.exe
```

The application runs in the system tray. Right-click the icon to access configuration and job queue.

---

## License

MIT License — See [LICENSE](LICENSE) for details.

---

## Contact

**Don Hughes**
GitHub: [@hughesdo](https://github.com/hughesdo)

**Interested in contributing?** Open an issue or PR. Let's explore what context-aware automation can become.
