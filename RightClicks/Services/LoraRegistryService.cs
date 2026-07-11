using RightClicks.Models.FalType;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace RightClicks.Services;

/// <summary>
/// Loads the LoRA preset registry (loras.json) that lives next to the executable. Presets are DATA,
/// so Don can add more LoRAs without a code change. Presets are gated to the endpoints they're valid
/// for via <see cref="LoraPreset.compatible_endpoints"/>.
/// </summary>
public static class LoraRegistryService
{
    private static List<LoraPreset>? _cache;

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>All presets from loras.json (cached). Empty list if the file is missing/invalid.</summary>
    public static List<LoraPreset> GetAll()
    {
        if (_cache != null)
            return _cache;

        try
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "loras.json");
            if (!File.Exists(path))
            {
                Log.Warning("loras.json not found at {Path} - LoRA selector will be empty", path);
                _cache = new List<LoraPreset>();
                return _cache;
            }

            var json = File.ReadAllText(path);
            var registry = JsonSerializer.Deserialize<LoraRegistry>(json, Options);
            _cache = registry?.loras ?? new List<LoraPreset>();
            Log.Information("Loaded {Count} LoRA presets from loras.json", _cache.Count);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load loras.json");
            _cache = new List<LoraPreset>();
        }

        return _cache;
    }

    /// <summary>Presets valid for the given fal endpoint. A preset with no gate list is shown for all.</summary>
    public static List<LoraPreset> GetForEndpoint(string endpointId)
    {
        return GetAll()
            .Where(p => p.compatible_endpoints.Count == 0
                        || p.compatible_endpoints.Any(e => string.Equals(e, endpointId, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }
}
