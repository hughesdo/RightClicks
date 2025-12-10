# Instructions for Claude - RightClicks Project

## Project Overview
You are helping develop **RightClicks**, a Windows context menu extension system that adds file-type-specific actions to Windows Explorer right-click menus.

**Key Documents:**
- `ARCHITECTURE.md` - All architectural decisions (READ THIS FIRST each session)
- `RightClicks.md` - Feature specifications and exact behaviors
- `TASKS.md` - Development task list and progress tracking
- `Systray Main.png` - UI mockup for system tray configuration window

## Your Primary Role

You are the **primary developer and tester** for this project. The user (Don) works on this part-time and relies on you to:

1. **Implement features** based on architectural decisions
2. **Test thoroughly** via CLI before handing off to Don
3. **Examine logs** after every test to verify correctness
4. **Report results clearly** so Don can do final acceptance testing
5. **Update TASKS.md** only after Don personally tests and approves

## Development Philosophy

**"Everything On by Default"**

During development and testing:
- ✅ **All features enabled** - Validates nothing breaks
- ✅ **All APIs configured** - Catches missing environment variables early
- ✅ **Full stack testing** - Ensures features play nicely together
- ✅ **Fresh config validation** - Prevents stale settings from causing issues

End users can disable features later via UI. Our job is to validate the **complete system**.

**Why This Matters:**
- Catches integration issues early (e.g., Cloudinary config missing from default config)
- Validates all code paths (e.g., all lip sync models tested together)
- Ensures config.json stays in sync with code (auto-discovery + auto-enable)
- Prevents "works on my machine" problems (environment variables validated)
- Avoids stale DLLs and config files (explicit cleanup steps)

## ⚠️ CRITICAL: .NET Version Compatibility

### The Hybrid Architecture Problem

RightClicks uses THREE projects with DIFFERENT .NET versions:

| Project | Target Framework | Purpose | Why This Version |
|---------|-----------------|---------|------------------|
| **RightClicks** | .NET 8.0 | Main WPF app, features, job queue | Modern framework, best NuGet support |
| **RightClicksShellExtension** | .NET Framework 4.8 | Context menu DLL loaded by Explorer | **Windows Explorer can ONLY load .NET Framework** |
| **RightClicksShellInstaller** | .NET 8.0 | CLI tool for shell registration | Uses SharpShell's ServerRegistrationManager |

### Why Shell Extension MUST Be .NET Framework 4.8

- `RightClicksShellExtension.dll` is a **COM server** loaded directly into `explorer.exe`
- Windows Explorer is a **native 64-bit process** that can only host **.NET Framework** assemblies
- **SharpShell** (our shell extension library) only works with .NET Framework 4.x
- This is a Windows limitation that cannot be changed - .NET 8 DLLs cannot run inside Explorer

### The Newtonsoft.Json DLL Conflict (LESSON LEARNED 2025-12-10)

**The Problem:**
- Both RightClicks (.NET 8) and RightClicksShellExtension (.NET 4.8) need `Newtonsoft.Json`
- .NET 8's Newtonsoft.Json references `System.Runtime, Version=6.0.0.0`
- .NET Framework 4.8 does NOT have `System.Runtime` - it causes assembly load failure
- If the wrong version is in `%LOCALAPPDATA%\RightClicks\`, the shell extension breaks silently

**The Symptom:**
- Right-clicking files shows only "Open RightClicks..." instead of cascading feature menus
- `ShellExtension-Debug.log` shows: `Could not load file or assembly 'System.Runtime, Version=6.0.0.0'`

**The Fix:**
- Build copies must ensure the **.NET Framework-compatible Newtonsoft.Json.dll** ends up in the install folder
- The shell extension's DLL must be copied AFTER the main app's files (to overwrite the .NET 8 version)
- See `RightClicks.csproj` post-build target - the order of Copy commands matters!

### Build Order Requirements

The `RightClicks.csproj` post-build event MUST:
1. First copy all main app files (including .NET 8 Newtonsoft.Json)
2. THEN copy shell extension files (overwriting with .NET Framework Newtonsoft.Json)
3. Use `SkipUnchangedFiles="false"` for Newtonsoft.Json to force overwrite

**NEVER change the copy order without understanding this constraint!**

### Quick Diagnostic

If context menus stop working, check the debug log:
```powershell
Get-Content "$env:LOCALAPPDATA\RightClicks\logs\ShellExtension-Debug.log" | Select-Object -Last 20
```

If you see `System.Runtime` errors, the wrong Newtonsoft.Json.dll was copied:
```powershell
# Fix: Copy the correct DLL from shell extension
Copy-Item "RightClicksShellExtension\bin\Release\Newtonsoft.Json.dll" "$env:LOCALAPPDATA\RightClicks\" -Force
taskkill /F /IM explorer.exe; Start-Sleep 2; Start-Process explorer.exe
```

## Development Workflow (CRITICAL)

### For Every Task:

1. **Implement Everywhere:**
   - Write the feature code (e.g., `ExtractMp3Feature.cs`)
   - ✅ **Feature will be auto-discovered** - No manual registration needed!
   - ✅ **Feature will be auto-enabled** - `CreateDefaultConfig()` enables all features
   - ⚠️ **If adding new API:** Add to `ApiKeys` dictionary in `CreateDefaultConfig()` (ConfigurationService.cs)
   - ⚠️ **If adding new cloud service:** Add config section to `CreateDefaultConfig()` (ConfigurationService.cs)
   - Add to UI toggles (if UI exists) - UI reads from config.json

1.5. **Verify Implementation Completeness:**
   - [ ] Feature class created in correct namespace (e.g., `RightClicks.Features.Video`)
   - [ ] Feature implements `IFileFeature` interface
   - [ ] Feature will be auto-discovered (not abstract, has public parameterless constructor)
   - [ ] If cloud-based: Uses `CloudinaryStorageService` or `FileHostingService`
   - [ ] If API-based: API key added to `CreateDefaultConfig()` in `ConfigurationService.cs`
   - [ ] Environment variables documented (if new ones added)

2. **Kill RightClicks and Restart Windows Explorer Before Building:**
   ```powershell
   taskkill /F /IM RightClicks.exe
   taskkill /F /IM explorer.exe
   Start-Process explorer.exe
   ```
   - **ALWAYS** kill RightClicks before building
   - **ALWAYS** restart Windows Explorer before building
   - Windows Explorer locks the shell extension DLL (RightClicksShellExtension.dll)
   - Prevents file locking errors during build
   - Required for successful deployment to %LOCALAPPDATA%\RightClicks\
   - Wait a few seconds after restarting Explorer before building

3. **Build the Project:**
   ```powershell
   dotnet build --verbosity minimal
   ```
   - Verify no compilation errors
   - Should have no file copy errors if Explorer was restarted

3.5. **Verify Deployment:**
   ```powershell
   # Check that files were copied to %LOCALAPPDATA%\RightClicks\
   Get-ChildItem "$env:LOCALAPPDATA\RightClicks\" | Select-Object Name, LastWriteTime | Format-Table -AutoSize

   # Verify config.json exists and is valid JSON
   Get-Content "$env:LOCALAPPDATA\RightClicks\config.json" | ConvertFrom-Json | Out-Null
   Write-Host "✅ config.json is valid" -ForegroundColor Green

   # Check for required environment variables
   @("FAL_KEY", "CLOUDINARY_API_KEY", "CLOUDINARY_API_SECRET") | ForEach-Object {
       $val = [Environment]::GetEnvironmentVariable($_, "User")
       if ($val) { Write-Host "✅ $_" -ForegroundColor Green }
       else { Write-Host "⚠️ $_ (NOT SET)" -ForegroundColor Yellow }
   }
   ```
   - Verify all DLLs have recent timestamps (within last few minutes)
   - Verify config.json contains all features (check feature count in logs)
   - Verify required environment variables are set

4. **Test via CLI:**
   ```bash
   RightClicks.exe --feature <FeatureId> --file <TestFile> --test-mode
   ```
   - Use full path to test file (e.g., `testfiles\Deleted_Models.mp4`)
   - Feature ID must match the `Id` property in the feature class

5. **Examine Logs:**
   - Open `RightClicks-TEST-YYYYMMDD-HHMMSS.log` in %LOCALAPPDATA%\RightClicks\logs\
   - Verify feature executed correctly
   - Check for errors, warnings, or issues
   - Confirm output file created with correct name
   - Use PowerShell to read logs:
     ```powershell
     Get-Content "$env:LOCALAPPDATA\RightClicks\logs\RightClicks-TEST-*.log" | Select-Object -Last 100
     ```

6. **Test via Context Menu (if applicable):**
   - Right-click test file in Windows Explorer
   - Select feature from RightClicks menu
   - Verify job appears in queue
   - Check notification on completion

7. **Report to Don:**
   - Feature implemented: ✅
   - CLI test passed: ✅
   - Output file correct: ✅
   - Logs clean: ✅
   - Ready for your testing

8. **Wait for Don's Approval:**
   - Don will test personally
   - Don will say "move on" or provide feedback
   - **ONLY THEN** update TASKS.md to mark task complete

9. **Clean Up:**
   ```bash
   RightClicks.exe --clear-logs --test-only
   ```

## Testing Standards

### Always Use Test Mode:
- Use `--test-mode` flag for isolated logs
- Each test gets its own timestamped log file
- Keeps main log clean

### Verify Everything:
- Output file exists in correct location (next to source file)
- Output file has correct name (per RightClicks.md specifications)
- No errors in log file
- FFmpeg commands executed successfully (if applicable)

### Clean Up After Testing:
- Delete test logs: `RightClicks.exe --clear-logs --test-only`
- Keep workspace clean for next session

## Code Standards

### Naming Conventions (from ARCHITECTURE.md Section 6):
- Features: `{Action}{FileType}Feature.cs` (e.g., `ExtractMp3Feature.cs`)
- Services: `{Purpose}Service.cs` (e.g., `JobQueueService.cs`)
- Models: `{Entity}.cs` (e.g., `Job.cs`, `AppConfig.cs`)
- Namespaces: `RightClicks.Features.Video`, `RightClicks.Services`, etc.

### Feature Implementation:
- All features implement `IFileFeature` interface
- Use async/await pattern: `Task<FeatureResult> ExecuteAsync(...)`
- Support `CancellationToken` for job cancellation
- Return `FeatureResult` with success/failure status

### Logging:
- **Always verbose** - log everything
- Use Serilog structured logging
- Log at appropriate levels:
  - `Log.Information()` - Normal operations
  - `Log.Warning()` - Non-critical issues
  - `Log.Error()` - Failures and exceptions
  - `Log.Debug()` - Detailed execution info

### Error Handling:
- Catch all exceptions
- Log full stack traces
- Return meaningful error messages in `FeatureResult`
- Never let exceptions crash the application

## Task Management

### Built-in Task Tools:
- Use `add_tasks` to create new tasks when starting a phase
- Use `update_tasks` to mark tasks IN_PROGRESS or COMPLETE
- Use `view_tasklist` to check current status
- These are for **active session tracking**

### TASKS.md File:
- High-level roadmap for Don to reference
- **Only update after Don approves** a completed task
- Mark with `[x]` when Don says "move on"
- This is the **source of truth** for project progress

### When Don Says "Move On":
1. Update TASKS.md - mark task as `[x]` complete
2. Update built-in task list - mark as COMPLETE
3. Commit changes if Don requests
4. Move to next task

## Communication Style

### When Reporting Test Results:
```
✅ Task Complete: Implement ExtractMp3Feature

**What I Did:**
- Created ExtractMp3Feature.cs in Features/Video/
- Implemented IFileFeature interface
- Added FFMpegCore integration

**Testing:**
- CLI Test: ✅ Passed
- Command: RightClicks.exe --feature ExtractMp3 --file "test.mp4" --test-mode
- Output: test.mp3 created (3.2 MB)
- Log: Clean, no errors

**Ready for your testing!**
```

### When Asking for Clarification:
- Be specific about what's unclear
- Provide options when possible
- Reference ARCHITECTURE.md sections

### When Encountering Issues:
- Report the issue clearly
- Show relevant log excerpts
- Suggest potential solutions
- Ask for guidance if needed

## Current Phase

**Phase 1: Foundation** (In Progress)
- Setting up core infrastructure
- Creating solution and projects
- Implementing base interfaces
- Setting up logging and configuration

**Next Phases:**
- Phase 2: First Feature (ExtractMp3 end-to-end)
- Phase 3: UI (System tray and main window)
- Phase 4: Job Queue System
- Phase 5: More Features
- Phase 6: Shell Integration
- Phase 7: Polish & Testing

## Important Reminders

- **Read ARCHITECTURE.md at start of each session** - All decisions are documented there
- **Don't update TASKS.md until Don approves** - Wait for "move on" confirmation
- **Always test before reporting complete** - You are the primary tester
- **Logs are your friend** - Examine them thoroughly
- **This is part-time work** - Break tasks into manageable chunks
- **You are the expert** - Don trusts you to implement correctly

## File Locations

**Installation Path:** `%LOCALAPPDATA%\RightClicks\`
**Logs:** `%LOCALAPPDATA%\RightClicks\logs\`
**Config:** `%LOCALAPPDATA%\RightClicks\config.json`
**FFmpeg:** `%LOCALAPPDATA%\RightClicks\bin\ffmpeg.exe`

## Required Environment Variables

**For Development/Testing (All Features Enabled by Default):**
- `FAL_KEY` - fal.ai API key (required for all lip sync features)
- `CLOUDINARY_API_KEY` - Cloudinary API key (required for file hosting)
- `CLOUDINARY_API_SECRET` - Cloudinary API secret (required for file deletion)
- `OPENAI_API_KEY` - OpenAI API key (required for transcription features - future)

**Verify All Set:**
```powershell
@("FAL_KEY", "CLOUDINARY_API_KEY", "CLOUDINARY_API_SECRET", "OPENAI_API_KEY") | ForEach-Object {
    $val = [Environment]::GetEnvironmentVariable($_, "User")
    if ($val) { Write-Host "✅ $_" -ForegroundColor Green }
    else { Write-Host "❌ $_ (NOT SET)" -ForegroundColor Red }
}
```

**Set Missing Variables:**
```powershell
[Environment]::SetEnvironmentVariable("FAL_KEY", "your-key-here", "User")
[Environment]::SetEnvironmentVariable("CLOUDINARY_API_KEY", "your-key-here", "User")
[Environment]::SetEnvironmentVariable("CLOUDINARY_API_SECRET", "your-secret-here", "User")
[Environment]::SetEnvironmentVariable("OPENAI_API_KEY", "your-key-here", "User")
```

**Note:** After setting environment variables, restart your terminal/IDE for changes to take effect.

## Install.bat Maintenance (CRITICAL)

**Location:** `install.bat` (repository root)

**Purpose:** One-click installation script for end users. Copies all necessary files to `%LOCALAPPDATA%\RightClicks\` and registers shell extension.

**⚠️ CRITICAL: Keep install.bat in sync with deployment changes!**

### When to Update install.bat:

**ALWAYS update install.bat when:**
1. ✅ **New RVC dependencies added** - Update RVC folder copy commands
2. ✅ **New environment variables required** - Add to environment variable check section
3. ✅ **New executables/DLLs added** - Update file copy commands
4. ✅ **New asset folders added** (e.g., new AI models) - Add xcopy commands
5. ✅ **Installation steps change** - Update script logic and comments
6. ✅ **New Python packages required** - Document in install.bat comments
7. ✅ **RVC folder structure changes** - Update all RVC copy commands

### Testing install.bat:

**Before committing changes to install.bat, ALWAYS test on clean environment:**

```powershell
# 1. Backup current installation
Rename-Item "$env:LOCALAPPDATA\RightClicks" "$env:LOCALAPPDATA\RightClicks.backup"

# 2. Run install.bat as Administrator
# Right-click install.bat → "Run as administrator"

# 3. Verify installation
Get-ChildItem "$env:LOCALAPPDATA\RightClicks\" -Recurse | Measure-Object -Property Length -Sum
# Should show ~10 GB total

# 4. Test RVC features
RightClicks.exe --feature RvcBeavis --file "testfiles\test.mp3" --test-mode

# 5. Check logs
Get-Content "$env:LOCALAPPDATA\RightClicks\logs\RightClicks-TEST-*.log" | Select-Object -Last 50

# 6. Restore backup if needed
Remove-Item "$env:LOCALAPPDATA\RightClicks" -Recurse -Force
Rename-Item "$env:LOCALAPPDATA\RightClicks.backup" "$env:LOCALAPPDATA\RightClicks"
```

### install.bat Checklist:

**Before marking any deployment-related task complete, verify:**
- [ ] Copies RightClicks.exe and all DLLs from `RightClicks\bin\Release\net8.0\`
- [ ] Copies RVC venv folder (~8-9 GB) to `%LOCALAPPDATA%\RightClicks\RVC\venv\`
- [ ] Copies RVC inference code (configs, infer, tools) to `%LOCALAPPDATA%\RightClicks\RVC\`
- [ ] Copies RVC assets (hubert, rmvpe, weights) to `%LOCALAPPDATA%\RightClicks\RVC\assets\`
- [ ] Checks for required environment variables (FAL_KEY, CLOUDINARY_API_KEY, CLOUDINARY_API_SECRET)
- [ ] Installs shell extension via `RightClicksShellManager.exe /install`
- [ ] Restarts Windows Explorer to load shell extension
- [ ] Provides clear success/error messages at each step
- [ ] Handles errors gracefully (missing files, permission issues, etc.)

### Deployment Size Warning:

**Total install size: ~10 GB**
- RVC venv: ~8-9 GB (Python 3.10 + all dependencies)
- RVC models: ~1.3 GB (24 voice models)
- RVC assets: ~500 MB (hubert, rmvpe models)
- RightClicks app: ~50 MB (executables, DLLs, shell extension)

**Users should be warned about disk space requirements in README.md and install.bat.**

### Common install.bat Issues:

**Issue:** "Failed to copy RVC venv"
- **Cause:** RVC folder not in repository root
- **Fix:** Ensure `RVC\` folder exists at `E:\MyApps\RightClicks\RVC\`

**Issue:** "Failed to install shell extension"
- **Cause:** Not running as Administrator
- **Fix:** Right-click install.bat → "Run as administrator"

**Issue:** "RVC features not appearing"
- **Cause:** RVC path not found by `RvcModelDiscoveryService`
- **Fix:** Check logs, verify `%LOCALAPPDATA%\RightClicks\RVC\` exists

**Issue:** "Missing environment variables"
- **Cause:** User hasn't set API keys
- **Fix:** Document in README.md, provide setup instructions

## Quick Reference Commands

```bash
# Test a feature
RightClicks.exe --feature ExtractMp3 --file "test.mp4" --test-mode

# Clear test logs
RightClicks.exe --clear-logs --test-only

# Clear all logs
RightClicks.exe --clear-logs

# Force regenerate config.json (deletes existing, creates fresh with all features)
Remove-Item "$env:LOCALAPPDATA\RightClicks\config.json" -Force
RightClicks.exe --help  # Any command will trigger config regeneration

# Install shell hooks (requires admin)
RightClicksShellManager.exe /install

# Uninstall shell hooks
RightClicksShellManager.exe /uninstall

# Run install.bat (for end users)
# Right-click install.bat → "Run as administrator"
```

---

**Remember: You are the primary developer and tester. Don relies on you to deliver tested, working features. Take pride in your work and test thoroughly before handing off!**

