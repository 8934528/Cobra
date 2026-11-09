using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace Cobra.Core.Environment
{
    public class PythonDiagnostic
    {
        public string FilePath { get; set; } = string.Empty;
        public int Line { get; set; }
        public int Column { get; set; }
        public string Severity { get; set; } = "Error";
        public string Message { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string DisplayText => $"[{Severity}] {Path.GetFileName(FilePath)} (Line {Line}, Col {Column}): {Message}";
    }

    public class PythonSymbolInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Kind { get; set; } = string.Empty;
        public int Line { get; set; }
        public int Column { get; set; }
        public string Signature { get; set; } = string.Empty;
        public string Docstring { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
    }

    public static class PythonIntelligenceService
    {
        private const string AstAnalyzerScript = @"
import ast, sys, json

file_path = sys.argv[1]
with open(file_path, 'r', encoding='utf-8', errors='replace') as f:
    source = f.read()

diagnostics = []
symbols = []
defined_imports = {}
used_names = set()

try:
    tree = ast.parse(source, filename=file_path)
    for node in ast.walk(tree):
        if isinstance(node, ast.Import):
            for alias in node.names:
                bound = alias.asname or alias.name.split('.')[0]
                defined_imports[bound] = (node.lineno, node.col_offset + 1, alias.name)
        elif isinstance(node, ast.ImportFrom):
            for alias in node.names:
                if alias.name != '*':
                    bound = alias.asname or alias.name
                    defined_imports[bound] = (node.lineno, node.col_offset + 1, f'{node.module}.{alias.name}')
        elif isinstance(node, ast.Name) and isinstance(node.ctx, ast.Load):
            used_names.add(node.id)
        elif isinstance(node, (ast.FunctionDef, ast.AsyncFunctionDef)):
            args = [a.arg for a in node.args.args]
            sig = f'def {node.name}({' + ', '.join(args) + '})'
            doc = ast.get_docstring(node) or ''
            symbols.append({'name': node.name, 'kind': 'Function', 'line': node.lineno, 'col': node.col_offset + 1, 'sig': sig, 'doc': doc})
        elif isinstance(node, ast.ClassDef):
            bases = [ast.unparse(b) if hasattr(ast, 'unparse') else '' for b in node.bases]
            sig = f'class {node.name}({', '.join(bases)})' if bases else f'class {node.name}'
            doc = ast.get_docstring(node) or ''
            symbols.append({'name': node.name, 'kind': 'Class', 'line': node.lineno, 'col': node.col_offset + 1, 'sig': sig, 'doc': doc})

    for imp_name, (line, col, full_mod) in defined_imports.items():
        if imp_name not in used_names:
            diagnostics.append({
                'line': line,
                'col': col,
                'severity': 'Warning',
                'code': 'F401',
                'message': f""'{full_mod}' imported but unused""
            })
except SyntaxError as e:
    diagnostics.append({
        'line': e.lineno or 1,
        'col': e.offset or 1,
        'severity': 'Error',
        'code': 'E999',
        'message': f'SyntaxError: {e.msg}'
    })

print(json.dumps({'diagnostics': diagnostics, 'symbols': symbols}))
";

        public static async Task<(List<PythonDiagnostic> Diagnostics, List<PythonSymbolInfo> Symbols)> AnalyzePythonFileAsync(
            string pythonExe,
            string filePath,
            CancellationToken cancellationToken = default)
        {
            var diagnostics = new List<PythonDiagnostic>();
            var symbols = new List<PythonSymbolInfo>();

            if (!File.Exists(filePath) || !filePath.EndsWith(".py", StringComparison.OrdinalIgnoreCase))
                return (diagnostics, symbols);

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
                psi.ArgumentList.Add("-c");
                psi.ArgumentList.Add(AstAnalyzerScript);
                psi.ArgumentList.Add(filePath);

                using Process? proc = Process.Start(psi);
                if (proc == null) return (diagnostics, symbols);

                string stdout = await proc.StandardOutput.ReadToEndAsync(cancellationToken);
                await proc.WaitForExitAsync(cancellationToken);

                if (proc.ExitCode == 0 && !string.IsNullOrWhiteSpace(stdout))
                {
                    using var doc = JsonDocument.Parse(stdout);
                    if (doc.RootElement.TryGetProperty("diagnostics", out var diagArr))
                    {
                        foreach (var item in diagArr.EnumerateArray())
                        {
                            diagnostics.Add(new PythonDiagnostic
                            {
                                FilePath = filePath,
                                Line = item.TryGetProperty("line", out var l) ? l.GetInt32() : 1,
                                Column = item.TryGetProperty("col", out var c) ? c.GetInt32() : 1,
                                Severity = item.TryGetProperty("severity", out var s) ? s.GetString() ?? "Warning" : "Warning",
                                Code = item.TryGetProperty("code", out var cd) ? cd.GetString() ?? "" : "",
                                Message = item.TryGetProperty("message", out var m) ? m.GetString() ?? "" : ""
                            });
                        }
                    }

                    if (doc.RootElement.TryGetProperty("symbols", out var symArr))
                    {
                        foreach (var item in symArr.EnumerateArray())
                        {
                            symbols.Add(new PythonSymbolInfo
                            {
                                FilePath = filePath,
                                Name = item.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "",
                                Kind = item.TryGetProperty("kind", out var k) ? k.GetString() ?? "Symbol" : "Symbol",
                                Line = item.TryGetProperty("line", out var l) ? l.GetInt32() : 1,
                                Column = item.TryGetProperty("col", out var c) ? c.GetInt32() : 1,
                                Signature = item.TryGetProperty("sig", out var sg) ? sg.GetString() ?? "" : "",
                                Docstring = item.TryGetProperty("doc", out var dc) ? dc.GetString() ?? "" : ""
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            return (diagnostics, symbols);
        }

        public static async Task<(bool Formatted, string Code)> FormatPythonCodeAsync(string pythonExe, string filePath, string sourceCode, CancellationToken cancellationToken = default)
        {
            // Try ruff format first if installed, otherwise normalize indentation & trailing whitespace cleanly
            try
            {
                ProcessStartInfo psi = new()
                {
                    FileName = pythonExe,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                psi.ArgumentList.Add("-m");
                psi.ArgumentList.Add("ruff");
                psi.ArgumentList.Add("format");
                if (!string.IsNullOrWhiteSpace(filePath))
                {
                    psi.ArgumentList.Add("--stdin-filename");
                    psi.ArgumentList.Add(filePath);
                }
                psi.ArgumentList.Add("-");

                using Process? proc = Process.Start(psi);
                if (proc != null)
                {
                    await proc.StandardInput.WriteAsync(sourceCode);
                    proc.StandardInput.Close();
                    string formatted = await proc.StandardOutput.ReadToEndAsync(cancellationToken);
                    await proc.WaitForExitAsync(cancellationToken);

                    if (proc.ExitCode == 0 && !string.IsNullOrWhiteSpace(formatted))
                        return (true, formatted);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            // Built-in fallback PEP-8 clean formatter (normalizes tabs to 4 spaces and trims trailing whitespace)
            var lines = sourceCode.Replace("\r\n", "\n").Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                lines[i] = lines[i].Replace("\t", "    ").TrimEnd();
            }
            string result = string.Join("\n", lines).TrimEnd() + "\n";
            return (true, result);
        }
    }
}

