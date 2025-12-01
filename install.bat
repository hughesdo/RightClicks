@echo off
REM ========================================
REM RightClicks Installation Script
REM ========================================
REM This script installs RightClicks to %LOCALAPPDATA%\RightClicks\
REM and registers the Windows Explorer shell extension.
REM
REM REQUIREMENTS:
REM - Must run as Administrator
REM - Requires .NET 8.0 SDK (to build) or prebuilt binaries
REM - Disk space: ~50 MB (core) or ~10 GB (with RVC)
REM
REM WHAT IT DOES:
REM 1. Builds the project (if not already built)
REM 2. Copies RightClicks application files
REM 3. Copies RVC inference engine (if available)
REM 4. Checks environment variables
REM 5. Installs shell extension
REM 6. Restarts Windows Explorer
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

REM Check disk space requirements
if exist "RVC\venv" (
    echo NOTE: RVC folder detected - full installation requires ~10 GB
) else (
    echo NOTE: Core installation requires ~50 MB
    echo       RVC voice conversion is not set up - see README.md
)
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
REM Step 0: Check and Install Prerequisites
REM ========================================
echo.
echo [0/5] Checking prerequisites...

REM Create temp directory for installers
if not exist "%TEMP%\RightClicksInstall" mkdir "%TEMP%\RightClicksInstall"

REM Check if .NET Framework 4.8 is installed (required for RightClicksShellManager)
set NETFX_OK=0
reg query "HKLM\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full" /v Release >NUL 2>&1
if %errorLevel% equ 0 (
    for /f "tokens=3" %%a in ('reg query "HKLM\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full" /v Release 2^>nul') do set NETFX_RELEASE=%%a
    if defined NETFX_RELEASE (
        if %NETFX_RELEASE% GEQ 528040 set NETFX_OK=1
    )
)

if %NETFX_OK%==0 (
    echo   .NET Framework 4.8 not found or outdated. Installing...
    echo.

    REM Download .NET Framework 4.8 web installer
    echo   Downloading .NET Framework 4.8 installer...
    powershell -Command "& {[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; Invoke-WebRequest -Uri 'https://go.microsoft.com/fwlink/?LinkId=2085155' -OutFile '%TEMP%\RightClicksInstall\ndp48-web.exe'}"

    if not exist "%TEMP%\RightClicksInstall\ndp48-web.exe" (
        echo   ERROR: Failed to download .NET Framework 4.8 installer.
        echo   Please manually install from:
        echo     https://dotnet.microsoft.com/download/dotnet-framework/net48
        pause
        exit /b 1
    )

    REM Install .NET Framework 4.8 silently
    echo   Installing .NET Framework 4.8 (this may take several minutes)...
    "%TEMP%\RightClicksInstall\ndp48-web.exe" /q /norestart

    set NETFX_RESULT=%errorLevel%
    if %NETFX_RESULT% equ 3010 (
        echo   ⚠ .NET Framework 4.8 installed - REBOOT REQUIRED
        echo   Please reboot your computer and run this installer again.
        pause
        exit /b 0
    )
    if %NETFX_RESULT% neq 0 (
        echo   ERROR: .NET Framework 4.8 installation failed (error %NETFX_RESULT%).
        echo   Please manually install from:
        echo     https://dotnet.microsoft.com/download/dotnet-framework/net48
        pause
        exit /b 1
    )

    echo   ✓ .NET Framework 4.8 installed successfully
    echo.
) else (
    echo   ✓ .NET Framework 4.8 already installed
)

REM Check if .NET 8.0 SDK is available
where dotnet >NUL 2>&1
if %errorLevel% neq 0 (
    echo   .NET 8.0 SDK not found. Installing...
    echo.

    REM Download .NET 8.0 SDK installer using PowerShell
    echo   Downloading .NET 8.0 SDK installer...
    powershell -Command "& {[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; Invoke-WebRequest -Uri 'https://dot.net/v1/dotnet-install.ps1' -OutFile '%TEMP%\RightClicksInstall\dotnet-install.ps1'}"

    if not exist "%TEMP%\RightClicksInstall\dotnet-install.ps1" (
        echo   ERROR: Failed to download .NET SDK installer script.
        echo   Please manually install .NET 8.0 SDK from:
        echo     https://dotnet.microsoft.com/download/dotnet/8.0
        pause
        exit /b 1
    )

    REM Install .NET 8.0 SDK
    echo   Installing .NET 8.0 SDK (this may take a few minutes)...
    powershell -ExecutionPolicy Bypass -File "%TEMP%\RightClicksInstall\dotnet-install.ps1" -Channel 8.0 -InstallDir "%ProgramFiles%\dotnet"

    if %errorLevel% neq 0 (
        echo   ERROR: .NET SDK installation failed.
        echo   Please manually install .NET 8.0 SDK from:
        echo     https://dotnet.microsoft.com/download/dotnet/8.0
        pause
        exit /b 1
    )

    REM Add dotnet to PATH for this session
    set "PATH=%ProgramFiles%\dotnet;%PATH%"

    REM Verify installation
    where dotnet >NUL 2>&1
    if %errorLevel% neq 0 (
        echo   ERROR: .NET SDK installation completed but 'dotnet' command not found.
        echo   Please restart this installer or manually install from:
        echo     https://dotnet.microsoft.com/download/dotnet/8.0
        pause
        exit /b 1
    )

    echo   ✓ .NET 8.0 SDK installed successfully
    echo.
) else (
    echo   ✓ .NET 8.0 SDK already installed
)

REM ========================================
REM Step 1: Build and Copy RightClicks Application
REM ========================================
echo.
echo [1/5] Building and copying RightClicks application...

REM Check if build is needed
if not exist "RightClicks\bin\Release\net8.0-windows\RightClicks.exe" (
    echo   Building project...

    dotnet build --configuration Release --verbosity minimal
    if %errorLevel% neq 0 (
        echo   ERROR: Build failed. Check the errors above.
        pause
        exit /b 1
    )
    echo   ✓ Build successful
) else (
    echo   Using existing build...
)

REM Verify build output exists
if not exist "RightClicks\bin\Release\net8.0-windows\RightClicks.exe" (
    echo   ERROR: Build output not found at RightClicks\bin\Release\net8.0-windows\
    pause
    exit /b 1
)

echo   Copying application files...
xcopy /Y /E /I /Q "RightClicks\bin\Release\net8.0-windows\*" "%INSTALL_DIR%\" >NUL
if %errorLevel% neq 0 (
    echo   ERROR: Failed to copy RightClicks application files
    pause
    exit /b 1
)

REM Copy RightClicksShellManager (builds to separate location - .NET Framework 4.8)
if exist "RightClicksShellManager\bin\Release\RightClicksShellManager.exe" (
    echo   Copying shell manager...
    copy /Y "RightClicksShellManager\bin\Release\RightClicksShellManager.exe" "%INSTALL_DIR%\" >NUL
    copy /Y "RightClicksShellManager\bin\Release\*.dll" "%INSTALL_DIR%\" >NUL 2>NUL
) else (
    echo   ⚠ RightClicksShellManager not found - shell extension may not install
)

REM Copy RightClicksShellExtension DLL and dependencies
if exist "RightClicksShellExtension\bin\Release\RightClicksShellExtension.dll" (
    echo   Copying shell extension...
    copy /Y "RightClicksShellExtension\bin\Release\RightClicksShellExtension.dll" "%INSTALL_DIR%\" >NUL
    copy /Y "RightClicksShellExtension\bin\Release\Newtonsoft.Json.dll" "%INSTALL_DIR%\" >NUL 2>NUL
    copy /Y "RightClicksShellExtension\SharpShell.dll" "%INSTALL_DIR%\" >NUL 2>NUL
) else (
    echo   ⚠ RightClicksShellExtension.dll not found - context menu may not work
)

echo   ✓ RightClicks application copied

REM ========================================
REM Step 2: Copy RVC Inference Engine
REM ========================================
echo.
echo [2/5] Setting up RVC inference engine...
echo.

REM Check if RVC folder exists
if not exist "RVC" (
    echo   ⚠ RVC folder not found - skipping RVC setup
    echo   RVC voice conversion features will not be available.
    set RVC_INSTALLED=0
    goto :skip_rvc
)

REM Check if RVC venv exists (required for RVC to work)
if not exist "RVC\venv" (
    echo   ⚠ RVC Python environment not found at: RVC\venv
    echo.
    echo   RVC requires a Python 3.10 virtual environment with dependencies.
    echo   To set up RVC manually:
    echo     1. Install Python 3.10
    echo     2. cd RVC
    echo     3. python -m venv venv
    echo     4. venv\Scripts\activate
    echo     5. pip install -r requirements.txt
    echo.
    echo   Skipping RVC installation - voice conversion features will not work.
    set RVC_INSTALLED=0
    goto :skip_rvc
)

REM Check for required RVC models
set RVC_MODELS_MISSING=0
if not exist "RVC\assets\hubert\hubert_base.pt" set RVC_MODELS_MISSING=1
if not exist "RVC\assets\rmvpe\rmvpe.pt" set RVC_MODELS_MISSING=1

if %RVC_MODELS_MISSING%==1 (
    echo   Downloading required RVC models...
    echo.

    REM Try to download models using Python
    if exist "RVC\venv\Scripts\python.exe" (
        echo   Downloading hubert_base.pt ^(~181 MB^)...
        "RVC\venv\Scripts\python.exe" -c "import requests; r=requests.get('https://huggingface.co/lj1995/VoiceConversionWebUI/resolve/main/hubert_base.pt'); open('RVC/assets/hubert/hubert_base.pt','wb').write(r.content)" 2>NUL
        if not exist "RVC\assets\hubert\hubert_base.pt" (
            echo   ⚠ Failed to download hubert_base.pt
        ) else (
            echo   ✓ hubert_base.pt downloaded
        )

        echo   Downloading rmvpe.pt ^(~173 MB^)...
        "RVC\venv\Scripts\python.exe" -c "import requests; r=requests.get('https://huggingface.co/lj1995/VoiceConversionWebUI/resolve/main/rmvpe.pt'); open('RVC/assets/rmvpe/rmvpe.pt','wb').write(r.content)" 2>NUL
        if not exist "RVC\assets\rmvpe\rmvpe.pt" (
            echo   ⚠ Failed to download rmvpe.pt
        ) else (
            echo   ✓ rmvpe.pt downloaded
        )
    ) else (
        echo   ⚠ Cannot download models - Python not available in venv
        echo   Please download manually from:
        echo     https://huggingface.co/lj1995/VoiceConversionWebUI
        echo   And place in RVC\assets\hubert\ and RVC\assets\rmvpe\
    )
)

REM Recheck if models exist after download attempt
set RVC_MODELS_MISSING=0
if not exist "RVC\assets\hubert\hubert_base.pt" set RVC_MODELS_MISSING=1
if not exist "RVC\assets\rmvpe\rmvpe.pt" set RVC_MODELS_MISSING=1

if %RVC_MODELS_MISSING%==1 (
    echo.
    echo   ⚠ Required RVC models are missing. RVC features will not work.
    echo   Continuing with partial installation...
    echo.
)

echo   Copying RVC files (this may take several minutes)...
echo.

REM Copy RVC venv (Python virtual environment)
echo   Copying Python virtual environment...
xcopy /Y /E /I /Q "RVC\venv" "%INSTALL_DIR%\RVC\venv\" >NUL
if %errorLevel% neq 0 (
    echo   ⚠ Failed to copy RVC venv - RVC features will not work
    set RVC_INSTALLED=0
    goto :skip_rvc
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
if exist "RVC\assets\hubert" xcopy /Y /E /I /Q "RVC\assets\hubert" "%INSTALL_DIR%\RVC\assets\hubert\" >NUL
if exist "RVC\assets\rmvpe" xcopy /Y /E /I /Q "RVC\assets\rmvpe" "%INSTALL_DIR%\RVC\assets\rmvpe\" >NUL
if exist "RVC\assets\weights" xcopy /Y /E /I /Q "RVC\assets\weights" "%INSTALL_DIR%\RVC\assets\weights\" >NUL

REM Copy .env if exists
if exist "RVC\.env" copy /Y "RVC\.env" "%INSTALL_DIR%\RVC\.env" >NUL

set RVC_INSTALLED=1
echo   ✓ RVC inference engine copied

:skip_rvc

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

if defined RVC_INSTALLED (
    if %RVC_INSTALLED%==1 (
        echo Features installed:
        echo   ✓ RightClicks core app
        echo   ✓ RVC voice conversion ^(24+ voice models^)
        echo.
    ) else (
        echo Features installed:
        echo   ✓ RightClicks core app
        echo   ⚠ RVC voice conversion NOT installed
        echo.
        echo To enable RVC features, see README.md for setup instructions.
        echo.
    )
) else (
    echo Features installed:
    echo   ✓ RightClicks core app
    echo.
)

echo NEXT STEPS:
echo   1. The RightClicks system tray icon should appear shortly
echo   2. Right-click any supported file in Windows Explorer
echo   3. Look for "RightClicks" in the context menu
echo   4. Select a feature to process your file
echo.
echo TROUBLESHOOTING:
echo   - If features don't appear, restart your computer
echo   - Check logs at: %INSTALL_DIR%\logs\
echo   - See README.md for configuration help
echo.
echo Thank you for using RightClicks!
echo.
pause

