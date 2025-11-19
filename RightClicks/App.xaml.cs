using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using RightClicks.Services;
using RightClicks.Models;
using Serilog;

namespace RightClicks;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    private bool _isTestMode = false;
    private bool _clearLogs = false;
    private bool _clearTestLogsOnly = false;
    private bool _useQueue = false;
    private string? _featureId = null;
    private string? _filePath = null;
    private NotifyIcon? _notifyIcon = null;
    private Mutex? _singleInstanceMutex = null;
    private IpcService? _ipcService = null;
    private FileStream? _lockFileStream = null;

    /// <summary>
    /// Job queue service instance (shared across application).
    /// </summary>
    public JobQueueService? JobQueueService { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Parse command line arguments first
        ParseCommandLineArguments(e.Args);

        // Check for single instance (ALWAYS, except for --clear-logs)
        if (!_clearLogs)
        {
            const string mutexName = @"Global\RightClicks_SingleInstance_Mutex_v2";
            bool isFirstInstance = false;

            try
            {
                // Try to open existing mutex first
                Mutex.OpenExisting(mutexName);
                // If we get here, mutex exists - another instance is running
                isFirstInstance = false;
            }
            catch (WaitHandleCannotBeOpenedException)
            {
                // Mutex doesn't exist - we're the first instance
                try
                {
                    _singleInstanceMutex = new Mutex(true, mutexName);
                    isFirstInstance = true;
                }
                catch
                {
                    // Failed to create mutex - assume first instance
                    isFirstInstance = true;
                }
            }
            catch
            {
                // Other error - assume first instance
                isFirstInstance = true;
            }

            if (!isFirstInstance)
            {
                // Another instance is already running
                if (!string.IsNullOrEmpty(_featureId) && !string.IsNullOrEmpty(_filePath) && _useQueue)
                {
                    // Send job to existing instance via IPC and exit
                    try
                    {
                        bool success = Task.Run(async () =>
                            await IpcService.SendJobRequestAsync(_featureId, _filePath, timeoutMs: 5000)
                        ).Result;

                        if (success)
                        {
                            // Job sent successfully - exit this instance
                            Environment.Exit(0);
                        }
                        else
                        {
                            // Failed to send - exit anyway
                            Environment.Exit(1);
                        }
                    }
                    catch
                    {
                        // IPC failed - exit anyway
                        Environment.Exit(1);
                    }
                }
                else
                {
                    // Just exit silently for other cases
                    Environment.Exit(0);
                }
                return;
            }
        }

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

        // Initialize job queue service (Phase 4)
        Log.Information("Initializing job queue service...");
        JobQueueService = new JobQueueService(config.Settings.MaxConcurrentJobs);

        // Subscribe to job completion events for notifications
        JobQueueService.JobStatusChanged += OnJobStatusChanged;

        Log.Information("Job queue service initialized");

        // Initialize IPC service to receive jobs from other instances
        Log.Information("Initializing IPC service...");
        _ipcService = new IpcService();
        _ipcService.JobRequestReceived += OnIpcJobRequestReceived;
        _ipcService.StartListening();
        Log.Information("IPC service initialized");

        // Handle feature execution via CLI
        if (!string.IsNullOrEmpty(_featureId) && !string.IsNullOrEmpty(_filePath))
        {
            if (_useQueue)
            {
                // Add job to queue (for right-click invocation)
                Task.Run(async () => await ExecuteFeatureViaQueueAsync(_featureId, _filePath)).GetAwaiter().GetResult();

                // Don't shut down - initialize tray icon and let job queue process the job
                Log.Information("Job queued. Initializing system tray to process queue...");
            }
            else
            {
                // Direct execution (for testing) - execute and exit
                Task.Run(async () => await ExecuteFeatureAsync(_featureId, _filePath)).GetAwaiter().GetResult();

                LoggingService.CloseLogger();
                Shutdown(0);
                return;
            }
        }

        // Initialize system tray icon (Phase 3: UI)
        Log.Information("Initializing system tray icon...");
        InitializeSystemTray();
        Log.Information("System tray icon initialized. Application running in background.");
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("Application exiting...");

        // Unsubscribe from events
        if (JobQueueService != null)
        {
            JobQueueService.JobStatusChanged -= OnJobStatusChanged;
        }

        if (_ipcService != null)
        {
            _ipcService.JobRequestReceived -= OnIpcJobRequestReceived;
        }

        // Dispose IPC service
        _ipcService?.Dispose();

        // Dispose job queue service
        JobQueueService?.Dispose();

        // Dispose system tray icon
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }

        // Release lock file
        _lockFileStream?.Close();
        _lockFileStream?.Dispose();

        // Release single instance mutex
        _singleInstanceMutex?.ReleaseMutex();
        _singleInstanceMutex?.Dispose();

        LoggingService.CloseLogger();
        base.OnExit(e);
    }

    private void InitializeSystemTray()
    {
        // Create system tray icon
        _notifyIcon = new NotifyIcon
        {
            // TODO: Replace with actual icon file in Phase 3
            Icon = System.Drawing.SystemIcons.Application,
            Visible = true,
            Text = "RightClicks - Context Menu Extensions"
        };

        // Create context menu
        var contextMenu = new ContextMenuStrip();

        var openMenuItem = new ToolStripMenuItem("Open RightClicks");
        openMenuItem.Click += (s, e) => ShowMainWindow();
        contextMenu.Items.Add(openMenuItem);

        contextMenu.Items.Add(new ToolStripSeparator());

        var exitMenuItem = new ToolStripMenuItem("Exit");
        exitMenuItem.Click += (s, e) => ExitApplication();
        contextMenu.Items.Add(exitMenuItem);

        _notifyIcon.ContextMenuStrip = contextMenu;

        // Handle double-click to open main window
        _notifyIcon.DoubleClick += (s, e) => ShowMainWindow();

        Log.Information("System tray icon created with context menu");
    }

    private void ShowMainWindow()
    {
        Log.Information("ShowMainWindow called");

        // Create MainWindow if it doesn't exist
        if (MainWindow == null)
        {
            Log.Information("Creating new MainWindow instance");
            MainWindow = new MainWindow();
        }

        // Show and activate the window
        MainWindow.Show();
        MainWindow.WindowState = WindowState.Normal;
        MainWindow.Activate();

        Log.Information("MainWindow shown and activated");
    }

    private void ExitApplication()
    {
        Log.Information("Exit requested from system tray");
        Shutdown(0);
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

                case "--queue":
                    _useQueue = true;
                    break;
            }
        }
    }

    private Task ExecuteFeatureViaQueueAsync(string featureId, string filePath)
    {
        Log.Information("=== CLI Feature Execution (Queue Mode) ===");
        Log.Information("Feature ID: {FeatureId}", featureId);
        Log.Information("File Path: {FilePath}", filePath);

        // Validate file exists
        if (!File.Exists(filePath))
        {
            Log.Error("File not found: {FilePath}", filePath);
            return Task.CompletedTask;
        }

        // Look up feature
        var feature = FeatureDiscoveryService.GetFeatureById(featureId);
        if (feature == null)
        {
            Log.Error("Feature not found: {FeatureId}", featureId);
            return Task.CompletedTask;
        }

        Log.Information("Found feature: {DisplayName}", feature.DisplayName);

        // Check if feature supports this file extension
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        if (!feature.SupportedExtensions.Any(ext => ext.Equals(extension, StringComparison.OrdinalIgnoreCase)))
        {
            Log.Warning("Feature {FeatureId} does not support extension {Extension}", featureId, extension);
            Log.Information("Supported extensions: {Extensions}", string.Join(", ", feature.SupportedExtensions));
        }

        // Create job and add to queue
        var job = new Models.Job
        {
            Id = Guid.NewGuid(),
            FeatureId = featureId,
            FeatureName = feature.DisplayName,
            FilePath = filePath,
            Status = Models.JobStatus.Pending,
            CreatedAt = DateTime.Now
        };

        Log.Information("Adding job to queue: {JobId}", job.Id);
        JobQueueService?.AddJob(job);
        Log.Information("Job added to queue successfully");

        // Note: Job will be processed asynchronously by JobQueueService
        // We don't wait for completion in queue mode
        return Task.CompletedTask;
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

    /// <summary>
    /// Handle job request received from another instance via IPC.
    /// </summary>
    private void OnIpcJobRequestReceived(object? sender, JobRequest jobRequest)
    {
        Log.Information("IPC job request received: Feature={FeatureId}, File={FilePath}",
            jobRequest.FeatureId, jobRequest.FilePath);

        // Add the job to the queue (same as if it came from CLI)
        Dispatcher.Invoke(() =>
        {
            Task.Run(async () => await ExecuteFeatureViaQueueAsync(jobRequest.FeatureId, jobRequest.FilePath));
        });
    }

    /// <summary>
    /// Handle job status changes and show notifications for completed/failed jobs.
    /// </summary>
    private void OnJobStatusChanged(object? sender, Job job)
    {
        // Only show notifications for completed or failed jobs
        if (job.Status == JobStatus.Completed)
        {
            ShowNotification(
                "Job Complete",
                $"{job.FeatureName} completed successfully\n{Path.GetFileName(job.OutputFilePath ?? job.FilePath)}",
                ToolTipIcon.Info
            );
        }
        else if (job.Status == JobStatus.Failed)
        {
            ShowNotification(
                "Job Failed",
                $"{job.FeatureName} failed\n{job.ErrorMessage ?? "Unknown error"}",
                ToolTipIcon.Error
            );
        }
    }

    /// <summary>
    /// Show a balloon notification from the system tray icon with sound.
    /// </summary>
    private void ShowNotification(string title, string message, ToolTipIcon icon)
    {
        if (_notifyIcon != null)
        {
            _notifyIcon.ShowBalloonTip(
                timeout: 5000, // 5 seconds
                tipTitle: title,
                tipText: message,
                tipIcon: icon
            );

            // Play system sound based on notification type
            try
            {
                if (icon == ToolTipIcon.Info)
                {
                    // Success sound
                    System.Media.SystemSounds.Asterisk.Play();
                }
                else if (icon == ToolTipIcon.Error)
                {
                    // Error sound
                    System.Media.SystemSounds.Hand.Play();
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to play notification sound");
            }

            Log.Debug("Notification shown: {Title} - {Message}", title, message);
        }
    }
}

