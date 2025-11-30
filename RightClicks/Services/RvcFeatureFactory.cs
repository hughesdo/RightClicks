using RightClicks.Features.Audio;
using RightClicks.Models;
using Serilog;

namespace RightClicks.Services;

/// <summary>
/// Factory for creating dynamically generated RVC voice conversion features.
/// One feature is created per .pth model file found in RVC/assets/weights/.
/// </summary>
public static class RvcFeatureFactory
{
    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(RvcFeatureFactory));

    /// <summary>
    /// Create RVC features for all discovered voice models.
    /// Returns empty list if RVC is not installed or no models found.
    /// </summary>
    public static List<IFileFeature> CreateRvcFeatures()
    {
        var features = new List<IFileFeature>();

        try
        {
            // Check if RVC is installed
            if (!RvcModelDiscoveryService.IsRvcInstalled())
            {
                Log.Information("RVC not installed - skipping RVC feature generation");
                return features;
            }

            // Discover all RVC models
            var models = RvcModelDiscoveryService.DiscoverModels();

            if (models.Count == 0)
            {
                Log.Warning("No RVC models found in weights directory");
                return features;
            }

            // Create one feature per model
            foreach (var modelName in models)
            {
                var feature = new DynamicRvcFeature(modelName);
                features.Add(feature);
            }

            Log.Information("Created {Count} RVC features for models: {Models}", 
                features.Count, 
                string.Join(", ", models));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error creating RVC features");
        }

        return features;
    }

    /// <summary>
    /// Dynamically generated RVC feature class.
    /// Each instance represents a specific voice model.
    /// This class is private and should not be discovered by reflection.
    /// Instances are created only by RvcFeatureFactory.
    /// </summary>
    private sealed class DynamicRvcFeature : RvcVoiceConversionFeatureBase
    {
        private readonly string _modelName;

        public DynamicRvcFeature(string modelName)
        {
            _modelName = modelName;
        }

        protected override string ModelName => _modelName;

        public override string Id => $"Rvc{_modelName}";

        public override string DisplayName => $"RVC > {_modelName}";

        public override string Description => $"Convert voice to {_modelName} using RVC (Retrieval-based Voice Conversion)";
    }
}

