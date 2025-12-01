using Serilog;
using System.IO;

namespace RightClicks.Services;

/// <summary>
/// Service for discovering RVC voice models from the RVC/assets/weights directory.
/// Scans for .pth files and returns model names for dynamic feature generation.
/// </summary>
public static class RvcModelDiscoveryService
{
    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(RvcModelDiscoveryService));

    /// <summary>
    /// Get the path to the RVC directory.
    /// Checks multiple locations in priority order:
    /// 1. Deployed location: %LOCALAPPDATA%\RightClicks\RVC\ (for end users)
    /// 2. Development location: E:\MyApps\RightClicks\RVC\ (for developer)
    /// 3. Fallback: Navigate up from app directory
    /// </summary>
    public static string GetRvcPath()
    {
        // Priority 1: Deployed location (where install.bat puts it)
        // This is where end users will have RVC after running install.bat
        var deployedPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RightClicks", "RVC");

        if (Directory.Exists(deployedPath))
        {
            Log.Information("Found RVC directory at deployed location: {RvcPath}", deployedPath);
            return deployedPath;
        }

        // Priority 2: Development location (hardcoded)
        // This is the expected location during development
        var devPath = @"E:\MyApps\RightClicks\RVC";
        if (Directory.Exists(devPath))
        {
            Log.Information("Found RVC directory at development location: {RvcPath}", devPath);
            return devPath;
        }

        // Priority 3: Fallback - Try to find RVC folder by navigating up from app directory
        // This handles cases where RVC is in a non-standard location
        var appPath = AppDomain.CurrentDomain.BaseDirectory;
        var currentDir = new DirectoryInfo(appPath);

        Log.Debug("Searching for RVC directory starting from: {AppPath}", appPath);

        while (currentDir != null)
        {
            var candidatePath = Path.Combine(currentDir.FullName, "RVC");
            if (Directory.Exists(candidatePath))
            {
                Log.Information("Found RVC directory via fallback search: {RvcPath}", candidatePath);
                return candidatePath;
            }
            currentDir = currentDir.Parent;
        }

        // Not found anywhere
        Log.Error("RVC directory not found. Checked locations: {DeployedPath}, {DevPath}, and parent directories of {AppPath}",
            deployedPath, devPath, appPath);
        return string.Empty;
    }

    /// <summary>
    /// Get the path to the RVC weights directory.
    /// </summary>
    public static string GetWeightsPath()
    {
        var rvcPath = GetRvcPath();
        if (string.IsNullOrEmpty(rvcPath))
            return string.Empty;

        return Path.Combine(rvcPath, "assets", "weights");
    }

    /// <summary>
    /// Discover all RVC voice models (.pth files) in the weights directory.
    /// Returns model names without the .pth extension.
    /// </summary>
    public static List<string> DiscoverModels()
    {
        var models = new List<string>();
        var weightsPath = GetWeightsPath();

        if (string.IsNullOrEmpty(weightsPath))
        {
            Log.Warning("Cannot discover RVC models: weights path not found");
            return models;
        }

        if (!Directory.Exists(weightsPath))
        {
            Log.Warning("RVC weights directory does not exist: {WeightsPath}", weightsPath);
            return models;
        }

        try
        {
            // Find all .pth files
            var pthFiles = Directory.GetFiles(weightsPath, "*.pth", SearchOption.TopDirectoryOnly);
            
            foreach (var pthFile in pthFiles)
            {
                // Extract model name (filename without extension)
                var modelName = Path.GetFileNameWithoutExtension(pthFile);
                models.Add(modelName);
            }

            models.Sort(); // Alphabetical order for consistent menu display

            Log.Information("Discovered {Count} RVC models: {Models}", models.Count, string.Join(", ", models));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error discovering RVC models from: {WeightsPath}", weightsPath);
        }

        return models;
    }

    /// <summary>
    /// Get the full path to a specific model file.
    /// </summary>
    public static string GetModelPath(string modelName)
    {
        var weightsPath = GetWeightsPath();
        if (string.IsNullOrEmpty(weightsPath))
            return string.Empty;

        return Path.Combine(weightsPath, $"{modelName}.pth");
    }

    /// <summary>
    /// Check if RVC is properly installed (venv exists, infer_cli.py exists).
    /// </summary>
    public static bool IsRvcInstalled()
    {
        var rvcPath = GetRvcPath();
        if (string.IsNullOrEmpty(rvcPath))
            return false;

        // Check for Python venv
        var pythonExe = Path.Combine(rvcPath, "venv", "Scripts", "python.exe");
        if (!File.Exists(pythonExe))
        {
            Log.Warning("RVC Python venv not found at: {PythonExe}", pythonExe);
            return false;
        }

        // Check for infer_cli.py
        var inferScript = Path.Combine(rvcPath, "tools", "infer_cli.py");
        if (!File.Exists(inferScript))
        {
            Log.Warning("RVC infer_cli.py not found at: {InferScript}", inferScript);
            return false;
        }

        Log.Debug("RVC installation verified at: {RvcPath}", rvcPath);
        return true;
    }
}

