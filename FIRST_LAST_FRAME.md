# First + Last Frame to Video Feature

Generate smooth, cinematic videos from just two images using AI models. Select a first frame and last frame, configure generation parameters, and let the AI create a seamless transition between them.

## How It Works

### Two-Click Pattern

1. **First Click**: Right-click on an image file (`.jpg`, `.jpeg`, `.png`, `.webp`, `.gif`, `.avif`)
   - Select "First + Last Frames" from the RightClicks menu
   - The image is marked as the **first frame**
   - You have 20 seconds to select the second image

2. **Second Click**: Right-click on another image file within 20 seconds
   - Select "First + Last Frames" again
   - The configuration window opens automatically
   - Both images are now ready for processing

3. **Configure & Generate**:
   - Select an AI model from the dropdown
   - View model details (description, pricing, processing time)
   - Adjust generation parameters (prompt, duration, resolution, etc.)
   - Click "Submit" to start video generation
   - The AI generates a smooth transition video between your two frames

## Supported Models

The feature supports 16 different AI models, each with unique capabilities:

| Model | Provider | Pricing | Speed | Quality |
|-------|----------|---------|-------|---------|
| Wan FLF2V | fal.ai | $0.20 | 2-3 min | High |
| Veo 3.1 | Google | $0.50 | 3-5 min | Premium |
| Pixverse v5 | Pixverse | $0.20 | 1-2 min | High |
| Kling 2.6 Pro | Kuaishou | $0.50 | 2-3 min | Premium |
| Seedance 1.0 Pro | ByteDance | $0.20 | 2-3 min | High |
| Hailuo 02 Pro | MiniMax | $0.30 | 2-3 min | High |
| And 10 more... | Various | Varies | Varies | Varies |

## Configuration

### Environment Variables

**Required:**
- `FAL_KEY` - Your fal.ai API key (get one at https://fal.ai)
- `CLOUDINARY_API_KEY` - Cloudinary API key for image hosting
- `CLOUDINARY_API_SECRET` - Cloudinary API secret for image deletion

### Model Parameters

Each model supports customizable parameters:

- **Prompt** (required) - Text description of the desired video transition
- **Negative Prompt** (optional) - What to avoid in generation
- **Duration** - Video length (varies by model: 4-8 seconds)
- **Resolution** - Output quality (480p, 720p, 1080p)
- **Aspect Ratio** - Video dimensions (16:9, 9:16, 1:1, etc.)
- **Seed** - Random seed for reproducibility
- **FPS** - Frames per second (varies by model)

## Output

Generated videos are saved next to your first image with the naming pattern:

```
{FirstImageName}_to_{LastImageName}_flf2v.mp4
```

Example:
- Input: `sunset.jpg` + `ocean.jpg`
- Output: `sunset_to_ocean_flf2v.mp4`

If a file with that name already exists, a counter is appended:
- `sunset_to_ocean_flf2v_1.mp4`
- `sunset_to_ocean_flf2v_2.mp4`

## Technical Details

### Image Hosting

Images are temporarily uploaded to Cloudinary during processing:
1. Both images are uploaded to Cloudinary
2. URLs are sent to the fal.ai API
3. Images are automatically deleted from Cloudinary after generation
4. Only the final video is kept locally

### Model-Specific Parameter Names

Each model uses different API parameter names for the start/end images. The feature automatically handles this:

- **Wan FLF2V**: `start_image_url`, `end_image_url`
- **Veo 3.1**: `first_frame_url`, `last_frame_url`
- **Pixverse**: `first_frame_image`, `last_frame_image`
- **Kling**: `image_url`, `tail_image_url`
- **Seedance**: `image_url`, `last_frame_url`
- **Hailuo**: `image_url`, `last_frame_url`

### Timeout

API requests have a 10-minute timeout to allow for longer processing times on premium models.

## Troubleshooting

### "FAL_KEY environment variable not set"
- Set your fal.ai API key: `[Environment]::SetEnvironmentVariable("FAL_KEY", "your-key", "User")`
- Restart your terminal/IDE

### "Cloudinary not configured"
- Verify `CLOUDINARY_API_KEY` and `CLOUDINARY_API_SECRET` are set
- Check that `config.json` has Cloudinary configuration

### Configuration window doesn't appear
- Ensure RightClicks is running
- Check that both images are valid (supported formats)
- Verify the 20-second window hasn't expired

### Video generation fails
- Check the logs in `%LOCALAPPDATA%\RightClicks\logs\`
- Verify your fal.ai account has sufficient credits
- Try a different model if one is unavailable

## Logs

Detailed logs are saved to:
```
%LOCALAPPDATA%\RightClicks\logs\RightClicks-TEST-YYYYMMDD-HHMMSS.log
```

Check logs for:
- Image upload status to Cloudinary
- API request/response details
- Video generation progress
- Download completion

## Feature ID

For CLI testing:
```powershell
RightClicks.exe --feature FirstLastFrame --file "image.jpg" --test-mode
```

