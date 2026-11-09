using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Cobra.Core.Security
{
    public static class SecurityValidator
    {
        private static readonly HashSet<string> ProtectedProjectFiles = new(StringComparer.OrdinalIgnoreCase)
        {
            "main.py",
            "cobra.project.json",
            "requirements.txt",
            ".gitignore"
        };

        private static readonly HashSet<string> AllowedAssetExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".png", ".jpg", ".jpeg", ".bmp", ".webp", ".gif",
            ".wav", ".mp3", ".ogg",
            ".glb", ".gltf", ".obj", ".egg", ".bam",
            ".ttf", ".otf", ".json", ".cobra", ".py", ".glsl", ".vert", ".frag", ".txt", ".md"
        };

        public const long MaxAssetFileSizeBytes = 100 * 1024 * 1024;

        public static bool IsValidProjectName(string name, out string error)
        {
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(name))
            {
                error = "Project name cannot be empty.";
                return false;
            }

            if (name.Length > 64)
            {
                error = "Project name cannot exceed 64 characters.";
                return false;
            }

            if (name.Contains("..") || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                error = "Project name contains invalid characters.";
                return false;
            }

            if (Regex.IsMatch(name, "^(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])$", RegexOptions.IgnoreCase))
            {
                error = "Project name is a reserved system name.";
                return false;
            }

            return true;
        }

        public static bool IsValidFileName(string name, out string error)
        {
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(name))
            {
                error = "File or folder name cannot be empty.";
                return false;
            }

            if (name.Contains("..") || name.Contains('/') || name.Contains('\\') || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                error = "Name contains invalid path characters.";
                return false;
            }

            return true;
        }

        public static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path cannot be empty.", nameof(path));

            return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        public static bool IsPathWithinDirectory(string targetPath, string baseDirectory)
        {
            try
            {
                string normalizedTarget = NormalizePath(targetPath);
                string normalizedBase = NormalizePath(baseDirectory);

                if (string.Equals(normalizedTarget, normalizedBase, StringComparison.OrdinalIgnoreCase))
                    return true;

                return normalizedTarget.StartsWith(normalizedBase + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return false;
            }
        }

        public static string ResolveSafeProjectPath(string projectRoot, string relativeOrFullPath)
        {
            string combined = Path.IsPathRooted(relativeOrFullPath)
                ? relativeOrFullPath
                : Path.Combine(projectRoot, relativeOrFullPath);

            string fullPath = NormalizePath(combined);
            if (!IsPathWithinDirectory(fullPath, projectRoot))
                throw new UnauthorizedAccessException("Access outside the project directory is not permitted.");

            return fullPath;
        }

        public static bool IsProtectedProjectFile(string projectRoot, string filePath)
        {
            try
            {
                string normalizedRoot = NormalizePath(projectRoot);
                string normalizedFile = NormalizePath(filePath);
                string? parentDir = Path.GetDirectoryName(normalizedFile);

                if (!string.Equals(parentDir, normalizedRoot, StringComparison.OrdinalIgnoreCase))
                    return false;

                return ProtectedProjectFiles.Contains(Path.GetFileName(normalizedFile));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return false;
            }
        }

        public static bool ValidateAssetImport(string sourceFilePath, out string error)
        {
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(sourceFilePath) || !File.Exists(sourceFilePath))
            {
                error = "Source asset file does not exist.";
                return false;
            }

            var fileInfo = new FileInfo(sourceFilePath);
            if (fileInfo.Length == 0)
            {
                error = "Asset file is empty.";
                return false;
            }

            if (fileInfo.Length > MaxAssetFileSizeBytes)
            {
                error = $"Asset file exceeds maximum allowed size of {MaxAssetFileSizeBytes / (1024 * 1024)} MB.";
                return false;
            }

            string ext = fileInfo.Extension;
            if (!AllowedAssetExtensions.Contains(ext))
            {
                error = $"Unsupported file extension '{ext}'.";
                return false;
            }

            if (!ValidateFileHeaderSignature(sourceFilePath, ext))
            {
                error = $"File content does not match expected format for '{ext}'.";
                return false;
            }

            return true;
        }

        public static bool ValidateFileHeaderSignature(string filePath, string extension)
        {
            try
            {
                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                byte[] header = new byte[Math.Min(12, fs.Length)];
                int read = fs.Read(header, 0, header.Length);
                if (read < 4) return true;

                return extension.ToLowerInvariant() switch
                {
                    ".png" => header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47,
                    ".jpg" or ".jpeg" => header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
                    ".gif" => header[0] == 0x47 && header[1] == 0x49 && header[2] == 0x46,
                    ".bmp" => header[0] == 0x42 && header[1] == 0x4D,
                    ".webp" => read >= 12 && header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46 && header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50,
                    ".wav" => header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46,
                    ".ogg" => header[0] == 0x4F && header[1] == 0x67 && header[2] == 0x67 && header[3] == 0x53,
                    ".mp3" => (header[0] == 0x49 && header[1] == 0x44 && header[2] == 0x33) || (header[0] == 0xFF && (header[1] & 0xE0) == 0xE0),
                    ".glb" => header[0] == 0x67 && header[1] == 0x6C && header[2] == 0x54 && header[3] == 0x46,
                    _ => true
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return false;
            }
        }

        public static bool IsValidPackageSpec(string packageSpec)
        {
            if (string.IsNullOrWhiteSpace(packageSpec) || packageSpec.Length > 128)
                return false;

            return Regex.IsMatch(packageSpec.Trim(), @"^[a-zA-Z0-9_\-\.\[\]=<>!~]+$");
        }

        public static string ProtectSecret(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return string.Empty;
            try
            {
                byte[] bytes = Encoding.UTF8.GetBytes(plainText);
                byte[] encrypted = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
                return Convert.ToBase64String(encrypted);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return string.Empty;
            }
        }

        public static string UnprotectSecret(string encryptedBase64)
        {
            if (string.IsNullOrEmpty(encryptedBase64)) return string.Empty;
            try
            {
                byte[] encrypted = Convert.FromBase64String(encryptedBase64);
                byte[] decrypted = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(decrypted);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return string.Empty;
            }
        }
    }
}

