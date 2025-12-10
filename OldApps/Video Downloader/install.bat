@echo off
setlocal enabledelayedexpansion

echo ========================================
echo SystemTrayApp - Video Downloader
echo Installation Script
echo ========================================
echo.

REM Get the directory where this script is located
set "INSTALL_DIR=%~dp0"
set "INSTALL_DIR=%INSTALL_DIR:~0,-1%"

echo Installation directory: %INSTALL_DIR%
echo.

REM Check for .NET Framework 4.8 (pre-installed on Windows 10/11)
echo [0/4] Checking .NET Framework...
set "DOTNET_FOUND=0"

REM Check registry for .NET 4.8
reg query "HKLM\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full" /v Release >nul 2>&1
if !errorlevel! equ 0 (
    for /f "tokens=3" %%a in ('reg query "HKLM\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full" /v Release ^| findstr Release') do set RELEASE=%%a
    REM .NET 4.8 = 528040 or higher
    if !RELEASE! geq 528040 (
        set "DOTNET_FOUND=1"
        echo    ✓ .NET Framework 4.8 is installed
    ) else (
        echo    ℹ .NET Framework found, but version is older than 4.8
        echo    ✓ App should still work, but 4.8 is recommended
        set "DOTNET_FOUND=1"
    )
)

if !DOTNET_FOUND! equ 0 (
    echo    ✗ .NET Framework 4.8 NOT found
    echo.
    echo    ERROR: This application requires .NET Framework 4.8
    echo.
    echo    Good news: Windows 10 and 11 have this pre-installed!
    echo    If you're on Windows 7/8, download from:
    echo    https://dotnet.microsoft.com/download/dotnet-framework/net48
    echo.
    choice /C YN /M "Would you like to open the download page now"
    if !errorlevel! equ 1 (
        start https://dotnet.microsoft.com/download/dotnet-framework/net48
        echo.
        echo    Please install .NET Framework 4.8, then run this installer again.
        echo.
        pause
        exit /b 1
    ) else (
        echo.
        echo    Please install .NET Framework 4.8 and run this installer again.
        echo.
        pause
        exit /b 1
    )
)
echo.

REM [Step 1/4] Locate the application
echo [1/4] Locating application...

REM Check if we're in the Release folder
if exist "%INSTALL_DIR%\SystemTrayApp.exe" (
    set "EXE_PATH=%INSTALL_DIR%\SystemTrayApp.exe"
    set "STARTUP_DIR=%INSTALL_DIR%"
    echo    Found application: SystemTrayApp.exe
) else if exist "%INSTALL_DIR%\bin\Release\SystemTrayApp.exe" (
    set "EXE_PATH=%INSTALL_DIR%\bin\Release\SystemTrayApp.exe"
    set "STARTUP_DIR=%INSTALL_DIR%\bin\Release"
    echo    Found application: bin\Release\SystemTrayApp.exe
) else (
    echo    ERROR: SystemTrayApp.exe not found!
    echo.
    echo    Please make sure you have the complete installation package.
    echo    The file should be in the current directory or bin\Release\
    echo.
    pause
    exit /b 1
)
echo.

REM [Step 2/4] Create startup shortcut
echo [2/4] Creating startup shortcut...
set "STARTUP_FOLDER=%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup"
set "SHORTCUT_PATH=%STARTUP_FOLDER%\SystemTrayApp.lnk"

REM Use PowerShell to create shortcut
powershell -Command "$WS = New-Object -ComObject WScript.Shell; $SC = $WS.CreateShortcut('%SHORTCUT_PATH%'); $SC.TargetPath = '!EXE_PATH!'; $SC.WorkingDirectory = '!STARTUP_DIR!'; $SC.Description = 'System Tray Video Downloader'; $SC.Save()"

if exist "%SHORTCUT_PATH%" (
    echo    ✓ Startup shortcut created successfully
) else (
    echo    ✗ Failed to create startup shortcut
)
echo.

REM [Step 3/4] Check for yt-dlp.exe
echo [3/4] Checking for yt-dlp.exe...

set "YTDLP_FOUND=0"

REM Check in application directory
if exist "!STARTUP_DIR!\yt-dlp.exe" (
    echo    ✓ Found yt-dlp.exe in application directory
    set "YTDLP_FOUND=1"
)

REM Check in C:\bin
if exist "C:\bin\yt-dlp.exe" (
    echo    ✓ Found yt-dlp.exe in C:\bin
    set "YTDLP_FOUND=1"
)

REM Check in PATH
where yt-dlp.exe >nul 2>&1
if !errorlevel! equ 0 (
    echo    ✓ Found yt-dlp.exe in PATH
    set "YTDLP_FOUND=1"
)

if !YTDLP_FOUND! equ 0 (
    echo    ℹ yt-dlp.exe not found
    echo.
    choice /C YN /M "Would you like to download yt-dlp.exe now"
    if !errorlevel! equ 1 (
        echo.
        echo    Downloading yt-dlp.exe...
        echo    This may take a minute depending on your connection...
        echo.

        REM Download to TEMP first to avoid antivirus corruption
        set "TEMP_YTDLP=%TEMP%\yt-dlp-installer.exe"
        set "TARGET_YTDLP=!STARTUP_DIR!\yt-dlp.exe"

        REM Delete temp file if exists
        if exist "!TEMP_YTDLP!" del "!TEMP_YTDLP!"

        REM Download using PowerShell
        powershell -Command "$ProgressPreference = 'SilentlyContinue'; try { Invoke-WebRequest -Uri 'https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe' -OutFile '!TEMP_YTDLP!' -UseBasicParsing; exit 0 } catch { exit 1 }"

        if !errorlevel! equ 0 (
            REM Verify download
            if exist "!TEMP_YTDLP!" (
                REM Copy to application directory
                copy "!TEMP_YTDLP!" "!TARGET_YTDLP!" >nul

                if exist "!TARGET_YTDLP!" (
                    echo    ✓ yt-dlp.exe downloaded successfully
                    echo    ✓ Installed to: !TARGET_YTDLP!

                    REM Clean up temp file
                    del "!TEMP_YTDLP!" 2>nul
                ) else (
                    echo    ✗ Failed to copy yt-dlp.exe to application directory
                    echo    ℹ The application will offer to download it on first run
                )
            ) else (
                echo    ✗ Download failed - file not found
                echo    ℹ The application will offer to download it on first run
            )
        ) else (
            echo    ✗ Download failed
            echo    ℹ The application will offer to download it on first run
        )
    ) else (
        echo.
        echo    ℹ Skipped download
        echo    The application will offer to download it automatically on first run.
        echo    Or you can download it manually from:
        echo    https://github.com/yt-dlp/yt-dlp/releases
        echo.
        echo    Recommended location: !STARTUP_DIR!\yt-dlp.exe
        echo                      or: C:\bin\yt-dlp.exe
    )
)
echo.

REM Create Memes folder in Documents
echo [4/4] Setting up download folder...
set "MEMES_FOLDER=%USERPROFILE%\Documents\Memes"
if not exist "%MEMES_FOLDER%" (
    mkdir "%MEMES_FOLDER%" 2>nul
    if exist "%MEMES_FOLDER%" (
        echo    ✓ Created Memes folder in Documents
    ) else (
        echo    ℹ Memes folder will be created automatically
    )
) else (
    echo    ✓ Memes folder already exists
)
echo.

echo [5/5] Installation complete!
echo.
echo ========================================
echo Installation Complete!
echo ========================================
echo.
echo The application will:
echo  • Start automatically when Windows starts
echo  • Monitor clipboard for video URLs
echo  • Download videos to: %USERPROFILE%\Documents\Memes_YYYY-MM-DD\
echo.
echo Supported URLs:
echo  • Twitter/X: https://x.com/i/status/...
echo  • YouTube: https://youtube.com/watch?v=...
echo  • YouTube: https://youtu.be/...
echo.
echo To start now, run: !EXE_PATH!
echo.
echo Log file location: !STARTUP_DIR!\SystemTrayApp_Debug.log
echo.

choice /C YN /M "Would you like to start the application now"
if !errorlevel! equ 1 (
    echo.
    echo Starting SystemTrayApp...
    start "" "!EXE_PATH!"
    echo.
    echo Application started! Check your system tray.
) else (
    echo.
    echo You can start it manually or it will start on next login.
)

echo.
echo Press any key to exit installer...
pause >nul
