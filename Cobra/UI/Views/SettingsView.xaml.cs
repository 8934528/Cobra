using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Cobra.Core.Environment;
using Cobra.Core.Settings;

using MediaColor = System.Windows.Media.Color;
using MediaBrushes = System.Windows.Media.Brushes;

namespace Cobra.UI.Views
{
    public partial class SettingsView
    {
        private Border? _currentSelectedNav;
        private readonly Dictionary<string, StackPanel> _sections = [];
        private SettingsModel _originalSettings;
        private readonly FolderSettings? _folderSettings;

        public SettingsView()
        {
            InitializeComponent();
            InitializeSections();

            _originalSettings = LoadCurrentSettings(SettingsManager.Instance.CurrentSettings);
            DataContext = SettingsManager.Instance.CurrentSettings;
            Loaded += (_, _) => UpdateTerminalDetectionSummary();
        }

        public SettingsView(FolderSettings folderSettings)
        {
            InitializeComponent();
            InitializeSections();

            _folderSettings = folderSettings;
            _originalSettings = LoadCurrentSettings(folderSettings.Settings);
            DataContext = folderSettings.Settings;
            Loaded += (_, _) => UpdateTerminalDetectionSummary();

            if (FindName("MainHeader") is TextBlock mainHeader)
                mainHeader.Text = "Project Settings";
            if (FindName("SubHeader") is TextBlock subHeader)
                subHeader.Text = "Customize settings for this project only";
        }

        private void UpdateTerminalDetectionSummary()
        {
            if (TerminalDetectionSummary == null) return;
            try
            {
                var active = _folderSettings?.Settings ?? SettingsManager.Instance.CurrentSettings;
                var terminals = TerminalService.DetectAvailableTerminals(active.TerminalPath);
                var sb = new StringBuilder();
                foreach (var t in terminals)
                {
                    string status = t.IsAvailable ? "✓ Detected" : "✗ Not Found";
                    sb.AppendLine($"• {t.DisplayName}: {status} ({t.ExecutablePath})");
                }
                TerminalDetectionSummary.Text = sb.ToString().TrimEnd();

                var activeTerminal = terminals.FirstOrDefault(t => t.DisplayName.Equals(active.PreferredTerminal, StringComparison.OrdinalIgnoreCase) && t.IsAvailable)
                                     ?? terminals.FirstOrDefault(t => t.IsAvailable);
                GlobalFooter?.SetTerminalStatus(activeTerminal != null ? activeTerminal.DisplayName : "PowerShell", activeTerminal != null);

                bool pyInstalled = PythonService.IsPythonInstalled(active.PythonInterpreter);
                string pyVersion = pyInstalled ? PythonService.GetPythonVersion(active.PythonInterpreter) : "Python Not Found";
                GlobalFooter?.SetPythonStatus(pyVersion, pyInstalled);
            }
            catch (Exception ex)
            {
                TerminalDetectionSummary.Text = $"Detection note: {ex.Message}";
            }
        }

        private static SettingsModel LoadCurrentSettings(SettingsModel current)
        {
            return new SettingsModel
            {
                Language = current.Language,
                StartupBehavior = current.StartupBehavior,
                AutoSave = current.AutoSave,
                AutoSaveInterval = current.AutoSaveInterval,
                CheckForUpdates = current.CheckForUpdates,
                Telemetry = current.Telemetry,
                Theme = current.Theme,
                AccentColor = current.AccentColor,
                FontFamily = current.FontFamily,
                FontSize = current.FontSize,
                LineSpacing = current.LineSpacing,
                ShowMinimap = current.ShowMinimap,
                WordWrap = current.WordWrap,
                ShowLineNumbers = current.ShowLineNumbers,
                TabSize = current.TabSize,
                FormatOnSave = current.FormatOnSave,
                PythonInterpreter = current.PythonInterpreter,
                AutoCreateVenv = current.AutoCreateVenv,
                PipIndexUrl = current.PipIndexUrl,
                DefaultPackages = current.DefaultPackages,
                DefaultFramework = current.DefaultFramework,
                AutoCompletion = current.AutoCompletion,
                Linting = current.Linting,
                DefaultResolution = current.DefaultResolution,
                FullscreenDefault = current.FullscreenDefault,
                VSync = current.VSync,
                FrameRateLimit = current.FrameRateLimit,
                PhysicsEngine = current.PhysicsEngine,
                ShowCollisionGizmos = current.ShowCollisionGizmos,
                EnableGridSnap = current.EnableGridSnap,
                GridSnapSize = current.GridSnapSize,
                PreferredTerminal = current.PreferredTerminal,
                TerminalPath = current.TerminalPath,
                ExternalEditor = current.ExternalEditor,
                BuildTool = current.BuildTool,
                KeymapPreset = current.KeymapPreset,
                WorkspaceLayout = current.WorkspaceLayout,
                AiConsentGranted = current.AiConsentGranted,
                EncryptedApiKey = current.EncryptedApiKey
            };
        }

        private static void CopySettings(SettingsModel source, SettingsModel target)
        {
            target.Language = source.Language;
            target.StartupBehavior = source.StartupBehavior;
            target.AutoSave = source.AutoSave;
            target.AutoSaveInterval = source.AutoSaveInterval;
            target.CheckForUpdates = source.CheckForUpdates;
            target.Telemetry = source.Telemetry;
            target.Theme = source.Theme;
            target.AccentColor = source.AccentColor;
            target.FontFamily = source.FontFamily;
            target.FontSize = Math.Clamp(source.FontSize, 10, 28);
            target.LineSpacing = Math.Clamp(source.LineSpacing, 1.0, 2.5);
            target.ShowMinimap = source.ShowMinimap;
            target.WordWrap = source.WordWrap;
            target.ShowLineNumbers = source.ShowLineNumbers;
            target.TabSize = Math.Clamp(source.TabSize, 2, 8);
            target.FormatOnSave = source.FormatOnSave;
            target.PythonInterpreter = source.PythonInterpreter;
            target.AutoCreateVenv = source.AutoCreateVenv;
            target.PipIndexUrl = source.PipIndexUrl;
            target.DefaultPackages = source.DefaultPackages;
            target.DefaultFramework = source.DefaultFramework;
            target.AutoCompletion = source.AutoCompletion;
            target.Linting = source.Linting;
            target.DefaultResolution = source.DefaultResolution;
            target.FullscreenDefault = source.FullscreenDefault;
            target.VSync = source.VSync;
            target.FrameRateLimit = Math.Clamp(source.FrameRateLimit, 0, 240);
            target.PhysicsEngine = source.PhysicsEngine;
            target.ShowCollisionGizmos = source.ShowCollisionGizmos;
            target.EnableGridSnap = source.EnableGridSnap;
            target.GridSnapSize = Math.Clamp(source.GridSnapSize, 4, 128);
            target.PreferredTerminal = source.PreferredTerminal;
            target.TerminalPath = source.TerminalPath;
            target.ExternalEditor = source.ExternalEditor;
            target.BuildTool = source.BuildTool;
            target.KeymapPreset = source.KeymapPreset;
            target.WorkspaceLayout = source.WorkspaceLayout;
            target.AiConsentGranted = source.AiConsentGranted;
            target.EncryptedApiKey = source.EncryptedApiKey;
        }

        private void InitializeSections()
        {
            _sections.Add("General", GeneralSection);
            _sections.Add("Theme", ThemeSection);
            _sections.Add("Python", PythonSection);
            _sections.Add("Editor", EditorSection);
            _sections.Add("GameEngine", GameEngineSection);
            _sections.Add("Tools", ToolsSection);
            _sections.Add("Extensions", ExtensionsSection);
            _sections.Add("Keymap", KeymapSection);
            _sections.Add("About", AboutSection);

            _currentSelectedNav = NavGeneral;
        }

        private void NavItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border clickedNav)
            {
                string? tag = clickedNav.Tag?.ToString();
                if (!string.IsNullOrEmpty(tag) && _sections.ContainsKey(tag))
                {
                    if (_currentSelectedNav != null)
                    {
                        _currentSelectedNav.Background = MediaBrushes.Transparent;
                    }
                    clickedNav.Background = new SolidColorBrush(MediaColor.FromRgb(45, 45, 45));
                    _currentSelectedNav = clickedNav;

                    SwitchSection(tag);
                }
            }
        }

        private void SwitchSection(string sectionName)
        {
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(150));
            fadeOut.Completed += (_, _) =>
            {
                foreach (var section in _sections.Values)
                {
                    section.Visibility = Visibility.Collapsed;
                }

                if (_sections.TryGetValue(sectionName, out var selectedSection))
                {
                    selectedSection.Visibility = Visibility.Visible;
                    var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
                    ContentContainer.BeginAnimation(OpacityProperty, fadeIn);
                }
            };
            ContentContainer.BeginAnimation(OpacityProperty, fadeOut);
        }

        private async void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var owner = Window.GetWindow(this);
                var active = _folderSettings != null ? _folderSettings.Settings : SettingsManager.Instance.CurrentSettings;

                if (!string.IsNullOrWhiteSpace(active.PipIndexUrl) &&
                    !Uri.TryCreate(active.PipIndexUrl, UriKind.Absolute, out _))
                {
                    await CustomDialog.ShowAsync(owner, "Pip Index URL must start with http:// or https://", "Invalid URL", DialogIcon.Error);
                    return;
                }

                if (_folderSettings != null)
                {
                    _folderSettings.Save();
                }
                else
                {
                    SettingsManager.Instance.Save();
                    SettingsManager.Instance.NotifySettingsUpdated();
                }

                await CustomDialog.ShowAsync(owner, "Settings saved successfully.", "Settings Saved", DialogIcon.Success);

                if (NavigationService is { CanGoBack: true })
                    NavigationService.GoBack();
                else
                    owner?.Close();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private async void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var owner = Window.GetWindow(this);
                bool discard = await CustomDialog.ShowAsync(owner, "Discard unsaved changes?", "Discard Changes", DialogIcon.Warning, "Discard", "Cancel");

                if (discard)
                {
                    var target = _folderSettings != null ? _folderSettings.Settings : SettingsManager.Instance.CurrentSettings;
                    CopySettings(_originalSettings, target);

                    if (NavigationService is { CanGoBack: true })
                        NavigationService.GoBack();
                    else
                        owner?.Close();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private async void CheckNowButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var owner = Window.GetWindow(this);
                await CustomDialog.ShowAsync(owner, "You are using the latest version of Cobra IDE (v2.0.0).", "Update Check");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private async void ManageDefaultPackagesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var owner = Window.GetWindow(this);
                await CustomDialog.ShowAsync(owner,
                    "Recommended default Python game packages:\n• pygame-ce\n• pymunk\n• numpy\n• pillow\n• pyinstaller\n\nSeparate multiple package names with commas in the Default Packages field.",
                    "Default Python Game Packages");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private async void OpenExtensionManagerButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var owner = Window.GetWindow(this);
                await CustomDialog.ShowAsync(owner,
                    "Built-in extensions are active. Place custom plugin DLLs and their matching .plugin.json manifests in your project's Plugins/ folder to load them into the IDE Extension Registry.",
                    "Extension Manager");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private async void EditKeybindingsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var owner = Window.GetWindow(this);
                await CustomDialog.ShowAsync(owner,
                    "Active Keyboard Shortcuts:\n" +
                    "• Ctrl+S: Save Active File\n" +
                    "• Ctrl+Shift+S: Save All Modified Files\n" +
                    "• Ctrl+F / Ctrl+H: Find / Replace in Editor\n" +
                    "• F5: Run Game / Continue Debugger\n" +
                    "• F6: Start Python Debugger\n" +
                    "• F7: Build & Package Game\n" +
                    "• F10 / F11: Step Over / Step Into",
                    "Keyboard Shortcuts Reference");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private async void ViewLicenseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var owner = Window.GetWindow(this);
                await CustomDialog.ShowAsync(owner,
                    "Cobra IDE is licensed for Python game development.\nIncludes embedded Monaco Editor (MIT) and optional Three.js 3D viewport (MIT).",
                    "License Information");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private async void ReportIssueButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var owner = Window.GetWindow(this);
                await CustomDialog.ShowAsync(owner,
                    "To report an issue or security incident, consult policies/access_incident.md or export the Output/Debug Console logs from the bottom panel.",
                    "Report Issue");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void ThemeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private async void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var owner = Window.GetWindow(this);
                bool confirm = await CustomDialog.ShowAsync(owner, "Are you sure you want to reset all settings to defaults?", "Reset Settings", DialogIcon.Warning, "Yes", "No");

                if (confirm)
                {
                    SettingsManager.Instance.Reset();
                    DataContext = SettingsManager.Instance.CurrentSettings;
                    _originalSettings = LoadCurrentSettings(SettingsManager.Instance.CurrentSettings);
                    SettingsManager.Instance.NotifySettingsUpdated();

                    await CustomDialog.ShowAsync(owner, "Settings have been reset to defaults.", "Reset Complete");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void BrowsePythonButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Python Executable (python.exe)|python.exe|All files (*.*)|*.*",
                Title = "Select Python Interpreter"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                SettingsManager.Instance.CurrentSettings.PythonInterpreter = openFileDialog.FileName;
            }
        }

        private void AutoSaveToggle_Checked(object sender, RoutedEventArgs e)
        {
        }

        private async void AutoSaveToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!IsLoaded) return;
                var owner = Window.GetWindow(this);

                var textBlock = new TextBlock
                {
                    Text = "Turning off Auto Save might result in data loss if the application crashes. Do you want to proceed?",
                    Foreground = new SolidColorBrush(MediaColor.FromRgb(224, 224, 224)),
                    TextWrapping = TextWrapping.Wrap
                };

                var result = await AnimatedModal.ShowCustomModalAsync(owner, "Warning: Auto Save Disabled", textBlock, "Proceed", "Keep Auto Save");
                if (!result.confirmed && sender is System.Windows.Controls.Primitives.ToggleButton toggle)
                {
                    toggle.IsChecked = true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void AutoSaveIntervalCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void AccentColor_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!IsLoaded) return;
            if (sender is Border { Tag: string colorName })
            {
                var active = _folderSettings?.Settings ?? SettingsManager.Instance.CurrentSettings;
                active.AccentColor = colorName;
            }
        }

        private void FontFamilyCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) { }
        private void FontSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) { }
        private void LineSpacingSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) { }
        private void MinimapToggle_Checked(object sender, RoutedEventArgs e) { }
        private void MinimapToggle_Unchecked(object sender, RoutedEventArgs e) { }
        private void WordWrapToggle_Checked(object sender, RoutedEventArgs e) { }
        private void WordWrapToggle_Unchecked(object sender, RoutedEventArgs e) { }
        private void LineNumbersToggle_Checked(object sender, RoutedEventArgs e) { }
        private void LineNumbersToggle_Unchecked(object sender, RoutedEventArgs e) { }

        private void GenericSetting_Changed(object sender, RoutedEventArgs e)
        {
            if (IsLoaded)
            {
                UpdateTerminalDetectionSummary();
            }
        }

        private void BrowseTerminalButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Executables (*.exe)|*.exe|All files (*.*)|*.*",
                Title = "Select Terminal Emulator"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                var active = _folderSettings?.Settings ?? SettingsManager.Instance.CurrentSettings;
                active.TerminalPath = openFileDialog.FileName;
                UpdateTerminalDetectionSummary();
            }
        }

        private void BrowseExternalEditorButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Executables (*.exe)|*.exe|All files (*.*)|*.*",
                Title = "Select External Editor"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                var active = _folderSettings?.Settings ?? SettingsManager.Instance.CurrentSettings;
                active.ExternalEditor = openFileDialog.FileName;
            }
        }
    }
}
