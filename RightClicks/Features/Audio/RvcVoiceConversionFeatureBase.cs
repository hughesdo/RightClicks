using System.Diagnostics;
using System.IO;
using FFMpegCore;
using RightClicks.Models;
using RightClicks.Services;
using Serilog;

namespace RightClicks.Features.Audio;

/// <summary>
/// Base class for RVC voice conversion features.
/// Handles Python venv activation, hidden process execution, and logging integration.
/// Each derived class represents a specific voice model.
/// </summary>
public abstract class RvcVoiceConversionFeatureBase : IFileFeature
{
    protected static readonly ILogger Log = Serilog.Log.ForContext<RvcVoiceConversionFeatureBase>();

    /// <summary>
    /// The name of the RVC model (without .pth extension).
    /// Must match a .pth file in RVC/assets/weights/
    /// </summary>
    protected abstract string ModelName { get; }

    public abstract string Id { get; }
    public abstract string DisplayName { get; }
    public abstract string Description { get; }

    public string[] SupportedExtensions => new[] { ".mp3", ".wav" };
    public bool IsCloudBased => false; // RVC runs locally

    public async Task<FeatureResult> ExecuteAsync(string filePath, CancellationToken cancellationToken)
    {
        var startTime = DateTime.Now;

        try
        {
            Log.Information("=== RVC Voice Conversion: {ModelName} ===", ModelName);
            Log.Information("Input file: {FilePath}", filePath);

            // Validate RVC installation
            if (!RvcModelDiscoveryService.IsRvcInstalled())
            {
                var errorMsg = "RVC is not properly installed. Please ensure RVC folder exists with venv and tools/infer_cli.py";
                Log.Error(errorMsg);
                return FeatureResult.CreateFailure(errorMsg, null, (long)(DateTime.Now - startTime).TotalMilliseconds);
            }

            // Validate model exists
            var modelPath = RvcModelDiscoveryService.GetModelPath(ModelName);
            if (!File.Exists(modelPath))
            {
                var errorMsg = $"RVC model not found: {modelPath}";
                Log.Error(errorMsg);
                return FeatureResult.CreateFailure(errorMsg, null, (long)(DateTime.Now - startTime).TotalMilliseconds);
            }

            // Validate input file
            if (!File.Exists(filePath))
            {
                var errorMsg = $"Input file not found: {filePath}";
                Log.Error(errorMsg);
                return FeatureResult.CreateFailure(errorMsg, null, (long)(DateTime.Now - startTime).TotalMilliseconds);
            }

            // Determine output path
            var directory = Path.GetDirectoryName(filePath) ?? string.Empty;
            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
            var extension = Path.GetExtension(filePath);
            var outputPath = Path.Combine(directory, $"{fileNameWithoutExt}_{ModelName}{extension}");

            Log.Information("Output file: {OutputPath}", outputPath);

            // Get RVC paths
            var rvcPath = RvcModelDiscoveryService.GetRvcPath();
            var pythonExe = Path.Combine(rvcPath, "venv", "Scripts", "python.exe");
            var inferScript = Path.Combine(rvcPath, "tools", "infer_cli.py");

            // Build command line arguments for infer_cli.py
            var arguments = $"\"{inferScript}\" " +
                          $"--input_path \"{filePath}\" " +
                          $"--model_name \"{ModelName}.pth\" " +
                          $"--opt_path \"{outputPath}\" " +
                          $"--f0method rmvpe " +
                          $"--index_rate 0.66 " +
                          $"--filter_radius 3 " +
                          $"--rms_mix_rate 1 " +
                          $"--protect 0.33";

            Log.Information("Executing RVC voice conversion...");
            Log.Debug("Python: {PythonExe}", pythonExe);
            Log.Debug("Arguments: {Arguments}", arguments);

            // Execute Python script with hidden window
            var startInfo = new ProcessStartInfo
            {
                FileName = pythonExe,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true, // Hidden execution
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = rvcPath
            };

            using var process = new Process { StartInfo = startInfo };
            
            var outputBuilder = new System.Text.StringBuilder();
            var errorBuilder = new System.Text.StringBuilder();

            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    outputBuilder.AppendLine(e.Data);
                    Log.Debug("[RVC stdout] {Output}", e.Data);
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    errorBuilder.AppendLine(e.Data);
                    Log.Debug("[RVC stderr] {Error}", e.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Wait for process to complete (with cancellation support)
            await Task.Run(() => process.WaitForExit(), cancellationToken);

            var exitCode = process.ExitCode;
            var duration = (long)(DateTime.Now - startTime).TotalMilliseconds;

            Log.Information("RVC process completed with exit code: {ExitCode}", exitCode);

            if (exitCode != 0)
            {
                var errorMsg = $"RVC voice conversion failed with exit code {exitCode}";
                Log.Error(errorMsg);
                Log.Error("stderr: {StdErr}", errorBuilder.ToString());
                return FeatureResult.CreateFailure(errorMsg, null, duration);
            }

            // Verify output file was created
            if (!File.Exists(outputPath))
            {
                var errorMsg = "RVC completed but output file was not created";
                Log.Error(errorMsg);
                return FeatureResult.CreateFailure(errorMsg, null, duration);
            }

            Log.Information("RVC output file created: {OutputPath}", outputPath);

            // Post-process: Convert mono to stereo WAV for lossless quality and universal compatibility
            Log.Information("Post-processing: Converting mono to stereo WAV format...");

            var finalOutputPath = Path.Combine(directory, $"{fileNameWithoutExt}_{ModelName}.wav");

            try
            {
                var conversionSuccess = await FFMpegArguments
                    .FromFileInput(outputPath)
                    .OutputToFile(finalOutputPath, overwrite: true, options => options
                        .WithAudioCodec("pcm_s16le")  // 16-bit PCM (standard WAV)
                        .WithCustomArgument("-ac 2"))  // Force stereo (duplicates mono to L=R)
                    .CancellableThrough(cancellationToken)
                    .ProcessAsynchronously();

                if (!conversionSuccess)
                {
                    Log.Warning("FFmpeg stereo conversion failed, keeping original mono output");
                    // Don't fail the whole operation - return the mono file
                    var successMsg = $"Successfully converted voice to {ModelName} (mono)";
                    Log.Information(successMsg);
                    return FeatureResult.CreateSuccess(successMsg, outputPath, (long)(DateTime.Now - startTime).TotalMilliseconds);
                }

                // Verify stereo output was created
                if (!File.Exists(finalOutputPath))
                {
                    Log.Warning("Stereo output file was not created, keeping original mono output");
                    var successMsg = $"Successfully converted voice to {ModelName} (mono)";
                    Log.Information(successMsg);
                    return FeatureResult.CreateSuccess(successMsg, outputPath, (long)(DateTime.Now - startTime).TotalMilliseconds);
                }

                // Delete the original mono file
                try
                {
                    File.Delete(outputPath);
                    Log.Debug("Deleted intermediate mono file: {OutputPath}", outputPath);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Failed to delete intermediate mono file: {OutputPath}", outputPath);
                    // Non-critical error, continue
                }

                var finalDuration = (long)(DateTime.Now - startTime).TotalMilliseconds;
                var finalSuccessMsg = $"Successfully converted voice to {ModelName} (stereo WAV)";
                Log.Information(finalSuccessMsg);
                Log.Information("Final output: {FinalOutputPath}", finalOutputPath);
                return FeatureResult.CreateSuccess(finalSuccessMsg, finalOutputPath, finalDuration);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Post-processing failed, keeping original mono output");
                // Don't fail the whole operation - return the mono file
                var successMsg = $"Successfully converted voice to {ModelName} (mono)";
                Log.Information(successMsg);
                return FeatureResult.CreateSuccess(successMsg, outputPath, (long)(DateTime.Now - startTime).TotalMilliseconds);
            }
        }
        catch (Exception ex)
        {
            var duration = (long)(DateTime.Now - startTime).TotalMilliseconds;
            Log.Error(ex, "Error during RVC voice conversion");
            return FeatureResult.CreateFailure($"RVC voice conversion failed: {ex.Message}", ex, duration);
        }
    }
}

