using RightClicks.Models;
using Serilog;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace RightClicks.Features.Text
{
    /// <summary>
    /// Feature to paste clipboard contents to a file.
    /// ONLY works if the target file is empty (0 bytes).
    /// Supports: .txt, .md, .glsl, .frag, .sql, and other text-based files
    /// </summary>
    public class ClipboardToFileFeature : IFileFeature
    {
        public string Id => "ClipboardToFile";

        public string DisplayName => "Clipboard to File";

        public string Description => "Paste clipboard contents to empty file";

        public string[] SupportedExtensions => new[] 
        { 
            ".txt", ".md", ".glsl", ".frag", ".sql", 
            ".cs", ".js", ".ts", ".json", ".xml", ".html", ".css",
            ".py", ".java", ".cpp", ".c", ".h", ".hpp",
            ".sh", ".bat", ".ps1", ".yaml", ".yml", ".ini", ".cfg"
        };

        public bool IsCloudBased => false;

        public Task<FeatureResult> ExecuteAsync(string filePath, CancellationToken cancellationToken)
        {
            var startTime = DateTime.Now;
            Log.Information("ClipboardToFileFeature: Starting execution for file: {FilePath}", filePath);

            try
            {
                // Resolve full path
                var fullPath = Path.GetFullPath(filePath);
                Log.Debug("Full path resolved: {FullPath}", fullPath);

                if (!File.Exists(fullPath))
                {
                    Log.Error("File not found: {FullPath}", fullPath);
                    var duration = (long)(DateTime.Now - startTime).TotalMilliseconds;
                    return Task.FromResult(FeatureResult.CreateFailure($"File not found: {fullPath}", null, duration));
                }

                // Check if file is empty (0 bytes)
                var fileInfo = new FileInfo(fullPath);
                if (fileInfo.Length > 0)
                {
                    Log.Warning("File is not empty: {FullPath} ({Size} bytes)", fullPath, fileInfo.Length);
                    var duration = (long)(DateTime.Now - startTime).TotalMilliseconds;
                    return Task.FromResult(FeatureResult.CreateFailure(
                        $"File is not empty ({fileInfo.Length} bytes). This feature only works on empty files.",
                        null,
                        duration
                    ));
                }

                Log.Information("File is empty, proceeding to paste clipboard contents");

                // Get clipboard text (must be done on STA thread)
                string? clipboardText = null;
                Exception? clipboardException = null;
                var thread = new Thread(() =>
                {
                    try
                    {
                        if (!System.Windows.Clipboard.ContainsText())
                        {
                            clipboardText = null;
                        }
                        else
                        {
                            clipboardText = System.Windows.Clipboard.GetText();
                        }
                    }
                    catch (Exception ex)
                    {
                        clipboardException = ex;
                    }
                });
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                thread.Join();

                if (clipboardException != null)
                {
                    throw clipboardException;
                }

                if (string.IsNullOrEmpty(clipboardText))
                {
                    Log.Warning("Clipboard does not contain text");
                    var duration = (long)(DateTime.Now - startTime).TotalMilliseconds;
                    return Task.FromResult(FeatureResult.CreateFailure("Clipboard does not contain text", null, duration));
                }

                Log.Debug("Clipboard content length: {Length} characters", clipboardText.Length);

                // Write clipboard contents to file
                Log.Information("Writing clipboard contents to file...");
                File.WriteAllText(fullPath, clipboardText);
                Log.Information("Clipboard contents written to file successfully");

                var finalDuration = (long)(DateTime.Now - startTime).TotalMilliseconds;
                Log.Information("ClipboardToFileFeature: Completed successfully in {Duration}ms", finalDuration);

                return Task.FromResult(FeatureResult.CreateSuccess(
                    $"Clipboard contents pasted to file ({clipboardText.Length} characters)",
                    fullPath,
                    finalDuration
                ));
            }
            catch (OperationCanceledException)
            {
                Log.Warning("ClipboardToFileFeature: Operation cancelled");
                var duration = (long)(DateTime.Now - startTime).TotalMilliseconds;
                return Task.FromResult(FeatureResult.CreateFailure("Operation cancelled by user", null, duration));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ClipboardToFileFeature: Execution failed");
                var duration = (long)(DateTime.Now - startTime).TotalMilliseconds;
                return Task.FromResult(FeatureResult.CreateFailure($"Failed to paste from clipboard: {ex.Message}", ex, duration));
            }
        }
    }
}

