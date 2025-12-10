# Installation Guide - SystemTrayApp

**Complete beginner-friendly guide to installing SystemTrayApp**

---

## 📥 Step 1: Download

1. Go to the [Releases page](https://github.com/hughesdo/SystemTrayApp/releases)
2. Download **SystemTrayApp_v1.1.0.zip** (the latest version)
3. Save it somewhere you can find it (like Downloads folder)

**What you're downloading:**
- A ZIP file containing the application and installer
- Size: ~50KB (very small!)
- No viruses, no ads, no tracking

---

## 📂 Step 2: Choose Installation Location

**IMPORTANT:** Don't run the app from your Downloads folder or Desktop!

### ✅ Good Locations (Pick One):

**Option A: Program Files (Recommended)**
```
C:\Program Files\SystemTrayApp\
```
- Standard location for Windows programs
- Organized and professional
- Easy to find

**Option B: Custom Apps Folder**
```
C:\Apps\SystemTrayApp\
```
- Simple and clean
- All your apps in one place

**Option C: Different Drive**
```
D:\Programs\SystemTrayApp\
```
- If you have multiple drives
- Keeps C: drive clean

### ❌ Bad Locations (Avoid):

- `C:\Users\YourName\Downloads\` - Downloads folder gets cleaned out
- `C:\Users\YourName\Desktop\` - Clutters your desktop
- Temporary folders - App needs a permanent home

---

## 📦 Step 3: Extract the ZIP File

1. **Navigate to your chosen location:**
   - Open File Explorer
   - Go to `C:\Program Files\` (or your chosen location)

2. **Create the folder:**
   - Right-click → New → Folder
   - Name it: `SystemTrayApp`
   - Open the folder

3. **Extract the ZIP:**
   - Go back to your Downloads folder
   - Right-click `SystemTrayApp_v1.0.zip`
   - Select "Extract All..."
   - Browse to `C:\Program Files\SystemTrayApp\`
   - Click "Extract"

**You should now see these files:**
```
C:\Program Files\SystemTrayApp\
├── SystemTrayApp.exe
├── install.bat
├── uninstall.bat
├── README.MD
├── LICENSE
└── urls.xml
```

---

## 🚀 Step 4: Run the Installer

1. **Find `install.bat`** in the SystemTrayApp folder

2. **Right-click on `install.bat`**

3. **Select "Run as administrator"**
   - This is needed to create the startup shortcut
   - Windows may ask "Do you want to allow this app to make changes?"
   - Click "Yes"

4. **Follow the prompts:**

   **Check 1: .NET Framework**
   - The installer checks if .NET Framework 4.0+ is installed
   - If not found, it will offer to open the download page
   - Install .NET Framework, then run install.bat again
   - (Most Windows PCs already have this)

   **Check 2: yt-dlp.exe**
   - The installer checks if yt-dlp.exe exists
   - If not found, it will tell you
   - The app will offer to download it automatically on first run

   **Prompt: Start now?**
   - "Would you like to start the application now? (Y/N)"
   - Press `Y` and Enter to start now
   - Or press `N` if you want to start it later

5. **Installation complete!**
   - You'll see a summary of what was installed
   - Press any key to close the installer

---

## ✅ Step 5: Verify Installation

### Check 1: System Tray Icon
- Look at the bottom-right of your screen (near the clock)
- You should see a small icon for SystemTrayApp
- If you don't see it, click the up arrow (^) to show hidden icons

### Check 2: Startup Shortcut
1. Press `Win + R` on your keyboard
2. Type: `shell:startup`
3. Press Enter
4. You should see a shortcut named "SystemTrayApp"
5. This means the app will start automatically when Windows boots

### Check 3: Test It!
1. Go to YouTube and find any video
2. Copy the URL (Ctrl+C or right-click → Copy)
3. Wait 1-2 seconds
4. A command window should appear briefly (yt-dlp downloading)
5. Check `Documents\Memes_YYYY-MM-DD\` for your video

---

## 🎯 First-Time Setup (Automatic)

When you first run the app:

1. **If yt-dlp.exe is missing:**
   - A dialog will appear: "yt-dlp.exe not found!"
   - Click "Yes" to download automatically
   - Wait ~30 seconds for download
   - App will start normally

2. **App starts:**
   - Icon appears in system tray
   - No windows open (it runs in background)
   - Ready to monitor clipboard

---

## 🖱️ Using the App

### Left-Click the Tray Icon:
- Opens today's Memes folder
- Quick access to downloaded videos

### Right-Click the Tray Icon:
- **Explorer** - Opens today's Memes folder
- **About** - Shows app info and credits
- **Exit** - Closes the app

### Downloading Videos:
1. Copy any YouTube or Twitter/X video URL
2. Wait 1-2 seconds
3. Download starts automatically
4. Video saves to `Documents\Memes_YYYY-MM-DD\`

---

## 🔧 Troubleshooting

### "Windows protected your PC" message
- This is normal for new apps
- Click "More info"
- Click "Run anyway"
- This happens because the app isn't digitally signed (costs money)

### App doesn't start
1. **Check .NET Framework:**
   - Run install.bat again - it will check for you
   - Or download from: https://dotnet.microsoft.com/download/dotnet-framework/net48
2. **Unblock the file:**
   - Right-click SystemTrayApp.exe → Properties
   - Click "Unblock" if you see that option
   - Click OK and try again

### Videos not downloading
1. Check the log file: `SystemTrayApp_Debug.log` (same folder as exe)
2. Look for error messages
3. Verify yt-dlp.exe exists in the app folder or C:\bin

### Can't find downloaded videos
- Open File Explorer
- Navigate to: `%USERPROFILE%\Documents\`
- Look for folders named `Memes_YYYY-MM-DD`
- Example: `Memes_2025-11-02`

### App not starting with Windows
1. Press `Win + R`
2. Type: `shell:startup`
3. Check if SystemTrayApp shortcut exists
4. If not, run `install.bat` again

---

## 🗑️ Uninstalling

1. Right-click the tray icon → Exit
2. Go to the app folder
3. Run `uninstall.bat`
4. Follow the prompts:
   - Remove startup shortcut? (Y/N)
   - Delete log file? (Y/N)
   - Delete downloaded videos? (Y/N)
5. Optionally delete the app folder

---

## 📞 Getting Help

### Check the Log File
- Location: Same folder as SystemTrayApp.exe
- Name: `SystemTrayApp_Debug.log`
- Contains detailed information about what the app is doing

### Common Issues
- **URL not detected:** Make sure you're copying the full URL
- **Download fails:** Check your internet connection
- **yt-dlp errors:** Try downloading yt-dlp manually from GitHub

### Report Issues
- GitHub Issues: [Link to your repo]/issues
- Include the log file contents
- Describe what you were trying to do

---

## 🎉 You're Done!

Your SystemTrayApp is now installed and ready to use!

**Quick reminder:**
- Copy video URLs → They download automatically
- Click tray icon → Opens today's folder
- Videos save to: `Documents\Memes_YYYY-MM-DD\`

**Created by @oneHung** | [X Profile](https://x.com/oneHung)

Licensed under CC BY 4.0 - Free to use and share!

