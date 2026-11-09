using System.IO;
using System.Text.Json;

namespace Cobra.Engine.Prefabs
{
    public class PrefabManager
    {
        private readonly string _prefabFolder;
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        public PrefabManager(string projectPath)
        {
            _prefabFolder = Path.Combine(projectPath, "prefabs");

            if (!Directory.Exists(_prefabFolder))
                Directory.CreateDirectory(_prefabFolder);
        }

        public void SavePrefab(string name, object gameObject)
        {
            string json = JsonSerializer.Serialize(gameObject, JsonOptions);

            File.WriteAllText(Path.Combine(_prefabFolder, name + ".json"), json);
        }

        public T? LoadPrefab<T>(string name)
        {
            string path = Path.Combine(_prefabFolder, name + ".json");

            if (!File.Exists(path))
                return default;

            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<T>(json);
        }
    }
}
