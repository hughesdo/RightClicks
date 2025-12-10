# SystemTrayApp - Build Script
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "SystemTrayApp - Build Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$BuildDir = $PSScriptRoot
Write-Host "Build directory: $BuildDir"
Write-Host ""

# Check if solution file exists
$SolutionPath = Join-Path $BuildDir "SystemTrayApp.sln"
if (-not (Test-Path $SolutionPath)) {
    Write-Host "ERROR: SystemTrayApp.sln not found!" -ForegroundColor Red
    Write-Host "Please run this script from the application directory."
    Write-Host ""
    Read-Host "Press Enter to exit"
    exit 1
}

# Unblock all files first (in case downloaded from internet)
Write-Host "Unblocking files..."
Get-ChildItem -Recurse | Unblock-File -ErrorAction SilentlyContinue
Write-Host "Done" -ForegroundColor Green
Write-Host ""

# Try to find MSBuild
Write-Host "Looking for MSBuild..."

$MSBuildPath = $null

# Check for Visual Studio MSBuild first
$VSMSBuildPaths = @(
    "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe",
    "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe",
    "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
    "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe",
    "C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe",
    "C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
)

foreach ($path in $VSMSBuildPaths) {
    if (Test-Path $path) {
        $MSBuildPath = $path
        Write-Host "Found Visual Studio MSBuild: $MSBuildPath" -ForegroundColor Green
        break
    }
}

# Check for .NET SDK MSBuild
if (-not $MSBuildPath) {
    Write-Host "Visual Studio not found. Checking for .NET SDK..." -ForegroundColor Yellow

    # Try to find dotnet
    $dotnetPath = (Get-Command dotnet -ErrorAction SilentlyContinue).Source

    if ($dotnetPath) {
        # Get SDK path
        $sdkPath = & dotnet --list-sdks | Select-Object -First 1
        if ($sdkPath -match '(\d+\.\d+\.\d+)\s+\[(.+)\]') {
            $sdkVersion = $matches[1]
            $sdkRoot = $matches[2]
            $MSBuildPath = Join-Path $sdkRoot "$sdkVersion\MSBuild.dll"

            if (Test-Path $MSBuildPath) {
                Write-Host "Found .NET SDK MSBuild: $MSBuildPath" -ForegroundColor Green
                # For .NET SDK, we need to use dotnet msbuild instead
                $MSBuildPath = "dotnet"
                $MSBuildArgs = "msbuild"
            } else {
                $MSBuildPath = $null
            }
        }
    }
}

# If still not found, offer to download .NET SDK
if (-not $MSBuildPath) {
    Write-Host ""
    Write-Host "MSBuild not found!" -ForegroundColor Red
    Write-Host ""
    Write-Host "This project needs MSBuild to compile. You have two options:" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Option 1: Install .NET SDK (Recommended - smaller download)"
    Write-Host "  Download from: https://dotnet.microsoft.com/download/dotnet/8.0"
    Write-Host "  Size: ~200 MB"
    Write-Host ""
    Write-Host "Option 2: Install Visual Studio Community (Full IDE)"
    Write-Host "  Download from: https://visualstudio.microsoft.com/downloads/"
    Write-Host "  Size: ~5 GB"
    Write-Host ""

    $choice = Read-Host "Open .NET SDK download page? (Y/N)"
    if ($choice -eq "Y" -or $choice -eq "y") {
        Start-Process "https://dotnet.microsoft.com/download/dotnet/8.0"
    }

    Write-Host ""
    Write-Host "After installing, run this script again." -ForegroundColor Yellow
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Host ""
Write-Host "Building application (this may take a minute)..."
Write-Host ""

# Build the solution
if ($MSBuildPath -eq "dotnet") {
    # Using .NET SDK
    Write-Host "Using .NET SDK MSBuild" -ForegroundColor Green
    & dotnet msbuild "$SolutionPath" /t:Rebuild /p:Configuration=Release /p:Platform=x86 /v:minimal /nologo
} else {
    # Using Visual Studio MSBuild
    Write-Host "Using Visual Studio MSBuild: $MSBuildPath" -ForegroundColor Green
    & "$MSBuildPath" "$SolutionPath" /t:Rebuild /p:Configuration=Release /p:Platform=x86 /v:minimal /nologo
}

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "ERROR: Build failed!" -ForegroundColor Red
    Write-Host "Please check the error messages above."
    Write-Host ""
    Read-Host "Press Enter to exit"
    exit 1
}

# Verify the build output
$ExePath = Join-Path $BuildDir "bin\Release\SystemTrayApp.exe"
if (Test-Path $ExePath) {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "Build Successful!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Application built at: $ExePath"
    Write-Host ""
    Write-Host "You can now run install.bat to install the application."
    Write-Host ""
} else {
    Write-Host ""
    Write-Host "ERROR: Build completed but SystemTrayApp.exe not found!" -ForegroundColor Red
    Write-Host "Expected location: $ExePath"
    Write-Host ""
    Read-Host "Press Enter to exit"
    exit 1
}

Read-Host "Press Enter to exit"

