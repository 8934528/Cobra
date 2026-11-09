using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Cobra.Core.Security;

namespace Cobra.Core.Environment
{
    public class PipPackageInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
    }

    public static class PipPackageService
    {
        public static async Task<List<PipPackageInfo>> GetInstalledPackagesAsync(string pythonExe, CancellationToken cancellationToken = default)
        {
            var packages = new List<PipPackageInfo>();
            try
            {
                ProcessStartInfo psi = new()
                {
                    FileName = pythonExe,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                psi.ArgumentList.Add("-m");
                psi.ArgumentList.Add("pip");
                psi.ArgumentList.Add("list");
                psi.ArgumentList.Add("--format=json");
                psi.ArgumentList.Add("--disable-pip-version-check");

                using Process? proc = Process.Start(psi);
                if (proc == null) return packages;

                string stdout = await proc.StandardOutput.ReadToEndAsync(cancellationToken);
                await proc.WaitForExitAsync(cancellationToken);

                if (proc.ExitCode == 0 && !string.IsNullOrWhiteSpace(stdout))
                {
                    using var doc = JsonDocument.Parse(stdout);
                    foreach (var elem in doc.RootElement.EnumerateArray())
                    {
                        packages.Add(new PipPackageInfo
                        {
                            Name = elem.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "",
                            Version = elem.TryGetProperty("version", out var v) ? v.GetString() ?? "" : ""
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            return packages;
        }

        public static async Task<(bool Success, string Output)> InstallPackageAsync(
            string pythonExe,
            string packageSpec,
            string? indexUrl = null,
            IProgress<string>? progress = null,
            CancellationToken cancellationToken = default)
        {
            if (!SecurityValidator.IsValidPackageSpec(packageSpec))
                return (false, $"Invalid package specification: '{packageSpec}'.");

            var args = new List<string> { "-m", "pip", "install", packageSpec.Trim(), "--disable-pip-version-check" };
            if (!string.IsNullOrWhiteSpace(indexUrl) && Uri.TryCreate(indexUrl, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp))
            {
                args.Add("--index-url");
                args.Add(indexUrl.Trim());
            }

            return await RunPipCommandAsync(pythonExe, args, progress, cancellationToken);
        }

        public static async Task<(bool Success, string Output)> UpdatePackageAsync(
            string pythonExe,
            string packageSpec,
            IProgress<string>? progress = null,
            CancellationToken cancellationToken = default)
        {
            if (!SecurityValidator.IsValidPackageSpec(packageSpec))
                return (false, $"Invalid package specification: '{packageSpec}'.");

            var args = new List<string> { "-m", "pip", "install", "--upgrade", packageSpec.Trim(), "--disable-pip-version-check" };
            return await RunPipCommandAsync(pythonExe, args, progress, cancellationToken);
        }

        public static async Task<(bool Success, string Output)> UninstallPackageAsync(
            string pythonExe,
            string packageName,
            IProgress<string>? progress = null,
            CancellationToken cancellationToken = default)
        {
            if (!SecurityValidator.IsValidPackageSpec(packageName))
                return (false, $"Invalid package name: '{packageName}'.");

            var args = new List<string> { "-m", "pip", "uninstall", "-y", packageName.Trim() };
            return await RunPipCommandAsync(pythonExe, args, progress, cancellationToken);
        }

        public static async Task<(bool Success, string Output)> InstallRequirementsFileAsync(
            string pythonExe,
            string requirementsFilePath,
            IProgress<string>? progress = null,
            CancellationToken cancellationToken = default)
        {
            if (!File.Exists(requirementsFilePath))
                return (false, "requirements.txt file does not exist.");

            var args = new List<string> { "-m", "pip", "install", "-r", requirementsFilePath, "--disable-pip-version-check" };
            return await RunPipCommandAsync(pythonExe, args, progress, cancellationToken);
        }

        public static async Task<string> GetPackageDetailsAsync(string pythonExe, string packageName, CancellationToken cancellationToken = default)
        {
            if (!SecurityValidator.IsValidPackageSpec(packageName))
                return "Invalid package name.";

            var (_, output) = await RunPipCommandAsync(pythonExe, ["-m", "pip", "show", packageName.Trim()], null, cancellationToken);
            return string.IsNullOrWhiteSpace(output) ? "No package metadata found." : output;
        }

        public static async Task UpdateRequirementsTxtAsync(string projectPath, string packageSpec, bool remove = false)
        {
            string reqPath = Path.Combine(projectPath, "requirements.txt");
            List<string> lines = File.Exists(reqPath) ? [.. await File.ReadAllLinesAsync(reqPath)] : [];
            string baseName = packageSpec.Split(['=', '<', '>', '!', '~', '['], 2)[0].Trim();

            lines.RemoveAll(l =>
            {
                string trimmed = l.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#')) return false;
                string existingBase = trimmed.Split(['=', '<', '>', '!', '~', '['], 2)[0].Trim();
                return existingBase.Equals(baseName, StringComparison.OrdinalIgnoreCase);
            });

            if (!remove)
                lines.Add(packageSpec.Trim());

            await File.WriteAllLinesAsync(reqPath, lines);
        }

        public static List<PipPackageInfo> GetCuratedGamePackages()
        {
            return
            [
                new PipPackageInfo { Name = "pygame-ce", Version = "latest", Summary = "Primary 2D game development framework for Python (Community Edition)" },
                new PipPackageInfo { Name = "pymunk", Version = "latest", Summary = "Easy-to-use 2D physics library built on Chipmunk2D" },
                new PipPackageInfo { Name = "panda3d", Version = "latest", Summary = "Powerful, mature open-source 3D game engine for Python" },
                new PipPackageInfo { Name = "ursina", Version = "latest", Summary = "Pythonic 3D game engine powered by Panda3D" },
                new PipPackageInfo { Name = "numpy", Version = "latest", Summary = "Fast array and math computing for procedural generation and physics" },
                new PipPackageInfo { Name = "pillow", Version = "latest", Summary = "Python Imaging Library for sprite processing and texture tools" },
                new PipPackageInfo { Name = "pyinstaller", Version = "latest", Summary = "Packages Python game projects into standalone executables" },
                new PipPackageInfo { Name = "debugpy", Version = "latest", Summary = "Official Python debugger protocol implementation" },
                new PipPackageInfo { Name = "ruff", Version = "latest", Summary = "Extremely fast Python linter and code formatter" },
                new PipPackageInfo { Name = "pytest", Version = "latest", Summary = "Testing framework for Python game logic and unit tests" }
            ];
        }

        private static async Task<(bool Success, string Output)> RunPipCommandAsync(
            string pythonExe,
            IEnumerable<string> arguments,
            IProgress<string>? progress,
            CancellationToken cancellationToken)
        {
            try
            {
                ProcessStartInfo psi = new()
                {
                    FileName = pythonExe,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                foreach (var arg in arguments)
                    psi.ArgumentList.Add(arg);

                using Process? process = Process.Start(psi);
                if (process == null)
                    return (false, "Failed to start Python interpreter.");

                var outputLines = new List<string>();
                process.OutputDataReceived += (_, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        lock (outputLines) outputLines.Add(e.Data);
                        progress?.Report(e.Data);
                    }
                };
                process.ErrorDataReceived += (_, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        lock (outputLines) outputLines.Add(e.Data);
                        progress?.Report(e.Data);
                    }
                };

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                await process.WaitForExitAsync(cancellationToken);

                string fullOutput = string.Join(System.Environment.NewLine, outputLines);
                return (process.ExitCode == 0, fullOutput);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }
    }
}

