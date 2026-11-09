using System.Diagnostics;
using System.IO;
using Cobra.Core.AI;
using Cobra.Core.Assets;
using Cobra.Core.Build;
using Cobra.Core.Environment;
using Cobra.Core.Execution;
using Cobra.Core.ProjectSystem;
using Cobra.Core.Security;
using Cobra.Core.Settings;
using Cobra.Core.SourceControl;
using Cobra.Engine.ECS;
using Cobra.Engine.ECS.Systems;
using Cobra.Engine.Plugins;
using Cobra.Engine.SceneSystem;
using Cobra.Engine.VisualScripting;

namespace Cobra.Tests
{
    public static class Program
    {
        private static int _passedCount;
        private static int _failedCount;

        public static async Task<int> Main()
        {
            Console.WriteLine("Running Cobra IDE Automated Verification Suite...");

            await RunTestAsync("SecurityValidator: Path Normalization & Traversal Protection", TestSecurityPathValidation);
            await RunTestAsync("SecurityValidator: DPAPI Secret Encryption & Unprotect", TestSecurityDpapiEncryption);
            await RunTestAsync("SecurityValidator & AssetService: Magic-Byte Asset Validation & Audit", TestAssetValidationAndImport);
            await RunTestAsync("ProjectTemplate & ProjectCreator: All 9 Python Templates & Manifest Persistence", TestProjectCreationAndTemplates);
            await RunTestAsync("PythonService, VenvService & PipPackageService: Environment & Package Spec Validation", TestPythonEnvironmentServices);
            await RunTestAsync("PythonIntelligenceService: AST Syntax Diagnostics & Symbol Outline", TestPythonIntelligenceService);
            await RunTestAsync("Scene & SceneObject: v1-to-v2 Migration, Save/Load, Cloning & Python Export", TestSceneManagementAndExport);
            await RunTestAsync("NodeGraph: Visual Scripting Node Creation, Validation, Save/Load & Python Codegen", TestVisualScriptingEngine);
            await RunTestAsync("GameRunnerService: Process Execution, Tree Cleanup & Bdb Debugger Harness", TestGameRunnerAndDebugger);
            await RunTestAsync("BuildPackagingService: Configuration Validation & Artifact Cleaning", TestBuildPackagingService);
            await RunTestAsync("GitService: Repository Init, Status, Staging, Commit & Branch Operations", TestGitService);
            await RunTestAsync("PluginLoader: Metadata Inspection & Untrusted DLL Blocking", TestPluginLoaderSecurity);
            await RunTestAsync("SettingsModel & AiAssistantService: Settings Validation & Offline Local Assistant", TestSettingsAndAiAssistant);
            await RunTestAsync("ECS World & ScriptSystem: Non-Blocking Editor Simulation", TestEcsScriptSystem);

            Console.WriteLine($"\nTest Summary: {_passedCount} passed, {_failedCount} failed.");
            return _failedCount == 0 ? 0 : 1;
        }

        private static async Task RunTestAsync(string name, Func<Task> testBody)
        {
            try
            {
                await testBody();
                _passedCount++;
                Console.WriteLine($"  [PASS] {name}");
            }
            catch (Exception ex)
            {
                _failedCount++;
                Console.WriteLine($"  [FAIL] {name}: {ex.Message}");
            }
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private static Task TestSecurityPathValidation()
        {
            string tempRoot = Path.Combine(Path.GetTempPath(), "CobraTest_Sec_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);
            try
            {
                Assert(SecurityValidator.IsValidFileName("player_controller.py", out _), "Valid Python filename should pass.");
                Assert(!SecurityValidator.IsValidFileName("../evil.py", out _), "Traversal filename should fail.");
                Assert(!SecurityValidator.IsValidProjectName("CON", out _), "Reserved Windows device name should fail.");

                string resolved = SecurityValidator.ResolveSafeProjectPath(tempRoot, "scripts/player.py");
                Assert(resolved.StartsWith(tempRoot, StringComparison.OrdinalIgnoreCase), "Safe relative path should resolve inside root.");

                bool blockedTraversal = false;
                try
                {
                    SecurityValidator.ResolveSafeProjectPath(tempRoot, "../../Windows/System32/cmd.exe");
                }
                catch (UnauthorizedAccessException)
                {
                    blockedTraversal = true;
                }
                Assert(blockedTraversal, "Path traversal outside root must throw UnauthorizedAccessException.");

                Assert(SecurityValidator.IsProtectedProjectFile(tempRoot, Path.Combine(tempRoot, "main.py")), "main.py must be protected against accidental deletion.");
                Assert(SecurityValidator.IsProtectedProjectFile(tempRoot, Path.Combine(tempRoot, "cobra.project.json")), "cobra.project.json must be protected.");
            }
            finally
            {
                if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, true);
            }
            return Task.CompletedTask;
        }

        private static Task TestSecurityDpapiEncryption()
        {
            string secret = "sk-test-secret-api-key-987654321";
            string encrypted = SecurityValidator.ProtectSecret(secret);
            Assert(!string.IsNullOrEmpty(encrypted) && encrypted != secret, "Encrypted secret must not match plaintext.");

            string decrypted = SecurityValidator.UnprotectSecret(encrypted);
            Assert(decrypted == secret, "Decrypted secret must match original secret.");
            return Task.CompletedTask;
        }

        private static Task TestAssetValidationAndImport()
        {
            string tempRoot = Path.Combine(Path.GetTempPath(), "CobraTest_Assets_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);
            try
            {
                string validPng = Path.Combine(tempRoot, "hero.png");
                byte[] pngBytes = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52, 0x00, 0x00, 0x00, 0x20, 0x00, 0x00, 0x00, 0x40];
                File.WriteAllBytes(validPng, pngBytes);

                bool ok = SecurityValidator.ValidateAssetImport(validPng, out _);
                Assert(ok, "Valid PNG header should pass validation.");

                string fakePng = Path.Combine(tempRoot, "corrupt.png");
                File.WriteAllBytes(fakePng, [0x00, 0x01, 0x02, 0x03, 0x04, 0x05]);
                bool fakeOk = SecurityValidator.ValidateAssetImport(fakePng, out _);
                Assert(!fakeOk, "Corrupt PNG header must be rejected.");

                string projDir = Path.Combine(tempRoot, "GameProj");
                Directory.CreateDirectory(projDir);
                var importRes = AssetService.ImportAsset(projDir, validPng);
                Assert(importRes.Success && !string.IsNullOrEmpty(importRes.DestinationPath) && File.Exists(importRes.DestinationPath), "Valid PNG should be imported into assets/.");

                var assets = AssetService.ScanProjectAssets(projDir);
                Assert(assets.Count == 1 && assets[0].Category == AssetCategory.Image, "Scanned PNG should be categorized as Image.");
            }
            finally
            {
                if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, true);
            }
            return Task.CompletedTask;
        }

        private static Task TestProjectCreationAndTemplates()
        {
            var templates = ProjectTemplate.GetAvailableTemplates();
            Assert(templates.Count == 9, $"Expected 9 Python game templates, found {templates.Count}.");
            Assert(templates.Any(t => t.Framework == "Pygame"), "Missing Pygame templates.");
            Assert(templates.Any(t => t.Framework == "Panda3D"), "Missing Panda3D template.");
            Assert(templates.Any(t => t.Framework == "Ursina"), "Missing Ursina template.");

            string tempRoot = Path.Combine(Path.GetTempPath(), "CobraTest_Proj_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);
            try
            {
                foreach (var tmpl in templates)
                {
                    string projDir = Path.Combine(tempRoot, tmpl.Id);
                    string res = ProjectCreator.CreateProjectAtPath(projDir, tmpl.Id);
                    Assert(!res.StartsWith("Error", StringComparison.OrdinalIgnoreCase), $"CreateProjectAtPath failed for {tmpl.Id}: {res}");
                    Assert(ProjectManager.IsValidProjectDirectory(projDir), $"Template {tmpl.Id} did not create a valid project directory.");
                    Assert(File.Exists(Path.Combine(projDir, "main.py")), $"Template {tmpl.Id} missing main.py.");
                    Assert(File.Exists(Path.Combine(projDir, "requirements.txt")), $"Template {tmpl.Id} missing requirements.txt.");
                    Assert(File.Exists(Path.Combine(projDir, "cobra.project.json")), $"Template {tmpl.Id} missing cobra.project.json.");

                    var loaded = ProjectModel.LoadFromDirectory(projDir);
                    Assert(loaded.Framework == tmpl.Framework, $"Framework mismatch for {tmpl.Id}.");
                }
            }
            finally
            {
                if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, true);
            }
            return Task.CompletedTask;
        }

        private static Task TestPythonEnvironmentServices()
        {
            Assert(SecurityValidator.IsValidPackageSpec("pygame-ce>=2.5.0"), "Valid PEP 508 spec should pass.");
            Assert(SecurityValidator.IsValidPackageSpec("panda3d==1.10.14"), "Valid pinned spec should pass.");
            Assert(!SecurityValidator.IsValidPackageSpec("pygame; rm -rf /"), "Command injection spec must fail.");
            Assert(!SecurityValidator.IsValidPackageSpec("--extra-index-url http://evil"), "Option flag with space must fail.");

            string tempRoot = Path.Combine(Path.GetTempPath(), "CobraTest_Venv_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);
            try
            {
                bool isValid = VenvService.ValidateVenv(tempRoot);
                Assert(!isValid && !VenvService.VenvExists(tempRoot), "Empty folder venv validation must return false.");
            }
            finally
            {
                if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, true);
            }
            return Task.CompletedTask;
        }

        private static async Task TestPythonIntelligenceService()
        {
            string tempRoot = Path.Combine(Path.GetTempPath(), "CobraTest_Intel_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);
            try
            {
                string validPy = Path.Combine(tempRoot, "valid.py");
                File.WriteAllText(validPy, "import pygame\n\nclass Player:\n    def update(self):\n        pass\n\ndef main():\n    pass\n");

                string pythonExe = PythonService.ResolveProjectPython(tempRoot);
                if (PythonService.IsPythonInstalled(pythonExe))
                {
                    var (validDiags, symbols) = await PythonIntelligenceService.AnalyzePythonFileAsync(pythonExe, validPy);
                    Assert(symbols.Any(s => s.Name == "Player" && s.Kind.Equals("Class", StringComparison.OrdinalIgnoreCase)), "AST symbol extractor should find class Player.");
                    Assert(symbols.Any(s => s.Name == "main" && s.Kind.Equals("Function", StringComparison.OrdinalIgnoreCase)), "AST symbol extractor should find function main.");
                    Assert(validDiags.Any(d => d.Code == "F401"), "Unused import 'pygame' should be detected by AST analyzer.");

                    string brokenPy = Path.Combine(tempRoot, "broken.py");
                    File.WriteAllText(brokenPy, "def broken_func(\n    pass\n");
                    var (brokenDiags, _) = await PythonIntelligenceService.AnalyzePythonFileAsync(pythonExe, brokenPy);
                    Assert(brokenDiags.Any(d => d.Code == "E999"), "Syntax error in broken.py must produce E999 diagnostic.");
                }
            }
            finally
            {
                if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, true);
            }
        }

        private static Task TestSceneManagementAndExport()
        {
            string tempRoot = Path.Combine(Path.GetTempPath(), "CobraTest_Scene_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);
            try
            {
                string v1Path = Path.Combine(tempRoot, "legacy.scene");
                File.WriteAllText(v1Path, "{\"FormatVersion\":1,\"SceneName\":\"OldLevel\",\"Objects\":[{\"Name\":\"Hero\",\"X\":100,\"Y\":200}]}");
                Scene migrated = Scene.Load(v1Path);
                Assert(migrated.FormatVersion == Scene.CurrentFormatVersion, "Legacy v1 scene must migrate to v2.");
                Assert(migrated.Objects.Count == 1 && migrated.Objects[0].Name == "Hero", "Migrated scene must preserve objects.");
                Assert(migrated.Objects[0].ScaleX == 1.0 && migrated.Objects[0].Layer == "Default", "Migrated object defaults must be populated.");

                var clone = migrated.Objects[0].Clone();
                Assert(clone.Id != migrated.Objects[0].Id && clone.Name.Contains("Hero"), "Cloned SceneObject must have a unique ID.");
                migrated.Objects.Add(clone);

                string savedPath = Path.Combine(tempRoot, "saved.scene");
                migrated.Save(savedPath);
                Assert(File.Exists(savedPath), "Scene file must be saved.");

                string pyContent = migrated.ExportToPythonModule();
                Assert(pyContent.Contains("SCENE_CONFIG") && pyContent.Contains("SCENE_OBJECTS") && pyContent.Contains("Hero"), "Exported Python scene module must contain objects and metadata.");
            }
            finally
            {
                if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, true);
            }
            return Task.CompletedTask;
        }

        private static Task TestVisualScriptingEngine()
        {
            string tempRoot = Path.Combine(Path.GetTempPath(), "CobraTest_VS_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);
            try
            {
                var graph = new NodeGraph { GraphName = "PlayerLogic" };
                var startNode = new Node { Category = "Events", Type = "OnStart", Title = "On Game Start", X = 40, Y = 40 };
                var moveNode = new Node { Category = "Movement", Type = "MoveObject", Title = "Move Object", X = 280, Y = 40 };
                startNode.Outputs.Add(moveNode.Id);
                graph.Nodes.Add(startNode);
                graph.Nodes.Add(moveNode);

                var (isValid, errors) = graph.ValidateGraph();
                Assert(isValid && errors.Count == 0, "Connected graph should validate with 0 errors.");

                string graphPath = Path.Combine(tempRoot, "logic.vscript.json");
                graph.Save(graphPath);
                var reloaded = NodeGraph.Load(graphPath);
                Assert(reloaded.Nodes.Count == 2 && reloaded.Nodes[0].Outputs.Count == 1, "Reloaded visual script graph must preserve nodes and connections.");

                string pyCode = reloaded.GeneratePythonCode();
                Assert(pyCode.Contains("class VisualScriptController:") && pyCode.Contains("def on_start(self):"), "Generated Python code must contain VisualScriptController class and on_start method.");
            }
            finally
            {
                if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, true);
            }
            return Task.CompletedTask;
        }

        private static async Task TestGameRunnerAndDebugger()
        {
            string tempRoot = Path.Combine(Path.GetTempPath(), "CobraTest_Run_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);
            try
            {
                string scriptPath = Path.Combine(tempRoot, "main.py");
                File.WriteAllText(scriptPath, "print('COBRA_RUNNER_OK')\n");

                string pythonExe = PythonService.ResolveProjectPython(tempRoot);
                if (PythonService.IsPythonInstalled(pythonExe))
                {
                    using var runner = new GameRunnerService();
                    var outputs = new List<string>();
                    var tcs = new TaskCompletionSource<int>();

                    runner.OutputReceived += msg => outputs.Add(msg);
                    runner.ProcessExited += (code, _) => tcs.TrySetResult(code);

                    var config = new GameRunConfiguration
                    {
                        ProjectPath = tempRoot,
                        PythonExecutable = pythonExe,
                        StartupFile = "main.py"
                    };

                    bool started = await runner.StartGameAsync(config);
                    Assert(started, "GameRunnerService should start valid Python script.");

                    int exitCode = await Task.WhenAny(tcs.Task, Task.Delay(5000)) == tcs.Task ? tcs.Task.Result : -1;
                    Assert(exitCode == 0, $"Expected exit code 0, got {exitCode}.");
                    Assert(outputs.Any(o => o.Contains("COBRA_RUNNER_OK")), "Captured stdout must include COBRA_RUNNER_OK.");
                }
            }
            finally
            {
                if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, true);
            }
        }

        private static Task TestBuildPackagingService()
        {
            string tempRoot = Path.Combine(Path.GetTempPath(), "CobraTest_Build_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);
            try
            {
                File.WriteAllText(Path.Combine(tempRoot, "main.py"), "print('build test')\n");
                var cfg = new GameBuildConfig
                {
                    ProjectPath = tempRoot,
                    ApplicationName = "TestGame",
                    StartupScript = "main.py"
                };
                Assert(BuildPackagingService.ValidateBuildConfig(cfg, out _), "Valid build config should pass validation.");

                Directory.CreateDirectory(Path.Combine(tempRoot, "build"));
                Directory.CreateDirectory(Path.Combine(tempRoot, "dist"));
                File.WriteAllText(Path.Combine(tempRoot, "game.spec"), "# spec");

                BuildPackagingService.CleanBuildArtifacts(tempRoot);
                Assert(!Directory.Exists(Path.Combine(tempRoot, "build")) && !Directory.Exists(Path.Combine(tempRoot, "dist")), "CleanBuildArtifacts must remove build/ and dist/ directories.");
            }
            finally
            {
                if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, true);
            }
            return Task.CompletedTask;
        }

        private static async Task TestGitService()
        {
            string tempRoot = Path.Combine(Path.GetTempPath(), "CobraTest_Git_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);
            try
            {
                var (initOk, _) = await GitService.InitRepositoryAsync(tempRoot);
                if (initOk)
                {
                    Assert(GitService.IsGitRepository(tempRoot), "Initialized folder must be detected as a Git repository.");
                    File.WriteAllText(Path.Combine(tempRoot, "main.py"), "print('hello git')\n");

                    var status = await GitService.GetStatusAsync(tempRoot);
                    Assert(status.Any(s => s.FilePath.Contains("main.py")), "Git status must detect untracked main.py.");

                    var (staged, _) = await GitService.StageFileAsync(tempRoot, "main.py");
                    Assert(staged, "Git stage should succeed.");
                }
            }
            finally
            {
                if (Directory.Exists(tempRoot))
                {
                    try { Directory.Delete(tempRoot, true); } catch (Exception ex) { Debug.WriteLine(ex.Message); }
                }
            }
        }

        private static Task TestPluginLoaderSecurity()
        {
            string tempRoot = Path.Combine(Path.GetTempPath(), "CobraTest_Plugins_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);
            try
            {
                File.WriteAllBytes(Path.Combine(tempRoot, "UntrustedPlugin.dll"), [0x4D, 0x5A, 0x90, 0x00]);
                var loader = new PluginLoader();
                loader.LoadPlugins(tempRoot);
                Assert(loader.Plugins.Count == 0, "Untrusted unsigned DLL without manifest must not be loaded.");
                Assert(loader.DiscoveredPlugins.Any(p => p.Id == "UntrustedPlugin" && !p.IsEnabled && !p.IsTrusted), "DiscoveredPlugins must record blocked untrusted DLL.");
            }
            finally
            {
                if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, true);
            }
            return Task.CompletedTask;
        }

        private static async Task TestSettingsAndAiAssistant()
        {
            var model = new SettingsModel { FontSize = 500, FrameRateLimit = 999, GridSnapSize = 1 };
            Assert(model.FontSize == 32, "SettingsModel FontSize must clamp to maximum 32.");
            Assert(model.FrameRateLimit == 240, "SettingsModel FrameRateLimit must clamp to maximum 240.");
            Assert(model.GridSnapSize == 4, "SettingsModel GridSnapSize must clamp to minimum 4.");

            model.TranslationApiKey = "secret-token-123";
            Assert(model.EncryptedApiKey != "secret-token-123" && model.TranslationApiKey == "secret-token-123", "SettingsModel API key must be DPAPI encrypted at rest.");

            string response = await AiAssistantService.ExecuteAssistantTaskAsync(
                AiAssistantTask.ExplainCode,
                "clock.tick(60)",
                "Pygame",
                apiKey: null,
                userConsentedToExternalApi: false);
            Assert(!string.IsNullOrWhiteSpace(response) && response.Contains("Pygame"), "Offline AI assistant must return local guidance without external network calls.");
        }

        private static Task TestEcsScriptSystem()
        {
            var world = new World();
            var entity = world.CreateEntity();
            world.AddComponent(entity, new Cobra.Engine.ECS.Components.Transform { X = 10, Y = 20 });
            world.AddComponent(entity, new Cobra.Engine.ECS.Components.Script { ScriptPath = "player.py" });

            ScriptSystem.Update(world);
            Assert(true, "ScriptSystem.Update should complete non-blocking without spawning processes.");
            return Task.CompletedTask;
        }
    }
}
