using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shell;
using Cobra.Core.Environment;
using Cobra.Core.ProjectSystem;
using Cobra.Core.Security;
using Cobra.Core.Settings;
using MahApps.Metro.IconPacks;

using Color = System.Windows.Media.Color;
using Brushes = System.Windows.Media.Brushes;
using TextBox = System.Windows.Controls.TextBox;
using Button = System.Windows.Controls.Button;
using CheckBox = System.Windows.Controls.CheckBox;
using ComboBox = System.Windows.Controls.ComboBox;
using ListBox = System.Windows.Controls.ListBox;
using Application = System.Windows.Application;

namespace Cobra.UI.Views
{
    public partial class MainWindow
    {
        private bool _isFullScreen;
        private WindowState _previousState;
        private readonly ProjectManager _projectManager;
        private readonly ObservableCollection<ProjectModel> _recentProjects;

        public string ProjectsPath { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            _projectManager = new ProjectManager();
            _recentProjects = [];
            RecentProjectsList.ItemsSource = _recentProjects;

            ProjectsPath = _projectManager.GetProjectsPath();
            DataContext = this;

            Loaded += MainWindow_Loaded;
            SettingsManager.Instance.SettingsUpdated += (_, _) => ApplySettings();
            ApplySettings();
        }

        private void ApplySettings()
        {
            var settings = SettingsManager.Instance.CurrentSettings;
            Background = settings.Theme == "Light"
                ? new SolidColorBrush(Color.FromRgb(242, 242, 245))
                : new SolidColorBrush(Color.FromRgb(30, 30, 30));
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                PopulateTemplateShowcase();
                CheckPythonStatus();
                LoadProjects();
                await CheckPython();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void PopulateTemplateShowcase()
        {
            if (TemplateShowcasePanel == null) return;
            TemplateShowcasePanel.Children.Clear();

            foreach (var tmpl in ProjectTemplate.GetAvailableTemplates())
            {
                var card = new Border
                {
                    Width = 225,
                    Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(12),
                    Margin = new Thickness(0, 0, 10, 10),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    ToolTip = $"Click to create a new '{tmpl.Name}' project"
                };

                var stack = new StackPanel();
                var headerRow = new Grid();
                headerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                headerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var titleText = new TextBlock
                {
                    Text = tmpl.Name,
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 12,
                    VerticalAlignment = VerticalAlignment.Center
                };
                var badge = new Border
                {
                    Background = tmpl.Category == "3D"
                        ? new SolidColorBrush(Color.FromRgb(78, 110, 180))
                        : new SolidColorBrush(Color.FromRgb(45, 90, 65)),
                    CornerRadius = new CornerRadius(3),
                    Padding = new Thickness(5, 1, 5, 1),
                    Child = new TextBlock
                    {
                        Text = $"{tmpl.Category} • {tmpl.Framework}",
                        Foreground = Brushes.White,
                        FontSize = 10
                    }
                };
                Grid.SetColumn(titleText, 0);
                Grid.SetColumn(badge, 1);
                headerRow.Children.Add(titleText);
                headerRow.Children.Add(badge);

                var descText = new TextBlock
                {
                    Text = tmpl.Description,
                    Foreground = new SolidColorBrush(Color.FromRgb(160, 160, 160)),
                    FontSize = 11,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 6, 0, 0),
                    Height = 34
                };

                stack.Children.Add(headerRow);
                stack.Children.Add(descText);
                card.Child = stack;

                string templateId = tmpl.Id;
                card.MouseLeftButtonDown += async (_, _) =>
                {
                    try
                    {
                        await PromptCreateProjectAsync(templateId);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                    }
                };
                TemplateShowcasePanel.Children.Add(card);
            }
        }

        private void LoadProjects()
        {
            var projects = _projectManager.GetAllProjects();
            _recentProjects.Clear();
            foreach (var project in projects)
                _recentProjects.Add(project);

            ProjectCountText.Text = _recentProjects.Count.ToString();
        }

        private void CheckPythonStatus()
        {
            string configured = SettingsManager.Instance.CurrentSettings.PythonInterpreter;
            if (PythonService.IsPythonInstalled(configured))
            {
                string version = PythonService.GetPythonVersion(configured);
                PythonStatusText.Text = version;
                PythonStatusText.Foreground = new SolidColorBrush(Color.FromRgb(78, 201, 176));
            }
            else
            {
                PythonStatusText.Text = "Python not found in PATH (Configure in Settings)";
                PythonStatusText.Foreground = new SolidColorBrush(Color.FromRgb(220, 53, 69));
            }
        }

        private static async Task CheckPython()
        {
            if (!PythonService.IsPythonInstalled())
            {
                var result = await CustomDialog.ShowAsync(
                    Application.Current.MainWindow,
                    "Python is not installed or not added to PATH.\n\nPlease install Python 3.10+ to run and debug Python games in Cobra IDE.\n\nDo you want to open the official Python download page?",
                    "Python Interpreter Not Found",
                    DialogIcon.Warning,
                    "Download Python",
                    "Later");

                if (result)
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "https://www.python.org/downloads/",
                        UseShellExecute = true
                    });
                }
            }
        }

        public async void OnNewProjectClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                await PromptCreateProjectAsync("pygame-2d-starter");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private async Task PromptCreateProjectAsync(string initialTemplateId)
        {
            var templates = ProjectTemplate.GetAvailableTemplates();
            var mainContentGrid = new Grid();
            for (int i = 0; i < 8; i++)
                mainContentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Template Picker
            var tmplLabel = new TextBlock
            {
                Text = "Game Project Template:",
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 6),
                FontSize = 12,
                FontWeight = FontWeights.SemiBold
            };
            Grid.SetRow(tmplLabel, 0);
            mainContentGrid.Children.Add(tmplLabel);

            var templateCombo = new ComboBox
            {
                Height = 34,
                Margin = new Thickness(0, 0, 0, 10),
                FontSize = 12,
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                Foreground = Brushes.Black
            };
            foreach (var t in templates)
            {
                templateCombo.Items.Add(new ComboBoxItem
                {
                    Content = $"{t.Name} ({t.Category} — {t.Framework})",
                    Tag = t
                });
            }
            templateCombo.SelectedIndex = Math.Max(0, templates.FindIndex(t => t.Id == initialTemplateId));
            Grid.SetRow(templateCombo, 1);
            mainContentGrid.Children.Add(templateCombo);

            var tmplDescText = new TextBlock
            {
                Foreground = new SolidColorBrush(Color.FromRgb(78, 201, 176)),
                FontSize = 11,
                Margin = new Thickness(0, 0, 0, 14),
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetRow(tmplDescText, 2);
            mainContentGrid.Children.Add(tmplDescText);

            void UpdateTemplateDescription()
            {
                if (templateCombo.SelectedItem is ComboBoxItem { Tag: ProjectTemplateDescriptor desc })
                {
                    tmplDescText.Text = $"{desc.Description}  |  Dependencies: {string.Join(", ", desc.Packages)}";
                }
            }
            templateCombo.SelectionChanged += (_, _) => UpdateTemplateDescription();
            UpdateTemplateDescription();

            // Project Name
            var nameLabel = new TextBlock
            {
                Text = "Project Name:",
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 6),
                FontSize = 12,
                FontWeight = FontWeights.SemiBold
            };
            Grid.SetRow(nameLabel, 3);
            mainContentGrid.Children.Add(nameLabel);

            var textBox = new TextBox
            {
                Text = "MyPythonGame",
                Margin = new Thickness(0, 0, 0, 14),
                FontSize = 12,
                Height = 32,
                VerticalContentAlignment = VerticalAlignment.Center,
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(85, 85, 85)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(8, 0, 8, 0)
            };
            Grid.SetRow(textBox, 4);
            mainContentGrid.Children.Add(textBox);

            // Location
            var locationLabel = new TextBlock
            {
                Text = "Project Location:",
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 6),
                FontSize = 12,
                FontWeight = FontWeights.SemiBold
            };
            Grid.SetRow(locationLabel, 5);
            mainContentGrid.Children.Add(locationLabel);

            var locationPanel = new Grid { Margin = new Thickness(0, 0, 0, 12) };
            locationPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            locationPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            Grid.SetRow(locationPanel, 6);

            var locationTextBox = new TextBox
            {
                Margin = new Thickness(0, 0, 8, 0),
                FontSize = 12,
                Height = 32,
                VerticalContentAlignment = VerticalAlignment.Center,
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(85, 85, 85)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(8, 0, 8, 0),
                Text = ProjectsPath
            };
            Grid.SetColumn(locationTextBox, 0);

            var browseBtn = new Button
            {
                Content = "Browse...",
                Width = 85,
                Height = 32,
                Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                Foreground = Brushes.White,
                FontSize = 12,
                Style = (Style)FindResource("CommonRoundedButtonStyle")
            };
            Grid.SetColumn(browseBtn, 1);
            locationPanel.Children.Add(locationTextBox);
            locationPanel.Children.Add(browseBtn);
            mainContentGrid.Children.Add(locationPanel);

            var createVenvCheck = new CheckBox
            {
                Content = "Create isolated Python virtual environment (venv) and install template packages",
                Margin = new Thickness(0, 4, 0, 6),
                Foreground = Brushes.White,
                FontSize = 12,
                IsChecked = SettingsManager.Instance.CurrentSettings.AutoCreateVenv
            };
            Grid.SetRow(createVenvCheck, 7);
            mainContentGrid.Children.Add(createVenvCheck);

            browseBtn.Click += (_, _) =>
            {
                var dialogFolder = new FolderBrowserDialog();
                if (dialogFolder.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    locationTextBox.Text = dialogFolder.SelectedPath;
            };

            var (confirmed, _) = await AnimatedModal.ShowCustomModalAsync(this, "Create New Python Game Project", mainContentGrid, "Create Project");
            if (!confirmed) return;

            string projectName = textBox.Text.Trim();
            string location = locationTextBox.Text.Trim();
            string selectedTemplateId = (templateCombo.SelectedItem as ComboBoxItem)?.Tag is ProjectTemplateDescriptor d ? d.Id : "pygame-2d-starter";
            bool useVenv = createVenvCheck.IsChecked == true;

            if (!SecurityValidator.IsValidProjectName(projectName, out string nameErr))
            {
                await CustomDialog.ShowAsync(this, nameErr, "Invalid Project Name", DialogIcon.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(location))
            {
                await CustomDialog.ShowAsync(this, "Location cannot be empty.", "Error", DialogIcon.Error);
                return;
            }

            string projectPath = Path.Combine(location, projectName);
            if (Directory.Exists(projectPath))
            {
                await CustomDialog.ShowAsync(this, "A project directory already exists at this location.", "Project Exists", DialogIcon.Error);
                return;
            }

            var statusLabel = new TextBlock
            {
                Text = "Creating project...",
                Foreground = Brushes.White,
                FontSize = 12,
                Margin = new Thickness(20)
            };
            var progressWindow = new Window
            {
                Title = "Creating Project",
                Width = 420,
                Height = 110,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                WindowStyle = WindowStyle.None,
                Background = new SolidColorBrush(Color.FromRgb(37, 37, 38)),
                Content = statusLabel
            };
            progressWindow.Show();

            var progress = new Progress<string>(msg => statusLabel.Text = msg);
            bool isDefaultRoot = string.Equals(
                Path.GetFullPath(location).TrimEnd(Path.DirectorySeparatorChar),
                Path.GetFullPath(ProjectsPath).TrimEnd(Path.DirectorySeparatorChar),
                StringComparison.OrdinalIgnoreCase);

            string result;
            if (isDefaultRoot)
            {
                result = useVenv
                    ? await ProjectCreator.CreateProjectWithVenvAsync(projectName, progress, selectedTemplateId)
                    : await Task.Run(() => ProjectCreator.CreateProject(projectName, selectedTemplateId));
            }
            else
            {
                result = useVenv
                    ? await ProjectCreator.CreateProjectWithVenvAtPathAsync(projectPath, progress, selectedTemplateId)
                    : await Task.Run(() => ProjectCreator.CreateProjectAtPath(projectPath, selectedTemplateId));
            }

            progressWindow.Close();

            if (result.StartsWith("Error", StringComparison.OrdinalIgnoreCase))
            {
                await CustomDialog.ShowAsync(this, result, "Creation Error", DialogIcon.Error);
            }
            else
            {
                LoadProjects();
                ShowToast("Project created. Opening editor...");
                OpenProjectEditor(projectPath, projectName);
            }
        }

        public async void OnOpenProjectClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                var projects = _projectManager.GetAllProjects();
                var selectionPanel = new StackPanel();

                selectionPanel.Children.Add(new TextBlock
                {
                    Text = "Select a Cobra Python Game Project:",
                    Foreground = Brushes.White,
                    Margin = new Thickness(0, 0, 0, 10),
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold
                });

                var listBox = new ListBox
                {
                    Height = 220,
                    Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                    Foreground = Brushes.White,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(75, 75, 75)),
                    FontSize = 12
                };

                foreach (var project in projects)
                    listBox.Items.Add($"{project.Name}  [{project.Framework}]  —  {project.Path}");

                selectionPanel.Children.Add(listBox);

                var browseOtherBtn = new Button
                {
                    Content = "Browse Folder on Disk...",
                    Margin = new Thickness(0, 12, 0, 0),
                    Height = 32,
                    Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                    Foreground = Brushes.White,
                    FontSize = 12,
                    Style = (Style)FindResource("CommonRoundedButtonStyle")
                };
                selectionPanel.Children.Add(browseOtherBtn);

                ProjectModel? selectedProject = null;
                listBox.SelectionChanged += (_, _) =>
                {
                    if (listBox.SelectedIndex >= 0 && listBox.SelectedIndex < projects.Count)
                        selectedProject = projects[listBox.SelectedIndex];
                };

                browseOtherBtn.Click += async (_, _) =>
                {
                    try
                    {
                        var folderDialog = new FolderBrowserDialog();
                        if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        {
                            string selectedPath = folderDialog.SelectedPath;
                            if (ProjectManager.IsValidProjectDirectory(selectedPath))
                            {
                                selectedProject = ProjectModel.LoadFromDirectory(selectedPath);
                                var w = Window.GetWindow(browseOtherBtn);
                                w?.Close();
                                OpenProjectEditor(selectedProject.Path, selectedProject.Name);
                            }
                            else
                            {
                                await CustomDialog.ShowAsync(this, "This folder does not contain a Python file (.py) or project file.", "Invalid Folder", DialogIcon.Warning);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                    }
                };

                var (confirmed, _) = await AnimatedModal.ShowCustomModalAsync(this, "Open Game Project", selectionPanel, "Open Project");
                if (confirmed && selectedProject != null)
                {
                    OpenProjectEditor(selectedProject.Path, selectedProject.Name);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void OnRecentProjectDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (RecentProjectsList.SelectedItem is ProjectModel project)
                OpenProjectEditor(project.Path, project.Name);
        }

        private void OnExitClicked(object sender, RoutedEventArgs e) => Application.Current.Shutdown();
        private void OnPreferencesClicked(object sender, RoutedEventArgs e) => OnSettingsIconClicked(sender, e);
        private void OnSettingsClicked(object sender, RoutedEventArgs e) => OnSettingsIconClicked(sender, e);
        private void OnToggleFullScreenClicked(object sender, RoutedEventArgs e) => ToggleFullScreen();

        private void OnResetLayoutClicked(object sender, RoutedEventArgs e)
        {
            SettingsManager.Instance.CurrentSettings.WorkspaceLayout = "Default";
            SettingsManager.Instance.Save();
            ShowToast("Workspace layout reset.");
        }

        private void OnRecentProjectsClicked(object sender, RoutedEventArgs e) => OnOpenProjectClicked(sender, e);

        private async void OnProjectSettingsClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (RecentProjectsList.SelectedItem is ProjectModel project)
                {
                    var folderSettings = new FolderSettings(project.Path);
                    var settingsView = new SettingsView(folderSettings);
                    var settingsWindow = new Window
                    {
                        Title = $"Project Settings - {project.Name}",
                        Content = settingsView,
                        Width = 1150,
                        Height = 760,
                        WindowStartupLocation = WindowStartupLocation.CenterScreen,
                        Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                        WindowStyle = WindowStyle.None
                    };
                    WindowChrome.SetWindowChrome(settingsWindow, new WindowChrome { CaptionHeight = 35, ResizeBorderThickness = new Thickness(5), GlassFrameThickness = new Thickness(0), CornerRadius = new CornerRadius(0) });
                    settingsWindow.ShowDialog();
                }
                else
                {
                    await CustomDialog.ShowAsync(this, "Select a project from the list to change settings.", "Select Project");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private async void OnBuildClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (RecentProjectsList.SelectedItem is ProjectModel project)
                    OpenProjectEditor(project.Path, project.Name);
                else
                    await CustomDialog.ShowAsync(this, "Select or open a project to build.", "Build Game");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void OnExportClicked(object sender, RoutedEventArgs e) => OnBuildClicked(sender, e);
        private void OnPackageManagerClicked(object sender, RoutedEventArgs e) => OnBuildClicked(sender, e);
        private void OnExtensionsClicked(object sender, RoutedEventArgs e) => OnSettingsIconClicked(sender, e);
        private void OnOpenTerminalClicked(object sender, RoutedEventArgs e) => OnBuildClicked(sender, e);
        private void OnRunCommandClicked(object sender, RoutedEventArgs e) => OnBuildClicked(sender, e);

        private void OnSettingsIconClicked(object sender, RoutedEventArgs e)
        {
            var settingsView = new SettingsView();
            var settingsWindow = new Window
            {
                Title = "Cobra IDE - Settings",
                Content = settingsView,
                Width = 1150,
                Height = 760,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                WindowStyle = WindowStyle.None
            };
            WindowChrome.SetWindowChrome(settingsWindow, new WindowChrome { CaptionHeight = 35, ResizeBorderThickness = new Thickness(5), GlassFrameThickness = new Thickness(0), CornerRadius = new CornerRadius(0) });
            settingsWindow.ShowDialog();
        }

        private void OnProjectSelected(object sender, SelectionChangedEventArgs e) { }

        private async void OnOpenSelectedProject(object sender, RoutedEventArgs e)
        {
            try
            {
                if (RecentProjectsList.SelectedItem is ProjectModel project)
                    OpenProjectEditor(project.Path, project.Name);
                else
                    await CustomDialog.ShowAsync(this, "Select a project from the list to open.", "Select Project");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void OnRefreshClicked(object sender, RoutedEventArgs e)
        {
            LoadProjects();
            CheckPythonStatus();
            ShowToast("Projects and Python status refreshed.");
        }

        private async void OnCheckPythonClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                var interpreters = PythonService.DetectAvailableInterpreters();
                string list = interpreters.Count > 0
                    ? string.Join("\n• ", interpreters.Select(i => i.DisplayName))
                    : "No Python interpreters detected.";

                await CustomDialog.ShowAsync(this,
                    $"Installed Python Interpreters:\n\n• {list}",
                    "Python Interpreters");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        public async void OnDocumentationClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                await CustomDialog.ShowAsync(this,
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

        public async void OnAboutClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                await CustomDialog.ShowAsync(this,
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

        private void ToggleFullScreen()
        {
            if (!_isFullScreen)
            {
                _previousState = WindowState;
                WindowStyle = WindowStyle.None;
                WindowState = WindowState.Maximized;
                ResizeMode = ResizeMode.NoResize;
                _isFullScreen = true;
            }
            else
            {
                WindowStyle = WindowStyle.SingleBorderWindow;
                WindowState = _previousState;
                ResizeMode = ResizeMode.CanResize;
                _isFullScreen = false;
            }
        }

        private void ShowToast(string message, bool isSuccess = true)
        {
            ToastMessage.Text = message;
            ToastIcon.Kind = isSuccess ? PackIconMaterialKind.CheckCircle : PackIconMaterialKind.CloseCircle;
            ToastOverlay.Visibility = Visibility.Visible;

            DoubleAnimation slideUp = new()
            {
                From = 100,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            ToastTranslate.BeginAnimation(TranslateTransform.YProperty, slideUp);

            var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
            timer.Tick += (_, _) =>
            {
                ToastOverlay.Visibility = Visibility.Collapsed;
                timer.Stop();
            };
            timer.Start();
        }

        public void OpenProjectEditor(string projectPath, string projectName)
        {
            var editorView = new EditorView(projectPath);
            var editorWindow = new Window
            {
                Title = $"Cobra IDE - {projectName}",
                Content = editorView,
                Width = 1440,
                Height = 900,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                WindowState = WindowState.Maximized,
                WindowStyle = WindowStyle.None
            };
            WindowChrome.SetWindowChrome(editorWindow, new WindowChrome { CaptionHeight = 35, ResizeBorderThickness = new Thickness(5), GlassFrameThickness = new Thickness(0), CornerRadius = new CornerRadius(0) });
            editorWindow.Show();
            Hide();
            editorWindow.Closed += (_, _) =>
            {
                editorView.CleanupResources();
                Show();
                LoadProjects();
            };
        }
    }
}
