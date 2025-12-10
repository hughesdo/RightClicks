# Building SystemTrayApp from Source

**Note:** Most users should download the pre-built release from the [Releases page](https://github.com/hughesdo/SystemTrayApp/releases). Only build from source if you want to modify the code or contribute.

---

## 📋 Prerequisites

### Required:
- **Windows 10 or 11** (recommended)
- **Visual Studio 2019 or later** (Community Edition is free)
  - Download: https://visualstudio.microsoft.com/downloads/
- **.NET Framework 4.8 Developer Pack**
  - Usually included with Visual Studio
  - Or download: https://dotnet.microsoft.com/download/dotnet-framework/net48

### Optional:
- **Git** (for cloning the repository)
  - Download: https://git-scm.com/downloads

---

## 🚀 Quick Build

### Option 1: Using Visual Studio (Recommended)

1. **Clone the repository:**
   ```bash
   git clone https://github.com/hughesdo/SystemTrayApp.git
   cd SystemTrayApp
   ```

2. **Open the solution:**
   - Double-click `SystemTrayApp.sln`
   - Or open Visual Studio → File → Open → Project/Solution → Select `SystemTrayApp.sln`

3. **Restore dependencies:**
   - Visual Studio will automatically restore NuGet packages
   - If not, right-click solution → Restore NuGet Packages

4. **Build:**
   - Select **Release** configuration (dropdown at top)
   - Select **x86** platform
   - Press `Ctrl+Shift+B` or Build → Build Solution

5. **Output:**
   - Built executable: `bin\Release\SystemTrayApp.exe`
   - Ready to run!

### Option 2: Using MSBuild (Command Line)

1. **Clone the repository:**
   ```bash
   git clone https://github.com/hughesdo/SystemTrayApp.git
   cd SystemTrayApp
   ```

2. **Open Developer Command Prompt:**
   - Start Menu → Visual Studio 2022 → Developer Command Prompt

3. **Build:**
   ```bash
   msbuild SystemTrayApp.sln /t:Rebuild /p:Configuration=Release /p:Platform=x86
   ```

4. **Output:**
   - Built executable: `bin\Release\SystemTrayApp.exe`

---

## 📦 After Building

### Test Your Build:

1. **Navigate to output:**
   ```bash
   cd bin\Release
   ```

2. **Run the app:**
   ```bash
   SystemTrayApp.exe
   ```

3. **Test functionality:**
   - Check system tray for icon
   - Copy a YouTube URL
   - Verify download starts

### Install Your Build:

1. **Copy installer files:**
   ```bash
   copy ..\..\install.bat .
   copy ..\..\uninstall.bat .
   copy ..\..\README.MD .
   copy ..\..\LICENSE .
   ```

2. **Run installer:**
   ```bash
   install.bat
   ```

---

## 🔧 Development Setup

### For Active Development:

1. **Set startup project:**
   - Right-click `SystemTrayApp` in Solution Explorer
   - Set as Startup Project

2. **Debug configuration:**
   - Use **Debug** configuration for development
   - Breakpoints and debugging enabled
   - Log file: `bin\Debug\SystemTrayApp_Debug.log`

3. **Run with debugging:**
   - Press `F5` to start with debugger
   - Press `Ctrl+F5` to start without debugger

### Project Structure:

```
SystemTrayApp/
├── Properties/
│   ├── AssemblyInfo.cs      # Version and metadata
│   ├── Resources.resx       # Embedded resources
│   └── Settings.settings    # App settings
├── Images/                  # Icons and images
│   ├── SystemTrayApp2.ico   # Main app icon
│   └── *.png                # Menu icons
├── AboutBox.cs              # About dialog
├── ContextMenus.cs          # Right-click menu
├── ProcessIcon.cs           # Main app logic
├── Program.cs               # Entry point
├── SystemTrayApp.csproj     # Project file
└── urls.xml                 # Legacy config (unused)
```

---

## 🐛 Troubleshooting

### Build Errors:

**Error: "The type or namespace name 'X' could not be found"**
- Solution: Restore NuGet packages
- Right-click solution → Restore NuGet Packages

**Error: "Could not find .NET Framework 4.8"**
- Solution: Install .NET Framework 4.8 Developer Pack
- Download: https://dotnet.microsoft.com/download/dotnet-framework/net48

**Error: "MSBuild not found"**
- Solution: Use Developer Command Prompt for Visual Studio
- Or add MSBuild to PATH

### Runtime Errors:

**App doesn't start after building:**
1. Check `bin\Release\SystemTrayApp_Debug.log` for errors
2. Verify .NET Framework 4.8 is installed
3. Right-click exe → Properties → Unblock

**"urls.xml not found":**
- This is auto-copied during build
- If missing, manually copy from project root to `bin\Release\`

---

## 🧪 Testing Your Changes

### Manual Testing:

1. **Build in Debug mode**
2. **Run from Visual Studio** (F5)
3. **Check log file:** `bin\Debug\SystemTrayApp_Debug.log`
4. **Test scenarios:**
   - Copy YouTube URL: `https://www.youtube.com/watch?v=dQw4w9WgXcQ`
   - Copy Twitter URL: `https://x.com/i/status/1234567890`
   - Left-click tray icon
   - Right-click → About
   - Right-click → Exit

### Test URLs:

See `TEST_URLS.txt` for comprehensive test cases.

---

## 📝 Making Changes

### Code Style:

- Use tabs for indentation (existing style)
- Add XML documentation comments for public methods
- Log important actions to debug log
- Handle exceptions gracefully

### Before Committing:

1. **Build in Release mode** - Verify no errors
2. **Test functionality** - Ensure nothing broke
3. **Update CHANGELOG.md** - Document your changes
4. **Update version** - In `Properties\AssemblyInfo.cs` if needed

---

## 🚀 Creating a Release Build

### For Distribution:

1. **Clean solution:**
   ```
   Build → Clean Solution
   ```

2. **Build Release:**
   ```
   Configuration: Release
   Platform: x86
   Build → Rebuild Solution
   ```

3. **Verify output:**
   ```
   bin\Release\SystemTrayApp.exe
   ```

4. **Copy required files:**
   ```bash
   copy install.bat bin\Release\
   copy uninstall.bat bin\Release\
   copy README.MD bin\Release\
   copy LICENSE bin\Release\
   copy CHANGELOG.md bin\Release\
   copy INSTALL_GUIDE.md bin\Release\
   copy TEST_URLS.txt bin\Release\
   ```

5. **Create ZIP:**
   - Zip contents of `bin\Release\`
   - Name: `SystemTrayApp_vX.X.zip`
   - Upload to GitHub Releases

---

## 🤝 Contributing

### Workflow:

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/my-feature`
3. Make your changes
4. Test thoroughly
5. Commit: `git commit -m "Add my feature"`
6. Push: `git push origin feature/my-feature`
7. Create Pull Request on GitHub

### Guidelines:

- Follow existing code style
- Add comments for complex logic
- Update documentation
- Test on Windows 10/11
- Don't commit binaries (exe, dll, pdb)

---

## 📚 Additional Resources

- **Visual Studio Docs:** https://docs.microsoft.com/visualstudio/
- **.NET Framework Docs:** https://docs.microsoft.com/dotnet/framework/
- **C# Language Reference:** https://docs.microsoft.com/dotnet/csharp/
- **Windows Forms:** https://docs.microsoft.com/dotnet/desktop/winforms/

---

## 📄 License

This project is licensed under CC BY 4.0 - see [LICENSE](LICENSE) file.

**Created by @oneHung** | https://x.com/oneHung

