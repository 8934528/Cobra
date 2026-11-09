using System.Diagnostics;

namespace Cobra.Core.Environment
{
    public static class NodeService
    {
        public static bool IsNodeInstalled()
        {
            try
            {
                ProcessStartInfo psi = new()
                {
                    FileName = "node",
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using Process? process = Process.Start(psi);
                if (process == null) return false;

                process.WaitForExit(5000);
                return process.ExitCode == 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return false;
            }
        }

        public static bool IsNpmInstalled()
        {
            try
            {
                ProcessStartInfo psi = new()
                {
                    FileName = "cmd.exe",
                    Arguments = "/c npm --version",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using Process? process = Process.Start(psi);
                if (process == null) return false;

                process.WaitForExit(5000);
                return process.ExitCode == 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return false;
            }
        }

        public static string GetNodeVersion()
        {
            try
            {
                ProcessStartInfo psi = new()
                {
                    FileName = "node",
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using Process? process = Process.Start(psi);
                if (process == null) return "Unknown";

                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit(5000);
                return output.Trim();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return "Not installed";
            }
        }

        public static string GetNpmVersion()
        {
            try
            {
                ProcessStartInfo psi = new()
                {
                    FileName = "cmd.exe",
                    Arguments = "/c npm --version",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using Process? process = Process.Start(psi);
                if (process == null) return "Unknown";

                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit(5000);
                return output.Trim();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return "Not installed";
            }
        }
    }
}
