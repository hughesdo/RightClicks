using FFMpegCore;
using RightClicks.Models;
using RightClicks.Models.FalAi;
using RightClicks.Services;
using RightClicks.Windows;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace RightClicks.Features.FalType;

/// <summary>
/// The settings the user submitted in the shared config window, carried on the Job until it runs.
/// </summary>
/// <param name="ModelId">The full fal endpoint the user chose.</param>
/// <param name="OutputType">"video" or "image" — decides how the result is saved.</param>
/// <param name="FileInputs">fal parameter name -> local file path OR an http(s) URL. Local paths are uploaded first.</param>
/// <param name="Parameters">Non-file values keyed by the model's fal parameter names (may include "loras").</param>
/// <param name="ReattachAudioFrom">File-input param whose audio to mux onto video output, or null.</param>
public record FalTypeConfiguration(
    string ModelId,
    string OutputType,
    Dictionary<string, string> FileInputs,
    Dictionary<string, object> Parameters,
    string? ReattachAudioFrom);

/// <summary>
/// Shared base for ALL the new fal.ai category types (Audio-to-Video, Video-to-Video, Swaps,
/// Text-to-Video). The workflow is identical across categories:
/// 1. Open the shared config window (renders from the category's JSON model files).
/// 2. Upload any local file inputs to fal storage (self-expiring, no cleanup).
/// 3. Call the fal QUEUE endpoint (video- or image-output).
/// 4. Save the result next to the source, auto-versioned; re-encode video X-safe.
///
/// A concrete feature is a tiny subclass supplying the menu label + which category folder + default
/// model. Adding a model to an existing category is just a new JSON file + one such subclass.
/// Image-to-Video does NOT use this base — it stays frozen on its own path.
/// </summary>
public abstract class FalTypeFeatureBase : IConfigurableFeature
{
    // Per-model identity (thin subclasses supply these).
    public abstract string Id { get; }
    public abstract string DisplayName { get; }
    public abstract string Description { get; }
    public abstract string[] SupportedExtensions { get; }

    public bool IsCloudBased => true;

    /// <summary>Folder (next to the exe) holding this category's model JSON files, e.g. "AudioToVideo".</summary>
    protected abstract string CategoryFolder { get; }

    /// <summary>Human title for the config window, e.g. "Audio to Video".</summary>
    protected abstract string CategoryTitle { get; }

    /// <summary>The model the window opens on. The user can switch; their choice wins.</summary>
    protected abstract string DefaultModelId { get; }

    /// <summary>Short tag used in output file names, e.g. "a2v" -> song_a2v_01.mp4.</summary>
    protected abstract string OutputSuffix { get; }

    public object? Configure(string fullPath)
    {
        Log.Information("=== {Category}: collecting configuration ===", CategoryTitle);
        Log.Information("Input file: {FilePath}", fullPath);

        if (!File.Exists(fullPath))
        {
            Log.Error("File not found: {FilePath}", fullPath);
            return null;
        }

        var window = new FalTypeConfigWindow(CategoryFolder, CategoryTitle, DefaultModelId, fullPath);
        if (window.ShowDialog() != true)
        {
            Log.Information("User cancelled {Category} configuration - no job queued", CategoryTitle);
            return null;
        }

        if (string.IsNullOrWhiteSpace(window.SelectedModelId))
        {
            Log.Error("No model selected in {Category} config window", CategoryTitle);
            return null;
        }

        Log.Information("{Category} config collected: model={ModelId}, {FileCount} file inputs, {ParamCount} params",
            CategoryTitle, window.SelectedModelId, window.FileInputs.Count, window.Parameters.Count);

        return new FalTypeConfiguration(
            window.SelectedModelId,
            window.SelectedOutputType,
            window.FileInputs,
            window.Parameters,
            window.ReattachAudioFrom);
    }

    public Task<FeatureResult> ExecuteAsync(string fullPath, CancellationToken cancellationToken)
        => ExecuteAsync(fullPath, null, cancellationToken);

    public async Task<FeatureResult> ExecuteAsync(string fullPath, object? configuration, CancellationToken cancellationToken)
    {
        var startTime = DateTime.Now;
        Log.Information("=== {DisplayName} Started ===", DisplayName);

        if (!File.Exists(fullPath))
            return Fail(startTime, "File not found");

        if (configuration is not FalTypeConfiguration config)
            return Fail(startTime, "Missing configuration - Configure() should have run first");

        var apiKey = Environment.GetEnvironmentVariable("FAL_KEY", EnvironmentVariableTarget.User);
        if (string.IsNullOrWhiteSpace(apiKey))
            return Fail(startTime, "FAL_KEY environment variable not set.");

        var cloudinary = GetCloudinaryConfig();
        if (cloudinary == null)
            return Fail(startTime, "Cloudinary not configured. Check config.json and CLOUDINARY_* environment variables.");

        // Track everything we push to Cloudinary so we can delete it afterward (success or failure).
        var uploaded = new List<(string PublicId, string ResourceType)>();

        try
        {
            // 1. Upload local file inputs to Cloudinary (skip anything already a URL).
            var payload = new Dictionary<string, object>();
            try
            {
                using var storage = new CloudinaryStorageService(cloudinary.Value.CloudName, cloudinary.Value.ApiKey, cloudinary.Value.ApiSecret);
                foreach (var (param, value) in config.FileInputs)
                {
                    if (string.IsNullOrWhiteSpace(value))
                        continue;

                    if (value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                        value.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    {
                        payload[param] = value;
                        Log.Information("Input '{Param}' is already a URL: {Url}", param, value);
                        continue;
                    }

                    if (!File.Exists(value))
                        return Fail(startTime, $"Input file for '{param}' not found: {value}");

                    Log.Information("Uploading input '{Param}' from {Path}...", param, value);
                    var up = await storage.UploadFileAsync(value, cancellationToken);
                    uploaded.Add((up.PublicId, up.ResourceType));
                    payload[param] = up.SecureUrl;
                    Log.Information("Input '{Param}' hosted at {Url}", param, up.SecureUrl);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to upload input file(s) to Cloudinary");
                return Fail(startTime, $"Failed to upload input: {ex.Message}", ex);
            }

            // 2. Merge non-file parameters (omit blanks - fal treats "" as a type error on string fields).
            foreach (var (key, val) in config.Parameters)
            {
                if (val == null) continue;
                if (val is string s && string.IsNullOrWhiteSpace(s)) continue;
                payload[key] = val;
            }

            // 3. Call the fal queue endpoint.
            FalTypeResult result;
            try
            {
                using var fal = new FalAiQueueService(apiKey, config.ModelId);
                result = await fal.RunAsync(payload, cancellationToken);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "fal call failed for {ModelId}", config.ModelId);
                return Fail(startTime, $"Generation failed: {ex.Message}", ex);
            }

            var outputUrl = result.PrimaryOutput!.Url;

            // 4. Save the output next to the source, auto-versioned.
            var isImage = string.Equals(config.OutputType, "image", StringComparison.OrdinalIgnoreCase);
            try
            {
                var directory = Path.GetDirectoryName(fullPath) ?? "";
                var baseName = Path.GetFileNameWithoutExtension(fullPath);

                if (isImage)
                {
                    var ext = GuessImageExtension(outputUrl);
                    var outputPath = NextVersionedPath(directory, baseName, OutputSuffix, ext);
                    await DownloadToFileAsync(outputUrl, outputPath, cancellationToken);
                    return Done(startTime, outputPath);
                }
                else
                {
                    // Download the (silent) LTX video.
                    var rawPath = Path.Combine(Path.GetTempPath(), $"rc_{Guid.NewGuid():N}.mp4");
                    await DownloadToFileAsync(outputUrl, rawPath, cancellationToken);

                    var outputPath = NextVersionedPath(directory, baseName, OutputSuffix, ".mp4");

                    // If the model says so, mux the source song back on (X-safe AAC 48kHz); else plain re-encode.
                    var audioPath = ResolveReattachAudioPath(config);
                    if (audioPath != null)
                        await MuxAudioXSafeAsync(rawPath, audioPath, outputPath, cancellationToken);
                    else
                        await ReencodeXSafeAsync(rawPath, outputPath, cancellationToken);

                    TryDelete(rawPath);
                    return Done(startTime, outputPath);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to save output");
                return Fail(startTime, $"Failed to save output: {ex.Message}", ex);
            }
        }
        finally
        {
            // Clean up hosted inputs regardless of outcome.
            if (uploaded.Count > 0)
            {
                try
                {
                    using var storage = new CloudinaryStorageService(cloudinary.Value.CloudName, cloudinary.Value.ApiKey, cloudinary.Value.ApiSecret);
                    foreach (var (publicId, resourceType) in uploaded)
                        await storage.DeleteFileAsync(publicId, resourceType, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Failed to clean up one or more Cloudinary inputs (non-fatal)");
                }
            }
        }
    }

    /// <summary>
    /// Cloudinary config from config.json + CLOUDINARY_* environment variables. Null if unconfigured.
    /// Same source Image-to-Video uses, so the proven preset applies.
    /// </summary>
    private static (string CloudName, string ApiKey, string ApiSecret)? GetCloudinaryConfig()
    {
        var config = ConfigurationService.LoadConfig();
        if (config?.Cloudinary == null || string.IsNullOrWhiteSpace(config.Cloudinary.CloudName))
            return null;

        var key = ConfigurationService.ResolveApiKey(config.Cloudinary.ApiKeyEnvVar);
        var secret = ConfigurationService.ResolveApiKey(config.Cloudinary.ApiSecretEnvVar);
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(secret))
            return null;

        return (config.Cloudinary.CloudName, key, secret);
    }

    /// <summary>
    /// The local audio file whose track should be muxed onto the video output, or null if not
    /// applicable (no reattach param, input wasn't local, or file missing).
    /// </summary>
    private static string? ResolveReattachAudioPath(FalTypeConfiguration config)
    {
        if (string.IsNullOrEmpty(config.ReattachAudioFrom))
            return null;
        if (!config.FileInputs.TryGetValue(config.ReattachAudioFrom, out var path))
            return null;
        return File.Exists(path) ? path : null; // http URLs / missing files fall back to plain re-encode
    }

    /// <summary>
    /// Mux the source audio onto the (silent) LTX video with an X-safe audio codec. Video is copied,
    /// audio is AAC 48kHz, trimmed to the shorter stream so it matches the clip length.
    /// ffmpeg -i video -i audio -map 0:v:0 -map 1:a:0 -c:v copy -c:a aac -b:a 192k -ar 48000 -shortest -movflags +faststart
    /// </summary>
    private static async Task MuxAudioXSafeAsync(string videoPath, string audioPath, string output, CancellationToken ct)
    {
        Log.Information("Muxing source audio onto video (X-safe AAC 48kHz): {Video} + {Audio} -> {Output}", videoPath, audioPath, output);
        var ok = await FFMpegArguments
            .FromFileInput(videoPath)
            .AddFileInput(audioPath)
            .OutputToFile(output, overwrite: true, o => o
                .WithVideoCodec("copy")
                .WithAudioCodec("aac")
                .WithAudioBitrate(192)
                .WithCustomArgument("-map 0:v:0 -map 1:a:0 -ar 48000 -shortest -movflags +faststart"))
            .CancellableThrough(ct)
            .ProcessAsynchronously();

        if (!ok || !File.Exists(output))
            throw new InvalidOperationException("ffmpeg audio mux failed.");
    }

    /// <summary>
    /// X-compatible re-encode: keep video stream, force AAC 48kHz audio, faststart for streaming.
    /// Matches: ffmpeg -i in.mp4 -c:v copy -c:a aac -b:a 192k -ar 48000 -movflags +faststart out.mp4
    /// </summary>
    private static async Task ReencodeXSafeAsync(string input, string output, CancellationToken ct)
    {
        Log.Information("Re-encoding X-safe: {Input} -> {Output}", input, output);
        var ok = await FFMpegArguments
            .FromFileInput(input)
            .OutputToFile(output, overwrite: true, o => o
                .WithVideoCodec("copy")
                .WithAudioCodec("aac")
                .WithAudioBitrate(192)
                .WithCustomArgument("-ar 48000 -movflags +faststart"))
            .CancellableThrough(ct)
            .ProcessAsynchronously();

        if (!ok || !File.Exists(output))
            throw new InvalidOperationException("ffmpeg X-safe re-encode failed.");
    }

    private static async Task DownloadToFileAsync(string url, string destPath, CancellationToken ct)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
        var bytes = await http.GetByteArrayAsync(url, ct);
        await File.WriteAllBytesAsync(destPath, bytes, ct);
        Log.Information("Saved {Path} ({KB:F1} KB)", destPath, bytes.Length / 1024.0);
    }

    /// <summary>First free "{base}_{suffix}_NN{ext}" in the directory, starting at 01. Never overwrites.</summary>
    private static string NextVersionedPath(string directory, string baseName, string suffix, string ext)
    {
        for (int n = 1; n < 1000; n++)
        {
            var candidate = Path.Combine(directory, $"{baseName}_{suffix}_{n:D2}{ext}");
            if (!File.Exists(candidate))
                return candidate;
        }
        // Extremely unlikely fallback.
        return Path.Combine(directory, $"{baseName}_{suffix}_{Guid.NewGuid():N}{ext}");
    }

    private static string GuessImageExtension(string url)
    {
        var ext = Path.GetExtension(new Uri(url).AbsolutePath).ToLowerInvariant();
        return ext is ".png" or ".jpg" or ".jpeg" or ".webp" ? ext : ".png";
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { /* best effort */ }
    }

    private FeatureResult Done(DateTime start, string outputPath)
    {
        var ms = (long)(DateTime.Now - start).TotalMilliseconds;
        Log.Information("=== {DisplayName} Completed === Output: {Path} ({Sec:F1}s)", DisplayName, outputPath, ms / 1000.0);
        return FeatureResult.CreateSuccess($"Generated: {Path.GetFileName(outputPath)}", outputPath, ms);
    }

    private FeatureResult Fail(DateTime start, string message, Exception? ex = null)
    {
        var ms = (long)(DateTime.Now - start).TotalMilliseconds;
        Log.Error("{DisplayName} failed: {Message}", DisplayName, message);
        return FeatureResult.CreateFailure(message, ex, ms);
    }
}
