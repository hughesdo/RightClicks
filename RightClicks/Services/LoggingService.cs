using Serilog;
using Serilog.Events;
using System.IO;
using System.Reflection;

namespace RightClicks.Services;

/// <summary>
/// Service for configuring and managing application logging.
/// Uses Serilog with file and console sinks.
/// </summary>
public static class LoggingService
{
    private static string? _currentLogFilePath;

    /// <summary>
    /// Gets the path to the current log file.
    /// </summary>
    public static string? CurrentLogFilePath => _currentLogFilePath;

    /// <summary>
    /// Configure Serilog logging with file and console sinks.
    /// </summary>
    /// <param name="isTestMode">If true, creates an isolated timestamped log file for testing.</param>
    /// <param name="logRetentionDays">Number of days to retain log files (default: 7).</param>
    /// <param name="logLevel">Minimum log level (default: Verbose).</param>
    public static void ConfigureLogging(
        bool isTestMode = false,
        int logRetentionDays = 7,
        LogEventLevel logLevel = LogEventLevel.Verbose)
    {
        // Determine log directory
        var logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RightClicks",
            "logs"
        );

        // Create log directory if it doesn't exist
        Directory.CreateDirectory(logPath);

        // Determine log file name based on mode
        string logFileName = isTestMode
            ? $"RightClicks-TEST-{DateTime.Now:yyyyMMdd-HHmmss}.log"
            : $"RightClicks-.log";

        _currentLogFilePath = Path.Combine(logPath, logFileName);

        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(logLevel)
            .WriteTo.Console(
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
            )
            .WriteTo.File(
                _currentLogFilePath,
                rollingInterval: isTestMode ? RollingInterval.Infinite : RollingInterval.Day,
                retainedFileCountLimit: logRetentionDays,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
            )
            .CreateLogger();

        // Log startup information
        Log.Information("=== RightClicks Started ===");
        Log.Information("Version: {Version}", Assembly.GetExecutingAssembly().GetName().Version);
        Log.Information("Test Mode: {TestMode}", isTestMode);
        Log.Information("Log File: {LogFile}", _currentLogFilePath);
        Log.Information("Log Level: {LogLevel}", logLevel);
        Log.Information("Log Retention Days: {RetentionDays}", logRetentionDays);
    }

    /// <summary>
    /// Clear all log files from the logs directory.
    /// </summary>
    /// <param name="testLogsOnly">If true, only delete test logs (RightClicks-TEST-*.log).</param>
    /// <returns>Number of log files deleted.</returns>
    public static int ClearLogs(bool testLogsOnly = false)
    {
        var logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RightClicks",
            "logs"
        );

        if (!Directory.Exists(logPath))
        {
            Log.Information("Log directory does not exist: {LogPath}", logPath);
            return 0;
        }

        var searchPattern = testLogsOnly ? "RightClicks-TEST-*.log" : "RightClicks-*.log";
        var logFiles = Directory.GetFiles(logPath, searchPattern);

        int deletedCount = 0;
        foreach (var logFile in logFiles)
        {
            try
            {
                File.Delete(logFile);
                deletedCount++;
                Log.Information("Deleted log file: {LogFile}", logFile);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to delete log file: {LogFile}", logFile);
            }
        }

        Log.Information("Cleared {Count} log files (testLogsOnly: {TestLogsOnly})", deletedCount, testLogsOnly);
        return deletedCount;
    }

    /// <summary>
    /// Flush and close the logger.
    /// Call this before application exit.
    /// </summary>
    public static void CloseLogger()
    {
        Log.Information("=== RightClicks Shutting Down ===");
        Log.CloseAndFlush();
    }

    /// <summary>
    /// Parse log level from string (e.g., "Info", "Debug", "Verbose").
    /// </summary>
    public static LogEventLevel ParseLogLevel(string? logLevelString)
    {
        if (string.IsNullOrWhiteSpace(logLevelString))
        {
            return LogEventLevel.Verbose;
        }

        return logLevelString.ToLowerInvariant() switch
        {
            "verbose" => LogEventLevel.Verbose,
            "debug" => LogEventLevel.Debug,
            "information" or "info" => LogEventLevel.Information,
            "warning" or "warn" => LogEventLevel.Warning,
            "error" => LogEventLevel.Error,
            "fatal" => LogEventLevel.Fatal,
            _ => LogEventLevel.Verbose
        };
    }
}

