using FFMpegCore;
using RightClicks.Models;
using RightClicks.Models.Whisper;
using RightClicks.Services;
using Serilog;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Whisper.net;
using Whisper.net.Ggml;
using GgmlType = Whisper.net.Ggml.GgmlType;

namespace RightClicks.Features.Audio;

/// <summary>
/// Base class for all Whisper transcription features.
/// Provides shared logic for audio extraction, model management, transcription, and cleanup.
/// 
/// ARCHITECTURE NOTE: This base class exists to avoid code duplication across multiple
/// Whisper models (Tiny, Base, Small, Medium, Large, Turbo). Each model has the same workflow:
/// 1. Extract audio from video (if needed) using FFmpeg
/// 2. Download/cache Whisper model
/// 3. Create processor with GPU detection and fallback
/// 4. Transcribe audio
/// 5. Save transcription to .txt file
/// 6. Clean up temporary files
/// 
/// The only differences between models are:
/// - Model type (Tiny, Base, Small, Medium, Large, Turbo)
/// - Display name and description
/// - VRAM requirements
/// 
/// GPU FALLBACK: Always attempts GPU acceleration first, silently falls back to CPU if unavailable.
/// No user alerts - only logging.
/// </summary>
public abstract class WhisperTranscribeFeatureBase : IFileFeature
{
    public abstract string Id { get; }
    public abstract string DisplayName { get; }
    public abstract string Description { get; }

    /// <summary>
    /// Whisper model type to use for transcription.
    /// </summary>
    protected abstract GgmlType WhisperModelType { get; }

    public string[] SupportedExtensions => new[]
    {
        // Audio formats
        ".mp3", ".wav", ".flac", ".m4a", ".ogg", ".aac", ".opus", ".mpga", ".webm",
        // Video formats (audio will be extracted)
        ".mp4", ".mpeg", ".mov", ".avi", ".mkv", ".wmv", ".flv"
    };

    public bool IsCloudBased => false;

    public async Task<FeatureResult> ExecuteAsync(string filePath, CancellationToken cancellationToken)
    {
        var startTime = DateTime.Now;
        Log.Information("WhisperTranscribeFeature ({ModelType}): Starting execution for file: {FilePath}",
            WhisperService.GetModelDisplayName(WhisperModelType), filePath);

        string? tempAudioPath = null;

        try
        {
            // 1. Resolve full path
            var fullPath = Path.GetFullPath(filePath);
            Log.Debug("Full path resolved: {FullPath}", fullPath);

            if (!File.Exists(fullPath))
            {
                Log.Error("File not found: {FullPath}", fullPath);
                var duration = (long)(DateTime.Now - startTime).TotalMilliseconds;
                return FeatureResult.CreateFailure($"File not found: {fullPath}", null, duration);
            }

            var fileExtension = Path.GetExtension(fullPath).ToLowerInvariant();
            var directory = Path.GetDirectoryName(fullPath) ?? string.Empty;
            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(fullPath);

            // 2. Convert to WAV format (Whisper.net requires WAV with 16kHz sample rate)
            // CRITICAL FIX: Even if the file is already WAV, we MUST convert it to ensure 16kHz sample rate.
            // Whisper.net throws "Only 16KHz sample rate is supported" error if the WAV file has a different
            // sample rate (e.g., 44.1kHz, 48kHz). This was causing WAV transcription to fail.
            // Solution: Always convert ALL files (including WAV) to temporary WAV with correct format.
            string audioPath;
            bool isVideoFile = IsVideoFile(fileExtension);

            // Always convert to temporary WAV with correct format (PCM 16-bit, 16kHz)
            var fileType = isVideoFile ? "Video" : "Audio";
            Log.Information("{FileType} file detected, converting to temporary WAV file (16kHz required)...", fileType);
            tempAudioPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.wav");

            await FFMpegArguments
                .FromFileInput(fullPath)
                .OutputToFile(tempAudioPath, overwrite: true, options => options
                    .WithAudioCodec("pcm_s16le")
                    .WithAudioSamplingRate(16000)
                    .ForceFormat("wav"))
                .CancellableThrough(cancellationToken)
                .ProcessAsynchronously();

            Log.Information("Audio converted to WAV (16kHz): {TempAudioPath}", tempAudioPath);
            audioPath = tempAudioPath;

            // 3. Download/cache Whisper model
            Log.Information("Ensuring Whisper model is available: {ModelType}", WhisperModelType);
            var modelPath = await WhisperService.GetModelPathAsync(WhisperModelType, cancellationToken);
            Log.Information("Model ready at: {ModelPath}", modelPath);

            // 4. Create processor (GPU acceleration automatic)
            Log.Information("Creating Whisper processor...");
            var processor = WhisperService.CreateProcessor(modelPath);

            // 5. Transcribe audio
            Log.Information("Starting transcription (this may take a few minutes)...");
            var transcriptionResult = await TranscribeAudioAsync(processor, audioPath, cancellationToken);

            processor.Dispose();

            Log.Information("Transcription completed in {DurationMs}ms",
                transcriptionResult.DurationMs);

            // 6. Save transcription to .txt file
            var outputPath = Path.Combine(directory, $"{fileNameWithoutExt}.txt");
            await File.WriteAllTextAsync(outputPath, transcriptionResult.Text, cancellationToken);

            Log.Information("Transcription saved to: {OutputPath}", outputPath);

            var totalDuration = (long)(DateTime.Now - startTime).TotalMilliseconds;
            return FeatureResult.CreateSuccess(
                $"Transcription completed using {WhisperService.GetModelDisplayName(WhisperModelType)} model",
                outputPath,
                totalDuration
            );
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Transcription failed: {Message}", ex.Message);
            var duration = (long)(DateTime.Now - startTime).TotalMilliseconds;
            return FeatureResult.CreateFailure($"Transcription failed: {ex.Message}", ex, duration);
        }
        finally
        {
            // Clean up temporary audio file
            if (tempAudioPath != null && File.Exists(tempAudioPath))
            {
                try
                {
                    File.Delete(tempAudioPath);
                    Log.Debug("Deleted temporary audio file: {TempAudioPath}", tempAudioPath);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Failed to delete temporary audio file: {TempAudioPath}", tempAudioPath);
                }
            }
        }
    }

    /// <summary>
    /// Transcribe audio file using Whisper processor.
    /// </summary>
    private async Task<WhisperTranscriptionResult> TranscribeAudioAsync(
        Whisper.net.WhisperProcessor processor,
        string audioPath,
        CancellationToken cancellationToken)
    {
        var transcriptionStart = DateTime.Now;
        var fullText = new StringBuilder();
        var segments = new List<WhisperSegment>();

        using var fileStream = File.OpenRead(audioPath);

        await foreach (var segment in processor.ProcessAsync(fileStream, cancellationToken))
        {
            Log.Debug("[{Start} --> {End}] {Text}",
                segment.Start.ToString(@"mm\:ss"),
                segment.End.ToString(@"mm\:ss"),
                segment.Text);

            fullText.AppendLine(segment.Text);

            segments.Add(new WhisperSegment
            {
                Start = segment.Start,
                End = segment.End,
                Text = segment.Text,
                Probability = segment.Probability
            });
        }

        var transcriptionDuration = (long)(DateTime.Now - transcriptionStart).TotalMilliseconds;

        return new WhisperTranscriptionResult
        {
            Text = fullText.ToString().Trim(),
            Segments = segments,
            DurationMs = transcriptionDuration,
            ModelType = WhisperService.GetModelDisplayName(WhisperModelType)
        };
    }

    /// <summary>
    /// Check if the file extension is a video format.
    /// </summary>
    private bool IsVideoFile(string extension)
    {
        var videoExtensions = new[] { ".mp4", ".mpeg", ".mov", ".avi", ".mkv", ".wmv", ".flv", ".webm" };
        return Array.Exists(videoExtensions, ext => ext.Equals(extension, StringComparison.OrdinalIgnoreCase));
    }
}
