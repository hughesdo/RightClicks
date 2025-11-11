# RightClicks Development Tasks

**Last Updated:** 2025-11-11

**Current Phase:** Phase 1 - Foundation

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
  - [ ] Implement JSON config read/write
  - [x] Create default `config.json` template
  - [x] Test config loading and saving

- [ ] Implement feature discovery
  - [ ] Create `FeatureDiscovery.cs`
  - [ ] Implement reflection-based feature discovery
  - [ ] Test discovery with mock features

---

## Phase 2: First Feature (ExtractMp3)
*Implement one complete feature end-to-end to validate architecture*

- [ ] Implement ExtractMp3Feature
  - [ ] Create `ExtractMp3Feature.cs` in `Features/Video/`
  - [ ] Implement `IFileFeature` interface
  - [ ] Integrate FFMpegCore for audio extraction
  - [ ] Handle file naming and conflict resolution
  - [ ] Add comprehensive logging

- [ ] Implement CLI execution mode
  - [ ] Parse command line arguments (`--feature`, `--file`)
  - [ ] Execute feature directly from CLI
  - [ ] Test: `RightClicks.exe --feature ExtractMp3 --file "test.mp4" --test-mode`

- [ ] Test and validate
  - [ ] Test with various MP4 files
  - [ ] Verify output file created correctly
  - [ ] Verify file naming: `video.mp4` → `video.mp3`
  - [ ] Verify conflict resolution: `video (1).mp3`
  - [ ] Examine logs for errors
  - [ ] Performance test with large files

---

## Phase 3: UI - System Tray and Main Window
*Build the user interface for configuration and monitoring*

- [ ] Implement system tray icon
  - [ ] Create `SystemTrayIcon.cs`
  - [ ] Add tray icon with context menu
  - [ ] Add "Open RightClicks" menu item
  - [ ] Add "Exit" menu item
  - [ ] Test tray icon appears and responds

- [ ] Create main configuration window
  - [ ] Create `MainWindow.xaml` with tabbed interface
  - [ ] Implement tab switching logic
  - [ ] Apply Windows 11 styling

- [ ] Implement Main Config tab
  - [ ] Add shell hook management buttons
  - [ ] Add feature toggle controls (sliders)
  - [ ] Organize features by file type (.MP4, .MP3, etc.)
  - [ ] Wire up toggle events to update config.json

- [ ] Implement Queued Jobs tab
  - [ ] Create job list display
  - [ ] Show job status (Pending/Running/Complete/Failed)
  - [ ] Add Cancel and Remove buttons
  - [ ] Add "Max Concurrent Jobs" setting with Save button
  - [ ] Make this tab default when jobs are active

- [ ] Implement API Config tab
  - [ ] Add API key configuration UI
  - [ ] Environment variable name inputs
  - [ ] Save button

- [ ] Implement Clipboard tab
  - [ ] Placeholder for future clipboard features

---

## Phase 4: Job Queue System
*Implement background job processing and notifications*

- [ ] Implement JobQueueService
  - [ ] Create `JobQueueService.cs`
  - [ ] Implement queue with configurable concurrency
  - [ ] Implement job execution with async/await
  - [ ] Support CancellationToken for job cancellation
  - [ ] Implement 7-day job history retention

- [ ] Implement NotificationService
  - [ ] Create `NotificationService.cs`
  - [ ] Integrate Windows toast notifications
  - [ ] Show success notifications
  - [ ] Show failure notifications with error details

- [ ] Integrate queue with UI
  - [ ] Update Queued Jobs tab in real-time
  - [ ] Show job progress
  - [ ] Handle Cancel button clicks
  - [ ] Handle Remove button clicks

- [ ] Test job queue
  - [ ] Queue multiple jobs
  - [ ] Verify concurrent execution (3 jobs default)
  - [ ] Test job cancellation
  - [ ] Test job removal
  - [ ] Verify notifications appear

---

## Phase 5: More Features
*Implement additional video and audio features*

- [ ] Implement ExtractWavFeature
- [ ] Implement FirstFrameToJpgFeature
- [ ] Implement LastFrameToJpgFeature
- [ ] Implement ReverseVideoFeature
- [ ] Implement Forward2ReverseFeature
- [ ] Implement TimeStretchFeature (with dialog)
- [ ] Implement TranscribeMp3Feature

---

## Phase 6: Shell Integration
*Integrate with Windows Explorer context menus*

- [ ] Implement shell hook manager
  - [ ] Create `RightClicksShellManager` project
  - [ ] Integrate SharpShell
  - [ ] Implement context menu handler
  - [ ] Read config.json to determine enabled features
  - [ ] Register context menu items per file type

- [ ] Implement shell hook installation
  - [ ] Create `/install` command line option
  - [ ] Write registry keys for enabled features
  - [ ] Test registration with `regsvr32` or SharpShell tools

- [ ] Implement shell hook uninstallation
  - [ ] Create `/uninstall` command line option
  - [ ] Remove registry keys
  - [ ] Test unregistration

- [ ] Create install.bat
  - [ ] Copy files to `%LOCALAPPDATA%\RightClicks\`
  - [ ] Create default config.json
  - [ ] Register shell hooks
  - [ ] Create startup shortcut

- [ ] Create uninstall.bat
  - [ ] Unregister shell hooks
  - [ ] Remove startup shortcut
  - [ ] Optionally delete files

- [ ] Test shell integration
  - [ ] Right-click .mp4 file in Explorer
  - [ ] Verify RightClicks menu appears
  - [ ] Verify correct features shown
  - [ ] Test feature execution from context menu
  - [ ] Verify job appears in queue

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

