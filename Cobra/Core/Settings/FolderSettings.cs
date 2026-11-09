using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace Cobra.Core.Settings
{
    public class FolderSettings
    {
        private const string SettingsFileName = "Cobra-settings.json";
        private const string CobraFolderName = ".Cobra";

        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
        
        public SettingsModel Settings { get; }
        private readonly string _folderPath;

        public FolderSettings(string folderPath)
        {
            _folderPath = folderPath;
            Settings = Load();
        }

        private string GetSettingsPath()
        {
            return Path.Combine(_folderPath, CobraFolderName, SettingsFileName);
        }

        public SettingsModel Load()
        {
            string path = GetSettingsPath();
            try
            {
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var settings = JsonSerializer.Deserialize<SettingsModel>(json);
                    return settings ?? new SettingsModel();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading folder settings: {ex.Message}");
            }

            // return a default model (could also return global settings as base)
            return new SettingsModel();
        }

        public void Save()
        {
            string path = GetSettingsPath();
            try
            {
                string? directory = Path.GetDirectoryName(path);
                if (directory != null && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    // hide the .cobra folder on windows
                    try { File.SetAttributes(directory, File.GetAttributes(directory) | FileAttributes.Hidden); } catch (Exception ex) { Debug.WriteLine(ex.Message); }
                }

                string json = JsonSerializer.Serialize(Settings, JsonOptions);
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving folder settings: {ex.Message}");
            }
        }
    }
}
