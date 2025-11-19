# RightClicks Development Tasks

**Last Updated:** 2025-11-19

**Current Phase:** Phase 5 - Additional Features (17 features complete!)

---

## 📊 Current Status Summary

### ✅ **Completed:**
- **Phase 1: Foundation** - Core infrastructure, interfaces, models, logging, configuration
- **Phase 2: CLI & Features** - All 4 video features working (ExtractMp3, ExtractWav, FirstFrameToJpg, LastFrameToJpg)
- **Phase 3: UI** - System tray icon, MainWindow with 4 tabs, Main Config tab complete with all action buttons
- **Phase 4: Job Queue System** - JobQueueService, Queued Jobs tab UI, CLI integration, IPC for single-instance
- **Phase 4: Notifications** - Windows balloon notifications for job completion/failure (auto-dismiss after 5 seconds)
- **Phase 6: Shell Integration** - Windows Explorer context menu integration WORKING! ✅
  - RightClicksShellExtension (.NET Framework 4.8 + SharpShell)
  - RightClicksShellInstaller (install/uninstall with admin elevation)
  - Single-instance enforcement with IPC (named pipes)
  - Context menu shows enabled features per file type
  - Jobs sent to running instance via IPC

### 🚧 **In Progress:**
- **Phase 5: Additional Features** - Identifying easy-win features to implement next

### ⏳ **Not Started:**
- **Phase 3: API Config Tab** - OpenAI API key configuration UI (needed for TranscribeMp3)
- **Phase 3: Clipboard Tab** - Features from TransformClipboard project
- **Phase 5: More Features** - ReverseVideo, Forward2Reverse, TimeStretch, TranscribeMp3, Image conversions
- **Phase 7: Polish & Testing** - Update checker, FFmpeg bundling, comprehensive testing

### 🎯 **Next Immediate Steps:**
1. Identify and implement 3-5 easy-win features (simple FFmpeg operations)
2. Test all features end-to-end via context menu
3. Implement ReverseVideo feature (simple FFmpeg reverse filter)
4. Implement Forward2Reverse feature (concatenate original + reversed)
5. Consider image conversion features (JPG ↔ PNG, WebP)

---

## Phase 1: Foundation
*Setting up core infrastructure, solution structure, and base interfaces*

- [x] Create Visual Studio solution and projects
  - [x] Create `RightClicks.sln`
  - [x] Create `RightClicks` project (.NET 8, WPF)
  - [x] Create `RightClicksShellManager` project (.NET Framework 4.8)
  - [x] Add NuGet packages (FFMpegCore, Serilog, Newtonsoft.Json, etc.)
  - [x] Verify solution builds successfully

- [x] Implement core interfaces and models
  - [x] Create `IFileFeature` interface
  - [x] Create `FeatureResult` model
  - [x] Create `Job` model
  - [x] Create `AppConfig` model
  - [x] Create `FeatureConfig` model

- [x] Set up logging infrastructure
  - [x] Configure Serilog with file sink
  - [x] Implement test mode logging (isolated log files)
  - [x] Implement log retention (7-day rolling)
  - [x] Add CLI flags: `--test-mode`, `--clear-logs`

- [x] Implement configuration service
  - [x] Create `ConfigurationService.cs`
  - [x] Implement JSON config read/write
  - [x] Create default `config.json` template
  - [x] Test config loading and saving

- [x] Implement feature discovery
  - [x] Create `FeatureDiscoveryService.cs`
  - [x] Implement reflection-based feature discovery
  - [x] Test discovery with mock features

---

## Phase 2: CLI & Feature Implementation
*Implement CLI and all video features end-to-end*

- [x] Implement CLI execution mode
  - [x] Parse command line arguments (`--feature`, `--file`, `--test-mode`, `--clear-logs`)
  - [x] Execute feature directly from CLI
  - [x] Fix WPF shutdown issue (UI thread deadlock)
  - [x] Test: `RightClicks.exe --feature ExtractMp3 --file "test.mp4" --test-mode`

- [x] Implement ExtractMp3Feature
  - [x] Create `ExtractMp3Feature.cs` in `Features/Video/`
  - [x] Implement `IFileFeature` interface
  - [x] Integrate FFMpegCore for audio extraction
  - [x] Handle file naming and conflict resolution
  - [x] Add comprehensive logging
  - [x] Test with real video files

- [x] Implement ExtractWavFeature
  - [x] Create `ExtractWavFeature.cs` in `Features/Video/`
  - [x] Implement WAV audio extraction
  - [x] Test with real video files

- [x] Implement FirstFrameToJpgFeature
  - [x] Create `FirstFrameToJpgFeature.cs` in `Features/Video/`
  - [x] Implement first frame capture
  - [x] Test with real video files

- [x] Implement LastFrameToJpgFeature
  - [x] Create `LastFrameToJpgFeature.cs` in `Features/Video/`
  - [x] Implement last frame capture
  - [x] Test with real video files

- [x] Test and validate all features
  - [x] Test with various MP4 files
  - [x] Verify output files created correctly
  - [x] Verify file naming conventions
  - [x] Examine logs for errors
  - [x] All features working successfully

---

## Phase 3: UI - System Tray and Main Window
*Build the user interface for configuration and monitoring*

- [x] Implement system tray icon
  - [x] Add NotifyIcon with context menu (using System.Windows.Forms)
  - [x] Add "Open RightClicks" menu item
  - [x] Add "Exit" menu item
  - [x] Double-click to open main window
  - [x] Test tray icon appears and responds
  - [x] App starts minimized to tray only (no MainWindow auto-open)
  - [x] Single-instance enforcement with mutex
  - [x] Windows balloon notifications for job completion/failure

- [x] Create main configuration window
  - [x] Create `MainWindow.xaml` with tabbed interface (4 tabs)
  - [x] Implement tab switching logic
  - [x] Apply Windows 11 styling (modern colors, rounded corners)
  - [x] Implement minimize-to-tray behavior
  - [x] Single MainWindow instance (reused when opened from tray)

- [x] Implement Main Config tab - Feature Management
  - [x] Add feature list display with checkboxes
  - [x] Show feature details (name, description, supported extensions)
  - [x] Add concurrent jobs slider (1-10, default: 3)
  - [x] Wire up toggle events to update config.json
  - [x] Add Save Configuration button
  - [x] Add four action buttons:
    - [x] Install Shell Hooks button (runs RightClicksShellInstaller.exe /install with admin elevation)
    - [x] Uninstall Shell Hooks button (runs RightClicksShellInstaller.exe /uninstall with admin elevation)
    - [x] Check for Updates button (placeholder - queries GitHub API)
    - [x] Open Logs Folder button (opens %LOCALAPPDATA%\RightClicks\logs\)

- [x] Implement API Config tab
  - [x] Add API key configuration UI (DataGrid for standard APIs)
  - [x] Add dedicated Cloudinary configuration section
  - [x] Cloud Name, API Key, API Secret fields with password masking
  - [x] Environment variable creation (CLOUDINARY_API_KEY, CLOUDINARY_API_SECRET)
  - [x] Save button with validation

- [ ] Implement Clipboard tab
  - [ ] Placeholder for future clipboard features (from TransformClipboard project)

---

## Phase 4: Job Queue System
*Implement background job processing and notifications*

- [x] Implement JobQueueService
  - [x] Create `JobQueueService.cs` (382 lines)
  - [x] Implement queue with configurable concurrency (SemaphoreSlim)
  - [x] Implement job execution with async/await
  - [x] Support CancellationToken for job cancellation
  - [x] Implement 7-day job history retention (automatic cleanup)
  - [x] Add real-time events (JobAdded, JobStatusChanged, JobRemoved)
  - [x] Thread-safe operations with lock statements
  - [x] ObservableCollection<Job> for UI binding
  - [x] Background timers for processing (500ms) and cleanup (hourly)

- [x] Implement Queued Jobs Tab UI
  - [x] Create DataGrid with job list display
  - [x] Show status icons (⏳ Pending, ▶️ Running, ✅ Completed, ❌ Failed, 🚫 Cancelled)
  - [x] Show job details (feature name, file name, status, duration)
  - [x] Add Cancel button (for running jobs)
  - [x] Add Remove button (for pending jobs)
  - [x] Add Clear Completed button
  - [x] Add status bar (running/pending/completed/failed counts)
  - [x] Add empty state message
  - [x] Implement JobViewModel with INotifyPropertyChanged
  - [x] Wire up event handlers (Cancel, Remove, Clear Completed)
  - [x] Add DispatcherTimer for elapsed time updates (1 second interval)

- [x] Integrate JobQueueService with App
  - [x] Initialize JobQueueService in App.xaml.cs OnStartup()
  - [x] Pass max concurrent jobs from config
  - [x] Dispose JobQueueService in OnExit()
  - [x] Make JobQueueService accessible from MainWindow

- [x] Update CLI to use JobQueueService
  - [x] Added `--queue` flag to use JobQueueService instead of direct execution
  - [x] Shell extension uses `--queue` flag when calling RightClicks.exe
  - [x] Jobs added to queue appear in Queued Jobs tab
  - [x] App stays running when `--queue` is used (doesn't exit after adding job)

- [x] Implement single-instance enforcement with IPC
  - [x] Create `IpcService.cs` using named pipes
  - [x] Mutex-based single-instance detection
  - [x] New instances send jobs to existing instance via IPC
  - [x] Only one system tray icon ever appears
  - [x] All jobs processed by single running instance

- [x] Implement Windows balloon notifications
  - [x] Success notifications (5 seconds, auto-dismiss)
  - [x] Failure notifications (5 seconds, auto-dismiss)
  - [x] Notifications appear near system tray
  - [x] Event handler in App.xaml.cs (OnJobStatusChanged)

---

## Phase 5: Additional Features
*Implement more video, audio, image, and text features*

### ✅ Cloud-Based Features Integration (COMPLETE!)

- [x] **Cloudinary File Hosting Service**
  - [x] CloudinaryStorageService.cs - Upload and delete files
  - [x] Unsigned upload preset configuration
  - [x] Automatic file deletion after processing (success or failure)
  - [x] Dedicated Cloudinary configuration UI in API Config tab
  - [x] Environment variable management (CLOUDINARY_API_KEY, CLOUDINARY_API_SECRET)
  - [x] Complete documentation in cloudinary.md

- [x] **fal.ai Lip Sync Integration**
  - [x] FalAiService.cs - Generic fal.ai API client
  - [x] FalAiLipSyncFeatureBase.cs - Base class for all lip sync features
  - [x] 5 lip sync models implemented (Pixverse, VEED, Kling, Creatify, Sync)
  - [x] Automatic MP3 extraction from video files
  - [x] Cloudinary integration for file hosting (required by fal.ai)
  - [x] Pricing displayed in menu names ($0.20/min vs $0.40/min)
  - [x] Comprehensive error handling and logging

### ✅ Completed Features (17 total)

#### Video Features (11 features)
- [x] ExtractMp3Feature - Extract MP3 audio from video
- [x] ExtractWavFeature - Extract WAV audio from video
- [x] FirstFrameToJpgFeature - Capture first frame as JPG
- [x] LastFrameToJpgFeature - Capture last frame as JPG
- [x] ReverseVideoFeature - Reverse video playback
- [x] Forward2ReverseFeature - Concatenate original + reversed
- [x] WebmToMp4Feature - Convert WebM to MP4
- [x] FalAiPixverseLipSyncFeature - AI lip sync ($0.20/min) ☁️
- [x] FalAiVeedLipSyncFeature - AI lip sync ($0.40/min) ☁️
- [x] FalAiKlingLipSyncFeature - AI lip sync ($0.40/min) ☁️
- [x] FalAiCreatifyLipSyncFeature - AI lip sync ($0.40/min) ☁️
- [x] FalAiSyncLipSyncFeature - AI lip sync ($0.40/min) ☁️

#### Audio Features (1 feature)
- [x] WavToMp3Feature - Convert WAV to MP3

#### Image Features (3 features)
- [x] JpgToPngFeature - Convert JPG to PNG
- [x] PngToJpgFeature - Convert PNG to JPG
- [x] WebpToJpgFeature - Convert WebP to JPG

#### Text Features (2 features)
- [x] ContentToClipboardFeature - Copy file contents to clipboard
- [x] ClipboardToFileFeature - Paste clipboard to empty file

### 🎯 Potential Future Features
*Additional features that could be implemented*

#### Video Features (.mp4, .avi, .mkv, .mov, .webm)

- [ ] **RotateVideoFeature** - Rotate video 90°/180°/270°
  - FFmpeg command: `ffmpeg -i input.mp4 -vf "transpose=1" output.mp4`
  - Output: `{basename}_Rotate90.mp4` (or 180, 270)
  - Complexity: LOW (single FFmpeg filter)
  - UI: Could add submenu for rotation angles

- [ ] **MuteVideoFeature** - Remove audio track entirely
  - FFmpeg command: `ffmpeg -i input.mp4 -an -c:v copy output.mp4`
  - Output: `{basename}_Muted.mp4`
  - Complexity: LOW (stream copy, no re-encoding)

#### Image Features (.jpg, .png, .bmp, .webp)
- [ ] **ImageToWebPFeature** - Convert any image to WebP
  - FFmpeg command: `ffmpeg -i input.jpg -c:v libwebp output.webp`
  - Output: `{basename}.webp`
  - Complexity: LOW (simple format conversion)

- [ ] **ResizeImageFeature** - Resize image to common sizes
  - FFmpeg command: `ffmpeg -i input.jpg -vf scale=1920:1080 output.jpg`
  - Output: `{basename}_1920x1080.jpg`
  - Complexity: MEDIUM (might want dialog for custom sizes)
  - UI: Submenu with presets (1920x1080, 1280x720, 640x480, etc.)



### 🔮 Advanced Features (Later)
*More complex features requiring dialogs or API integration*

- [ ] **TimeStretchFeature** - Stretch/compress video duration
  - Requires dialog for duration input
  - FFmpeg: setpts filter for video, atempo for audio
  - Complexity: HIGH (dialog + complex FFmpeg filters)

- [ ] **TranscribeMp3Feature** - Transcribe MP3 to text
  - Requires OpenAI API integration
  - Requires API Config tab implementation
  - Complexity: HIGH (API integration + error handling)

---

## Phase 6: Shell Integration ✅ COMPLETE!
*Integrate with Windows Explorer context menus*

- [x] Implement shell extension
  - [x] Create `RightClicksShellExtension` project (.NET Framework 4.8)
  - [x] Integrate SharpShell 2.7.2
  - [x] Implement `RightClicksContextMenu` class (SharpContextMenu)
  - [x] Read config.json to determine enabled features
  - [x] Filter features by file extension
  - [x] Create dynamic context menu items per file type
  - [x] Handle feature execution (call RightClicks.exe with --feature --file --queue)

- [x] Implement shell installer
  - [x] Create `RightClicksShellInstaller` project (.NET Framework 4.8)
  - [x] Use SharpShell ServerRegistrationManager
  - [x] Implement `/install` command (requires admin elevation)
  - [x] Implement `/uninstall` command (requires admin elevation)
  - [x] Register shell extension DLL with Windows
  - [x] Approve shell extension in Windows 11
  - [x] Test registration and unregistration

- [x] Fix assembly loading issues
  - [x] Resolved System.Runtime version conflicts (.NET Framework 4.8 vs .NET 6.0)
  - [x] Added correct Newtonsoft.Json for .NET Framework 4.8
  - [x] Fixed property name mismatch (IsEnabled vs Enabled)
  - [x] Verified all dependencies load correctly

- [x] Test shell integration end-to-end
  - [x] Right-click .mp4 file in Explorer → "Show more options" (Windows 11)
  - [x] Verify "RightClicks" menu appears with cascading features
  - [x] Verify only enabled features shown
  - [x] Test feature execution from context menu
  - [x] Verify job appears in queue
  - [x] Verify notification appears on completion
  - [x] Verify only one system tray icon appears (single-instance with IPC)

---

## Phase 7: Polish & Testing
*Final refinements, testing, and documentation*

- [ ] Implement update checker
  - [ ] Create `UpdateService.cs`
  - [ ] Check GitHub for version.json
  - [ ] Show notification when update available
  - [ ] Add "Check for Updates" button in UI

- [ ] Bundle FFmpeg binaries
  - [ ] Download ffmpeg.exe and ffprobe.exe
  - [ ] Add to project as embedded resources or copy to output
  - [ ] Configure FFMpegCore to use bundled binaries

- [ ] Create README.md
  - [ ] Installation instructions
  - [ ] Feature list
  - [ ] Screenshots
  - [ ] Troubleshooting

- [ ] Comprehensive testing
  - [ ] Test all features with various file types
  - [ ] Test error handling (invalid files, missing FFmpeg, etc.)
  - [ ] Test on clean Windows 10 and Windows 11 machines
  - [ ] Performance testing with large files
  - [ ] Stress testing with many queued jobs

- [ ] Create GitHub release
  - [ ] Package as ZIP with all files
  - [ ] Create version.json manifest
  - [ ] Write release notes
  - [ ] Upload to GitHub releases

---

## Future Enhancements
*Ideas for future development*

- [ ] Image conversion features (JPG ↔ PNG, WebP)
- [ ] Text file features (clipboard integration)
- [ ] GLSL shader conversion features
- [ ] Plugin architecture for external features
- [ ] MSI/MSIX installer
- [ ] Automatic delta updates with Squirrel.Windows
- [ ] Localization (multiple languages)
- [ ] Dark mode theme
- [ ] Custom output folder configuration
- [ ] Batch processing (multiple files at once)

---

## Notes

- Tasks marked `[x]` are complete and approved by Don
- Tasks marked `[ ]` are pending or in progress
- Only mark tasks complete after Don personally tests and says "move on"
- This file is the source of truth for project progress

