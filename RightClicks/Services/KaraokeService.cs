using FFMpegCore;
using RightClicks.Models.Karaoke;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Whisper.net;
using Whisper.net.Ggml;
using GgmlType = Whisper.net.Ggml.GgmlType;

namespace RightClicks.Services;

/// <summary>
/// Service for generating karaoke-style ASS subtitles and rendering them into videos.
/// Handles Whisper transcription, ASS generation with word-level timing, and FFmpeg video rendering.
/// </summary>
public class KaraokeService
{
    private static readonly string StylesDirectory = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory,
        "KaraokeStyles"
    );

    /// <summary>
    /// Load a karaoke style configuration from JSON.
    /// </summary>
    public static KaraokeStyleConfig LoadStyleConfig(string styleName)
    {
        var styleConfigPath = Path.Combine(StylesDirectory, styleName, "style.json");
        
        if (!File.Exists(styleConfigPath))
        {
            Log.Warning("Style config not found: {StyleConfigPath}, using defaults", styleConfigPath);
            return new KaraokeStyleConfig { StyleName = styleName };
        }

        try
        {
            var json = File.ReadAllText(styleConfigPath);
            var config = JsonSerializer.Deserialize<KaraokeStyleConfig>(json);
            
            if (config == null)
            {
                Log.Warning("Failed to deserialize style config: {StyleConfigPath}, using defaults", styleConfigPath);
                return new KaraokeStyleConfig { StyleName = styleName };
            }

            Log.Information("Loaded karaoke style config: {StyleName}", styleName);
            return config;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error loading style config: {StyleConfigPath}", styleConfigPath);
            return new KaraokeStyleConfig { StyleName = styleName };
        }
    }

    /// <summary>
    /// Convert seconds to ASS time format (h:mm:ss.cc).
    /// </summary>
    private static string ConvertToAssTime(double seconds)
    {
        var timeSpan = TimeSpan.FromSeconds(seconds);
        var hours = (int)timeSpan.TotalHours;
        var minutes = timeSpan.Minutes;
        var secs = timeSpan.Seconds;
        var centisecs = timeSpan.Milliseconds / 10; // Convert milliseconds to centiseconds

        var result = string.Format("{0}:{1:D2}:{2:D2}.{3:D2}", hours, minutes, secs, centisecs);

        // DEBUG: Log conversion details for first few calls
        if (seconds < 10)
        {
            Log.Debug("ConvertToAssTime({Seconds}s) -> h={Hours}, m={Minutes}, s={Secs}, cs={Centisecs} -> {Result}",
                seconds, hours, minutes, secs, centisecs, result);
        }

        return result;
    }

    /// <summary>
    /// Generate ASS subtitle file with karaoke word-level highlighting.
    /// </summary>
    public static async Task<string> GenerateAssSubtitlesAsync(
        string videoPath,
        KaraokeStyleConfig styleConfig,
        GgmlType whisperModel,
        CancellationToken cancellationToken)
    {
        Log.Information("Starting karaoke ASS generation for: {VideoPath}", videoPath);
        Log.Information("Using Whisper model: {WhisperModel}, Style: {StyleName}", whisperModel, styleConfig.StyleName);

        // 1. Extract audio from video if needed
        var audioPath = await ExtractAudioAsync(videoPath, cancellationToken);

        try
        {
            // 2. Transcribe audio with Whisper (word-level timestamps)
            var segments = await TranscribeWithWordTimestampsAsync(audioPath, whisperModel, cancellationToken);

            // 3. Generate ASS file
            var assPath = Path.ChangeExtension(videoPath, ".ass");
            GenerateAssFile(segments, styleConfig, assPath);

            Log.Information("ASS subtitle file generated: {AssPath}", assPath);
            return assPath;
        }
        finally
        {
            // Clean up temporary audio file if it was extracted
            if (audioPath != videoPath && File.Exists(audioPath))
            {
                try
                {
                    File.Delete(audioPath);
                    Log.Debug("Deleted temporary audio file: {AudioPath}", audioPath);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Failed to delete temporary audio file: {AudioPath}", audioPath);
                }
            }
        }
    }

    /// <summary>
    /// Extract audio from video file to temporary WAV (16kHz for Whisper).
    /// </summary>
    private static async Task<string> ExtractAudioAsync(string videoPath, CancellationToken cancellationToken)
    {
        var tempAudioPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.wav");
        
        Log.Information("Extracting audio from video to: {TempAudioPath}", tempAudioPath);

        await FFMpegArguments
            .FromFileInput(videoPath)
            .OutputToFile(tempAudioPath, overwrite: true, options => options
                .WithAudioCodec("pcm_s16le")
                .WithAudioSamplingRate(16000) // Whisper requires 16kHz
                .ForceFormat("wav"))
            .CancellableThrough(cancellationToken)
            .ProcessAsynchronously();

        Log.Information("Audio extraction complete: {TempAudioPath}", tempAudioPath);
        return tempAudioPath;
    }

    /// <summary>
    /// Transcribe audio using Whisper with word-level timestamps.
    /// Normalizes timestamps to start from 0:00 (removes leading silence offset).
    /// </summary>
    private static async Task<List<KaraokeSegment>> TranscribeWithWordTimestampsAsync(
        string audioPath,
        GgmlType whisperModel,
        CancellationToken cancellationToken)
    {
        Log.Information("Transcribing audio with Whisper model: {WhisperModel}", whisperModel);

        // Get or download Whisper model
        var modelPath = await WhisperService.GetModelPathAsync(whisperModel, cancellationToken);

        // Create Whisper processor
        using var processor = WhisperService.CreateProcessor(modelPath);

        var segments = new List<KaraokeSegment>();

        // Process audio file - Whisper.net requires a Stream, not a file path
        using var fileStream = File.OpenRead(audioPath);

        await foreach (var segment in processor.ProcessAsync(fileStream, cancellationToken))
        {
            Log.Debug("Whisper segment: Start={Start}, End={End}, Text={Text}",
                segment.Start, segment.End, segment.Text.Trim());

            var karaokeSegment = new KaraokeSegment
            {
                Start = segment.Start,
                End = segment.End,
                Text = segment.Text.Trim(),
                Words = new List<KaraokeWord>()
            };

            // Fallback: Split text into words and estimate timing
            // Note: Whisper.net 1.9.0 SegmentData doesn't expose word-level timestamps directly
            // We need to estimate word timing based on segment duration
            var words = karaokeSegment.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var segmentDuration = (karaokeSegment.End - karaokeSegment.Start).TotalSeconds;
            var durationPerWord = words.Length > 0 ? segmentDuration / words.Length : 0;

            for (int i = 0; i < words.Length; i++)
            {
                var wordStart = karaokeSegment.Start.TotalSeconds + (i * durationPerWord);
                var wordEnd = wordStart + durationPerWord;

                karaokeSegment.Words.Add(new KaraokeWord
                {
                    Start = TimeSpan.FromSeconds(wordStart),
                    End = TimeSpan.FromSeconds(wordEnd),
                    Word = words[i]
                });
            }

            segments.Add(karaokeSegment);
        }

        Log.Information("Transcription complete: {SegmentCount} segments, {WordCount} words",
            segments.Count, segments.Sum(s => s.Words.Count));

        // CRITICAL FIX: Normalize timestamps to start from 0:00
        // Whisper may return timestamps with a large offset if there's silence at the beginning
        if (segments.Any())
        {
            var firstSegmentStart = segments.First().Start;
            Log.Information("First segment starts at: {FirstStart} - normalizing all timestamps to start from 0:00", firstSegmentStart);

            foreach (var segment in segments)
            {
                // Adjust segment timestamps
                segment.Start -= firstSegmentStart;
                segment.End -= firstSegmentStart;

                // Adjust word timestamps
                foreach (var word in segment.Words)
                {
                    word.Start -= firstSegmentStart;
                    word.End -= firstSegmentStart;
                }
            }

            Log.Information("Timestamps normalized - first segment now starts at: {NewStart}", segments.First().Start);
        }

        return segments;
    }

    /// <summary>
    /// Generate ASS file with karaoke-style word highlighting.
    /// </summary>
    private static void GenerateAssFile(
        List<KaraokeSegment> segments,
        KaraokeStyleConfig styleConfig,
        string assPath)
    {
        Log.Information("Generating ASS file: {AssPath}", assPath);

        // DEBUG: Log first segment timing
        if (segments.Any())
        {
            var firstSeg = segments.First();
            Log.Debug("GenerateAssFile - First segment: Start={Start}, End={End}, Text={Text}",
                firstSeg.Start, firstSeg.End, firstSeg.Text);
        }

        var sb = new StringBuilder();

        // [Script Info]
        sb.AppendLine("[Script Info]");
        sb.AppendLine("Title: RightClicks Karaoke Subtitles");
        sb.AppendLine($"ScriptType: v4.00+");
        sb.AppendLine($"PlayResX: {styleConfig.PlayResX}");
        sb.AppendLine($"PlayResY: {styleConfig.PlayResY}");
        sb.AppendLine("Collision: Normal");
        sb.AppendLine();

        // [V4+ Styles]
        sb.AppendLine("[V4+ Styles]");
        sb.AppendLine("Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, " +
                      "Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, " +
                      "Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding");

        // Default style (entire line, always visible)
        sb.AppendLine($"Style: Default,{styleConfig.FontName},{styleConfig.FontSize}," +
                      $"{styleConfig.DefaultPrimaryColor},&H00000000,{styleConfig.DefaultOutlineColor},&H00000000," +
                      $"0,0,0,0,100,100,0,0,1,{styleConfig.DefaultOutlineThickness},0," +
                      $"{styleConfig.Alignment},{styleConfig.MarginL},{styleConfig.MarginR},{styleConfig.MarginV},1");

        // Highlight style (active word)
        sb.AppendLine($"Style: Highlight,{styleConfig.FontName},{styleConfig.FontSize}," +
                      $"{styleConfig.HighlightPrimaryColor},&H00000000,{styleConfig.HighlightOutlineColor},&H00000000," +
                      $"0,0,0,0,100,100,0,0,1,{styleConfig.HighlightOutlineThickness},0," +
                      $"{styleConfig.Alignment},{styleConfig.MarginL},{styleConfig.MarginR},{styleConfig.MarginV},1");
        sb.AppendLine();

        // [Events]
        sb.AppendLine("[Events]");
        sb.AppendLine("Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text");

        foreach (var segment in segments)
        {
            var startTime = ConvertToAssTime(segment.Start.TotalSeconds);
            var endTime = ConvertToAssTime(segment.End.TotalSeconds);
            var fullText = segment.Text;

            // DEBUG: Log the actual seconds being converted
            Log.Debug("Segment timing: Start={StartSeconds}s -> {StartTime}, End={EndSeconds}s -> {EndTime}",
                segment.Start.TotalSeconds, startTime, segment.End.TotalSeconds, endTime);

            // Layer 0: Full line in default style (always visible)
            sb.AppendLine($"Dialogue: 0,{startTime},{endTime},Default,,0,0,0,,{fullText}");

            // Layer 1: Word-by-word highlighting
            if (segment.Words.Any())
            {
                foreach (var word in segment.Words)
                {
                    var wordStart = ConvertToAssTime(word.Start.TotalSeconds);
                    var wordEnd = ConvertToAssTime(word.End.TotalSeconds);

                    // Build highlight line: only current word is visible
                    var highlightLine = new StringBuilder();
                    foreach (var w in segment.Words)
                    {
                        if (w == word)
                        {
                            // Active word: fully opaque
                            highlightLine.Append($"{{\\alpha&H00&}}{w.Word} ");
                        }
                        else
                        {
                            // Inactive word: fully transparent
                            highlightLine.Append($"{{\\alpha&HFF&}}{w.Word} ");
                        }
                    }

                    sb.AppendLine($"Dialogue: 1,{wordStart},{wordEnd},Highlight,,0,0,0,,{highlightLine.ToString().TrimEnd()}");
                }
            }
        }

        File.WriteAllText(assPath, sb.ToString(), Encoding.UTF8);
        Log.Information("ASS file written successfully: {AssPath}", assPath);
    }

    /// <summary>
    /// Render video with burned-in ASS subtitles using the 'subtitles' filter.
    /// The 'subtitles' filter is more reliable than 'ass' filter for Windows paths.
    /// </summary>
    public static async Task<string> RenderVideoWithSubtitlesAsync(
        string videoPath,
        string assPath,
        CancellationToken cancellationToken)
    {
        var outputPath = Path.Combine(
            Path.GetDirectoryName(videoPath)!,
            Path.GetFileNameWithoutExtension(videoPath) + "_SUBTITLED.mp4"
        );

        Log.Information("Rendering video with subtitles: {OutputPath}", outputPath);
        Log.Information("Input video: {VideoPath}", videoPath);
        Log.Information("ASS subtitles: {AssPath}", assPath);

        // Verify ASS file exists
        if (!File.Exists(assPath))
        {
            throw new FileNotFoundException($"ASS subtitle file not found: {assPath}");
        }

        // For the 'subtitles' filter, we need to escape backslashes and colons
        // Windows path: E:\My Apps\file.ass -> E\\:\\\\My Apps\\\\file.ass
        var escapedAssPath = assPath.Replace("\\", "\\\\").Replace(":", "\\:");
        Log.Information("Escaped ASS path for FFmpeg: {EscapedAssPath}", escapedAssPath);

        // Use 'subtitles' filter instead of 'ass' filter (more reliable for Windows paths)
        var filterArg = $"subtitles='{escapedAssPath}'";
        Log.Information("FFmpeg video filter: {FilterArg}", filterArg);

        await FFMpegArguments
            .FromFileInput(videoPath)
            .OutputToFile(outputPath, overwrite: true, options => options
                .WithCustomArgument($"-vf \"{filterArg}\"")
                .WithAudioCodec("copy") // Copy audio stream without re-encoding
                .WithVideoCodec("libx264") // H.264 encoding
                .WithConstantRateFactor(23) // Quality (lower = better, 23 is default)
                .WithFastStart()) // Optimize for streaming
            .CancellableThrough(cancellationToken)
            .ProcessAsynchronously();

        Log.Information("Video rendering complete: {OutputPath}", outputPath);
        return outputPath;
    }
}

/// <summary>
/// Represents a transcribed segment with word-level timing for karaoke.
/// </summary>
public class KaraokeSegment
{
    public TimeSpan Start { get; set; }
    public TimeSpan End { get; set; }
    public string Text { get; set; } = string.Empty;
    public List<KaraokeWord> Words { get; set; } = new();
}

/// <summary>
/// Represents a single word with timing information for karaoke.
/// </summary>
public class KaraokeWord
{
    public TimeSpan Start { get; set; }
    public TimeSpan End { get; set; }
    public string Word { get; set; } = string.Empty;
}
