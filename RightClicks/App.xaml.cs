using System.IO;
using System.Threading;
using System.Windows;
using RightClicks.Services;
using Serilog;

namespace RightClicks;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private bool _isTestMode = false;
    private bool _clearLogs = false;
    private bool _clearTestLogsOnly = false;
    private string? _featureId = null;
    private string? _filePath = null;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Parse command line arguments
        ParseCommandLineArguments(e.Args);

        // Handle --clear-logs flag
        if (_clearLogs)
        {
            // Configure minimal logging just to log the clear operation
            LoggingService.ConfigureLogging(isTestMode: false);

            int deletedCount = LoggingService.ClearLogs(testLogsOnly: _clearTestLogsOnly);
            Console.WriteLine($"Cleared {deletedCount} log files.");

            LoggingService.CloseLogger();
            Shutdown();
            return;
        }

        // Configure logging (before loading config so we can log config loading)
        LoggingService.ConfigureLogging(isTestMode: _isTestMode);

        Log.Information("Application started with {ArgCount} arguments", e.Args.Length);
        if (e.Args.Length > 0)
        {
            Log.Information("Arguments: {Args}", string.Join(" ", e.Args));
        }

        // Load configuration
        var config = ConfigurationService.LoadConfig();
        Log.Information("Configuration loaded successfully");

        // Discover features via reflection
        FeatureDiscoveryService.DiscoverFeatures();
        FeatureDiscoveryService.LogDiscoveredFeatures(config);

        // Handle feature execution via CLI
        if (!string.IsNullOrEmpty(_featureId) && !string.IsNullOrEmpty(_filePath))
        {
            // Run async method synchronously using Task.Run to avoid UI thread deadlock
            Task.Run(async () => await ExecuteFeatureAsync(_featureId, _filePath)).GetAwaiter().GetResult();
            LoggingService.CloseLogger();
            Shutdown(0);
            return;
        }

        // TODO: Show main window (UI implementation in Phase 3)
        Log.Information("No CLI feature execution requested. Exiting (UI not yet implemented).");
        LoggingService.CloseLogger();
        Shutdown(0);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        LoggingService.CloseLogger();
        base.OnExit(e);
    }

    private void ParseCommandLineArguments(string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLowerInvariant())
            {
                case "--test-mode":
                    _isTestMode = true;
                    break;

                case "--clear-logs":
                    _clearLogs = true;
                    // Check if next argument is --test-only
                    if (i + 1 < args.Length && args[i + 1].ToLowerInvariant() == "--test-only")
                    {
                        _clearTestLogsOnly = true;
                        i++; // Skip next argument
                    }
                    break;

                case "--feature":
                    if (i + 1 < args.Length)
                    {
                        _featureId = args[i + 1];
                        i++; // Skip next argument
                    }
                    break;

                case "--file":
                    if (i + 1 < args.Length)
                    {
                        _filePath = args[i + 1];
                        i++; // Skip next argument
                    }
                    break;
            }
        }
    }

    private async Task ExecuteFeatureAsync(string featureId, string filePath)
    {
        Log.Information("=== CLI Feature Execution ===");
        Log.Information("Feature ID: {FeatureId}", featureId);
        Log.Information("File Path: {FilePath}", filePath);

        // Validate file exists
        if (!File.Exists(filePath))
        {
            Log.Error("File not found: {FilePath}", filePath);
            Console.WriteLine($"ERROR: File not found: {filePath}");
            return;
        }

        // Look up feature
        var feature = FeatureDiscoveryService.GetFeatureById(featureId);
        if (feature == null)
        {
            Log.Error("Feature not found: {FeatureId}", featureId);
            Console.WriteLine($"ERROR: Feature not found: {featureId}");
            Console.WriteLine("Available features:");
            foreach (var f in FeatureDiscoveryService.GetFeatures())
            {
                Console.WriteLine($"  - {f.Id}: {f.DisplayName}");
            }
            return;
        }

        Log.Information("Found feature: {DisplayName}", feature.DisplayName);

        // Check if feature supports this file extension
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        if (!feature.SupportedExtensions.Any(ext => ext.Equals(extension, StringComparison.OrdinalIgnoreCase)))
        {
            Log.Warning("Feature {FeatureId} does not support extension {Extension}", featureId, extension);
            Log.Information("Supported extensions: {Extensions}", string.Join(", ", feature.SupportedExtensions));
            Console.WriteLine($"WARNING: Feature '{feature.DisplayName}' does not support {extension} files");
            Console.WriteLine($"Supported extensions: {string.Join(", ", feature.SupportedExtensions)}");
        }

        // Execute feature
        Log.Information("Executing feature...");
        var startTime = DateTime.Now;

        try
        {
            using var cts = new CancellationTokenSource();
            var result = await feature.ExecuteAsync(filePath, cts.Token);

            var duration = (DateTime.Now - startTime).TotalSeconds;

            if (result.Success)
            {
                Log.Information("Feature execution completed successfully in {Duration:F2}s", duration);
                Log.Information("Result: {Message}", result.Message);
                if (!string.IsNullOrEmpty(result.OutputFilePath))
                {
                    Log.Information("Output file: {OutputFilePath}", result.OutputFilePath);
                    Console.WriteLine($"SUCCESS: {result.Message}");
                    Console.WriteLine($"Output file: {result.OutputFilePath}");
                }
            }
            else
            {
                Log.Error("Feature execution failed: {Message}", result.Message);
                if (result.Exception != null)
                {
                    Log.Error(result.Exception, "Exception details");
                }
                Console.WriteLine($"FAILED: {result.Message}");
            }
        }
        catch (Exception ex)
        {
            var duration = (DateTime.Now - startTime).TotalSeconds;
            Log.Error(ex, "Unhandled exception during feature execution after {Duration:F2}s", duration);
            Console.WriteLine($"ERROR: Unhandled exception: {ex.Message}");
        }
    }
}

