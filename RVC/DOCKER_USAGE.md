# RVC Docker Container - Usage Guide

## Overview
This Docker container provides a complete RVC (Retrieval-based Voice Conversion) inference environment with:
- ✅ Full CLI support for voice conversion
- ✅ Gradio web interface
- ✅ GPU support (NVIDIA CUDA) with automatic CPU fallback
- ✅ All dependencies pre-installed
- ✅ Python 3.10 environment

## Quick Start

### 1. Build the Docker Image
```bash
docker build -t rvc-inference:latest .
```

### 2. Run CLI Inference

**Basic usage:**
```bash
docker run --rm \
  rvc-inference:latest \
  python tools/infer_cli.py \
  --f0up_key 0 \
  --input_path "assets/TestAudios/Brandon saying.mp3" \
  --index_path "assets/weights/Brandon/Brandon.index" \
  --f0method rmvpe \
  --opt_path "output/test_output.wav" \
  --model_name "Brandon/Brandon.pth" \
  --index_rate 0.66 \
  --device cpu
```

**To extract the output file:**
```bash
# Run with a container name
docker run --name rvc-job rvc-inference:latest python tools/infer_cli.py [args...]

# Copy output back to host
docker cp rvc-job:/app/output/test_output.wav ./output/

# Clean up
docker rm rvc-job
```

### 3. Run Web Interface
```bash
docker run -p 7865:7865 rvc-inference:latest python infer-web.py
```
Then open http://localhost:7865 in your browser.

## CLI Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `--f0up_key` | Pitch shift in semitones | 0 |
| `--input_path` | Input audio file path | Required |
| `--index_path` | Index file for voice retrieval | Optional |
| `--f0method` | Pitch extraction method (harvest, pm, crepe, rmvpe) | rmvpe |
| `--opt_path` | Output file path | Required |
| `--model_name` | Model file (relative to assets/weights) | Required |
| `--index_rate` | Index influence rate (0.0-1.0) | 0.66 |
| `--device` | Device (cpu, cuda:0, etc.) | cpu |
| `--is_half` | Use FP16 precision | True |
| `--filter_radius` | Median filtering radius | 3 |
| `--resample_sr` | Resample output sample rate (0=no resample) | 0 |
| `--rms_mix_rate` | Volume envelope mix rate | 1 |
| `--protect` | Protect voiceless consonants (0.0-0.5) | 0.33 |

## Working with Your Own Models

### Option 1: Copy Files into Container
```bash
# Start a container
docker run -d --name rvc-work rvc-inference:latest tail -f /dev/null

# Copy your model
docker cp ./my_model.pth rvc-work:/app/assets/weights/MyModel/
docker cp ./my_model.index rvc-work:/app/assets/weights/MyModel/

# Copy your audio
docker cp ./my_audio.mp3 rvc-work:/app/input/

# Run inference
docker exec rvc-work python tools/infer_cli.py \
  --input_path "input/my_audio.mp3" \
  --model_name "MyModel/my_model.pth" \
  --index_path "assets/weights/MyModel/my_model.index" \
  --opt_path "output/result.wav" \
  --f0method rmvpe

# Copy output back
docker cp rvc-work:/app/output/result.wav ./

# Clean up
docker rm -f rvc-work
```

### Option 2: Build Image with Your Models
Add your models to the `assets/weights/` directory before building, and they'll be included in the image.

## GPU Support

### Enable GPU (NVIDIA only)
```bash
docker run --gpus all \
  rvc-inference:latest \
  python tools/infer_cli.py --device cuda:0 [other args...]
```

### Check GPU Detection
```bash
docker run --gpus all rvc-inference:latest python -c "import torch; print(f'CUDA available: {torch.cuda.is_available()}')"
```

## Troubleshooting

### Volume Mount Issues on Windows
Docker Desktop on Windows may have issues with non-C: drives. Use the copy method (Option 1 above) instead of volume mounts.

### Out of Memory
- Use `--device cpu` for CPU inference
- Reduce audio file length
- Use `--is_half False` to disable FP16

### Model Not Found
- Ensure model path is relative to `assets/weights/`
- Example: For `assets/weights/Brandon/Brandon.pth`, use `--model_name "Brandon/Brandon.pth"`

## Built-in Test Assets
The image includes test assets:
- **Model**: Brandon voice model at `assets/weights/Brandon/Brandon.pth`
- **Audio**: Test audio at `assets/TestAudios/Brandon saying.mp3`
- **Hubert**: Base model at `assets/hubert/hubert_base.pt`
- **RMVPE**: Pitch extraction model at `assets/rmvpe/rmvpe.pt`

## Next Steps
- See `CLAUDE.md` for project development status
- See `README.md` for general RVC information
- For training (not included in this inference-only build), see the original RVC documentation

