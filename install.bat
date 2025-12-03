@echo off
setlocal enabledelayedexpansion
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

REM Setup logging
set LOG_FILE=%~dp0install.log
echo. > "%LOG_FILE%"
call :log "=========================================="
call :log "RightClicks Installation Log"
call :log "Date: %DATE% %TIME%"
call :log "=========================================="

echo.
echo ========================================
echo RightClicks Installation
echo ========================================
echo.

REM Check if running as Administrator
call :log "Checking administrator privileges..."
net session >nul 2>&1
if %errorLevel% neq 0 (
    call :log "ERROR: Not running as Administrator"
    echo ERROR: This script must be run as Administrator
    echo.
    echo Right-click install.bat and select "Run as administrator"
    echo.
    pause
    exit /b 1
)
call :log "OK: Running as Administrator"

REM Set installation directory
set INSTALL_DIR=%LOCALAPPDATA%\RightClicks
call :log "Installation directory: %INSTALL_DIR%"
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
call :log "Step 0: Checking prerequisites..."

REM Create temp directory for installers
call :log "Creating temp directory: %TEMP%\RightClicksInstall"
if not exist "%TEMP%\RightClicksInstall" mkdir "%TEMP%\RightClicksInstall"

REM Check if .NET Framework 4.8 is installed (required for RightClicksShellManager)
call :log "Checking .NET Framework 4.8..."
set NETFX_OK=0
set NETFX_RELEASE=0

REM Use PowerShell to get the decimal value directly (avoids hex comparison issues)
for /f "tokens=*" %%a in ('powershell -Command "(Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full' -ErrorAction SilentlyContinue).Release" 2^>nul') do set NETFX_RELEASE=%%a
call :log ".NET Framework Release value: !NETFX_RELEASE!"

if defined NETFX_RELEASE (
    if !NETFX_RELEASE! GEQ 528040 set NETFX_OK=1
)
call :log ".NET Framework OK: !NETFX_OK!"

REM Use goto to avoid nested if/else issues with delayed expansion
if "!NETFX_OK!"=="1" goto :netfx_ok

call :log "NETFX_OK is 0, need to install .NET Framework"
echo   .NET Framework 4.8 not found or outdated. Installing...
echo.

REM Download .NET Framework 4.8 web installer
echo   Downloading .NET Framework 4.8 installer...
powershell -Command "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; Invoke-WebRequest -Uri 'https://go.microsoft.com/fwlink/?LinkId=2085155' -OutFile '%TEMP%\RightClicksInstall\ndp48-web.exe'"

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
set NETFX_RESULT=!errorLevel!

if "!NETFX_RESULT!"=="3010" (
    echo   ⚠ .NET Framework 4.8 installed - REBOOT REQUIRED
    echo   Please reboot your computer and run this installer again.
    pause
    exit /b 0
)
if not "!NETFX_RESULT!"=="0" (
    echo   ERROR: .NET Framework 4.8 installation failed - error !NETFX_RESULT!
    echo   Please manually install from:
    echo     https://dotnet.microsoft.com/download/dotnet-framework/net48
    pause
    exit /b 1
)

echo   ✓ .NET Framework 4.8 installed successfully
echo.
goto :netfx_done

:netfx_ok
call :log ".NET Framework 4.8 already installed, skipping"
echo   ✓ .NET Framework 4.8 already installed

:netfx_done

call :log "Checking .NET 8.0 SDK..."
REM Check if .NET 8.0 SDK is available
where dotnet >NUL 2>&1
set DOTNET_CHECK=!errorLevel!
call :log ".NET SDK check result: !DOTNET_CHECK!"
if "!DOTNET_CHECK!"=="0" goto :dotnetsdk_ok

call :log ".NET 8.0 SDK not found, need to install"
echo   .NET 8.0 SDK not found. Installing...
echo.

REM Download .NET 8.0 SDK installer using PowerShell
echo   Downloading .NET 8.0 SDK installer...
powershell -Command "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; Invoke-WebRequest -Uri 'https://dot.net/v1/dotnet-install.ps1' -OutFile '%TEMP%\RightClicksInstall\dotnet-install.ps1'"

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
set SDK_RESULT=!errorLevel!

if not "!SDK_RESULT!"=="0" (
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
set VERIFY_RESULT=!errorLevel!
if not "!VERIFY_RESULT!"=="0" (
    echo   ERROR: .NET SDK installation completed but 'dotnet' command not found.
    echo   Please restart this installer or manually install from:
    echo     https://dotnet.microsoft.com/download/dotnet/8.0
    pause
    exit /b 1
)

echo   ✓ .NET 8.0 SDK installed successfully
echo.
goto :dotnetsdk_done

:dotnetsdk_ok
echo   ✓ .NET 8.0 SDK already installed

:dotnetsdk_done

REM ========================================
REM Step 1: Build and Copy RightClicks Application
REM ========================================
echo.
echo [1/5] Building and copying RightClicks application...

REM Check if build is needed
if not exist "RightClicks\bin\Release\net8.0-windows\RightClicks.exe" (
    echo   Building project...

    dotnet build --configuration Release --verbosity minimal
    if !errorLevel! neq 0 (
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
if !errorLevel! neq 0 (
    echo   ERROR: Failed to copy RightClicks application files
    pause
    exit /b 1
)

REM Copy RightClicksShellManager (from RightClicksShellInstaller project - outputs as RightClicksShellManager.exe)
if exist "RightClicksShellInstaller\bin\Release\net8.0\RightClicksShellManager.exe" (
    echo   Copying shell manager...
    copy /Y "RightClicksShellInstaller\bin\Release\net8.0\RightClicksShellManager.exe" "%INSTALL_DIR%\" >NUL
    copy /Y "RightClicksShellInstaller\bin\Release\net8.0\*.dll" "%INSTALL_DIR%\" >NUL 2>NUL
    copy /Y "RightClicksShellInstaller\bin\Release\net8.0\*.json" "%INSTALL_DIR%\" >NUL 2>NUL
) else (
    echo   ⚠ RightClicksShellManager not found in RightClicksShellInstaller - shell extension may not install
    echo   Looking for: RightClicksShellInstaller\bin\Release\net8.0\RightClicksShellManager.exe
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
REM Step 2: Setup RVC Inference Engine
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

REM Check for pre-built fairseq (required - cannot be pip installed without Visual C++ Build Tools)
if not exist "RVC\venv\Lib\site-packages\fairseq" (
    echo   ⚠ Pre-built fairseq not found in repo - RVC will not work
    echo   The repo should contain RVC\venv\Lib\site-packages\fairseq\
    set RVC_INSTALLED=0
    goto :skip_rvc
)

REM ----------------------------------------
REM Step 2a: Find or Install Python 3.10+
REM ----------------------------------------
echo   Checking for Python 3.10+...

set PYTHON_CMD=
set PYTHON_VER=

REM Try Python 3.10 first (preferred for RVC)
for /f "tokens=*" %%i in ('py -3.10 -c "import sys; print(sys.executable)" 2^>NUL') do (
    set PYTHON_CMD=py -3.10
    set PYTHON_VER=3.10
)

REM Try Python 3.11 if 3.10 not found
if not defined PYTHON_VER (
    for /f "tokens=*" %%i in ('py -3.11 -c "import sys; print(sys.executable)" 2^>NUL') do (
        set PYTHON_CMD=py -3.11
        set PYTHON_VER=3.11
    )
)

REM Try Python 3.12 if others not found
if not defined PYTHON_VER (
    for /f "tokens=*" %%i in ('py -3.12 -c "import sys; print(sys.executable)" 2^>NUL') do (
        set PYTHON_CMD=py -3.12
        set PYTHON_VER=3.12
    )
)

if defined PYTHON_VER (
    echo   ✓ Found Python !PYTHON_VER!
    goto :python_found
)

REM No compatible Python found - need to install
echo   Python 3.10+ not found. Installing Python 3.10...
echo.

REM Download Python 3.10 installer
echo   Downloading Python 3.10 installer...
powershell -Command "& {[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; Invoke-WebRequest -Uri 'https://www.python.org/ftp/python/3.10.11/python-3.10.11-amd64.exe' -OutFile '%TEMP%\RightClicksInstall\python-3.10.11-amd64.exe' -UseBasicParsing}"

if not exist "%TEMP%\RightClicksInstall\python-3.10.11-amd64.exe" (
    echo   ERROR: Failed to download Python installer.
    echo   Please manually install Python 3.10 from:
    echo     https://www.python.org/downloads/release/python-31011/
    set RVC_INSTALLED=0
    goto :skip_rvc
)

REM Install Python 3.10 for current user
echo   Installing Python 3.10 (this may take a minute)...
"%TEMP%\RightClicksInstall\python-3.10.11-amd64.exe" /quiet InstallAllUsers=0 PrependPath=0 Include_pip=1 Include_launcher=1

REM Verify installation
set PYTHON_CMD=py -3.10
set PYTHON_VER=3.10
for /f "tokens=*" %%i in ('py -3.10 -c "import sys; print(sys.executable)" 2^>NUL') do set PYTHON_FOUND=1
if not defined PYTHON_FOUND (
    echo   ERROR: Python 3.10 not accessible via py launcher.
    echo   Please restart your computer and run this installer again.
    set RVC_INSTALLED=0
    goto :skip_rvc
)
echo   ✓ Python 3.10 installed

:python_found

REM ----------------------------------------
REM Step 2b: Create Virtual Environment
REM ----------------------------------------
echo   Creating Python virtual environment...

if not exist "%INSTALL_DIR%\RVC" mkdir "%INSTALL_DIR%\RVC"

REM Remove old venv if exists
if exist "%INSTALL_DIR%\RVC\venv" (
    echo   Removing old venv...
    rmdir /s /q "%INSTALL_DIR%\RVC\venv" 2>NUL
)

!PYTHON_CMD! -m venv "%INSTALL_DIR%\RVC\venv"
if !errorLevel! neq 0 (
    echo   ERROR: Failed to create virtual environment.
    set RVC_INSTALLED=0
    goto :skip_rvc
)
echo   ✓ Virtual environment created

REM ----------------------------------------
REM Step 2c: Install Dependencies
REM ----------------------------------------
echo   Installing dependencies (this may take 10-15 minutes)...
echo.

REM Upgrade pip first
echo   Upgrading pip...
"%INSTALL_DIR%\RVC\venv\Scripts\python.exe" -m pip install --upgrade pip >NUL 2>&1

REM Install PyTorch with CUDA support
echo   Installing PyTorch with CUDA support (this may take 5-10 minutes)...
"%INSTALL_DIR%\RVC\venv\Scripts\pip.exe" install --no-cache-dir torch torchvision torchaudio --index-url https://download.pytorch.org/whl/cu118
if !errorLevel! neq 0 (
    echo   ⚠ PyTorch CUDA install failed, trying CPU version...
    "%INSTALL_DIR%\RVC\venv\Scripts\pip.exe" install --no-cache-dir torch torchvision torchaudio
)

REM Install omegaconf and hydra-core (fairseq dependencies - specific versions required)
echo   Installing fairseq dependencies...
"%INSTALL_DIR%\RVC\venv\Scripts\pip.exe" install "omegaconf==2.0.6" "hydra-core==1.0.7" bitarray cython regex sacrebleu --use-deprecated=legacy-resolver 2>NUL
if !errorLevel! neq 0 (
    "%INSTALL_DIR%\RVC\venv\Scripts\pip.exe" install "omegaconf<2.1" "hydra-core<1.1,>=1.0.7" bitarray cython regex sacrebleu 2>NUL
)

REM Install other RVC requirements
echo   Installing RVC requirements...
"%INSTALL_DIR%\RVC\venv\Scripts\pip.exe" install python-dotenv scipy soundfile librosa audioread ffmpeg-python av faiss-cpu praat-parselmouth pyworld torchcrepe numpy numba requests
if !errorLevel! neq 0 (
    echo   ⚠ Some packages failed, continuing...
)

REM Install additional requirements from file if exists
if exist "RVC\requirements-inference.txt" (
    echo   Installing from requirements-inference.txt...
    "%INSTALL_DIR%\RVC\venv\Scripts\pip.exe" install -r "RVC\requirements-inference.txt" 2>NUL
)

echo   ✓ Dependencies installed

REM ----------------------------------------
REM Step 2d: Copy Pre-built Packages (fairseq)
REM ----------------------------------------
echo   Copying pre-built fairseq package...

if not exist "%INSTALL_DIR%\RVC\venv\Lib\site-packages" mkdir "%INSTALL_DIR%\RVC\venv\Lib\site-packages"

xcopy /Y /E /I /Q "RVC\venv\Lib\site-packages\fairseq" "%INSTALL_DIR%\RVC\venv\Lib\site-packages\fairseq\" >NUL
if !errorLevel! neq 0 (
    echo   ⚠ Failed to copy fairseq - RVC may not work
)

if exist "RVC\venv\Lib\site-packages\fairseq-0.12.2.dist-info" (
    xcopy /Y /E /I /Q "RVC\venv\Lib\site-packages\fairseq-0.12.2.dist-info" "%INSTALL_DIR%\RVC\venv\Lib\site-packages\fairseq-0.12.2.dist-info\" >NUL
)
echo   ✓ Pre-built packages copied

REM ----------------------------------------
REM Step 2e: Copy RVC Code and Models
REM ----------------------------------------
echo   Copying RVC code and models...

REM Copy RVC configs
if exist "RVC\configs" xcopy /Y /E /I /Q "RVC\configs" "%INSTALL_DIR%\RVC\configs\" >NUL

REM Copy RVC infer modules
if exist "RVC\infer" xcopy /Y /E /I /Q "RVC\infer" "%INSTALL_DIR%\RVC\infer\" >NUL

REM Copy RVC tools
if not exist "%INSTALL_DIR%\RVC\tools" mkdir "%INSTALL_DIR%\RVC\tools"
if exist "RVC\tools\infer_cli.py" copy /Y "RVC\tools\infer_cli.py" "%INSTALL_DIR%\RVC\tools\" >NUL
if exist "RVC\tools\infer_batch_rvc.py" copy /Y "RVC\tools\infer_batch_rvc.py" "%INSTALL_DIR%\RVC\tools\" >NUL

REM Copy RVC assets (models) - download if missing
echo   Setting up RVC models...
if not exist "%INSTALL_DIR%\RVC\assets\hubert" mkdir "%INSTALL_DIR%\RVC\assets\hubert"
if not exist "%INSTALL_DIR%\RVC\assets\rmvpe" mkdir "%INSTALL_DIR%\RVC\assets\rmvpe"
if not exist "%INSTALL_DIR%\RVC\assets\weights" mkdir "%INSTALL_DIR%\RVC\assets\weights"

REM Copy or download hubert model
if exist "RVC\assets\hubert\hubert_base.pt" (
    copy /Y "RVC\assets\hubert\hubert_base.pt" "%INSTALL_DIR%\RVC\assets\hubert\" >NUL
) else (
    if not exist "%INSTALL_DIR%\RVC\assets\hubert\hubert_base.pt" (
        echo   Downloading hubert_base.pt ~181 MB...
        powershell -Command "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; Invoke-WebRequest -Uri 'https://huggingface.co/lj1995/VoiceConversionWebUI/resolve/main/hubert_base.pt' -OutFile '%INSTALL_DIR%\RVC\assets\hubert\hubert_base.pt' -UseBasicParsing"
    )
)

REM Copy or download rmvpe model
if exist "RVC\assets\rmvpe\rmvpe.pt" (
    copy /Y "RVC\assets\rmvpe\rmvpe.pt" "%INSTALL_DIR%\RVC\assets\rmvpe\" >NUL
) else (
    if not exist "%INSTALL_DIR%\RVC\assets\rmvpe\rmvpe.pt" (
        echo   Downloading rmvpe.pt ~173 MB...
        powershell -Command "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; Invoke-WebRequest -Uri 'https://huggingface.co/lj1995/VoiceConversionWebUI/resolve/main/rmvpe.pt' -OutFile '%INSTALL_DIR%\RVC\assets\rmvpe\rmvpe.pt' -UseBasicParsing"
    )
)

REM Copy voice model weights if available
if exist "RVC\assets\weights" xcopy /Y /E /I /Q "RVC\assets\weights" "%INSTALL_DIR%\RVC\assets\weights\" >NUL

REM Copy .env if exists
if exist "RVC\.env" copy /Y "RVC\.env" "%INSTALL_DIR%\RVC\.env" >NUL

set RVC_INSTALLED=1
echo   ✓ RVC inference engine installed

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
if !errorLevel! neq 0 (
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
    if !RVC_INSTALLED!==1 (
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
echo   1. RightClicks is starting now...
echo   2. Right-click any supported file in Windows Explorer
echo   3. Look for "RightClicks" in the context menu
echo   4. Select a feature to process your file
echo.
echo TROUBLESHOOTING:
echo   - If features don't appear, restart your computer
echo   - Check logs at: %INSTALL_DIR%\logs\
echo   - Installation log: %LOG_FILE%
echo   - See README.md for configuration help
echo.
echo Thank you for using RightClicks!
echo.
call :log "Installation completed successfully"

REM Launch RightClicks application
echo Starting RightClicks...
start "" "%INSTALL_DIR%\RightClicks.exe"
call :log "RightClicks.exe launched"

pause
goto :eof

REM ========================================
REM Logging subroutine
REM ========================================
:log
echo [%DATE% %TIME%] %~1 >> "%LOG_FILE%"
goto :eof

