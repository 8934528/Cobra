using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace Cobra.Core.Environment
{
    public class TerminalInfo
    {
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string ExecutablePath { get; set; } = string.Empty;
        public bool IsAvailable { get; set; }
        public string VersionInfo { get; set; } = string.Empty;
        public string PromptSymbol { get; set; } = ">";

        public override string ToString() => DisplayName;
    }

    public static class TerminalService
    {
        public const string TerminalPowerShell = "Windows PowerShell";
        public const string TerminalGitBash = "Git Bash";
        public const string TerminalCmd = "Command Prompt";
        public const string TerminalCustom = "Custom";

        private static readonly Regex AnsiRegex = new(@"\x1B\[[^@-~]*[@-~]", RegexOptions.Compiled);

        public static List<TerminalInfo> DetectAvailableTerminals(string? customPath = null)
        {
            var list = new List<TerminalInfo>();

            // windows powershell
            string psPath = FindPowerShell();
            bool psExists = File.Exists(psPath);
            list.Add(new TerminalInfo
            {
                Id = "powershell",
                DisplayName = TerminalPowerShell,
                ExecutablePath = psPath,
                IsAvailable = psExists,
                VersionInfo = psExists ? "PowerShell 5.1+" : "Not Installed",
                PromptSymbol = "PS"
            });

            // git bash
            string bashPath = FindGitBash();
            bool bashExists = File.Exists(bashPath);
            list.Add(new TerminalInfo
            {
                Id = "gitbash",
                DisplayName = TerminalGitBash,
                ExecutablePath = bashPath,
                IsAvailable = bashExists,
                VersionInfo = bashExists ? "Git Bash (MSYS2)" : "Not Found (Install Git for Windows)",
                PromptSymbol = "bash$"
            });

            // command prompt (cmd.exe)
            string cmdPath = FindCmd();
            bool cmdExists = File.Exists(cmdPath);
            list.Add(new TerminalInfo
            {
                Id = "cmd",
                DisplayName = TerminalCmd,
                ExecutablePath = cmdPath,
                IsAvailable = cmdExists,
                VersionInfo = cmdExists ? "Windows CMD" : "Not Found",
                PromptSymbol = "cmd>"
            });

            // custom terminal if configured
            if (!string.IsNullOrWhiteSpace(customPath))
            {
                bool customExists = File.Exists(customPath);
                list.Add(new TerminalInfo
                {
                    Id = "custom",
                    DisplayName = $"{TerminalCustom} ({Path.GetFileName(customPath)})",
                    ExecutablePath = customPath,
                    IsAvailable = customExists,
                    VersionInfo = customExists ? "Custom Shell" : "File Not Found",
                    PromptSymbol = "$"
                });
            }

            return list;
        }

        public static string FindPowerShell()
        {
            string systemRoot = System.Environment.GetFolderPath(System.Environment.SpecialFolder.System);
            string standardPs = Path.Combine(systemRoot, "WindowsPowerShell", "v1.0", "powershell.exe");
            if (File.Exists(standardPs)) return standardPs;

            string pwshProgramFiles = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFiles), "PowerShell", "7", "pwsh.exe");
            if (File.Exists(pwshProgramFiles)) return pwshProgramFiles;

            string? fromPath = FindInPath("powershell.exe");
            if (!string.IsNullOrEmpty(fromPath)) return fromPath;

            return standardPs;
        }

        public static string FindGitBash()
        {
            string progFiles = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFiles);
            string progFilesX86 = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFilesX86);
            string localAppData = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);

            string[] candidates =
            [
                Path.Combine(progFiles, "Git", "bin", "bash.exe"),
                Path.Combine(progFiles, "Git", "usr", "bin", "bash.exe"),
                Path.Combine(progFilesX86, "Git", "bin", "bash.exe"),
                Path.Combine(localAppData, "Programs", "Git", "bin", "bash.exe"),
                Path.Combine(localAppData, "Programs", "Git", "usr", "bin", "bash.exe")
            ];

            foreach (var candidate in candidates)
            {
                if (File.Exists(candidate))
                    return candidate;
            }

            string? fromPath = FindInPath("bash.exe");
            if (!string.IsNullOrEmpty(fromPath)) return fromPath;

            // default fallback path for display
            return Path.Combine(progFiles, "Git", "bin", "bash.exe");
        }

        public static string FindCmd()
        {
            string systemRoot = System.Environment.GetFolderPath(System.Environment.SpecialFolder.System);
            string standardCmd = Path.Combine(systemRoot, "cmd.exe");
            if (File.Exists(standardCmd)) return standardCmd;

            return "cmd.exe";
        }

        public static string? FindInPath(string executableName)
        {
            try
            {
                string? pathEnv = System.Environment.GetEnvironmentVariable("PATH");
                if (string.IsNullOrEmpty(pathEnv)) return null;

                string[] paths = pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
                foreach (string dir in paths)
                {
                    try
                    {
                        string fullPath = Path.Combine(dir.Trim(), executableName);
                        if (File.Exists(fullPath))
                            return fullPath;
                    }
                    catch
                    {
                        // ignore invalid path entries
                    }
                }
            }
            catch
            {
                // ignore environment reading failures
            }

            return null;
        }

        public static async Task<(int ExitCode, string StdOut, string StdErr)> ExecuteCommandAsync(
            string command,
            string terminalType,
            string workingDirectory,
            string? customExecutablePath = null,
            int timeoutMs = 30000)
        {
            string exe;
            var args = new List<string>();

            if (terminalType.Equals(TerminalGitBash, StringComparison.OrdinalIgnoreCase) ||
                terminalType.Equals("gitbash", StringComparison.OrdinalIgnoreCase))
            {
                exe = FindGitBash();
                if (!File.Exists(exe))
                {
                    return (-1, string.Empty,
                        $"Error: Git Bash executable was not found at '{exe}'.\n" +
                        "Please install Git for Windows (https://git-scm.com/download/win) or switch your terminal to Windows PowerShell.");
                }

                args.Add("-c");
                args.Add(command);
            }
            else if (terminalType.Equals(TerminalCmd, StringComparison.OrdinalIgnoreCase) ||
                     terminalType.Equals("cmd", StringComparison.OrdinalIgnoreCase))
            {
                exe = FindCmd();
                if (!File.Exists(exe))
                {
                    return (-1, string.Empty, $"Error: Windows Command Prompt was not found at '{exe}'.");
                }

                args.Add("/c");
                args.Add(command);
            }
            else if (terminalType.Equals(TerminalCustom, StringComparison.OrdinalIgnoreCase) ||
                     terminalType.Equals("custom", StringComparison.OrdinalIgnoreCase))
            {
                exe = customExecutablePath ?? "";
                if (string.IsNullOrEmpty(exe) || !File.Exists(exe))
                {
                    return (-1, string.Empty, $"Error: Custom terminal executable '{exe}' does not exist. Check settings.");
                }

                args.Add("/c");
                args.Add(command);
            }
            else // default to powershell
            {
                exe = FindPowerShell();
                if (!File.Exists(exe))
                {
                    return (-1, string.Empty,
                        $"Error: Windows PowerShell was not found at '{exe}'.\n" +
                        "Please check that PowerShell is enabled on Windows or choose another terminal in Settings.");
                }

                args.Add("-NoProfile");
                args.Add("-ExecutionPolicy");
                args.Add("Bypass");
                args.Add("-Command");
                args.Add(command);
            }

            var psi = new ProcessStartInfo
            {
                FileName = exe,
                WorkingDirectory = Directory.Exists(workingDirectory) ? workingDirectory : System.Environment.CurrentDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            foreach (var arg in args)
                psi.ArgumentList.Add(arg);

            return await Task.Run(() =>
            {
                try
                {
                    using var proc = Process.Start(psi);
                    if (proc == null)
                    {
                        return (-1, string.Empty, "Failed to start terminal process.");
                    }

                    string stdout = proc.StandardOutput.ReadToEnd();
                    string stderr = proc.StandardError.ReadToEnd();

                    if (!proc.WaitForExit(timeoutMs))
                    {
                        try { proc.Kill(true); } catch (Exception ex) { Debug.WriteLine(ex.Message); }
                        return (-1, stdout, "Command timed out after " + (timeoutMs / 1000) + " seconds.");
                    }

                    return (proc.ExitCode, StripAnsi(stdout), StripAnsi(stderr));
                }
                catch (Exception ex)
                {
                    return (-1, string.Empty, $"Terminal launch error: {ex.Message}");
                }
            });
        }

        public static string StripAnsi(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            return AnsiRegex.Replace(input, string.Empty);
        }
    }
}

