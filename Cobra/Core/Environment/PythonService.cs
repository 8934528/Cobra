using System.Diagnostics;
using System.IO;

namespace Cobra.Core.Environment
{
    public class PythonInterpreterInfo
    {
        public string Path { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public bool IsVirtualEnv { get; set; }
        public string DisplayName => IsVirtualEnv ? $"{Version} (venv: {Path})" : $"{Version} ({Path})";
    }

    public static class PythonService
    {
        public static bool IsPythonInstalled(string? interpreterPath = null)
        {
            string exe = ResolveExecutable(interpreterPath);
            try
            {
                ProcessStartInfo psi = new()
                {
                    FileName = exe,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                psi.ArgumentList.Add("--version");

                using Process? process = Process.Start(psi);
                if (process == null)
                    return false;

                process.WaitForExit(5000);
                return process.ExitCode == 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return false;
            }
        }

        public static string GetPythonVersion(string? interpreterPath = null)
        {
            string exe = ResolveExecutable(interpreterPath);
            try
            {
                ProcessStartInfo psi = new()
                {
                    FileName = exe,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                psi.ArgumentList.Add("--version");

                using Process? process = Process.Start(psi);
                if (process == null) return "Unknown";

                string output = process.StandardOutput.ReadToEnd();
                string err = process.StandardError.ReadToEnd();
                process.WaitForExit(5000);

                string combined = !string.IsNullOrWhiteSpace(output) ? output.Trim() : err.Trim();
                return string.IsNullOrWhiteSpace(combined) ? "Unknown" : combined;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return "Not installed";
            }
        }

        public static List<PythonInterpreterInfo> DetectAvailableInterpreters(string? projectPath = null)
        {
            var results = new List<PythonInterpreterInfo>();
            var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(projectPath))
            {
                string venvWin = Path.Combine(projectPath, "venv", "Scripts", "python.exe");
                string dotVenvWin = Path.Combine(projectPath, ".venv", "Scripts", "python.exe");
                if (File.Exists(venvWin)) candidates.Add(venvWin);
                if (File.Exists(dotVenvWin)) candidates.Add(dotVenvWin);
            }

            string localAppData = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
            string programsPython = Path.Combine(localAppData, "Programs", "Python");
            if (Directory.Exists(programsPython))
            {
                foreach (var dir in Directory.GetDirectories(programsPython))
                {
                    string exe = Path.Combine(dir, "python.exe");
                    if (File.Exists(exe)) candidates.Add(exe);
                }
            }

            string? pathEnv = System.Environment.GetEnvironmentVariable("PATH");
            if (!string.IsNullOrEmpty(pathEnv))
            {
                foreach (var part in pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
                {
                    try
                    {
                        string exe = Path.Combine(part.Trim(), "python.exe");
                        if (File.Exists(exe) && !exe.Contains("WindowsApps", StringComparison.OrdinalIgnoreCase))
                            candidates.Add(exe);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                    }
                }
            }

            foreach (var candidate in candidates)
            {
                string version = GetPythonVersion(candidate);
                if (version != "Not installed" && version != "Unknown")
                {
                    bool isVenv = candidate.Contains($"{Path.DirectorySeparatorChar}venv{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                               || candidate.Contains($"{Path.DirectorySeparatorChar}.venv{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                               || File.Exists(Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(candidate) ?? "") ?? "", "pyvenv.cfg"));

                    results.Add(new PythonInterpreterInfo
                    {
                        Path = candidate,
                        Version = version,
                        IsVirtualEnv = isVenv
                    });
                }
            }

            if (results.Count == 0 && IsPythonInstalled("python"))
            {
                results.Add(new PythonInterpreterInfo
                {
                    Path = "python",
                    Version = GetPythonVersion("python"),
                    IsVirtualEnv = false
                });
            }

            return results;
        }

        public static string ResolveProjectPython(string projectPath, string? configuredInterpreter = null)
        {
            if (!string.IsNullOrWhiteSpace(projectPath))
            {
                string venvWin = Path.Combine(projectPath, "venv", "Scripts", "python.exe");
                if (File.Exists(venvWin)) return venvWin;

                string dotVenvWin = Path.Combine(projectPath, ".venv", "Scripts", "python.exe");
                if (File.Exists(dotVenvWin)) return dotVenvWin;

                string venvUnix = Path.Combine(projectPath, "venv", "bin", "python");
                if (File.Exists(venvUnix)) return venvUnix;
            }

            if (!string.IsNullOrWhiteSpace(configuredInterpreter) && File.Exists(configuredInterpreter))
                return configuredInterpreter;

            return "python";
        }

        public static async Task<(bool IsValid, List<string> MissingPackages)> ValidateProjectEnvironmentAsync(string projectPath, string? configuredInterpreter = null)
        {
            var missing = new List<string>();
            string pythonExe = ResolveProjectPython(projectPath, configuredInterpreter);
            if (!IsPythonInstalled(pythonExe))
                return (false, missing);

            string reqPath = Path.Combine(projectPath, "requirements.txt");
            if (!File.Exists(reqPath))
                return (true, missing);

            var installed = await PipPackageService.GetInstalledPackagesAsync(pythonExe);
            var installedNames = new HashSet<string>(installed.Select(p => NormalizePackageName(p.Name)), StringComparer.OrdinalIgnoreCase);

            foreach (var rawLine in await File.ReadAllLinesAsync(reqPath))
            {
                string line = rawLine.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith('#')) continue;

                string pkgName = line.Split(['=', '<', '>', '!', '~', '['], 2)[0].Trim();
                if (!string.IsNullOrEmpty(pkgName) && !installedNames.Contains(NormalizePackageName(pkgName)))
                {
                    // pygame-ce satisfies pygame and vice versa if imported as pygame
                    if (pkgName.Equals("pygame-ce", StringComparison.OrdinalIgnoreCase) && installedNames.Contains("pygame"))
                        continue;
                    missing.Add(pkgName);
                }
            }

            return (missing.Count == 0, missing);
        }

        private static string NormalizePackageName(string name) => name.Replace('_', '-').ToLowerInvariant();

        private static string ResolveExecutable(string? interpreterPath)
        {
            if (!string.IsNullOrWhiteSpace(interpreterPath) && File.Exists(interpreterPath))
                return interpreterPath;
            return "python";
        }
    }
}
