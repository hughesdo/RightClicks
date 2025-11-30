# RVC Inference-Only Project - Development Guide

## Project Goals

### 1. Dual Operation Modes
- **Web Interface**: Maintain existing Gradio-based web UI for inference
- **Command-Line Interface**: Full CLI capability with all inference parameters accessible via arguments

### 2. Docker Containerization
- Complete isolation from system dependencies
- Stable, reproducible environment
- Easy deployment and portability
- **GPU/CPU Auto-detection**: Container will use GPU if available (NVIDIA CUDA), fallback to CPU
- Single Docker image supports both GPU and CPU execution

### 3. Inference-Only Focus
- Load any RVC voice model
- Process audio input files (.wav, .mp3)
- Output converted audio with configurable quality settings
- **Remove all training functionality** once inference is stable
- Uninstall training-only dependencies
- Delete training-only modules and code paths

## Working Methodology

### AI Agent (Claude) Responsibilities
- **Primary driver** of implementation and testing
- Autonomous decision-making on technical approaches
- Self-directed testing using command-line arguments
- Proactive problem-solving and iteration
- Minimal user involvement during initial development phases

### User Involvement - Request When Needed
- **Audio files**: Request sample input files for testing when needed
- **RVC models**: Request voice models for validation
- **Output verification**: Provide output files for user to verify quality
- **Clarifications**: Ask for guidance only when technical decisions require user input

## Testing Approach
- Use mid-quality settings for iterative testing
- Ensure high-quality output parameters are available and configurable
- Command-line testing mirrors web GUI functionality
- Validate all inference parameters work correctly via CLI

## Development Phases
1. **Assessment**: Analyze current CLI capabilities
2. **CLI Enhancement**: Implement full command-line inference if needed
3. **Testing**: Validate inference works reliably (web + CLI)
4. **Dockerization**: Create containerized environment
5. **Cleanup**: Remove training functionality and dependencies
6. **Optimization**: Reduce footprint and overhead

---

## Current Status

### Phase 1: Assessment - COMPLETE ✅

**Findings:**
- ✅ CLI infrastructure exists: `tools/infer_cli.py` and `tools/infer_batch_rvc.py`
- ✅ Core inference module: `infer/modules/vc/modules.py` with `VC` class
- ✅ Pipeline implementation: `infer/modules/vc/pipeline.py`
- ✅ Web UI: `infer-web.py` with Gradio interface
- ✅ Configuration system: `.env` and `configs/config.py`
- ✅ Test assets available: Brandon model, test audio, hubert model
- ✅ Python version requirement identified: **3.7-3.10** (not 3.13)

**CLI Parameters Available:**
- `--f0up_key`: Pitch shift (default: 0)
- `--input_path`: Input audio file path
- `--index_path`: Index file path for voice retrieval
- `--f0method`: Pitch extraction method (harvest, pm, crepe, rmvpe, etc.)
- `--opt_path`: Output file path
- `--model_name`: Model file name (stored in assets/weights)
- `--index_rate`: Index influence rate (default: 0.66)
- `--device`: Device selection (cuda:0, cpu, etc.)
- `--is_half`: Use FP16 precision (default: True)
- `--filter_radius`: Median filtering radius (default: 3)
- `--resample_sr`: Resample output sample rate (default: 0 = no resample)
- `--rms_mix_rate`: Volume envelope mix rate (default: 1)
- `--protect`: Protect voiceless consonants (default: 0.33)

### Phase 4: Dockerization - COMPLETE ✅

**Implementation:**
- ✅ Python 3.10 base image (nvidia/cuda:11.8.0-cudnn8-runtime-ubuntu22.04)
- ✅ GPU support via NVIDIA CUDA with automatic CPU fallback
- ✅ PyTorch with CUDA 11.8 support installed
- ✅ All dependencies resolved for Python 3.10 compatibility
- ✅ Optimized .dockerignore to reduce image size
- ✅ CLI inference tested and working
- ✅ Test assets included in image (Brandon model, test audio, hubert, rmvpe)

**Test Results:**
- ✅ Image builds successfully (~5.8s with cache)
- ✅ CLI help displays correctly
- ✅ Full inference pipeline works on CPU
- ✅ Generated 2.15MB output file from test audio
- ✅ GPU detection works (falls back to CPU when GPU unavailable)
- ✅ All models load correctly (Brandon voice, hubert, rmvpe)

**Known Issues:**
- ⚠️ Volume mounts on Windows E: drive have Docker Desktop compatibility issues
- **Workaround**: Use `docker cp` to transfer files in/out of container (documented in DOCKER_USAGE.md)

**Files Created:**
- `Dockerfile` - Multi-stage build with all dependencies
- `docker-compose.yml` - Service definition for easy deployment
- `DOCKER_USAGE.md` - Complete usage guide with examples
- `.dockerignore` - Optimized file exclusions

### Phase 5: Cleanup - NEXT 🔜

**Planned Actions:**
1. Remove training-only code paths
2. Uninstall training-only dependencies
3. Delete training-only modules
4. Optimize image size
5. Create minimal inference-only build

---

*This document will be updated as the project evolves.*

