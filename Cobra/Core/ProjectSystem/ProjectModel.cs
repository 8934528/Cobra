using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace Cobra.Core.ProjectSystem
{
    public class ProjectModel
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string Framework { get; set; } = "Pygame";
        public string TemplateId { get; set; } = "pygame-2d-starter";
        public string StartupFile { get; set; } = "main.py";
        public string PythonInterpreter { get; set; } = string.Empty;
        public string CommandLineArgs { get; set; } = string.Empty;
        public Dictionary<string, string> EnvironmentVariables { get; set; } = [];
        public string Version { get; set; } = "1.0.0";
        public string LastModified { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm");

        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        public void SaveManifest()
        {
            if (string.IsNullOrWhiteSpace(Path) || !Directory.Exists(Path)) return;
            try
            {
                string manifestPath = System.IO.Path.Combine(Path, "cobra.project.json");
                LastModified = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm");
                File.WriteAllText(manifestPath, JsonSerializer.Serialize(this, JsonOptions));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        public static ProjectModel LoadFromDirectory(string dirPath)
        {
            string manifestPath = System.IO.Path.Combine(dirPath, "cobra.project.json");
            if (File.Exists(manifestPath))
            {
                try
                {
                    var loaded = JsonSerializer.Deserialize<ProjectModel>(File.ReadAllText(manifestPath));
                    if (loaded != null)
                    {
                        loaded.Path = dirPath;
                        if (string.IsNullOrWhiteSpace(loaded.Name))
                            loaded.Name = System.IO.Path.GetFileName(dirPath);
                        return loaded;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }

            string framework = "Pygame";
            string reqPath = System.IO.Path.Combine(dirPath, "requirements.txt");
            if (File.Exists(reqPath))
            {
                string reqContent = File.ReadAllText(reqPath);
                if (reqContent.Contains("ursina", StringComparison.OrdinalIgnoreCase))
                    framework = "Ursina";
                else if (reqContent.Contains("panda3d", StringComparison.OrdinalIgnoreCase))
                    framework = "Panda3D";
            }

            return new ProjectModel
            {
                Name = System.IO.Path.GetFileName(dirPath),
                Path = dirPath,
                Framework = framework,
                StartupFile = "main.py"
            };
        }
    }
}
