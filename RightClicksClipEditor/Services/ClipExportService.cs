using FFMpegCore;
using FFMpegCore.Arguments;
using FFMpegCore.Enums;
using RightClicksClipEditor.Models;
using Serilog;

namespace RightClicksClipEditor.Services;

/// <summary>
/// Handles clip export using FFmpeg
/// </summary>
public static class ClipExportService
{
    public static async Task<bool> ExportVideoClipAsync(
        string sourceFile,
        string outputFile,
        TimeSpan startTime,
        TimeSpan duration,
        ExportSettings settings,
        CancellationToken ct = default)
    {
        try
        {
            Log.Information("Exporting video clip: {Source} -> {Output}", sourceFile, outputFile);
            Log.Information("Start: {Start}, Duration: {Duration}", startTime, duration);
            Log.Information("Settings: StreamCopy={StreamCopy}, Codec={Codec}, Quality={Quality}",
                settings.UseStreamCopy, settings.VideoCodec, settings.Quality);
            
            if (settings.UseStreamCopy)
            {
                return await ExportVideoStreamCopyAsync(sourceFile, outputFile, startTime, duration, ct);
            }
            else
            {
                return await ExportVideoReencodeAsync(sourceFile, outputFile, startTime, duration, settings, ct);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to export video clip");
            return false;
        }
    }
    
    private static async Task<bool> ExportVideoReencodeAsync(
        string sourceFile,
        string outputFile,
        TimeSpan startTime,
        TimeSpan duration,
        ExportSettings settings,
        CancellationToken ct)
    {
        var success = await FFMpegArguments
            .FromFileInput(sourceFile, verifyExists: true, options => options
                .Seek(startTime))
            .OutputToFile(outputFile, overwrite: true, options => options
                .WithDuration(duration)
                .WithVideoCodec(settings.VideoCodec ?? "libx264")
                .WithConstantRateFactor(settings.Quality)
                .WithAudioCodec(settings.AudioCodec ?? "aac")
                .WithAudioBitrate(settings.AudioBitrate)
                .ForceFormat(settings.OutputFormat ?? "mp4"))
            .CancellableThrough(ct)
            .ProcessAsynchronously();
        
        Log.Information("Video export {Result}: {Output}", success ? "succeeded" : "failed", outputFile);
        return success;
    }
    
    private static async Task<bool> ExportVideoStreamCopyAsync(
        string sourceFile,
        string outputFile,
        TimeSpan startTime,
        TimeSpan duration,
        CancellationToken ct)
    {
        var success = await FFMpegArguments
            .FromFileInput(sourceFile, verifyExists: true, options => options
                .Seek(startTime))
            .OutputToFile(outputFile, overwrite: true, options => options
                .WithDuration(duration)
                .CopyChannel(Channel.Both)
                .ForceFormat("mp4"))
            .CancellableThrough(ct)
            .ProcessAsynchronously();
        
        Log.Information("Video stream copy {Result}: {Output}", success ? "succeeded" : "failed", outputFile);
        return success;
    }
    
    public static async Task<bool> ExportAudioClipAsync(
        string sourceFile,
        string outputFile,
        TimeSpan startTime,
        TimeSpan duration,
        ExportSettings settings,
        CancellationToken ct = default)
    {
        try
        {
            Log.Information("Exporting audio clip: {Source} -> {Output}", sourceFile, outputFile);
            Log.Information("Start: {Start}, Duration: {Duration}", startTime, duration);
            Log.Information("Settings: Codec={Codec}, Bitrate={Bitrate}",
                settings.AudioCodec, settings.AudioBitrate);
            
            var success = await FFMpegArguments
                .FromFileInput(sourceFile, verifyExists: true, options => options
                    .Seek(startTime))
                .OutputToFile(outputFile, overwrite: true, options => options
                    .WithDuration(duration)
                    .WithAudioCodec(settings.AudioCodec ?? "libmp3lame")
                    .WithAudioBitrate(settings.AudioBitrate)
                    .ForceFormat(settings.OutputFormat ?? "mp3"))
                .CancellableThrough(ct)
                .ProcessAsynchronously();
            
            Log.Information("Audio export {Result}: {Output}", success ? "succeeded" : "failed", outputFile);
            return success;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to export audio clip");
            return false;
        }
    }
}

