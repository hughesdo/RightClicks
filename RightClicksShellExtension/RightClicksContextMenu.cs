using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Newtonsoft.Json;
using SharpShell.Attributes;
using SharpShell.SharpContextMenu;

namespace RightClicksShellExtension
{
    /// <summary>
    /// RightClicks shell extension - adds context menu items to Windows Explorer
    /// </summary>
    [ComVisible(true)]
    [COMServerAssociation(AssociationType.AllFiles)]
    public class RightClicksContextMenu : SharpContextMenu
    {
        private ContextMenuStrip menu = new ContextMenuStrip();
        private AppConfig config;
        private string rightClicksExePath;

        private void DebugLog(string message)
        {
            try
            {
                string logPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "RightClicks", "logs", "ShellExtension-Debug.log");
                Directory.CreateDirectory(Path.GetDirectoryName(logPath));
                File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss}] {message}\r\n");
            }
            catch { }
        }

        /// <summary>
        /// Determines whether the menu should be shown
        /// </summary>
        protected override bool CanShowMenu()
        {
            try
            {
                DebugLog("=== CanShowMenu called ===");

                // Do NOT load configuration here to avoid assembly load issues in Explorer.
                // Always allow menu, CreateMenu will handle config loading safely.
                DebugLog("Returning: true");
                return true;
            }
            catch (Exception ex)
            {
                DebugLog($"ERROR in CanShowMenu: {ex.Message}");
                DebugLog($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Creates the context menu
        /// </summary>
        protected override ContextMenuStrip CreateMenu()
        {
            menu.Items.Clear();

            try
            {
                // Try to load configuration, but do not fail the menu if it doesn't load
                try { LoadConfiguration(); }
                catch (Exception ex) { DebugLog($"CreateMenu LoadConfiguration error: {ex.Message}"); config = new AppConfig(); }

                // Ensure RightClicks.exe path even if config failed to load
                if (string.IsNullOrEmpty(rightClicksExePath))
                {
                    string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    rightClicksExePath = Path.Combine(localAppData, "RightClicks", "RightClicks.exe");
                }

                string filePath = SelectedItemPaths.First();
                string extension = Path.GetExtension(filePath).ToLowerInvariant();

                // Get applicable features
                var applicableFeatures = GetApplicableFeatures(extension);

                // Create parent menu item
                var parentMenu = new ToolStripMenuItem
                {
                    Text = "RightClicks",
                    Image = GetMenuIcon()
                };

                if (applicableFeatures.Any())
                {
                    // Group features by prefix (for cascading menus)
                    // Features with "Parent > Child" format will be grouped under "Parent"
                    var groupedFeatures = new Dictionary<string, List<FeatureConfig>>();
                    var ungroupedFeatures = new List<FeatureConfig>();

                    foreach (var feature in applicableFeatures)
                    {
                        // Check if DisplayName contains " > " separator for cascading menu
                        if (feature.DisplayName.Contains(" > "))
                        {
                            var parts = feature.DisplayName.Split(new[] { " > " }, 2, StringSplitOptions.None);
                            var parentName = parts[0]; // e.g., "Lip Sync"
                            var childName = parts[1];  // e.g., "fal.ai.Pixverse $.20/min"

                            if (!groupedFeatures.ContainsKey(parentName))
                            {
                                groupedFeatures[parentName] = new List<FeatureConfig>();
                            }

                            // Store feature with modified DisplayName (just the child part)
                            var childFeature = new FeatureConfig
                            {
                                Id = feature.Id,
                                DisplayName = childName, // Use only the child name in submenu
                                Description = feature.Description,
                                Enabled = feature.Enabled,
                                SupportedExtensions = feature.SupportedExtensions
                            };
                            groupedFeatures[parentName].Add(childFeature);
                        }
                        else
                        {
                            // No separator - add as top-level item
                            ungroupedFeatures.Add(feature);
                        }
                    }

                    // Add ungrouped features first (top-level items)
                    foreach (var feature in ungroupedFeatures)
                    {
                        var featureItem = new ToolStripMenuItem
                        {
                            Text = feature.DisplayName,
                            Tag = feature.Id
                        };

                        featureItem.Click += (sender, args) => ExecuteFeature(feature.Id, filePath);
                        parentMenu.DropDownItems.Add(featureItem);
                    }

                    // Add grouped features (cascading submenus)
                    foreach (var group in groupedFeatures.OrderBy(g => g.Key))
                    {
                        var groupParentItem = new ToolStripMenuItem
                        {
                            Text = group.Key // e.g., "Lip Sync"
                        };

                        // Add child items to the group parent
                        foreach (var childFeature in group.Value.OrderBy(f => f.DisplayName))
                        {
                            var childItem = new ToolStripMenuItem
                            {
                                Text = childFeature.DisplayName, // e.g., "☁️ fal.ai.Pixverse $.20/min"
                                Tag = childFeature.Id
                            };

                            childItem.Click += (sender, args) => ExecuteFeature(childFeature.Id, filePath);
                            groupParentItem.DropDownItems.Add(childItem);
                        }

                        parentMenu.DropDownItems.Add(groupParentItem);
                    }
                }
                else
                {
                    // Fallback item so the menu is visible even when config fails
                    var openUiItem = new ToolStripMenuItem
                    {
                        Text = "Open RightClicks..."
                    };
                    openUiItem.Click += (sender, args) =>
                    {
                        try
                        {
                            if (File.Exists(rightClicksExePath))
                            {
                                Process.Start(new ProcessStartInfo { FileName = rightClicksExePath, UseShellExecute = true });
                            }
                        }
                        catch (Exception ex)
                        {
                            DebugLog($"Error launching RightClicks: {ex.Message}");
                        }
                    };
                    parentMenu.DropDownItems.Add(openUiItem);
                }

                menu.Items.Add(parentMenu);
            }
            catch (Exception ex)
            {
                // Log error (could write to a log file if needed)
                Debug.WriteLine($"Error creating menu: {ex.Message}");
            }

            return menu;
        }

        /// <summary>
        /// Loads configuration from config.json
        /// </summary>
        private void LoadConfiguration()
        {
            try
            {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string configPath = Path.Combine(localAppData, "RightClicks", "config.json");
                rightClicksExePath = Path.Combine(localAppData, "RightClicks", "RightClicks.exe");

                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    config = JsonConvert.DeserializeObject<AppConfig>(json);
                }
                else
                {
                    config = new AppConfig();
                }
            }
            catch
            {
                config = new AppConfig();
            }
        }

        /// <summary>
        /// Gets features that are enabled and support the given file extension
        /// </summary>
        private List<FeatureConfig> GetApplicableFeatures(string extension)
        {
            var applicableFeatures = new List<FeatureConfig>();

            if (config?.Features == null)
            {
                return applicableFeatures;
            }

            foreach (var feature in config.Features)
            {
                // Check if feature is enabled
                if (!feature.Enabled)
                {
                    continue;
                }

                // Check if feature supports this extension
                if (feature.SupportedExtensions != null &&
                    feature.SupportedExtensions.Any(ext => ext.Equals(extension, StringComparison.OrdinalIgnoreCase)))
                {
                    applicableFeatures.Add(feature);
                }
            }

            return applicableFeatures;
        }

        /// <summary>
        /// Executes a feature by calling RightClicks.exe with --queue flag
        /// </summary>
        private void ExecuteFeature(string featureId, string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(rightClicksExePath) || !File.Exists(rightClicksExePath))
                {
                    MessageBox.Show("RightClicks.exe not found. Please reinstall RightClicks.",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Build command line arguments
                string arguments = $"--feature {featureId} --file \"{filePath}\" --queue";

                // Start RightClicks.exe
                var startInfo = new ProcessStartInfo
                {
                    FileName = rightClicksExePath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error executing feature: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Gets the menu icon from embedded resources.
        /// Falls back to null if icon cannot be loaded.
        /// </summary>
        private Image GetMenuIcon()
        {
            try
            {
                // Try to load from embedded resource
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = "RightClicksShellExtension.Resources.RightClick-16x16.ico";

                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream != null)
                    {
                        // Load icon and convert to bitmap for menu display
                        using (var icon = new Icon(stream, 16, 16))
                        {
                            return icon.ToBitmap();
                        }
                    }
                }

                DebugLog("Could not load menu icon from embedded resource");
            }
            catch (Exception ex)
            {
                DebugLog($"Error loading menu icon: {ex.Message}");
            }

            // Fallback: no icon
            return null;
        }
    }

    #region Configuration Models

    /// <summary>
    /// Application configuration model
    /// </summary>
    public class AppConfig
    {
        public List<FeatureConfig> Features { get; set; } = new List<FeatureConfig>();
        public int MaxConcurrentJobs { get; set; } = 3;
    }

    /// <summary>
    /// Feature configuration model
    /// </summary>
    public class FeatureConfig
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public bool Enabled { get; set; }
        public string[] SupportedExtensions { get; set; }
    }

    #endregion
}

