using Editor.Features.Settings;
using Editor.Features.Scripting;
using Engine.Core;
using Serilog;

namespace Editor.Features.Project;

public class ProjectManager(
    IEditorPreferences editorPreferences,
    GameScriptWorkspace scriptWorkspace,
    IGameProjectScriptBootstrapper gameProjectScriptBootstrapper)
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

    public bool TryCreateNewProject(string parentDirectory, string projectName, out string error)
    {
        error = string.Empty;

        try
        {
            if (string.IsNullOrWhiteSpace(parentDirectory))
            {
                error = "Parent folder path is required.";
                return false;
            }

            var parentFull = Path.GetFullPath(Path.IsPathRooted(parentDirectory.Trim())
                ? parentDirectory.Trim()
                : Path.Combine(Environment.CurrentDirectory, parentDirectory.Trim()));

            if (!Directory.Exists(parentFull))
            {
                error = "Parent folder does not exist.";
                return false;
            }

            if (!IsValidProjectName(projectName))
            {
                error =
                    "Project name must be non-empty and contain only letters, numbers, spaces, dashes, or underscores.";
                return false;
            }

            var projectDir = Path.GetFullPath(Path.Combine(parentFull, projectName.Trim()));
            if (Directory.Exists(projectDir))
            {
                error = "A folder with this name already exists in the selected location.";
                return false;
            }

            Directory.CreateDirectory(projectDir);

            foreach (var rel in RequiredDirs)
                Directory.CreateDirectory(Path.Combine(projectDir, rel));

            if (!gameProjectScriptBootstrapper.TryInstallScriptSdkForNewProject(projectDir, projectName.Trim(),
                    out var sdkError))
            {
                try
                {
                    Directory.Delete(projectDir, recursive: true);
                }
                catch (Exception delEx)
                {
                    Logger.Warning(delEx, "Failed to remove project directory after SDK bootstrap error");
                }

                error = sdkError;
                return false;
            }

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

            gameProjectScriptBootstrapper.TryEnsureScriptSdkAfterOpen(full);

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

        AssetsManager.SetAssetsPath(assetsDir);

        // Point the scripting engine to /assets/scripts if that exists
        var scriptsDir = Path.Combine(projectDir, "assets", "scripts");
        scriptWorkspace.SetScriptsDirectory(scriptsDir, GameScriptWorkspace.ResolveEditorDllPath(projectDir));
    }
}