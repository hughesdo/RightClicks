using System.IO;
using System.Text.Json;
using RightClicksClipEditor.Models;
using Serilog;

namespace RightClicksClipEditor.Services;

/// <summary>
/// Manages user settings persistence
/// </summary>
public static class SettingsService
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "RightClicks",
        "ClipEditorSettings.json");
    
    public static ClipEditorSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                var settings = JsonSerializer.Deserialize<ClipEditorSettings>(json);
                
                if (settings != null)
                {
                    Log.Information("Settings loaded from {Path}", SettingsPath);
                    return settings;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load settings from {Path}", SettingsPath);
        }
        
        Log.Information("Using default settings");
        return new ClipEditorSettings();
    }
    
    public static void Save(ClipEditorSettings settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            File.WriteAllText(SettingsPath, json);
            
            Log.Information("Settings saved to {Path}", SettingsPath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to save settings to {Path}", SettingsPath);
        }
    }
}

