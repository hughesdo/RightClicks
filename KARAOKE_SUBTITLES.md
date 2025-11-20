# Karaoke Subtitle Rendering

**Transform videos into karaoke-style subtitled content with word-by-word highlighting**

---

## Overview

The Karaoke Subtitle Rendering feature automatically transcribes video/audio content using OpenAI's Whisper AI model and generates professional karaoke-style subtitles with word-level timing and highlighting. Subtitles are burned directly into the video for maximum compatibility.

**Key Features:**
- ✅ **Local AI transcription** — No internet required, runs on your GPU
- ✅ **Word-level timing** — Each word highlights individually as it's spoken
- ✅ **3 visual styles** — Classic, Modern Glow, Neon Pop
- ✅ **3 quality tiers** — Tiny (fast), Medium (balanced), High (best quality)
- ✅ **Burned-in subtitles** — Works on any video player, no external subtitle file needed
- ✅ **ASS subtitle export** — Separate `.ass` file for advanced editing

---

## Quick Start

1. **Right-click any video file** (`.mp4`, `.avi`, `.mkv`, `.mov`, etc.)
2. **Navigate to:** `Karaoke ▶`
3. **Choose a style:** Classic, Modern Glow, or Neon Pop
4. **Choose a quality tier:** Tiny (fastest), Medium (balanced), or High (best)
5. **Wait for processing** — Job runs in background, notification on completion
6. **Output files:**
   - `{filename}_SUBTITLED.mp4` — Video with burned-in karaoke subtitles
   - `{filename}.ass` — ASS subtitle file for editing/reuse

**Example:** Right-click `song.mp4` → Karaoke ▶ Classic ▶ Tiny (fastest)  
**Result:** `song_SUBTITLED.mp4` + `song.ass` created in same folder

---

## Visual Styles

### 1. Classic Style
**Best for:** Traditional karaoke, sing-along videos, lyric videos

**Appearance:**
- **Font:** MV Boli (playful, rounded)
- **Size:** 98px (large, readable)
- **Default color:** White text with black outline
- **Highlight color:** Yellow text with black outline
- **Animation:** Fill effect (word fills with yellow as spoken)
- **Positioning:** Bottom center, 30px from bottom edge

**Use cases:**
- Karaoke party videos
- Sing-along content for kids
- Traditional lyric videos
- Music education

**Whisper Model Recommendations:**
- **Tiny** — Songs with clear vocals, fast processing
- **Medium** — Songs with background music, better accuracy
- **High** — Complex audio, multiple speakers, best quality

---

### 2. Modern Glow Style
**Best for:** Professional content, tutorials, presentations

**Appearance:**
- **Font:** Arial (clean, professional)
- **Size:** 88px (slightly smaller, elegant)
- **Default color:** White text with dark gray outline
- **Highlight color:** White text with orange glow outline
- **Animation:** Glow effect (word glows orange as spoken)
- **Positioning:** Bottom center, 40px from bottom edge

**Use cases:**
- Tutorial videos
- Educational content
- Professional presentations
- Documentary-style videos

**Whisper Model Recommendations:**
- **Tiny** — Clear speech, single speaker
- **Medium** — Multiple speakers, background noise
- **High** — Technical content, accents, complex terminology

---

### 3. Neon Pop Style
**Best for:** Social media, energetic content, modern aesthetics

**Appearance:**
- **Font:** Impact (bold, attention-grabbing)
- **Size:** 92px (bold and prominent)
- **Default color:** Magenta text with black outline
- **Highlight color:** Cyan text with magenta outline
- **Animation:** Pulse effect (word pulses with color change)
- **Positioning:** Bottom center, 35px from bottom edge

**Use cases:**
- Social media content (TikTok, Instagram, YouTube Shorts)
- Music videos
- Energetic/upbeat content
- Gaming videos

**Whisper Model Recommendations:**
- **Tiny** — Fast-paced content, quick turnaround
- **Medium** — Balanced quality for social media
- **High** — Premium content, maximum accuracy

---

## Quality Tiers (Whisper Models)

### Tiny (Fastest)
- **Model:** Whisper Tiny English
- **VRAM:** ~1 GB
- **Speed:** Fastest (5-10 seconds for 1 minute of audio)
- **Accuracy:** Basic (good for clear speech)
- **Best for:** Quick tests, clear vocals, simple content

### Medium (Balanced)
- **Model:** Whisper Small English
- **VRAM:** ~2 GB
- **Speed:** Moderate (15-30 seconds for 1 minute of audio)
- **Accuracy:** High (handles background noise well)
- **Best for:** Most use cases, balanced speed/quality

### High (Best Quality)
- **Model:** Whisper Large V3 Turbo
- **VRAM:** ~6 GB
- **Speed:** Slower (30-60 seconds for 1 minute of audio)
- **Accuracy:** Best (handles accents, technical terms, complex audio)
- **Best for:** Professional content, complex audio, maximum accuracy

---

## Technical Details

### How It Works

1. **Audio Extraction** — FFmpeg extracts audio from video as 16kHz WAV
2. **AI Transcription** — Whisper.net transcribes audio with segment-level timestamps
3. **Word Timing Estimation** — Words are evenly distributed across segment duration
4. **ASS Generation** — Two-layer ASS subtitle file created:
   - **Layer 0:** Full text always visible (default style)
   - **Layer 1:** Word-by-word highlighting (highlight style)
5. **Video Rendering** — FFmpeg burns subtitles into video using libass filter
6. **Output** — Subtitled video + ASS file saved next to original

### File Formats

**Input:** `.mp4`, `.avi`, `.mkv`, `.mov`, `.wmv`, `.flv`, `.webm`, `.mpeg`, `.mpg`, `.m4v`

**Output:**
- `{filename}_SUBTITLED.mp4` — H.264 video with burned-in subtitles
- `{filename}.ass` — Advanced SubStation Alpha subtitle file

### Performance

**Processing time depends on:**
- Video duration (longer = more time)
- Whisper model (Tiny fastest, High slowest)
- GPU availability (CUDA/DirectML acceleration)
- System resources (CPU, RAM, VRAM)

**Example timings (5-second video):**
- Tiny: ~15-20 seconds
- Medium: ~20-30 seconds
- High: ~30-45 seconds

---

## Customization

### Editing Styles

Style configurations are stored in JSON files:
- `KaraokeStyles/Classic/style.json`
- `KaraokeStyles/ModernGlow/style.json`
- `KaraokeStyles/NeonPop/style.json`

**Editable properties:**
- `fontName` — Font family (must be installed on system)
- `fontSize` — Font size in pixels
- `defaultPrimaryColor` — Default text color (BGR format: `&H00BBGGRR`)
- `highlightPrimaryColor` — Highlight text color
- `defaultOutlineColor` — Default outline color
- `highlightOutlineColor` — Highlight outline color
- `defaultOutlineThickness` — Default outline thickness (pixels)
- `highlightOutlineThickness` — Highlight outline thickness (pixels)
- `positioning.alignment` — Text alignment (2 = bottom center)
- `positioning.marginV` — Vertical margin from bottom (pixels)
- `positioning.marginL` — Left margin (pixels)
- `positioning.marginR` — Right margin (pixels)

**Color format:** ASS uses BGR (Blue-Green-Red) format, not RGB!
- White: `&H00FFFFFF`
- Black: `&H00000000`
- Red: `&H000000FF`
- Green: `&H0000FF00`
- Blue: `&H00FF0000`
- Yellow: `&H0000FFFF`
- Cyan: `&H00FFFF00`
- Magenta: `&H00FF00FF`

### Creating Custom Styles

1. **Copy an existing style folder** (e.g., `KaraokeStyles/Classic/`)
2. **Rename the folder** (e.g., `KaraokeStyles/MyStyle/`)
3. **Edit `style.json`** with your custom settings
4. **Update `styleName`** in the JSON to match your folder name
5. **Create a new feature class** in `RightClicks/Features/Video/`:

```csharp
public class KaraokeMyStyleTinyFeature : KaraokeFeatureBase
{
    public override string Id => "KaraokeMyStyleTiny";
    public override string DisplayName => "Karaoke > My Style > Tiny (fastest, ~1 GB VRAM)";
    public override string Description => "Generate karaoke-style subtitled video with My Style using Whisper Tiny model";
    protected override string StyleName => "MyStyle"; // Must match folder name
    protected override GgmlType WhisperModelType => GgmlType.TinyEn;
}
```

6. **Rebuild the project** — Feature will be auto-discovered
7. **Test via CLI:** `RightClicks.exe --feature KaraokeMyStyleTiny --file "test.mp4" --test-mode`

---

## Troubleshooting

### Subtitles Not Appearing

**Problem:** Video plays but no subtitles visible

**Solutions:**
1. **Check video duration** — Subtitles may start after video ends (check ASS file timestamps)
2. **Check video player** — Some players don't render burned-in subtitles correctly (try VLC)
3. **Check ASS file** — Open `.ass` file in text editor, verify timestamps start at `0:00:00.00`
4. **Check logs** — Look for errors in `%LOCALAPPDATA%\RightClicks\logs\`

### Transcription Inaccurate

**Problem:** Whisper transcribes words incorrectly

**Solutions:**
1. **Use a higher quality model** — Try Medium or High instead of Tiny
2. **Check audio quality** — Poor audio = poor transcription
3. **Check language** — Whisper models are English-only (TinyEn, SmallEn, etc.)
4. **Edit ASS file manually** — Fix transcription errors in text editor

### Processing Too Slow

**Problem:** Karaoke generation takes too long

**Solutions:**
1. **Use Tiny model** — Fastest processing, good for clear speech
2. **Check GPU availability** — Whisper.net uses GPU acceleration (CUDA/DirectML)
3. **Close other applications** — Free up VRAM and CPU resources
4. **Process shorter clips** — Split long videos into segments

### Out of Memory

**Problem:** "Out of memory" or "CUDA out of memory" errors

**Solutions:**
1. **Use Tiny model** — Requires only ~1 GB VRAM
2. **Close other GPU applications** — Free up VRAM (browsers, games, etc.)
3. **Reduce video resolution** — Lower resolution = less memory
4. **Upgrade GPU** — High model requires ~6 GB VRAM

### Wrong Font Displayed

**Problem:** Subtitles use wrong font or default font

**Solutions:**
1. **Install the font** — Font must be installed on system (e.g., MV Boli, Impact)
2. **Check font name** — Font name in JSON must match system font name exactly
3. **Use common fonts** — Arial, Times New Roman, Impact are widely available
4. **Rebuild video** — Font is embedded during rendering, not playback

---

## Advanced Usage

### Batch Processing

Process multiple videos with the same style:

```powershell
# PowerShell script to process all MP4 files in a folder
Get-ChildItem "*.mp4" | ForEach-Object {
    & "$env:LOCALAPPDATA\RightClicks\RightClicks.exe" --feature KaraokeClassicTiny --file $_.FullName
}
```

### Reusing ASS Files

If you already have an ASS file, you can burn it into a different video:

```bash
ffmpeg -i video.mp4 -vf "subtitles=subtitles.ass" -c:a copy -c:v libx264 -crf 23 output.mp4
```

### Editing ASS Files

ASS files are plain text and can be edited in any text editor:

1. **Open `.ass` file** in Notepad, VS Code, or Aegisub (subtitle editor)
2. **Edit text** — Fix transcription errors
3. **Adjust timing** — Change start/end times (format: `h:mm:ss.cc`)
4. **Change colors** — Modify color codes in Style definitions
5. **Save and re-render** — Use FFmpeg or RightClicks to burn updated subtitles

**Recommended editors:**
- **Aegisub** — Professional subtitle editor (free, open-source)
- **Subtitle Edit** — Powerful subtitle editor with many features
- **VS Code** — Text editor with syntax highlighting

### Extracting Subtitles Only

If you only want the ASS file without rendering:

1. Run the karaoke feature normally
2. Delete the `_SUBTITLED.mp4` file
3. Keep the `.ass` file for use in video players that support external subtitles

---

## FAQ

**Q: Can I use this for non-English videos?**
A: Currently, the Whisper models are English-only (TinyEn, SmallEn, LargeV3Turbo). Multilingual support may be added in the future.

**Q: Can I change the subtitle position?**
A: Yes! Edit the `positioning.marginV` value in the style JSON. Higher values move subtitles up from the bottom.

**Q: Why are there two output files?**
A: The `_SUBTITLED.mp4` has subtitles burned in (works everywhere). The `.ass` file is for editing or use with players that support external subtitles.

**Q: Can I use my own fonts?**
A: Yes! Install the font on your system, then edit the `fontName` in the style JSON to match the font's exact name.

**Q: How accurate is the transcription?**
A: Accuracy depends on audio quality and model choice. High model is very accurate (~95%+) for clear speech. Tiny model is faster but less accurate (~85-90%).

**Q: Can I adjust word timing manually?**
A: Yes! Edit the `.ass` file in a text editor or subtitle editor like Aegisub. Each word has start/end timestamps you can adjust.

**Q: Does this work with music videos?**
A: Yes! Whisper can transcribe lyrics from music videos. Use Medium or High model for best results with background music.

**Q: Can I remove the karaoke highlighting?**
A: Yes! Edit the `.ass` file and delete all `Dialogue: 1` lines (Layer 1 = highlighting). Keep only `Dialogue: 0` lines (Layer 0 = static text).

**Q: Why does processing take so long?**
A: AI transcription is computationally intensive. Use Tiny model for faster processing, or upgrade your GPU for better performance.

**Q: Can I use this commercially?**
A: RightClicks is for personal use. Whisper models are open-source (MIT license). Check licensing for fonts and video content separately.

---

## Examples

### Example 1: Music Video Karaoke
**Input:** `song.mp4` (3-minute music video)
**Style:** Classic
**Model:** Medium
**Output:** `song_SUBTITLED.mp4` with yellow word-by-word highlighting
**Use case:** Karaoke party, sing-along video

### Example 2: Tutorial Subtitles
**Input:** `tutorial.mp4` (10-minute coding tutorial)
**Style:** Modern Glow
**Model:** High
**Output:** `tutorial_SUBTITLED.mp4` with professional orange glow effect
**Use case:** Educational content, accessibility

### Example 3: Social Media Content
**Input:** `short.mp4` (30-second TikTok video)
**Style:** Neon Pop
**Model:** Tiny
**Output:** `short_SUBTITLED.mp4` with vibrant cyan/magenta colors
**Use case:** Social media, viral content

---

## Credits

**Technologies Used:**
- **Whisper.net** — Local AI transcription (OpenAI Whisper models)
- **FFMpegCore** — Video processing and subtitle rendering
- **libass** — Advanced SubStation Alpha subtitle rendering
- **Serilog** — Structured logging

**Inspired by:**
- Traditional karaoke systems
- YouTube auto-generated captions
- TikTok/Instagram subtitle trends

---

## See Also

- **[README.md](README.md)** — Main project documentation
- **[TASKS.md](TASKS.md)** — Development roadmap and progress
- **[ARCHITECTURE.md](ARCHITECTURE.md)** — Technical architecture and design decisions


