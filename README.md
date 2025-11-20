# RightClicks

**Context-aware Windows automation through intelligent right-click menus**

**Quick Demo:** https://www.youtube.com/watch?v=MAjGchRpTNw — Here's a quick demo of it in use so far.

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
- ✅ **33 working features** (video, audio, image, text operations)
- ✅ **Windows Explorer integration** (right-click context menu)
- ✅ **Background job queue** with configurable concurrency
- ✅ **System tray application** with configuration UI
- ✅ **Windows notifications with sound** for job completion
- ✅ **Cloud-based AI features** (fal.ai integration with 5 lip sync models)
- ✅ **Local AI transcription** (Whisper.net with 6 models, GPU-accelerated)
- ✅ **Karaoke subtitle rendering** (9 features: 3 styles × 3 quality tiers)

See **[TASKS.md](TASKS.md)** for detailed development progress and roadmap.

---

## Features

### Local AI Features (No Internet Required)

**Whisper Transcription** — Local AI-powered audio/video transcription using OpenAI's Whisper model
- ✅ **6 models available** — Choose speed vs. accuracy tradeoff
  - Tiny (~1 GB VRAM) — Fastest, basic accuracy
  - Base (~1 GB VRAM) — Fast, good accuracy
  - Small (~2 GB VRAM) — Balanced speed and accuracy
  - Medium (~5 GB VRAM) — High accuracy, slower
  - Large (~10 GB VRAM) — Best accuracy, slowest
  - Turbo (~6 GB VRAM) — Fast with near-large accuracy
- ✅ **GPU acceleration** — Automatic CUDA support (falls back to CPU)
- ✅ **Automatic model download** — Models downloaded on first use (~75 MB - 3 GB)
- ✅ **Supports all audio/video formats** — MP3, WAV, MP4, AVI, MKV, etc.
- ✅ **Output:** Plain text file (`.txt`) next to source file

**How to use:** Right-click any audio/video file → **Transcribe ▶** → Select model

---

**Karaoke Subtitle Rendering** — Transform videos into karaoke-style subtitled content with word-by-word highlighting
- ✅ **3 visual styles** — Classic (traditional), Modern Glow (professional), Neon Pop (social media)
- ✅ **3 quality tiers** — Tiny (fastest), Medium (balanced), High (best quality)
- ✅ **Word-level timing** — Each word highlights individually as it's spoken
- ✅ **Burned-in subtitles** — Works on any video player, no external subtitle file needed
- ✅ **ASS subtitle export** — Separate `.ass` file for advanced editing
- ✅ **Fully customizable** — Edit styles, fonts, colors, positioning via JSON config

**How to use:** Right-click any video file → **Karaoke ▶** → Select style → Select quality tier

**📖 Full documentation:** See **[KARAOKE_SUBTITLES.md](KARAOKE_SUBTITLES.md)** for detailed style descriptions, customization guide, and troubleshooting

---

## Cloud-Based AI Features

RightClicks supports **cloud-based AI features** that leverage external APIs for advanced operations. These features are marked with a ☁️ icon in the UI.

### Currently Supported APIs

**fal.ai** — AI-powered video and image processing
- ✅ **Lip Sync (5 models)** — Sync video with audio using AI
  - Kling ($0.17/min) — Most affordable
  - Pixverse ($0.20/min) — Budget option
  - VEED ($0.40/min) — Standard quality
  - Sync v1.9 ($0.70/min) — High quality
  - Creatify ($1.00/min) — Premium quality

### Setup Instructions

1. **Open RightClicks** from the system tray
2. **Navigate to API Config tab**
3. **Add API service:**
   - Service Name: `fal.ai`
   - Environment Variable: `FAL_KEY`
4. **Enter your API key** (get one from [fal.ai](https://fal.ai))
5. **Click Save**

API-based features (marked with ☁️) will now appear in context menus when you right-click supported files.

### Important Notes

- **Internet connectivity required** — API features require an active internet connection
- **Usage costs** — Most AI APIs charge per request. Check your provider's pricing
- **Processing time** — Cloud-based operations may take several minutes depending on file size and API queue
- **Security** — API keys are stored securely in Windows User environment variables, never in config files

### File Hosting for API Features

**fal.ai Lip Sync** and other cloud-based features require temporary file hosting to send files to external APIs.

**RightClicks uses Cloudinary** (enterprise-grade cloud storage) as the primary file hosting service. See **[cloudinary.md](cloudinary.md)** for detailed setup instructions.

**Why Cloudinary?**
- ✅ Enterprise-grade reliability (unlike 0x0.st which has server-side bugs)
- ✅ Generous free tier (25 GB storage, 25 GB bandwidth/month)
- ✅ Fast global CDN
- ✅ Works perfectly with fal.ai and other external APIs

**Setup Required:**
1. Create a free Cloudinary account
2. Configure API keys in RightClicks settings
3. Create an unsigned upload preset named "RightClicks"

See **[cloudinary.md](cloudinary.md)** for complete setup instructions.

**Current Limits:**
- **Maximum file size:** 100 MB per file (Cloudinary free tier)
- **File retention:** Files are automatically deleted after processing completes
- **Recommended video length:** Up to 30 seconds for lip sync features
- **Audio format:** Always uses MP3 (automatically extracted if needed)

**Privacy & Security:**
- ✅ API keys stored securely in Windows environment variables
- ✅ Unsigned uploads (no API secrets exposed)
- ✅ Files automatically deleted after processing (success or failure)
- ⚠️ Uploaded files are temporarily publicly accessible via their URLs (but URLs are unguessable)

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
- **[cloudinary.md](cloudinary.md)** — Cloudinary setup and configuration for API-based features

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
