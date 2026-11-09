using System.IO;
using Cobra.Core.Environment;
using Cobra.Core.Security;

namespace Cobra.Core.ProjectSystem
{
    public static class ProjectCreator
    {
        public static string CreateProject(string projectName, string templateId = "pygame-2d-starter")
        {
            if (!SecurityValidator.IsValidProjectName(projectName, out string error))
                return $"Error: {error}";

            try
            {
                string basePath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);
                string projectsPath = Path.Combine(basePath, "CobraProjects");

                if (!Directory.Exists(projectsPath))
                    Directory.CreateDirectory(projectsPath);

                string projectPath = Path.Combine(projectsPath, projectName);
                if (Directory.Exists(projectPath))
                    return "Error: Project already exists!";

                return CreateProjectAtPath(projectPath, templateId);
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        public static string CreateProjectAtPath(string projectPath, string templateId = "pygame-2d-starter")
        {
            try
            {
                string fullPath = SecurityValidator.NormalizePath(projectPath);
                string projectName = Path.GetFileName(fullPath);

                if (!SecurityValidator.IsValidProjectName(projectName, out string error))
                    return $"Error: {error}";

                Directory.CreateDirectory(fullPath);
                Directory.CreateDirectory(Path.Combine(fullPath, "assets"));
                Directory.CreateDirectory(Path.Combine(fullPath, "scenes"));
                Directory.CreateDirectory(Path.Combine(fullPath, "scripts"));

                ProjectTemplate.ApplyTemplate(fullPath, templateId);

                var template = ProjectTemplate.GetAvailableTemplates().FirstOrDefault(t => t.Id.Equals(templateId, StringComparison.OrdinalIgnoreCase));
                var model = new ProjectModel
                {
                    Name = projectName,
                    Path = fullPath,
                    Framework = template?.Framework ?? "Pygame",
                    TemplateId = templateId,
                    StartupFile = "main.py"
                };
                model.SaveManifest();

                return $"Project created successfully!\nLocation: {fullPath}";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        public static async Task<string> CreateProjectWithVenvAsync(string projectName, IProgress<string>? progress, string templateId = "pygame-2d-starter")
        {
            if (!SecurityValidator.IsValidProjectName(projectName, out string error))
                return $"Error: {error}";

            try
            {
                string basePath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);
                string projectsPath = Path.Combine(basePath, "CobraProjects");

                if (!Directory.Exists(projectsPath))
                    Directory.CreateDirectory(projectsPath);

                string projectPath = Path.Combine(projectsPath, projectName);
                if (Directory.Exists(projectPath))
                    return "Error: Project already exists!";

                return await CreateProjectWithVenvAtPathAsync(projectPath, progress, templateId);
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        public static async Task<string> CreateProjectWithVenvAtPathAsync(string projectPath, IProgress<string>? progress, string templateId = "pygame-2d-starter")
        {
            try
            {
                string createResult = CreateProjectAtPath(projectPath, templateId);
                if (createResult.StartsWith("Error", StringComparison.OrdinalIgnoreCase))
                    return createResult;

                await VenvService.CopyVenvToProjectAsync(projectPath, progress);

                progress?.Report("Installing project dependencies...");
                await InstallRequirementsAsync(projectPath, progress);

                var model = ProjectModel.LoadFromDirectory(projectPath);
                model.PythonInterpreter = VenvService.GetVenvPythonPath(projectPath) ?? string.Empty;
                model.SaveManifest();

                progress?.Report("Project ready!");
                return $"Project created successfully!\nLocation: {projectPath}";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        private static async Task InstallRequirementsAsync(string projectPath, IProgress<string>? progress = null)
        {
            string? python = VenvService.GetVenvPythonPath(projectPath);
            if (string.IsNullOrEmpty(python)) return;

            string reqFile = Path.Combine(projectPath, "requirements.txt");
            if (File.Exists(reqFile))
            {
                await PipPackageService.InstallRequirementsFileAsync(python, reqFile, progress);
            }
        }

        public static async Task<string> AddBackendToProjectAsync(string projectPath, string backendType, IProgress<string>? progress)
        {
            try
            {
                string backendPath = SecurityValidator.ResolveSafeProjectPath(projectPath, "backend");
                if (Directory.Exists(backendPath))
                    return "Error: backend folder already exists!";

                progress?.Report("Initializing Python backend...");
                ProjectTemplate.ApplyBackendTemplate(backendPath, backendType);
                progress?.Report("Python backend added successfully!");
                await Task.CompletedTask;
                return "Python backend added successfully!";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }
    }
}
