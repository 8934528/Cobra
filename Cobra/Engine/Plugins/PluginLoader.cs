using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.Json;
using Cobra.Core.Security;

namespace Cobra.Engine.Plugins
{
    public class PluginMetadata
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = "1.0.0";
        public string Author { get; set; } = "Cobra Built-in";
        public string Category { get; set; } = "Editor Tools";
        public string Description { get; set; } = string.Empty;
        public string TargetCobraVersion { get; set; } = "1.0";
        public bool IsEnabled { get; set; } = true;
        public bool IsTrusted { get; set; } = true;
        public List<string> Dependencies { get; set; } = [];
        public string StatusMessage { get; set; } = "Compatible & Active";
    }

    public class PluginLoader
    {
        public List<IPlugin> Plugins { get; } = [];
        public List<PluginMetadata> DiscoveredPlugins { get; } = [];

        public void LoadPlugins(string folder)
        {
            Plugins.Clear();
            DiscoveredPlugins.Clear();

            RegisterBuiltInExtensions();

            if (string.IsNullOrWhiteSpace(folder)) return;

            string fullFolder = Path.IsPathRooted(folder)
                ? folder
                : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, folder);

            if (!Directory.Exists(fullFolder))
            {
                try { Directory.CreateDirectory(fullFolder); } catch (Exception ex) { Debug.WriteLine(ex.Message); }
                return;
            }

            // discover json manifests first for controlled plugin validation
            foreach (var manifestFile in Directory.GetFiles(fullFolder, "*.plugin.json"))
            {
                try
                {
                    string json = File.ReadAllText(manifestFile);
                    var meta = JsonSerializer.Deserialize<PluginMetadata>(json);
                    if (meta != null && ValidatePluginMetadata(meta, out string validationMsg))
                    {
                        meta.StatusMessage = validationMsg;
                        DiscoveredPlugins.Add(meta);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }

            // only load dll plugins that have a matching enabled and validated manifest
            foreach (var file in Directory.GetFiles(fullFolder, "*.dll"))
            {
                try
                {
                    if (!SecurityValidator.IsPathWithinDirectory(file, fullFolder))
                        continue;

                    string baseName = Path.GetFileNameWithoutExtension(file);
                    var matchingMeta = DiscoveredPlugins.FirstOrDefault(p => p.Id.Equals(baseName, StringComparison.OrdinalIgnoreCase));
                    if (matchingMeta == null)
                    {
                        DiscoveredPlugins.Add(new PluginMetadata
                        {
                            Id = baseName,
                            Name = baseName,
                            Category = "Third-Party Extension",
                            Description = "Unverified DLL plugin. Requires a valid .plugin.json manifest and explicit enablement.",
                            IsEnabled = false,
                            IsTrusted = false,
                            StatusMessage = "Blocked (Untrusted / Missing Manifest)"
                        });
                        continue;
                    }

                    if (!matchingMeta.IsEnabled || !matchingMeta.IsTrusted)
                    {
                        matchingMeta.StatusMessage = "Disabled (Untrusted third-party code)";
                        continue;
                    }

                    var assembly = Assembly.LoadFrom(file);
                    foreach (var type in assembly.GetTypes())
                    {
                        if (typeof(IPlugin).IsAssignableFrom(type) && type is { IsInterface: false, IsAbstract: false })
                        {
                            var plugin = (IPlugin)Activator.CreateInstance(type)!;
                            matchingMeta.Version = plugin.Version;
                            matchingMeta.Category = plugin.Category;
                            matchingMeta.Description = plugin.Description;
                            SafeInvoke(plugin.Initialize, matchingMeta);
                            Plugins.Add(plugin);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[PluginLoader] Isolated error loading plugin from {file}: {ex.Message}");
                }
            }
        }

        public void UpdateAll()
        {
            foreach (var plugin in Plugins)
            {
                try
                {
                    plugin.Update();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[PluginLoader] Isolated runtime error in plugin '{plugin.Name}': {ex.Message}");
                }
            }
        }

        public static bool ValidatePluginMetadata(PluginMetadata meta, out string status)
        {
            if (string.IsNullOrWhiteSpace(meta.Id) || string.IsNullOrWhiteSpace(meta.Name))
            {
                status = "Invalid plugin metadata: missing ID or Name.";
                return false;
            }

            if (!Version.TryParse(meta.Version, out _))
            {
                status = $"Invalid version format '{meta.Version}'.";
                return false;
            }

            status = meta.IsTrusted ? "Verified & Compatible" : "Untrusted Extension (Warning)";
            return true;
        }

        private static void SafeInvoke(Action action, PluginMetadata meta)
        {
            try
            {
                action();
                meta.StatusMessage = "Active";
            }
            catch (Exception ex)
            {
                meta.IsEnabled = false;
                meta.StatusMessage = $"Isolated Error: {ex.Message}";
            }
        }

        private void RegisterBuiltInExtensions()
        {
            DiscoveredPlugins.AddRange(
            [
                new PluginMetadata
                {
                    Id = "cobra.python.intelligence",
                    Name = "Python AST & Ruff Intelligence",
                    Version = "1.2.0",
                    Category = "Debugging Integrations",
                    Description = "Provides Python syntax diagnostics, unused import checks, symbol outline, and PEP-8 formatting.",
                    IsEnabled = true,
                    IsTrusted = true
                },
                new PluginMetadata
                {
                    Id = "cobra.pygame.toolkit",
                    Name = "Pygame-ce 2D Scene & Physics Toolkit",
                    Version = "1.2.0",
                    Category = "Framework Integrations",
                    Description = "2D sprite canvas, rectangle/circle collision visualizers, Pymunk physics properties, and sprite sheet animation previewer.",
                    IsEnabled = true,
                    IsTrusted = true
                },
                new PluginMetadata
                {
                    Id = "cobra.3d.frameworks",
                    Name = "Panda3D & Ursina 3D Integration",
                    Version = "1.1.0",
                    Category = "Framework Integrations",
                    Description = "Optional 3D project templates, Three.js scene viewport bridge, and 3D model inspector.",
                    IsEnabled = true,
                    IsTrusted = true
                },
                new PluginMetadata
                {
                    Id = "cobra.asset.importer",
                    Name = "Validated Game Asset Importer",
                    Version = "1.0.0",
                    Category = "Asset Importers",
                    Description = "Header-validated asset importer for PNG, JPG, WebP, WAV, OGG, MP3, OBJ, and GLTF/GLB assets.",
                    IsEnabled = true,
                    IsTrusted = true
                },
                new PluginMetadata
                {
                    Id = "cobra.packaging.pyinstaller",
                    Name = "PyInstaller Game Packager",
                    Version = "1.0.0",
                    Category = "Productivity Tools",
                    Description = "Packages Python games into standalone Windows executables with automatic asset bundling.",
                    IsEnabled = true,
                    IsTrusted = true
                }
            ]);
        }
    }
}
