using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using MahApps.Metro.IconPacks;
using Cobra.Core.AI;
using Cobra.Core.Assets;
using Cobra.Core.Build;
using Cobra.Core.Environment;
using Cobra.Core.Execution;
using Cobra.Core.ProjectSystem;
using Cobra.Core.Security;
using Cobra.Core.Settings;
using Cobra.Core.SourceControl;
using Cobra.Editor;
using Cobra.Engine;
using Cobra.Engine.ECS;
using Cobra.Engine.ECS.Components;
using Cobra.Engine.ECS.Systems;
using Cobra.Engine.Physics;
using Cobra.Engine.Plugins;
using Cobra.Engine.Prefabs;
using Cobra.Engine.SceneSystem;
using Cobra.Engine.VisualScripting;
using Microsoft.Web.WebView2.Core;

using Point = System.Windows.Point;
using Color = System.Windows.Media.Color;
using Brushes = System.Windows.Media.Brushes;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using DragEventArgs = System.Windows.DragEventArgs;
using DataFormats = System.Windows.DataFormats;
using Cursors = System.Windows.Input.Cursors;
using Button = System.Windows.Controls.Button;
using TextBox = System.Windows.Controls.TextBox;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using VerticalAlignment = System.Windows.VerticalAlignment;
using Path = System.IO.Path;
using CheckBox = System.Windows.Controls.CheckBox;
using ColorConverter = System.Windows.Media.ColorConverter;
using Clipboard = System.Windows.Clipboard;
using DragDropEffects = System.Windows.DragDropEffects;
using Application = System.Windows.Application;

namespace Cobra.UI.Views
{
    public class OpenTabItem : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private bool _isDirty;

        public string FilePath { get; set; } = string.Empty;
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayName)); }
        }
        public string Content { get; set; } = string.Empty;
        public bool IsDirty
        {
            get => _isDirty;
            set { _isDirty = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayName)); }
        }
        public string Icon { get; set; } = "File";
        public string DisplayName => IsDirty ? $"{Name} *" : Name;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? prop = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }

    public partial class EditorView
    {
        private readonly string _projectPath;
        private ProjectModel _projectConfig = new();
        private string _currentFilePath = string.Empty;
        private string _currentScenePath = string.Empty;

        private bool _isWebViewReady;
        private bool _is3DWebViewReady;
        private bool _is3DMode;
        private bool _isVenvActive;
        private bool _isFullScreen;
        private bool _isPlaying;
        private bool _isDebuggingSession;
        private bool _isBottomPanelCollapsed;
        private double _savedBottomPanelHeight = 230;
        private Window? _parentWindow;

        private Scene _currentScene = new();
        private readonly Stack<string> _sceneUndoStack = new();
        private readonly Stack<string> _sceneRedoStack = new();

        private readonly NodeGraph _currentGraph = new();
        private Node? _selectedNode;
        private Node? _pendingConnectionSource;

        private Border? _currentSelectedNav;
        private string? _selectedRequirementsPath;
        private static readonly JsonSerializerOptions JsonIndentedOptions = new() { WriteIndented = true };

        private SceneObject? _selectedGameObject;
        private bool _isDragging;
        private Point _dragOffset;
        private double _zoom = 1.0;
        private double _camX;
        private double _camY;
        private bool _isPanning;
        private Point _lastPanPoint;

        private readonly GameLoop _gameLoop = new();
        private DispatcherTimer? _profilerTimer;
        private DispatcherTimer? _animPreviewTimer;
        private const int DefaultGridSize = 20;

        private enum GizmoMode
        {
            None,
            MoveX,
            MoveY,
            Rotate
        }

        private GizmoMode _currentGizmo = GizmoMode.None;
        private Point _lastMousePos;

        private readonly World _world = new();
        private readonly PluginLoader _pluginLoader = new();
        private readonly GameRunnerService _gameRunner = new();
        private readonly MediaPlayer _audioPreviewPlayer = new();

        private BitmapImage? _loadedSpriteSheet;
        private int _currentAnimFrame;
        private readonly List<double> _memoryHistory = [];
        private readonly List<PythonDiagnostic> _currentDiagnostics = [];
        private readonly Dictionary<string, HashSet<int>> _fileBreakpoints = new(StringComparer.OrdinalIgnoreCase);

        [GeneratedRegex(@"\x1B\[[^@-~]*[@-~]")]
        private static partial Regex AnsiRegex();

        private readonly ObservableCollection<OpenTabItem> _openTabs = [];

        public EditorView(string path)
        {
            InitializeComponent();
            _projectPath = Path.GetFullPath(path);
            FileTabs.ItemsSource = _openTabs;
            Loaded += EditorView_Loaded;
            Unloaded += EditorView_Unloaded;
            SceneCanvas.Loaded += (_, _) => RedrawScene();
            _currentSelectedNav = NavCodeEditor;

            SettingsManager.Instance.SettingsUpdated += (_, _) => ApplySettings();
            WireGameRunnerEvents();
        }

        private void WireGameRunnerEvents()
        {
            _gameRunner.OutputReceived += text => Dispatcher.Invoke(() =>
            {
                string clean = StripAnsi(text) + "\n";
                AppendTextToConsole(GameLogsBox, clean);
                AppendTextToConsole(OutputConsole, clean);
                if (clean.StartsWith("[Debug Eval]", StringComparison.Ordinal))
                {
                    AppendTextToConsole(DebugConsoleBox, clean);
                }
            });

            _gameRunner.ErrorReceived += text => Dispatcher.Invoke(() =>
            {
                string clean = StripAnsi(text) + "\n";
                AppendTextToConsole(GameLogsBox, $"[ERR] {clean}");
                AppendTextToConsole(OutputConsole, $"[ERR] {clean}");
            });

            _gameRunner.ProcessExited += (code, crashed) => Dispatcher.Invoke(() =>
            {
                _isDebuggingSession = false;
                StatusRightText.Text = crashed ? $"Game exited with code {code}" : "Ready";
                DebuggerStatusLabel.Text = $"Process exited (Code {code})";
            });

            _gameRunner.DebuggerPaused += state => Dispatcher.Invoke(() =>
            {
                DebuggerStatusLabel.Text = $"Paused at {Path.GetFileName(state.FilePath)}:{state.Line} ({state.FunctionName})";
                DebugCallStackList.ItemsSource = state.CallStack;
                List<string> locals = [.. state.LocalVariables.Select(kv => $"{kv.Key} = {kv.Value}")];
                DebugLocalsList.ItemsSource = locals;
                if (!string.IsNullOrEmpty(state.ExceptionMessage))
                {
                    AppendTextToConsole(DebugConsoleBox, $"[Exception] {state.ExceptionMessage}\n");
                }
                OnBottomTabClicked(TabDebugBtn, new RoutedEventArgs());

                if (File.Exists(state.FilePath))
                {
                    OpenFileInTab(state.FilePath);
                    _ = RevealLineInEditorAsync(state.Line);
                }
            });
        }

        private static void AppendTextToConsole(TextBox box, string text)
        {
            box.AppendText(text);
            box.ScrollToEnd();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _parentWindow = Window.GetWindow(this);
            if (_parentWindow != null)
            {
                _parentWindow.StateChanged += ParentWindow_StateChanged;
                _parentWindow.PreviewKeyDown += ParentWindow_PreviewKeyDown;
            }
        }

        private void EditorView_Unloaded(object sender, RoutedEventArgs e)
        {
            CleanupResources();
        }

        public void CleanupResources()
        {
            try
            {
                if (_parentWindow != null)
                {
                    _parentWindow.StateChanged -= ParentWindow_StateChanged;
                    _parentWindow.PreviewKeyDown -= ParentWindow_PreviewKeyDown;
                }
                _gameLoop.Stop();
                _profilerTimer?.Stop();
                _animPreviewTimer?.Stop();
                _audioPreviewPlayer.Stop();
                _audioPreviewPlayer.Close();
                _gameRunner.Dispose();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Cleanup error: {ex.Message}");
            }
        }

        private void ParentWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.S)
            {
                e.Handled = true;
                OnSaveClicked(this, new RoutedEventArgs());
            }
            else if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift) && e.Key == Key.S)
            {
                e.Handled = true;
                OnSaveAllClicked(this, new RoutedEventArgs());
            }
            else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.F)
            {
                e.Handled = true;
                OnFindClicked(this, new RoutedEventArgs());
            }
            else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.H)
            {
                e.Handled = true;
                OnReplaceClicked(this, new RoutedEventArgs());
            }
            else if (e.Key == Key.F5)
            {
                e.Handled = true;
                if (_isDebuggingSession && _gameRunner.IsRunning)
                    OnDebugContinueClicked(this, new RoutedEventArgs());
                else
                    OnRunClicked(this, new RoutedEventArgs());
            }
            else if (e.Key == Key.F6)
            {
                e.Handled = true;
                OnDebugClicked(this, new RoutedEventArgs());
            }
            else if (e.Key == Key.F7)
            {
                e.Handled = true;
                OnBuildClicked(this, new RoutedEventArgs());
            }
            else if (e.Key == Key.F10 && _isDebuggingSession)
            {
                e.Handled = true;
                OnDebugStepOverClicked(this, new RoutedEventArgs());
            }
            else if (e.Key == Key.F11 && Keyboard.Modifiers == ModifierKeys.None && _isDebuggingSession)
            {
                e.Handled = true;
                OnDebugStepIntoClicked(this, new RoutedEventArgs());
            }
        }

        private void ParentWindow_StateChanged(object? sender, EventArgs e)
        {
            if (_parentWindow != null)
            {
                _isFullScreen = _parentWindow.WindowState == WindowState.Maximized && _parentWindow.WindowStyle == WindowStyle.None;
            }
        }

        private async void EditorView_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _projectConfig = ProjectModel.LoadFromDirectory(_projectPath);
                BuildAppNameBox.Text = SecurityValidator.IsValidProjectName(_projectConfig.Name, out _) ? _projectConfig.Name : "CobraGame";

                await InitializeWebView();
                LoadFiles();
                PopulateStartupFiles();
                CheckVenvStatus();
                InitializeVisualScriptingLibrary();
                RefreshPythonEnvironmentWorkspace();
                RefreshAssetsWorkspace();
                _ = RefreshGitWorkspaceAsync();

                OutputConsole.Text = $"Cobra Python Game Development IDE Ready\nProject: {_projectConfig.Name} ({_projectPath})\nFramework: {_projectConfig.Framework}\n-----------------------------------------------------------------------------\n";
                TerminalConsole.Text = $"Cobra Integrated Shell\nWorking Directory: {_projectPath}\nType Python, pip, git, or shell commands below.\n-----------------------------------------------------------------------------\n";

                InitEngine();
                LoadDefaultOrInitialScene();
                InitTerminalShell();
                Populate3DToolPalette();
                ApplySettings();
                SettingsManager.Instance.SettingsUpdated += (_, _) => Dispatcher.Invoke(ApplySettings);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void InitEngine()
        {
            _pluginLoader.LoadPlugins(Path.Combine(_projectPath, "Plugins"));
            ExtensionsRegistryList.ItemsSource = _pluginLoader.DiscoveredPlugins;

            var rootEntity = _world.CreateEntity();
            _world.AddComponent(rootEntity, new Engine.ECS.Components.Transform { X = 0, Y = 0 });
            _world.AddComponent(rootEntity, new Sprite { Path = "default", Color = "#ff8c00" });
            _ = _world.GetComponent<Sprite>(rootEntity)?.Path;
            _ = _world.GetComponent<Sprite>(rootEntity)?.Color;

            var rootActor = new GameObject
            {
                Name = "RootActor",
                Rigidbody = new Rigidbody { Mass = 1f },
                Collider = new Collider { Bounds = new Rect(0, 0, 32, 32), IsTrigger = false }
            };
            var scriptComp = rootActor.AddComponent<ScriptComponent>();
            scriptComp.ScriptPath = "main.py";
            scriptComp.Graph = _currentGraph;
            foreach (var comp in rootActor.GetAllComponents())
            {
                comp.Start();
                comp.Update();
            }
            _ = rootActor.GetComponent<ScriptComponent>();
            _ = rootActor.Rigidbody.Mass;
            _ = rootActor.Collider.IsTrigger;
            _ = rootActor.Id;

            var physicsSystem = new PhysicsSystem(new PhysicsEngine());

            _gameLoop.OnUpdate = dt =>
            {
                if (_isPlaying)
                {
                    ScriptSystem.Update(_world);
                    TransformSystem.Update(_world);
                    physicsSystem.Update(dt);
                    _pluginLoader.UpdateAll();
                    if (!_is3DMode && _currentScene.Objects.Count == 0 && SceneCanvas != null)
                    {
                        RenderSystem.Render(_world, SceneCanvas);
                    }
                    UpdateSceneSimulation(dt);
                    RedrawScene();
                }
            };
            _gameLoop.Start();

            _profilerTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _profilerTimer.Tick += (_, _) => UpdateProfilerMetrics();
            _profilerTimer.Start();

            _animPreviewTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(120) };
            _animPreviewTimer.Tick += (_, _) => StepAnimationPreview();
        }

        private void LoadDefaultOrInitialScene()
        {
            string defaultSceneFile = Path.Combine(_projectPath, "scenes", "main.Cobra");
            if (File.Exists(defaultSceneFile))
            {
                try
                {
                    _currentScene = Scene.Load(defaultSceneFile);
                    _currentScenePath = defaultSceneFile;
                    ActiveSceneNameLabel.Text = _currentScene.SceneName;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    CreateStarterSceneObjects();
                }
            }
            else
            {
                CreateStarterSceneObjects();
            }

            bool is3DFramework = _projectConfig.Framework.Equals("Panda3D", StringComparison.OrdinalIgnoreCase)
                              || _projectConfig.Framework.Equals("Ursina", StringComparison.OrdinalIgnoreCase);

            StatusFrameworkText.Text = is3DFramework
                ? $"Framework: {_projectConfig.Framework} (3D)"
                : "Framework: Pygame (2D)";

            SaveSceneUndoSnapshot();
            RedrawScene();
        }

        private void CreateStarterSceneObjects()
        {
            _currentScene = new Scene { SceneName = "MainScene", Framework = _projectConfig.Framework };
            _currentScene.Objects.Add(new SceneObject
            {
                Name = "Player",
                Primitive3D = "Cube",
                X = 160,
                Y = 180,
                Width = 64,
                Height = 64,
                Color = "#ff8c00",
                ColliderType = "Rectangle",
                HasRigidbody = true
            });
            _currentScene.Objects.Add(new SceneObject
            {
                Name = "GroundPlatform",
                Primitive3D = "Cube",
                X = 100,
                Y = 340,
                Width = 360,
                Height = 32,
                Color = "#4ec9b0",
                ColliderType = "Rectangle",
                HasRigidbody = false
            });
        }

        private async Task InitializeWebView()
        {
            try
            {
                await CodeWebView.EnsureCoreWebView2Async();
                ConfigureSecureWebView(CodeWebView.CoreWebView2);

                string htmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "monaco.html");
                if (File.Exists(htmlPath))
                {
                    CodeWebView.Source = new Uri(htmlPath);
                }

                CodeWebView.CoreWebView2.NavigationCompleted += (_, _) =>
                {
                    _isWebViewReady = true;
                    ApplySettings();
                    string startupPath = Path.Combine(_projectPath, _projectConfig.StartupFile);
                    if (File.Exists(startupPath) && _openTabs.Count == 0)
                    {
                        OpenFileInTab(startupPath);
                    }
                };

                CodeWebView.CoreWebView2.WebMessageReceived += OnCodeWebViewMessageReceived;

                await Viewport3DWebView.EnsureCoreWebView2Async();
                ConfigureSecureWebView(Viewport3DWebView.CoreWebView2);
                string view3DPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "viewport3d.html");
                if (File.Exists(view3DPath))
                {
                    Viewport3DWebView.Source = new Uri(view3DPath);
                }
                Viewport3DWebView.CoreWebView2.NavigationCompleted += (_, _) =>
                {
                    _is3DWebViewReady = true;
                    SyncSceneTo3DViewport();
                };
                Viewport3DWebView.CoreWebView2.WebMessageReceived += OnViewport3DMessageReceived;
            }
            catch (Exception ex)
            {
                OutputConsole.AppendText($"[WebView2 Warning] {ex.Message}\n");
            }
        }

        private static void ConfigureSecureWebView(CoreWebView2 webView)
        {
            webView.Settings.IsStatusBarEnabled = false;
            webView.Settings.AreDefaultScriptDialogsEnabled = false;
            webView.Settings.IsWebMessageEnabled = true;

            webView.NavigationStarting += (_, args) =>
            {
                if (Uri.TryCreate(args.Uri, UriKind.Absolute, out var uri))
                {
                    if (uri.Scheme != Uri.UriSchemeFile && !uri.Host.Contains("jsdelivr.net", StringComparison.OrdinalIgnoreCase))
                    {
                        args.Cancel = true;
                    }
                }
            };
        }

        private void OnCodeWebViewMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                string json = e.TryGetWebMessageAsString();
                if (string.IsNullOrWhiteSpace(json)) return;

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                string type = root.TryGetProperty("type", out var t) ? t.GetString() ?? "" : "";

                if (type == "contentChanged" && FileTabs.SelectedItem is OpenTabItem activeTab)
                {
                    string content = root.TryGetProperty("content", out var c) ? c.GetString() ?? "" : "";
                    if (activeTab.Content != content)
                    {
                        activeTab.Content = content;
                        activeTab.IsDirty = true;
                        if (SplitEditorBorder.Visibility == Visibility.Visible)
                        {
                            SplitPreviewTextBox.Text = content;
                        }
                    }
                }
                else if (type == "saveRequested")
                {
                    OnSaveClicked(this, new RoutedEventArgs());
                }
                else if (type == "breakpoint" && !string.IsNullOrEmpty(_currentFilePath))
                {
                    int line = root.TryGetProperty("line", out var l) ? l.GetInt32() : 1;
                    bool enabled = root.TryGetProperty("enabled", out var en) && en.GetBoolean();
                    if (!_fileBreakpoints.TryGetValue(_currentFilePath, out var set))
                    {
                        set = [];
                        _fileBreakpoints[_currentFilePath] = set;
                    }
                    if (enabled) set.Add(line);
                    else set.Remove(line);

                    ShowToast(enabled ? $"Breakpoint added at line {line}" : $"Breakpoint removed at line {line}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Editor message error: {ex.Message}");
            }
        }

        private void OnViewport3DMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                string json = e.TryGetWebMessageAsString();
                if (string.IsNullOrWhiteSpace(json)) return;

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                string type = root.TryGetProperty("type", out var t) ? t.GetString() ?? "" : "";

                if (type == "objectSelected")
                {
                    string id = root.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? "" : "";
                    var obj = _currentScene.Objects.FirstOrDefault(o => o.Id == id);
                    if (obj != null)
                    {
                        _selectedGameObject = obj;
                        SceneHierarchy.SelectedItem = obj;
                        UpdateInspector();
                    }
                }
                else if (type == "objectTransformed")
                {
                    string id = root.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? "" : "";
                    var obj = _currentScene.Objects.FirstOrDefault(o => o.Id == id);
                    if (obj is { IsLocked: false })
                    {
                        SaveSceneUndoSnapshot();
                        if (root.TryGetProperty("x", out var x)) obj.X = x.GetDouble();
                        if (root.TryGetProperty("y", out var y)) obj.Y = y.GetDouble();
                        if (root.TryGetProperty("z", out var z)) obj.Z = z.GetDouble();
                        if (root.TryGetProperty("rotationX", out var rx)) obj.RotationX = rx.GetDouble();
                        if (root.TryGetProperty("rotationY", out var ry)) obj.RotationY = ry.GetDouble();
                        if (root.TryGetProperty("rotation", out var rz)) obj.Rotation = rz.GetDouble();
                        if (root.TryGetProperty("scaleX", out var sx)) obj.ScaleX = sx.GetDouble();
                        if (root.TryGetProperty("scaleY", out var sy)) obj.ScaleY = sy.GetDouble();
                        if (root.TryGetProperty("scaleZ", out var sz)) obj.ScaleZ = sz.GetDouble();
                        UpdateInspector();
                        AutoSyncSceneCode();
                    }
                }
                else if (type == "objectCreated")
                {
                    SaveSceneUndoSnapshot();
                    string name = root.TryGetProperty("name", out var n) ? n.GetString() ?? "3D_Entity" : "3D_Entity";
                    string category = root.TryGetProperty("category", out var c) ? c.GetString() ?? "BasicShapes" : "BasicShapes";
                    string primitive = root.TryGetProperty("primitive3D", out var p) ? p.GetString() ?? "Cube" : "Cube";
                    string subtype = root.TryGetProperty("subtype3D", out var sub) ? sub.GetString() ?? primitive : primitive;
                    string color = root.TryGetProperty("color", out var clr) ? clr.GetString() ?? "#ff8c00" : "#ff8c00";
                    double x = root.TryGetProperty("x", out var xProp) ? xProp.GetDouble() : 0;
                    double y = root.TryGetProperty("y", out var yProp) ? yProp.GetDouble() : 0;
                    double z = root.TryGetProperty("z", out var zProp) ? zProp.GetDouble() : 0;

                    var newObj = new SceneObject
                    {
                        Name = $"{name}_{_currentScene.Objects.Count + 1}",
                        Category3D = category,
                        Primitive3D = primitive,
                        Subtype3D = subtype,
                        Color = color,
                        X = x,
                        Y = y,
                        Z = z,
                        Width = 64,
                        Height = 64,
                        Depth = 64
                    };
                    _currentScene.Objects.Add(newObj);
                    _selectedGameObject = newObj;
                    UpdateHierarchy();
                    SceneHierarchy.SelectedItem = newObj;
                    UpdateInspector();
                    RedrawScene();
                    SyncSceneTo3DViewport();
                    AutoSyncSceneCode();
                    ShowToast($"Created {newObj.Name}");
                }
                else if (type == "objectDeleted")
                {
                    string id = root.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? "" : "";
                    var obj = _currentScene.Objects.FirstOrDefault(o => o.Id == id);
                    if (obj != null)
                    {
                        SaveSceneUndoSnapshot();
                        _currentScene.Objects.Remove(obj);
                        _selectedGameObject = null;
                        UpdateHierarchy();
                        UpdateInspector();
                        RedrawScene();
                        SyncSceneTo3DViewport();
                        AutoSyncSceneCode();
                        ShowToast($"Deleted {obj.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"3D viewport message error: {ex.Message}");
            }
        }

        private async void ApplySettings()
        {
            try
            {
                var settings = SettingsManager.Instance.CurrentSettings;
                AiConsentCheckBox.IsChecked = settings.AiConsentGranted;
                SnapToGridCheck.IsChecked = settings.EnableGridSnap;
                ShowCollidersCheck.IsChecked = settings.ShowCollisionGizmos;

                if (!_isWebViewReady || CodeWebView?.CoreWebView2 == null) return;

                string script = $@"
                    if (window.editor) {{
                        window.editor.updateOptions({{
                            fontSize: {settings.FontSize},
                            lineHeight: {Math.Round(settings.FontSize * settings.LineSpacing)},
                            fontFamily: '{settings.FontFamily}',
                            tabSize: {settings.TabSize},
                            minimap: {{ enabled: {(settings.ShowMinimap ? "true" : "false")} }},
                            wordWrap: '{(settings.WordWrap ? "on" : "off")}',
                            lineNumbers: '{(settings.ShowLineNumbers ? "on" : "off")}'
                        }});
                    }}";
                await CodeWebView.CoreWebView2.ExecuteScriptAsync(script);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void CheckVenvStatus()
        {
            _isVenvActive = VenvService.ValidateVenv(_projectPath);
            if (NavAddVenv != null)
            {
                NavAddVenv.Visibility = _isVenvActive ? Visibility.Collapsed : Visibility.Visible;
            }

            string interpreter = PythonService.ResolveProjectPython(_projectPath, _projectConfig.PythonInterpreter);
            StatusInterpreterText.Text = _isVenvActive
                ? "Python: Project Venv (Active)"
                : $"Python: {Path.GetFileName(interpreter)}";
        }

        private void PopulateStartupFiles()
        {
            if (!Directory.Exists(_projectPath)) return;

            List<FileItem> fileItems =
            [
                .. Directory.GetFiles(_projectPath, "*.py", SearchOption.TopDirectoryOnly)
                    .Select(f => new FileItem { Name = Path.GetFileName(f), FullPath = f })
                    .Where(item => !string.IsNullOrEmpty(item.Name) && File.Exists(item.FullPath))
                    .OrderBy(item => item.Name)
            ];

            List<string> pyFiles = [.. fileItems.Select(item => item.Name)];
            if (pyFiles.Count == 0) pyFiles.Add("main.py");

            StartupFileCombo.ItemsSource = pyFiles;
            StartupFileCombo.SelectedItem = pyFiles.Contains(_projectConfig.StartupFile)
                ? _projectConfig.StartupFile
                : pyFiles.FirstOrDefault();
        }

        private void StartupFileCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (StartupFileCombo.SelectedItem is string selected && !string.IsNullOrWhiteSpace(selected))
            {
                _projectConfig.StartupFile = selected;
                _projectConfig.SaveManifest();
            }
        }

        private void LayoutPresetCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded || LayoutPresetCombo.SelectedItem is not ComboBoxItem item) return;
            string preset = item.Content?.ToString() ?? "Default";

            switch (preset)
            {
                case "Coding Focus":
                    LeftSidebarCol.Width = new GridLength(175);
                    RightSidebarCol.Width = new GridLength(230);
                    BottomPanelRow.Height = new GridLength(140);
                    SwitchWorkspace("CodeEditor", NavCodeEditor);
                    break;
                case "Scene Design":
                    LeftSidebarCol.Width = new GridLength(175);
                    RightSidebarCol.Width = new GridLength(250);
                    BottomPanelRow.Height = new GridLength(120);
                    SwitchWorkspace("SceneEditor", NavSceneEditor);
                    break;
                case "Debugging":
                    LeftSidebarCol.Width = new GridLength(185);
                    RightSidebarCol.Width = new GridLength(260);
                    BottomPanelRow.Height = new GridLength(260);
                    SwitchWorkspace("Debugger", NavDebugger);
                    OnBottomTabClicked(TabDebugBtn, new RoutedEventArgs());
                    break;
                default:
                    LeftSidebarCol.Width = new GridLength(195);
                    RightSidebarCol.Width = new GridLength(285);
                    BottomPanelRow.Height = new GridLength(230);
                    SwitchWorkspace("CodeEditor", NavCodeEditor);
                    break;
            }
        }

        private void OnToggleBottomPanelClicked(object sender, RoutedEventArgs e)
        {
            if (!_isBottomPanelCollapsed)
            {
                _savedBottomPanelHeight = BottomPanelRow.ActualHeight > 60 ? BottomPanelRow.ActualHeight : 230;
                BottomPanelRow.MinHeight = 0;
                BottomPanelRow.Height = new GridLength(0);
                BottomPanelBorder.Visibility = Visibility.Collapsed;
                _isBottomPanelCollapsed = true;
            }
            else
            {
                BottomPanelBorder.Visibility = Visibility.Visible;
                BottomPanelRow.MinHeight = 80;
                BottomPanelRow.Height = new GridLength(_savedBottomPanelHeight);
                _isBottomPanelCollapsed = false;
            }
        }

        private void OnBottomTabClicked(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            string tag = btn.Tag?.ToString() ?? "Output";

            Button[] allBtns = [TabOutputBtn, TabTerminalBtn, TabDebugBtn, TabProblemsBtn, TabTestsBtn, TabBuildBtn, TabGameLogsBtn];
            foreach (var b in allBtns)
            {
                b.Foreground = new SolidColorBrush(Color.FromRgb(170, 170, 170));
                b.BorderBrush = Brushes.Transparent;
            }

            btn.Foreground = Brushes.White;
            btn.BorderBrush = new SolidColorBrush(Color.FromRgb(255, 140, 0));

            OutputConsole.Visibility = tag == "Output" ? Visibility.Visible : Visibility.Collapsed;
            TerminalContainer.Visibility = tag == "Terminal" ? Visibility.Visible : Visibility.Collapsed;
            DebugConsoleBox.Visibility = tag == "DebugConsole" ? Visibility.Visible : Visibility.Collapsed;
            ProblemsListBox.Visibility = tag == "Problems" ? Visibility.Visible : Visibility.Collapsed;
            TestResultsBox.Visibility = tag == "TestResults" ? Visibility.Visible : Visibility.Collapsed;
            BuildOutputBox.Visibility = tag == "BuildOutput" ? Visibility.Visible : Visibility.Collapsed;
            GameLogsBox.Visibility = tag == "GameLogs" ? Visibility.Visible : Visibility.Collapsed;

            if (TerminalShellPanel != null)
            {
                TerminalShellPanel.Visibility = tag == "Terminal" ? Visibility.Visible : Visibility.Collapsed;
            }

            if (tag == "Terminal")
            {
                TerminalInput.Focus();
            }

            if (_isBottomPanelCollapsed)
            {
                OnToggleBottomPanelClicked(sender, e);
            }
        }

        private void OnClearConsoleClicked(object sender, RoutedEventArgs e)
        {
            if (OutputConsole.Visibility == Visibility.Visible) OutputConsole.Clear();
            else if (TerminalContainer.Visibility == Visibility.Visible) TerminalConsole.Clear();
            else if (DebugConsoleBox.Visibility == Visibility.Visible) DebugConsoleBox.Clear();
            else if (TestResultsBox.Visibility == Visibility.Visible) TestResultsBox.Clear();
            else if (BuildOutputBox.Visibility == Visibility.Visible) BuildOutputBox.Clear();
            else if (GameLogsBox.Visibility == Visibility.Visible) GameLogsBox.Clear();
            else if (ProblemsListBox.Visibility == Visibility.Visible) ProblemsListBox.ItemsSource = null;
        }

        private async void OnRunTestsClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                OnBottomTabClicked(TabTestsBtn, e);
                TestResultsBox.Text = "Running Python unit tests (unittest discover)...\n";

                string pythonExe = PythonService.ResolveProjectPython(_projectPath, _projectConfig.PythonInterpreter);
                await Task.Run(() =>
                {
                    try
                    {
                        var psi = new ProcessStartInfo
                        {
                            FileName = pythonExe,
                            WorkingDirectory = _projectPath,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };
                        psi.ArgumentList.Add("-m");
                        psi.ArgumentList.Add("unittest");
                        psi.ArgumentList.Add("discover");
                        psi.ArgumentList.Add("-v");

                        using var proc = Process.Start(psi);
                        if (proc == null) return;
                        string stdout = proc.StandardOutput.ReadToEnd();
                        string stderr = proc.StandardError.ReadToEnd();
                        proc.WaitForExit(15000);

                        Dispatcher.Invoke(() =>
                        {
                            TestResultsBox.AppendText(stdout);
                            if (!string.IsNullOrWhiteSpace(stderr))
                                TestResultsBox.AppendText("\n" + stderr);
                            TestResultsBox.AppendText($"\nExit code: {proc.ExitCode}\n");
                        });
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(() => TestResultsBox.AppendText($"Test execution error: {ex.Message}\n"));
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void NavItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border { Tag: string tag } clickedNav)
            {
                SwitchWorkspace(tag, clickedNav);
            }
        }

        private void SwitchWorkspace(string tag, Border? navBorder)
        {
            if (_currentSelectedNav != null)
            {
                _currentSelectedNav.Background = Brushes.Transparent;
            }
            if (navBorder != null)
            {
                navBorder.Background = new SolidColorBrush(Color.FromRgb(45, 45, 48));
                _currentSelectedNav = navBorder;
            }

            CodeEditorContainer.Visibility = tag == "CodeEditor" ? Visibility.Visible : Visibility.Collapsed;
            SceneEditorContainer.Visibility = tag == "SceneEditor" ? Visibility.Visible : Visibility.Collapsed;
            VisualScriptingContainer.Visibility = tag == "VisualScripting" ? Visibility.Visible : Visibility.Collapsed;
            AssetBrowserContainer.Visibility = tag == "AssetBrowser" ? Visibility.Visible : Visibility.Collapsed;
            AnimationToolsContainer.Visibility = tag == "AnimationTools" ? Visibility.Visible : Visibility.Collapsed;
            PythonEnvContainer.Visibility = tag == "PythonEnv" ? Visibility.Visible : Visibility.Collapsed;
            DebuggerContainer.Visibility = tag == "Debugger" ? Visibility.Visible : Visibility.Collapsed;
            ProfilerContainer.Visibility = tag == "Profiler" ? Visibility.Visible : Visibility.Collapsed;
            SourceControlContainer.Visibility = tag == "SourceControl" ? Visibility.Visible : Visibility.Collapsed;
            BuildPackageContainer.Visibility = tag == "BuildPackage" ? Visibility.Visible : Visibility.Collapsed;
            ExtensionsWorkspaceContainer.Visibility = tag == "Extensions" ? Visibility.Visible : Visibility.Collapsed;
            AiAssistantContainer.Visibility = tag == "AiAssistant" ? Visibility.Visible : Visibility.Collapsed;

            if (tag == "SceneEditor") RedrawScene();
            else if (tag == "VisualScripting") RedrawNodeGraph();
            else if (tag == "AssetBrowser") RefreshAssetsWorkspace();
            else if (tag == "PythonEnv") RefreshPythonEnvironmentWorkspace();
            else if (tag == "SourceControl") _ = RefreshGitWorkspaceAsync();
        }

        private void LoadFiles()
        {
            FileTree.Items.Clear();
            if (!Directory.Exists(_projectPath)) return;

            string search = ExplorerSearchBox?.Text.Trim() ?? "";
            string extFilter = (ExplorerFilterCombo?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All";

            var rootNode = CreateDirectoryNode(new DirectoryInfo(_projectPath), search, extFilter);
            rootNode.IsExpanded = true;
            rootNode.IsSelected = false;
            FileTree.Items.Add(rootNode);
        }

        private static FileNode CreateDirectoryNode(DirectoryInfo dir, string search, string extFilter)
        {
            var node = new FileNode
            {
                Name = dir.Name,
                FullPath = dir.FullName,
                IsDirectory = true,
                IconKind = "Folder"
            };

            try
            {
                foreach (var subDir in dir.GetDirectories())
                {
                    if (subDir.Name is ".git" or "__pycache__" or ".vs" or "obj" or "bin")
                        continue;

                    var childDirNode = CreateDirectoryNode(subDir, search, extFilter);
                    if (childDirNode.Children.Count > 0 || (string.IsNullOrEmpty(search) && extFilter == "All"))
                    {
                        node.Children.Add(childDirNode);
                    }
                }

                foreach (var file in dir.GetFiles())
                {
                    if (!string.IsNullOrEmpty(search) && !file.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (extFilter != "All" && !file.Extension.Equals(extFilter, StringComparison.OrdinalIgnoreCase))
                        continue;

                    node.Children.Add(new FileNode
                    {
                        Name = file.Name,
                        FullPath = file.FullName,
                        IsDirectory = false,
                        IconKind = GetFileIcon(file.Name)
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Directory scan warning: {ex.Message}");
            }

            return node;
        }

        private static string GetFileIcon(string fileName)
        {
            string ext = Path.GetExtension(fileName).ToLowerInvariant();
            return ext switch
            {
                ".py" => "LanguagePython",
                ".cobra" => "GamepadVariant",
                ".json" => "CodeJson",
                ".md" or ".txt" => "FileDocumentOutline",
                ".png" or ".jpg" or ".jpeg" or ".bmp" or ".gif" => "FileImage",
                ".wav" or ".ogg" or ".mp3" => "FileMusic",
                ".obj" or ".gltf" or ".glb" => "CubeOutline",
                _ => "FileOutline"
            };
        }

        private void ExplorerSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IsLoaded) LoadFiles();
        }

        private void ExplorerFilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded) LoadFiles();
        }

        private void OnFileTreeSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (FileTree.SelectedItem is not FileNode selectedFileNode || selectedFileNode.IsDirectory)
                return;

            selectedFileNode.IsSelected = true;
            string path = selectedFileNode.FullPath;
            if (Path.GetFileName(path).Equals("requirements.txt", StringComparison.OrdinalIgnoreCase))
            {
                _selectedRequirementsPath = path;
                InstallReqButton.Visibility = Visibility.Visible;
            }
            else
            {
                InstallReqButton.Visibility = Visibility.Collapsed;
            }

            if (path.EndsWith(".Cobra", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    _currentScene = Scene.Load(path);
                    _currentScenePath = path;
                    ActiveSceneNameLabel.Text = _currentScene.SceneName;
                    SwitchWorkspace("SceneEditor", NavSceneEditor);
                    RedrawScene();
                    ShowToast($"Loaded scene: {_currentScene.SceneName}");
                }
                catch (Exception ex)
                {
                    OutputConsole.AppendText($"Failed to load scene: {ex.Message}\n");
                }
                return;
            }

            OpenFileInTab(path);
        }

        private void OpenFileInTab(string filePath)
        {
            if (!File.Exists(filePath)) return;

            var existing = _openTabs.FirstOrDefault(t => t.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                FileTabs.SelectedItem = existing;
                return;
            }

            string content = File.ReadAllText(filePath);
            var newTab = new OpenTabItem
            {
                FilePath = filePath,
                Name = Path.GetFileName(filePath),
                Content = content,
                Icon = GetFileIcon(filePath),
                IsDirty = false
            };

            _openTabs.Add(newTab);
            FileTabs.SelectedItem = newTab;
            SwitchWorkspace("CodeEditor", NavCodeEditor);
        }

        private async void OnTabSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (FileTabs.SelectedItem is not OpenTabItem tab) return;

                _currentFilePath = tab.FilePath;
                if (SplitEditorBorder.Visibility == Visibility.Visible)
                {
                    SplitPreviewTextBox.Text = tab.Content;
                }

                if (!_isWebViewReady || CodeWebView.CoreWebView2 == null) return;

                string lang = DetectMonacoLanguage(tab.FilePath);
                string escapedContent = JsonSerializer.Serialize(tab.Content);
                await CodeWebView.CoreWebView2.ExecuteScriptAsync($"if (window.setEditorContent) window.setEditorContent({escapedContent}, '{lang}');");

                if (tab.FilePath.EndsWith(".py", StringComparison.OrdinalIgnoreCase))
                {
                    await UpdatePythonIntelligenceForFileAsync(tab.FilePath);
                }
                else
                {
                    SymbolOutlineCombo.ItemsSource = null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private static string DetectMonacoLanguage(string path)
        {
            return Path.GetExtension(path).ToLowerInvariant() switch
            {
                ".py" => "python",
                ".json" or ".cobra" => "json",
                ".md" => "markdown",
                ".html" => "html",
                ".css" => "css",
                _ => "plaintext"
            };
        }

        private async Task UpdatePythonIntelligenceForFileAsync(string filePath)
        {
            string pythonExe = PythonService.ResolveProjectPython(_projectPath, _projectConfig.PythonInterpreter);
            var (diagnostics, symbols) = await PythonIntelligenceService.AnalyzePythonFileAsync(pythonExe, filePath);

            SymbolOutlineCombo.ItemsSource = symbols;
            SymbolOutlineCombo.DisplayMemberPath = "Signature";

            if (!SettingsManager.Instance.CurrentSettings.Linting) return;

            _currentDiagnostics.Clear();
            _currentDiagnostics.AddRange(diagnostics);
            List<string> diagTexts = [.. _currentDiagnostics.Select(d => d.DisplayText)];
            ProblemsListBox.ItemsSource = diagTexts;

            if (_isWebViewReady && CodeWebView.CoreWebView2 != null)
            {
                string diagJson = JsonSerializer.Serialize(diagnostics.Select(d => new
                {
                    line = d.Line,
                    column = d.Column,
                    message = d.Message,
                    severity = d.Severity
                }));
                await CodeWebView.CoreWebView2.ExecuteScriptAsync($"if (window.setDiagnostics) window.setDiagnostics({diagJson});");
            }
        }

        private async void SymbolOutlineCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (SymbolOutlineCombo.SelectedItem is PythonSymbolInfo symbol)
                {
                    await RevealLineInEditorAsync(symbol.Line);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private async void ProblemsListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                int idx = ProblemsListBox.SelectedIndex;
                if (idx >= 0 && idx < _currentDiagnostics.Count)
                {
                    await RevealLineInEditorAsync(_currentDiagnostics[idx].Line);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private async Task RevealLineInEditorAsync(int line)
        {
            if (_isWebViewReady && CodeWebView.CoreWebView2 != null)
            {
                await CodeWebView.CoreWebView2.ExecuteScriptAsync($"if (window.revealLine) window.revealLine({line});");
            }
        }

        private async void OnCloseTabClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is not Button btn || btn.Tag is not OpenTabItem tab) return;

                if (tab.IsDirty)
                {
                    bool save = await CustomDialog.ShowAsync(Window.GetWindow(this),
                        $"Save changes to '{tab.Name}' before closing?",
                        "Unsaved Changes",
                        DialogIcon.Warning,
                        "Save",
                        "Discard");
                    if (save)
                    {
                        await SaveTabAsync(tab);
                    }
                }

                _openTabs.Remove(tab);
                if (_openTabs.Count == 0 && _isWebViewReady && CodeWebView.CoreWebView2 != null)
                {
                    _currentFilePath = string.Empty;
                    await CodeWebView.CoreWebView2.ExecuteScriptAsync("if (window.setEditorContent) window.setEditorContent('', 'python');");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private async void OnSaveClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (FileTabs.SelectedItem is OpenTabItem activeTab)
                {
                    await SaveTabAsync(activeTab);
                    ShowToast($"Saved {activeTab.Name}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private async void OnSaveAllClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (var tab in _openTabs.Where(t => t.IsDirty).ToList())
                {
                    await SaveTabAsync(tab);
                }
                PopulateStartupFiles();
                ShowToast("Saved all files");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private async Task SaveTabAsync(OpenTabItem tab)
        {
            if (string.IsNullOrEmpty(tab.FilePath)) return;

            if (_isWebViewReady && CodeWebView.CoreWebView2 != null && FileTabs.SelectedItem == tab)
            {
                string raw = await CodeWebView.CoreWebView2.ExecuteScriptAsync("window.getEditorContent ? window.getEditorContent() : ''");
                string? unescaped = JsonSerializer.Deserialize<string>(raw);
                if (unescaped != null) tab.Content = unescaped;
            }

            if (SettingsManager.Instance.CurrentSettings.FormatOnSave && tab.FilePath.EndsWith(".py", StringComparison.OrdinalIgnoreCase))
            {
                string pythonExe = PythonService.ResolveProjectPython(_projectPath, _projectConfig.PythonInterpreter);
                var (_, formatted) = await PythonIntelligenceService.FormatPythonCodeAsync(pythonExe, tab.FilePath, tab.Content);
                tab.Content = formatted;
            }

            await File.WriteAllTextAsync(tab.FilePath, tab.Content);
            tab.IsDirty = false;

            if (tab.FilePath.EndsWith(".py", StringComparison.OrdinalIgnoreCase))
            {
                await UpdatePythonIntelligenceForFileAsync(tab.FilePath);

                // bidirectional sync: if python file defines scene objects, update visual scene workspace
                if (tab.Content.Contains("SCENE_OBJECTS"))
                {
                    ParseSceneCodeIntoCurrentScene(tab.Content);
                    RedrawScene();
                    SyncSceneTo3DViewport();
                    UpdateHierarchy();
                    UpdateInspector();
                }
            }
        }

        private async void OnUndoClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (SceneEditorContainer.Visibility == Visibility.Visible)
                {
                    OnSceneUndoClicked(sender, e);
                    return;
                }
                if (_isWebViewReady && CodeWebView.CoreWebView2 != null)
                {
                    await CodeWebView.CoreWebView2.ExecuteScriptAsync("if (window.triggerEditorAction) window.triggerEditorAction('undo');");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private async void OnRedoClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (SceneEditorContainer.Visibility == Visibility.Visible)
                {
                    OnSceneRedoClicked(sender, e);
                    return;
                }
                if (_isWebViewReady && CodeWebView.CoreWebView2 != null)
                {
                    await CodeWebView.CoreWebView2.ExecuteScriptAsync("if (window.triggerEditorAction) window.triggerEditorAction('redo');");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private async void OnFormatCodeClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (FileTabs.SelectedItem is not OpenTabItem activeTab) return;

                string pythonExe = PythonService.ResolveProjectPython(_projectPath, _projectConfig.PythonInterpreter);
                var (_, formatted) = await PythonIntelligenceService.FormatPythonCodeAsync(pythonExe, activeTab.FilePath, activeTab.Content);
                activeTab.Content = formatted;
                activeTab.IsDirty = true;

                if (_isWebViewReady && CodeWebView.CoreWebView2 != null)
                {
                    string escaped = JsonSerializer.Serialize(formatted);
                    await CodeWebView.CoreWebView2.ExecuteScriptAsync($"if (window.setEditorContent) window.setEditorContent({escaped}, 'python');");
                }
                ShowToast("Code formatted");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private async void OnFindClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_isWebViewReady && CodeWebView.CoreWebView2 != null)
                {
                    await CodeWebView.CoreWebView2.ExecuteScriptAsync("if (window.triggerEditorAction) window.triggerEditorAction('actions.find');");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private async void OnReplaceClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_isWebViewReady && CodeWebView.CoreWebView2 != null)
                {
                    await CodeWebView.CoreWebView2.ExecuteScriptAsync("if (window.triggerEditorAction) window.triggerEditorAction('editor.action.startFindReplaceAction');");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void OnSplitEditorToggleClicked(object sender, RoutedEventArgs e)
        {
            if (SplitEditorBorder.Visibility == Visibility.Collapsed)
            {
                SplitEditorCol.Width = new GridLength(1, GridUnitType.Star);
                SplitEditorBorder.Visibility = Visibility.Visible;
                if (FileTabs.SelectedItem is OpenTabItem activeTab)
                {
                    SplitPreviewTextBox.Text = activeTab.Content;
                }
            }
            else
            {
                SplitEditorCol.Width = new GridLength(0);
                SplitEditorBorder.Visibility = Visibility.Collapsed;
            }
        }

        private async void OnRunClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                await SaveAllDirtyTabsQuietlyAsync();
                OnBottomTabClicked(TabGameLogsBtn, e);
                GameLogsBox.Clear();
                _isDebuggingSession = false;

                var config = BuildRunConfiguration(debugMode: false);
                bool started = await _gameRunner.StartGameAsync(config);
                if (started)
                {
                    StatusRightText.Text = $"Running {config.StartupFile}";
                    ShowToast($"Running {config.StartupFile}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private async void OnDebugClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                await SaveAllDirtyTabsQuietlyAsync();
                SwitchWorkspace("Debugger", NavDebugger);
                OnBottomTabClicked(TabDebugBtn, e);
                DebugConsoleBox.Clear();
                _isDebuggingSession = true;

                var config = BuildRunConfiguration(debugMode: true);
                bool started = await _gameRunner.StartGameAsync(config);
                if (started)
                {
                    StatusRightText.Text = $"Debugging {config.StartupFile}";
                    ShowToast("Debugger started");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void OnStopClicked(object sender, RoutedEventArgs e)
        {
            _isDebuggingSession = false;
            _gameRunner.StopGame();
            StatusRightText.Text = "Ready";
            ShowToast("Game stopped");
        }

        private async void OnRestartGameClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                await SaveAllDirtyTabsQuietlyAsync();
                _isDebuggingSession = false;
                var config = BuildRunConfiguration(debugMode: false);
                await _gameRunner.StartGameAsync(config);
                ShowToast("Game restarted");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private GameRunConfiguration BuildRunConfiguration(bool debugMode)
        {
            string startup = StartupFileCombo.SelectedItem as string ?? _projectConfig.StartupFile;
            string pythonExe = PythonService.ResolveProjectPython(_projectPath, _projectConfig.PythonInterpreter);
            string fullStartup = Path.Combine(_projectPath, startup);

            List<int> bps = _fileBreakpoints.TryGetValue(fullStartup, out var set) ? [.. set] : [];
            return new GameRunConfiguration
            {
                ProjectPath = _projectPath,
                StartupFile = startup,
                PythonExecutable = pythonExe,
                CommandLineArguments = _projectConfig.CommandLineArgs,
                EnvironmentVariables = new Dictionary<string, string>(_projectConfig.EnvironmentVariables),
                EnableDebugMode = debugMode,
                BreakpointLines = bps
            };
        }

        private async Task SaveAllDirtyTabsQuietlyAsync()
        {
            foreach (var tab in _openTabs.Where(t => t.IsDirty).ToList())
            {
                await SaveTabAsync(tab);
            }
        }

        private void OnDebugContinueClicked(object sender, RoutedEventArgs e) => _gameRunner.SendDebugCommand("continue");
        private void OnDebugStepOverClicked(object sender, RoutedEventArgs e) => _gameRunner.SendDebugCommand("step_over");
        private void OnDebugStepIntoClicked(object sender, RoutedEventArgs e) => _gameRunner.SendDebugCommand("step_into");
        private void OnDebugStepOutClicked(object sender, RoutedEventArgs e) => _gameRunner.SendDebugCommand("step_out");

        private void OnEvaluateWatchClicked(object sender, RoutedEventArgs e)
        {
            string expr = DebugWatchInput.Text.Trim();
            if (string.IsNullOrEmpty(expr)) return;
            _gameRunner.SendDebugCommand($"eval:{expr}");
            DebugWatchInput.Clear();
        }

        private async void OnProjectConfigModalClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                var stack = new StackPanel { Width = 400 };
                stack.Children.Add(new TextBlock { Text = "Startup Python File:", Foreground = Brushes.White, Margin = new Thickness(0, 0, 0, 4) });
                var startupBox = new TextBox { Text = _projectConfig.StartupFile, Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)), Foreground = Brushes.White, Padding = new Thickness(6), Margin = new Thickness(0, 0, 0, 10) };
                stack.Children.Add(startupBox);

                stack.Children.Add(new TextBlock { Text = "Command-Line Arguments:", Foreground = Brushes.White, Margin = new Thickness(0, 0, 0, 4) });
                var argsBox = new TextBox { Text = _projectConfig.CommandLineArgs, Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)), Foreground = Brushes.White, Padding = new Thickness(6), Margin = new Thickness(0, 0, 0, 10) };
                stack.Children.Add(argsBox);

                stack.Children.Add(new TextBlock { Text = "Environment Variables (KEY=VALUE per line):", Foreground = Brushes.White, Margin = new Thickness(0, 0, 0, 4) });
                var envBox = new TextBox
                {
                    Text = string.Join("\n", _projectConfig.EnvironmentVariables.Select(kv => $"{kv.Key}={kv.Value}")),
                    AcceptsReturn = true,
                    Height = 80,
                    Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                    Foreground = Brushes.White,
                    Padding = new Thickness(6)
                };
                stack.Children.Add(envBox);

                var (confirmed, _) = await AnimatedModal.ShowCustomModalAsync(Window.GetWindow(this), "Run & Environment Configuration", stack, "Save Configuration");
                if (confirmed)
                {
                    _projectConfig.StartupFile = string.IsNullOrWhiteSpace(startupBox.Text) ? "main.py" : startupBox.Text.Trim();
                    _projectConfig.CommandLineArgs = argsBox.Text.Trim();
                    _projectConfig.EnvironmentVariables.Clear();
                    foreach (var line in envBox.Text.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                    {
                        int eq = line.IndexOf('=');
                        if (eq > 0)
                        {
                            _projectConfig.EnvironmentVariables[line[..eq].Trim()] = line[(eq + 1)..].Trim();
                        }
                    }
                    _projectConfig.SaveManifest();
                    PopulateStartupFiles();
                    ShowToast("Run settings saved");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void SaveSceneUndoSnapshot()
        {
            string json = JsonSerializer.Serialize(_currentScene);
            _sceneUndoStack.Push(json);
            _sceneRedoStack.Clear();
        }

        private void OnSceneUndoClicked(object sender, RoutedEventArgs e)
        {
            if (_sceneUndoStack.Count <= 1) return;
            string currentJson = _sceneUndoStack.Pop();
            _sceneRedoStack.Push(currentJson);

            string previousJson = _sceneUndoStack.Peek();
            var restored = JsonSerializer.Deserialize<Scene>(previousJson);
            if (restored != null)
            {
                _currentScene = restored;
                _selectedGameObject = null;
                RedrawScene();
                SyncSceneTo3DViewport();
                UpdateInspector();
            }
        }

        private void OnSceneRedoClicked(object sender, RoutedEventArgs e)
        {
            if (_sceneRedoStack.Count == 0) return;
            string nextJson = _sceneRedoStack.Pop();
            _sceneUndoStack.Push(nextJson);

            var restored = JsonSerializer.Deserialize<Scene>(nextJson);
            if (restored != null)
            {
                _currentScene = restored;
                _selectedGameObject = null;
                RedrawScene();
                SyncSceneTo3DViewport();
                UpdateInspector();
            }
        }

        private void OnNewSceneClicked(object sender, RoutedEventArgs e)
        {
            SaveSceneUndoSnapshot();
            _currentScene = new Scene { SceneName = $"Scene_{DateTime.Now:HHmm}", Framework = _projectConfig.Framework };
            _currentScenePath = string.Empty;
            _selectedGameObject = null;
            ActiveSceneNameLabel.Text = _currentScene.SceneName;
            RedrawScene();
            SyncSceneTo3DViewport();
            UpdateInspector();
            ShowToast("Created new scene");
        }

        private void OnAddGameObject(object sender, RoutedEventArgs e)
        {
            SaveSceneUndoSnapshot();
            var obj = new SceneObject
            {
                Name = $"Object_{_currentScene.Objects.Count + 1}",
                Primitive3D = "Cube",
                X = 120 + (_currentScene.Objects.Count * 20),
                Y = 120 + (_currentScene.Objects.Count * 20),
                Width = 64,
                Height = 64,
                Color = "#ff8c00"
            };
            _currentScene.Objects.Add(obj);
            _selectedGameObject = obj;
            RedrawScene();
            SyncSceneTo3DViewport();
            UpdateInspector();
        }

        private void OnDuplicateSceneObject(object sender, RoutedEventArgs e)
        {
            if (_selectedGameObject == null) return;
            SaveSceneUndoSnapshot();
            var clone = _selectedGameObject.Clone();
            _currentScene.Objects.Add(clone);
            _selectedGameObject = clone;
            RedrawScene();
            SyncSceneTo3DViewport();
            UpdateInspector();
            ShowToast($"Duplicated {clone.Name}");
        }

        private void OnDeleteSceneObject(object sender, RoutedEventArgs e)
        {
            if (_selectedGameObject == null) return;
            SaveSceneUndoSnapshot();
            _currentScene.Objects.Remove(_selectedGameObject);
            _selectedGameObject = null;
            RedrawScene();
            SyncSceneTo3DViewport();
            UpdateInspector();
        }

        private void OnSaveScene(object sender, RoutedEventArgs e)
        {
            try
            {
                string scenesDir = Path.Combine(_projectPath, "scenes");
                Directory.CreateDirectory(scenesDir);

                if (string.IsNullOrEmpty(_currentScenePath))
                {
                    _currentScenePath = Path.Combine(scenesDir, $"{_currentScene.SceneName}.Cobra");
                }

                _currentScene.Save(_currentScenePath);

                string pyScenePath = Path.Combine(scenesDir, $"{_currentScene.SceneName.ToLowerInvariant()}_scene.py");
                File.WriteAllText(pyScenePath, _currentScene.ExportToPythonModule());

                LoadFiles();
                ShowToast($"Saved {Path.GetFileName(_currentScenePath)} and synced Python");
            }
            catch (Exception ex)
            {
                OutputConsole.AppendText($"Failed to save scene: {ex.Message}\n");
            }
        }

        private void OnLoadScene(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                InitialDirectory = Path.Combine(_projectPath, "scenes"),
                Filter = "Cobra Scene (*.Cobra)|*.Cobra|All Files (*.*)|*.*",
                Title = "Open Cobra Scene"
            };

            if (dlg.ShowDialog() == true)
            {
                _currentScene = Scene.Load(dlg.FileName);
                _currentScenePath = dlg.FileName;
                ActiveSceneNameLabel.Text = _currentScene.SceneName;
                SaveSceneUndoSnapshot();
                RedrawScene();
                SyncSceneTo3DViewport();
                UpdateInspector();
                ShowToast($"Opened {_currentScene.SceneName}");
            }
        }

        private void OnSavePrefab(object sender, RoutedEventArgs e)
        {
            if (_selectedGameObject == null) return;
            var prefabManager = new PrefabManager(_projectPath);
            var prefabMeta = new Prefab
            {
                Name = _selectedGameObject.Name,
                JsonData = JsonSerializer.Serialize(_selectedGameObject, JsonIndentedOptions)
            };
            prefabManager.SavePrefab(_selectedGameObject.Name, _selectedGameObject);
            LoadFiles();
            ShowToast($"Saved prefab: {prefabMeta.Name} ({prefabMeta.JsonData.Length} bytes)");
        }

        private void OnLoadPrefab(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                InitialDirectory = _projectPath,
                Filter = "Prefab Files (*.json)|*.json",
                Title = "Instantiate Prefab"
            };

            if (dlg.ShowDialog() == true)
            {
                var prefabManager = new PrefabManager(_projectPath);
                string prefabName = Path.GetFileNameWithoutExtension(dlg.FileName);
                var obj = prefabManager.LoadPrefab<SceneObject>(prefabName)
                       ?? JsonSerializer.Deserialize<SceneObject>(File.ReadAllText(dlg.FileName));
                if (obj != null)
                {
                    obj.Id = Guid.NewGuid().ToString("N")[..8];
                    SaveSceneUndoSnapshot();
                    _currentScene.Objects.Add(obj);
                    _selectedGameObject = obj;
                    RedrawScene();
                    SyncSceneTo3DViewport();
                    UpdateInspector();
                }
            }
        }

        private record Tool3DItem(
            string Name,
            string Category,
            string Primitive,
            string Subtype,
            string Color,
            double Width,
            double Height,
            double Depth,
            PackIconMaterialKind Icon);

        private static readonly List<Tool3DItem> Predefined3DTools =
        [
            // basic shapes
            new("Cube", "Basic Shapes", "Cube", "Cube", "#ff8c00", 64, 64, 64, PackIconMaterialKind.CubeOutline),
            new("Sphere", "Basic Shapes", "Sphere", "Sphere", "#4ec9b0", 64, 64, 64, PackIconMaterialKind.CircleOutline),
            new("Cylinder", "Basic Shapes", "Cylinder", "Cylinder", "#569cd6", 64, 64, 64, PackIconMaterialKind.ShapePlus),
            new("Cone", "Basic Shapes", "Cone", "Cone", "#ce9178", 64, 64, 64, PackIconMaterialKind.TriangleOutline),
            new("Plane", "Basic Shapes", "Plane", "Plane", "#9cdcfe", 128, 8, 128, PackIconMaterialKind.VectorSquare),
            new("Torus", "Basic Shapes", "Torus", "Torus", "#dcdcaa", 64, 64, 64, PackIconMaterialKind.CircleOutline),
            new("Capsule", "Basic Shapes", "Capsule", "Capsule", "#c586c0", 48, 80, 48, PackIconMaterialKind.Pill),

            // buildings
            new("House", "Buildings", "House", "House", "#d16969", 120, 90, 100, PackIconMaterialKind.Home),
            new("Office Building", "Buildings", "Office", "Office", "#4fc1ff", 140, 200, 120, PackIconMaterialKind.City),
            new("Wall", "Buildings", "Wall", "Wall", "#808080", 120, 60, 16, PackIconMaterialKind.Wall),
            new("Door", "Buildings", "Door", "Door", "#a0522d", 40, 80, 10, PackIconMaterialKind.Door),
            new("Window", "Buildings", "Window", "Window", "#add8e6", 50, 50, 8, PackIconMaterialKind.WindowClosed),
            new("Roof", "Buildings", "Roof", "Roof", "#8b0000", 120, 40, 100, PackIconMaterialKind.TriangleOutline),

            // vehicles
            new("Car", "Vehicles", "Car", "Car", "#e74c3c", 130, 50, 70, PackIconMaterialKind.Car),
            new("Truck", "Vehicles", "Truck", "Truck", "#3498db", 180, 80, 80, PackIconMaterialKind.Truck),
            new("Bus", "Vehicles", "Bus", "Bus", "#f39c12", 240, 90, 80, PackIconMaterialKind.Bus),
            new("Drone", "Vehicles", "Drone", "Drone", "#2c3e50", 60, 20, 60, PackIconMaterialKind.Airplane),

            // environments
            new("Road", "Environments", "Road", "Road", "#34495e", 200, 4, 100, PackIconMaterialKind.Road),
            new("Landscape", "Environments", "Landscape", "Landscape", "#27ae60", 300, 12, 300, PackIconMaterialKind.Terrain),
            new("Mountain", "Environments", "Mountain", "Mountain", "#7f8c8d", 220, 180, 220, PackIconMaterialKind.ImageFilterHdr),
            new("Tree", "Environments", "Tree", "Tree", "#2ecc71", 50, 120, 50, PackIconMaterialKind.Tree),
            new("Grass Patch", "Environments", "Grass", "Grass", "#27ae60", 40, 20, 40, PackIconMaterialKind.Grass),
            new("Water Surface", "Environments", "Water", "Water", "#2980b9", 260, 4, 260, PackIconMaterialKind.Water),

            // objects and furniture
            new("Table", "Objects & Furniture", "Table", "Table", "#8e44ad", 80, 50, 60, PackIconMaterialKind.TableFurniture),
            new("Chair", "Objects & Furniture", "Chair", "Chair", "#d35400", 40, 70, 40, PackIconMaterialKind.Seat),
            new("Desk", "Objects & Furniture", "Desk", "Desk", "#795548", 100, 60, 60, PackIconMaterialKind.Desk),
            new("Street Lamp", "Objects & Furniture", "Lamp", "Lamp", "#f1c40f", 24, 140, 24, PackIconMaterialKind.Lamp),
            new("Fence", "Objects & Furniture", "Fence", "Fence", "#bdc3c7", 120, 40, 12, PackIconMaterialKind.Fence),
            new("Crate", "Objects & Furniture", "Crate", "Crate", "#d2b48c", 50, 50, 50, PackIconMaterialKind.PackageVariantClosed),
            new("Barrel", "Objects & Furniture", "Barrel", "Barrel", "#607d8b", 44, 64, 44, PackIconMaterialKind.Barrel)
        ];

        private void OnToolCategorySelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ToolCategorySelector?.SelectedItem is ComboBoxItem item)
            {
                string cat = item.Content?.ToString() ?? "All Categories";
                Populate3DToolPalette(cat);
            }
        }

        private void Populate3DToolPalette(string category = "All Categories")
        {
            if (ToolItemsPanel == null) return;
            ToolItemsPanel.Children.Clear();

            var filtered = category == "All Categories"
                ? Predefined3DTools
                : [.. Predefined3DTools.Where(t => t.Category.Equals(category, StringComparison.OrdinalIgnoreCase))];

            foreach (var tool in filtered)
            {
                var card = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(35, 35, 38)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(55, 55, 60)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(3),
                    Padding = new Thickness(8, 6, 8, 6),
                    Margin = new Thickness(0, 0, 0, 4),
                    Cursor = Cursors.Hand,
                    ToolTip = $"Click to add or drag onto 3D Viewport: {tool.Name} ({tool.Category})"
                };

                card.MouseEnter += (_, _) =>
                {
                    card.Background = new SolidColorBrush(Color.FromRgb(45, 45, 50));
                    card.BorderBrush = new SolidColorBrush(Color.FromRgb(255, 140, 0));
                };
                card.MouseLeave += (_, _) =>
                {
                    card.Background = new SolidColorBrush(Color.FromRgb(35, 35, 38));
                    card.BorderBrush = new SolidColorBrush(Color.FromRgb(55, 55, 60));
                };

                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(24) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var icon = new PackIconMaterial
                {
                    Kind = tool.Icon,
                    Width = 14,
                    Height = 14,
                    Foreground = (System.Windows.Media.Brush)new BrushConverter().ConvertFromString(tool.Color)!,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(icon, 0);
                grid.Children.Add(icon);

                var textStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(6, 0, 0, 0) };
                textStack.Children.Add(new TextBlock { Text = tool.Name, Foreground = Brushes.White, FontSize = 11, FontWeight = FontWeights.SemiBold });
                textStack.Children.Add(new TextBlock { Text = tool.Category, Foreground = Brushes.Gray, FontSize = 9 });
                Grid.SetColumn(textStack, 1);
                grid.Children.Add(textStack);

                var addBtn = new Button
                {
                    Content = "+",
                    Width = 20,
                    Height = 20,
                    FontSize = 11,
                    FontWeight = FontWeights.Bold,
                    Background = new SolidColorBrush(Color.FromRgb(50, 50, 55)),
                    Foreground = Brushes.White,
                    BorderThickness = new Thickness(0),
                    Cursor = Cursors.Hand,
                    ToolTip = $"Add {tool.Name} to Scene"
                };
                addBtn.Click += (_, _) => Add3DObjectFromTool(tool);
                Grid.SetColumn(addBtn, 2);
                grid.Children.Add(addBtn);

                card.Child = grid;

                // drag and drop handler
                Point startPoint = new();
                card.PreviewMouseLeftButtonDown += (_, ev) => startPoint = ev.GetPosition(null);
                card.PreviewMouseMove += (_, ev) =>
                {
                    if (ev.LeftButton == MouseButtonState.Pressed)
                    {
                        Point current = ev.GetPosition(null);
                        Vector diff = startPoint - current;
                        if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                            Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                        {
                            var dragData = new
                            {
                                name = tool.Name,
                                category = tool.Category,
                                primitive = tool.Primitive,
                                subtype = tool.Subtype,
                                color = tool.Color
                            };
                            string json = JsonSerializer.Serialize(dragData);
                            DragDrop.DoDragDrop(card, new System.Windows.DataObject(DataFormats.Text, json), DragDropEffects.Copy);
                        }
                    }
                };

                card.MouseLeftButtonDown += (_, ev) =>
                {
                    if (ev.ClickCount == 2)
                    {
                        Add3DObjectFromTool(tool);
                    }
                };

                ToolItemsPanel.Children.Add(card);
            }
        }

        private void Add3DObjectFromTool(Tool3DItem tool)
        {
            SaveSceneUndoSnapshot();
            var obj = new SceneObject
            {
                Name = $"{tool.Name}_{_currentScene.Objects.Count + 1}",
                Category3D = tool.Category,
                Primitive3D = tool.Primitive,
                Subtype3D = tool.Subtype,
                Color = tool.Color,
                Width = tool.Width,
                Height = tool.Height,
                Depth = tool.Depth,
                X = 140 + (_currentScene.Objects.Count * 25),
                Y = 140 + (_currentScene.Objects.Count * 25),
                Z = 0
            };
            _currentScene.Objects.Add(obj);
            _selectedGameObject = obj;
            UpdateHierarchy();
            SceneHierarchy.SelectedItem = obj;
            RedrawScene();
            SyncSceneTo3DViewport();
            UpdateInspector();
            AutoSyncSceneCode();
            ShowToast($"Added {obj.Name} to scene");
        }

        private void AutoSyncSceneCode()
        {
            try
            {
                string scenesDir = Path.Combine(_projectPath, "scenes");
                if (!Directory.Exists(scenesDir)) Directory.CreateDirectory(scenesDir);
                string sceneFileName = $"{_currentScene.SceneName.ToLowerInvariant()}_scene.py";
                string pyScenePath = Path.Combine(scenesDir, sceneFileName);
                string code = _currentScene.ExportToPythonModule();
                File.WriteAllText(pyScenePath, code);

                string mainScenePy = Path.Combine(scenesDir, "main_scene.py");
                File.WriteAllText(mainScenePy, code);

                var tab = _openTabs.FirstOrDefault(t => t.FilePath.Equals(pyScenePath, StringComparison.OrdinalIgnoreCase) || t.FilePath.Equals(mainScenePy, StringComparison.OrdinalIgnoreCase));
                if (tab != null)
                {
                    tab.Content = code;
                    tab.IsDirty = false;
                    if (FileTabs.SelectedItem == tab && _isWebViewReady && CodeWebView.CoreWebView2 != null)
                    {
                        string escaped = JsonSerializer.Serialize(code);
                        CodeWebView.CoreWebView2.ExecuteScriptAsync($"if (window.setEditorContent) window.setEditorContent({escaped}, 'python');");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AutoSync error: {ex.Message}");
            }
        }

        private async void OnSyncCodeToSceneClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                string scenesDir = Path.Combine(_projectPath, "scenes");
                string pyScenePath = Path.Combine(scenesDir, $"{_currentScene.SceneName.ToLowerInvariant()}_scene.py");
                if (!File.Exists(pyScenePath)) pyScenePath = Path.Combine(scenesDir, "main_scene.py");

                if (File.Exists(pyScenePath))
                {
                    var tab = _openTabs.FirstOrDefault(t => t.FilePath.Equals(pyScenePath, StringComparison.OrdinalIgnoreCase));
                    if (tab != null)
                    {
                        await SaveTabAsync(tab);
                    }
                    string code = await File.ReadAllTextAsync(pyScenePath);
                    ParseSceneCodeIntoCurrentScene(code);
                }
                else
                {
                    AutoSyncSceneCode();
                }

                RedrawScene();
                SyncSceneTo3DViewport();
                UpdateHierarchy();
                UpdateInspector();
                ShowToast("Synchronized 3D Scene and Python Code");
            }
            catch (Exception ex)
            {
                await CustomDialog.ShowAsync(Window.GetWindow(this), $"Sync note: {ex.Message}", "Synchronization");
            }
        }

        private void ParseSceneCodeIntoCurrentScene(string pythonCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(pythonCode) || !pythonCode.Contains("SCENE_OBJECTS")) return;

                var dictMatches = Regex.Matches(pythonCode, @"\{\s*'id':\s*'([^']*)'.*?\}", RegexOptions.Singleline);
                if (dictMatches.Count == 0) return;

                SaveSceneUndoSnapshot();
                _currentScene.Objects.Clear();

                foreach (Match m in dictMatches)
                {
                    string dictText = m.Value;
                    string id = Regex.Match(dictText, @"'id':\s*'([^']*)'").Groups[1].Value;
                    string name = Regex.Match(dictText, @"'name':\s*'([^']*)'").Groups[1].Value;
                    string cat = Regex.Match(dictText, @"'category_3d':\s*'([^']*)'").Groups[1].Value;
                    string prim = Regex.Match(dictText, @"'primitive_3d':\s*'([^']*)'").Groups[1].Value;
                    string sub = Regex.Match(dictText, @"'subtype_3d':\s*'([^']*)'").Groups[1].Value;
                    string color = Regex.Match(dictText, @"'color':\s*'([^']*)'").Groups[1].Value;

                    double.TryParse(Regex.Match(dictText, @"'x':\s*([\d\.-]+)").Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double x);
                    double.TryParse(Regex.Match(dictText, @"'y':\s*([\d\.-]+)").Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double y);
                    double.TryParse(Regex.Match(dictText, @"'z':\s*([\d\.-]+)").Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double z);
                    double.TryParse(Regex.Match(dictText, @"'width':\s*([\d\.-]+)").Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double width);
                    double.TryParse(Regex.Match(dictText, @"'height':\s*([\d\.-]+)").Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double height);
                    double.TryParse(Regex.Match(dictText, @"'depth':\s*([\d\.-]+)").Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double depth);
                    double.TryParse(Regex.Match(dictText, @"'scale_x':\s*([\d\.-]+)").Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double sx);
                    double.TryParse(Regex.Match(dictText, @"'scale_y':\s*([\d\.-]+)").Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double sy);
                    double.TryParse(Regex.Match(dictText, @"'scale_z':\s*([\d\.-]+)").Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double sz);
                    double.TryParse(Regex.Match(dictText, @"'rotation':\s*([\d\.-]+)").Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double rot);

                    _currentScene.Objects.Add(new SceneObject
                    {
                        Id = string.IsNullOrEmpty(id) ? Guid.NewGuid().ToString("N")[..8] : id,
                        Name = string.IsNullOrEmpty(name) ? "Object" : name,
                        Category3D = string.IsNullOrEmpty(cat) ? "BasicShapes" : cat,
                        Primitive3D = string.IsNullOrEmpty(prim) ? "Cube" : prim,
                        Subtype3D = string.IsNullOrEmpty(sub) ? "Cube" : sub,
                        Color = string.IsNullOrEmpty(color) ? "#ff8c00" : color,
                        X = x,
                        Y = y,
                        Z = z,
                        Width = width > 0 ? width : 64,
                        Height = height > 0 ? height : 64,
                        Depth = depth > 0 ? depth : 64,
                        ScaleX = sx > 0 ? sx : 1.0,
                        ScaleY = sy > 0 ? sy : 1.0,
                        ScaleZ = sz > 0 ? sz : 1.0,
                        Rotation = rot
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ParseSceneCode error: {ex.Message}");
            }
        }

        private async void OnImport3DAssetClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "3D Asset Models (*.glb;*.gltf;*.obj;*.fbx)|*.glb;*.gltf;*.obj;*.fbx|All Files (*.*)|*.*",
                    Title = "Import 3D Asset Model"
                };

                if (dlg.ShowDialog() == true)
                {
                    string modelsDir = Path.Combine(_projectPath, "assets", "models");
                    if (!Directory.Exists(modelsDir)) Directory.CreateDirectory(modelsDir);

                    string destFile = Path.Combine(modelsDir, Path.GetFileName(dlg.FileName));
                    File.Copy(dlg.FileName, destFile, true);

                    SaveSceneUndoSnapshot();
                    string relPath = Path.GetRelativePath(_projectPath, destFile).Replace('\\', '/');
                    var modelObj = new SceneObject
                    {
                        Name = Path.GetFileNameWithoutExtension(dlg.FileName),
                        Category3D = "Custom Models",
                        Primitive3D = "CustomModel",
                        Subtype3D = Path.GetExtension(dlg.FileName).TrimStart('.').ToUpperInvariant(),
                        ModelPath = relPath,
                        Color = "#ff8c00",
                        X = 160 + (_currentScene.Objects.Count * 25),
                        Y = 160 + (_currentScene.Objects.Count * 25),
                        Z = 0,
                        Width = 64,
                        Height = 64,
                        Depth = 64
                    };

                    _currentScene.Objects.Add(modelObj);
                    _selectedGameObject = modelObj;
                    LoadFiles();
                    RefreshAssetsWorkspace();
                    UpdateHierarchy();
                    SceneHierarchy.SelectedItem = modelObj;
                    RedrawScene();
                    SyncSceneTo3DViewport();
                    UpdateInspector();
                    AutoSyncSceneCode();
                    Populate3DToolPalette();

                    await CustomDialog.ShowAsync(Window.GetWindow(this),
                        $"Successfully imported 3D asset '{Path.GetFileName(dlg.FileName)}' into assets/models/ and placed it in the scene.",
                        "3D Asset Imported",
                        DialogIcon.Success);
                }
            }
            catch (Exception ex)
            {
                await CustomDialog.ShowAsync(Window.GetWindow(this), $"Failed to import 3D asset: {ex.Message}", "Import Error", DialogIcon.Error);
            }
        }

        private async void OnManage3DPackagesClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                var packageDialog = new StackPanel { Width = 560, Margin = new Thickness(6) };
                packageDialog.Children.Add(new TextBlock
                {
                    Text = "Install and enable pre-packaged 3D asset libraries, props, environments, and engine tools:",
                    Foreground = Brushes.Silver,
                    FontSize = 12,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 14)
                });

                var packages = new[]
                {
                    ("Nature & Environment Pack", "Low-poly trees, rocks, terrain chunks, grass foliage, and water bodies.", "assets/models/nature"),
                    ("Urban City & Road Kit", "Modular roads, city buildings, sidewalks, fences, and street lamps.", "assets/models/city"),
                    ("Vehicles & Transport Pack", "Low-poly sedans, freight trucks, city transit buses, and quadcopters.", "assets/models/vehicles"),
                    ("Interior Furniture Pack", "Dining tables, office desks, chairs, doors, windows, and crates.", "assets/models/furniture"),
                    ("Sci-Fi Station Set", "Futuristic crates, cargo containers, consoles, platforms, and antennas.", "assets/models/scifi")
                };

                foreach (var (pkgName, pkgDesc, pkgPath) in packages)
                {
                    string fullPath = Path.Combine(_projectPath, pkgPath);
                    bool isInstalled = Directory.Exists(fullPath);

                    var card = new Border
                    {
                        Background = new SolidColorBrush(Color.FromRgb(35, 35, 38)),
                        BorderBrush = new SolidColorBrush(Color.FromRgb(60, 60, 65)),
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(4),
                        Padding = new Thickness(12, 10, 12, 10),
                        Margin = new Thickness(0, 0, 0, 10)
                    };

                    var row = new Grid();
                    row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                    var info = new StackPanel();
                    var titleRow = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };
                    titleRow.Children.Add(new TextBlock { Text = pkgName, Foreground = Brushes.White, FontWeight = FontWeights.SemiBold, FontSize = 12 });

                    var statusBadge = new Border
                    {
                        Background = isInstalled ? new SolidColorBrush(Color.FromRgb(40, 95, 60)) : new SolidColorBrush(Color.FromRgb(60, 60, 65)),
                        CornerRadius = new CornerRadius(3),
                        Padding = new Thickness(6, 1, 6, 1),
                        Margin = new Thickness(10, 0, 0, 0),
                        Child = new TextBlock
                        {
                            Text = isInstalled ? "Installed & Enabled" : "Available",
                            Foreground = isInstalled ? new SolidColorBrush(Color.FromRgb(78, 201, 176)) : Brushes.Silver,
                            FontSize = 10
                        }
                    };
                    titleRow.Children.Add(statusBadge);
                    info.Children.Add(titleRow);
                    info.Children.Add(new TextBlock { Text = pkgDesc, Foreground = new SolidColorBrush(Color.FromRgb(160, 160, 160)), FontSize = 11, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 0, 0) });

                    var actionBtn = new Button
                    {
                        Content = isInstalled ? "Disable / Remove" : "Install Package",
                        Background = isInstalled ? new SolidColorBrush(Color.FromRgb(70, 40, 40)) : new SolidColorBrush(Color.FromRgb(255, 140, 0)),
                        Foreground = Brushes.White,
                        Height = 28,
                        Padding = new Thickness(12, 0, 12, 0),
                        Style = (Style)FindResource("CommonRoundedButtonStyle"),
                        FontSize = 11
                    };

                    actionBtn.Click += async (_, _) =>
                    {
                        if (isInstalled)
                        {
                            if (Directory.Exists(fullPath)) Directory.Delete(fullPath, true);
                            ShowToast($"Disabled {pkgName}");
                        }
                        else
                        {
                            Directory.CreateDirectory(fullPath);
                            await File.WriteAllTextAsync(Path.Combine(fullPath, "README.md"), $"# {pkgName}\nInstalled for Cobra 3D Engine.\n");
                            await File.WriteAllTextAsync(Path.Combine(fullPath, "package_manifest.json"), JsonSerializer.Serialize(new { package = pkgName, version = "1.0.0", status = "active" }));
                            ShowToast($"Installed {pkgName} successfully!");
                        }
                        LoadFiles();
                        RefreshAssetsWorkspace();
                        Populate3DToolPalette();
                    };

                    Grid.SetColumn(info, 0);
                    Grid.SetColumn(actionBtn, 1);
                    row.Children.Add(info);
                    row.Children.Add(actionBtn);
                    card.Child = row;
                    packageDialog.Children.Add(card);
                }

                await AnimatedModal.ShowCustomModalAsync(Window.GetWindow(this), "3D Asset Packages & Preset Kits", packageDialog, "Close");
            }
            catch (Exception ex)
            {
                await CustomDialog.ShowAsync(Window.GetWindow(this), $"Package error: {ex.Message}", "Packages");
            }
        }

        private void OnSceneOverlayOptionChanged(object sender, RoutedEventArgs e)
        {
            if (IsLoaded) RedrawScene();
        }

        private void OnSceneZoomInClicked(object sender, RoutedEventArgs e)
        {
            _zoom = Math.Clamp(_zoom * 1.15, 0.25, 4.0);
            RedrawScene();
        }

        private void OnSceneZoomOutClicked(object sender, RoutedEventArgs e)
        {
            _zoom = Math.Clamp(_zoom * 0.85, 0.25, 4.0);
            RedrawScene();
        }

        private void OnSceneResetViewClicked(object sender, RoutedEventArgs e)
        {
            _zoom = 1.0;
            _camX = 0;
            _camY = 0;
            RedrawScene();
        }

        private void OnToggle2D3DViewportClicked(object sender, RoutedEventArgs e)
        {
            _is3DMode = !_is3DMode;
            _currentScene.Is3D = _is3DMode;
            Toggle2D3DText.Text = _is3DMode ? "Mode: 3D Three.js" : "Mode: 2D Canvas";
            SceneCanvas.Visibility = _is3DMode ? Visibility.Collapsed : Visibility.Visible;
            Viewport3DWebView.Visibility = _is3DMode ? Visibility.Visible : Visibility.Collapsed;

            if (_is3DMode)
            {
                SyncSceneTo3DViewport();
            }
            else
            {
                RedrawScene();
            }
        }

        private void SyncSceneTo3DViewport()
        {
            if (!_is3DWebViewReady || Viewport3DWebView?.CoreWebView2 == null) return;

            try
            {
                var payload = new
                {
                    Objects = _currentScene.Objects.Select(o => new
                    {
                        o.Id,
                        o.Name,
                        o.Category3D,
                        o.Primitive3D,
                        o.Subtype3D,
                        o.ModelPath,
                        o.X,
                        o.Y,
                        o.Z,
                        o.Width,
                        o.Height,
                        o.Depth,
                        o.ScaleX,
                        o.ScaleY,
                        o.ScaleZ,
                        o.Rotation,
                        o.RotationX,
                        o.RotationY,
                        o.Color,
                        o.IsVisible,
                        IsSelected = o == _selectedGameObject
                    })
                };

                string json = JsonSerializer.Serialize(payload);
                Viewport3DWebView.CoreWebView2.ExecuteScriptAsync($"if (window.updateSceneFromHost) window.updateSceneFromHost({json});");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SyncSceneTo3DViewport error: {ex.Message}");
            }
        }

        private void OnPlayToggle(object sender, RoutedEventArgs e)
        {
            _isPlaying = PlayToggle.IsChecked == true;
            ShowToast(_isPlaying ? "Physics simulation started" : "Physics simulation stopped");
        }

        private void UpdateSceneSimulation(double dt)
        {
            foreach (var obj in _currentScene.Objects)
            {
                if (!obj.IsVisible || !obj.HasRigidbody || !obj.UseGravity) continue;
                obj.Y += 400.0 * obj.GravityScale * dt * 0.25;
                if (obj.Y + obj.Height > 480)
                {
                    obj.Y = 480 - obj.Height;
                }
            }
        }

        private void RedrawScene()
        {
            if (SceneCanvas == null) return;
            SceneCanvas.Children.Clear();
            DrawGrid();

            foreach (var obj in _currentScene.Objects.OrderBy(o => o.SortingOrder))
            {
                if (!obj.IsVisible) continue;

                double sx = (obj.X * _zoom) + _camX;
                double sy = (obj.Y * _zoom) + _camY;
                double sw = Math.Max(8, obj.Width * obj.ScaleX * _zoom);
                double sh = Math.Max(8, obj.Height * obj.ScaleY * _zoom);

                FrameworkElement visual;
                string fullSpritePath = !string.IsNullOrEmpty(obj.SpritePath)
                    ? (Path.IsPathRooted(obj.SpritePath) ? obj.SpritePath : Path.Combine(_projectPath, obj.SpritePath))
                    : string.Empty;

                if (!string.IsNullOrEmpty(fullSpritePath) && File.Exists(fullSpritePath))
                {
                    visual = new System.Windows.Controls.Image
                    {
                        Source = new BitmapImage(new Uri(fullSpritePath)),
                        Width = sw,
                        Height = sh,
                        Stretch = Stretch.Fill
                    };
                }
                else
                {
                    obj.IsSelected = obj == _selectedGameObject;
                    Color fillColor = Color.FromRgb(255, 140, 0);
                    try
                    {
                        fillColor = (Color)ColorConverter.ConvertFromString(obj.Color);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                    }

                    visual = new Border
                    {
                        Width = sw,
                        Height = sh,
                        Background = new SolidColorBrush(Color.FromArgb(220, fillColor.R, fillColor.G, fillColor.B)),
                        BorderBrush = obj.IsSelected ? Brushes.Yellow : Brushes.White,
                        BorderThickness = new Thickness(obj.IsSelected ? 2 : 1),
                        CornerRadius = new CornerRadius(2),
                        Child = new TextBlock
                        {
                            Text = obj.Name,
                            Foreground = Brushes.White,
                            FontSize = 10,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    };
                }

                visual.RenderTransformOrigin = new Point(0.5, 0.5);
                visual.RenderTransform = new RotateTransform(obj.Rotation);

                Canvas.SetLeft(visual, sx);
                Canvas.SetTop(visual, sy);
                SceneCanvas.Children.Add(visual);

                if (ShowCollidersCheck?.IsChecked == true && !string.Equals(obj.ColliderType, "None", StringComparison.OrdinalIgnoreCase))
                {
                    if (obj.ColliderType == "Circle")
                    {
                        double r = obj.ColliderRadius * 2 * _zoom;
                        var circle = new Ellipse
                        {
                            Width = r,
                            Height = r,
                            Stroke = Brushes.LimeGreen,
                            StrokeThickness = 1.5,
                            StrokeDashArray = [3, 2]
                        };
                        Canvas.SetLeft(circle, sx + (sw - r) / 2);
                        Canvas.SetTop(circle, sy + (sh - r) / 2);
                        SceneCanvas.Children.Add(circle);
                    }
                    else
                    {
                        var rect = new System.Windows.Shapes.Rectangle
                        {
                            Width = sw,
                            Height = sh,
                            Stroke = Brushes.LimeGreen,
                            StrokeThickness = 1.5,
                            StrokeDashArray = [4, 2]
                        };
                        Canvas.SetLeft(rect, sx);
                        Canvas.SetTop(rect, sy);
                        SceneCanvas.Children.Add(rect);
                    }
                }
            }

            if (_selectedGameObject is { IsVisible: true })
            {
                DrawGizmos(_selectedGameObject);
            }

            UpdateHierarchy();
        }

        private void DrawGrid()
        {
            double width = SceneCanvas.ActualWidth > 0 ? SceneCanvas.ActualWidth : 900;
            double height = SceneCanvas.ActualHeight > 0 ? SceneCanvas.ActualHeight : 600;
            double step = Math.Max(10, DefaultGridSize * _zoom);

            var brush = new SolidColorBrush(Color.FromRgb(38, 38, 42));
            for (double x = _camX % step; x < width; x += step)
            {
                SceneCanvas.Children.Add(new Line { X1 = x, Y1 = 0, X2 = x, Y2 = height, Stroke = brush, StrokeThickness = 1 });
            }
            for (double y = _camY % step; y < height; y += step)
            {
                SceneCanvas.Children.Add(new Line { X1 = 0, Y1 = y, X2 = width, Y2 = y, Stroke = brush, StrokeThickness = 1 });
            }
        }

        private void DrawGizmos(SceneObject obj)
        {
            double cx = (obj.X + obj.Width / 2) * _zoom + _camX;
            double cy = (obj.Y + obj.Height / 2) * _zoom + _camY;

            SceneCanvas.Children.Add(new Line { X1 = cx, Y1 = cy, X2 = cx + 55, Y2 = cy, Stroke = Brushes.Red, StrokeThickness = 2.5 });
            SceneCanvas.Children.Add(new Line { X1 = cx, Y1 = cy, X2 = cx, Y2 = cy - 55, Stroke = Brushes.LimeGreen, StrokeThickness = 2.5 });

            var rotRing = new Ellipse
            {
                Width = 76,
                Height = 76,
                Stroke = Brushes.DeepSkyBlue,
                StrokeThickness = 1.2,
                StrokeDashArray = [3, 3]
            };
            Canvas.SetLeft(rotRing, cx - 38);
            Canvas.SetTop(rotRing, cy - 38);
            SceneCanvas.Children.Add(rotRing);
        }

        private void SceneCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Point mouse = e.GetPosition(SceneCanvas);
            _lastMousePos = mouse;

            if (Keyboard.IsKeyDown(Key.Space))
            {
                _isPanning = true;
                _lastPanPoint = mouse;
                return;
            }

            if (_selectedGameObject is { IsLocked: false })
            {
                double cx = (_selectedGameObject.X + _selectedGameObject.Width / 2) * _zoom + _camX;
                double cy = (_selectedGameObject.Y + _selectedGameObject.Height / 2) * _zoom + _camY;

                if (Math.Abs(mouse.X - (cx + 55)) < 12 && Math.Abs(mouse.Y - cy) < 12)
                {
                    SaveSceneUndoSnapshot();
                    _currentGizmo = GizmoMode.MoveX;
                    return;
                }
                if (Math.Abs(mouse.X - cx) < 12 && Math.Abs(mouse.Y - (cy - 55)) < 12)
                {
                    SaveSceneUndoSnapshot();
                    _currentGizmo = GizmoMode.MoveY;
                    return;
                }
                double dist = Math.Sqrt(Math.Pow(mouse.X - cx, 2) + Math.Pow(mouse.Y - cy, 2));
                if (Math.Abs(dist - 38) < 8)
                {
                    SaveSceneUndoSnapshot();
                    _currentGizmo = GizmoMode.Rotate;
                    return;
                }
            }

            double worldX = (mouse.X - _camX) / _zoom;
            double worldY = (mouse.Y - _camY) / _zoom;

            _selectedGameObject = _currentScene.Objects
                .OrderByDescending(o => o.SortingOrder)
                .FirstOrDefault(o => o.IsVisible && worldX >= o.X && worldX <= o.X + o.Width && worldY >= o.Y && worldY <= o.Y + o.Height);

            if (_selectedGameObject is { IsLocked: false })
            {
                SaveSceneUndoSnapshot();
                _isDragging = true;
                _dragOffset = new Point(worldX - _selectedGameObject.X, worldY - _selectedGameObject.Y);
            }

            RedrawScene();
            UpdateInspector();
        }

        private void SceneCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            Point pos = e.GetPosition(SceneCanvas);

            if (_isPanning && e.LeftButton == MouseButtonState.Pressed)
            {
                _camX += pos.X - _lastPanPoint.X;
                _camY += pos.Y - _lastPanPoint.Y;
                _lastPanPoint = pos;
                RedrawScene();
                return;
            }

            if (_selectedGameObject is { IsLocked: false } && _currentGizmo != GizmoMode.None)
            {
                Vector delta = pos - _lastMousePos;
                if (_currentGizmo == GizmoMode.MoveX) _selectedGameObject.X += delta.X / _zoom;
                else if (_currentGizmo == GizmoMode.MoveY) _selectedGameObject.Y += delta.Y / _zoom;
                else if (_currentGizmo == GizmoMode.Rotate) _selectedGameObject.Rotation = (_selectedGameObject.Rotation + delta.X) % 360;

                _lastMousePos = pos;
                RedrawScene();
                UpdateInspector();
                return;
            }

            if (_isDragging && _selectedGameObject is { IsLocked: false } && e.LeftButton == MouseButtonState.Pressed)
            {
                double worldX = (pos.X - _camX) / _zoom - _dragOffset.X;
                double worldY = (pos.Y - _camY) / _zoom - _dragOffset.Y;

                if (SnapToGridCheck.IsChecked == true)
                {
                    int snap = Math.Max(4, SettingsManager.Instance.CurrentSettings.GridSnapSize);
                    worldX = Math.Round(worldX / snap) * snap;
                    worldY = Math.Round(worldY / snap) * snap;
                }

                _selectedGameObject.X = worldX;
                _selectedGameObject.Y = worldY;
                RedrawScene();
                UpdateInspector();
            }
        }

        private void SceneCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            _isPanning = false;
            _currentGizmo = GizmoMode.None;
        }

        private void SceneCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            _zoom = Math.Clamp(_zoom + (e.Delta > 0 ? 0.1 : -0.1), 0.25, 4.0);
            RedrawScene();
        }

        private void OnAssetDropped(object sender, DragEventArgs e)
        {
            Point pos = e.GetPosition(SceneCanvas);
            SaveSceneUndoSnapshot();

            // check if tool or object was dragged from the 3d tools palette
            if (e.Data.GetDataPresent(DataFormats.Text) && e.Data.GetData(DataFormats.Text) is string text && !string.IsNullOrWhiteSpace(text))
            {
                try
                {
                    using var doc = JsonDocument.Parse(text);
                    var root = doc.RootElement;
                    string name = root.TryGetProperty("name", out var n) ? n.GetString() ?? "3D_Object" : "3D_Object";
                    string category = root.TryGetProperty("category", out var c) ? c.GetString() ?? "BasicShapes" : "BasicShapes";
                    string primitive = root.TryGetProperty("primitive", out var p) ? p.GetString() ?? "Cube" : "Cube";
                    string subtype = root.TryGetProperty("subtype", out var sub) ? sub.GetString() ?? primitive : primitive;
                    string color = root.TryGetProperty("color", out var clr) ? clr.GetString() ?? "#ff8c00" : "#ff8c00";

                    var toolObj = new SceneObject
                    {
                        Name = $"{name}_{_currentScene.Objects.Count + 1}",
                        Category3D = category,
                        Primitive3D = primitive,
                        Subtype3D = subtype,
                        Color = color,
                        X = (pos.X - _camX) / _zoom,
                        Y = (pos.Y - _camY) / _zoom,
                        Z = 0,
                        Width = 64,
                        Height = 64,
                        Depth = 64
                    };
                    _currentScene.Objects.Add(toolObj);
                    _selectedGameObject = toolObj;
                    UpdateHierarchy();
                    SceneHierarchy.SelectedItem = toolObj;
                    RedrawScene();
                    SyncSceneTo3DViewport();
                    UpdateInspector();
                    AutoSyncSceneCode();
                    ShowToast($"Placed {toolObj.Name}");
                    return;
                }
                catch
                {
                    // fall through to file drop
                }
            }

            // check if a 3d model or image asset was dropped from file system or asset browser
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            if (e.Data.GetData(DataFormats.FileDrop) is not string[] { Length: > 0 } files) return;

            string file = files[0];
            string ext = Path.GetExtension(file).ToLowerInvariant();
            bool is3D = ext is ".glb" or ".gltf" or ".obj" or ".fbx";

            var obj = new SceneObject
            {
                Name = Path.GetFileNameWithoutExtension(file),
                SpritePath = !is3D ? Path.GetRelativePath(_projectPath, file).Replace('\\', '/') : string.Empty,
                ModelPath = is3D ? Path.GetRelativePath(_projectPath, file).Replace('\\', '/') : string.Empty,
                Primitive3D = is3D ? "CustomModel" : "Cube",
                Category3D = is3D ? "Custom" : "BasicShapes",
                X = (pos.X - _camX) / _zoom,
                Y = (pos.Y - _camY) / _zoom,
                Z = 0,
                Width = 64,
                Height = 64,
                Depth = 64
            };
            _currentScene.Objects.Add(obj);
            _selectedGameObject = obj;
            UpdateHierarchy();
            SceneHierarchy.SelectedItem = obj;
            RedrawScene();
            SyncSceneTo3DViewport();
            UpdateInspector();
            AutoSyncSceneCode();
            ShowToast($"Placed {obj.Name}");
        }

        private void UpdateHierarchy()
        {
            if (SceneHierarchy == null) return;
            string filter = SearchHierarchy?.Text.Trim() ?? "";

            List<SceneObject> list = string.IsNullOrEmpty(filter)
                ? [.. _currentScene.Objects]
                : [.. _currentScene.Objects.Where(o => o.Name.Contains(filter, StringComparison.OrdinalIgnoreCase))];

            SceneHierarchy.ItemsSource = list;
        }

        private void SearchHierarchy_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IsLoaded) UpdateHierarchy();
        }

        private void OnHierarchySelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SceneHierarchy.SelectedItem is SceneObject obj)
            {
                _selectedGameObject = obj;
                RedrawScene();
                UpdateInspector();
                if (_is3DMode && _is3DWebViewReady && Viewport3DWebView.CoreWebView2 != null)
                {
                    Viewport3DWebView.CoreWebView2.PostWebMessageAsString(JsonSerializer.Serialize(new { type = "selectObject", id = obj.Id }));
                }
            }
        }

        private void UpdateInspector()
        {
            if (InspectorPanel == null) return;
            InspectorPanel.Children.Clear();

            if (_selectedGameObject == null)
            {
                InspectorPanel.Children.Add(new TextBlock
                {
                    Text = "Select an object in the scene or hierarchy to inspect its properties.",
                    Foreground = Brushes.Gray,
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 11
                });
                return;
            }

            var obj = _selectedGameObject;

            AddInspectorTextField("Name", obj.Name, v => { obj.Name = v; RedrawScene(); SyncSceneTo3DViewport(); AutoSyncSceneCode(); });
            AddInspectorTextField("3D Category", obj.Category3D, v => { obj.Category3D = v; AutoSyncSceneCode(); });
            AddInspectorTextField("3D Shape / Primitive", obj.Primitive3D, v => { obj.Primitive3D = v; obj.Subtype3D = v; RedrawScene(); SyncSceneTo3DViewport(); AutoSyncSceneCode(); });
            AddInspectorNumberField("Position X", obj.X, v => { obj.X = v; RedrawScene(); SyncSceneTo3DViewport(); AutoSyncSceneCode(); });
            AddInspectorNumberField("Position Y", obj.Y, v => { obj.Y = v; RedrawScene(); SyncSceneTo3DViewport(); AutoSyncSceneCode(); });
            AddInspectorNumberField("Position Z (3D)", obj.Z, v => { obj.Z = v; SyncSceneTo3DViewport(); AutoSyncSceneCode(); });
            AddInspectorNumberField("Rotation", obj.Rotation, v => { obj.Rotation = v; RedrawScene(); SyncSceneTo3DViewport(); AutoSyncSceneCode(); });
            AddInspectorNumberField("Rotation X (3D)", obj.RotationX, v => { obj.RotationX = v; SyncSceneTo3DViewport(); AutoSyncSceneCode(); });
            AddInspectorNumberField("Rotation Y (3D)", obj.RotationY, v => { obj.RotationY = v; SyncSceneTo3DViewport(); AutoSyncSceneCode(); });
            AddInspectorNumberField("Width", obj.Width, v => { obj.Width = Math.Max(4, v); RedrawScene(); AutoSyncSceneCode(); });
            AddInspectorNumberField("Height", obj.Height, v => { obj.Height = Math.Max(4, v); RedrawScene(); AutoSyncSceneCode(); });
            AddInspectorNumberField("Depth (3D)", obj.Depth, v => { obj.Depth = Math.Max(4, v); SyncSceneTo3DViewport(); AutoSyncSceneCode(); });
            AddInspectorNumberField("Scale X", obj.ScaleX, v => { obj.ScaleX = Math.Max(0.1, v); RedrawScene(); SyncSceneTo3DViewport(); AutoSyncSceneCode(); });
            AddInspectorNumberField("Scale Y", obj.ScaleY, v => { obj.ScaleY = Math.Max(0.1, v); RedrawScene(); SyncSceneTo3DViewport(); AutoSyncSceneCode(); });
            AddInspectorNumberField("Scale Z (3D)", obj.ScaleZ, v => { obj.ScaleZ = Math.Max(0.1, v); SyncSceneTo3DViewport(); AutoSyncSceneCode(); });
            AddInspectorNumberField("Roughness (0-1)", obj.Roughness, v => { obj.Roughness = Math.Clamp(v, 0, 1); SyncSceneTo3DViewport(); AutoSyncSceneCode(); });
            AddInspectorNumberField("Metalness (0-1)", obj.Metalness, v => { obj.Metalness = Math.Clamp(v, 0, 1); SyncSceneTo3DViewport(); AutoSyncSceneCode(); });
            AddInspectorTextField("Color Hex", obj.Color, v => { obj.Color = v; RedrawScene(); SyncSceneTo3DViewport(); AutoSyncSceneCode(); });
            AddInspectorTextField("Layer", obj.Layer, v => { obj.Layer = v; RedrawScene(); AutoSyncSceneCode(); });
            AddInspectorNumberField("Sorting Order", obj.SortingOrder, v => { obj.SortingOrder = (int)v; RedrawScene(); AutoSyncSceneCode(); });
            if (!string.IsNullOrEmpty(obj.ModelPath))
                AddInspectorTextField("3D Model Path", obj.ModelPath, v => { obj.ModelPath = v; SyncSceneTo3DViewport(); AutoSyncSceneCode(); });
            AddInspectorTextField("Sprite Path", obj.SpritePath, v => { obj.SpritePath = v; RedrawScene(); AutoSyncSceneCode(); });
            AddInspectorTextField("Python Script", obj.ScriptPath, v => { obj.ScriptPath = v; AutoSyncSceneCode(); });
            AddInspectorTextField("Collider (Rectangle/Circle/None)", obj.ColliderType, v => { obj.ColliderType = v; RedrawScene(); AutoSyncSceneCode(); });

            var visCheck = new CheckBox { Content = "Visible", Foreground = Brushes.White, IsChecked = obj.IsVisible, Margin = new Thickness(0, 6, 0, 2) };
            visCheck.Checked += (_, _) => { obj.IsVisible = true; RedrawScene(); SyncSceneTo3DViewport(); AutoSyncSceneCode(); };
            visCheck.Unchecked += (_, _) => { obj.IsVisible = false; RedrawScene(); SyncSceneTo3DViewport(); AutoSyncSceneCode(); };
            InspectorPanel.Children.Add(visCheck);

            var lockCheck = new CheckBox { Content = "Lock Transform", Foreground = Brushes.White, IsChecked = obj.IsLocked, Margin = new Thickness(0, 4, 0, 6) };
            lockCheck.Checked += (_, _) => { obj.IsLocked = true; UpdateHierarchy(); };
            lockCheck.Unchecked += (_, _) => { obj.IsLocked = false; UpdateHierarchy(); };
            InspectorPanel.Children.Add(lockCheck);

            var rbCheck = new CheckBox { Content = "Enable Rigidbody", Foreground = Brushes.LightGreen, IsChecked = obj.HasRigidbody, Margin = new Thickness(0, 4, 0, 4) };
            rbCheck.Checked += (_, _) => { obj.HasRigidbody = true; AutoSyncSceneCode(); };
            rbCheck.Unchecked += (_, _) => { obj.HasRigidbody = false; AutoSyncSceneCode(); };
            InspectorPanel.Children.Add(rbCheck);

            var gravCheck = new CheckBox { Content = "Use Gravity (Physics)", Foreground = Brushes.LightSkyBlue, IsChecked = obj.UseGravity, Margin = new Thickness(0, 4, 0, 4) };
            gravCheck.Checked += (_, _) => { obj.UseGravity = true; AutoSyncSceneCode(); };
            gravCheck.Unchecked += (_, _) => { obj.UseGravity = false; AutoSyncSceneCode(); };
            InspectorPanel.Children.Add(gravCheck);
        }

        private void AddInspectorTextField(string label, string initialValue, Action<string> onUpdate)
        {
            InspectorPanel.Children.Add(new TextBlock { Text = label, Foreground = Brushes.Silver, FontSize = 11, Margin = new Thickness(0, 4, 0, 2) });
            var box = new TextBox
            {
                Text = initialValue,
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(68, 68, 68)),
                Padding = new Thickness(4, 2, 4, 2),
                FontSize = 11
            };
            box.LostFocus += (_, _) => onUpdate(box.Text);
            InspectorPanel.Children.Add(box);
        }

        private void AddInspectorNumberField(string label, double initialValue, Action<double> onUpdate)
        {
            AddInspectorTextField(label, Math.Round(initialValue, 2).ToString(CultureInfo.InvariantCulture), text =>
            {
                if (double.TryParse(text, out double val))
                    onUpdate(val);
            });
        }

        private void InitializeVisualScriptingLibrary()
        {
            var lib = NodeGraph.GetStandardLibrary();
            NodeTemplateCombo.ItemsSource = lib;
            NodeTemplateCombo.DisplayMemberPath = "Title";
            NodeTemplateCombo.SelectedIndex = 0;

            if (_currentGraph.Nodes.Count == 0 && lib.Count >= 4)
            {
                var startNode = CreateNodeFromTemplateDef(lib[0], 80, 120);
                var moveNode = CreateNodeFromTemplateDef(lib[3], 320, 120);
                startNode.Outputs.Add(moveNode.Id);
                _currentGraph.Nodes.Add(startNode);
                _currentGraph.Nodes.Add(moveNode);
            }
        }

        private static Node CreateNodeFromTemplateDef(NodeTemplateDefinition def, double x, double y)
        {
            return new Node
            {
                Category = def.Category,
                Type = def.Type,
                Title = def.Title,
                X = x,
                Y = y,
                Properties = new Dictionary<string, string>(def.DefaultProperties)
            };
        }

        private void OnAddVisualNodeClicked(object sender, RoutedEventArgs e)
        {
            if (NodeTemplateCombo.SelectedItem is not NodeTemplateDefinition def) return;
            var node = CreateNodeFromTemplateDef(def, 120 + (_currentGraph.Nodes.Count * 35), 100 + (_currentGraph.Nodes.Count * 25));
            _currentGraph.Nodes.Add(node);
            _selectedNode = node;
            RedrawNodeGraph();
        }

        private void OnDeleteVisualNodeClicked(object sender, RoutedEventArgs e)
        {
            if (_selectedNode == null) return;
            _currentGraph.Nodes.Remove(_selectedNode);
            foreach (var n in _currentGraph.Nodes)
            {
                n.Outputs.Remove(_selectedNode.Id);
            }
            _selectedNode = null;
            RedrawNodeGraph();
        }

        private async void OnValidateNodeGraphClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                var (isValid, errors) = _currentGraph.ValidateGraph();
                string msg = isValid ? "Graph is valid and ready to generate Python code." : string.Join("\n", errors);
                await CustomDialog.ShowAsync(Window.GetWindow(this), msg, "Graph Validation", isValid ? DialogIcon.Success : DialogIcon.Warning);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void OnGeneratePythonFromGraphClicked(object sender, RoutedEventArgs e)
        {
            string scriptsDir = Path.Combine(_projectPath, "scripts");
            Directory.CreateDirectory(scriptsDir);
            string outPath = Path.Combine(scriptsDir, "visual_logic.py");
            File.WriteAllText(outPath, _currentGraph.GeneratePythonCode());
            LoadFiles();
            OpenFileInTab(outPath);
            ShowToast("Generated scripts/visual_logic.py");
        }

        private void OnSaveNodeGraphClicked(object sender, RoutedEventArgs e)
        {
            string scriptsDir = Path.Combine(_projectPath, "scripts");
            Directory.CreateDirectory(scriptsDir);
            string path = Path.Combine(scriptsDir, $"{_currentGraph.GraphName}.graph.json");
            _currentGraph.Save(path);
            LoadFiles();
            ShowToast($"Saved {Path.GetFileName(path)}");
        }

        private void OnLoadNodeGraphClicked(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                InitialDirectory = _projectPath,
                Filter = "Visual Graph (*.graph.json)|*.graph.json|All Files (*.*)|*.*"
            };
            if (dlg.ShowDialog() == true)
            {
                var loaded = NodeGraph.Load(dlg.FileName);
                _currentGraph.Nodes = loaded.Nodes;
                _currentGraph.GraphName = loaded.GraphName;
                RedrawNodeGraph();
                ShowToast($"Loaded {loaded.GraphName}");
            }
        }

        private void RedrawNodeGraph()
        {
            if (NodeCanvas == null) return;
            NodeCanvas.Children.Clear();

            foreach (var node in _currentGraph.Nodes)
            {
                foreach (var targetId in node.Outputs)
                {
                    var target = _currentGraph.Nodes.FirstOrDefault(n => n.Id == targetId);
                    if (target != null)
                    {
                        NodeCanvas.Children.Add(new Line
                        {
                            X1 = node.X + 150,
                            Y1 = node.Y + 32,
                            X2 = target.X,
                            Y2 = target.Y + 32,
                            Stroke = Brushes.Orange,
                            StrokeThickness = 2
                        });
                    }
                }
            }

            foreach (var node in _currentGraph.Nodes)
            {
                string summary = node.Properties.Count > 0
                    ? string.Join(", ", node.Properties.Select(kv => $"{kv.Key}={kv.Value}"))
                    : node.Type;

                var card = new Border
                {
                    Width = 155,
                    MinHeight = 64,
                    Background = new SolidColorBrush(Color.FromRgb(42, 42, 46)),
                    BorderBrush = node == _selectedNode ? Brushes.Orange : new SolidColorBrush(Color.FromRgb(80, 80, 86)),
                    BorderThickness = new Thickness(node == _selectedNode ? 2 : 1),
                    CornerRadius = new CornerRadius(5),
                    Padding = new Thickness(8),
                    Cursor = Cursors.Hand,
                    Child = new StackPanel
                    {
                        Children =
                        {
                            new TextBlock { Text = node.Category.ToUpperInvariant(), Foreground = Brushes.MediumAquamarine, FontSize = 9, FontWeight = FontWeights.Bold },
                            new TextBlock { Text = node.Title, Foreground = Brushes.White, FontSize = 11, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 2, 0, 2) },
                            new TextBlock { Text = summary, Foreground = Brushes.Silver, FontSize = 10, TextTrimming = TextTrimming.CharacterEllipsis }
                        }
                    }
                };

                Point dragStart = default;
                bool draggingNode = false;

                card.MouseLeftButtonDown += (_, ev) =>
                {
                    ev.Handled = true;
                    if (_pendingConnectionSource != null && _pendingConnectionSource != node)
                    {
                        if (!_pendingConnectionSource.Outputs.Contains(node.Id))
                            _pendingConnectionSource.Outputs.Add(node.Id);
                        _pendingConnectionSource = null;
                        RedrawNodeGraph();
                        return;
                    }

                    _selectedNode = node;
                    draggingNode = true;
                    dragStart = ev.GetPosition(NodeCanvas);
                    card.CaptureMouse();
                    PopulateNodeInspector(node);
                };

                card.MouseMove += (_, ev) =>
                {
                    if (draggingNode && ev.LeftButton == MouseButtonState.Pressed)
                    {
                        Point cur = ev.GetPosition(NodeCanvas);
                        node.X += cur.X - dragStart.X;
                        node.Y += cur.Y - dragStart.Y;
                        dragStart = cur;
                        RedrawNodeGraph();
                    }
                };

                card.MouseLeftButtonUp += (_, _) =>
                {
                    draggingNode = false;
                    card.ReleaseMouseCapture();
                };

                Canvas.SetLeft(card, node.X);
                Canvas.SetTop(card, node.Y);
                NodeCanvas.Children.Add(card);
            }
        }

        private void PopulateNodeInspector(Node node)
        {
            NodePropertiesPanel.Children.Clear();
            NodePropertiesPanel.Children.Add(new TextBlock { Text = "NODE PROPERTIES", Foreground = Brushes.Silver, FontWeight = FontWeights.Bold, FontSize = 11, Margin = new Thickness(0, 0, 0, 8) });
            NodePropertiesPanel.Children.Add(new TextBlock { Text = $"Title: {node.Title}", Foreground = Brushes.White, FontSize = 12, Margin = new Thickness(0, 0, 0, 6) });

            foreach (var key in node.Properties.Keys.ToList())
            {
                NodePropertiesPanel.Children.Add(new TextBlock { Text = key, Foreground = Brushes.Silver, FontSize = 11 });
                var paramBox = new TextBox { Text = node.Properties[key], Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)), Foreground = Brushes.White, Margin = new Thickness(0, 4, 0, 8), Padding = new Thickness(5, 3, 5, 3) };
                string capturedKey = key;
                paramBox.TextChanged += (_, _) => { node.Properties[capturedKey] = paramBox.Text; RedrawNodeGraph(); };
                NodePropertiesPanel.Children.Add(paramBox);
            }

            var connectBtn = new Button { Content = "Connect to Another Node", Padding = new Thickness(8, 5, 8, 5), Background = Brushes.DarkOrange, Foreground = Brushes.White, BorderThickness = new Thickness(0), Cursor = Cursors.Hand };
            connectBtn.Click += (_, _) =>
            {
                _pendingConnectionSource = node;
                ShowToast("Click another node to connect");
            };
            NodePropertiesPanel.Children.Add(connectBtn);
        }

        private void RefreshAssetsWorkspace()
        {
            if (AssetListBox == null) return;

            string search = AssetSearchBox?.Text.Trim() ?? "";
            string cat = (AssetCategoryCombo?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All Assets";
            AssetCategory? filterCat = null;
            if (cat != "All Assets" && Enum.TryParse<AssetCategory>(cat, out var parsedCat))
            {
                filterCat = parsedCat;
            }

            AssetListBox.ItemsSource = AssetService.ScanProjectAssets(_projectPath, search, filterCat);
        }

        private void AssetSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IsLoaded) RefreshAssetsWorkspace();
        }

        private void AssetCategoryCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded) RefreshAssetsWorkspace();
        }

        private void OnRefreshAssetsClicked(object sender, RoutedEventArgs e) => RefreshAssetsWorkspace();

        private async void OnImportAssetClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "Import Validated Game Asset",
                    Filter = "Game Assets (*.png;*.jpg;*.bmp;*.gif;*.wav;*.ogg;*.mp3;*.obj;*.gltf;*.glb;*.ttf)|*.png;*.jpg;*.bmp;*.gif;*.wav;*.ogg;*.mp3;*.obj;*.gltf;*.glb;*.ttf|All Files (*.*)|*.*"
                };

                if (dlg.ShowDialog() == true)
                {
                    var (ok, message, _) = AssetService.ImportAsset(_projectPath, dlg.FileName);
                    if (ok)
                    {
                        RefreshAssetsWorkspace();
                        LoadFiles();
                        ShowToast(message);
                    }
                    else
                    {
                        await CustomDialog.ShowAsync(Window.GetWindow(this), message, "Asset Validation Failed", DialogIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private async void OnCheckMissingAssetsClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                var missing = AssetService.FindMissingSceneAssets(_projectPath);
                var duplicates = AssetService.FindDuplicateAssets(_projectPath);

                string report = $"Missing Scene Asset References: {missing.Count}\n" +
                                (missing.Count > 0 ? string.Join("\n", missing) + "\n\n" : "\n") +
                                $"Duplicate Asset Files: {duplicates.Count}\n" +
                                (duplicates.Count > 0 ? string.Join("\n", duplicates) : "No duplicate assets found.");

                await CustomDialog.ShowAsync(Window.GetWindow(this), report, "Asset Audit Report");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void AssetListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AssetListBox.SelectedItem is not AssetItem asset) return;

            AssetDetailsText.Text = $"Name: {asset.Name}\nCategory: {asset.Category}\nSize: {asset.FormattedSize}\nInfo: {asset.DimensionsOrInfo}\nCode References: {asset.UsageCount}\nPath: {asset.RelativePath}";
            AudioPreviewControls.Visibility = asset.Category == AssetCategory.Audio ? Visibility.Visible : Visibility.Collapsed;

            if (asset.Category == AssetCategory.Image && File.Exists(asset.FullPath))
            {
                AssetImagePreview.Source = new BitmapImage(new Uri(asset.FullPath));
            }
            else
            {
                AssetImagePreview.Source = null;
            }
        }

        private void OnPlayAudioAssetClicked(object sender, RoutedEventArgs e)
        {
            if (AssetListBox.SelectedItem is AssetItem asset && File.Exists(asset.FullPath))
            {
                _audioPreviewPlayer.Open(new Uri(asset.FullPath));
                _audioPreviewPlayer.Play();
                ShowToast($"Playing {asset.Name}");
            }
        }

        private void OnStopAudioAssetClicked(object sender, RoutedEventArgs e)
        {
            _audioPreviewPlayer.Stop();
        }

        private void OnSelectSpriteSheetClicked(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                InitialDirectory = _projectPath,
                Filter = "Image Files (*.png;*.jpg;*.bmp)|*.png;*.jpg;*.bmp"
            };
            if (dlg.ShowDialog() == true)
            {
                _loadedSpriteSheet = new BitmapImage(new Uri(dlg.FileName));
                AnimPreviewImage.Source = _loadedSpriteSheet;
                AnimStatusLabel.Text = $"Loaded: {Path.GetFileName(dlg.FileName)} ({_loadedSpriteSheet.PixelWidth}x{_loadedSpriteSheet.PixelHeight})";
            }
        }

        private void OnStartAnimPreviewClicked(object sender, RoutedEventArgs e)
        {
            if (_loadedSpriteSheet == null || _animPreviewTimer == null)
            {
                ShowToast("Load a sprite sheet first");
                return;
            }
            if (int.TryParse(AnimFrameDurationBox.Text, out int ms) && ms >= 20)
            {
                _animPreviewTimer.Interval = TimeSpan.FromMilliseconds(ms);
            }
            _currentAnimFrame = 0;
            _animPreviewTimer.Start();
        }

        private void OnStopAnimPreviewClicked(object sender, RoutedEventArgs e)
        {
            _animPreviewTimer?.Stop();
        }

        private void StepAnimationPreview()
        {
            if (_loadedSpriteSheet == null) return;
            int frameCount = int.TryParse(AnimFrameCountBox.Text, out int fc) && fc > 0 ? fc : 4;
            int frameWidth = Math.Max(1, _loadedSpriteSheet.PixelWidth / frameCount);
            int frameHeight = _loadedSpriteSheet.PixelHeight;

            if (_currentAnimFrame >= frameCount)
            {
                if (AnimLoopCheck.IsChecked == true) _currentAnimFrame = 0;
                else { _animPreviewTimer?.Stop(); return; }
            }

            int x = _currentAnimFrame * frameWidth;
            if (x + frameWidth <= _loadedSpriteSheet.PixelWidth)
            {
                AnimPreviewImage.Source = new CroppedBitmap(_loadedSpriteSheet, new Int32Rect(x, 0, frameWidth, frameHeight));
                AnimStatusLabel.Text = $"Frame {_currentAnimFrame + 1} / {frameCount}";
            }
            _currentAnimFrame++;
        }

        private async void RefreshPythonEnvironmentWorkspace()
        {
            try
            {
                if (ProjectInterpreterCombo == null) return;

                var interpreters = PythonService.DetectAvailableInterpreters(_projectPath);
                ProjectInterpreterCombo.ItemsSource = interpreters;
                ProjectInterpreterCombo.DisplayMemberPath = "DisplayName";
                if (interpreters.Count > 0) ProjectInterpreterCombo.SelectedIndex = 0;

                List<string> curatedPackages =
                [
                    .. PipPackageService.GetCuratedGamePackages()
                        .Select(p => $"{p.Name} — {p.Summary}")
                ];
                CuratedPackagesList.ItemsSource = curatedPackages;

                string activePython = PythonService.ResolveProjectPython(_projectPath, _projectConfig.PythonInterpreter);
                var (isValid, missing) = await PythonService.ValidateProjectEnvironmentAsync(_projectPath, _projectConfig.PythonInterpreter);
                string nodeInfo = NodeService.IsNodeInstalled() && NodeService.IsNpmInstalled()
                    ? $"Node {NodeService.GetNodeVersion()} / npm {NodeService.GetNpmVersion()}"
                    : "Optional Node/npm not detected";
                EnvValidationStatusText.ToolTip = nodeInfo;
                EnvValidationStatusText.Text = isValid
                    ? $"Environment validated ({activePython}) — All dependencies installed"
                    : $"Missing dependencies in requirements.txt: {string.Join(", ", missing)}";

                var installed = await PipPackageService.GetInstalledPackagesAsync(activePython);
                List<string> installedPackages = [.. installed.Select(p => $"{p.Name}=={p.Version}")];
                InstalledPackagesList.ItemsSource = installedPackages;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void ProjectInterpreterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProjectInterpreterCombo.SelectedItem is PythonInterpreterInfo info)
            {
                _projectConfig.PythonInterpreter = info.Path;
                _projectConfig.SaveManifest();
                StatusInterpreterText.Text = $"Python: {info.Version}";
            }
        }

        private void OnBrowseInterpreterClicked(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Python Executable (python.exe)|python.exe|All Files (*.*)|*.*",
                Title = "Select Python Interpreter"
            };
            if (dlg.ShowDialog() == true && PythonService.IsPythonInstalled(dlg.FileName))
            {
                string ver = PythonService.GetPythonVersion(dlg.FileName);
                _projectConfig.PythonInterpreter = dlg.FileName;
                _projectConfig.SaveManifest();
                RefreshPythonEnvironmentWorkspace();
                ShowToast($"Selected {ver}");
            }
        }

        private async void OnCreateOrValidateVenvClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                OnBottomTabClicked(TabOutputBtn, e);
                OutputConsole.AppendText("Creating / validating project virtual environment...\n");
                var progress = new Progress<string>(msg => OutputConsole.AppendText(msg + "\n"));
                bool ok = await VenvService.CreateProjectVenvAsync(_projectPath, _projectConfig.PythonInterpreter, progress);
                CheckVenvStatus();
                RefreshPythonEnvironmentWorkspace();
                ShowToast(ok ? "Virtual environment ready" : "Venv setup encountered an error");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void OnRefreshPackagesClicked(object sender, RoutedEventArgs e) => RefreshPythonEnvironmentWorkspace();

        private async void OnInstallTypedPackageClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                string pkg = PackageInstallInput.Text.Trim();
                if (string.IsNullOrEmpty(pkg)) return;

                string pythonExe = PythonService.ResolveProjectPython(_projectPath, _projectConfig.PythonInterpreter);
                OnBottomTabClicked(TabOutputBtn, e);
                var progress = new Progress<string>(msg => OutputConsole.AppendText(msg + "\n"));
                var (ok, output) = await PipPackageService.InstallPackageAsync(pythonExe, pkg, SettingsManager.Instance.CurrentSettings.PipIndexUrl, progress);
                if (ok)
                {
                    await PipPackageService.UpdateRequirementsTxtAsync(_projectPath, pkg);
                    PackageInstallInput.Clear();
                }
                else
                {
                    OutputConsole.AppendText(output + "\n");
                }
                RefreshPythonEnvironmentWorkspace();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void OnCuratedPackageDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (CuratedPackagesList.SelectedItem is string item)
            {
                string pkgName = item.Split('—')[0].Trim();
                PackageInstallInput.Text = pkgName;
            }
        }

        private async void OnUpdateSelectedPackageClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (InstalledPackagesList.SelectedItem is not string item) return;
                string pkgName = item.Split("==")[0].Trim();
                string pythonExe = PythonService.ResolveProjectPython(_projectPath, _projectConfig.PythonInterpreter);
                OnBottomTabClicked(TabOutputBtn, e);
                string details = await PipPackageService.GetPackageDetailsAsync(pythonExe, pkgName);
                OutputConsole.AppendText(details + "\n");
                var progress = new Progress<string>(msg => OutputConsole.AppendText(msg + "\n"));
                await PipPackageService.UpdatePackageAsync(pythonExe, pkgName, progress);
                RefreshPythonEnvironmentWorkspace();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private async void OnUninstallSelectedPackageClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (InstalledPackagesList.SelectedItem is not string item) return;
                string pkgName = item.Split("==")[0].Trim();
                string pythonExe = PythonService.ResolveProjectPython(_projectPath, _projectConfig.PythonInterpreter);
                OnBottomTabClicked(TabOutputBtn, e);
                var progress = new Progress<string>(msg => OutputConsole.AppendText(msg + "\n"));
                await PipPackageService.UninstallPackageAsync(pythonExe, pkgName, progress);
                await PipPackageService.UpdateRequirementsTxtAsync(_projectPath, pkgName, remove: true);
                RefreshPythonEnvironmentWorkspace();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private async void OnInstallAllRequirementsClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                string reqPath = Path.Combine(_projectPath, "requirements.txt");
                string pythonExe = PythonService.ResolveProjectPython(_projectPath, _projectConfig.PythonInterpreter);
                OnBottomTabClicked(TabOutputBtn, e);
                var progress = new Progress<string>(msg => OutputConsole.AppendText(msg + "\n"));
                await PipPackageService.InstallRequirementsFileAsync(pythonExe, reqPath, progress);
                RefreshPythonEnvironmentWorkspace();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void UpdateProfilerMetrics()
        {
            if (ProfilerContainer.Visibility != Visibility.Visible) return;

            var snap = _gameRunner.CaptureMetrics(60.0);
            ProfCpuText.Text = $"{snap.IdeCpuPercent:F1} %";
            ProfIdeMemText.Text = $"{snap.IdeMemoryMb:F1} MB";
            ProfGameMemText.Text = snap.IsGameRunning ? $"{snap.GameMemoryMb:F1} MB (PID {_gameRunner.ActiveProcessId})" : "Not Running";
            ProfFpsText.Text = $"{snap.EstimatedFps:F0} FPS ({snap.FrameTimeMs:F1} ms)";

            _memoryHistory.Add(snap.IdeMemoryMb);
            if (_memoryHistory.Count > 40) _memoryHistory.RemoveAt(0);

            ProfilerHistoryLog.Items.Insert(0, $"[{snap.Timestamp:HH:mm:ss}] CPU: {snap.IdeCpuPercent:F1}% | IDE RAM: {snap.IdeMemoryMb:F1} MB | Game RAM: {snap.GameMemoryMb:F1} MB");
            while (ProfilerHistoryLog.Items.Count > 50)
                ProfilerHistoryLog.Items.RemoveAt(ProfilerHistoryLog.Items.Count - 1);

            DrawProfilerGraph();
        }

        private void DrawProfilerGraph()
        {
            ProfilerGraphCanvas.Children.Clear();
            double w = ProfilerGraphCanvas.ActualWidth > 0 ? ProfilerGraphCanvas.ActualWidth : 600;
            double h = ProfilerGraphCanvas.ActualHeight > 0 ? ProfilerGraphCanvas.ActualHeight : 200;
            if (_memoryHistory.Count < 2) return;

            double max = Math.Max(100, _memoryHistory.Max() * 1.2);
            var poly = new Polyline
            {
                Stroke = new SolidColorBrush(Color.FromRgb(78, 201, 176)),
                StrokeThickness = 2
            };

            for (int i = 0; i < _memoryHistory.Count; i++)
            {
                double x = (i / (double)(_memoryHistory.Count - 1)) * w;
                double y = h - ((_memoryHistory[i] / max) * (h - 20)) - 10;
                poly.Points.Add(new Point(x, Math.Clamp(y, 4, h - 4)));
            }
            ProfilerGraphCanvas.Children.Add(poly);
        }

        private async Task RefreshGitWorkspaceAsync()
        {
            if (GitBranchLabel == null) return;
            if (!GitService.IsGitRepository(_projectPath))
            {
                GitBranchLabel.Text = "Branch: (No Repo)";
                GitStatusList.ItemsSource = null;
                GitHistoryList.ItemsSource = null;
                StatusGitText.Text = "Git: Not initialized";
                return;
            }

            string branch = await GitService.GetCurrentBranchAsync(_projectPath);
            GitBranchLabel.Text = $"Branch: {branch}";
            StatusGitText.Text = $"Git: {branch}";

            var entries = await GitService.GetStatusAsync(_projectPath);
            GitStatusList.ItemsSource = entries;
            GitStatusList.DisplayMemberPath = "DisplayText";
            if (entries.Count > 0)
            {
                GitStatusList.ToolTip = entries[0].DisplayText;
            }

            var commits = await GitService.GetCommitHistoryAsync(_projectPath);
            GitHistoryList.ItemsSource = commits;
        }

        private async void OnGitInitClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                var (_, output) = await GitService.InitRepositoryAsync(_projectPath);
                OutputConsole.AppendText(output + "\n");
                await RefreshGitWorkspaceAsync();
                ShowToast("Initialized Git repository");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private async void OnGitSwitchBranchClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                var branches = await GitService.GetBranchesAsync(_projectPath);
                string defaultBranch = branches.FirstOrDefault() ?? "feature/gameplay";
                string? branchName = await AnimatedModal.ShowInputModalAsync(
                    Window.GetWindow(this),
                    "Switch or Create Git Branch",
                    branches.Count > 0 ? $"Existing branches: {string.Join(", ", branches)}" : "Enter branch name:",
                    defaultBranch);
                if (!string.IsNullOrWhiteSpace(branchName))
                {
                    bool createNew = !branches.Contains(branchName.Trim(), StringComparer.OrdinalIgnoreCase);
                    await GitService.CreateOrSwitchBranchAsync(_projectPath, branchName.Trim(), createNew);
                    await RefreshGitWorkspaceAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private async void OnGitStageAllClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                await GitService.StageAllAsync(_projectPath);
                await RefreshGitWorkspaceAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private async void OnGitStageSelectedClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (GitStatusList.SelectedItem is GitFileStatus entry)
                {
                    await GitService.StageFileAsync(_projectPath, entry.FilePath);
                    await RefreshGitWorkspaceAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private async void OnGitUnstageSelectedClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (GitStatusList.SelectedItem is GitFileStatus entry)
                {
                    await GitService.UnstageFileAsync(_projectPath, entry.FilePath);
                    await RefreshGitWorkspaceAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private async void OnGitCommitClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                string msg = GitCommitMessageBox.Text.Trim();
                var (ok, output) = await GitService.CommitAsync(_projectPath, msg);
                if (ok)
                {
                    GitCommitMessageBox.Clear();
                    ShowToast("Committed changes");
                }
                else
                {
                    OutputConsole.AppendText($"[Git Commit] {output}\n");
                }
                await RefreshGitWorkspaceAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private async void OnGitPullClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                var (_, output) = await GitService.PullAsync(_projectPath);
                OutputConsole.AppendText($"[Git Pull] {output}\n");
                await RefreshGitWorkspaceAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private async void OnGitPushClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                var (_, output) = await GitService.PushAsync(_projectPath);
                OutputConsole.AppendText($"[Git Push] {output}\n");
                await RefreshGitWorkspaceAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private async void OnGitFetchClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                var (_, output) = await GitService.FetchAsync(_projectPath);
                OutputConsole.AppendText($"[Git Fetch] {output}\n");
                await RefreshGitWorkspaceAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private async void OnGitRefreshClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                await RefreshGitWorkspaceAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private async void GitStatusList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (GitStatusList.SelectedItem is GitFileStatus entry)
                {
                    GitDiffTextBox.Text = await GitService.GetFileDiffAsync(_projectPath, entry.FilePath);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private async void OnStartBuildPackageClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                OnBottomTabClicked(TabBuildBtn, e);
                BuildOutputBox.Clear();

                var cfg = CreateBuildConfigFromInputs();
                var progress = new Progress<string>(msg => BuildOutputBox.AppendText(msg + "\n"));
                var (ok, output, exePath) = await BuildPackagingService.BuildGameAsync(cfg, progress);
                if (!ok)
                {
                    BuildOutputBox.AppendText($"\n[Build Failed]\n{output}\n");
                }
                else
                {
                    BuildOutputBox.AppendText($"\n[Build Succeeded] Output: {exePath}\n");
                    ShowToast("Game packaged successfully");
                }
                LoadFiles();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void OnTestPackagedGameClicked(object sender, RoutedEventArgs e)
        {
            var cfg = CreateBuildConfigFromInputs();
            string exePath = Path.Combine(_projectPath, cfg.OutputDirectory, $"{cfg.ApplicationName}.exe");
            if (File.Exists(exePath))
            {
                Process.Start(new ProcessStartInfo { FileName = exePath, WorkingDirectory = Path.GetDirectoryName(exePath), UseShellExecute = true });
            }
            else
            {
                ShowToast("Executable not found. Click 'Package Game Now' first.");
            }
        }

        private void OnCleanBuildClicked(object sender, RoutedEventArgs e)
        {
            OnBottomTabClicked(TabBuildBtn, e);
            BuildPackagingService.CleanBuildArtifacts(_projectPath);
            BuildOutputBox.AppendText("Cleaned build folders.\n");
            LoadFiles();
        }

        private GameBuildConfig CreateBuildConfigFromInputs()
        {
            return new GameBuildConfig
            {
                ProjectPath = _projectPath,
                ApplicationName = BuildAppNameBox.Text.Trim(),
                Version = BuildVersionBox.Text.Trim(),
                StartupScript = _projectConfig.StartupFile,
                OutputDirectory = BuildOutputDirBox.Text.Trim(),
                PythonExecutable = PythonService.ResolveProjectPython(_projectPath, _projectConfig.PythonInterpreter),
                Mode = BuildModeCombo.SelectedIndex == 0 ? BuildMode.Release : BuildMode.Development,
                OneFile = true,
                Windowed = BuildModeCombo.SelectedIndex == 0,
                CleanBeforeBuild = BuildCleanCheck.IsChecked == true
            };
        }

        private async void OnAiActionClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is not Button btn || btn.Tag is not string tag) return;
                if (!Enum.TryParse<AiAssistantTask>(tag, out var task)) return;

                bool consent = AiConsentCheckBox.IsChecked == true;
                SettingsManager.Instance.CurrentSettings.AiConsentGranted = consent;

                string activeCode = (FileTabs.SelectedItem as OpenTabItem)?.Content ?? "";
                string inputContext = task == AiAssistantTask.ExplainError ? OutputConsole.Text : activeCode;

                AiOutputTextBox.Text = "Analyzing Python game context...";
                string response = await AiAssistantService.ExecuteAssistantTaskAsync(
                    task,
                    inputContext,
                    _projectConfig.Framework,
                    SettingsManager.Instance.CurrentSettings.TranslationApiKey,
                    consent);

                AiOutputTextBox.Text = response;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private string _currentTerminalShell = TerminalService.TerminalPowerShell;

        private void InitTerminalShell()
        {
            try
            {
                string preferred = SettingsManager.Instance.CurrentSettings.PreferredTerminal;
                var terminals = TerminalService.DetectAvailableTerminals(SettingsManager.Instance.CurrentSettings.TerminalPath);
                var active = terminals.FirstOrDefault(t => t.DisplayName.Equals(preferred, StringComparison.OrdinalIgnoreCase) && t.IsAvailable)
                             ?? terminals.FirstOrDefault(t => t.IsAvailable)
                             ?? terminals.First();

                _currentTerminalShell = active.DisplayName;

                if (TerminalShellSelector != null)
                {
                    TerminalShellSelector.Items.Clear();
                    foreach (var t in terminals)
                    {
                        var item = new ComboBoxItem
                        {
                            Content = t.IsAvailable ? t.DisplayName : $"{t.DisplayName} (Not Installed)",
                            Tag = t.DisplayName,
                            IsEnabled = t.IsAvailable
                        };
                        TerminalShellSelector.Items.Add(item);
                        if (t.DisplayName.Equals(_currentTerminalShell, StringComparison.OrdinalIgnoreCase))
                            TerminalShellSelector.SelectedItem = item;
                    }
                }

                UpdateTerminalPrompt();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"InitTerminalShell error: {ex.Message}");
            }
        }

        private void TerminalShellSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TerminalShellSelector?.SelectedItem is ComboBoxItem { Tag: string shellName })
            {
                _currentTerminalShell = shellName;
                SettingsManager.Instance.CurrentSettings.PreferredTerminal = shellName;
                SettingsManager.Instance.Save();
                UpdateTerminalPrompt();
                TerminalConsole.AppendText($"\n[Switched to {shellName} shell]\n");
                TerminalConsole.ScrollToEnd();
            }
        }

        private void UpdateTerminalPrompt()
        {
            if (TerminalPromptLabel == null) return;
            if (_currentTerminalShell.Equals(TerminalService.TerminalGitBash, StringComparison.OrdinalIgnoreCase))
                TerminalPromptLabel.Text = "bash$ ";
            else if (_currentTerminalShell.Equals(TerminalService.TerminalCmd, StringComparison.OrdinalIgnoreCase))
                TerminalPromptLabel.Text = "cmd> ";
            else
                TerminalPromptLabel.Text = "PS > ";
        }

        private async void TerminalInput_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.Key != Key.Enter) return;
                string command = TerminalInput.Text.Trim();
                if (string.IsNullOrEmpty(command)) return;

                string promptPrefix = _currentTerminalShell.Equals(TerminalService.TerminalGitBash, StringComparison.OrdinalIgnoreCase)
                    ? $"bash:{Path.GetFileName(_projectPath)}$ {command}\n"
                    : _currentTerminalShell.Equals(TerminalService.TerminalCmd, StringComparison.OrdinalIgnoreCase)
                        ? $"{_projectPath}> {command}\n"
                        : $"PS {_projectPath}> {command}\n";

                TerminalConsole.AppendText(promptPrefix);
                TerminalInput.Clear();

                if (command.Equals("cls", StringComparison.OrdinalIgnoreCase) || command.Equals("clear", StringComparison.OrdinalIgnoreCase))
                {
                    TerminalConsole.Clear();
                    return;
                }

                if (command.Equals("cobra init-backend", StringComparison.OrdinalIgnoreCase))
                {
                    var progress = new Progress<string>(m => TerminalConsole.AppendText(m + "\n"));
                    string backendMsg = await ProjectCreator.AddBackendToProjectAsync(_projectPath, "python", progress);
                    TerminalConsole.AppendText(backendMsg + "\n");
                    LoadFiles();
                    return;
                }

                var (_, stdout, stderr) = await TerminalService.ExecuteCommandAsync(
                    command,
                    _currentTerminalShell,
                    _projectPath,
                    SettingsManager.Instance.CurrentSettings.TerminalPath);

                if (!string.IsNullOrEmpty(stdout))
                    TerminalConsole.AppendText(stdout);
                if (!string.IsNullOrEmpty(stderr))
                    TerminalConsole.AppendText(stderr + "\n");

                TerminalConsole.ScrollToEnd();
            }
            catch (Exception ex)
            {
                TerminalConsole.AppendText($"Terminal error: {ex.Message}\n");
            }
        }

        private void OnTreeViewPreviewRightClick(object sender, MouseButtonEventArgs e)
        {
            DependencyObject? source = e.OriginalSource as DependencyObject;
            while (source != null && source is not TreeViewItem)
            {
                source = VisualTreeHelper.GetParent(source);
            }

            if (source is TreeViewItem item)
            {
                item.IsSelected = true;
                if (FindResource("FileContextMenu") is ContextMenu menu)
                {
                    item.ContextMenu = menu;
                }
            }
        }

        private void OnOpenFileClick(object sender, RoutedEventArgs e)
        {
            if (FileTree.SelectedItem is FileNode { IsDirectory: false } node)
            {
                OpenFileInTab(node.FullPath);
            }
        }

        private void OnNewFileToolbarClick(object sender, RoutedEventArgs e) => CreateNewFileOrFolder(isFolder: false);
        private void OnNewFolderToolbarClick(object sender, RoutedEventArgs e) => CreateNewFileOrFolder(isFolder: true);
        private void OnNewFileClick(object sender, RoutedEventArgs e) => CreateNewFileOrFolder(isFolder: false);
        private void OnNewFolderClick(object sender, RoutedEventArgs e) => CreateNewFileOrFolder(isFolder: true);

        private async void CreateNewFileOrFolder(bool isFolder)
        {
            try
            {
                string targetDir = _projectPath;
                if (FileTree.SelectedItem is FileNode selected)
                {
                    targetDir = selected.IsDirectory ? selected.FullPath : (Path.GetDirectoryName(selected.FullPath) ?? _projectPath);
                }

                var box = new TextBox
                {
                    Text = isFolder ? "new_folder" : "new_script.py",
                    Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                    Foreground = Brushes.White,
                    Padding = new Thickness(6)
                };

                var (confirmed, _) = await AnimatedModal.ShowCustomModalAsync(
                    Window.GetWindow(this),
                    isFolder ? "Create New Folder" : "Create New File",
                    box);

                if (!confirmed) return;

                string name = box.Text.Trim();
                if (!SecurityValidator.IsValidFileName(name, out string nameError))
                {
                    await CustomDialog.ShowAsync(Window.GetWindow(this), nameError, "Validation Error", DialogIcon.Error);
                    return;
                }

                string newPath = Path.Combine(targetDir, name);
                if (!SecurityValidator.IsPathWithinDirectory(newPath, _projectPath)) return;

                if (isFolder)
                {
                    Directory.CreateDirectory(newPath);
                }
                else
                {
                    if (!File.Exists(newPath))
                    {
                        string initialContent = name.EndsWith(".py", StringComparison.OrdinalIgnoreCase)
                            ? "# Cobra Python Script\n"
                            : "";
                        await File.WriteAllTextAsync(newPath, initialContent);
                    }
                    OpenFileInTab(newPath);
                }

                LoadFiles();
                PopulateStartupFiles();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void OnDuplicateFileClick(object sender, RoutedEventArgs e)
        {
            if (FileTree.SelectedItem is not FileNode node || node.IsDirectory || !File.Exists(node.FullPath)) return;

            string dir = Path.GetDirectoryName(node.FullPath) ?? _projectPath;
            string copyName = $"{Path.GetFileNameWithoutExtension(node.FullPath)}_copy{Path.GetExtension(node.FullPath)}";
            string dest = Path.Combine(dir, copyName);
            File.Copy(node.FullPath, dest, overwrite: true);
            LoadFiles();
            ShowToast($"Duplicated as {copyName}");
        }

        private async void OnRenameClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (FileTree.SelectedItem is not FileNode node) return;
                if (node.FullPath.Equals(_projectPath, StringComparison.OrdinalIgnoreCase)) return;

                var box = new TextBox { Text = node.Name, Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)), Foreground = Brushes.White, Padding = new Thickness(6) };
                var (confirmed, _) = await AnimatedModal.ShowCustomModalAsync(Window.GetWindow(this), "Rename Item", box, "Rename");
                if (!confirmed) return;

                string newName = box.Text.Trim();
                if (!SecurityValidator.IsValidFileName(newName, out _)) return;

                string parentDir = Path.GetDirectoryName(node.FullPath) ?? _projectPath;
                string destPath = Path.Combine(parentDir, newName);
                if (!SecurityValidator.IsPathWithinDirectory(destPath, _projectPath)) return;

                if (node.IsDirectory) Directory.Move(node.FullPath, destPath);
                else File.Move(node.FullPath, destPath);

                LoadFiles();
                PopulateStartupFiles();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private async void OnDeleteClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (FileTree.SelectedItem is not FileNode node) return;

                if (!node.IsDirectory && SecurityValidator.IsProtectedProjectFile(_projectPath, node.FullPath))
                {
                    bool force = await CustomDialog.ShowAsync(Window.GetWindow(this),
                        $"'{node.Name}' is a core project file. Are you sure you want to delete it?",
                        "Protected Project File",
                        DialogIcon.Warning,
                        "Delete Anyway",
                        "Keep File");
                    if (!force) return;
                }
                else
                {
                    bool confirm = await CustomDialog.ShowAsync(Window.GetWindow(this),
                        $"Delete '{node.Name}' permanently?",
                        "Confirm Delete",
                        DialogIcon.Warning,
                        "Delete",
                        "Cancel");
                    if (!confirm) return;
                }

                if (node.IsDirectory && Directory.Exists(node.FullPath))
                    Directory.Delete(node.FullPath, recursive: true);
                else if (File.Exists(node.FullPath))
                    File.Delete(node.FullPath);

                LoadFiles();
                PopulateStartupFiles();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void OnSetStartupFileClick(object sender, RoutedEventArgs e)
        {
            if (FileTree.SelectedItem is FileNode { IsDirectory: false } node && node.FullPath.EndsWith(".py", StringComparison.OrdinalIgnoreCase))
            {
                _projectConfig.StartupFile = Path.GetRelativePath(_projectPath, node.FullPath);
                _projectConfig.SaveManifest();
                PopulateStartupFiles();
                ShowToast($"Startup script set to {_projectConfig.StartupFile}");
            }
        }

        private void OnCopyPathClick(object sender, RoutedEventArgs e)
        {
            if (FileTree.SelectedItem is FileNode node)
            {
                Clipboard.SetText(node.FullPath);
                ShowToast("Path copied to clipboard");
            }
        }

        private void OnRevealInExplorerClick(object sender, RoutedEventArgs e)
        {
            string target = (FileTree.SelectedItem as FileNode)?.FullPath ?? _projectPath;
            Process.Start(new ProcessStartInfo { FileName = "explorer.exe", Arguments = $"/select,\"{target}\"", UseShellExecute = true });
        }

        private void OnRefreshClicked(object sender, RoutedEventArgs e)
        {
            LoadFiles();
            PopulateStartupFiles();
            ShowToast("Explorer refreshed");
        }

        private void OnTreeViewDragEnter(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        private void OnTreeViewDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            if (e.Data.GetData(DataFormats.FileDrop) is not string[] files) return;
            foreach (var file in files)
            {
                if (File.Exists(file))
                {
                    AssetService.ImportAsset(_projectPath, file);
                }
            }
            LoadFiles();
            RefreshAssetsWorkspace();
        }

        private void OnAddVenvClicked(object sender, MouseButtonEventArgs e)
        {
            OnCreateOrValidateVenvClicked(sender, new RoutedEventArgs());
        }

        private async void OnInstallRequirementsClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(_selectedRequirementsPath)) return;
                string pythonExe = PythonService.ResolveProjectPython(_projectPath, _projectConfig.PythonInterpreter);
                OnBottomTabClicked(TabOutputBtn, e);
                var progress = new Progress<string>(msg => OutputConsole.AppendText(msg + "\n"));
                await PipPackageService.InstallRequirementsFileAsync(pythonExe, _selectedRequirementsPath, progress);
                RefreshPythonEnvironmentWorkspace();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void OnNewProjectClicked(object sender, RoutedEventArgs e)
        {
            CleanupResources();
            Window.GetWindow(this)?.Close();
        }

        private void OnOpenProjectClicked(object sender, RoutedEventArgs e)
        {
            using var dialog = new FolderBrowserDialog();
            dialog.Description = "Select a Cobra Python Game Project Folder";
            dialog.UseDescriptionForTitle = true;
            if (dialog.ShowDialog() == DialogResult.OK && ProjectManager.IsValidProjectDirectory(dialog.SelectedPath))
            {
                CleanupResources();
                if (Window.GetWindow(this) is { } win)
                {
                    win.Title = $"Cobra IDE - {Path.GetFileName(dialog.SelectedPath)}";
                    win.Content = new EditorView(dialog.SelectedPath);
                }
            }
        }

        private void OnExitClicked(object sender, RoutedEventArgs e)
        {
            CleanupResources();
            Application.Current.Shutdown();
        }

        private void OnPreferencesClicked(object sender, RoutedEventArgs e)
            => NavigationService?.Navigate(new SettingsView());

        private void OnSettingsClicked(object sender, RoutedEventArgs e)
            => NavigationService?.Navigate(new SettingsView());

        private void OnSettingsIconClicked(object sender, RoutedEventArgs e)
            => NavigationService?.Navigate(new SettingsView());

        private void OnProjectSettingsClicked(object sender, RoutedEventArgs e)
            => NavigationService?.Navigate(new SettingsView(new FolderSettings(_projectPath)));

        private void OnToggleFullScreenClicked(object sender, RoutedEventArgs e)
        {
            if (_parentWindow == null) return;
            if (!_isFullScreen)
            {
                _parentWindow.WindowStyle = WindowStyle.None;
                _parentWindow.WindowState = WindowState.Maximized;
                _isFullScreen = true;
            }
            else
            {
                _parentWindow.WindowStyle = WindowStyle.SingleBorderWindow;
                _parentWindow.WindowState = WindowState.Normal;
                _isFullScreen = false;
            }
        }

        private void OnResetLayoutClicked(object sender, RoutedEventArgs e)
        {
            LayoutPresetCombo.SelectedIndex = 0;
            LeftSidebarCol.Width = new GridLength(195);
            RightSidebarCol.Width = new GridLength(285);
            BottomPanelRow.Height = new GridLength(230);
            BottomPanelBorder.Visibility = Visibility.Visible;
            _isBottomPanelCollapsed = false;
            ShowToast("Layout reset to default");
        }

        private void OnRecentProjectsClicked(object sender, RoutedEventArgs e)
            => OnNewProjectClicked(sender, e);

        private void OnBuildClicked(object sender, RoutedEventArgs e)
            => SwitchWorkspace("BuildPackage", NavBuildPackage);

        private void OnExportClicked(object sender, RoutedEventArgs e)
            => SwitchWorkspace("BuildPackage", NavBuildPackage);

        private void OnPackageManagerClicked(object sender, RoutedEventArgs e)
            => SwitchWorkspace("PythonEnv", NavPythonEnv);

        private void OnExtensionsClicked(object sender, RoutedEventArgs e)
            => SwitchWorkspace("Extensions", NavExtensions);

        private void OnOpenTerminalClicked(object sender, RoutedEventArgs e)
        {
            OnBottomTabClicked(TabTerminalBtn, e);
            TerminalInput.Focus();
        }

        private void OnRunCommandClicked(object sender, RoutedEventArgs e)
            => OnOpenTerminalClicked(sender, e);

        private async void OnDocumentationClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                await CustomDialog.ShowAsync(Window.GetWindow(this),
                    "Documentation files in the Docs folder:\n\n" +
                    "• README.md — Quick Start Guide\n" +
                    "• ARCHITECTURE.md — System Design\n" +
                    "• Docs/user_guide.md — User Guide\n" +
                    "• Docs/configuration.md — Settings Guide\n" +
                    "• policies/data_protection.md — Privacy Policy",
                    "Documentation");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private async void OnAboutClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                await CustomDialog.ShowAsync(Window.GetWindow(this),
                    "Cobra IDE v2.0\nPython Game Development Environment\n\n" +
                    "• Language: Python 3.10+\n" +
                    "• 2D Engine: Pygame (pygame-ce)\n" +
                    "• 3D Engines: Panda3D, Ursina\n" +
                    "• Platform: .NET 8, WPF, WebView2",
                    "About Cobra IDE");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private static string StripAnsi(string text)
        {
            return string.IsNullOrEmpty(text) ? text : AnsiRegex().Replace(text, "");
        }

        private async void ShowToast(string message)
        {
            try
            {
                ToastMessage.Text = message;
                ToastOverlay.Visibility = Visibility.Visible;

                var slideIn = new DoubleAnimation(100, 0, TimeSpan.FromMilliseconds(220))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                ToastTranslate.BeginAnimation(TranslateTransform.YProperty, slideIn);

                await Task.Delay(2400);

                var slideOut = new DoubleAnimation(0, 100, TimeSpan.FromMilliseconds(220))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
                };
                slideOut.Completed += (_, _) => ToastOverlay.Visibility = Visibility.Collapsed;
                ToastTranslate.BeginAnimation(TranslateTransform.YProperty, slideOut);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
    }
}
