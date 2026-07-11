# Image-to-Video Feature

## Overview

The Image-to-Video feature generates videos from static images using AI models. Select an image file, choose a model, configure parameters, and the system will generate a video showing motion/animation based on your prompt.

## Supported Models

Five fal.ai models available via cascading menu:

| Model | Price | Duration | Quality | Best For |
|-------|-------|----------|---------|----------|
| **Veo 3.1** | $0.50/vid | 8s max | High | Professional quality, detailed prompts |
| **Kling 2.6** | $0.50/vid | 10s max | High | Longer videos, smooth motion |
| **Vidu 1.5** | $0.30/vid | 8s max | Medium | Balanced quality/price |
| **Wan 2.1** | $0.10/vid | 4s max | Medium | Budget option, quick generation |
| **Seedance 1.0** | $0.20/vid | 4s max | Medium | Budget option, alternative |

## How to Use

### 1. Select Image
Right-click any image file (.jpg, .png, .webp, .gif, .avif) in Windows Explorer.

### 2. Open Configuration
Select **Image to Video >** → Choose a model (e.g., "☁️ Veo 3.1 $0.50/vid")

### 3. Configure Parameters
The configuration window opens with:
- **Image Preview** - Shows your selected image
- **Model Info** - Description, pricing, processing time
- **Dynamic Form** - Parameters specific to the selected model:
  - **Prompt** (required) - Describe the motion/animation you want
  - **Negative Prompt** - What to avoid (blur, distortion, etc.)
  - **Duration** - Video length (5s or 10s, model-dependent)
  - **Resolution** - Output quality (480p, 720p, 1080p)
  - **Aspect Ratio** - Video format (16:9, 9:16, 1:1)
  - **Generate Audio** - Add AI-generated audio (some models)

### 4. Submit
Click **Submit** to start processing. The feature will:
1. Upload image to Cloudinary
2. Call fal.ai API (2-5 minutes processing)
3. Download result video
4. Clean up temporary files
5. Save as `{filename}_video.mp4` next to original image

## Example Workflow

**Input:** `sunset.jpg`

**Configuration:**
- Model: Veo 3.1
- Prompt: "Camera pans across a beautiful sunset, clouds moving slowly"
- Duration: 8 seconds
- Resolution: 1080p

**Output:** `sunset_video.mp4` (1080p, 8-second video)

## Requirements

### Environment Variables (User-level)
- `FAL_KEY` - fal.ai API key
- `CLOUDINARY_API_KEY` - Cloudinary API key
- `CLOUDINARY_API_SECRET` - Cloudinary API secret

### Configuration
- Cloudinary must be configured in `config.json`

## Output

- **Location:** Same directory as input image
- **Filename:** `{original_name}_video.mp4`
- **Format:** MP4 video
- **Resolution:** As configured (480p-1080p)
- **Duration:** As configured (4-10 seconds depending on model)

## Tips

1. **Prompt Quality** - More detailed prompts = better results
   - ✅ Good: "Camera slowly zooms in on a flower blooming, petals opening"
   - ❌ Bad: "flower"

2. **Model Selection**
   - Use **Veo 3.1** or **Kling 2.6** for best quality
   - Use **Wan 2.1** or **Seedance 1.0** for quick/cheap generation

3. **Duration** - Longer durations (10s) cost more but look smoother

4. **Aspect Ratio** - Match your image's aspect ratio for best results

## Troubleshooting

### "FAL_KEY environment variable not set"
- Set `FAL_KEY` in Windows environment variables (User-level)
- Restart terminal/IDE after setting

### "Cloudinary not configured"
- Check `config.json` has Cloudinary section
- Verify `CLOUDINARY_API_KEY` and `CLOUDINARY_API_SECRET` are set

### Video generation takes too long
- Normal: 2-5 minutes depending on model
- Check logs: `%LOCALAPPDATA%\RightClicks\logs\RightClicks-*.log`

### Output file not created
- Check logs for errors
- Verify Cloudinary upload succeeded
- Verify fal.ai API call succeeded

## Architecture

**Workflow:**
1. Open configuration window (dynamic form based on model)
2. Upload image to Cloudinary (required by fal.ai)
3. Call fal.ai synchronous endpoint (waits for result)
4. Download result video from fal.ai
5. Clean up Cloudinary image
6. Save video next to original image

**Design:**
- Base class pattern (ImageToVideoFeatureBase) - avoids code duplication
- JSON-driven configuration - easy to add new models
- Dynamic form generation - no hardcoded UI per model
- Comprehensive error handling and cleanup

## See Also

- **First + Last Frames** - Generate video between TWO images (interpolation)
- **Image to Video** - Generate video from ONE image (AI imagination)

