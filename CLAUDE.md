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

## Development Workflow (CRITICAL)

### For Every Task:

1. **Implement Everywhere:**
   - Write the feature code (e.g., `ExtractMp3Feature.cs`)
   - Add to UI toggles (if UI exists)
   - Add to default `config.json`
   - Update any related components

2. **Test via CLI:**
   ```bash
   RightClicks.exe --feature <FeatureId> --file <TestFile> --test-mode
   ```

3. **Examine Logs:**
   - Open `RightClicks-TEST-YYYYMMDD-HHMMSS.log`
   - Verify feature executed correctly
   - Check for errors, warnings, or issues
   - Confirm output file created with correct name

4. **Test via Context Menu (if applicable):**
   - Right-click test file in Windows Explorer
   - Select feature from RightClicks menu
   - Verify job appears in queue
   - Check notification on completion

5. **Report to Don:**
   - Feature implemented: ✅
   - CLI test passed: ✅
   - Output file correct: ✅
   - Logs clean: ✅
   - Ready for your testing

6. **Wait for Don's Approval:**
   - Don will test personally
   - Don will say "move on" or provide feedback
   - **ONLY THEN** update TASKS.md to mark task complete

7. **Clean Up:**
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

## Quick Reference Commands

```bash
# Test a feature
RightClicks.exe --feature ExtractMp3 --file "test.mp4" --test-mode

# Clear test logs
RightClicks.exe --clear-logs --test-only

# Clear all logs
RightClicks.exe --clear-logs

# Install shell hooks (requires admin)
RightClicksShellManager.exe /install

# Uninstall shell hooks
RightClicksShellManager.exe /uninstall
```

---

**Remember: You are the primary developer and tester. Don relies on you to deliver tested, working features. Take pride in your work and test thoroughly before handing off!**

