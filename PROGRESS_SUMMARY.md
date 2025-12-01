# RightClicks - Progress Summary

**Date:** 2025-11-12  
**Status:** Core Features Complete ✅ - Ready for Feature Expansion

---

## 🎉 Major Milestones Achieved

### ✅ Phase 1-4: Foundation & Core Features (COMPLETE)
- **CLI & Feature System** - 4 working video features with FFmpeg integration
- **Job Queue System** - Background processing with configurable concurrency
- **System Tray Application** - Minimized by default, opens on user action
- **Main Configuration Window** - Feature toggles, concurrent jobs slider, action buttons
- **Windows Balloon Notifications** - Auto-dismissing toast notifications (5 seconds)

### ✅ Phase 6: Shell Integration (COMPLETE!)
- **Windows Explorer Context Menu** - Right-click integration working perfectly
- **Dynamic Feature Discovery** - Context menu shows only enabled features per file type
- **Single-Instance Enforcement** - Only one RightClicks tray icon ever appears
- **IPC Communication** - Named pipes for inter-process job requests
- **Shell Extension** - .NET Framework 4.8 + SharpShell 2.7.2
- **Shell Installer** - Admin-elevated install/uninstall with Windows 11 approval

---

## 📊 Current Feature Set

### Video Features (.mp4, .avi, .mkv, .mov, .webm)
1. ✅ **Extract MP3** - Extract audio as MP3 (192 kbps)
2. ✅ **Extract WAV** - Extract audio as WAV (44.1 kHz, 16-bit PCM)
3. ✅ **First Frame to JPG** - Capture first video frame
4. ✅ **Last Frame to JPG** - Capture last video frame (0.1s before end)

**All features:**
- Execute via CLI: `RightClicks.exe --feature <FeatureId> --file "<path>" --queue`
- Execute via context menu: Right-click file → "Show more options" → RightClicks → Select feature
- Jobs appear in Queued Jobs tab with real-time status updates
- Notifications appear on completion/failure

---

## 🎯 Proposed Easy-Win Features (Next Phase)

### High Priority - Simple FFmpeg Operations

#### Video Features
1. **ReverseVideo** - Reverse video playback
   - Command: `ffmpeg -i input.mp4 -vf reverse -af areverse output.mp4`
   - Output: `{basename}_Reverse.mp4`
   - Complexity: LOW ⭐

2. **Forward2Reverse** - Original + reversed concatenation
   - Two-step: Create reversed copy, then concat
   - Output: `{basename}_Forward2Reverse.mp4`
   - Complexity: MEDIUM ⭐⭐

3. **MuteVideo** - Remove audio track (stream copy, no re-encoding)
   - Command: `ffmpeg -i input.mp4 -an -c:v copy output.mp4`
   - Output: `{basename}_Muted.mp4`
   - Complexity: LOW ⭐

4. **RotateVideo** - Rotate 90°/180°/270°
   - Command: `ffmpeg -i input.mp4 -vf "transpose=1" output.mp4`
   - Output: `{basename}_Rotate90.mp4`
   - Complexity: LOW ⭐
   - Could add submenu for rotation angles

#### Image Features (.jpg, .png, .bmp, .webp)
5. **JpgToPng** - Convert JPG to PNG
   - Command: `ffmpeg -i input.jpg output.png`
   - Complexity: LOW ⭐

6. **PngToJpg** - Convert PNG to JPG
   - Command: `ffmpeg -i input.png output.jpg`
   - Complexity: LOW ⭐

7. **ImageToWebP** - Convert any image to WebP
   - Command: `ffmpeg -i input.jpg -c:v libwebp output.webp`
   - Complexity: LOW ⭐

#### Text Features (.txt)
8. **ContentToClipboard** - Copy file contents to clipboard
   - Pure C# implementation (File.ReadAllText + Clipboard.SetText)
   - Complexity: LOW ⭐

9. **ClipboardToFile** - Write clipboard to empty file
   - Pure C# implementation (Clipboard.GetText + File.WriteAllText)
   - Complexity: LOW ⭐

### Medium Priority - Requires Dialogs or More Complex Logic

10. **ResizeImage** - Resize to common presets or custom size
    - Submenu with presets (1920x1080, 1280x720, 640x480, custom)
    - Complexity: MEDIUM ⭐⭐

11. **TimeStretch** - Stretch/compress video duration
    - Requires dialog for duration input
    - Complex FFmpeg filters (setpts + atempo)
    - Complexity: HIGH ⭐⭐⭐

### Low Priority - Requires API Integration

12. **TranscribeMp3** - Transcribe MP3 to text using OpenAI
    - Requires API Config tab implementation
    - Requires OpenAI API key
    - Complexity: HIGH ⭐⭐⭐

---

## 🚀 Recommended Next Steps

### Option A: Quick Wins (Recommended)
Implement 3-5 simple features to expand utility quickly:
1. **ReverseVideo** (30 min)
2. **MuteVideo** (20 min)
3. **JpgToPng** (20 min)
4. **PngToJpg** (20 min)
5. **ContentToClipboard** (15 min)

**Total time:** ~2 hours for 5 new features

### Option B: Focus on Video
Implement all simple video features:
1. **ReverseVideo**
2. **Forward2Reverse**
3. **MuteVideo**
4. **RotateVideo** (with submenu)

**Total time:** ~2-3 hours for 4 video features

### Option C: Image Conversion Suite
Implement all image conversion features:
1. **JpgToPng**
2. **PngToJpg**
3. **ImageToWebP**
4. **ResizeImage** (with dialog)

**Total time:** ~2-3 hours for 4 image features

---

## 📝 Notes

- All features use the same pattern as existing features (implement `IFileFeature`)
- Automatic discovery via reflection - no manual registration needed
- Features automatically appear in UI and context menu when enabled
- Testing via CLI is fast: `RightClicks.exe --feature <FeatureId> --file "<path>" --test-mode`
- Shell extension automatically picks up new features after rebuild

---

## 🎯 Your Decision

**Don, which approach would you like to take?**

1. **Option A** - Quick wins across multiple file types (5 features, ~2 hours)
2. **Option B** - Focus on video features (4 features, ~2-3 hours)
3. **Option C** - Image conversion suite (4 features, ~2-3 hours)
4. **Custom** - Pick specific features from the list above
5. **Other** - Different priority or approach

Let me know and I'll implement the features you choose!

