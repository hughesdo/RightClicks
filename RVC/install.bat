@echo off
REM ========================================
REM RVC Installation Script
REM ========================================
REM This script sets up RVC (Retrieval-based Voice Conversion)
REM - Installs Python 3.10 if not present
REM - Creates virtual environment
REM - Installs dependencies
REM - Downloads required models (hubert, rmvpe)
REM ========================================

echo.
echo ========================================
echo RVC Setup
echo ========================================
echo.

REM Get the directory where this script is located
set "RVC_DIR=%~dp0"
cd /d "%RVC_DIR%"

REM Check if already set up
if exist "venv\Scripts\python.exe" (
    echo   Checking existing venv...
    "venv\Scripts\python.exe" --version >NUL 2>&1
    if %errorLevel% equ 0 (
        echo   ✓ RVC venv already exists
        goto :check_models
    )
)

REM ========================================
REM Step 1: Find Compatible Python (3.10, 3.11, or 3.12)
REM ========================================
echo [1/4] Checking for compatible Python version...

REM Try to find Python 3.10, 3.11, or 3.12 via py launcher
set PYTHON_VER=
set PYTHON_CMD=

REM Try Python 3.10 first (preferred)
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
    echo   ✓ Found Python %PYTHON_VER%
    goto :create_venv
)

REM No compatible Python found - need to install
echo   No compatible Python found. Installing Python 3.10...
echo.

REM Download Python 3.10 installer (required for RVC compatibility)
echo   Downloading Python 3.10 installer...
powershell -Command "& {[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; Invoke-WebRequest -Uri 'https://www.python.org/ftp/python/3.10.11/python-3.10.11-amd64.exe' -OutFile '%TEMP%\python-3.10.11-amd64.exe' -UseBasicParsing}"

if not exist "%TEMP%\python-3.10.11-amd64.exe" (
    echo   ERROR: Failed to download Python installer.
    echo   Please manually install Python 3.10 from:
    echo     https://www.python.org/downloads/release/python-31011/
    exit /b 1
)

REM Install Python 3.10 for current user (more reliable than system-wide)
echo   Installing Python 3.10 (this may take a minute)...
"%TEMP%\python-3.10.11-amd64.exe" /quiet InstallAllUsers=0 PrependPath=0 Include_pip=1 Include_launcher=1

if %errorLevel% neq 0 (
    echo   ⚠ Python installer returned error %errorLevel%
    echo   Checking if Python is available anyway...
)

REM Verify installation
set PYTHON_CMD=py -3.10
set PYTHON_VER=3.10
for /f "tokens=*" %%i in ('py -3.10 -c "import sys; print(sys.executable)" 2^>NUL') do set PYTHON_FOUND=1
if not defined PYTHON_FOUND (
    echo   ERROR: Python 3.10 not accessible via py launcher.
    echo   Please restart your command prompt and try again.
    exit /b 1
)

echo   ✓ Python 3.10 installed successfully

:create_venv
REM ========================================
REM Step 2: Create Virtual Environment
REM ========================================
echo.
echo [2/4] Creating virtual environment with Python %PYTHON_VER%...

if exist "venv" (
    echo   Removing old venv...
    rmdir /s /q "venv" 2>NUL
)

%PYTHON_CMD% -m venv venv
if %errorLevel% neq 0 (
    echo   ERROR: Failed to create virtual environment.
    exit /b 1
)

echo   ✓ Virtual environment created

REM ========================================
REM Step 3: Install Dependencies
REM ========================================
echo.
echo [3/4] Installing dependencies (this may take 10-15 minutes)...
echo.

REM Upgrade pip first
echo   Upgrading pip...
"venv\Scripts\python.exe" -m pip install --upgrade pip >NUL 2>&1

REM Clear pip cache to avoid corrupted downloads
echo   Clearing pip cache...
"venv\Scripts\pip.exe" cache purge >NUL 2>&1

REM Install PyTorch with CUDA support first (use --no-cache-dir for reliability)
echo   Installing PyTorch with CUDA support (this may take 5-10 minutes)...
"venv\Scripts\pip.exe" install --no-cache-dir torch torchvision torchaudio --index-url https://download.pytorch.org/whl/cu118
if %errorLevel% neq 0 (
    echo   ⚠ PyTorch CUDA install failed, trying CPU version...
    "venv\Scripts\pip.exe" install --no-cache-dir torch torchvision torchaudio
)

REM Install omegaconf and hydra-core first (fairseq dependencies with old metadata)
echo   Installing fairseq dependencies...
"venv\Scripts\pip.exe" install "omegaconf==2.0.6" "hydra-core==1.0.7" --use-deprecated=legacy-resolver 2>NUL
if %errorLevel% neq 0 (
    echo   ⚠ omegaconf install with legacy resolver failed, trying alternative...
    "venv\Scripts\pip.exe" install "omegaconf>=2.1.0" "hydra-core>=1.1.0" 2>NUL
)

REM Install requirements (use inference requirements for minimal install)
echo   Installing RVC requirements...
"venv\Scripts\pip.exe" install -r requirements-inference.txt
if %errorLevel% neq 0 (
    echo   ⚠ Some requirements failed, this is usually OK
)

REM Install requests for model downloading
"venv\Scripts\pip.exe" install requests >NUL 2>&1

echo   ✓ Dependencies installed

:check_models
REM ========================================
REM Step 4: Download Required Models
REM ========================================
echo.
echo [4/4] Checking and downloading models...

REM Create directories if needed
if not exist "assets\hubert" mkdir "assets\hubert"
if not exist "assets\rmvpe" mkdir "assets\rmvpe"

REM Download hubert_base.pt if missing
if not exist "assets\hubert\hubert_base.pt" (
    echo   Downloading hubert_base.pt ~181 MB...
    powershell -Command "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; Invoke-WebRequest -Uri 'https://huggingface.co/lj1995/VoiceConversionWebUI/resolve/main/hubert_base.pt' -OutFile 'assets\hubert\hubert_base.pt' -UseBasicParsing"
)
if exist "assets\hubert\hubert_base.pt" (
    echo   ✓ hubert_base.pt ready
) else (
    echo   ⚠ Failed to download hubert_base.pt
)

REM Download rmvpe.pt if missing
if not exist "assets\rmvpe\rmvpe.pt" (
    echo   Downloading rmvpe.pt ~173 MB...
    powershell -Command "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; Invoke-WebRequest -Uri 'https://huggingface.co/lj1995/VoiceConversionWebUI/resolve/main/rmvpe.pt' -OutFile 'assets\rmvpe\rmvpe.pt' -UseBasicParsing"
)
if exist "assets\rmvpe\rmvpe.pt" (
    echo   ✓ rmvpe.pt ready
) else (
    echo   ⚠ Failed to download rmvpe.pt
)

REM Check for voice model weights
echo.
echo   Checking voice models...
if exist "assets\weights" (
    dir /b "assets\weights\*.pth" 2>NUL | find /c /v "" > "%TEMP%\weight_count.txt"
    set /p WEIGHT_COUNT=<"%TEMP%\weight_count.txt"
    echo   Found voice models in assets\weights\
) else (
    echo   No voice models found in assets\weights\
    set WEIGHT_COUNT=0
)

REM ========================================
REM Complete
REM ========================================
echo.
echo ========================================
echo RVC Setup Complete!
echo ========================================
echo.
echo RVC is ready for voice conversion.
echo Voice models available: %WEIGHT_COUNT%
echo.

