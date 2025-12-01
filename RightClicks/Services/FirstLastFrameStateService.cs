using Serilog;
using System;
using System.IO;

namespace RightClicks.Services;

/// <summary>
/// Service to manage stateful First+Last Frame operations (two-image pairing).
/// Tracks pending image pairs with a 20-second timeout window.
/// </summary>
public class FirstLastFrameStateService
{
    private static readonly object _lock = new object();
    private static ImagePair? _pendingPair = null;
    private static readonly TimeSpan PairingTimeout = TimeSpan.FromSeconds(20);

    /// <summary>
    /// Represents a pending image pair (first click).
    /// </summary>
    private class ImagePair
    {
        public string FilePath { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Result of attempting to pair two images.
    /// </summary>
    public class ImagePairResult
    {
        public bool IsReadyToProcess { get; set; }
        public string? FirstImagePath { get; set; }
        public string? LastImagePath { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Attempts to pair an image with a pending operation.
    /// Returns a result indicating whether processing should proceed.
    /// </summary>
    public static ImagePairResult TryPairImage(string filePath)
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;

            Log.Information("FirstLastFrame: Attempting to pair image: {FilePath}", filePath);

            // Check if there's a pending pair
            if (_pendingPair == null)
            {
                // First click - store the image
                _pendingPair = new ImagePair
                {
                    FilePath = filePath,
                    Timestamp = now
                };

                Log.Information("FirstLastFrame: First image stored: {FilePath}", filePath);
                return new ImagePairResult
                {
                    IsReadyToProcess = false,
                    Message = $"First image selected: {Path.GetFileName(filePath)}. Now select the last frame image within 20 seconds."
                };
            }

            // Check if the pending pair has timed out
            var elapsed = now - _pendingPair.Timestamp;
            if (elapsed > PairingTimeout)
            {
                Log.Warning("FirstLastFrame: Pending pair timed out after {Elapsed:F1} seconds. Resetting state.", elapsed.TotalSeconds);
                
                // Reset and store the new image as the first click
                _pendingPair = new ImagePair
                {
                    FilePath = filePath,
                    Timestamp = now
                };

                return new ImagePairResult
                {
                    IsReadyToProcess = false,
                    Message = $"Previous pairing timed out. First image selected: {Path.GetFileName(filePath)}. Now select the last frame image within 20 seconds."
                };
            }

            // Check if the same file was clicked twice
            if (_pendingPair.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase))
            {
                Log.Information("FirstLastFrame: Same image clicked twice. Replacing pending image.");
                
                // Replace the pending image with the new one (user might have edited it)
                _pendingPair = new ImagePair
                {
                    FilePath = filePath,
                    Timestamp = now
                };

                return new ImagePairResult
                {
                    IsReadyToProcess = false,
                    Message = $"Same image selected. Replaced first image. Now select a different image for the last frame within 20 seconds."
                };
            }

            // We have a valid image pair!
            Log.Information("FirstLastFrame: Valid pair found! First: {FirstImage}, Last: {LastImage}", 
                _pendingPair.FilePath, filePath);

            var result = new ImagePairResult
            {
                IsReadyToProcess = true,
                FirstImagePath = _pendingPair.FilePath,
                LastImagePath = filePath,
                Message = "First and last frame images paired successfully. Opening configuration window..."
            };

            // Clear the pending pair
            _pendingPair = null;

            return result;
        }
    }

    /// <summary>
    /// Clears any pending image pair (for testing or manual reset).
    /// </summary>
    public static void ClearPendingPair()
    {
        lock (_lock)
        {
            if (_pendingPair != null)
            {
                Log.Information("FirstLastFrame: Clearing pending pair: {FilePath}", _pendingPair.FilePath);
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
                return "No pending image pair.";

            var elapsed = DateTime.UtcNow - _pendingPair.Timestamp;
            var remaining = PairingTimeout - elapsed;

            return $"Pending: '{Path.GetFileName(_pendingPair.FilePath)}' " +
                   $"(Elapsed: {elapsed.TotalSeconds:F1}s, Remaining: {remaining.TotalSeconds:F1}s)";
        }
    }
}

