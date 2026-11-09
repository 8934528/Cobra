using System.IO;
using System.Text.Json;

namespace Cobra.Core.Settings
{
    public class SettingsManager
    {
        private static SettingsManager? _instance;
        public static SettingsManager Instance => _instance ??= new SettingsManager();

        private static readonly string SettingsPath = Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData),
            "Cobra",
            "settings.json"
        );
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        public SettingsModel CurrentSettings { get; private set; }
        public event EventHandler? SettingsUpdated;

        private SettingsManager()
        {
            CurrentSettings = Load();
        }

        public void NotifySettingsUpdated()
        {
            SettingsUpdated?.Invoke(this, EventArgs.Empty);
        }

        public static SettingsModel Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    string json = File.ReadAllText(SettingsPath);
                    var settings = JsonSerializer.Deserialize<SettingsModel>(json);
                    return settings ?? new SettingsModel();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading settings: {ex.Message}");
            }

            return new SettingsModel();
        }

        public void Save()
        {
            try
            {
                string? directory = Path.GetDirectoryName(SettingsPath);
                if (directory != null && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string json = JsonSerializer.Serialize(CurrentSettings, JsonOptions);
                File.WriteAllText(SettingsPath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving settings: {ex.Message}");
            }
        }

        public void Reset()
        {
            CurrentSettings = new SettingsModel();
            Save();
        }
    }
}
