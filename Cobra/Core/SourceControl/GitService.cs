using System.Diagnostics;
using System.IO;

namespace Cobra.Core.SourceControl
{
    public class GitFileStatus
    {
        public string FilePath { get; set; } = string.Empty;
        public string StatusCode { get; set; } = string.Empty;
        public bool IsStaged { get; set; }
        public bool HasConflict { get; set; }
        public string DisplayText => $"{(IsStaged ? "[Staged] " : "[Modified] ")}{(HasConflict ? "[CONFLICT] " : "")}{StatusCode}  {FilePath}";
    }

    public static class GitService
    {
        public static bool IsGitRepository(string projectPath)
        {
            return !string.IsNullOrWhiteSpace(projectPath) && Directory.Exists(Path.Combine(projectPath, ".git"));
        }

        public static async Task<(bool Success, string Output)> InitRepositoryAsync(string projectPath)
        {
            return await RunGitAsync(projectPath, ["init"]);
        }

        public static async Task<string> GetCurrentBranchAsync(string projectPath)
        {
            if (!IsGitRepository(projectPath)) return "Not a Git repo";
            var (ok, output) = await RunGitAsync(projectPath, ["branch", "--show-current"]);
            if (ok && !string.IsNullOrWhiteSpace(output)) return output.Trim();
            return "main";
        }

        public static async Task<List<string>> GetBranchesAsync(string projectPath)
        {
            var branches = new List<string>();
            if (!IsGitRepository(projectPath)) return branches;

            var (ok, output) = await RunGitAsync(projectPath, ["branch", "--format=%(refname:short)"]);
            if (ok && !string.IsNullOrWhiteSpace(output))
            {
                foreach (var line in output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
                    branches.Add(line.Trim());
            }
            return branches;
        }

        public static async Task<List<GitFileStatus>> GetStatusAsync(string projectPath)
        {
            var results = new List<GitFileStatus>();
            if (!IsGitRepository(projectPath)) return results;

            var (ok, output) = await RunGitAsync(projectPath, ["status", "--porcelain"]);
            if (!ok || string.IsNullOrWhiteSpace(output)) return results;

            foreach (var rawLine in output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
            {
                if (rawLine.Length < 4) continue;
                char indexStatus = rawLine[0];
                char workTreeStatus = rawLine[1];
                string file = rawLine[3..].Trim();

                bool isConflict = indexStatus == 'U' || workTreeStatus == 'U' || (indexStatus == 'A' && workTreeStatus == 'A');
                bool isStaged = indexStatus != ' ' && indexStatus != '?';

                results.Add(new GitFileStatus
                {
                    FilePath = file,
                    StatusCode = rawLine[..2].Trim(),
                    IsStaged = isStaged,
                    HasConflict = isConflict
                });
            }

            return results;
        }

        public static async Task<(bool Success, string Output)> StageFileAsync(string projectPath, string relativeFilePath)
        {
            return await RunGitAsync(projectPath, ["add", "--", relativeFilePath]);
        }

        public static async Task<(bool Success, string Output)> StageAllAsync(string projectPath)
        {
            return await RunGitAsync(projectPath, ["add", "-A"]);
        }

        public static async Task<(bool Success, string Output)> UnstageFileAsync(string projectPath, string relativeFilePath)
        {
            return await RunGitAsync(projectPath, ["reset", "HEAD", "--", relativeFilePath]);
        }

        public static async Task<(bool Success, string Output)> CommitAsync(string projectPath, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return (false, "Commit message cannot be empty.");

            return await RunGitAsync(projectPath, ["commit", "-m", message.Trim()]);
        }

        public static async Task<List<string>> GetCommitHistoryAsync(string projectPath, int maxCount = 25)
        {
            var commits = new List<string>();
            if (!IsGitRepository(projectPath)) return commits;

            var (ok, output) = await RunGitAsync(projectPath, ["log", $"-n {maxCount}", "--oneline", "--decorate"]);
            if (ok && !string.IsNullOrWhiteSpace(output))
            {
                commits.AddRange(output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries));
            }
            return commits;
        }

        public static async Task<string> GetFileDiffAsync(string projectPath, string? relativeFilePath = null)
        {
            var args = new List<string> { "diff" };
            if (!string.IsNullOrWhiteSpace(relativeFilePath))
            {
                args.Add("--");
                args.Add(relativeFilePath);
            }

            var (_, output) = await RunGitAsync(projectPath, args);
            return string.IsNullOrWhiteSpace(output) ? "No unstaged diff changes." : output;
        }

        public static async Task<(bool Success, string Output)> CreateOrSwitchBranchAsync(string projectPath, string branchName, bool createNew)
        {
            if (string.IsNullOrWhiteSpace(branchName))
                return (false, "Branch name cannot be empty.");

            return createNew
                ? await RunGitAsync(projectPath, ["checkout", "-b", branchName.Trim()])
                : await RunGitAsync(projectPath, ["checkout", branchName.Trim()]);
        }

        public static async Task<(bool Success, string Output)> PullAsync(string projectPath) => await RunGitAsync(projectPath, ["pull"]);
        public static async Task<(bool Success, string Output)> PushAsync(string projectPath) => await RunGitAsync(projectPath, ["push"]);
        public static async Task<(bool Success, string Output)> FetchAsync(string projectPath) => await RunGitAsync(projectPath, ["fetch"]);

        private static async Task<(bool Success, string Output)> RunGitAsync(string workingDir, IEnumerable<string> arguments, CancellationToken cancellationToken = default)
        {
            try
            {
                ProcessStartInfo psi = new()
                {
                    FileName = "git",
                    WorkingDirectory = workingDir,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    Environment =
                    {
                        ["GIT_TERMINAL_PROMPT"] = "0"
                    }
                };

                foreach (var arg in arguments)
                    psi.ArgumentList.Add(arg);

                using Process? proc = Process.Start(psi);
                if (proc == null)
                    return (false, "Failed to start git executable.");

                string stdout = await proc.StandardOutput.ReadToEndAsync(cancellationToken);
                string stderr = await proc.StandardError.ReadToEndAsync(cancellationToken);
                await proc.WaitForExitAsync(cancellationToken);

                string combined = !string.IsNullOrWhiteSpace(stdout) ? stdout.Trim() : stderr.Trim();
                return (proc.ExitCode == 0, combined);
            }
            catch (Exception ex)
            {
                return (false, $"Git error: {ex.Message}");
            }
        }
    }
}

