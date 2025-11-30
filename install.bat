@echo off
REM ========================================
REM RightClicks Installation Script
REM ========================================
REM This script installs RightClicks to %LOCALAPPDATA%\RightClicks\
REM and registers the Windows Explorer shell extension.
REM
REM REQUIREMENTS:
REM - Must run as Administrator
REM - Requires ~10 GB disk space
REM - Requires .NET 8.0 Runtime
REM
REM WHAT IT DOES:
REM 1. Copies RightClicks application files
REM 2. Copies RVC inference engine (~10 GB)
REM 3. Checks environment variables
REM 4. Installs shell extension
REM 5. Restarts Windows Explorer
REM ========================================

echo.
echo ========================================
echo RightClicks Installation
echo ========================================
echo.

REM Check if running as Administrator
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo ERROR: This script must be run as Administrator
    echo.
    echo Right-click install.bat and select "Run as administrator"
    echo.
    pause
    exit /b 1
)

REM Set installation directory
set INSTALL_DIR=%LOCALAPPDATA%\RightClicks
echo Installing to: %INSTALL_DIR%
echo.
echo WARNING: This installation requires approximately 10 GB of disk space.
echo.
pause

REM Create installation directory
if not exist "%INSTALL_DIR%" (
    echo Creating installation directory...
    mkdir "%INSTALL_DIR%"
)

REM Check if RightClicks is already running
tasklist /FI "IMAGENAME eq RightClicks.exe" 2>NUL | find /I /N "RightClicks.exe">NUL
if "%ERRORLEVEL%"=="0" (
    echo Stopping RightClicks...
    taskkill /F /IM RightClicks.exe >NUL 2>&1
    timeout /t 2 /nobreak >NUL
)

REM ========================================
REM Step 1: Copy RightClicks Application
REM ========================================
echo.
echo [1/5] Copying RightClicks application...

if not exist "RightClicks\bin\Release\net8.0" (
    echo ERROR: RightClicks application not found. Please build the project first:
    echo   dotnet build --configuration Release
    echo.
    pause
    exit /b 1
)

xcopy /Y /E /I /Q "RightClicks\bin\Release\net8.0\*" "%INSTALL_DIR%\" >NUL
if %errorLevel% neq 0 (
    echo ERROR: Failed to copy RightClicks application files
    pause
    exit /b 1
)
echo   ✓ RightClicks application copied

REM ========================================
REM Step 2: Copy RVC Inference Engine
REM ========================================
echo.
echo [2/5] Copying RVC inference engine...
echo   This may take several minutes (copying ~10 GB)...
echo.

REM Check if RVC folder exists
if not exist "RVC" (
    echo ERROR: RVC folder not found at: %CD%\RVC
    echo   Please ensure the RVC folder is in the repository root.
    pause
    exit /b 1
)

REM Copy RVC venv (Python virtual environment)
echo   Copying Python virtual environment...
xcopy /Y /E /I /Q "RVC\venv" "%INSTALL_DIR%\RVC\venv\" >NUL
if %errorLevel% neq 0 (
    echo ERROR: Failed to copy RVC venv
    pause
    exit /b 1
)

REM Copy RVC configs
echo   Copying RVC configs...
xcopy /Y /E /I /Q "RVC\configs" "%INSTALL_DIR%\RVC\configs\" >NUL

REM Copy RVC infer modules
echo   Copying RVC inference modules...
xcopy /Y /E /I /Q "RVC\infer" "%INSTALL_DIR%\RVC\infer\" >NUL

REM Copy RVC tools
echo   Copying RVC tools...
if not exist "%INSTALL_DIR%\RVC\tools" mkdir "%INSTALL_DIR%\RVC\tools"
copy /Y "RVC\tools\infer_cli.py" "%INSTALL_DIR%\RVC\tools\" >NUL
if exist "RVC\tools\infer_batch_rvc.py" copy /Y "RVC\tools\infer_batch_rvc.py" "%INSTALL_DIR%\RVC\tools\" >NUL

REM Copy RVC assets (models)
echo   Copying RVC models and assets...
xcopy /Y /E /I /Q "RVC\assets\hubert" "%INSTALL_DIR%\RVC\assets\hubert\" >NUL
xcopy /Y /E /I /Q "RVC\assets\rmvpe" "%INSTALL_DIR%\RVC\assets\rmvpe\" >NUL
xcopy /Y /E /I /Q "RVC\assets\weights" "%INSTALL_DIR%\RVC\assets\weights\" >NUL

REM Copy .env if exists
if exist "RVC\.env" copy /Y "RVC\.env" "%INSTALL_DIR%\RVC\.env" >NUL

echo   ✓ RVC inference engine copied

REM ========================================
REM Step 3: Check Environment Variables
REM ========================================
echo.
echo [3/5] Checking environment variables...

set MISSING_VARS=
if not defined FAL_KEY set MISSING_VARS=%MISSING_VARS% FAL_KEY
if not defined CLOUDINARY_API_KEY set MISSING_VARS=%MISSING_VARS% CLOUDINARY_API_KEY
if not defined CLOUDINARY_API_SECRET set MISSING_VARS=%MISSING_VARS% CLOUDINARY_API_SECRET

if not "%MISSING_VARS%"=="" (
    echo   ⚠ WARNING: Missing environment variables:%MISSING_VARS%
    echo   Some features may not work. See README.md for setup instructions.
) else (
    echo   ✓ All required environment variables are set
)

REM ========================================
REM Step 4: Install Shell Extension
REM ========================================
echo.
echo [4/5] Installing Windows Explorer shell extension...

if not exist "%INSTALL_DIR%\RightClicksShellManager.exe" (
    echo ERROR: RightClicksShellManager.exe not found
    echo   Please ensure the shell manager was built correctly.
    pause
    exit /b 1
)

"%INSTALL_DIR%\RightClicksShellManager.exe" /install
if %errorLevel% neq 0 (
    echo ERROR: Failed to install shell extension
    echo   Make sure you are running as Administrator.
    pause
    exit /b 1
)
echo   ✓ Shell extension installed

REM ========================================
REM Step 5: Restart Windows Explorer
REM ========================================
echo.
echo [5/5] Restarting Windows Explorer...

taskkill /F /IM explorer.exe >NUL 2>&1
timeout /t 2 /nobreak >NUL
start explorer.exe
echo   ✓ Windows Explorer restarted

REM ========================================
REM Installation Complete
REM ========================================
echo.
echo ========================================
echo Installation Complete!
echo ========================================
echo.
echo RightClicks has been installed to:
echo   %INSTALL_DIR%
echo.
echo Installation size: ~10 GB
echo   - RightClicks app: ~50 MB
echo   - RVC inference engine: ~10 GB
echo.
echo NEXT STEPS:
echo   1. The RightClicks system tray icon should appear shortly
echo   2. Right-click any supported file in Windows Explorer
echo   3. Look for "RightClicks" in the context menu
echo   4. Select a feature to process your file
echo.
echo TROUBLESHOOTING:
echo   - If features don't appear, restart your computer
echo   - Check logs at: %INSTALL_DIR%\logs\
echo   - See README.md for environment variable setup
echo.
echo Thank you for using RightClicks!
echo.
pause

