using RightClicks.Models;
using RightClicks.Models.Karaoke;
using RightClicks.Services;
using Serilog;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Whisper.net.Ggml;
using GgmlType = Whisper.net.Ggml.GgmlType;

namespace RightClicks.Features.Video;

/// <summary>
/// Base class for all karaoke subtitle rendering features.
/// Provides shared logic for ASS generation, Whisper transcription, and video rendering.
/// 
/// ARCHITECTURE NOTE: This base class exists to avoid code duplication across 9 karaoke features
/// (3 styles × 3 Whisper model tiers). Each feature has the same workflow:
/// 1. Load style configuration from JSON
/// 2. Extract audio from video
/// 3. Transcribe with Whisper (word-level timestamps)
/// 4. Generate ASS subtitle file with karaoke highlighting
/// 5. Render video with burned-in subtitles using FFmpeg
/// 6. Output: {filename}_SUBTITLED.mp4 + {filename}.ass
/// 
/// The only differences between features are:
/// - Style name (Classic, ModernGlow, NeonPop)
/// - Whisper model tier (Tiny, Medium, High)
/// - Display name and description
/// </summary>
public abstract class KaraokeFeatureBase : IFileFeature
{
    public abstract string Id { get; }
    public abstract string DisplayName { get; }
    public abstract string Description { get; }

    /// <summary>
    /// Karaoke style name (Classic, ModernGlow, NeonPop).
    /// </summary>
    protected abstract string StyleName { get; }

    /// <summary>
    /// Whisper model type to use for transcription.
    /// </summary>
    protected abstract GgmlType WhisperModelType { get; }

    public string[] SupportedExtensions => new[]
    {
        ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm", ".mpeg", ".mpg", ".m4v"
    };

    public bool IsCloudBased => false; // Karaoke uses local Whisper.net, not cloud APIs

    public string Category => "Video";

    public async Task<FeatureResult> ExecuteAsync(string fullPath, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        string? assPath = null;
        string? outputPath = null;

        try
        {
            Log.Information("=== Karaoke Subtitle Rendering Started ===");
            Log.Information("Feature: {FeatureId} ({DisplayName})", Id, DisplayName);
            Log.Information("Input file: {FullPath}", fullPath);
            Log.Information("Style: {StyleName}, Whisper Model: {WhisperModel}", StyleName, WhisperModelType);

            // Validate input file
            if (!File.Exists(fullPath))
            {
                var error = $"Input file not found: {fullPath}";
                Log.Error(error);
                return FeatureResult.CreateFailure(error, durationMs: stopwatch.ElapsedMilliseconds);
            }

            // 1. Load style configuration
            Log.Information("Loading karaoke style configuration: {StyleName}", StyleName);
            var styleConfig = KaraokeService.LoadStyleConfig(StyleName);

            // 2. Generate ASS subtitles (includes Whisper transcription)
            Log.Information("Generating ASS subtitles with word-level karaoke timing...");
            assPath = await KaraokeService.GenerateAssSubtitlesAsync(
                fullPath,
                styleConfig,
                WhisperModelType,
                cancellationToken
            );

            if (!File.Exists(assPath))
            {
                var error = "ASS subtitle file was not created";
                Log.Error(error);
                return FeatureResult.CreateFailure(error, durationMs: stopwatch.ElapsedMilliseconds);
            }

            Log.Information("ASS subtitles generated successfully: {AssPath}", assPath);

            // 3. Render video with burned-in subtitles
            Log.Information("Rendering video with burned-in subtitles...");
            outputPath = await KaraokeService.RenderVideoWithSubtitlesAsync(
                fullPath,
                assPath,
                cancellationToken
            );

            if (!File.Exists(outputPath))
            {
                var error = "Output video file was not created";
                Log.Error(error);
                return FeatureResult.CreateFailure(error, durationMs: stopwatch.ElapsedMilliseconds);
            }

            stopwatch.Stop();

            Log.Information("=== Karaoke Subtitle Rendering Complete ===");
            Log.Information("Output video: {OutputPath}", outputPath);
            Log.Information("ASS subtitles: {AssPath}", assPath);
            Log.Information("Duration: {Duration:F2} seconds", stopwatch.Elapsed.TotalSeconds);

            return FeatureResult.CreateSuccess(
                $"Karaoke video created successfully with {StyleName} style",
                outputPath,
                stopwatch.ElapsedMilliseconds
            );
        }
        catch (OperationCanceledException)
        {
            Log.Warning("Karaoke rendering was cancelled");
            return FeatureResult.CreateFailure("Operation was cancelled", durationMs: stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Karaoke rendering failed");
            return FeatureResult.CreateFailure($"Karaoke rendering failed: {ex.Message}", ex, stopwatch.ElapsedMilliseconds);
        }
    }
}

