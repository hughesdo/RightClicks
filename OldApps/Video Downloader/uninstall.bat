@echo off
setlocal enabledelayedexpansion

echo ========================================
echo SystemTrayApp - Video Downloader
echo Uninstallation Script
echo ========================================
echo.

REM Kill the application if running
echo [1/4] Stopping application...
taskkill /F /IM SystemTrayApp.exe >nul 2>&1
if !errorlevel! equ 0 (
    echo    ✓ Application stopped
    timeout /t 2 /nobreak >nul
) else (
    echo    ℹ Application was not running
)
echo.

REM Remove startup shortcut
echo [2/4] Removing startup shortcut...
set "STARTUP_FOLDER=%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup"
set "SHORTCUT_PATH=%STARTUP_FOLDER%\SystemTrayApp.lnk"

if exist "%SHORTCUT_PATH%" (
    del "%SHORTCUT_PATH%"
    echo    ✓ Startup shortcut removed
) else (
    echo    ℹ Startup shortcut not found
)
echo.

REM Ask about log file
echo [3/4] Cleaning up log files...
set "INSTALL_DIR=%~dp0"
set "INSTALL_DIR=%INSTALL_DIR:~0,-1%"

if exist "%INSTALL_DIR%\SystemTrayApp_Debug.log" (
    choice /C YN /M "Delete log file (SystemTrayApp_Debug.log)"
    if !errorlevel! equ 1 (
        del "%INSTALL_DIR%\SystemTrayApp_Debug.log"
        echo    ✓ Log file deleted
    ) else (
        echo    ℹ Log file kept
    )
) else if exist "%INSTALL_DIR%\bin\Release\SystemTrayApp_Debug.log" (
    choice /C YN /M "Delete log file (SystemTrayApp_Debug.log)"
    if !errorlevel! equ 1 (
        del "%INSTALL_DIR%\bin\Release\SystemTrayApp_Debug.log"
        echo    ✓ Log file deleted
    ) else (
        echo    ℹ Log file kept
    )
) else (
    echo    ℹ No log file found
)
echo.

REM Ask about downloaded videos
echo [4/4] Downloaded videos...
set "MEMES_FOLDER=%USERPROFILE%\Documents"
echo.
echo Your downloaded videos are in: %MEMES_FOLDER%\Memes_*
echo.
choice /C YN /M "Do you want to keep your downloaded videos"
if !errorlevel! equ 2 (
    echo.
    echo WARNING: This will delete ALL Memes_* folders in Documents!
    choice /C YN /M "Are you absolutely sure"
    if !errorlevel! equ 1 (
        for /d %%D in ("%MEMES_FOLDER%\Memes_*") do (
            echo    Deleting: %%D
            rd /s /q "%%D"
        )
        echo    ✓ Downloaded videos deleted
    ) else (
        echo    ℹ Videos kept
    )
) else (
    echo    ℹ Videos kept
)
echo.

echo ========================================
echo Uninstallation Complete!
echo ========================================
echo.
echo The application has been removed from startup.
echo.
echo Note: The application files are still in:
echo %INSTALL_DIR%
echo.
echo You can manually delete this folder if desired.
echo.
echo Press any key to exit...
pause >nul

