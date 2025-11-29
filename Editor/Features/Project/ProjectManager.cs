using Editor.Features.Settings;
using Engine.Core;
using Engine.Scripting;
using Serilog;

namespace Editor.Features.Project;

public class ProjectManager(
    IEditorPreferences editorPreferences,
    IScriptEngine scriptEngine,
    IAssetsManager assetsManager)
    : IProjectManager
{
    private static readonly ILogger Logger = Log.ForContext<ProjectManager>();

    private static readonly string[] RequiredDirs =
    [
        "assets",
        Path.Combine("assets", "scenes"),
        Path.Combine("assets", "textures"),
        Path.Combine("assets", "scripts"),
        Path.Combine("assets", "prefabs")
    ];

    public string? CurrentProjectDirectory { get; private set; }
    
    public string? ScriptsDir => CurrentProjectDirectory is null
        ? null
        : Path.Combine(CurrentProjectDirectory, "assets", "scripts");

    public string? ScenesDir => CurrentProjectDirectory is null
        ? null
        : Path.Combine(CurrentProjectDirectory, "assets", "scenes");

    public bool IsValidProjectName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) 
            return false;
        
        return System.Text.RegularExpressions.Regex.IsMatch(name, @"^[a-zA-Z0-9_\- ]+$");
    }

    public bool TryCreateNewProject(string projectName, out string error)
    {
        error = string.Empty;

        try
        {
            if (!IsValidProjectName(projectName))
            {
                error =
                    "Project name must be non-empty and contain only letters, numbers, spaces, dashes, or underscores.";
                return false;
            }

            var projectDir = Path.Combine(Environment.CurrentDirectory, projectName.Trim());
            if (Directory.Exists(projectDir))
            {
                error = "A directory with this name already exists.";
                return false;
            }

            Directory.CreateDirectory(projectDir);

            foreach (var rel in RequiredDirs)
                Directory.CreateDirectory(Path.Combine(projectDir, rel));

            SetCurrentProject(projectDir);

            Logger.Information("🆕 Project '{ProjectName}' created at {ProjectDir}", projectName, projectDir);
            editorPreferences.AddRecentProject(projectDir, projectName.Trim());
            return true;
        }
        catch (Exception ex)
        {
            error = $"Failed to create project: {ex.Message}";
            Logger.Error(ex, "Create project failed");
            return false;
        }
    }

    public bool TryOpenProject(string projectDir, out string error)
    {
        error = string.Empty;

        try
        {
            // allow relative input
            var full = Path.GetFullPath(Path.IsPathRooted(projectDir)
                ? projectDir
                : Path.Combine(Environment.CurrentDirectory, projectDir));
            if (!Directory.Exists(full))
            {
                error = "Project directory does not exist.";
                editorPreferences.RemoveRecentProject(full);
                return false;
            }

            // If /assets doesn’t exist, fallback to the root as assets path to keep old samples working.
            if (!Directory.Exists(Path.Combine(full, "assets")))
            {
                Logger.Warning("⚠️ 'assets' directory not found. Falling back to project root as assets path.");
            }

            SetCurrentProject(full);

            Logger.Information("📂 Project opened: {ProjectPath}", full);
            var projectName = Path.GetFileName(full);
            editorPreferences.AddRecentProject(full, projectName);

            return true;
        }
        catch (Exception ex)
        {
            error = $"Failed to open project: {ex.Message}";
            Logger.Error(ex, "Open project failed");
            return false;
        }
    }

    private void SetCurrentProject(string projectDir)
    {
        CurrentProjectDirectory = projectDir;

        // Determine assets root (prefer /assets if present)
        var assetsDir = Directory.Exists(Path.Combine(projectDir, "assets"))
            ? Path.Combine(projectDir, "assets")
            : projectDir;

        assetsManager.SetAssetsPath(assetsDir);

        // Point the scripting engine to /assets/scripts if that exists
        var scriptsDir = Path.Combine(projectDir, "assets", "scripts");
        scriptEngine.SetScriptsDirectory(scriptsDir);
    }
}