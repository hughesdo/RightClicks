# RightClicks Clip Editor

**A standalone, frame-accurate video and audio clip editor integrated with Windows Explorer context menus.**

---

## Overview

The RightClicks Clip Editor is a lightweight WPF application that allows users to:
- Extract precise clips from video files (frame-accurate)
- Extract precise clips from audio files (sample-accurate)
- Save multiple clips from a single session
- Launch directly from Windows Explorer right-click menus

---

## Current Status (2026-01-14)

### ✅ Completed
- **Phase 1:** Project setup, data models, services (100%)
- **Phase 2:** Video editor MVP (80% - missing timeline control)
  - Video playback with MediaElement
  - Frame-accurate stepping
  - IN/OUT point management
  - Clip list management
  - Batch export with FFmpeg
  - Keyboard shortcuts

### 🚧 In Progress
- Timeline control (visual timeline with draggable markers)

### 📋 Planned
- Audio editor with waveform visualization
- Context menu integration
- Settings window
- Comprehensive testing

---

## How to Use (Current Version)

### Launch from Command Line

```powershell
# Video mode
RightClicksClipEditor.exe --video "C:\path\to\video.mp4"

# Audio mode (placeholder only)
RightClicksClipEditor.exe --audio "C:\path\to\audio.mp3"

# Auto-detect mode
RightClicksClipEditor.exe "C:\path\to\file.mp4"
```

### Video Editor Workflow

1. **Load Video** - Window opens with video loaded and analyzed
2. **Play/Pause** - Click Play button or press Spacebar
3. **Navigate** - Use Left/Right arrows for frame stepping, Shift+Left/Right for ±1 second
4. **Set IN Point** - Press **I** key at desired start position
5. **Set OUT Point** - Press **O** key at desired end position
6. **Add Clip** - Press **Ctrl+A** or click "Add Current Selection"
7. **Repeat** - Set more IN/OUT points and add more clips
8. **Export** - Press **Ctrl+S** or click "Save All Clips"

### Keyboard Shortcuts

| Key | Action |
|-----|--------|
| **Spacebar** | Play/Pause |
| **I** | Set IN point |
| **O** | Set OUT point |
| **Left/Right** | Frame stepping |
| **Shift+Left/Right** | Time stepping (±1s) |
| **Ctrl+A** | Add current selection to clip list |
| **Ctrl+S** | Save all clips |
| **Ctrl+W** | Close window |

---

## Output Files

### Default Naming Pattern
```
{filename}_clip_{index}.{ext}
```

**Examples:**
```
video.mp4 → video_clip_01.mp4
          → video_clip_02.mp4
          → video_clip_03.mp4
```

### Output Location
- By default, clips are saved in the same folder as the source file
- Configurable via settings (future feature)

---

## Technical Details

### Architecture
- **Framework:** .NET 8 WPF
- **Video Processing:** FFMpegCore
- **Audio Processing:** NAudio
- **Logging:** Serilog

### File Locations
- **Executable:** `%LOCALAPPDATA%\RightClicks\RightClicksClipEditor.exe`
- **Logs:** `%LOCALAPPDATA%\RightClicks\logs\ClipEditor-*.log`
- **Settings:** `%LOCALAPPDATA%\RightClicks\ClipEditorSettings.json`

### Supported Formats

**Video:**
- MP4, AVI, MKV, MOV, WMV, FLV, WebM, M4V, MPG, MPEG

**Audio:**
- MP3, WAV, FLAC, M4A, AAC, OGG, WMA, Opus

---

## Export Modes

### Re-encoding Mode (Default)
- **Accuracy:** Frame-accurate
- **Speed:** Slower (re-encodes video)
- **Quality:** Configurable (CRF 18 default)
- **Use Case:** When precision is critical

### Stream Copy Mode
- **Accuracy:** Keyframe-accurate only
- **Speed:** Very fast (no re-encoding)
- **Quality:** Lossless (same as source)
- **Use Case:** When speed is more important than precision

---

## Configuration

### Settings File
Location: `%LOCALAPPDATA%\RightClicks\ClipEditorSettings.json`

```json
{
  "VideoOutputFormat": "mp4",
  "AudioOutputFormat": "mp3",
  "VideoCodec": "libx264",
  "AudioCodec": "libmp3lame",
  "VideoQuality": 18,
  "AudioBitrate": 192,
  "NamingPattern": "{filename}_clip_{index}",
  "UseSameFolder": true,
  "CustomOutputFolder": "",
  "UseStreamCopy": false,
  "LoopSelection": false,
  "DefaultZoomLevel": 1.0,
  "WindowWidth": 1000,
  "WindowHeight": 700
}
```

---

## Development

### Build

```powershell
dotnet build RightClicksClipEditor\RightClicksClipEditor.csproj --configuration Release
```

### Test

```powershell
.\RightClicksClipEditor\bin\Release\net8.0-windows\RightClicksClipEditor.exe --video "testfiles\test.mp4"
```

### View Logs

```powershell
Get-Content "$env:LOCALAPPDATA\RightClicks\logs\ClipEditor-*.log" | Select-Object -Last 50
```

---

## Roadmap

### Next Steps
1. ✅ Complete timeline control (visual timeline with draggable markers)
2. ⬜ Build audio editor with waveform visualization
3. ⬜ Integrate with RightClicks context menus
4. ⬜ Create settings window
5. ⬜ Comprehensive testing

### Future Enhancements
- Batch export progress bar
- Clip preview before export
- Undo/Redo for clip list
- Export presets (YouTube, Instagram, etc.)
- Fade in/out effects
- Volume adjustment per clip

---

## License

Part of the RightClicks project.


