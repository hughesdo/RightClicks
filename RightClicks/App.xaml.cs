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

        // TODO: Parse other CLI arguments (--feature, --file) in future tasks
        // For now, just show the main window
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

                // TODO: Add --feature and --file arguments in Phase 2
            }
        }
    }
}

