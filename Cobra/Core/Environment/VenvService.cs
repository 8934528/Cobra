using System.Diagnostics;
using System.IO;

namespace Cobra.Core.Environment
{
    public static class VenvService
    {
        private static readonly string TemplatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "VenvTemplate");
        private static readonly string VenvDir = Path.Combine(TemplatePath, "venv");

        public static async Task<bool> CreateProjectVenvAsync(string projectPath, string? basePython = null, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
        {
            string targetVenv = Path.Combine(projectPath, "venv");
            if (Directory.Exists(targetVenv) && ValidateVenv(projectPath))
            {
                progress?.Report("Virtual environment is already valid.");
                return true;
            }

            string pythonExe = !string.IsNullOrWhiteSpace(basePython) && File.Exists(basePython) ? basePython : "python";
            progress?.Report($"Creating virtual environment using {pythonExe}...");

            return await Task.Run(() =>
            {
                try
                {
                    ProcessStartInfo psi = new()
                    {
                        FileName = pythonExe,
                        WorkingDirectory = projectPath,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };
                    psi.ArgumentList.Add("-m");
                    psi.ArgumentList.Add("venv");
                    psi.ArgumentList.Add("venv");

                    using Process? process = Process.Start(psi);
                    if (process == null) return false;

                    while (!process.WaitForExit(500))
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            try { process.Kill(true); } catch (Exception ex) { Debug.WriteLine(ex.Message); }
                            return false;
                        }
                    }

                    return process.ExitCode == 0 && ValidateVenv(projectPath);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error creating project venv: {ex.Message}");
                    return false;
                }
            }, cancellationToken);
        }

        public static async Task EnsureTemplateExistsAsync(IProgress<string>? progress = null)
        {
            if (Directory.Exists(VenvDir)) return;

            Directory.CreateDirectory(TemplatePath);
            progress?.Report("Initializing venv template...");

            await Task.Run(() =>
            {
                try
                {
                    ProcessStartInfo psi = new()
                    {
                        FileName = "python",
                        WorkingDirectory = TemplatePath,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };
                    psi.ArgumentList.Add("-m");
                    psi.ArgumentList.Add("venv");
                    psi.ArgumentList.Add("venv");

                    using Process? process = Process.Start(psi);
                    process?.WaitForExit(60000);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error creating venv template: {ex.Message}");
                }
            });
        }

        public static async Task CopyVenvToProjectAsync(string projectPath, IProgress<string>? progress = null)
        {
            bool createdDirectly = await CreateProjectVenvAsync(projectPath, null, progress);
            if (createdDirectly) return;

            await EnsureTemplateExistsAsync(progress);
            progress?.Report("Adding virtual environment...");
            string targetVenv = Path.Combine(projectPath, "venv");

            await Task.Run(() =>
            {
                try
                {
                    if (Directory.Exists(VenvDir))
                    {
                        CopyDirectory(VenvDir, targetVenv);
                        FixVenvPaths(targetVenv);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error copying venv: {ex.Message}");
                }
            });
        }

        public static bool ValidateVenv(string projectPath)
        {
            if (!VenvExists(projectPath))
                return false;

            string winPython = Path.Combine(projectPath, "venv", "Scripts", "python.exe");
            string unixPython = Path.Combine(projectPath, "venv", "bin", "python");
            string cfgPath = Path.Combine(projectPath, "venv", "pyvenv.cfg");

            return (File.Exists(winPython) || File.Exists(unixPython)) && File.Exists(cfgPath);
        }

        public static bool VenvExists(string projectPath)
        {
            return Directory.Exists(Path.Combine(projectPath, "venv")) || Directory.Exists(Path.Combine(projectPath, ".venv"));
        }

        public static string? GetVenvPythonPath(string projectPath)
        {
            string win = Path.Combine(projectPath, "venv", "Scripts", "python.exe");
            if (File.Exists(win)) return win;

            string dotWin = Path.Combine(projectPath, ".venv", "Scripts", "python.exe");
            if (File.Exists(dotWin)) return dotWin;

            string unix = Path.Combine(projectPath, "venv", "bin", "python");
            if (File.Exists(unix)) return unix;

            return null;
        }

        private static void CopyDirectory(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);

            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }

            foreach (string dir in Directory.GetDirectories(sourceDir))
            {
                string destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
                CopyDirectory(dir, destSubDir);
            }
        }

        private static void FixVenvPaths(string venvPath)
        {
            string cfgPath = Path.Combine(venvPath, "pyvenv.cfg");
            if (!File.Exists(cfgPath)) return;

            try
            {
                var lines = File.ReadAllLines(cfgPath);
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].StartsWith("command =", StringComparison.OrdinalIgnoreCase))
                    {
                        lines[i] = $"command = python -m venv \"{venvPath}\"";
                    }
                }
                File.WriteAllLines(cfgPath, lines);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
    }
}
