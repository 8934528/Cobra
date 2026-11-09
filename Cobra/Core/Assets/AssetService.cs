using System.Diagnostics;
using System.IO;
using System.Windows.Media.Imaging;
using Cobra.Core.Security;
using Cobra.Engine.SceneSystem;

namespace Cobra.Core.Assets
{
    public enum AssetCategory
    {
        Image,
        Audio,
        Model3D,
        Font,
        Scene,
        Script,
        Shader,
        Config,
        Other
    }

    public class AssetItem
    {
        public string Name { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public string RelativePath { get; set; } = string.Empty;
        public string Extension { get; set; } = string.Empty;
        public AssetCategory Category { get; set; }
        public long SizeBytes { get; set; }
        public string FormattedSize => FormatBytes(SizeBytes);
        public string DimensionsOrInfo { get; set; } = string.Empty;
        public string IconKind { get; set; } = "File";
        public int UsageCount { get; set; }

        private static string FormatBytes(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            return $"{bytes / (1024.0 * 1024.0):F2} MB";
        }
    }

    public static class AssetService
    {
        public static List<AssetItem> ScanProjectAssets(string projectPath, string? searchFilter = null, AssetCategory? categoryFilter = null)
        {
            var results = new List<AssetItem>();
            if (string.IsNullOrWhiteSpace(projectPath) || !Directory.Exists(projectPath))
                return results;

            var scriptAndSceneContents = LoadAllReferenceTexts(projectPath);

            ScanDirectoryRecursive(projectPath, projectPath, results, scriptAndSceneContents);

            if (!string.IsNullOrWhiteSpace(searchFilter))
            {
                results =
                [
                    .. results.Where(a => a.Name.Contains(searchFilter.Trim(), StringComparison.OrdinalIgnoreCase)
                                       || a.RelativePath.Contains(searchFilter.Trim(), StringComparison.OrdinalIgnoreCase))
                ];
            }

            if (categoryFilter.HasValue)
            {
                results = [.. results.Where(a => a.Category == categoryFilter.Value)];
            }

            return [.. results.OrderBy(a => a.Category).ThenBy(a => a.Name)];
        }

        public static (bool Success, string Message, string? DestinationPath) ImportAsset(string projectPath, string sourceFilePath, string targetSubfolder = "assets")
        {
            if (!SecurityValidator.ValidateAssetImport(sourceFilePath, out string validationError))
                return (false, validationError, null);

            try
            {
                string destDir = SecurityValidator.ResolveSafeProjectPath(projectPath, targetSubfolder);
                Directory.CreateDirectory(destDir);

                string fileName = Path.GetFileName(sourceFilePath);
                if (!SecurityValidator.IsValidFileName(fileName, out string nameErr))
                    return (false, nameErr, null);

                string destPath = Path.Combine(destDir, fileName);
                if (File.Exists(destPath))
                    return (false, $"An asset named '{fileName}' already exists in '{targetSubfolder}'. Rename the file or delete the existing asset first.", null);

                File.Copy(sourceFilePath, destPath, overwrite: false);
                return (true, $"Imported '{fileName}' into {targetSubfolder}/.", destPath);
            }
            catch (Exception ex)
            {
                return (false, $"Asset import failed: {ex.Message}", null);
            }
        }

        public static List<string> FindDuplicateAssets(string projectPath)
        {
            var assets = ScanProjectAssets(projectPath);
            return
            [
                .. assets
                    .GroupBy(a => (a.Name.ToLowerInvariant(), a.SizeBytes))
                    .Where(g => g.Count() > 1)
                    .Select(g => $"{g.Key.Item1} ({g.Count()} copies: {string.Join(", ", g.Select(x => x.RelativePath))})")
            ];
        }

        public static List<string> FindMissingSceneAssets(string projectPath)
        {
            var missing = new List<string>();
            if (!Directory.Exists(projectPath)) return missing;

            foreach (var sceneFile in Directory.GetFiles(projectPath, "*.Cobra", SearchOption.AllDirectories))
            {
                try
                {
                    var scene = Scene.Load(sceneFile);
                    foreach (var obj in scene.Objects)
                    {
                        CheckAssetPath(projectPath, sceneFile, obj.Name, obj.SpritePath, missing);
                        CheckAssetPath(projectPath, sceneFile, obj.Name, obj.TexturePath, missing);
                        CheckAssetPath(projectPath, sceneFile, obj.Name, obj.ModelPath, missing);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }

            return missing;
        }

        private static void CheckAssetPath(string projectPath, string sceneFile, string objName, string assetPath, List<string> missing)
        {
            if (string.IsNullOrWhiteSpace(assetPath)) return;
            string resolved = Path.IsPathRooted(assetPath) ? assetPath : Path.Combine(projectPath, assetPath);
            if (!File.Exists(resolved))
            {
                missing.Add($"[{Path.GetFileName(sceneFile)}] Object '{objName}' references missing asset: {assetPath}");
            }
        }

        public static AssetCategory ClassifyExtension(string ext)
        {
            return ext.ToLowerInvariant() switch
            {
                ".png" or ".jpg" or ".jpeg" or ".bmp" or ".webp" or ".gif" => AssetCategory.Image,
                ".wav" or ".mp3" or ".ogg" => AssetCategory.Audio,
                ".glb" or ".gltf" or ".obj" or ".egg" or ".bam" => AssetCategory.Model3D,
                ".ttf" or ".otf" => AssetCategory.Font,
                ".cobra" => AssetCategory.Scene,
                ".py" => AssetCategory.Script,
                ".glsl" or ".vert" or ".frag" => AssetCategory.Shader,
                ".json" or ".ini" or ".cfg" or ".toml" => AssetCategory.Config,
                _ => AssetCategory.Other
            };
        }

        public static string GetCategoryIcon(AssetCategory category)
        {
            return category switch
            {
                AssetCategory.Image => "FileImage",
                AssetCategory.Audio => "MusicNote",
                AssetCategory.Model3D => "CubeOutline",
                AssetCategory.Font => "FormatFont",
                AssetCategory.Scene => "Palette",
                AssetCategory.Script => "LanguagePython",
                AssetCategory.Shader => "Creation",
                AssetCategory.Config => "CogOutline",
                _ => "FileOutline"
            };
        }

        private static void ScanDirectoryRecursive(string rootPath, string currentDir, List<AssetItem> results, List<string> referenceTexts)
        {
            try
            {
                foreach (var dir in Directory.GetDirectories(currentDir))
                {
                    string name = Path.GetFileName(dir);
                    if (name.Equals("venv", StringComparison.OrdinalIgnoreCase) ||
                        name.Equals(".venv", StringComparison.OrdinalIgnoreCase) ||
                        name.Equals("__pycache__", StringComparison.OrdinalIgnoreCase) ||
                        name.Equals(".git", StringComparison.OrdinalIgnoreCase) ||
                        name.Equals(".Cobra", StringComparison.OrdinalIgnoreCase) ||
                        name.Equals("build", StringComparison.OrdinalIgnoreCase) ||
                        name.Equals("dist", StringComparison.OrdinalIgnoreCase))
                        continue;

                    ScanDirectoryRecursive(rootPath, dir, results, referenceTexts);
                }

                foreach (var file in Directory.GetFiles(currentDir))
                {
                    var fi = new FileInfo(file);
                    if (fi.Name.StartsWith('.')) continue;

                    var category = ClassifyExtension(fi.Extension);
                    string rel = Path.GetRelativePath(rootPath, file).Replace('\\', '/');
                    int refs = referenceTexts.Count(t => t.Contains(fi.Name, StringComparison.OrdinalIgnoreCase) || t.Contains(rel, StringComparison.OrdinalIgnoreCase));

                    results.Add(new AssetItem
                    {
                        Name = fi.Name,
                        FullPath = file,
                        RelativePath = rel,
                        Extension = fi.Extension.ToLowerInvariant(),
                        Category = category,
                        SizeBytes = fi.Length,
                        DimensionsOrInfo = ExtractQuickMetadata(file, category),
                        IconKind = GetCategoryIcon(category),
                        UsageCount = refs
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private static string ExtractQuickMetadata(string filePath, AssetCategory category)
        {
            try
            {
                if (category == AssetCategory.Image)
                {
                    using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
                    if (decoder.Frames.Count > 0)
                    {
                        var frame = decoder.Frames[0];
                        return $"{frame.PixelWidth}x{frame.PixelHeight} px";
                    }
                }
                else if (category == AssetCategory.Model3D)
                {
                    string ext = Path.GetExtension(filePath).ToLowerInvariant();
                    if (ext == ".obj")
                    {
                        int vertices = 0;
                        int faces = 0;
                        foreach (var line in File.ReadLines(filePath))
                        {
                            if (line.StartsWith("v ")) vertices++;
                            else if (line.StartsWith("f ")) faces++;
                        }
                        return $"{vertices} verts, {faces} faces";
                    }
                    return $"3D Model ({ext.TrimStart('.').ToUpperInvariant()})";
                }
                else if (category == AssetCategory.Audio)
                {
                    string ext = Path.GetExtension(filePath).TrimStart('.').ToUpperInvariant();
                    return $"Audio ({ext})";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            return category.ToString();
        }

        private static List<string> LoadAllReferenceTexts(string projectPath)
        {
            var texts = new List<string>();
            try
            {
                foreach (var f in Directory.GetFiles(projectPath, "*.*", SearchOption.AllDirectories))
                {
                    if (f.Contains($"{Path.DirectorySeparatorChar}venv{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) ||
                        f.Contains($"{Path.DirectorySeparatorChar}.git{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                        continue;

                    string ext = Path.GetExtension(f).ToLowerInvariant();
                    if (ext == ".py" || ext == ".cobra" || ext == ".json")
                    {
                        texts.Add(File.ReadAllText(f));
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            return texts;
        }
    }
}

