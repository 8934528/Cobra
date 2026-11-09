using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Cobra.Core.Security;

namespace Cobra.Core.Settings
{
    public class SettingsModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string _language = "English (US)";
        public string Language
        {
            get => _language;
            set { if (_language != value) { _language = value; OnPropertyChanged(); } }
        }

        private string _startupBehavior = "Show Dashboard";
        public string StartupBehavior
        {
            get => _startupBehavior;
            set { if (_startupBehavior != value) { _startupBehavior = value; OnPropertyChanged(); } }
        }

        private bool _autoSave = true;
        public bool AutoSave
        {
            get => _autoSave;
            set { if (_autoSave != value) { _autoSave = value; OnPropertyChanged(); } }
        }

        private string _autoSaveInterval = "Every 30 seconds";
        public string AutoSaveInterval
        {
            get => _autoSaveInterval;
            set { if (_autoSaveInterval != value) { _autoSaveInterval = value; OnPropertyChanged(); } }
        }

        private bool _checkForUpdates = true;
        public bool CheckForUpdates
        {
            get => _checkForUpdates;
            set { if (_checkForUpdates != value) { _checkForUpdates = value; OnPropertyChanged(); } }
        }

        private bool _telemetry;
        public bool Telemetry
        {
            get => _telemetry;
            set { if (_telemetry != value) { _telemetry = value; OnPropertyChanged(); } }
        }

        private string _theme = "Dark (Default)";
        public string Theme
        {
            get => _theme;
            set { if (_theme != value) { _theme = value; OnPropertyChanged(); } }
        }

        private string _accentColor = "#ff8c00";
        public string AccentColor
        {
            get => _accentColor;
            set { if (_accentColor != value) { _accentColor = value; OnPropertyChanged(); } }
        }

        private string _fontFamily = "Consolas";
        public string FontFamily
        {
            get => _fontFamily;
            set { if (_fontFamily != value) { _fontFamily = value; OnPropertyChanged(); } }
        }

        private double _fontSize = 14;
        public double FontSize
        {
            get => _fontSize;
            set { double clamped = Math.Clamp(value, 10, 32); if (Math.Abs(_fontSize - clamped) > 0.01) { _fontSize = clamped; OnPropertyChanged(); } }
        }

        private double _lineSpacing = 1.4;
        public double LineSpacing
        {
            get => _lineSpacing;
            set { double clamped = Math.Clamp(value, 1.0, 2.5); if (Math.Abs(_lineSpacing - clamped) > 0.01) { _lineSpacing = clamped; OnPropertyChanged(); } }
        }

        private int _tabSize = 4;
        public int TabSize
        {
            get => _tabSize;
            set { int clamped = Math.Clamp(value, 2, 8); if (_tabSize != clamped) { _tabSize = clamped; OnPropertyChanged(); } }
        }

        private bool _showMinimap = true;
        public bool ShowMinimap
        {
            get => _showMinimap;
            set { if (_showMinimap != value) { _showMinimap = value; OnPropertyChanged(); } }
        }

        private bool _wordWrap;
        public bool WordWrap
        {
            get => _wordWrap;
            set { if (_wordWrap != value) { _wordWrap = value; OnPropertyChanged(); } }
        }

        private bool _showLineNumbers = true;
        public bool ShowLineNumbers
        {
            get => _showLineNumbers;
            set { if (_showLineNumbers != value) { _showLineNumbers = value; OnPropertyChanged(); } }
        }

        private bool _formatOnSave = true;
        public bool FormatOnSave
        {
            get => _formatOnSave;
            set { if (_formatOnSave != value) { _formatOnSave = value; OnPropertyChanged(); } }
        }

        private string _pythonInterpreter = "python";
        public string PythonInterpreter
        {
            get => _pythonInterpreter;
            set { if (_pythonInterpreter != value) { _pythonInterpreter = value; OnPropertyChanged(); } }
        }

        private bool _autoCreateVenv = true;
        public bool AutoCreateVenv
        {
            get => _autoCreateVenv;
            set { if (_autoCreateVenv != value) { _autoCreateVenv = value; OnPropertyChanged(); } }
        }

        private string _pipIndexUrl = "https://pypi.org/simple";
        public string PipIndexUrl
        {
            get => _pipIndexUrl;
            set { if (_pipIndexUrl != value) { _pipIndexUrl = value; OnPropertyChanged(); } }
        }

        private string _defaultPackages = "pygame-ce, pymunk, numpy";
        public string DefaultPackages
        {
            get => _defaultPackages;
            set { if (_defaultPackages != value) { _defaultPackages = value; OnPropertyChanged(); } }
        }

        private bool _autoCompletion = true;
        public bool AutoCompletion
        {
            get => _autoCompletion;
            set { if (_autoCompletion != value) { _autoCompletion = value; OnPropertyChanged(); } }
        }

        private bool _linting = true;
        public bool Linting
        {
            get => _linting;
            set { if (_linting != value) { _linting = value; OnPropertyChanged(); } }
        }

        private string _defaultFramework = "Pygame";
        public string DefaultFramework
        {
            get => _defaultFramework;
            set { if (_defaultFramework != value) { _defaultFramework = value; OnPropertyChanged(); } }
        }

        private string _defaultResolution = "800x600";
        public string DefaultResolution
        {
            get => _defaultResolution;
            set { if (_defaultResolution != value) { _defaultResolution = value; OnPropertyChanged(); } }
        }

        private bool _fullscreenDefault;
        public bool FullscreenDefault
        {
            get => _fullscreenDefault;
            set { if (_fullscreenDefault != value) { _fullscreenDefault = value; OnPropertyChanged(); } }
        }

        private bool _vSync = true;
        public bool VSync
        {
            get => _vSync;
            set { if (_vSync != value) { _vSync = value; OnPropertyChanged(); } }
        }

        private double _frameRateLimit = 60;
        public double FrameRateLimit
        {
            get => _frameRateLimit;
            set { double clamped = Math.Clamp(value, 0, 240); if (Math.Abs(_frameRateLimit - clamped) > 0.01) { _frameRateLimit = clamped; OnPropertyChanged(); } }
        }

        private string _physicsEngine = "Pymunk";
        public string PhysicsEngine
        {
            get => _physicsEngine;
            set { if (_physicsEngine != value) { _physicsEngine = value; OnPropertyChanged(); } }
        }

        private bool _showCollisionGizmos = true;
        public bool ShowCollisionGizmos
        {
            get => _showCollisionGizmos;
            set { if (_showCollisionGizmos != value) { _showCollisionGizmos = value; OnPropertyChanged(); } }
        }

        private bool _enableGridSnap = true;
        public bool EnableGridSnap
        {
            get => _enableGridSnap;
            set { if (_enableGridSnap != value) { _enableGridSnap = value; OnPropertyChanged(); } }
        }

        private int _gridSnapSize = 20;
        public int GridSnapSize
        {
            get => _gridSnapSize;
            set { int clamped = Math.Clamp(value, 4, 128); if (_gridSnapSize != clamped) { _gridSnapSize = clamped; OnPropertyChanged(); } }
        }

        private string _terminalPath = "cmd.exe";
        public string TerminalPath
        {
            get => _terminalPath;
            set { if (_terminalPath != value) { _terminalPath = value; OnPropertyChanged(); } }
        }

        private string _externalEditor = "notepad.exe";
        public string ExternalEditor
        {
            get => _externalEditor;
            set { if (_externalEditor != value) { _externalEditor = value; OnPropertyChanged(); } }
        }

        private string _buildTool = "PyInstaller";
        public string BuildTool
        {
            get => _buildTool;
            set { if (_buildTool != value) { _buildTool = value; OnPropertyChanged(); } }
        }

        private string _keymapPreset = "Cobra Default";
        public string KeymapPreset
        {
            get => _keymapPreset;
            set { if (_keymapPreset != value) { _keymapPreset = value; OnPropertyChanged(); } }
        }

        private string _workspaceLayout = "Default";
        public string WorkspaceLayout
        {
            get => _workspaceLayout;
            set { if (_workspaceLayout != value) { _workspaceLayout = value; OnPropertyChanged(); } }
        }

        private bool _aiConsentGranted;
        public bool AiConsentGranted
        {
            get => _aiConsentGranted;
            set { if (_aiConsentGranted != value) { _aiConsentGranted = value; OnPropertyChanged(); } }
        }

        public string EncryptedApiKey { get; set; } = string.Empty;

        [JsonIgnore]
        public string TranslationApiKey
        {
            get => SecurityValidator.UnprotectSecret(EncryptedApiKey);
            set
            {
                EncryptedApiKey = SecurityValidator.ProtectSecret(value);
                OnPropertyChanged();
            }
        }
    }
}
