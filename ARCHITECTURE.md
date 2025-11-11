# RightClicks - Application Architecture

**Status:** In Planning  
**Last Updated:** 2025-11-10  
**Target Release:** TBD

---

## Overview

RightClicks is a modern Windows context menu extension system that provides file-type-specific actions through a system tray application with configurable features. This document outlines the architectural decisions and technical foundation for the project.

---

## 1. Technology Stack & Framework ✅ DECIDED

### Main Application
- **Framework:** .NET 8 (LTS - supported until November 2026)
- **UI Framework:** WPF (Windows Presentation Foundation)
- **Language:** C# 12
- **Target Platform:** Windows 10/11 (x64)

**Rationale:**
- .NET 8 LTS provides long-term stability and support
- Full compatibility with required NuGet packages (NAudio, FFMpegCore, Xabe.FFmpeg, FFMediaToolkit)
- WPF is mature, well-documented, and excellent for system tray applications
- Modern C# features while maintaining practical development velocity
- Strong tooling support in both VS Code and Visual Studio 2022

### Shell Hook Manager (Separate Component)
- **Framework:** .NET Framework 4.8
- **Shell Integration:** SharpShell
- **Purpose:** Standalone program for registering/unregistering context menu hooks

**Rationale:**
- SharpShell requires .NET Framework (not compatible with .NET 8)
- Isolates shell registration complexity from main application
- Proven, stable solution for Windows shell extensions
- Can be replaced later without affecting main application

### Hybrid Architecture Benefits
- ✅ 95% of codebase uses modern .NET 8
- ✅ All required NuGet packages fully supported
- ✅ Clean separation between shell integration and business logic
- ✅ Easy to maintain and test independently
- ✅ Future-proof: shell layer can be modernized without rewriting main app

### Development Environment
- **Primary IDE:** Visual Studio Code (daily development)
- **Secondary IDE:** Visual Studio 2022 (exploration and advanced debugging)
- **Project Files:** Maintain compatibility with both environments

---

## 2. Configuration Management ✅ DECIDED

### System Tray Configuration UI

**Tabbed Interface:**
- **Main Config Tab** - Shell hook management and feature toggles
- **API Config Tab** - API key environment variable configuration
- **ClipBoard Manipulations Tab** - Clipboard-related features
- *(Additional tabs as features are added)*

### Main Config Tab Layout

**Top Section - Shell Hook Management:**
Four buttons for managing shell integration:
- **Uninstall All Shell Hooks** - Remove all context menu items
- **Install Selected Shell Hooks** - Register enabled features to context menus
- **Uninstall Systray Start Up** - Remove from Windows startup
- **Install Systray Start Up** - Add to Windows startup

**Feature Toggle Section:**
Organized by file type with enable/disable sliders:
- **.MP4 Files**
  - Extract MP3
  - Extract WAV
  - Last Frame to JPG
  - First Frame to JPG
- **.MP3 Files**
  - Transcribe
- **Image File Types**
  - *(Future features)*

### Configuration Storage (JSON)

**Primary Config File:** `config.json`
```json
{
  "features": [
    { "id": "ExtractMp3", "enabled": true },
    { "id": "ExtractWav", "enabled": true },
    { "id": "LastFrameToJpg", "enabled": true },
    { "id": "FirstFrameToJpg", "enabled": true },
    { "id": "TranscribeMp3", "enabled": false }
  ],
  "apiKeys": {
    "openAI": "OPENAI_API_KEY"  // Environment variable name, not actual key
  },
  "settings": {
    "logLevel": "Info",
    "outputPath": "%USERPROFILE%\\RightClicks\\Output"
  }
}
```

**Configuration Flow:**
1. User toggles feature slider in UI
2. Main app (.NET 8) updates `config.json`
3. User clicks "Install Selected Shell Hooks"
4. Main app calls Shell Hook Manager (.NET 4.8)
5. Shell Hook Manager reads `config.json` and registers only enabled features

### API Key Management
- Actual API keys stored in **Windows environment variables**
- Config file stores **variable name reference only**
- Example: Config contains `"OPENAI_API_KEY"` → reads from env var of that name
- Users can customize variable names in API Config tab
- Keeps sensitive data out of config files

---

## 3. Shell Hook Architecture ✅ DECIDED

### Component Separation

**Shell Hook Manager (.NET Framework 4.8):**
- Separate standalone executable: `RightClicksShellManager.exe`
- Uses SharpShell for Windows shell extension registration
- Reads `config.json` to determine which features to register
- Registers context menu items per file type based on enabled features
- Called by main app when user clicks "Install Selected Shell Hooks"

**Main Application (.NET 8):**
- Contains all feature implementations
- Provides CLI interface for feature execution
- Shell hooks invoke main app via command line when user selects context menu item

### Feature Architecture Pattern

**Interface-Based Design:**
All features implement `IFileFeature` interface:

```csharp
public interface IFileFeature
{
    string Id { get; }                          // Unique identifier: "ExtractMp3"
    string DisplayName { get; }                 // UI display: "Extract MP3"
    string Description { get; }                 // Help text
    string[] SupportedExtensions { get; }       // File types: [".mp4", ".avi", ".mkv"]

    Task<FeatureResult> ExecuteAsync(string filePath, CancellationToken ct);
}
```

**Example Feature Implementation:**
```csharp
public class ExtractMp3Feature : IFileFeature
{
    public string Id => "ExtractMp3";
    public string DisplayName => "Extract MP3";
    public string Description => "Extract audio track as MP3 file";
    public string[] SupportedExtensions => new[] { ".mp4", ".avi", ".mkv", ".mov" };

    public async Task<FeatureResult> ExecuteAsync(string filePath, CancellationToken ct)
    {
        // Use FFMpegCore to extract audio
        // Return success/failure result
    }
}
```

### Feature Discovery & Registration

**Automatic Discovery via Reflection:**
- Features are C# classes compiled into main executable
- At startup, main app uses reflection to find all `IFileFeature` implementations
- No manual registration required - just create a new class and it's automatically discovered
- Similar pattern to TransformClipboard's `TextTransformerBase` approach

**Discovery Code Pattern:**
```csharp
var allFeatures = Assembly.GetExecutingAssembly()
    .GetTypes()
    .Where(t => typeof(IFileFeature).IsAssignableFrom(t) && !t.IsInterface)
    .Select(t => (IFileFeature)Activator.CreateInstance(t))
    .ToList();
```

### Context Menu Organization

**File Type → Available Actions:**
When user right-clicks a file, only relevant features appear:

**Right-click on `video.mp4`:**
```
→ RightClicks (submenu)
  → Extract MP3 (if enabled)
  → Extract WAV (if enabled)
  → Last Frame to JPG (if enabled)
  → First Frame to JPG (if enabled)
```

**Right-click on `audio.mp3`:**
```
→ RightClicks (submenu)
  → Transcribe (if enabled)
```

**Right-click on `image.jpg`:**
```
→ RightClicks (submenu)
  → (Future image features)
```

### Extension Strategy

**Easy to Add New Features:**
1. Create new class implementing `IFileFeature`
2. Specify supported file extensions
3. Implement `ExecuteAsync()` method
4. Feature is automatically discovered and available
5. Add UI toggle in Main Config tab
6. No additional registration code needed

**Rationale:**
- Inspired by successful TransformClipboard pattern
- Simple, maintainable, easy to extend
- Start with core features, grow organically over time
- Low friction for adding new capabilities

---

## 4. Feature Execution Model ✅ DECIDED

### Task Queue System

**Execution Flow:**
1. User right-clicks file and selects action from context menu
2. For interactive features (e.g., Time Stretch), dialog appears first for user input
3. Job is added to **task queue** managed by system tray application
4. If queue is empty → job starts immediately
5. If queue has jobs → job waits in line
6. **Default: 3 simultaneous jobs** can run concurrently (user configurable)

**Queue Requirements:**
- System tray application **must be running** for context menu actions to work
- Tray app manages all job execution and queuing
- Jobs are processed asynchronously using Task-based async/await pattern

### Queued Jobs Tab

**New Tab in Main Config Window:**
- **"Queued Jobs"** tab added to main configuration window
- **Becomes default tab** when jobs are in queue or running
- Shows all jobs with status: Pending, Running, Completed, Failed

**Job Information Display:**
- Source file path
- Action/feature name
- Status (Pending/Running/Completed/Failed)
- Start time
- Duration/elapsed time
- Output file path (when completed)
- Error message (when failed)

**User Controls:**
- **Cancel button** - Cancel running jobs
- **Remove button** - Remove pending jobs from queue before they start
- **Clear Completed button** - Manually clear completed/failed jobs
- **Max Simultaneous Jobs setting** - Numeric input with Save button (default: 3)

**Job History Retention:**
- Completed and failed jobs remain visible for **7 days**
- Jobs older than 7 days are automatically removed from history
- User can manually clear jobs at any time

### Notification System

**Windows Toast Notifications:**
- Use native Windows notification system (lower right, near system tray)
- Appear for job completion and failures
- Auto-dismiss after a few seconds (standard Windows behavior)

**Success Notification:**
```
✓ RightClicks - Job Complete
"vacation.mp3" created successfully
```

**Failure Notification:**
```
✗ RightClicks - Job Failed
Failed to extract audio from "vacation.mp4"
Click to view details
```

**Notification Triggers:**
- Job completes successfully
- Job fails with error
- Optional: Job starts (configurable in settings)

### Output File Handling

**File Location:**
- Output files are **always saved in the same folder** as the source file
- No user prompts for save location
- Consistent, predictable behavior

**Naming Patterns:**
Based on specifications in RightClicks.md:

| Action | Input | Output Pattern |
|--------|-------|----------------|
| Audio to MP3 | `vacation.mp4` | `vacation.mp3` |
| Audio to WAV | `vacation.mp4` | `vacation.wav` |
| First Frame to JPG | `vacation.mp4` | `vacation_First.jpg` |
| Last Frame to JPG | `vacation.mp4` | `vacation_Last.jpg` |
| Reverse Video | `vacation.mp4` | `vacation_Reverse.mp4` |
| Forward2Reverse | `vacation.mp4` | `vacation_Forward2Reverse.mp4` |
| Time Stretch | `vacation.mp4` | `vacation_Stretch.mp4` |

**File Conflict Resolution:**
- If output file already exists (e.g., `vacation.mp3`)
- **Auto-rename** using Windows convention: `vacation (1).mp3`
- Continue pattern if needed: `vacation (2).mp3`, `vacation (3).mp3`, etc.
- No user prompts - automatic resolution
- Original source file is **never modified**

### Error Handling Strategy

**Error Detection:**
- FFmpeg/processing errors caught and logged
- File system errors (permissions, disk space) caught
- Invalid input file errors detected

**Error Reporting:**
1. Job marked as "Failed" in Queued Jobs tab
2. Toast notification appears with error summary
3. Full error details visible in Queued Jobs tab
4. Error logged to application log file

**User Actions on Failure:**
- View error details in Queued Jobs tab
- Retry job (if retry button implemented)
- Remove failed job from history
- Check application logs for debugging

### Interactive Features

**Features Requiring User Input:**
Some features require user input before execution (e.g., Time Stretch dialog)

**Execution Flow for Interactive Features:**
1. User selects "Time Stretch" from context menu
2. Dialog appears **immediately** with:
   - Read-only display of original duration
   - Slider to set target duration
   - Numeric input for precise duration
   - OK and Cancel buttons
3. User configures settings and clicks OK
4. **Job is queued** with user's settings
5. Job executes when queue position is reached
6. If user clicks Cancel, no job is created

**Dialog Behavior:**
- Dialogs are modal and block until user responds
- Dialogs appear on top of other windows
- Settings are validated before job is queued
- Invalid settings show error message and prevent job creation

### Performance Considerations

**Concurrent Job Execution:**
- Default: 3 simultaneous jobs
- Prevents system resource exhaustion
- User can increase on powerful machines
- User can decrease on older/slower machines

**Resource Management:**
- Each job runs in separate Task
- CancellationToken support for clean cancellation
- Proper disposal of FFmpeg processes
- Memory-efficient streaming where possible

**Recommended Settings by System:**
- Low-end systems (4GB RAM, dual-core): 1-2 jobs
- Mid-range systems (8GB RAM, quad-core): 3 jobs (default)
- High-end systems (16GB+ RAM, 8+ cores): 4-6 jobs

---

## 5. Installation & Deployment ✅ DECIDED

### Distribution Method

**Primary Distribution:** Portable ZIP via GitHub Releases

**Package Contents:**
```
RightClicks-v1.0.0.zip
├── RightClicks.exe              (Main .NET 8 application)
├── RightClicksShellManager.exe  (.NET 4.8 shell hook manager)
├── install.bat                  (Installation script)
├── uninstall.bat                (Uninstallation script)
├── config.json                  (Default configuration - all features enabled)
├── README.md                    (Quick start guide)
└── bin\
    ├── ffmpeg.exe               (Bundled FFmpeg binary)
    └── ffprobe.exe              (Bundled FFprobe binary)
```

**Rationale:**
- No installer complexity - simple ZIP extraction
- Users can extract anywhere, `install.bat` handles the rest
- Easy to update - just extract new version and run `install.bat` again
- GitHub releases provide free hosting and version management
- Future: Can add MSI/MSIX installer once application stabilizes

### Installation Process

**Target Location:** `%LOCALAPPDATA%\RightClicks\`
- Example: `C:\Users\hughe\AppData\Local\RightClicks\`
- Per-user installation (no system-wide files)
- Ensures libraries are in known, predictable location
- No conflicts with other users on same machine

**install.bat Responsibilities:**

1. **Copy Files**
   - Copies all files from extraction location to `%LOCALAPPDATA%\RightClicks\`
   - Creates directory structure if it doesn't exist
   - Overwrites existing files (supports upgrades)

2. **Create Default Configuration**
   - Creates `config.json` with all features enabled by default
   - User can customize later via UI
   - Initial config provides immediate functionality

3. **Register Shell Hooks**
   - Executes: `RightClicksShellManager.exe /install`
   - Registers context menu items for enabled features
   - Requires admin elevation for registry writes to `HKEY_CLASSES_ROOT`
   - Assumes user has admin rights

4. **Create Startup Shortcut**
   - Creates shortcut in `shell:startup` folder
   - Ensures system tray app launches on Windows boot
   - User can disable via "Uninstall Systray Start Up" button in UI

**Example install.bat:**
```batch
@echo off
echo ========================================
echo  RightClicks Installation
echo ========================================
echo.

REM Copy files to installation directory
echo [1/4] Copying files to %LOCALAPPDATA%\RightClicks...
if not exist "%LOCALAPPDATA%\RightClicks" mkdir "%LOCALAPPDATA%\RightClicks"
if not exist "%LOCALAPPDATA%\RightClicks\bin" mkdir "%LOCALAPPDATA%\RightClicks\bin"
if not exist "%LOCALAPPDATA%\RightClicks\logs" mkdir "%LOCALAPPDATA%\RightClicks\logs"

xcopy /E /I /Y *.* "%LOCALAPPDATA%\RightClicks\" >nul
echo    Files copied successfully.
echo.

REM Change to installation directory
cd /d "%LOCALAPPDATA%\RightClicks"

REM Register shell hooks
echo [2/4] Registering shell hooks (requires admin)...
RightClicksShellManager.exe /install
if %ERRORLEVEL% NEQ 0 (
    echo    WARNING: Shell hook registration failed. Run as Administrator.
) else (
    echo    Shell hooks registered successfully.
)
echo.

REM Create startup shortcut
echo [3/4] Creating startup shortcut...
powershell -Command "$WshShell = New-Object -ComObject WScript.Shell; $Shortcut = $WshShell.CreateShortcut('%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup\RightClicks.lnk'); $Shortcut.TargetPath = '%LOCALAPPDATA%\RightClicks\RightClicks.exe'; $Shortcut.Save()"
echo    Startup shortcut created.
echo.

REM Launch application
echo [4/4] Launching RightClicks...
start "" "%LOCALAPPDATA%\RightClicks\RightClicks.exe"
echo.

echo ========================================
echo  Installation Complete!
echo ========================================
echo.
echo RightClicks is now running in your system tray.
echo Right-click any supported file to see available actions.
echo.
pause
```

### Uninstallation Process

**uninstall.bat Responsibilities:**

1. **Unregister Shell Hooks**
   - Executes: `RightClicksShellManager.exe /uninstall`
   - Removes all context menu items
   - Requires admin elevation

2. **Remove Startup Shortcut**
   - Deletes shortcut from `shell:startup` folder
   - Prevents auto-launch on boot

3. **Stop Running Application**
   - Terminates RightClicks.exe if running
   - Ensures clean uninstall

4. **Delete Files (Optional)**
   - Prompts user: "Delete all files including config and logs? (Y/N)"
   - If Yes: Deletes entire `%LOCALAPPDATA%\RightClicks\` folder
   - If No: Preserves config.json and logs for potential reinstall

**Example uninstall.bat:**
```batch
@echo off
echo ========================================
echo  RightClicks Uninstallation
echo ========================================
echo.

REM Change to installation directory
cd /d "%LOCALAPPDATA%\RightClicks"

REM Unregister shell hooks
echo [1/4] Unregistering shell hooks...
RightClicksShellManager.exe /uninstall
echo    Shell hooks removed.
echo.

REM Remove startup shortcut
echo [2/4] Removing startup shortcut...
del "%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup\RightClicks.lnk" >nul 2>&1
echo    Startup shortcut removed.
echo.

REM Stop application
echo [3/4] Stopping RightClicks application...
taskkill /F /IM RightClicks.exe >nul 2>&1
echo    Application stopped.
echo.

REM Ask about file deletion
echo [4/4] Delete all files?
set /p DELETE_FILES="Delete config, logs, and all files? (Y/N): "
if /i "%DELETE_FILES%"=="Y" (
    cd /d "%LOCALAPPDATA%"
    rmdir /S /Q "RightClicks"
    echo    All files deleted.
) else (
    echo    Files preserved in %LOCALAPPDATA%\RightClicks
)
echo.

echo ========================================
echo  Uninstallation Complete!
echo ========================================
pause
```

### Default Configuration

**Initial config.json created by install.bat:**
```json
{
  "version": "1.0.0",
  "features": [
    { "id": "ExtractMp3", "enabled": true },
    { "id": "ExtractWav", "enabled": true },
    { "id": "FirstFrameToJpg", "enabled": true },
    { "id": "LastFrameToJpg", "enabled": true },
    { "id": "ReverseVideo", "enabled": true },
    { "id": "Forward2Reverse", "enabled": true },
    { "id": "TimeStretch", "enabled": true },
    { "id": "TranscribeMp3", "enabled": true }
  ],
  "apiKeys": {
    "openAI": "OPENAI_API_KEY"
  },
  "settings": {
    "logLevel": "Info",
    "maxConcurrentJobs": 3,
    "jobHistoryDays": 7,
    "checkForUpdates": true
  }
}
```

**Note:** All features enabled by default. Users can disable unwanted features via Main Config tab. Default set will be refined based on user feedback during development.

### Update Mechanism

**GitHub-Based Version Checking:**

1. **Version Manifest File**
   - Host `version.json` on GitHub releases
   - Contains latest version number and download URL
   - Example: `https://github.com/hughesdo/RightClicks/releases/latest/version.json`

**version.json format:**
```json
{
  "version": "1.2.0",
  "releaseDate": "2025-11-15",
  "downloadUrl": "https://github.com/hughesdo/RightClicks/releases/download/v1.2.0/RightClicks-v1.2.0.zip",
  "releaseNotes": "https://github.com/hughesdo/RightClicks/releases/tag/v1.2.0",
  "minimumVersion": "1.0.0"
}
```

2. **Startup Version Check**
   - On application startup, check `version.json` via HttpClient
   - Compare current version with latest version
   - If newer version available, show notification

3. **Update Notification**
   - Toast notification: "RightClicks v1.2.0 available - Click to download"
   - Clicking opens GitHub releases page in browser
   - User manually downloads and runs `install.bat` to upgrade

4. **Settings Option**
   - "Check for Updates" toggle in settings (enabled by default)
   - "Check Now" button for manual version check
   - Display current version in About section

**Future Enhancement:**
- Consider integrating Squirrel.Windows for automatic delta updates
- Implement in-app download and install process
- Add release notes viewer in UI

### Prerequisites & Dependencies

**Self-Contained Deployment:**
- Use `dotnet publish` with `--self-contained` flag
- Bundles .NET 8 runtime with application
- No separate runtime installation required
- Larger file size (~80-100MB) but zero dependencies

**Publish Command:**
```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false
```

**Why `PublishSingleFile=false`:**
- Keeps ffmpeg.exe and ffprobe.exe as separate files
- Easier to update FFmpeg independently
- Better for debugging and troubleshooting
- Allows users to replace FFmpeg if needed

**FFmpeg Strategy:**
- **Package:** FFMpegCore (NuGet)
- **Binaries:** Bundle ffmpeg.exe and ffprobe.exe in `\bin` folder
- **Rationale:**
  - FFMpegCore provides robust C# API with full FFmpeg capabilities
  - Bundled binaries ensure availability and version consistency
  - No runtime downloads or external dependencies
  - Supports all current and future video/audio features
  - Maximum flexibility for feature development

**Folder Structure After Installation:**
```
%LOCALAPPDATA%\RightClicks\
├── RightClicks.exe
├── RightClicksShellManager.exe
├── config.json
├── install.bat
├── uninstall.bat
├── bin\
│   ├── ffmpeg.exe
│   └── ffprobe.exe
├── logs\
│   └── RightClicks.log
└── (various .NET runtime DLLs)
```

### Installation Requirements

**System Requirements:**
- Windows 10 version 1809 or later (or Windows 11)
- x64 processor
- 200MB free disk space
- Administrator rights (for shell hook registration)

**User Assumptions:**
- User has admin rights on their machine
- User can extract ZIP files
- User can run batch files
- User understands basic Windows file system navigation

---

## 6. Project Structure ✅ DECIDED

### Solution Organization

**Approach:** Hybrid - Single main project + separate shell manager project

**Rationale:**
- Simple to start with and navigate
- Easy to understand for future contributors
- Shell manager requires separate project (different .NET framework)
- Can split into multiple projects later if complexity grows
- Matches "start simple, evolve over time" philosophy

### Visual Studio Solution Structure

```
RightClicks.sln
├── RightClicks (main .NET 8 application)
│   └── Target: .NET 8, Windows, x64
│       Output: RightClicks.exe
│
└── RightClicksShellManager (.NET Framework 4.8)
    └── Target: .NET Framework 4.8, Windows, x64
        Output: RightClicksShellManager.exe
```

### Main Project Folder Structure

**RightClicks Project (.NET 8):**

```
RightClicks/
├── Features/                           # All feature implementations
│   ├── IFileFeature.cs                # Core feature interface
│   ├── FeatureResult.cs               # Result model for feature execution
│   ├── FeatureDiscovery.cs            # Reflection-based feature discovery
│   │
│   ├── Video/                         # Video processing features
│   │   ├── ExtractMp3Feature.cs       # Extract MP3 audio from video
│   │   ├── ExtractWavFeature.cs       # Extract WAV audio from video
│   │   ├── FirstFrameToJpgFeature.cs  # Capture first frame as JPG
│   │   ├── LastFrameToJpgFeature.cs   # Capture last frame as JPG
│   │   ├── ReverseVideoFeature.cs     # Reverse video playback
│   │   ├── Forward2ReverseFeature.cs  # Forward + reverse concatenation
│   │   └── TimeStretchFeature.cs      # Time stretch/compress video
│   │
│   ├── Audio/                         # Audio processing features
│   │   └── TranscribeMp3Feature.cs    # Transcribe MP3 to text
│   │
│   ├── Image/                         # Image processing features (future)
│   │   ├── JpgToPngFeature.cs
│   │   └── PngToJpgFeature.cs
│   │
│   └── Text/                          # Text processing features (future)
│       └── ClipboardToFileFeature.cs
│
├── UI/                                # User interface components
│   ├── MainWindow.xaml                # Main configuration window
│   ├── MainWindow.xaml.cs
│   │
│   ├── SystemTray/                    # System tray integration
│   │   ├── SystemTrayIcon.cs          # System tray icon and menu
│   │   └── TrayIconManager.cs
│   │
│   ├── Tabs/                          # Tab content for main window
│   │   ├── MainConfigTab.xaml         # Shell hooks & feature toggles
│   │   ├── ApiConfigTab.xaml          # API key configuration
│   │   ├── ClipboardTab.xaml          # Clipboard manipulations
│   │   └── QueuedJobsTab.xaml         # Job queue viewer
│   │
│   ├── Dialogs/                       # Modal dialogs
│   │   ├── TimeStretchDialog.xaml     # Time stretch configuration
│   │   └── AboutDialog.xaml           # About/version information
│   │
│   └── Controls/                      # Reusable custom controls
│       ├── FeatureToggleControl.xaml  # Feature enable/disable slider
│       └── JobListItemControl.xaml    # Job queue item display
│
├── Services/                          # Business logic services
│   ├── JobQueueService.cs             # Job queue management
│   ├── ConfigurationService.cs        # Config file read/write
│   ├── NotificationService.cs         # Windows toast notifications
│   ├── UpdateService.cs               # Version checking and updates
│   ├── ShellHookService.cs            # Interface to shell manager
│   └── LoggingService.cs              # Application logging
│
├── Models/                            # Data models
│   ├── Job.cs                         # Job queue item model
│   ├── JobStatus.cs                   # Enum: Pending/Running/Complete/Failed
│   ├── AppConfig.cs                   # Application configuration model
│   ├── FeatureConfig.cs               # Feature enable/disable state
│   └── VersionInfo.cs                 # Version manifest model
│
├── Utilities/                         # Helper classes
│   ├── FileHelper.cs                  # File naming, conflict resolution
│   ├── ProcessHelper.cs               # Process execution helpers
│   └── RegistryHelper.cs              # Registry access utilities
│
├── Resources/                         # Application resources
│   ├── Icons/                         # Application icons
│   │   ├── TrayIcon.ico
│   │   └── AppIcon.ico
│   ├── Images/                        # UI images
│   └── Styles/                        # WPF styles and themes
│       └── AppStyles.xaml
│
├── App.xaml                           # WPF application definition
├── App.xaml.cs                        # Application startup logic
├── Program.cs                         # Entry point (CLI support)
└── RightClicks.csproj                 # Project file
```

### Shell Manager Project Structure

**RightClicksShellManager Project (.NET Framework 4.8):**

```
RightClicksShellManager/
├── ShellExtensions/                   # SharpShell extensions
│   ├── RightClicksContextMenu.cs      # Main context menu handler
│   └── FileTypeHandlers/              # Per-file-type handlers
│       ├── Mp4ContextMenu.cs
│       ├── Mp3ContextMenu.cs
│       └── ImageContextMenu.cs
│
├── Models/                            # Shared models (minimal)
│   └── AppConfig.cs                   # Config model (duplicated from main)
│
├── Utilities/                         # Helper classes
│   ├── ConfigReader.cs                # Read config.json
│   └── RegistryManager.cs             # Registry operations
│
├── Program.cs                         # Entry point (CLI: /install, /uninstall)
└── RightClicksShellManager.csproj     # Project file
```

### Shared Code Strategy

**Configuration Model:**
- `AppConfig.cs` is duplicated in both projects
- Alternative: Create shared class library (adds complexity)
- Decision: Duplicate for simplicity, keep in sync manually
- Both projects read same `config.json` file

**Communication Between Projects:**
- Shell manager reads `config.json` to know which features to register
- Shell hooks invoke main app via command line: `RightClicks.exe --feature ExtractMp3 --file "C:\path\to\video.mp4"`
- Main app processes command and adds job to queue

### Naming Conventions

**Projects:**
- `RightClicks` - Main application
- `RightClicksShellManager` - Shell hook manager

**Namespaces:**
```csharp
RightClicks                          // Root namespace
RightClicks.Features                 // Feature implementations
RightClicks.Features.Video           // Video features
RightClicks.Features.Audio           // Audio features
RightClicks.UI                       // UI components
RightClicks.UI.Tabs                  // Tab content
RightClicks.UI.Dialogs               // Modal dialogs
RightClicks.Services                 // Business logic services
RightClicks.Models                   // Data models
RightClicks.Utilities                // Helper classes

RightClicksShellManager              // Shell manager root
RightClicksShellManager.ShellExtensions
RightClicksShellManager.Models
```

**File Naming:**
- Features: `{Action}{FileType}Feature.cs` (e.g., `ExtractMp3Feature.cs`)
- Services: `{Purpose}Service.cs` (e.g., `JobQueueService.cs`)
- Models: `{Entity}.cs` (e.g., `Job.cs`, `AppConfig.cs`)
- UI: `{Name}Window.xaml` or `{Name}Dialog.xaml` or `{Name}Tab.xaml`

**Class Naming:**
- Features implement `IFileFeature` interface
- Services end with `Service` suffix
- Dialogs end with `Dialog` suffix
- Tabs end with `Tab` suffix

### Project Dependencies

**RightClicks Project (.NET 8) - NuGet Packages:**
```xml
<ItemGroup>
  <!-- FFmpeg Integration -->
  <PackageReference Include="FFMpegCore" Version="5.1.0" />

  <!-- Audio Processing -->
  <PackageReference Include="NAudio" Version="2.2.1" />

  <!-- JSON Configuration -->
  <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />

  <!-- Logging -->
  <PackageReference Include="Serilog" Version="3.1.1" />
  <PackageReference Include="Serilog.Sinks.File" Version="5.0.0" />

  <!-- HTTP Client (for updates) -->
  <!-- Built-in System.Net.Http -->

  <!-- WPF (built-in for .NET 8 Windows projects) -->
</ItemGroup>
```

**RightClicksShellManager Project (.NET 4.8) - NuGet Packages:**
```xml
<ItemGroup>
  <!-- Shell Extension Framework -->
  <PackageReference Include="SharpShell" Version="2.7.2" />

  <!-- JSON Configuration -->
  <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
</ItemGroup>
```

### Build Configuration

**Solution Configurations:**
- Debug (x64)
- Release (x64)

**Output Paths:**
```
Debug:
  bin\Debug\net8.0-windows\RightClicks.exe
  bin\Debug\net48\RightClicksShellManager.exe

Release:
  bin\Release\net8.0-windows\win-x64\publish\RightClicks.exe
  bin\Release\net48\RightClicksShellManager.exe
```

**Post-Build Events:**
- Copy `ffmpeg.exe` and `ffprobe.exe` to `bin\` folder
- Copy default `config.json` to output directory
- Copy `install.bat` and `uninstall.bat` to output directory

### Development Workflow

**Adding a New Feature:**
1. Create new class in appropriate `Features/` subfolder
2. Implement `IFileFeature` interface
3. Feature is automatically discovered via reflection
4. Add UI toggle in `MainConfigTab.xaml`
5. Add feature ID to default `config.json`
6. Test via CLI: `RightClicks.exe --feature NewFeature --file "test.mp4"`
7. Test via context menu after running `install.bat`

**Modifying Configuration:**
1. Update `AppConfig.cs` in both projects (keep in sync)
2. Update default `config.json`
3. Handle migration for existing user configs (if needed)

**Adding a New UI Tab:**
1. Create new XAML file in `UI/Tabs/`
2. Add tab to `MainWindow.xaml`
3. Wire up tab switching logic in `MainWindow.xaml.cs`

### Future Scalability

**When to Split into More Projects:**
- If feature count exceeds 50+ features → consider `RightClicks.Features` project
- If UI becomes complex → consider `RightClicks.UI` project
- If shared models grow → consider `RightClicks.Core` project
- Current structure supports 20-30 features comfortably

**Plugin Architecture (Future):**
- If external developers want to contribute features
- Create `RightClicks.PluginSDK` project with `IFileFeature` interface
- Load external DLLs from `%LOCALAPPDATA%\RightClicks\Plugins\`
- Not needed for initial release

---

## 7. Logging & Diagnostics ✅ DECIDED

### Logging Framework

**Selected Framework:** Serilog

**Rationale:**
- Structured logging with rich context
- Easy configuration and setup
- Multiple sinks (file, console)
- Excellent for debugging and troubleshooting
- Industry standard for .NET applications

**NuGet Packages:**
```xml
<PackageReference Include="Serilog" Version="3.1.1" />
<PackageReference Include="Serilog.Sinks.File" Version="5.0.0" />
<PackageReference Include="Serilog.Sinks.Console" Version="5.0.1" />
```

### Logging Strategy

**Primary Purpose:** Development and debugging
- **Primary user:** AI assistant (for testing and debugging features)
- **Secondary user:** Developer (for occasional troubleshooting)
- **NOT for end users:** Users rely on notifications and queue status

**Logging Level:** Always verbose
- Log everything during development and production
- Detailed execution traces for all operations
- Full context for debugging issues

**No UI Log Viewer:**
- No "Logs" tab in main configuration window
- Logs are for development/debugging only
- End users don't need to see logs

### Log File Location

**Storage Path:** `%LOCALAPPDATA%\RightClicks\logs\`
- Example: `C:\Users\hughe\AppData\Local\RightClicks\logs\`
- Keeps logs separate from application binaries
- Easy to locate for troubleshooting

**File Naming Convention:**

**Production Logs:**
```
RightClicks-20251111.log    (daily rolling log)
RightClicks-20251112.log
RightClicks-20251113.log
```

**Test Mode Logs:**
```
RightClicks-TEST-20251111-143022.log    (isolated test run)
RightClicks-TEST-20251111-143145.log    (another test run)
RightClicks-TEST-20251111-150330.log    (another test run)
```

### Log Retention

**Configuration in config.json:**
```json
{
  "settings": {
    "logLevel": "Verbose",
    "logRetentionDays": 7,
    "maxConcurrentJobs": 3,
    "jobHistoryDays": 7,
    "checkForUpdates": true
  }
}
```

**Default Retention:** 7 days rolling
- Logs older than 7 days are automatically deleted
- Prevents disk space issues
- User can manually edit `logRetentionDays` in config.json
- Serilog handles automatic cleanup

### Command Line Interface for Testing

**Purpose:** Enable AI assistant and developer to test features without UI

**CLI Syntax:**
```bash
RightClicks.exe --feature <FeatureId> --file <FilePath> [options]
```

**Required Arguments:**
- `--feature <FeatureId>` - Feature to execute (e.g., ExtractMp3, ReverseVideo)
- `--file <FilePath>` - Full path to input file

**Optional Flags:**
- `--test-mode` - Create isolated log file for this test run
- `--clear-logs` - Delete all log files before execution
- `--clear-logs --test-only` - Delete only test logs, keep main log

**Examples:**

**Normal Execution:**
```bash
RightClicks.exe --feature ExtractMp3 --file "C:\Videos\test.mp4"
```
- Executes feature
- Logs to: `RightClicks-20251111.log` (main log)
- Adds job to queue (if tray app running)
- Or executes immediately (if CLI-only mode)

**Test Mode (Isolated Log):**
```bash
RightClicks.exe --feature ExtractMp3 --file "C:\Videos\test.mp4" --test-mode
```
- Executes feature
- Logs to: `RightClicks-TEST-20251111-143022.log` (isolated log with timestamp)
- Main log remains untouched
- Easy to review specific test execution

**Clear Logs Before Test:**
```bash
RightClicks.exe --feature ExtractMp3 --file "C:\Videos\test.mp4" --clear-logs
```
- Deletes all existing log files
- Starts fresh log for this execution
- Useful for clean slate testing

**Clear Only Test Logs:**
```bash
RightClicks.exe --clear-logs --test-only
```
- Deletes only `*-TEST-*.log` files
- Preserves main production log
- Quick cleanup after testing session

**Clear All Logs:**
```bash
RightClicks.exe --clear-logs
```
- Deletes all log files in `logs\` folder
- Complete reset

### Development Workflow

**Adding and Testing a New Feature:**

1. **Developer provides feature description**
   - Example: "Add reverse video feature for .mp4 files"

2. **AI assistant implements feature**
   - Create `ReverseVideoFeature.cs` in `Features/Video/`
   - Implement `IFileFeature` interface
   - Add to UI toggles in `MainConfigTab.xaml`
   - Add to default `config.json`
   - Update shell hook registration logic

3. **AI assistant tests via CLI**
   ```bash
   RightClicks.exe --feature ReverseVideo --file "test.mp4" --test-mode
   ```

4. **AI assistant examines log**
   - Open `RightClicks-TEST-20251111-143022.log`
   - Verify feature execution
   - Check for errors or warnings
   - Confirm output file created

5. **AI assistant tests via context menu**
   - Right-click `test.mp4` in Windows Explorer
   - Select "RightClicks → Reverse Video"
   - Verify job appears in queue
   - Check notification on completion

6. **AI assistant reports results**
   - Feature working: ✅
   - Output file created: ✅
   - Logs clean: ✅
   - Ready for next feature

7. **Clean up test logs**
   ```bash
   RightClicks.exe --clear-logs --test-only
   ```

### Logging Configuration

**Serilog Configuration (in Program.cs or App.xaml.cs):**

```csharp
using Serilog;
using Serilog.Events;

public static void ConfigureLogging(bool isTestMode = false)
{
    var config = ConfigurationService.LoadConfig();
    var logPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "RightClicks",
        "logs"
    );

    Directory.CreateDirectory(logPath);

    string logFileName = isTestMode
        ? $"RightClicks-TEST-{DateTime.Now:yyyyMMdd-HHmmss}.log"
        : $"RightClicks-{DateTime.Now:yyyyMMdd}.log";

    var logFilePath = Path.Combine(logPath, logFileName);

    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Verbose()
        .WriteTo.Console()
        .WriteTo.File(
            logFilePath,
            rollingInterval: isTestMode ? RollingInterval.Infinite : RollingInterval.Day,
            retainedFileCountLimit: config.Settings.LogRetentionDays,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
        )
        .CreateLogger();

    Log.Information("=== RightClicks Started ===");
    Log.Information("Version: {Version}", Assembly.GetExecutingAssembly().GetName().Version);
    Log.Information("Test Mode: {TestMode}", isTestMode);
    Log.Information("Log File: {LogFile}", logFilePath);
}
```

### What Gets Logged

**Application Lifecycle:**
- Application startup
- Configuration loaded
- Features discovered (count and names)
- System tray initialized
- Shell hooks status checked

**Feature Execution:**
- Feature invoked (name, file path)
- Job added to queue (job ID, position)
- Job started (job ID, timestamp)
- FFmpeg command executed (full command line)
- FFmpeg output (stdout and stderr)
- Output file created (path, size)
- Job completed (duration, status)
- Job failed (error message, stack trace)

**Configuration Changes:**
- Feature enabled/disabled
- Settings changed
- Config file saved

**Shell Hook Operations:**
- Shell hooks installed
- Shell hooks uninstalled
- Registry keys written
- Startup shortcut created/removed

**Errors and Exceptions:**
- All exceptions with full stack traces
- File system errors (permissions, disk space)
- FFmpeg errors
- Configuration errors
- Network errors (update checks)

**Example Log Output:**
```
2025-11-11 14:30:22.123 [INF] === RightClicks Started ===
2025-11-11 14:30:22.125 [INF] Version: 1.0.0.0
2025-11-11 14:30:22.126 [INF] Test Mode: True
2025-11-11 14:30:22.127 [INF] Log File: C:\Users\hughe\AppData\Local\RightClicks\logs\RightClicks-TEST-20251111-143022.log
2025-11-11 14:30:22.150 [INF] Configuration loaded from: C:\Users\hughe\AppData\Local\RightClicks\config.json
2025-11-11 14:30:22.175 [INF] Discovered 8 features via reflection
2025-11-11 14:30:22.176 [INF] Feature: ExtractMp3Feature (Enabled)
2025-11-11 14:30:22.177 [INF] Feature: ExtractWavFeature (Enabled)
2025-11-11 14:30:22.178 [INF] Feature: FirstFrameToJpgFeature (Enabled)
2025-11-11 14:30:22.179 [INF] Feature: LastFrameToJpgFeature (Enabled)
2025-11-11 14:30:22.180 [INF] Feature: ReverseVideoFeature (Enabled)
2025-11-11 14:30:22.181 [INF] Feature: Forward2ReverseFeature (Enabled)
2025-11-11 14:30:22.182 [INF] Feature: TimeStretchFeature (Enabled)
2025-11-11 14:30:22.183 [INF] Feature: TranscribeMp3Feature (Enabled)
2025-11-11 14:30:22.200 [INF] CLI Mode: Feature=ExtractMp3, File=C:\Videos\test.mp4
2025-11-11 14:30:22.250 [INF] Job created: ID=job-12345, Feature=ExtractMp3, File=test.mp4
2025-11-11 14:30:22.251 [INF] Job started: ID=job-12345
2025-11-11 14:30:22.300 [INF] FFmpeg command: ffmpeg -i "C:\Videos\test.mp4" -vn -acodec libmp3lame -q:a 2 "C:\Videos\test.mp3"
2025-11-11 14:30:22.350 [DBG] FFmpeg stdout: ffmpeg version 6.0 Copyright (c) 2000-2023 the FFmpeg developers
2025-11-11 14:30:23.500 [DBG] FFmpeg stdout: Stream #0:0: Video: h264, yuv420p, 1920x1080, 30 fps
2025-11-11 14:30:23.501 [DBG] FFmpeg stdout: Stream #0:1: Audio: aac, 48000 Hz, stereo
2025-11-11 14:30:25.750 [DBG] FFmpeg progress: frame=150 fps=60 time=00:00:05.00
2025-11-11 14:30:28.123 [INF] Output file created: C:\Videos\test.mp3 (3.2 MB)
2025-11-11 14:30:28.124 [INF] Job completed: ID=job-12345, Duration=5.87s, Status=Success
2025-11-11 14:30:28.125 [INF] Notification sent: Job Complete - test.mp3 created successfully
2025-11-11 14:30:28.150 [INF] === RightClicks Exiting ===
```

### Diagnostic Utilities

**Log Analysis Helper (Future):**
- Create `LogAnalyzer.cs` utility class
- Parse logs for common error patterns
- Generate summary reports
- Useful for batch testing multiple features

**Performance Metrics:**
- Log execution time for each feature
- Track FFmpeg performance
- Identify slow operations
- Optimize based on metrics

### Error Handling and Logging

**Exception Logging Pattern:**
```csharp
try
{
    Log.Information("Starting feature: {Feature} on file: {File}", featureId, filePath);
    var result = await feature.ExecuteAsync(filePath, cancellationToken);

    if (result.Success)
    {
        Log.Information("Feature completed successfully: {Feature}, Output: {Output}",
            featureId, result.OutputPath);
    }
    else
    {
        Log.Error("Feature failed: {Feature}, Error: {Error}",
            featureId, result.ErrorMessage);
    }
}
catch (Exception ex)
{
    Log.Error(ex, "Unhandled exception in feature: {Feature}, File: {File}",
        featureId, filePath);
    throw;
}
```

**Structured Logging Benefits:**
- Easy to search logs for specific features
- Easy to filter by file path
- Easy to identify error patterns
- Machine-readable for automated analysis

---

## Application Vision Summary

### System Tray Application
- Primary interface via system tray icon
- Clicking icon opens main configuration UI
- Tabbed interface for different configuration areas
- Main tab: Shell hook management and refresh

### Shell Hook Management
- Each feature (e.g., "MP4 to LastFrame") has enable/disable toggle
- Disabling a feature removes its shell integration
- One-button refresh to apply changes
- Users can clear all hooks and reconfigure at any time

### Command-Line Interface
- Callable via CLI parameters
- Enables testing and debugging of individual features
- Independent of tray UI entry point

### API Key Management Strategy
- API keys stored in **environment variables** (never in config files)
- Config file (JSON) stores **variable name reference** only
- Example: Config contains `"ChatGPT_APIKey"` → reads from env var of that name
- Users can rename the reference (e.g., to `"ChatGPT_RightClick_API"`)
- Decouples sensitive data from configuration
- User-independent and customizable

### Planned NuGet Dependencies
- **NAudio** - Audio processing
- **FFMpegCore** - Video/audio manipulation
- **Xabe.FFmpeg** - FFmpeg wrapper
- **FFMediaToolkit** - Media file handling
- Additional packages as features are developed

---

## Next Steps

1. ✅ ~~Finalize Technology Stack & Framework~~
2. ✅ ~~Define Configuration Management approach~~
3. ✅ ~~Complete Shell Hook Architecture design~~
4. ✅ ~~Determine Feature Execution Model~~
5. ✅ ~~Plan Installation & Deployment strategy~~
6. ✅ ~~Design Project Structure~~
7. ✅ ~~Select Logging & Diagnostics framework~~

---

## 🎉 Architectural Planning Complete!

All 7 sections have been decided and documented. The architecture is ready for implementation.

**Ready to begin development when you are!**

---

## Notes from OldProjects

The following projects provide lessons and code to consolidate:
- **RightClickApps** - C# Windows Forms with SharpShell context menu extensions
- **TransformClipboard** - WPF app for clipboard transformation
- **DynamicSubMenus** - Shell extension for dynamic context menus

Key takeaways:
- SharpShell integration patterns
- Context menu registration approaches
- Configuration management patterns
- Common pitfalls to avoid

---

**Document Status:** Living document - will be updated as architectural decisions are made.

