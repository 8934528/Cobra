using System.IO;

namespace Cobra.Core.ProjectSystem
{
    public class ProjectManager
    {
        private readonly string _basePath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);
        private readonly HashSet<string> _hiddenProjects = new(StringComparer.OrdinalIgnoreCase);

        public void HideProject(string path)
        {
            if (!string.IsNullOrWhiteSpace(path))
                _hiddenProjects.Add(path);
        }

        public string GetProjectsPath()
        {
            return Path.Combine(_basePath, "CobraProjects");
        }

        public List<ProjectModel> GetAllProjects()
        {
            List<ProjectModel> projects = [];
            string projectsPath = GetProjectsPath();

            if (!Directory.Exists(projectsPath))
                return projects;

            var dirs = Directory.GetDirectories(projectsPath);
            foreach (var dir in dirs)
            {
                if (IsValidProjectDirectory(dir) && !_hiddenProjects.Contains(dir))
                {
                    projects.Add(ProjectModel.LoadFromDirectory(dir));
                }
            }
            return [.. projects.OrderByDescending(p => p.LastModified)];
        }

        public static bool IsValidProjectDirectory(string dir)
        {
            if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
                return false;

            if (File.Exists(Path.Combine(dir, "cobra.project.json")) || File.Exists(Path.Combine(dir, "main.py")))
                return true;

            return Directory.GetFiles(dir, "*.py").Length > 0;
        }
    }
}
