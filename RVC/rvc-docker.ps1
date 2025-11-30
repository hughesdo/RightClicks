# RVC Docker Helper Script for Windows
# Usage: .\rvc-docker.ps1 <command> [args...]

param(
    [Parameter(Position=0)]
    [string]$Command = "help",
    
    [Parameter(Position=1, ValueFromRemainingArguments=$true)]
    [string[]]$Args
)

$IMAGE_NAME = "rvc-inference:latest"
$CONTAINER_NAME = "rvc-work"

function Show-Help {
    Write-Host @"
RVC Docker Helper Script
========================

Commands:
  build                 Build the Docker image
  infer                 Run voice conversion inference
  web                   Start web interface on http://localhost:7865
  shell                 Open interactive shell in container
  copy-in <file>        Copy file into container
  copy-out <file>       Copy file from container to host
  clean                 Remove stopped containers
  help                  Show this help message

Examples:
  # Build image
  .\rvc-docker.ps1 build

  # Run inference with built-in test model
  .\rvc-docker.ps1 infer `
    --input_path "assets/TestAudios/Brandon saying.mp3" `
    --model_name "Brandon/Brandon.pth" `
    --index_path "assets/weights/Brandon/Brandon.index" `
    --opt_path "output/result.wav" `
    --f0method rmvpe

  # Start web interface
  .\rvc-docker.ps1 web

  # Copy your audio file into container
  .\rvc-docker.ps1 copy-in "C:\my_audio.mp3"

  # Copy output from container
  .\rvc-docker.ps1 copy-out "output/result.wav"

For more details, see DOCKER_USAGE.md
"@
}

function Build-Image {
    Write-Host "Building Docker image..." -ForegroundColor Cyan
    docker build -t $IMAGE_NAME .
}

function Run-Inference {
    Write-Host "Running inference..." -ForegroundColor Cyan
    
    # Create container name with timestamp
    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $jobName = "rvc-job-$timestamp"
    
    # Run inference
    docker run --name $jobName $IMAGE_NAME python tools/infer_cli.py $Args
    
    # Check if output directory exists in container
    $outputExists = docker exec $jobName test -d /app/output 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Copying output files..." -ForegroundColor Cyan
        New-Item -ItemType Directory -Force -Path ".\output" | Out-Null
        docker cp "${jobName}:/app/output/." ".\output\"
        Write-Host "Output files copied to .\output\" -ForegroundColor Green
    }
    
    # Clean up
    docker rm $jobName | Out-Null
    Write-Host "Job complete!" -ForegroundColor Green
}

function Start-Web {
    Write-Host "Starting web interface..." -ForegroundColor Cyan
    Write-Host "Access at: http://localhost:7865" -ForegroundColor Green
    Write-Host "Press Ctrl+C to stop" -ForegroundColor Yellow
    docker run --rm -p 7865:7865 $IMAGE_NAME python infer-web.py
}

function Open-Shell {
    Write-Host "Opening shell in container..." -ForegroundColor Cyan
    docker run --rm -it $IMAGE_NAME /bin/bash
}

function Copy-In {
    param([string]$FilePath)
    
    if (-not $FilePath) {
        Write-Host "Error: Please specify a file path" -ForegroundColor Red
        return
    }
    
    # Ensure container is running
    $running = docker ps -q -f name=$CONTAINER_NAME
    if (-not $running) {
        Write-Host "Starting work container..." -ForegroundColor Cyan
        docker run -d --name $CONTAINER_NAME $IMAGE_NAME tail -f /dev/null | Out-Null
    }
    
    $fileName = Split-Path -Leaf $FilePath
    Write-Host "Copying $fileName to container..." -ForegroundColor Cyan
    docker cp $FilePath "${CONTAINER_NAME}:/app/input/$fileName"
    Write-Host "File copied to: /app/input/$fileName" -ForegroundColor Green
}

function Copy-Out {
    param([string]$FilePath)
    
    if (-not $FilePath) {
        Write-Host "Error: Please specify a file path" -ForegroundColor Red
        return
    }
    
    $fileName = Split-Path -Leaf $FilePath
    Write-Host "Copying $fileName from container..." -ForegroundColor Cyan
    docker cp "${CONTAINER_NAME}:/app/$FilePath" ".\$fileName"
    Write-Host "File copied to: .\$fileName" -ForegroundColor Green
}

function Clean-Containers {
    Write-Host "Cleaning up stopped containers..." -ForegroundColor Cyan
    docker container prune -f
    Write-Host "Cleanup complete!" -ForegroundColor Green
}

# Main command dispatcher
switch ($Command.ToLower()) {
    "build" { Build-Image }
    "infer" { Run-Inference }
    "web" { Start-Web }
    "shell" { Open-Shell }
    "copy-in" { Copy-In $Args[0] }
    "copy-out" { Copy-Out $Args[0] }
    "clean" { Clean-Containers }
    "help" { Show-Help }
    default {
        Write-Host "Unknown command: $Command" -ForegroundColor Red
        Write-Host ""
        Show-Help
    }
}

