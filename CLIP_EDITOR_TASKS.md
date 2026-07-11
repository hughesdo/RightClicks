# Lightweight Clip Editing - Implementation Tasks

**Status:** 🟡 Ready to Start  
**Created:** 2026-01-14  
**Specification:** See CLIP_EDITOR_SPECIFICATION.md

---

## Task Breakdown

### Phase 1: Project Setup & Infrastructure

#### Task 1.1: Create RightClicksClipEditor Project
- [ ] Create new .NET 8 WPF project in solution
- [ ] Configure project properties (WinExe, net8.0-windows, UseWPF)
- [ ] Add NuGet packages (FFMpegCore, NAudio, Serilog)
- [ ] Create folder structure (Windows/, Controls/, Services/, Models/, Resources/)
- [ ] Add application icon
- [ ] Configure post-build event to copy to RightClicks output

#### Task 1.2: Create App.xaml and Entry Point
- [ ] Create App.xaml with application resources
- [ ] Implement App.xaml.cs with command-line argument parsing
- [ ] Configure Serilog logging
- [ ] Implement media type detection
- [ ] Add error handling for missing files

#### Task 1.3: Create Data Models
- [ ] Create ClipSegment.cs (represents a clip with IN/OUT points)
- [ ] Create MediaInfo.cs (media file metadata)
- [ ] Create ExportSettings.cs (export configuration)
- [ ] Create ClipEditorSettings.cs (user preferences)

#### Task 1.4: Create SettingsService
- [ ] Implement settings persistence (JSON to %LOCALAPPDATA%)
- [ ] Create default settings
- [ ] Implement Load() and Save() methods

---

### Phase 2: Video Clip Editor (MVP)

#### Task 2.1: Create VideoClipEditorWindow XAML
- [ ] Create window layout with Grid rows
- [ ] Add MediaElement for video preview
- [ ] Add transport controls (Play, Pause, Step buttons)
- [ ] Add timeline placeholder (will be replaced with TimelineControl)
- [ ] Add clip list (ListBox)
- [ ] Add action buttons (Add Selection, Save All, Close)
- [ ] Apply Windows 11 styling (colors, rounded corners)

#### Task 2.2: Implement VideoClipEditorWindow Code-Behind
- [ ] Load video file in constructor
- [ ] Analyze media info using FFProbe
- [ ] Initialize MediaElement
- [ ] Implement Play/Pause handlers
- [ ] Implement frame stepping (detect FPS, step by 1 frame)
- [ ] Implement time stepping (±1 second)
- [ ] Wire up position update timer

#### Task 2.3: Create TimelineControl (Shared Component)
- [ ] Create UserControl with Canvas for timeline rendering
- [ ] Implement draggable IN/OUT markers
- [ ] Implement draggable playhead
- [ ] Implement zoom (mouse wheel)
- [ ] Implement horizontal scroll (Shift + mouse wheel)
- [ ] Add timecode labels
- [ ] Add selection highlight
- [ ] Expose events (PositionChanged, InPointChanged, OutPointChanged)

#### Task 2.4: Integrate TimelineControl into VideoClipEditorWindow
- [ ] Replace timeline placeholder with TimelineControl
- [ ] Wire up timeline events to video player
- [ ] Sync playhead with video position
- [ ] Sync IN/OUT markers with selection
- [ ] Implement click-to-seek on timeline

#### Task 2.5: Implement Clip List Management
- [ ] Add current selection to clip list
- [ ] Display clips with timecodes and duration
- [ ] Implement remove clip from list
- [ ] Implement enable/disable clip (checkbox)
- [ ] Implement clip selection in list

#### Task 2.6: Create ClipExportService
- [ ] Implement ExportVideoClip() with re-encoding
- [ ] Implement ExportVideoStreamCopy() for fast mode
- [ ] Add progress reporting (optional for MVP)
- [ ] Add cancellation support
- [ ] Handle FFmpeg errors

#### Task 2.7: Implement Save All Clips
- [ ] Generate output filenames using naming pattern
- [ ] Export each enabled clip
- [ ] Show progress indicator
- [ ] Show success/error messages
- [ ] Log export operations

#### Task 2.8: Implement Keyboard Shortcuts (Video)
- [ ] Spacebar: Play/Pause
- [ ] I: Set IN point
- [ ] O: Set OUT point
- [ ] Left/Right: Frame stepping
- [ ] Shift+Left/Right: Time stepping
- [ ] Ctrl+S: Save all clips
- [ ] Ctrl+W: Close window
- [ ] Ctrl+A: Add selection

---

### Phase 3: Audio Clip Editor (MVP)

#### Task 3.1: Create WaveformControl
- [ ] Create UserControl with Image for waveform display
- [ ] Implement WaveformGeneratorService
- [ ] Generate waveform from audio file (NAudio)
- [ ] Render waveform to WriteableBitmap
- [ ] Add zoom support
- [ ] Add scroll support

#### Task 3.2: Create AudioClipEditorWindow XAML
- [ ] Create window layout (similar to video editor)
- [ ] Add WaveformControl
- [ ] Add transport controls
- [ ] Add timeline (reuse TimelineControl)
- [ ] Add clip list
- [ ] Add action buttons
- [ ] Add loop selection checkbox
- [ ] Apply Windows 11 styling

#### Task 3.3: Implement AudioClipEditorWindow Code-Behind
- [ ] Load audio file in constructor
- [ ] Analyze media info
- [ ] Initialize NAudio playback (WaveOutEvent)
- [ ] Implement Play/Pause handlers
- [ ] Implement time stepping (±1s, ±10ms, ±1ms)
- [ ] Wire up position update timer
- [ ] Implement loop selection

#### Task 3.4: Integrate WaveformControl
- [ ] Generate waveform on file load
- [ ] Show loading indicator during generation
- [ ] Sync waveform with timeline
- [ ] Implement zoom on waveform
- [ ] Highlight selection on waveform

#### Task 3.5: Implement Audio Clip Export
- [ ] Implement ExportAudioClip() in ClipExportService
- [ ] Support MP3, WAV, FLAC formats
- [ ] Add sample-accurate seeking
- [ ] Handle export errors

#### Task 3.6: Implement Keyboard Shortcuts (Audio)
- [ ] Spacebar: Play/Pause
- [ ] I: Set IN point
- [ ] O: Set OUT point
- [ ] Left/Right: Time stepping (1s)
- [ ] Shift+Left/Right: Fine stepping (10ms)
- [ ] L: Toggle loop selection
- [ ] Ctrl+S: Save all clips
- [ ] Ctrl+W: Close window

---

### Phase 4: Integration with RightClicks

#### Task 4.1: Create VideoClipFeature
- [ ] Create Features/Clipping/VideoClipFeature.cs
- [ ] Implement IFileFeature interface
- [ ] Launch RightClicksClipEditor.exe with --video argument
- [ ] Handle missing executable error
- [ ] Return informational result (no job created)

#### Task 4.2: Create AudioClipFeature
- [ ] Create Features/Clipping/AudioClipFeature.cs
- [ ] Implement IFileFeature interface
- [ ] Launch RightClicksClipEditor.exe with --audio argument
- [ ] Handle missing executable error
- [ ] Return informational result (no job created)

#### Task 4.3: Update RightClicks Build Process
- [ ] Update RightClicks.csproj PostBuild to copy ClipEditor files
- [ ] Copy RightClicksClipEditor.exe to %LOCALAPPDATA%\RightClicks
- [ ] Copy RightClicksClipEditor.dll
- [ ] Copy RightClicksClipEditor.runtimeconfig.json
- [ ] Test deployment

---

### Phase 5: Settings & Configuration

#### Task 5.1: Create Settings Window
- [ ] Create SettingsWindow.xaml
- [ ] Add output format dropdowns (video/audio)
- [ ] Add codec selection
- [ ] Add quality/bitrate sliders
- [ ] Add naming pattern textbox
- [ ] Add output location selection
- [ ] Add stream copy checkbox
- [ ] Apply/Cancel buttons

#### Task 5.2: Integrate Settings into Editors
- [ ] Add Settings button to video editor
- [ ] Add Settings button to audio editor
- [ ] Load settings on startup
- [ ] Save settings on change
- [ ] Apply settings to export operations

---

### Phase 6: Testing & Polish

#### Task 6.1: Standalone Testing
- [ ] Test video editor with various formats (MP4, AVI, MKV, MOV)
- [ ] Test audio editor with various formats (MP3, WAV, FLAC)
- [ ] Test frame-accurate clipping (verify output duration)
- [ ] Test sample-accurate clipping (verify output duration)
- [ ] Test multiple clips per session
- [ ] Test keyboard shortcuts
- [ ] Test zoom and scroll
- [ ] Test edge cases (very short clips, very long files)

#### Task 6.2: Integration Testing
- [ ] Right-click video file → Video Clip Editor launches
- [ ] Right-click audio file → Audio Clip Editor launches
- [ ] Verify file path passed correctly
- [ ] Verify editor loads file
- [ ] Test from different directories

#### Task 6.3: Error Handling Testing
- [ ] Test with missing file
- [ ] Test with unsupported format
- [ ] Test with corrupted file
- [ ] Test with missing FFmpeg
- [ ] Test export failure scenarios
- [ ] Verify error messages are user-friendly

#### Task 6.4: Performance Testing
- [ ] Test with 4K video
- [ ] Test with large audio files (> 100MB)
- [ ] Test with long files (> 1 hour)
- [ ] Measure waveform generation time
- [ ] Measure export time
- [ ] Optimize if needed

#### Task 6.5: UI Polish
- [ ] Add tooltips to all buttons
- [ ] Add status bar with current position/duration
- [ ] Add loading indicators
- [ ] Add progress bars for export
- [ ] Improve visual feedback for markers
- [ ] Test window resizing
- [ ] Test on different screen resolutions

#### Task 6.6: Logging & Diagnostics
- [ ] Verify all operations are logged
- [ ] Test log file creation
- [ ] Test log rotation (if implemented)
- [ ] Add diagnostic info to logs (OS, .NET version, FFmpeg version)
- [ ] Test error logging

---

### Phase 7: Documentation & Deployment

#### Task 7.1: Update Documentation
- [ ] Update ARCHITECTURE.md with clip editor details
- [ ] Update TASKS.md with completed tasks
- [ ] Create user guide (optional)
- [ ] Document keyboard shortcuts
- [ ] Document settings

#### Task 7.2: Update install.bat
- [ ] Add RightClicksClipEditor.exe to deployment
- [ ] Add RightClicksClipEditor.dll to deployment
- [ ] Add RightClicksClipEditor.runtimeconfig.json to deployment
- [ ] Test installation on clean machine

#### Task 7.3: Final Testing
- [ ] Test full installation process
- [ ] Test uninstallation
- [ ] Test upgrade from previous version
- [ ] Verify all features work after installation
- [ ] Test on Windows 10 and Windows 11

---

## Task Dependencies

```
Phase 1 (Setup)
    ↓
Phase 2 (Video Editor) ←→ Phase 3 (Audio Editor)
    ↓                           ↓
    └───────────┬───────────────┘
                ↓
        Phase 4 (Integration)
                ↓
        Phase 5 (Settings)
                ↓
        Phase 6 (Testing)
                ↓
        Phase 7 (Deployment)
```

**Notes:**
- Phase 2 and Phase 3 can be developed in parallel
- TimelineControl (Task 2.3) is shared between both editors
- Phase 4 requires Phase 2 and Phase 3 to be complete
- Phase 6 testing should be done throughout development

---

## Estimated Time

| Phase | Tasks | Estimated Time |
|-------|-------|----------------|
| Phase 1: Setup | 4 tasks | 1-2 hours |
| Phase 2: Video Editor | 8 tasks | 4-6 hours |
| Phase 3: Audio Editor | 6 tasks | 3-4 hours |
| Phase 4: Integration | 3 tasks | 1-2 hours |
| Phase 5: Settings | 2 tasks | 1-2 hours |
| Phase 6: Testing | 6 tasks | 2-3 hours |
| Phase 7: Deployment | 3 tasks | 1 hour |
| **Total** | **32 tasks** | **13-20 hours** |

---

## Success Metrics

### MVP Complete When:
- [x] All Phase 1-4 tasks complete
- [x] Video editor can clip videos frame-accurately
- [x] Audio editor can clip audio sample-accurately
- [x] Multiple clips can be saved per session
- [x] Context menu integration works
- [x] Basic error handling in place
- [x] Logs are written

### Production Ready When:
- [x] All Phase 1-7 tasks complete
- [x] Settings window functional
- [x] All tests passing
- [x] Documentation updated
- [x] install.bat updated
- [x] Tested on clean machine

---

## Current Status

**Phase:** Not Started
**Completed Tasks:** 0 / 32
**Progress:** 0%

---

## Next Steps

1. **Create RightClicksClipEditor project** (Task 1.1)
2. **Set up App.xaml and entry point** (Task 1.2)
3. **Create data models** (Task 1.3)
4. **Begin video editor UI** (Task 2.1)

---

**Ready to begin implementation!**


