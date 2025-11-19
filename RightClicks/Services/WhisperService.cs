using Serilog;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Whisper.net;
using Whisper.net.Ggml;
using GgmlType = Whisper.net.Ggml.GgmlType;

namespace RightClicks.Services;

/// <summary>
/// Service for managing Whisper models and GPU detection.
/// Handles model downloads, caching, and processor creation.
/// </summary>
public class WhisperService
{
    private static readonly string ModelsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "RightClicks",
        "models",
        "whisper"
    );

    /// <summary>
    /// Ensure the models directory exists.
    /// </summary>
    public static void EnsureModelsDirectory()
    {
        if (!Directory.Exists(ModelsDirectory))
        {
            Directory.CreateDirectory(ModelsDirectory);
            Log.Information("Created Whisper models directory: {ModelsDirectory}", ModelsDirectory);
        }
    }

    /// <summary>
    /// Get or download a Whisper model.
    /// </summary>
    /// <param name="modelType">The Whisper model type to download.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Path to the model file.</returns>
    public static async Task<string> GetModelPathAsync(GgmlType modelType, CancellationToken cancellationToken)
    {
        EnsureModelsDirectory();

        var modelFileName = GetModelFileName(modelType);
        var modelPath = Path.Combine(ModelsDirectory, modelFileName);

        if (File.Exists(modelPath))
        {
            Log.Information("Whisper model already cached: {ModelType} at {ModelPath}", modelType, modelPath);
            return modelPath;
        }

        Log.Information("Downloading Whisper model: {ModelType} (this may take a few minutes)...", modelType);

        try
        {
            // Download the model using Whisper.net's downloader
            using var modelStream = await WhisperGgmlDownloader.Default.GetGgmlModelAsync(modelType);
            using var fileWriter = File.OpenWrite(modelPath);
            await modelStream.CopyToAsync(fileWriter, cancellationToken);

            Log.Information("Whisper model downloaded successfully: {ModelType}", modelType);
            return modelPath;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to download Whisper model: {ModelType}", modelType);
            throw;
        }
    }

    /// <summary>
    /// Create a Whisper processor.
    /// GPU acceleration is automatic based on installed runtime (Cuda, CoreML, etc.).
    /// </summary>
    /// <param name="modelPath">Path to the model file.</param>
    /// <returns>WhisperProcessor instance.</returns>
    public static WhisperProcessor CreateProcessor(string modelPath)
    {
        Log.Information("Creating Whisper processor from model: {ModelPath}", modelPath);

        var factory = WhisperFactory.FromPath(modelPath);
        var builder = factory.CreateBuilder();

        // Configure for optimal performance
        builder.WithLanguage("en");
        builder.WithThreads(Environment.ProcessorCount);

        var processor = builder.Build();

        Log.Information("Whisper processor created successfully (GPU acceleration automatic based on runtime)");

        return processor;
    }

    /// <summary>
    /// Get the model file name for a given model type.
    /// </summary>
    private static string GetModelFileName(GgmlType modelType)
    {
        return modelType switch
        {
            GgmlType.Tiny => "ggml-tiny.en.bin",
            GgmlType.TinyEn => "ggml-tiny.en.bin",
            GgmlType.Base => "ggml-base.en.bin",
            GgmlType.BaseEn => "ggml-base.en.bin",
            GgmlType.Small => "ggml-small.en.bin",
            GgmlType.SmallEn => "ggml-small.en.bin",
            GgmlType.Medium => "ggml-medium.en.bin",
            GgmlType.MediumEn => "ggml-medium.en.bin",
            GgmlType.LargeV1 => "ggml-large-v1.bin",
            GgmlType.LargeV2 => "ggml-large-v2.bin",
            GgmlType.LargeV3 => "ggml-large-v3.bin",
            GgmlType.LargeV3Turbo => "ggml-large-v3-turbo.bin",
            _ => throw new ArgumentException($"Unknown model type: {modelType}", nameof(modelType))
        };
    }

    /// <summary>
    /// Get a human-readable display name for a model type.
    /// </summary>
    public static string GetModelDisplayName(GgmlType modelType)
    {
        return modelType switch
        {
            GgmlType.TinyEn => "Tiny",
            GgmlType.BaseEn => "Base",
            GgmlType.SmallEn => "Small",
            GgmlType.MediumEn => "Medium",
            GgmlType.LargeV3 => "Large",
            GgmlType.LargeV3Turbo => "Turbo",
            _ => modelType.ToString()
        };
    }
}

