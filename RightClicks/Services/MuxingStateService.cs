using Serilog;
using System;
using System.IO;
using System.Linq;

namespace RightClicks.Services;

/// <summary>
/// Service to manage stateful muxing operations (audio + video pairing).
/// Tracks pending muxing pairs with a 30-second timeout window.
/// </summary>
public class MuxingStateService
{
    private static readonly object _lock = new object();
    private static MuxingPair? _pendingPair = null;
    private static readonly TimeSpan PairingTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Represents a pending muxing pair (first click).
    /// </summary>
    private class MuxingPair
    {
        public string FilePath { get; set; } = string.Empty;
        public MuxingFileType FileType { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// File type for muxing operations.
    /// </summary>
    public enum MuxingFileType
    {
        Audio,
        Video
    }

    /// <summary>
    /// Result of attempting to pair two files for muxing.
    /// </summary>
    public class MuxingPairResult
    {
        public bool IsReadyToMux { get; set; }
        public string? AudioFilePath { get; set; }
        public string? VideoFilePath { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Determines the file type based on extension.
    /// </summary>
    public static MuxingFileType GetFileType(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        
        var audioExtensions = new[] { ".mp3", ".wav", ".aac", ".flac", ".m4a", ".ogg", ".opus", ".wma" };
        var videoExtensions = new[] { ".mp4", ".mov", ".mkv", ".avi", ".wmv", ".flv", ".webm", ".mpeg", ".mpg", ".m4v" };

        if (audioExtensions.Contains(extension))
            return MuxingFileType.Audio;
        
        if (videoExtensions.Contains(extension))
            return MuxingFileType.Video;

        throw new ArgumentException($"Unsupported file type: {extension}");
    }

    /// <summary>
    /// Attempts to pair a file with a pending muxing operation.
    /// Returns a result indicating whether muxing should proceed.
    /// </summary>
    public static MuxingPairResult TryPairFile(string filePath)
    {
        lock (_lock)
        {
            var fileType = GetFileType(filePath);
            var now = DateTime.UtcNow;

            Log.Information("Muxing: Attempting to pair file: {FilePath} (Type: {FileType})", filePath, fileType);

            // Check if there's a pending pair
            if (_pendingPair == null)
            {
                // First click - store the file
                _pendingPair = new MuxingPair
                {
                    FilePath = filePath,
                    FileType = fileType,
                    Timestamp = now
                };

                Log.Information("Muxing: First click stored - {FileType} file: {FilePath}", fileType, filePath);
                return new MuxingPairResult
                {
                    IsReadyToMux = false,
                    Message = $"First file selected ({fileType}). Now select a {(fileType == MuxingFileType.Audio ? "video" : "audio")} file within 30 seconds."
                };
            }

            // Check if the pending pair has timed out
            var elapsed = now - _pendingPair.Timestamp;
            if (elapsed > PairingTimeout)
            {
                Log.Warning("Muxing: Pending pair timed out after {Elapsed:F1} seconds. Resetting state.", elapsed.TotalSeconds);
                
                // Reset and store the new file as the first click
                _pendingPair = new MuxingPair
                {
                    FilePath = filePath,
                    FileType = fileType,
                    Timestamp = now
                };

                return new MuxingPairResult
                {
                    IsReadyToMux = false,
                    Message = $"Previous pairing timed out. First file selected ({fileType}). Now select a {(fileType == MuxingFileType.Audio ? "video" : "audio")} file within 30 seconds."
                };
            }

            // Check if the file types match (both audio or both video)
            if (_pendingPair.FileType == fileType)
            {
                Log.Information("Muxing: Same file type clicked twice ({FileType}). Replacing pending file.", fileType);
                
                // Replace the pending file with the new one
                _pendingPair = new MuxingPair
                {
                    FilePath = filePath,
                    FileType = fileType,
                    Timestamp = now
                };

                return new MuxingPairResult
                {
                    IsReadyToMux = false,
                    Message = $"Same file type selected. Replaced pending {fileType} file. Now select a {(fileType == MuxingFileType.Audio ? "video" : "audio")} file within 30 seconds."
                };
            }

            // We have a valid audio + video pair!
            Log.Information("Muxing: Valid pair found! Audio: {AudioFile}, Video: {VideoFile}", 
                _pendingPair.FileType == MuxingFileType.Audio ? _pendingPair.FilePath : filePath,
                _pendingPair.FileType == MuxingFileType.Video ? _pendingPair.FilePath : filePath);

            var result = new MuxingPairResult
            {
                IsReadyToMux = true,
                AudioFilePath = _pendingPair.FileType == MuxingFileType.Audio ? _pendingPair.FilePath : filePath,
                VideoFilePath = _pendingPair.FileType == MuxingFileType.Video ? _pendingPair.FilePath : filePath,
                Message = "Audio and video files paired successfully. Starting muxing operation..."
            };

            // Clear the pending pair
            _pendingPair = null;

            return result;
        }
    }

    /// <summary>
    /// Clears any pending muxing pair (for testing or manual reset).
    /// </summary>
    public static void ClearPendingPair()
    {
        lock (_lock)
        {
            if (_pendingPair != null)
            {
                Log.Information("Muxing: Clearing pending pair: {FilePath} ({FileType})", 
                    _pendingPair.FilePath, _pendingPair.FileType);
                _pendingPair = null;
            }
        }
    }

    /// <summary>
    /// Gets information about the current pending pair (for debugging/testing).
    /// </summary>
    public static string GetPendingPairInfo()
    {
        lock (_lock)
        {
            if (_pendingPair == null)
                return "No pending muxing pair.";

            var elapsed = DateTime.UtcNow - _pendingPair.Timestamp;
            var remaining = PairingTimeout - elapsed;

            return $"Pending: {_pendingPair.FileType} file '{Path.GetFileName(_pendingPair.FilePath)}' " +
                   $"(Elapsed: {elapsed.TotalSeconds:F1}s, Remaining: {remaining.TotalSeconds:F1}s)";
        }
    }
}

