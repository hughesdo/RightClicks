using RightClicks.Models;
using Serilog;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace RightClicks.Features.Text
{
    /// <summary>
    /// Feature to copy text file contents to clipboard.
    /// Supports: .txt, .md, .glsl, .frag, .sql, and other text-based files
    /// </summary>
    public class ContentToClipboardFeature : IFileFeature
    {
        public string Id => "ContentToClipboard";

        public string DisplayName => "Content to Clipboard";

        public string Description => "Copy file contents to clipboard";

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
            Log.Information("ContentToClipboardFeature: Starting execution for file: {FilePath}", filePath);

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

                // Read file contents
                Log.Information("Reading file contents...");
                string content = File.ReadAllText(fullPath);
                Log.Debug("File content length: {Length} characters", content.Length);

                // Copy to clipboard (must be done on STA thread)
                Log.Information("Copying to clipboard...");

                Exception? clipboardException = null;
                var thread = new Thread(() =>
                {
                    try
                    {
                        System.Windows.Clipboard.SetText(content);
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

                Log.Information("Content copied to clipboard successfully");

                var finalDuration = (long)(DateTime.Now - startTime).TotalMilliseconds;
                Log.Information("ContentToClipboardFeature: Completed successfully in {Duration}ms", finalDuration);

                return Task.FromResult(FeatureResult.CreateSuccess(
                    $"File contents copied to clipboard ({content.Length} characters)",
                    fullPath,
                    finalDuration
                ));
            }
            catch (OperationCanceledException)
            {
                Log.Warning("ContentToClipboardFeature: Operation cancelled");
                var duration = (long)(DateTime.Now - startTime).TotalMilliseconds;
                return Task.FromResult(FeatureResult.CreateFailure("Operation cancelled by user", null, duration));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ContentToClipboardFeature: Execution failed");
                var duration = (long)(DateTime.Now - startTime).TotalMilliseconds;
                return Task.FromResult(FeatureResult.CreateFailure($"Failed to copy to clipboard: {ex.Message}", ex, duration));
            }
        }
    }
}

