# SystemTrayApp - Automatic Video Downloader

**Copy a video URL. It downloads automatically. That's it.**

---

## ⚡ **Quick Start (3 Steps)**

1. **[Download the latest release](https://github.com/hughesdo/SystemTrayApp/releases)** (SystemTrayApp.zip)
2. **Extract to:** `C:\Program Files\SystemTrayApp\` (or any permanent location - NOT Downloads or Desktop)
3. **Double-click `install.bat`**

Done! The app is now in your system tray. Copy any YouTube or Twitter video URL and it downloads automatically.

---

## 📖 What It Does

This Windows app sits in your system tray and watches your clipboard. When you copy a YouTube or Twitter/X video URL, it automatically downloads the video to organized daily folders in your Documents.

**No clicking. No pasting. Just copy the URL.**

---

## 📋 Requirements

- **Windows 10 or 11** (recommended) - Has .NET Framework 4.8 pre-installed
- **Windows 7/8** - Requires .NET Framework 4.8 download (free)
- Internet connection

**Note:** The installer automatically checks for .NET Framework 4.8 and guides you to download it if needed. Windows 10/11 users already have this!

---

## 🎬 Supported Videos

| Platform | URL Example | Status |
|----------|-------------|--------|
| YouTube | `youtube.com/watch?v=...` | ✅ Works |
| YouTube | `youtu.be/...` | ✅ Works |
| Twitter/X | `x.com/i/status/...` | ✅ Works |

---

## 📥 Installation (Detailed)

### Simple Installation (Recommended)

1. **Download** [SystemTrayApp.zip](https://github.com/hughesdo/SystemTrayApp/releases) from the latest release

2. **Extract to a permanent location:**
   - ✅ Good: `C:\Program Files\SystemTrayApp\`
   - ✅ Good: `C:\Apps\SystemTrayApp\`
   - ✅ Good: `D:\Programs\SystemTrayApp\`
   - ❌ Bad: `C:\Users\YourName\Downloads\` (temporary)
   - ❌ Bad: Desktop (clutters desktop)

3. **Double-click `install.bat`:**
   - No need to run as Administrator
   - The installer will:
     - ✅ Check for .NET Framework 4.8 (pre-installed on Windows 10/11)
     - ✅ Find the application executable
     - ✅ Create Windows startup shortcut (auto-run on boot)
     - ✅ Check for yt-dlp.exe (offers to download if missing)
     - ✅ Create the Memes folder in Documents
     - ✅ Optionally start the app immediately

4. **Done!** Look for the icon in your system tray (near the clock)

**That's it! No Visual Studio, no .NET SDK, no building required. Just download and run install.bat.**

### Manual Install (Advanced Users)

1. Extract ZIP to permanent location
2. Download [yt-dlp.exe](https://github.com/yt-dlp/yt-dlp/releases) and place in `bin\Release\` folder or `C:\bin\`
3. Run `bin\Release\SystemTrayApp.exe`
4. (Optional) Create startup shortcut: `Win+R` → `shell:startup` → create shortcut to exe

## 🎯 How to Use

1. **The app runs in your system tray** (near the clock) - you'll see a small icon
2. **Copy any video URL** - YouTube or Twitter/X
3. **Video downloads automatically** to `Documents\Memes_YYYY-MM-DD\`
4. **Click the tray icon** to open today's download folder

That's it. No buttons to click, no windows to open.

### 🖱️ System Tray Interaction

**Left-click** the tray icon:
- Opens today's Memes folder (`Documents\Memes_YYYY-MM-DD\`)
- Creates the folder if it doesn't exist yet
- Quick access to your downloaded videos!

**Right-click** the tray icon for menu:
- **Explorer**: Opens today's Memes folder (same as left-click)
- **About**: View application information and version
- **Exit**: Close the application

## 🔧 How It Works

1. **Clipboard Monitoring**: Checks your clipboard every second for changes
2. **URL Detection**: Uses regex patterns to identify supported video URLs
3. **Auto-Download**: When detected:
   - Creates a dated folder: `Documents\Memes_YYYY-MM-DD\`
   - Generates a batch file with yt-dlp command
   - Launches the download process
   - Shows a notification
4. **Logging**: All actions are logged to `SystemTrayApp_Debug.log`

## 🛠️ Building from Source (For Developers)

### Prerequisites
- Visual Studio 2019 or later with .NET desktop development workload
- .NET Framework 4.8 SDK (included with Visual Studio)

### Steps
1. Clone the repository:
   ```bash
   git clone https://github.com/hughesdo/SystemTrayApp.git
   cd SystemTrayApp
   ```

2. Open `SystemTrayApp.sln` in Visual Studio

3. Build the solution:
   - Press `F5` or `Ctrl+Shift+B`
   - Or use Build → Build Solution
   - **Debug**: For testing with full logging
   - **Release**: For production use

4. Output location:
   - Debug: `bin\Debug\SystemTrayApp.exe`
   - Release: `bin\Release\SystemTrayApp.exe`

5. Run `install.bat` to install the built application

**Note:** Regular users don't need to build from source. Just download the pre-built release and run `install.bat`.

## 🗑️ Uninstallation

**Simple Method:**
1. Double-click `uninstall.bat`
2. Follow the prompts

The uninstaller will:
- ✅ Stop the running application
- ✅ Remove the startup shortcut
- ✅ Ask if you want to delete log files
- ✅ Ask if you want to delete downloaded videos
- ✅ Leave application files (you can delete the folder manually)

**Manual Method:**
1. Right-click the tray icon and select **Exit**
2. Delete the startup shortcut from: `%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup\`
3. Delete the application folder

## 📝 Configuration

The application uses these default settings:

| Setting | Value | Customizable |
|---------|-------|--------------|
| **Download Location** | `%USERPROFILE%\Documents\Memes_YYYY-MM-DD\` | ❌ (hardcoded) |
| **Clipboard Check Interval** | 1 second | ❌ (hardcoded) |
| **yt-dlp Search Paths** | App folder, `C:\bin`, PATH | ✅ (automatic) |
| **Log File Location** | Same as executable | ❌ (hardcoded) |

## 🐛 Troubleshooting

### Videos aren't downloading
1. Check if yt-dlp.exe is installed (app will prompt on first run)
2. Check the log file: `SystemTrayApp_Debug.log`
3. Verify the URL format is supported
4. Ensure you have internet connection

### App doesn't start with Windows
1. Run `install.bat` again
2. Or manually create a shortcut in: `shell:startup`

### Can't find downloaded videos
- Look in: `%USERPROFILE%\Documents\Memes_YYYY-MM-DD\`
- Check the log file for the exact path used

### Log file location
- Same folder as `SystemTrayApp.exe`
- Contains detailed information about all operations
- Includes clipboard content, regex matches, download attempts

## 🔒 Privacy & Security

- **No data collection**: Everything runs locally on your machine
- **No internet access** except:
  - Downloading yt-dlp.exe (one-time, optional)
  - yt-dlp downloading videos (when you copy URLs)
- **Clipboard monitoring**: Only reads text, never modifies (except during download)
- **Open source**: All code is visible and auditable

## 📦 Dependencies

- **yt-dlp**: Command-line video downloader
  - Auto-downloaded by the app if missing
  - Manual download: https://github.com/yt-dlp/yt-dlp/releases
  - License: Unlicense

## 🤝 Contributing

Contributions are welcome! Please:
1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Test thoroughly
5. Submit a pull request

## 📄 License

This project is licensed under the **Creative Commons Attribution 4.0 International License (CC BY 4.0)**.

You are free to:
- ✅ **Share** — copy and redistribute the material in any medium or format
- ✅ **Adapt** — remix, transform, and build upon the material for any purpose, even commercially

Under the following terms:
- 📝 **Attribution** — You must give appropriate credit, provide a link to the license, and indicate if changes were made

See the [LICENSE](LICENSE) file for full details or visit: https://creativecommons.org/licenses/by/4.0/

## 👤 Author

**@oneHung**
- X (Twitter): [@oneHung](https://x.com/oneHung)
- Created: 2025

If you use or modify this software, please give credit by mentioning @oneHung and linking back to the original project.

## 🙏 Acknowledgments

- **yt-dlp** team for the excellent video downloader
- Original system tray template inspiration

## 📊 Changelog

### Version 1.0 (Current)
- ✅ Automatic clipboard monitoring
- ✅ YouTube and Twitter/X support
- ✅ Auto-download yt-dlp.exe
- ✅ Multi-location yt-dlp detection
- ✅ Comprehensive logging
- ✅ Installer and uninstaller scripts
- ✅ Balloon notifications

