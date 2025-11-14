using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using RightClicks.Models;
using RightClicks.Services;
using Serilog;
using MessageBox = System.Windows.MessageBox;

namespace RightClicks;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private ObservableCollection<FeatureViewModel> _features = new();
    private ObservableCollection<FeatureGroupViewModel> _featureGroups = new();
    private ObservableCollection<JobViewModel> _jobViewModels = new();
    private ObservableCollection<ApiKeyViewModel> _apiKeys = new();
    private AppConfig _config = null!;
    private JobQueueService? _jobQueueService;
    private DispatcherTimer? _uiUpdateTimer;

    public MainWindow()
    {
        InitializeComponent();
        Log.Information("MainWindow initialized");

        LoadConfiguration();
        LoadApiKeys();
        InitializeJobQueue();
    }

    private void LoadConfiguration()
    {
        Log.Information("Loading configuration for UI...");

        // Load config
        _config = ConfigurationService.LoadConfig();

        // Load features from discovery service
        var discoveredFeatures = FeatureDiscoveryService.GetFeatures();

        // Create view models for each feature
        _features.Clear();
        foreach (var feature in discoveredFeatures)
        {
            var featureConfig = _config.Features.FirstOrDefault(f => f.Id == feature.Id);
            var isEnabled = featureConfig?.Enabled ?? true; // Default to enabled if not in config

            _features.Add(new FeatureViewModel
            {
                Id = feature.Id,
                DisplayName = feature.DisplayName,
                Description = feature.Description,
                SupportedExtensions = feature.SupportedExtensions,
                SupportedExtensionsText = string.Join(", ", feature.SupportedExtensions),
                IsEnabled = isEnabled
            });
        }

        // Group features by category
        GroupFeaturesByCategory();

        // Bind to UI
        FeaturesListControl.ItemsSource = _featureGroups;

        // Set concurrent jobs slider
        ConcurrentJobsSlider.Value = _config.Settings.MaxConcurrentJobs;
        ConcurrentJobsValueText.Text = _config.Settings.MaxConcurrentJobs.ToString();

        Log.Information("Configuration loaded. {FeatureCount} features displayed", _features.Count);
    }

    private void GroupFeaturesByCategory()
    {
        _featureGroups.Clear();

        // Define category detection rules
        var videoExtensions = new[] { ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm" };
        var audioExtensions = new[] { ".mp3", ".wav", ".aac", ".flac", ".ogg", ".m4a" };
        var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif", ".bmp" };
        var textExtensions = new[] { ".txt", ".md", ".glsl", ".frag", ".sql", ".cs", ".js", ".ts",
                                      ".json", ".xml", ".html", ".css", ".py", ".java", ".cpp",
                                      ".c", ".h", ".hpp", ".sh", ".bat", ".ps1", ".yaml", ".yml",
                                      ".ini", ".cfg" };

        // Categorize features
        var videoFeatures = new List<FeatureViewModel>();
        var audioFeatures = new List<FeatureViewModel>();
        var imageFeatures = new List<FeatureViewModel>();
        var textFeatures = new List<FeatureViewModel>();

        foreach (var feature in _features)
        {
            var extensions = feature.SupportedExtensions;

            // Determine primary category (first match wins)
            if (extensions.Any(ext => videoExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase)))
            {
                videoFeatures.Add(feature);
            }
            else if (extensions.Any(ext => audioExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase)))
            {
                audioFeatures.Add(feature);
            }
            else if (extensions.Any(ext => imageExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase)))
            {
                imageFeatures.Add(feature);
            }
            else if (extensions.Any(ext => textExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase)))
            {
                textFeatures.Add(feature);
            }
        }

        // Create groups (only add non-empty groups)
        if (videoFeatures.Any())
        {
            _featureGroups.Add(new FeatureGroupViewModel
            {
                CategoryName = "Video Files",
                Features = new ObservableCollection<FeatureViewModel>(videoFeatures)
            });
        }

        if (audioFeatures.Any())
        {
            _featureGroups.Add(new FeatureGroupViewModel
            {
                CategoryName = "Audio Files",
                Features = new ObservableCollection<FeatureViewModel>(audioFeatures)
            });
        }

        if (imageFeatures.Any())
        {
            _featureGroups.Add(new FeatureGroupViewModel
            {
                CategoryName = "Image Files",
                Features = new ObservableCollection<FeatureViewModel>(imageFeatures)
            });
        }

        if (textFeatures.Any())
        {
            _featureGroups.Add(new FeatureGroupViewModel
            {
                CategoryName = "Text Files",
                Features = new ObservableCollection<FeatureViewModel>(textFeatures)
            });
        }

        Log.Debug("Features grouped: Video={VideoCount}, Audio={AudioCount}, Image={ImageCount}, Text={TextCount}",
            videoFeatures.Count, audioFeatures.Count, imageFeatures.Count, textFeatures.Count);
    }

    private void FeatureToggle_Changed(object sender, RoutedEventArgs e)
    {
        Log.Debug("Feature toggle changed");
        // Configuration will be saved when user clicks Save button
    }

    private void ConcurrentJobsSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (ConcurrentJobsValueText != null)
        {
            ConcurrentJobsValueText.Text = ((int)e.NewValue).ToString();
        }
    }

    private void SaveConfigButton_Click(object sender, RoutedEventArgs e)
    {
        Log.Information("Saving configuration...");

        try
        {
            // Update feature enabled states
            foreach (var featureVM in _features)
            {
                var featureConfig = _config.Features.FirstOrDefault(f => f.Id == featureVM.Id);
                if (featureConfig != null)
                {
                    featureConfig.Enabled = featureVM.IsEnabled;
                }
                else
                {
                    // Add new feature config if it doesn't exist
                    // Get full metadata from discovery service
                    var discoveredFeature = FeatureDiscoveryService.GetFeatures()
                        .FirstOrDefault(f => f.Id == featureVM.Id);

                    _config.Features.Add(new FeatureConfig
                    {
                        Id = featureVM.Id,
                        DisplayName = discoveredFeature?.DisplayName ?? featureVM.DisplayName,
                        Description = discoveredFeature?.Description ?? featureVM.Description,
                        SupportedExtensions = discoveredFeature?.SupportedExtensions ?? Array.Empty<string>(),
                        Enabled = featureVM.IsEnabled
                    });
                }
            }

            // Update concurrent jobs
            _config.Settings.MaxConcurrentJobs = (int)ConcurrentJobsSlider.Value;

            // Save to file
            ConfigurationService.SaveConfig(_config);

            Log.Information("Configuration saved successfully");
            System.Windows.MessageBox.Show(
                "Configuration saved successfully!",
                "RightClicks",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to save configuration");
            System.Windows.MessageBox.Show(
                $"Failed to save configuration:\n{ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
    }

    #region API Key Management

    /// <summary>
    /// Load API keys from config and environment variables.
    /// </summary>
    private void LoadApiKeys()
    {
        Log.Information("Loading API keys configuration...");

        _apiKeys.Clear();

        // Load existing API keys from config
        foreach (var kvp in _config.ApiKeys)
        {
            var serviceName = kvp.Key;
            var envVarName = kvp.Value;

            // Try to read the actual API key from environment variable
            var apiKeyValue = Environment.GetEnvironmentVariable(envVarName, EnvironmentVariableTarget.User);

            _apiKeys.Add(new ApiKeyViewModel
            {
                ServiceName = serviceName,
                EnvironmentVariableName = envVarName,
                ApiKeyValue = apiKeyValue ?? string.Empty,
                IsExistingEntry = true
            });
        }

        // Sort by service name
        var sortedKeys = _apiKeys.OrderBy(k => k.ServiceName, StringComparer.OrdinalIgnoreCase).ToList();
        _apiKeys.Clear();
        foreach (var key in sortedKeys)
        {
            _apiKeys.Add(key);
        }

        // Add one blank row for new entries
        _apiKeys.Add(new ApiKeyViewModel
        {
            ServiceName = string.Empty,
            EnvironmentVariableName = string.Empty,
            ApiKeyValue = string.Empty,
            IsExistingEntry = false
        });

        // Bind to UI
        if (ApiKeysDataGrid != null)
        {
            ApiKeysDataGrid.ItemsSource = _apiKeys;
        }

        Log.Information("API keys loaded. {Count} existing entries", _config.ApiKeys.Count);
    }

    /// <summary>
    /// Save API configuration button click handler.
    /// </summary>
    private void SaveApiConfigButton_Click(object sender, RoutedEventArgs e)
    {
        Log.Information("Saving API configuration...");

        try
        {
            var validEntries = new List<ApiKeyViewModel>();
            var hasErrors = false;

            // Validate all non-empty rows
            foreach (var apiKey in _apiKeys)
            {
                // Skip completely empty rows
                if (string.IsNullOrWhiteSpace(apiKey.ServiceName) &&
                    string.IsNullOrWhiteSpace(apiKey.EnvironmentVariableName) &&
                    string.IsNullOrWhiteSpace(apiKey.ApiKeyValue))
                {
                    continue;
                }

                // Validate partially filled rows
                if (string.IsNullOrWhiteSpace(apiKey.ServiceName))
                {
                    MessageBox.Show("Service Name is required for all entries.", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    hasErrors = true;
                    break;
                }

                if (string.IsNullOrWhiteSpace(apiKey.EnvironmentVariableName))
                {
                    MessageBox.Show($"Environment Variable Name is required for '{apiKey.ServiceName}'.",
                        "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    hasErrors = true;
                    break;
                }

                if (string.IsNullOrWhiteSpace(apiKey.ApiKeyValue))
                {
                    MessageBox.Show($"API Key Value is required for '{apiKey.ServiceName}'.",
                        "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    hasErrors = true;
                    break;
                }

                if (apiKey.ApiKeyValue.Length < 10)
                {
                    MessageBox.Show($"API Key Value for '{apiKey.ServiceName}' is too short (minimum 10 characters).",
                        "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    hasErrors = true;
                    break;
                }

                validEntries.Add(apiKey);
            }

            if (hasErrors) return;

            if (validEntries.Count == 0)
            {
                MessageBox.Show("No valid API key entries to save.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Update config and environment variables
            _config.ApiKeys.Clear();

            foreach (var apiKey in validEntries)
            {
                // Write API key to environment variable
                Environment.SetEnvironmentVariable(
                    apiKey.EnvironmentVariableName,
                    apiKey.ApiKeyValue,
                    EnvironmentVariableTarget.User
                );

                // Add to config (service name -> env var name mapping)
                _config.ApiKeys[apiKey.ServiceName] = apiKey.EnvironmentVariableName;

                Log.Information("Saved API key for service: {ServiceName} -> {EnvVarName}",
                    apiKey.ServiceName, apiKey.EnvironmentVariableName);
            }

            // Save config to file
            ConfigurationService.SaveConfig(_config);

            Log.Information("API configuration saved successfully. {Count} entries", validEntries.Count);
            MessageBox.Show(
                "API configuration saved successfully!",
                "RightClicks",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );

            // Reload API keys to refresh UI
            LoadApiKeys();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to save API configuration");
            MessageBox.Show(
                $"Failed to save API configuration:\n{ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
    }

    /// <summary>
    /// Delete API key button click handler.
    /// </summary>
    private void DeleteApiKeyButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button) return;
        if (button.DataContext is not ApiKeyViewModel apiKey) return;

        Log.Information("Deleting API key: {ServiceName}", apiKey.ServiceName);

        try
        {
            // Remove from config
            _config.ApiKeys.Remove(apiKey.ServiceName);

            // Save config
            ConfigurationService.SaveConfig(_config);

            // Remove from UI
            _apiKeys.Remove(apiKey);

            Log.Information("API key deleted successfully: {ServiceName}", apiKey.ServiceName);
            MessageBox.Show(
                $"API key for '{apiKey.ServiceName}' deleted successfully.\n\n" +
                "Note: The environment variable was not deleted and can still be used by other applications.",
                "RightClicks",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to delete API key: {ServiceName}", apiKey.ServiceName);
            MessageBox.Show(
                $"Failed to delete API key:\n{ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
    }

    /// <summary>
    /// Toggle password visibility button click handler.
    /// </summary>
    private void TogglePasswordVisibility_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button) return;
        if (button.DataContext is not ApiKeyViewModel apiKey) return;

        apiKey.IsPasswordVisible = !apiKey.IsPasswordVisible;
    }

    /// <summary>
    /// PasswordBox loaded event handler - sync initial value from ViewModel.
    /// </summary>
    private void PasswordBox_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.PasswordBox passwordBox) return;
        if (passwordBox.DataContext is not ApiKeyViewModel apiKey) return;

        // Set initial password value
        passwordBox.Password = apiKey.ApiKeyValue;
    }

    /// <summary>
    /// PasswordBox password changed event handler - sync to ViewModel.
    /// </summary>
    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.PasswordBox passwordBox) return;
        if (passwordBox.DataContext is not ApiKeyViewModel apiKey) return;

        // Update ViewModel when password changes
        apiKey.ApiKeyValue = passwordBox.Password;
    }

    #endregion

    #region Job Queue Management

    /// <summary>
    /// Initialize job queue service and UI bindings.
    /// </summary>
    private void InitializeJobQueue()
    {
        Log.Information("Initializing job queue UI...");

        // Get the job queue service from App (will be initialized there)
        _jobQueueService = ((App)System.Windows.Application.Current).JobQueueService;

        if (_jobQueueService == null)
        {
            Log.Warning("JobQueueService not available in App");
            return;
        }

        // Subscribe to job events
        _jobQueueService.JobAdded += OnJobAdded;
        _jobQueueService.JobStatusChanged += OnJobStatusChanged;
        _jobQueueService.JobRemoved += OnJobRemoved;

        // Bind jobs to DataGrid
        JobsDataGrid.ItemsSource = _jobViewModels;

        // Setup UI update timer for elapsed time
        _uiUpdateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _uiUpdateTimer.Tick += UpdateJobElapsedTimes;
        _uiUpdateTimer.Start();

        // Load existing jobs (if any)
        RefreshJobList();

        Log.Information("Job queue UI initialized");
    }

    /// <summary>
    /// Refresh the job list from the service.
    /// </summary>
    private void RefreshJobList()
    {
        if (_jobQueueService == null) return;

        Dispatcher.Invoke(() =>
        {
            _jobViewModels.Clear();

            foreach (var job in _jobQueueService.Jobs)
            {
                _jobViewModels.Add(new JobViewModel(job));
            }

            UpdateJobCounts();
            UpdateEmptyState();
        });
    }

    /// <summary>
    /// Handle job added event.
    /// </summary>
    private void OnJobAdded(object? sender, Job job)
    {
        Dispatcher.Invoke(() =>
        {
            _jobViewModels.Add(new JobViewModel(job));
            UpdateJobCounts();
            UpdateEmptyState();

            Log.Debug("Job added to UI: {JobId}", job.Id);
        });
    }

    /// <summary>
    /// Handle job status changed event.
    /// </summary>
    private void OnJobStatusChanged(object? sender, Job job)
    {
        Dispatcher.Invoke(() =>
        {
            var viewModel = _jobViewModels.FirstOrDefault(vm => vm.JobId == job.Id);
            if (viewModel != null)
            {
                viewModel.UpdateFromJob(job);
                UpdateJobCounts();
            }

            Log.Debug("Job status updated in UI: {JobId} - {Status}", job.Id, job.Status);
        });
    }

    /// <summary>
    /// Handle job removed event.
    /// </summary>
    private void OnJobRemoved(object? sender, Job job)
    {
        Dispatcher.Invoke(() =>
        {
            var viewModel = _jobViewModels.FirstOrDefault(vm => vm.JobId == job.Id);
            if (viewModel != null)
            {
                _jobViewModels.Remove(viewModel);
                UpdateJobCounts();
                UpdateEmptyState();
            }

            Log.Debug("Job removed from UI: {JobId}", job.Id);
        });
    }

    /// <summary>
    /// Update elapsed time for running jobs.
    /// </summary>
    private void UpdateJobElapsedTimes(object? sender, EventArgs e)
    {
        foreach (var viewModel in _jobViewModels.Where(vm => vm.Status == JobStatus.Running))
        {
            viewModel.UpdateElapsedTime();
        }
    }

    /// <summary>
    /// Update job count displays.
    /// </summary>
    private void UpdateJobCounts()
    {
        var totalCount = _jobViewModels.Count;
        var runningCount = _jobViewModels.Count(vm => vm.Status == JobStatus.Running);
        var pendingCount = _jobViewModels.Count(vm => vm.Status == JobStatus.Pending);
        var completedCount = _jobViewModels.Count(vm => vm.Status == JobStatus.Completed);
        var failedCount = _jobViewModels.Count(vm => vm.Status == JobStatus.Failed);

        JobCountText.Text = $"{totalCount} job{(totalCount != 1 ? "s" : "")} in queue";
        RunningCountText.Text = runningCount.ToString();
        PendingCountText.Text = pendingCount.ToString();
        CompletedCountText.Text = completedCount.ToString();
        FailedCountText.Text = failedCount.ToString();
        MaxConcurrentJobsText.Text = $"Max concurrent: {_config.Settings.MaxConcurrentJobs}";
    }

    /// <summary>
    /// Update empty state visibility.
    /// </summary>
    private void UpdateEmptyState()
    {
        if (_jobViewModels.Count == 0)
        {
            EmptyStateText.Visibility = Visibility.Visible;
            JobsDataGrid.Visibility = Visibility.Collapsed;
        }
        else
        {
            EmptyStateText.Visibility = Visibility.Collapsed;
            JobsDataGrid.Visibility = Visibility.Visible;
        }
    }

    #endregion

    #region Job Queue Button Handlers

    /// <summary>
    /// Handle Cancel button click.
    /// </summary>
    private void CancelJobButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button button && button.Tag is Guid jobId)
        {
            Log.Information("Cancel button clicked for job: {JobId}", jobId);
            _jobQueueService?.CancelJob(jobId);
        }
    }

    /// <summary>
    /// Handle Remove button click.
    /// </summary>
    private void RemoveJobButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button button && button.Tag is Guid jobId)
        {
            Log.Information("Remove button clicked for job: {JobId}", jobId);
            _jobQueueService?.RemoveJob(jobId);
        }
    }

    /// <summary>
    /// Handle Clear Completed button click.
    /// </summary>
    private void ClearCompletedButton_Click(object sender, RoutedEventArgs e)
    {
        Log.Information("Clear Completed button clicked");
        _jobQueueService?.ClearCompleted();
    }

    #endregion

    #region Shell Integration & System Controls

    /// <summary>
    /// Handle Uninstall All Shell Hooks button click.
    /// </summary>
    private void UninstallShellHooksButton_Click(object sender, RoutedEventArgs e)
    {
        Log.Information("Uninstall All Shell Hooks button clicked");

        try
        {
            // Path to RightClicksShellManager.exe
            var shellManagerPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RightClicksShellManager.exe");

            if (!File.Exists(shellManagerPath))
            {
                MessageBox.Show(
                    "RightClicksShellManager.exe not found!\n\n" +
                    $"Expected location: {shellManagerPath}\n\n" +
                    "Please ensure the shell manager is in the same folder as RightClicks.exe",
                    "Shell Manager Not Found",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Log.Error("RightClicksShellManager.exe not found at: {Path}", shellManagerPath);
                return;
            }

            // Run RightClicksShellManager.exe /uninstall with admin elevation
            var startInfo = new ProcessStartInfo
            {
                FileName = shellManagerPath,
                Arguments = "/uninstall",
                UseShellExecute = true,
                Verb = "runas" // Request admin elevation
            };

            Log.Information("Launching RightClicksShellManager.exe /uninstall with admin elevation");
            var process = Process.Start(startInfo);
            process?.WaitForExit();

            if (process?.ExitCode == 0)
            {
                MessageBox.Show(
                    "Shell hooks uninstalled successfully!\n\n" +
                    "The RightClicks context menu has been removed from Windows Explorer.",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                Log.Information("Shell hooks uninstalled successfully");
            }
            else
            {
                MessageBox.Show(
                    "Shell hooks uninstallation may have failed.\n\n" +
                    $"Exit code: {process?.ExitCode}\n\n" +
                    "Check the logs for more details.",
                    "Warning",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                Log.Warning("Shell hooks uninstallation returned exit code: {ExitCode}", process?.ExitCode);
            }
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            // User cancelled UAC prompt
            MessageBox.Show(
                "Administrator privileges are required to uninstall shell hooks.\n\n" +
                "Operation cancelled.",
                "Cancelled",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            Log.Information("User cancelled UAC prompt for shell hooks uninstallation");
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Error uninstalling shell hooks:\n\n{ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Log.Error(ex, "Error uninstalling shell hooks");
        }
    }

    /// <summary>
    /// Handle Install Selected Shell Hooks button click.
    /// </summary>
    private void InstallShellHooksButton_Click(object sender, RoutedEventArgs e)
    {
        Log.Information("Install Selected Shell Hooks button clicked");

        try
        {
            // Path to RightClicksShellManager.exe
            var shellManagerPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RightClicksShellManager.exe");

            if (!File.Exists(shellManagerPath))
            {
                MessageBox.Show(
                    "RightClicksShellManager.exe not found!\n\n" +
                    $"Expected location: {shellManagerPath}\n\n" +
                    "Please ensure the shell manager is in the same folder as RightClicks.exe",
                    "Shell Manager Not Found",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Log.Error("RightClicksShellManager.exe not found at: {Path}", shellManagerPath);
                return;
            }

            // Run RightClicksShellManager.exe /install with admin elevation
            var startInfo = new ProcessStartInfo
            {
                FileName = shellManagerPath,
                Arguments = "/install",
                UseShellExecute = true,
                Verb = "runas" // Request admin elevation
            };

            Log.Information("Launching RightClicksShellManager.exe /install with admin elevation");
            var process = Process.Start(startInfo);
            process?.WaitForExit();

            if (process?.ExitCode == 0)
            {
                MessageBox.Show(
                    "Shell hooks installed successfully!\n\n" +
                    "You can now right-click on video files in Windows Explorer and see the RightClicks menu.\n\n" +
                    "Note: You may need to restart Windows Explorer for changes to take effect.",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                Log.Information("Shell hooks installed successfully");
            }
            else
            {
                MessageBox.Show(
                    "Shell hooks installation may have failed.\n\n" +
                    $"Exit code: {process?.ExitCode}\n\n" +
                    "Check the logs for more details.",
                    "Warning",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                Log.Warning("Shell hooks installation returned exit code: {ExitCode}", process?.ExitCode);
            }
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            // User cancelled UAC prompt
            MessageBox.Show(
                "Administrator privileges are required to install shell hooks.\n\n" +
                "Operation cancelled.",
                "Cancelled",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            Log.Information("User cancelled UAC prompt for shell hooks installation");
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Error installing shell hooks:\n\n{ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Log.Error(ex, "Error installing shell hooks");
        }
    }

    /// <summary>
    /// Handle Uninstall SysTray Startup button click.
    /// </summary>
    private void UninstallStartupButton_Click(object sender, RoutedEventArgs e)
    {
        Log.Information("Uninstall SysTray Startup button clicked");

        try
        {
            // Path to startup folder shortcut
            var startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            var shortcutPath = Path.Combine(startupFolder, "RightClicks.lnk");

            if (File.Exists(shortcutPath))
            {
                File.Delete(shortcutPath);
                MessageBox.Show(
                    "Startup shortcut removed successfully!\n\n" +
                    "RightClicks will no longer start automatically when Windows starts.",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                Log.Information("Startup shortcut removed from: {Path}", shortcutPath);
            }
            else
            {
                MessageBox.Show(
                    "Startup shortcut not found.\n\n" +
                    "RightClicks is not currently set to start automatically.",
                    "Not Found",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                Log.Information("Startup shortcut not found at: {Path}", shortcutPath);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Error removing startup shortcut:\n\n{ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Log.Error(ex, "Error removing startup shortcut");
        }
    }

    /// <summary>
    /// Handle Install SysTray Startup button click.
    /// </summary>
    private void InstallStartupButton_Click(object sender, RoutedEventArgs e)
    {
        Log.Information("Install SysTray Startup button clicked");

        try
        {
            // Path to startup folder
            var startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            var shortcutPath = Path.Combine(startupFolder, "RightClicks.lnk");
            var targetPath = Process.GetCurrentProcess().MainModule?.FileName;

            if (string.IsNullOrEmpty(targetPath))
            {
                MessageBox.Show(
                    "Could not determine RightClicks.exe path.",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Log.Error("Could not determine RightClicks.exe path");
                return;
            }

            // Use PowerShell to create the shortcut (works on all Windows versions)
            var psScript = $@"
$WshShell = New-Object -ComObject WScript.Shell
$Shortcut = $WshShell.CreateShortcut('{shortcutPath}')
$Shortcut.TargetPath = '{targetPath}'
$Shortcut.WorkingDirectory = '{Path.GetDirectoryName(targetPath)}'
$Shortcut.Description = 'RightClicks - Context Menu Extension'
$Shortcut.Save()
";

            var startInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{psScript}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            var process = Process.Start(startInfo);
            process?.WaitForExit();

            if (process?.ExitCode == 0 && File.Exists(shortcutPath))
            {
                MessageBox.Show(
                    "Startup shortcut created successfully!\n\n" +
                    "RightClicks will now start automatically when Windows starts.\n\n" +
                    $"Shortcut location: {shortcutPath}",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                Log.Information("Startup shortcut created at: {Path}", shortcutPath);
            }
            else
            {
                MessageBox.Show(
                    "Failed to create startup shortcut.\n\n" +
                    "You can manually create a shortcut to RightClicks.exe in:\n" +
                    $"{startupFolder}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Log.Error("Failed to create startup shortcut. Exit code: {ExitCode}", process?.ExitCode);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Error creating startup shortcut:\n\n{ex.Message}\n\n" +
                "Note: You may need to manually create a shortcut in the Startup folder.",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Log.Error(ex, "Error creating startup shortcut");
        }
    }

    /// <summary>
    /// Handle Check for Updates button click.
    /// </summary>
    private void CheckForUpdatesButton_Click(object sender, RoutedEventArgs e)
    {
        Log.Information("Check for Updates button clicked");

        try
        {
            // GitHub repository information
            const string repoOwner = "hughesdo";
            const string repoName = "RightClicks";
            const string currentVersion = "1.0.0"; // TODO: Get from assembly version

            // Open GitHub releases page in browser
            var releasesUrl = $"https://github.com/{repoOwner}/{repoName}/releases";
            Process.Start(new ProcessStartInfo
            {
                FileName = releasesUrl,
                UseShellExecute = true
            });

            MessageBox.Show(
                $"Current Version: {currentVersion}\n\n" +
                "Opening GitHub releases page in your browser...\n\n" +
                "Check for newer versions and download if available.",
                "Check for Updates",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            Log.Information("Opened GitHub releases page: {Url}", releasesUrl);

            // TODO: Implement automatic version checking via GitHub API
            // - Query https://api.github.com/repos/{owner}/{repo}/releases/latest
            // - Compare version numbers
            // - Show update notification if newer version available
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Error checking for updates:\n\n{ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Log.Error(ex, "Error checking for updates");
        }
    }

    /// <summary>
    /// Handle Open Logs Folder button click.
    /// </summary>
    private void OpenLogsFolderButton_Click(object sender, RoutedEventArgs e)
    {
        Log.Information("Open Logs Folder button clicked");

        try
        {
            var logsFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RightClicks",
                "logs");

            // Create folder if it doesn't exist
            if (!Directory.Exists(logsFolder))
            {
                Directory.CreateDirectory(logsFolder);
                Log.Information("Created logs folder: {Path}", logsFolder);
            }

            // Open folder in Windows Explorer
            Process.Start(new ProcessStartInfo
            {
                FileName = logsFolder,
                UseShellExecute = true,
                Verb = "open"
            });

            Log.Information("Opened logs folder: {Path}", logsFolder);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Error opening logs folder:\n\n{ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Log.Error(ex, "Error opening logs folder");
        }
    }

    #endregion

    /// <summary>
    /// Handle window closing - minimize to tray instead of exiting
    /// </summary>
    private void Window_Closing(object sender, CancelEventArgs e)
    {
        // Cancel the close event
        e.Cancel = true;

        // Hide the window instead
        this.Hide();

        Log.Information("MainWindow hidden (minimized to tray)");
    }
}

/// <summary>
/// View model for feature display in UI
/// </summary>
public class FeatureViewModel : INotifyPropertyChanged
{
    private bool _isEnabled;

    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string[] SupportedExtensions { get; set; } = Array.Empty<string>();
    public string SupportedExtensionsText { get; set; } = string.Empty;

    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (_isEnabled != value)
            {
                _isEnabled = value;
                OnPropertyChanged(nameof(IsEnabled));
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// View model for job display in UI
/// </summary>
public class JobViewModel : INotifyPropertyChanged
{
    private readonly Job _job;
    private string _durationText = "";
    private string _statusIcon = "";
    private string _statusText = "";

    public JobViewModel(Job job)
    {
        _job = job;
        UpdateFromJob(job);
    }

    public Guid JobId => _job.Id;
    public string FeatureName => _job.FeatureName;
    public string FileName => Path.GetFileName(_job.FilePath);
    public JobStatus Status => _job.Status;

    public string StatusIcon
    {
        get => _statusIcon;
        private set
        {
            if (_statusIcon != value)
            {
                _statusIcon = value;
                OnPropertyChanged(nameof(StatusIcon));
            }
        }
    }

    public string StatusText
    {
        get => _statusText;
        private set
        {
            if (_statusText != value)
            {
                _statusText = value;
                OnPropertyChanged(nameof(StatusText));
            }
        }
    }

    public string DurationText
    {
        get => _durationText;
        private set
        {
            if (_durationText != value)
            {
                _durationText = value;
                OnPropertyChanged(nameof(DurationText));
            }
        }
    }

    public bool CanCancel => _job.Status == JobStatus.Running;
    public bool CanRemove => _job.Status == JobStatus.Pending;

    public void UpdateFromJob(Job job)
    {
        // Update status icon
        StatusIcon = job.Status switch
        {
            JobStatus.Pending => "⏳",
            JobStatus.Running => "▶️",
            JobStatus.Completed => "✅",
            JobStatus.Failed => "❌",
            JobStatus.Cancelled => "🚫",
            _ => "❓"
        };

        // Update status text
        StatusText = job.Status.ToString();

        // Update duration
        UpdateDurationText();

        // Notify property changes
        OnPropertyChanged(nameof(CanCancel));
        OnPropertyChanged(nameof(CanRemove));
        OnPropertyChanged(nameof(Status));
    }

    public void UpdateElapsedTime()
    {
        if (_job.Status == JobStatus.Running && _job.StartedAt.HasValue)
        {
            UpdateDurationText();
        }
    }

    private void UpdateDurationText()
    {
        if (_job.Status == JobStatus.Running && _job.StartedAt.HasValue)
        {
            var elapsed = DateTime.Now - _job.StartedAt.Value;
            DurationText = $"{elapsed.TotalSeconds:F1}s";
        }
        else if (_job.CompletedAt.HasValue && _job.StartedAt.HasValue)
        {
            var duration = _job.CompletedAt.Value - _job.StartedAt.Value;
            DurationText = $"{duration.TotalSeconds:F1}s";
        }
        else
        {
            DurationText = "-";
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// View model for feature group (category) display in UI
/// </summary>
public class FeatureGroupViewModel : INotifyPropertyChanged
{
    private bool _isExpanded;

    public string CategoryName { get; set; } = string.Empty;
    public ObservableCollection<FeatureViewModel> Features { get; set; } = new();

    public int FeatureCount => Features.Count;

    public string HeaderText => $"{CategoryName} ({FeatureCount})";

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded != value)
            {
                _isExpanded = value;
                OnPropertyChanged(nameof(IsExpanded));
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// View model for API key configuration display in UI
/// </summary>
public class ApiKeyViewModel : INotifyPropertyChanged
{
    private string _serviceName = string.Empty;
    private string _environmentVariableName = string.Empty;
    private string _apiKeyValue = string.Empty;
    private bool _isPasswordVisible = false;

    public string ServiceName
    {
        get => _serviceName;
        set
        {
            if (_serviceName != value)
            {
                _serviceName = value;
                OnPropertyChanged(nameof(ServiceName));
            }
        }
    }

    public string EnvironmentVariableName
    {
        get => _environmentVariableName;
        set
        {
            if (_environmentVariableName != value)
            {
                _environmentVariableName = value;
                OnPropertyChanged(nameof(EnvironmentVariableName));
            }
        }
    }

    public string ApiKeyValue
    {
        get => _apiKeyValue;
        set
        {
            if (_apiKeyValue != value)
            {
                _apiKeyValue = value;
                OnPropertyChanged(nameof(ApiKeyValue));
            }
        }
    }

    public bool IsPasswordVisible
    {
        get => _isPasswordVisible;
        set
        {
            if (_isPasswordVisible != value)
            {
                _isPasswordVisible = value;
                OnPropertyChanged(nameof(IsPasswordVisible));
                OnPropertyChanged(nameof(VisibilityIcon));
            }
        }
    }

    public string VisibilityIcon => IsPasswordVisible ? "🙈" : "👁️";

    public bool IsExistingEntry { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}