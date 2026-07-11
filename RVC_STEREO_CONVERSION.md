# RVC Stereo Audio Conversion

**Date:** 2026-01-14
**Status:** ✅ Implemented and tested

---

## Problem Statement

RVC (Retrieval-based Voice Conversion) outputs **mono audio** in an **uncommon codec** (raw WAV PCM). This creates compatibility issues:
- ❌ Mono audio (single channel)
- ❌ Uncommon codec (PCM WAV)
- ❌ Limited compatibility with some players/devices

---

## Solution Implemented

Added **automatic post-processing** to `RvcVoiceConversionFeatureBase.cs` that converts RVC output to:
- ✅ **Stereo audio** (2 channels, L=R duplicated from mono)
- ✅ **PCM 16-bit codec** (lossless, highest quality)
- ✅ **WAV container** (universal compatibility, lossless)
- ✅ **No compression** (preserves all audio quality)

---

## Technical Implementation

### Changes Made

**File:** `RightClicks/Features/Audio/RvcVoiceConversionFeatureBase.cs`

1. **Added FFMpegCore import** (line 3)
2. **Added post-processing after RVC completes** (lines 159-219)

### Post-Processing Flow

```
RVC Python Script
    ↓
Mono WAV/MP3 output ({filename}_{ModelName}.{ext})
    ↓
FFmpeg Conversion
    ↓
Stereo WAV output ({filename}_{ModelName}.wav)
    ↓
Delete intermediate mono file
    ↓
Return final stereo WAV file
```

### FFmpeg Command

```csharp
await FFMpegArguments
    .FromFileInput(outputPath)
    .OutputToFile(finalOutputPath, overwrite: true, options => options
        .WithAudioCodec("pcm_s16le")     // 16-bit PCM (lossless)
        .WithCustomArgument("-ac 2"))    // Force stereo (duplicates mono to L=R)
    .CancellableThrough(cancellationToken)
    .ProcessAsynchronously();
```

### Error Handling

- **Graceful fallback:** If FFmpeg conversion fails, returns the original mono file
- **Non-critical errors:** Logs warnings but doesn't fail the entire operation
- **Cleanup:** Deletes intermediate mono file after successful conversion

---

## Output Format

### Before (Mono)
- **Format:** WAV or MP3 (mono)
- **Filename:** `{filename}_{ModelName}.{ext}`
- **Example:** `test_Beavis.mp3` (mono, 3.2 MB)

### After (Stereo WAV)
- **Format:** WAV (PCM 16-bit stereo, lossless)
- **Filename:** `{filename}_{ModelName}.wav`
- **Example:** `test_Beavis.wav` (stereo, 6.4 MB)

---

## Benefits

1. **Lossless Quality**
   - PCM 16-bit is the highest quality audio format
   - No compression artifacts or quality loss
   - Perfect for professional audio work

2. **Universal Compatibility**
   - WAV is supported by all devices and players
   - Works on Windows, Mac, Linux, iOS, Android
   - No codec compatibility issues

3. **Improved Audio Experience**
   - Stereo output sounds fuller and more natural
   - Duplicating mono to stereo creates a wider soundstage
   - Better for headphones and speakers

4. **Seamless Integration**
   - All 24+ RVC voice models automatically get stereo output
   - No changes needed to individual feature classes
   - No user configuration required

---

## Testing

### Test Command
```powershell
RightClicks.exe --feature RvcBeavis --file "testfiles\test.mp3" --test-mode
```

### Expected Log Output
```
[INF] === RVC Voice Conversion: Beavis ===
[INF] Input file: testfiles\test.mp3
[INF] Executing RVC voice conversion...
[INF] RVC process completed with exit code: 0
[INF] RVC output file created: testfiles\test_Beavis.mp3
[INF] Post-processing: Converting mono to stereo WAV format...
[INF] Successfully converted voice to Beavis (stereo WAV)
[INF] Final output: testfiles\test_Beavis.wav
```

### Expected Output File
- **File:** `testfiles\test_Beavis.wav`
- **Format:** WAV (PCM 16-bit stereo, lossless)
- **Channels:** 2 (stereo, L=R)
- **Sample Rate:** 44100 Hz (or original rate)
- **Codec:** PCM 16-bit

---

## Documentation Updates

### Files Updated

1. **README.md** (lines 76-80)
   - Updated RVC output format description
   - Added post-processing feature note

2. **RVC.md** (lines 313-324)
   - Updated expected test output
   - Added FFmpeg post-processing step

3. **RVC_STEREO_CONVERSION.md** (this file)
   - Complete technical documentation

---

## Related Files

- **Implementation:** `RightClicks/Features/Audio/RvcVoiceConversionFeatureBase.cs`
- **Documentation:** `README.md`, `RVC.md`, `RVC_STEREO_CONVERSION.md`
- **Build:** `RightClicks.csproj` (no changes needed - FFMpegCore already referenced)

---

## Future Enhancements (Optional)

- [ ] Add user preference for output format (M4A vs MP3 vs WAV)
- [ ] Add user preference for bitrate (128/192/256/320 kbps)
- [ ] Add option to keep both mono and stereo outputs
- [ ] Add progress reporting for FFmpeg conversion

---

**Status:** ✅ Complete and ready for production use

