# Test script for RightClicksClipEditor - Audio Editor
# Launches the audio editor with a test file

$ErrorActionPreference = "Stop"

Write-Host "=== RightClicksClipEditor - Audio Editor Test ===" -ForegroundColor Cyan
Write-Host ""

# Build the project
Write-Host "Building RightClicksClipEditor..." -ForegroundColor Yellow
dotnet build RightClicksClipEditor/RightClicksClipEditor.csproj --verbosity minimal

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "Build succeeded!" -ForegroundColor Green
Write-Host ""

# Find the executable
$exePath = "RightClicksClipEditor\bin\Debug\net8.0-windows\RightClicksClipEditor.exe"

if (-not (Test-Path $exePath)) {
    Write-Host "Executable not found: $exePath" -ForegroundColor Red
    exit 1
}

# Find test audio file
$testFile = "testfiles\Needs Beavis Voice change.mp3"

if (-not (Test-Path $testFile)) {
    Write-Host "Test file not found: $testFile" -ForegroundColor Red
    Write-Host "Please provide a test audio file." -ForegroundColor Yellow
    exit 1
}

$testFileFullPath = (Resolve-Path $testFile).Path

Write-Host "Launching Audio Editor..." -ForegroundColor Yellow
Write-Host "Test file: $testFileFullPath" -ForegroundColor Cyan
Write-Host ""
Write-Host "KEYBOARD SHORTCUTS:" -ForegroundColor Green
Write-Host "  Spacebar       - Play/Pause" -ForegroundColor White
Write-Host "  I              - Set IN point" -ForegroundColor White
Write-Host "  O              - Set OUT point" -ForegroundColor White
Write-Host "  L              - Toggle loop selection" -ForegroundColor White
Write-Host "  Left/Right     - Step 10ms" -ForegroundColor White
Write-Host "  Shift+Left/Right - Step 1 second" -ForegroundColor White
Write-Host "  Ctrl+A         - Add current selection to clip list" -ForegroundColor White
Write-Host "  Ctrl+S         - Save all clips" -ForegroundColor White
Write-Host "  Ctrl+W         - Close window" -ForegroundColor White
Write-Host "  F1             - Show help" -ForegroundColor White
Write-Host ""
Write-Host "WAVEFORM CONTROLS:" -ForegroundColor Green
Write-Host "  Ctrl+Mouse Wheel - Zoom in/out" -ForegroundColor White
Write-Host "  Shift+Mouse Wheel - Scroll horizontally" -ForegroundColor White
Write-Host ""

# Launch the editor
& $exePath $testFileFullPath

Write-Host ""
Write-Host "Audio Editor closed." -ForegroundColor Yellow

