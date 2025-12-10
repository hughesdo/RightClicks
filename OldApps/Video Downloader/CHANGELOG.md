# Changelog

All notable changes to SystemTrayApp will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2025-11-02

### Added
- **Automatic clipboard monitoring** for video URLs
- **Multi-location yt-dlp.exe detection** (app folder, C:\bin, PATH)
- **Auto-download yt-dlp.exe** from GitHub releases if not found
- **Comprehensive debug logging** to SystemTrayApp_Debug.log
- **Balloon notifications** for download status
- **Smart folder opening** - Left-click tray icon opens today's Memes folder
- **Clickable X profile link** in About dialog - click @oneHung to visit profile
- **Professional installer** (install.bat) with startup shortcut creation
- **Clean uninstaller** (uninstall.bat) with optional cleanup
- **Organized downloads** to dated folders (Memes_YYYY-MM-DD)
- **CC BY 4.0 License** - Free to use, modify, and share with attribution

### Supported Platforms
- **YouTube**: All URL formats
  - `https://www.youtube.com/watch?v=...`
  - `https://youtube.com/watch?v=...`
  - `https://youtu.be/...`
  - `https://m.youtube.com/watch?v=...`
  - With query parameters (timestamps, playlists, etc.)
  - Both HTTP and HTTPS
- **Twitter/X**: Status URLs
  - `https://x.com/i/status/...`

### Features
- **Clipboard Check Interval**: 1 second
- **Download Location**: `%USERPROFILE%\Documents\Memes_YYYY-MM-DD\`
- **Auto-start**: Creates Windows startup shortcut
- **Tray Icon Interaction**:
  - Left-click: Opens today's download folder
  - Right-click: Context menu (Explorer, About, Exit)
- **Error Handling**: Comprehensive try-catch with logging
- **User Feedback**: Balloon tip notifications

### Technical Details
- **Framework**: .NET Framework 4.0 Client Profile
- **Platform**: Windows x86
- **Language**: C# 7.3
- **Dependencies**: yt-dlp.exe (auto-downloaded)
- **Log File**: SystemTrayApp_Debug.log (same folder as exe)

### Documentation
- README.MD - User guide
- RELEASE_CHECKLIST.md - Release process
- IMPROVEMENTS_SUMMARY.md - Development changelog
- QUICK_START_FOR_GITHUB.md - Deployment guide
- TEST_URLS.txt - Testing scenarios

### Known Limitations
- Windows-only (by design)
- Requires .NET Framework 4.0+
- Clipboard check interval is hardcoded (1 second)
- Download folder format is hardcoded (Memes_YYYY-MM-DD)
- No GUI for settings/configuration

### Security & Privacy
- No data collection
- No internet access except:
  - Downloading yt-dlp.exe (one-time, optional)
  - yt-dlp downloading videos (when URLs detected)
- All processing is local
- Open source and auditable

## [Unreleased]

### Planned Features
- Settings UI for configuration
- Custom download location
- Video quality selection
- Download queue management
- Update checker
- Multiple language support
- Download progress indicator
- Keyboard shortcuts
- Portable mode option

### Potential Improvements
- Configurable clipboard check interval
- Custom folder naming patterns
- Support for more video platforms
- Download history viewer
- Duplicate detection
- Bandwidth limiting
- Scheduled downloads
- Browser extension integration

---

## Version History

### v1.0.0 (2025-11-02) - Initial Release
First public release with core functionality:
- Automatic clipboard monitoring
- YouTube and Twitter/X support
- Auto-download yt-dlp
- Professional installer/uninstaller
- Comprehensive logging
- Smart folder opening

---

## Upgrade Notes

### From Development to v1.0.0
- No breaking changes
- Existing log files will continue to work
- Existing downloads are not affected
- Startup shortcut may need to be recreated (run install.bat)

---

## Support

- **Issues**: https://github.com/YOUR_USERNAME/SystemTrayApp/issues
- **Documentation**: See README.MD
- **Log File**: Check SystemTrayApp_Debug.log for troubleshooting

---

## Credits

- **Created by**: @oneHung (https://x.com/oneHung)
- **yt-dlp**: https://github.com/yt-dlp/yt-dlp

---

## License

Creative Commons Attribution 4.0 International (CC BY 4.0)
https://creativecommons.org/licenses/by/4.0/

Free to use, modify, and share - just give credit to @oneHung

