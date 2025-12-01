# RVC Inference-Only Docker Container - MINIMAL BUILD
# CLI-only, no web UI, no training code
# Supports both GPU (NVIDIA CUDA) and CPU with automatic detection
# Python 3.10 for compatibility with numba, llvmlite, and other dependencies

FROM nvidia/cuda:11.8.0-cudnn8-runtime-ubuntu22.04

# Prevent interactive prompts during build
ENV DEBIAN_FRONTEND=noninteractive

# Set working directory
WORKDIR /app

# Install system dependencies (minimal set)
RUN apt-get update && apt-get install -y \
    python3.10 \
    python3.10-dev \
    python3-pip \
    ffmpeg \
    git \
    build-essential \
    && rm -rf /var/lib/apt/lists/*

# Set Python 3.10 as default
RUN update-alternatives --install /usr/bin/python python /usr/bin/python3.10 1 && \
    update-alternatives --install /usr/bin/python3 python3 /usr/bin/python3.10 1

# Upgrade pip
RUN python3 -m pip install --no-cache-dir --upgrade pip

# Copy minimal requirements for inference only
COPY requirements-inference.txt .

# Install PyTorch with CUDA support (will fallback to CPU if no GPU available)
RUN pip install --no-cache-dir torch torchvision torchaudio --index-url https://download.pytorch.org/whl/cu118

# Install base numerical dependencies
RUN pip install --no-cache-dir \
    numpy==1.23.5 \
    scipy \
    Cython

# Install numba and llvmlite (critical for Python 3.10 compatibility)
RUN pip install --no-cache-dir \
    llvmlite==0.39.0 \
    numba==0.56.4

# Install audio processing libraries (inference only)
RUN pip install --no-cache-dir \
    librosa==0.9.1 \
    soundfile>=0.12.1 \
    audioread \
    resampy>=0.4.2 \
    pydub>=0.25.1 \
    pyworld==0.3.2 \
    praat-parselmouth>=0.4.2

# Install faiss for voice retrieval
RUN pip install --no-cache-dir faiss-cpu==1.7.3

# Install minimal utilities (no web UI, no visualization)
RUN pip install --no-cache-dir \
    ffmpeg-python>=0.2.0 \
    python-dotenv>=1.0.0 \
    tqdm>=4.63.1

# Temporarily downgrade pip to install omegaconf with old metadata format
RUN pip install --no-cache-dir pip==24.0

# Install fairseq dependencies
RUN pip install --no-cache-dir \
    omegaconf==2.0.6 \
    hydra-core==1.0.7

# Install fairseq from git (required for hubert model)
RUN pip install --no-cache-dir git+https://github.com/facebookresearch/fairseq.git@v0.12.2

# Upgrade pip back
RUN pip install --no-cache-dir --upgrade pip

# Install pitch extraction tools
RUN pip install --no-cache-dir \
    torchcrepe==0.0.20 \
    torchfcpe

# Copy only necessary application code (inference only)
COPY configs/ ./configs/
COPY infer/ ./infer/
COPY tools/infer_cli.py ./tools/infer_cli.py
COPY tools/infer_batch_rvc.py ./tools/infer_batch_rvc.py
COPY .env .env

# Copy test assets for built-in inference capability
COPY assets/hubert/ ./assets/hubert/
COPY assets/rmvpe/ ./assets/rmvpe/
COPY assets/weights/ ./assets/weights/
COPY assets/TestAudios/ ./assets/TestAudios/

# Create output and logs directories
RUN mkdir -p output logs input

# Set environment variables
ENV PYTHONUNBUFFERED=1
ENV PYTORCH_ENABLE_MPS_FALLBACK=1

# Default command: run CLI help
CMD ["python", "tools/infer_cli.py", "--help"]
