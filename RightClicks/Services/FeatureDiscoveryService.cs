using RightClicks.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RightClicks.Services
{
    /// <summary>
    /// Service for discovering and managing feature implementations via reflection.
    /// Automatically finds all classes that implement IFileFeature interface.
    /// </summary>
    public static class FeatureDiscoveryService
    {
        private static List<IFileFeature>? _discoveredFeatures;

        /// <summary>
        /// Discovers all feature implementations in the current assembly using reflection.
        /// Features are cached after first discovery.
        /// </summary>
        /// <returns>List of all discovered feature instances.</returns>
        public static List<IFileFeature> DiscoverFeatures()
        {
            if (_discoveredFeatures != null)
            {
                Log.Debug("Returning cached features: {Count} features", _discoveredFeatures.Count);
                return _discoveredFeatures;
            }

            Log.Information("Starting feature discovery via reflection...");

            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                Log.Debug("Scanning assembly: {AssemblyName}", assembly.FullName);

                var featureTypes = assembly.GetTypes()
                    .Where(t => typeof(IFileFeature).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                    .ToList();

                Log.Debug("Found {Count} feature types", featureTypes.Count);

                _discoveredFeatures = new List<IFileFeature>();

                foreach (var type in featureTypes)
                {
                    try
                    {
                        var instance = (IFileFeature)Activator.CreateInstance(type)!;
                        _discoveredFeatures.Add(instance);
                        Log.Debug("Instantiated feature: {FeatureId} ({TypeName})", instance.Id, type.Name);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Failed to instantiate feature type: {TypeName}", type.Name);
                    }
                }

                Log.Information("Discovered {Count} features via reflection", _discoveredFeatures.Count);

                return _discoveredFeatures;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Feature discovery failed");
                return new List<IFileFeature>();
            }
        }

        /// <summary>
        /// Gets all discovered features. Returns cached features if already discovered.
        /// </summary>
        /// <returns>List of all discovered features.</returns>
        public static List<IFileFeature> GetFeatures()
        {
            return _discoveredFeatures ?? DiscoverFeatures();
        }

        /// <summary>
        /// Gets a specific feature by its ID.
        /// </summary>
        /// <param name="featureId">The feature ID to search for.</param>
        /// <returns>The feature instance, or null if not found.</returns>
        public static IFileFeature? GetFeatureById(string featureId)
        {
            var features = GetFeatures();
            var feature = features.FirstOrDefault(f => f.Id.Equals(featureId, StringComparison.OrdinalIgnoreCase));

            if (feature == null)
            {
                Log.Warning("Feature not found: {FeatureId}", featureId);
            }

            return feature;
        }

        /// <summary>
        /// Gets all features that support a specific file extension.
        /// </summary>
        /// <param name="fileExtension">The file extension (e.g., ".mp4", ".mp3").</param>
        /// <returns>List of features that support the given extension.</returns>
        public static List<IFileFeature> GetFeaturesForExtension(string fileExtension)
        {
            var features = GetFeatures();
            var normalizedExtension = fileExtension.ToLowerInvariant();

            if (!normalizedExtension.StartsWith("."))
            {
                normalizedExtension = "." + normalizedExtension;
            }

            var matchingFeatures = features
                .Where(f => f.SupportedExtensions.Any(ext => ext.Equals(normalizedExtension, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            Log.Debug("Found {Count} features for extension: {Extension}", matchingFeatures.Count, normalizedExtension);

            return matchingFeatures;
        }

        /// <summary>
        /// Clears the feature cache, forcing re-discovery on next access.
        /// Useful for testing or dynamic feature loading scenarios.
        /// </summary>
        public static void ClearCache()
        {
            Log.Debug("Clearing feature cache");
            _discoveredFeatures = null;
        }

        /// <summary>
        /// Logs all discovered features with their details.
        /// </summary>
        public static void LogDiscoveredFeatures(AppConfig config)
        {
            var features = GetFeatures();

            Log.Information("=== Discovered Features ===");
            Log.Information("Total features: {Count}", features.Count);

            foreach (var feature in features)
            {
                var featureConfig = config.Features.FirstOrDefault(fc => fc.Id.Equals(feature.Id, StringComparison.OrdinalIgnoreCase));
                var isEnabled = featureConfig?.Enabled ?? false;
                var status = isEnabled ? "Enabled" : "Disabled";

                Log.Information("Feature: {FeatureId} - {DisplayName} ({Status})", feature.Id, feature.DisplayName, status);
                Log.Debug("  Description: {Description}", feature.Description);
                Log.Debug("  Supported Extensions: {Extensions}", string.Join(", ", feature.SupportedExtensions));
            }
        }
    }
}

