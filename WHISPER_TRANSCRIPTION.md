# Whisper Transcription Feature - Implementation Plan

**Last Updated:** 2025-11-19  
**Status:** Planning Phase  
**Feature Category:** Audio/Video Transcription (Local AI)

---

## Overview

Add local AI-powered transcription to RightClicks using OpenAI's Whisper model via Whisper.net. This feature runs **entirely offline** with zero API costs after initial setup.

**Key Benefits:**
- ✅ **100% Local** - No cloud API required, no internet needed after model download
- ✅ **Zero Ongoing Costs** - One-time model download, unlimited transcriptions
- ✅ **Privacy-First** - Audio never leaves your machine
- ✅ **GPU Acceleration** - Automatic GPU detection with silent CPU fallback
- ✅ **Multi-Format Support** - Audio and video files (FFmpeg integration)

---

## Feature Specification

### Context Menu Structure

```
Right-click on audio/video file
└── Transcribe ▶
    ├── Tiny (fastest, ~1 GB VRAM)
    ├── Base (fast, ~1 GB VRAM)
    ├── Small (balanced, ~2 GB VRAM)
    ├── Medium (accurate, ~5 GB VRAM)
    ├── Large (best quality, ~10 GB VRAM)
    └── Turbo (fast + accurate, ~6 GB VRAM)
```

### Supported File Formats

**Audio Files:**
- `.mp3`, `.wav`, `.flac`, `.m4a`, `.ogg`, `.aac`, `.opus`, `.mpga`, `.webm`

**Video Files:**
- `.mp4`, `.mpeg`, `.mov`, `.avi`, `.mkv`, `.wmv`, `.flv`, `.webm`
- Audio will be extracted automatically using FFmpeg

### Output

**File Naming:** `{original_name}.txt` (next to source file)

**Example:**
- Input: `meeting_recording.mp4`
- Output: `meeting_recording.txt`

---

## Architecture Integration

### 1. Feature Implementation Pattern

Following existing RightClicks patterns:

**Base Class:** `WhisperTranscribeFeatureBase.cs`
- Shared logic for all Whisper models
- GPU/CPU detection and fallback
- Model download and caching
- Audio extraction (FFmpeg)
- Transcription execution
- Error handling and logging

**Model-Specific Features:** (6 classes)
- `WhisperTinyTranscribeFeature.cs`
- `WhisperBaseTranscribeFeature.cs`
- `WhisperSmallTranscribeFeature.cs`
- `WhisperMediumTranscribeFeature.cs`
- `WhisperLargeTranscribeFeature.cs`
- `WhisperTurboTranscribeFeature.cs`

Each model class inherits from base and specifies:
- `Id` (e.g., "WhisperTinyTranscribe")
- `DisplayName` (e.g., "Transcribe > Tiny (fastest)")
- `WhisperModelType` (e.g., `WhisperGgmlType.Tiny`)

### 2. Folder Structure

```
RightClicks/
├── Features/
│   └── Audio/
│       ├── WavToMp3Feature.cs (existing)
│       ├── WhisperTranscribeFeatureBase.cs (NEW)
│       ├── WhisperTinyTranscribeFeature.cs (NEW)
│       ├── WhisperBaseTranscribeFeature.cs (NEW)
│       ├── WhisperSmallTranscribeFeature.cs (NEW)
│       ├── WhisperMediumTranscribeFeature.cs (NEW)
│       ├── WhisperLargeTranscribeFeature.cs (NEW)
│       └── WhisperTurboTranscribeFeature.cs (NEW)
├── Services/
│   └── WhisperService.cs (NEW - model management, GPU detection)
└── Models/
    └── Whisper/
        └── WhisperTranscriptionResult.cs (NEW)
```

### 3. Model Storage Location

**Path:** `%LOCALAPPDATA%\RightClicks\models\whisper\`

**Example:**
```
C:\Users\Don\AppData\Local\RightClicks\models\whisper\
├── ggml-tiny.en.bin
├── ggml-base.en.bin
├── ggml-small.en.bin
├── ggml-medium.en.bin
├── ggml-large-v3.bin
└── ggml-turbo.bin
```

**Model Download:**
- First use of each model triggers automatic download
- Downloaded via `WhisperGgmlDownloader.GetEnglishModelAsync()`
- Cached locally for subsequent uses
- User sees "Downloading model..." in job queue

---

## Implementation Details

### NuGet Packages Required

```xml
<PackageReference Include="Whisper.net" Version="1.7.0" />
<PackageReference Include="Whisper.net.Runtime" Version="1.7.0" />
<PackageReference Include="Whisper.net.Runtime.Cuda" Version="1.7.0" />
```

**Note:** `Whisper.net.Runtime.Cuda` is optional but recommended for GPU acceleration.

### GPU Detection Strategy

```csharp
// Silent fallback - no user alerts
try
{
    builder.WithGpuEnabled();
    Log.Information("GPU acceleration enabled for Whisper transcription");
}
catch (Exception ex)
{
    Log.Warning(ex, "GPU not available, falling back to CPU mode");
    // Continue with CPU - no user notification
}
```

**Logging Only:**
- ✅ Log GPU availability at INFO level
- ✅ Log CPU fallback at WARNING level
- ❌ NO MessageBox or balloon notifications
- ❌ NO UI alerts about GPU/CPU mode

### Audio Extraction (Video Files)

For video files, extract audio to temporary WAV file:

```csharp
// Use FFMpegCore (already in project)
var tempWavPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.wav");

await FFMpegArguments
    .FromFileInput(videoFilePath)
    .OutputToFile(tempWavPath, overwrite: true, options => options
        .WithAudioCodec("pcm_s16le")
        .WithAudioSamplingRate(16000)
        .ForceFormat("wav"))
    .ProcessAsynchronously(cancellationToken);

// Transcribe the WAV file
// Delete temp file after transcription
```

---

## Phase 2 - Future Enhancement (NOT IMPLEMENTED NOW)

### LLM Post-Processing

**Goal:** Clean up transcription with AI (grammar, punctuation, paragraphs)

**Implementation:**
- Add checkbox in UI: "Post-process with LLM"
- Use existing API Config system (OpenAI, Claude, etc.)
- Send transcription text to LLM with prompt:
  ```
  Clean up this transcription: fix grammar, add punctuation, organize into paragraphs.
  Preserve the original meaning and wording as much as possible.
  ```
- Save cleaned version as `{original_name}_cleaned.txt`

**Status:** Documented for future, not implementing in Phase 1

---

## Testing Plan

### Test Files Needed

1. **Audio Files:**
   - `test_audio.mp3` (short, 30 seconds)
   - `test_audio.wav` (short, 30 seconds)
   - `test_audio_long.mp3` (5+ minutes)

2. **Video Files:**
   - `test_video.mp4` (short, 30 seconds with speech)
   - `test_video_long.mp4` (5+ minutes with speech)

### Test Scenarios

1. **Model Download:**
   - First use of Tiny model → downloads automatically
   - Second use of Tiny model → uses cached version

2. **GPU/CPU Fallback:**
   - Test on GPU machine → verify GPU used (check logs)
   - Test on CPU-only machine → verify silent fallback (check logs)

3. **Audio Extraction:**
   - Test with MP4 video → verify audio extracted
   - Test with MP3 audio → verify direct transcription

4. **Output Files:**
   - Verify `.txt` file created next to source
   - Verify transcription accuracy (manual review)

5. **Cancellation:**
   - Start transcription, cancel from job queue
   - Verify cleanup (temp files deleted)

---

## Success Criteria

✅ All 6 Whisper models available in context menu  
✅ Automatic model download on first use  
✅ GPU acceleration with silent CPU fallback  
✅ Audio extraction from video files (FFmpeg)  
✅ Transcription saved as `.txt` file  
✅ Comprehensive logging (no user alerts)  
✅ Job queue integration (progress tracking)  
✅ Cancellation support (cleanup temp files)  
✅ No errors in logs  
✅ Accurate transcriptions (manual verification)

---

## Next Steps

1. **Review this plan with Don** - Confirm approach aligns with vision
2. **Install NuGet packages** - Whisper.net, Whisper.net.Runtime, Whisper.net.Runtime.Cuda
3. **Create WhisperService.cs** - Model management, GPU detection
4. **Create WhisperTranscribeFeatureBase.cs** - Shared logic
5. **Create 6 model-specific feature classes** - Tiny, Base, Small, Medium, Large, Turbo
6. **Test with CLI** - Verify each model works
7. **Test via context menu** - Verify shell integration
8. **Update documentation** - README.md, TASKS.md

---

**Ready to proceed when Don approves this plan!**

