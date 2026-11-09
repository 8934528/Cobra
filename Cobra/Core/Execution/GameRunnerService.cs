using System.Diagnostics;
using System.IO;

namespace Cobra.Core.Execution
{
    public class GameRunConfiguration
    {
        public string ProjectPath { get; set; } = string.Empty;
        public string PythonExecutable { get; set; } = "python";
        public string StartupFile { get; set; } = "main.py";
        public string CommandLineArguments { get; set; } = string.Empty;
        public Dictionary<string, string> EnvironmentVariables { get; set; } = [];
        public bool EnableDebugMode { get; set; }
        public List<int> BreakpointLines { get; set; } = [];
        public Dictionary<int, string> ConditionalBreakpoints { get; set; } = [];
    }

    public class DebuggerPauseState
    {
        public string FilePath { get; set; } = string.Empty;
        public int Line { get; set; }
        public string FunctionName { get; set; } = string.Empty;
        public List<string> CallStack { get; set; } = [];
        public Dictionary<string, string> LocalVariables { get; set; } = [];
        public string? ExceptionMessage { get; set; }
    }

    public class GameProcessMetricSnapshot
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public double IdeCpuPercent { get; set; }
        public double IdeMemoryMb { get; set; }
        public double GameMemoryMb { get; set; }
        public double EstimatedFps { get; set; }
        public double FrameTimeMs { get; set; }
        public bool IsGameRunning { get; set; }
    }

    public class GameRunnerService : IDisposable
    {
        private Process? _activeProcess;
        private readonly object _processLock = new();
        private DateTime _lastCpuTimeCheck = DateTime.UtcNow;
        private TimeSpan _lastIdeProcessorTime = TimeSpan.Zero;

        public event Action<string>? OutputReceived;
        public event Action<string>? ErrorReceived;
        public event Action<int, bool>? ProcessExited;
        public event Action<DebuggerPauseState>? DebuggerPaused;

        public bool IsRunning
        {
            get
            {
                lock (_processLock)
                {
                    try { return _activeProcess is { HasExited: false }; }
                    catch (Exception ex) { Debug.WriteLine(ex.Message); return false; }
                }
            }
        }

        public int? ActiveProcessId
        {
            get
            {
                lock (_processLock)
                {
                    try { return _activeProcess is { HasExited: false } ? _activeProcess.Id : null; }
                    catch (Exception ex) { Debug.WriteLine(ex.Message); return null; }
                }
            }
        }

        public async Task<bool> StartGameAsync(GameRunConfiguration config)
        {
            StopGame();

            string scriptPath = Path.IsPathRooted(config.StartupFile)
                ? config.StartupFile
                : Path.Combine(config.ProjectPath, config.StartupFile);

            if (!File.Exists(scriptPath))
            {
                ErrorReceived?.Invoke($"Startup script not found: {scriptPath}");
                return false;
            }

            try
            {
                ProcessStartInfo psi = new()
                {
                    FileName = config.PythonExecutable,
                    WorkingDirectory = config.ProjectPath,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    Environment =
                    {
                        ["PYTHONUNBUFFERED"] = "1"
                    }
                };

                foreach (var kv in config.EnvironmentVariables)
                {
                    if (!string.IsNullOrWhiteSpace(kv.Key))
                        psi.Environment[kv.Key] = kv.Value;
                }

                if (config.EnableDebugMode)
                {
                    string wrapperPath = EnsureDebugHarnessScript(config.ProjectPath, scriptPath, config.BreakpointLines, config.ConditionalBreakpoints);
                    psi.ArgumentList.Add("-u");
                    psi.ArgumentList.Add(wrapperPath);
                }
                else
                {
                    psi.ArgumentList.Add("-u");
                    psi.ArgumentList.Add(scriptPath);
                }

                if (!string.IsNullOrWhiteSpace(config.CommandLineArguments))
                {
                    foreach (var arg in SplitArguments(config.CommandLineArguments))
                        psi.ArgumentList.Add(arg);
                }

                var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
                proc.OutputDataReceived += (_, e) =>
                {
                    if (e.Data == null) return;
                    if (e.Data.StartsWith("__COBRA_DBG__:", StringComparison.Ordinal))
                    {
                        ParseDebuggerPayload(e.Data["__COBRA_DBG__:".Length..]);
                    }
                    else
                    {
                        OutputReceived?.Invoke(e.Data);
                    }
                };
                proc.ErrorDataReceived += (_, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        ErrorReceived?.Invoke(e.Data);
                };
                proc.Exited += (_, _) =>
                {
                    int code = 0;
                    try { code = proc.ExitCode; } catch (Exception ex) { Debug.WriteLine(ex.Message); }
                    bool crashed = code != 0 && code != -1;
                    ProcessExited?.Invoke(code, crashed);
                };

                lock (_processLock)
                {
                    _activeProcess = proc;
                }

                proc.Start();
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();
                await Task.CompletedTask;
                return true;
            }
            catch (Exception ex)
            {
                ErrorReceived?.Invoke($"Failed to launch game process: {ex.Message}");
                return false;
            }
        }

        public void SendDebugCommand(string command)
        {
            lock (_processLock)
            {
                try
                {
                    if (_activeProcess is { HasExited: false })
                    {
                        _activeProcess.StandardInput.WriteLine(command);
                        _activeProcess.StandardInput.Flush();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }
        }

        public void StopGame()
        {
            lock (_processLock)
            {
                if (_activeProcess == null) return;
                try
                {
                    if (!_activeProcess.HasExited)
                    {
                        _activeProcess.Kill(entireProcessTree: true);
                        _activeProcess.WaitForExit(2000);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
                finally
                {
                    try { _activeProcess.Dispose(); } catch (Exception ex) { Debug.WriteLine(ex.Message); }
                    _activeProcess = null;
                }
            }
        }

        public GameProcessMetricSnapshot CaptureMetrics(double editorFps)
        {
            var snapshot = new GameProcessMetricSnapshot
            {
                EstimatedFps = editorFps,
                FrameTimeMs = editorFps > 0 ? 1000.0 / editorFps : 16.6,
                IsGameRunning = IsRunning
            };

            try
            {
                using var currentProc = Process.GetCurrentProcess();
                snapshot.IdeMemoryMb = currentProc.WorkingSet64 / (1024.0 * 1024.0);

                var now = DateTime.UtcNow;
                var cpuTime = currentProc.TotalProcessorTime;
                double elapsedMs = (now - _lastCpuTimeCheck).TotalMilliseconds;
                if (elapsedMs > 100 && _lastIdeProcessorTime != TimeSpan.Zero)
                {
                    double cpuMs = (cpuTime - _lastIdeProcessorTime).TotalMilliseconds;
                    snapshot.IdeCpuPercent = Math.Clamp((cpuMs / (elapsedMs * System.Environment.ProcessorCount)) * 100.0, 0, 100);
                }
                _lastCpuTimeCheck = now;
                _lastIdeProcessorTime = cpuTime;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            lock (_processLock)
            {
                try
                {
                    if (_activeProcess is { HasExited: false })
                    {
                        _activeProcess.Refresh();
                        snapshot.GameMemoryMb = _activeProcess.WorkingSet64 / (1024.0 * 1024.0);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }

            return snapshot;
        }

        private void ParseDebuggerPayload(string jsonPayload)
        {
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(jsonPayload);
                var root = doc.RootElement;
                var state = new DebuggerPauseState
                {
                    FilePath = root.TryGetProperty("file", out var f) ? f.GetString() ?? "" : "",
                    Line = root.TryGetProperty("line", out var l) ? l.GetInt32() : 1,
                    FunctionName = root.TryGetProperty("func", out var fn) ? fn.GetString() ?? "" : "",
                    ExceptionMessage = root.TryGetProperty("exception", out var ex) ? ex.GetString() : null
                };

                if (root.TryGetProperty("stack", out var st) && st.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    foreach (var item in st.EnumerateArray())
                        state.CallStack.Add(item.GetString() ?? "");
                }

                if (root.TryGetProperty("locals", out var loc) && loc.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    foreach (var prop in loc.EnumerateObject())
                        state.LocalVariables[prop.Name] = prop.Value.GetString() ?? "";
                }

                DebuggerPaused?.Invoke(state);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private static string EnsureDebugHarnessScript(string projectPath, string targetScript, List<int> breakpoints, Dictionary<int, string> conditions)
        {
            string cobraDir = Path.Combine(projectPath, ".Cobra");
            Directory.CreateDirectory(cobraDir);
            string harnessPath = Path.Combine(cobraDir, "cobra_debug_runner.py");

            string bpList = string.Join(", ", breakpoints);
            var condEntries = new List<string>();
            foreach (var kv in conditions)
            {
                string escaped = kv.Value.Replace("\\", "\\\\").Replace("'", "\\'");
                condEntries.Add($"{kv.Key}: '{escaped}'");
            }
            string condDict = "{" + string.Join(", ", condEntries) + "}";
            string escapedTarget = targetScript.Replace("\\", "/");

            string code = $@"import sys, bdb, json, os, traceback

TARGET_FILE = os.path.normcase(os.path.abspath('{escapedTarget}'))
BREAKPOINTS = set([{bpList}])
CONDITIONS = {condDict}

class CobraDebugger(bdb.Bdb):
    def __init__(self):
        super().__init__()
        self.stepping = {(breakpoints.Count == 0 ? "True" : "False")}

    def user_line(self, frame):
        fname = os.path.normcase(os.path.abspath(frame.f_code.co_filename))
        if fname != TARGET_FILE:
            return
        lineno = frame.f_lineno
        should_pause = self.stepping or (lineno in BREAKPOINTS)
        if should_pause and lineno in CONDITIONS:
            try:
                should_pause = bool(eval(CONDITIONS[lineno], frame.f_globals, frame.f_locals))
            except Exception:
                should_pause = True
        if not should_pause:
            return
        self.report_and_wait(frame, None)

    def user_exception(self, frame, exc_info):
        fname = os.path.normcase(os.path.abspath(frame.f_code.co_filename))
        if fname == TARGET_FILE:
            exc_type, exc_val, _ = exc_info
            self.report_and_wait(frame, f'{{exc_type.__name__}}: {{exc_val}}')

    def report_and_wait(self, frame, exc_text):
        stack_list = []
        cur = frame
        while cur is not None:
            stack_list.append(f'{{cur.f_code.co_name}} ({{os.path.basename(cur.f_code.co_filename)}}:{{cur.f_lineno}})')
            cur = cur.f_back
        local_vars = {{}}
        for k, v in frame.f_locals.items():
            if not k.startswith('__'):
                try:
                    local_vars[k] = repr(v)[:120]
                except Exception:
                    local_vars[k] = '<unrepresentable>'
        payload = {{
            'file': frame.f_code.co_filename,
            'line': frame.f_lineno,
            'func': frame.f_code.co_name,
            'stack': stack_list,
            'locals': local_vars,
            'exception': exc_text
        }}
        print('__COBRA_DBG__:' + json.dumps(payload), flush=True)
        while True:
            cmd = sys.stdin.readline().strip()
            if cmd == 'continue':
                self.stepping = False
                self.set_continue()
                for bp in BREAKPOINTS:
                    self.set_break(TARGET_FILE, bp)
                break
            elif cmd == 'step_over':
                self.stepping = True
                self.set_next(frame)
                break
            elif cmd == 'step_into':
                self.stepping = True
                self.set_step()
                break
            elif cmd == 'step_out':
                self.stepping = True
                self.set_return(frame)
                break
            elif cmd.startswith('eval:'):
                expr = cmd[5:]
                try:
                    res = repr(eval(expr, frame.f_globals, frame.f_locals))
                except Exception as e:
                    res = f'Error: {{e}}'
                print(f'[Debug Eval] {{expr}} = {{res}}', flush=True)
            elif cmd == 'stop':
                sys.exit(0)

if __name__ == '__main__':
    dbg = CobraDebugger()
    for bp in BREAKPOINTS:
        dbg.set_break(TARGET_FILE, bp)
    sys.argv = ['{escapedTarget}'] + sys.argv[1:]
    with open('{escapedTarget}', 'r', encoding='utf-8') as f:
        code_obj = compile(f.read(), '{escapedTarget}', 'exec')
    dbg.run(code_obj, {{'__name__': '__main__', '__file__': '{escapedTarget}'}})
";
            File.WriteAllText(harnessPath, code);
            return harnessPath;
        }

        private static List<string> SplitArguments(string commandLine)
        {
            var args = new List<string>();
            bool inQuotes = false;
            var current = new System.Text.StringBuilder();

            foreach (char c in commandLine)
            {
                if (c == '\"')
                {
                    inQuotes = !inQuotes;
                }
                else if (char.IsWhiteSpace(c) && !inQuotes)
                {
                    if (current.Length > 0)
                    {
                        args.Add(current.ToString());
                        current.Clear();
                    }
                }
                else
                {
                    current.Append(c);
                }
            }

            if (current.Length > 0)
                args.Add(current.ToString());

            return args;
        }

        public void Dispose()
        {
            StopGame();
            GC.SuppressFinalize(this);
        }
    }
}

