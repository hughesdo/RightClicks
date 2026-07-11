using System.IO;
using System.Reflection;
using System.Windows;
using Serilog;

namespace RightClicksClipEditor;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Configure logging
        ConfigureLogging();

        Log.Information("=== RightClicks Clip Editor Started ===");
        Log.Information("Version: {Version}", Assembly.GetExecutingAssembly().GetName().Version);
        Log.Information("OS: {OS}", Environment.OSVersion);
        Log.Information("Command-line args: {Args}", string.Join(" ", e.Args));

        // Parse command-line arguments
        if (e.Args.Length == 0)
        {
            Log.Warning("No command-line arguments provided");
            MessageBox.Show(
                "Usage: RightClicksClipEditor.exe [--video|--audio] <filepath>\n\n" +
                "Examples:\n" +
                "  RightClicksClipEditor.exe --video \"C:\\path\\to\\video.mp4\"\n" +
                "  RightClicksClipEditor.exe --audio \"C:\\path\\to\\audio.mp3\"\n" +
                "  RightClicksClipEditor.exe \"C:\\path\\to\\file.mp4\"",
                "Clip Editor - Usage",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            Shutdown();
            return;
        }

        string? filePath = null;
        MediaType? mediaType = null;

        // Parse arguments
        if (e.Args.Length == 2 && (e.Args[0] == "--video" || e.Args[0] == "--audio"))
        {
            mediaType = e.Args[0] == "--video" ? MediaType.Video : MediaType.Audio;
            filePath = e.Args[1];
            Log.Information("Explicit mode: {MediaType}, File: {FilePath}", mediaType, filePath);
        }
        else if (e.Args.Length == 1)
        {
            filePath = e.Args[0];
            mediaType = DetectMediaType(filePath);
            Log.Information("Auto-detected mode: {MediaType}, File: {FilePath}", mediaType, filePath);
        }
        else
        {
            Log.Error("Invalid command-line arguments");
            MessageBox.Show(
                "Invalid arguments. Usage:\n" +
                "  RightClicksClipEditor.exe [--video|--audio] <filepath>",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
            return;
        }

        // Validate file exists
        if (filePath == null || !File.Exists(filePath))
        {
            Log.Error("File not found: {FilePath}", filePath);
            MessageBox.Show(
                $"File not found: {filePath}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
            return;
        }

        // Launch appropriate editor
        try
        {
            if (mediaType == MediaType.Video)
            {
                Log.Information("Launching Video Clip Editor");
                var window = new Windows.VideoClipEditorWindow(filePath);
                window.Show();
            }
            else
            {
                Log.Information("Launching Audio Clip Editor");
                var window = new Windows.AudioClipEditorWindow(filePath);
                window.Show();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to launch editor window");
            MessageBox.Show(
                $"Failed to open editor: {ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
        }
    }

    private void ConfigureLogging()
    {
        var logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RightClicks", "logs");

        Directory.CreateDirectory(logDir);

        var logFile = Path.Combine(logDir, $"ClipEditor-{DateTime.Now:yyyyMMdd-HHmmss}.log");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(logFile,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
    }

    private MediaType DetectMediaType(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();

        var videoExtensions = new[] { ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm", ".m4v", ".mpg", ".mpeg" };
        var audioExtensions = new[] { ".mp3", ".wav", ".flac", ".m4a", ".aac", ".ogg", ".wma", ".opus" };

        if (videoExtensions.Contains(extension))
        {
            return MediaType.Video;
        }
        else if (audioExtensions.Contains(extension))
        {
            return MediaType.Audio;
        }
        else
        {
            // Default to video for unknown extensions
            Log.Warning("Unknown file extension: {Extension}, defaulting to Video mode", extension);
            return MediaType.Video;
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("=== RightClicks Clip Editor Exiting ===");
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}

public enum MediaType
{
    Video,
    Audio
}

