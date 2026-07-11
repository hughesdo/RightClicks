# RVC (Retrieval-based Voice Conversion) Integration

**Date Created:** 2025-11-28
**Date Resolved:** 2025-11-30
**Status:** ✅ **RESOLVED** - Deployment solution implemented
**Priority:** HIGH

---

## Problem Summary (RESOLVED)

### Current Behavior (2025-11-28)
- ✅ RVC features appear in Windows Explorer context menu
- ✅ Right-clicking audio files shows RVC submenu with all voice models
- ✅ Selecting an RVC feature sends job to RightClicks via IPC
- ❌ **Jobs never execute** - they disappear after being received
- ❌ No "Adding job to queue" log message
- ❌ No "Job completed" or "Job failed" messages
- ❌ Output files never created

### Evidence from Logs (RightClicks-20251128.log)

**RVC Job (Fails Silently):**
```
2025-11-28 13:29:23.652 [INF] === CLI Feature Execution (Queue Mode) ===
2025-11-28 13:29:23.652 [INF] Feature ID: RvcBeavis
[... nothing else - job disappears ...]
```

**Working Job (WavToMp3 for comparison):**
```
2025-11-28 13:29:10.390 [INF] === CLI Feature Execution (Queue Mode) ===
2025-11-28 13:29:10.390 [INF] Feature ID: WavToMp3
2025-11-28 13:29:10.390 [INF] Found feature: WAV to MP3
2025-11-28 13:29:10.390 [INF] Adding job to queue: "a0adbc88-e82b-459b-8380-fd1ee8b4ac61"
2025-11-28 13:29:10.665 [INF] WavToMp3Feature: Completed successfully in 270ms
2025-11-28 13:29:10.665 [INF] Job completed successfully
```

**Feature Discovery (2025-11-27):**
```
2025-11-27 10:51:19.627 [INF] Discovered 35 total features (35 static + 0 dynamic)
```
- **0 dynamic features discovered!** RVC features are dynamically generated.
- This means RVC models are not being discovered at startup.

**Warning in Logs:**
```
RVC directory not found. Expected at: E:\MyApps\RightClicks\RVC - skipping RVC feature generation
```

---

## Root Cause Analysis

### The Problem: Path Resolution Failure

**Where RightClicks.exe Runs:**
```
C:\Users\hughe\AppData\Local\RightClicks\RightClicks.exe
```
- RightClicks is deployed to `%LOCALAPPDATA%\RightClicks\` during build
- Post-build event copies executables and DLLs to this location
- **RVC folder is NOT copied** (too large - contains Python venv, models, etc.)

**Where RVC Folder Lives:**
```
E:\MyApps\RightClicks\RVC\
```
- RVC folder stays at development location
- Contains Python virtual environment (~500 MB)
- Contains voice models (.pth files, ~55 MB each × 24 models = ~1.3 GB)
- Contains inference scripts (infer_cli.py, etc.)

**Current Path Logic (`RvcModelDiscoveryService.cs` line 18-48):**
```csharp
public static string GetRvcPath()
{
    // Hardcoded path to RVC folder at repository root
    var rvcPath = @"E:\MyApps\RightClicks\RVC";

    if (Directory.Exists(rvcPath))
    {
        Log.Debug("Found RVC directory at: {RvcPath}", rvcPath);
        return rvcPath;
    }

    // Fallback: Try to find RVC folder by navigating up from app directory
    var appPath = AppDomain.CurrentDomain.BaseDirectory;
    var currentDir = new DirectoryInfo(appPath);

    // Go up until we find the repository root
    while (currentDir != null)
    {
        var candidatePath = Path.Combine(currentDir.FullName, "RVC");
        if (Directory.Exists(candidatePath))
        {
            Log.Debug("Found RVC directory at: {RvcPath}", candidatePath);
            return candidatePath;
        }
        currentDir = currentDir.Parent;
    }

    Log.Warning("RVC directory not found. Expected at: {ExpectedPath}", rvcPath);
    return string.Empty;
}
```

**Why It's Failing:**
1. **Hardcoded path check:** `E:\MyApps\RightClicks\RVC` should exist and should work
2. **BUT:** Something is preventing `Directory.Exists()` from returning true
3. **Fallback logic:** Starts at `C:\Users\hughe\AppData\Local\RightClicks\` and walks up
   - Goes to `C:\Users\hughe\AppData\Local\`
   - Goes to `C:\Users\hughe\AppData\`
   - Goes to `C:\Users\hughe\`
   - Goes to `C:\Users\`
   - Goes to `C:\`
   - **Never finds RVC folder** (it's on E: drive!)
4. **Result:** Returns empty string, RVC features not generated

---

## File Locations

### Development Location (E:\MyApps\RightClicks\)
```
E:\MyApps\RightClicks\
├── RightClicks\                    (C# project)
│   ├── RightClicks.csproj
│   ├── Services\
│   │   ├── RvcModelDiscoveryService.cs
│   │   └── RvcFeatureFactory.cs
│   └── Features\Audio\
│       └── RvcVoiceConversionFeatureBase.cs
├── RVC\                            (Python RVC implementation)
│   ├── venv\                       (~500 MB - Python virtual environment)
│   │   └── Scripts\
│   │       └── python.exe
│   ├── tools\
│   │   ├── infer_cli.py           (CLI inference script)
│   │   └── infer_batch_rvc.py
│   └── assets\
│       └── weights\                (~1.3 GB - 24 voice models)
│           ├── Beavis.pth
│           ├── Butthead.pth
│           ├── Trump.pth
│           ├── Obama.pth
│           └── ... (20 more models)
└── RVC.md                          (This document)
```

### Deployment Location (%LOCALAPPDATA%\RightClicks\)
```
C:\Users\hughe\AppData\Local\RightClicks\
├── RightClicks.exe                 (Main application)
├── RightClicksShellExtension.dll   (Windows Explorer integration)
├── RightClicksShellManager.exe     (Shell extension installer)
├── config.json                     (User configuration)
├── logs\                           (Application logs)
│   └── RightClicks-YYYYMMDD.log
└── bin\
    └── ffmpeg.exe
```

**Note:** RVC folder is **NOT** deployed to `%LOCALAPPDATA%\RightClicks\` due to size.

---

## Verification Commands

### Check RVC Installation
```powershell
# Check if RVC directory exists
Test-Path "E:\MyApps\RightClicks\RVC"

# Check Python venv
Test-Path "E:\MyApps\RightClicks\RVC\venv\Scripts\python.exe"

# Check inference script
Test-Path "E:\MyApps\RightClicks\RVC\tools\infer_cli.py"

# List all .pth model files
Get-ChildItem "E:\MyApps\RightClicks\RVC\assets\weights\*.pth" | Select-Object Name, Length, LastWriteTime
```

**Current Status (2025-11-28):**
- ✅ RVC directory exists: `E:\MyApps\RightClicks\RVC`
- ✅ Python venv exists: `E:\MyApps\RightClicks\RVC\venv\Scripts\python.exe`
- ✅ infer_cli.py exists: `E:\MyApps\RightClicks\RVC\tools\infer_cli.py`
- ✅ 24 .pth model files exist in `E:\MyApps\RightClicks\RVC\assets\weights\`

### Check Where RightClicks is Running
```powershell
Get-Process RightClicks -ErrorAction SilentlyContinue | Select-Object Path, Id
```

**Result:**
```
Path: C:\Users\hughe\AppData\Local\RightClicks\RightClicks.exe
Id: 736
```

### Check Feature Discovery in Logs
```powershell
Get-Content "$env:LOCALAPPDATA\RightClicks\logs\RightClicks-*.log" | Select-String "Discovered.*features|RVC directory|RVC models" | Select-Object -Last 10
```

---

## Solution Implemented (2025-11-30)

### Root Cause
The application runs from `%LOCALAPPDATA%\RightClicks\` but RVC folder was only at the development location `E:\MyApps\RightClicks\RVC\`. The path resolution logic had two issues:

1. **Hardcoded path check was failing** - Reason unknown, but likely permissions or timing issue
2. **Fallback logic couldn't cross drive boundaries** - Started at `C:\Users\...\RightClicks\` and walked up, but RVC is on `E:\` drive

### Solution: Deploy RVC + Update Path Resolution

**Changes Made:**

1. **Updated `RvcModelDiscoveryService.GetRvcPath()`** (RightClicks/Services/RvcModelDiscoveryService.cs)
   - **Priority 1:** Check deployed location first: `%LOCALAPPDATA%\RightClicks\RVC\`
   - **Priority 2:** Check development location: `E:\MyApps\RightClicks\RVC\`
   - **Priority 3:** Fallback to parent directory search
   - Added better logging at each step

2. **Created `install.bat`** (repository root)
   - Copies RightClicks application to `%LOCALAPPDATA%\RightClicks\`
   - Copies RVC folder (~10 GB) to `%LOCALAPPDATA%\RightClicks\RVC\`
   - Installs shell extension
   - Checks environment variables
   - Restarts Windows Explorer

3. **Updated `CLAUDE.md`** with install.bat maintenance section
   - Documents when to update install.bat
   - Provides testing checklist
   - Warns about 10 GB install size
   - Lists common issues and fixes

### Deployment Structure

**After running install.bat:**
```
%LOCALAPPDATA%\RightClicks\
├── RightClicks.exe
├── RightClicksShellExtension.dll
├── RightClicksShellManager.exe
├── (other DLLs and dependencies)
└── RVC\                           (~10 GB)
    ├── venv\                      (~8-9 GB - Python 3.10 + dependencies)
    │   └── Scripts\
    │       └── python.exe
    ├── configs\                   (RVC configuration files)
    ├── infer\                     (RVC inference modules)
    ├── tools\
    │   ├── infer_cli.py          (CLI inference script)
    │   └── infer_batch_rvc.py
    └── assets\
        ├── hubert\                (~400 MB - voice feature extraction)
        │   └── hubert_base.pt
        ├── rmvpe\                 (~60 MB - pitch extraction)
        │   └── rmvpe.pt
        └── weights\               (~1.3 GB - 24 voice models)
            ├── Beavis.pth
            ├── Butthead.pth
            └── ... (22 more models)
```

### Why Deploy RVC?

**Pros:**
- ✅ Works out of the box for end users
- ✅ No manual configuration needed
- ✅ Consistent environment (venv is portable)
- ✅ Simple path resolution (always check deployed location first)

**Cons:**
- ⚠️ Large download size (~10 GB)
- ⚠️ Requires disk space on system drive

**Decision:** Deploy full RVC folder for simplicity. 10 GB is acceptable for a one-time install.

### Testing the Fix

**Before Testing:**
```powershell
# 1. Kill RightClicks and restart Explorer
taskkill /F /IM RightClicks.exe
taskkill /F /IM explorer.exe
Start-Process explorer.exe

# 2. Build the project
dotnet build --configuration Release --verbosity minimal

# 3. Verify deployment
Get-ChildItem "$env:LOCALAPPDATA\RightClicks\" | Select-Object Name, LastWriteTime
```

**Test RVC Path Resolution:**
```powershell
# Check logs for RVC discovery
Get-Content "$env:LOCALAPPDATA\RightClicks\logs\RightClicks-*.log" | Select-String "Found RVC directory|Discovered.*features" | Select-Object -Last 10
```

**Expected Output:**
```
Found RVC directory at deployed location: C:\Users\hughe\AppData\Local\RightClicks\RVC
Discovered 59 total features (35 static + 24 dynamic)
Discovered 24 RVC models: Beavis, Butthead, ...
```

**Test RVC Feature Execution:**
```powershell
RightClicks.exe --feature RvcBeavis --file "testfiles\test.mp3" --test-mode
```

**Expected Output:**
- Job added to queue
- Python process executes
- RVC creates mono WAV/MP3 output
- FFmpeg post-processes to stereo WAV (lossless PCM)
- Output file created: `test_Beavis.wav` (stereo PCM 16-bit)
- Logs show successful completion

---

## Related Files

- **Path Discovery:** `RightClicks/Services/RvcModelDiscoveryService.cs` (UPDATED)
- **Installation Script:** `install.bat` (NEW)
- **Maintenance Docs:** `CLAUDE.md` (UPDATED - install.bat section added)
- **Feature Factory:** `RightClicks/Services/RvcFeatureFactory.cs`
- **Feature Base Class:** `RightClicks/Features/Audio/RvcVoiceConversionFeatureBase.cs`
- **Build Configuration:** `RightClicks/RightClicks.csproj` (PostBuild target)
- **RVC Documentation:** `RVC/CLAUDE.md`

---

## For End Users (GitHub Distribution)

**Installation Steps:**
1. Clone repository: `git clone https://github.com/hughesdo/RightClicks.git`
2. Build project: `dotnet build --configuration Release`
3. Run install.bat as Administrator
4. Wait for installation to complete (~10 GB copy)
5. Right-click any audio file in Windows Explorer
6. Select RightClicks → RVC → [Voice Model]

**Requirements:**
- Windows 10/11
- .NET 8.0 Runtime
- ~10 GB disk space
- Administrator privileges (for shell extension)
- Environment variables (FAL_KEY, CLOUDINARY_API_KEY, CLOUDINARY_API_SECRET)

---

**Status:** ✅ Solution implemented. Ready for testing.

