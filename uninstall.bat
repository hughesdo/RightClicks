@echo off
setlocal enabledelayedexpansion

REM ========================================
REM RightClicks Uninstaller
REM ========================================
REM This script removes RightClicks from your system.
REM Run as Administrator for best results.
REM ========================================

echo.
echo ========================================
echo RightClicks Uninstaller
echo ========================================
echo.

set INSTALL_DIR=%LOCALAPPDATA%\RightClicks

REM Check if RightClicks is installed
if not exist "%INSTALL_DIR%" (
    echo RightClicks is not installed.
    echo   Expected location: %INSTALL_DIR%
    echo.
    pause
    exit /b 0
)

echo This will remove RightClicks from:
echo   %INSTALL_DIR%
echo.
echo Press any key to continue or Ctrl+C to cancel...
pause >NUL

echo.
echo [1/4] Stopping RightClicks...

REM Kill RightClicks.exe if running
taskkill /F /IM RightClicks.exe >NUL 2>&1
if !errorLevel! equ 0 (
    echo   ✓ RightClicks stopped
) else (
    echo   - RightClicks was not running
)

echo.
echo [2/4] Uninstalling shell extension...

REM Uninstall shell extension
if exist "%INSTALL_DIR%\RightClicksShellManager.exe" (
    "%INSTALL_DIR%\RightClicksShellManager.exe" /uninstall
    if !errorLevel! equ 0 (
        echo   ✓ Shell extension uninstalled
    ) else (
        echo   ⚠ Shell extension uninstall may have failed
    )
) else (
    echo   - Shell manager not found, skipping
)

echo.
echo [3/4] Restarting Windows Explorer...

taskkill /F /IM explorer.exe >NUL 2>&1
timeout /t 2 /nobreak >NUL
start explorer.exe
echo   ✓ Windows Explorer restarted

echo.
echo [4/4] Removing files...

REM Wait a moment for Explorer to release any file locks
timeout /t 2 /nobreak >NUL

REM Remove the installation directory
rmdir /S /Q "%INSTALL_DIR%" 2>NUL
if exist "%INSTALL_DIR%" (
    echo   ⚠ Some files could not be removed
    echo   You may need to manually delete: %INSTALL_DIR%
) else (
    echo   ✓ All files removed
)

echo.
echo ========================================
echo Uninstallation Complete!
echo ========================================
echo.
echo RightClicks has been removed from your system.
echo.
echo If you want to reinstall later, run install.bat as Administrator.
echo.
echo Thank you for trying RightClicks!
echo.
pause

