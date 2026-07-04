using Editor.Features.Scene;
using Editor.Features.Settings;
using Editor.Features.Scripting;
using Engine.Core;
using Engine.Scene;
using Engine.Scripting;
using Serilog;

namespace Editor.Features.Project;

public class ProjectManager(
    IEditorPreferences editorPreferences,
    IProjectContext projectContext,
    GameScriptWorkspace scriptWorkspace,
    IGameProjectScriptBootstrapper gameProjectScriptBootstrapper,
    IScriptEngine scriptEngine,
    ISceneContext sceneContext,
    Lazy<ISceneManager> sceneManager)
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

    public event Action<ProjectPaths>? ProjectOpened;
    public event Action? ProjectClosed;

    public string? CurrentProjectDirectory => projectContext.Root;

    public string? ScriptsDir => projectContext.ScriptsDir;

    public string? ScenesDir => projectContext.ScenesDir;

    public bool IsProjectLoaded => projectContext.HasProject;

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

            CloseProject();
            ApplyProjectPaths(projectDir);
            InitializeScripts();

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
            var full = Path.GetFullPath(Path.IsPathRooted(projectDir)
                ? projectDir
                : Path.Combine(Environment.CurrentDirectory, projectDir));
            if (!Directory.Exists(full))
            {
                error = "Project directory does not exist.";
                editorPreferences.RemoveRecentProject(full);
                return false;
            }

            if (!Directory.Exists(Path.Combine(full, "assets")))
            {
                Logger.Warning("⚠️ 'assets' directory not found. Falling back to project root as assets path.");
            }

            CloseProject();
            ApplyProjectPaths(full);
            InitializeScripts();

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

    public void CloseProject()
    {
        if (!projectContext.HasProject)
            return;

        if (sceneContext.State == SceneState.Play)
            sceneManager.Value.Stop();

        scriptEngine.UnloadGameAssembly();
        projectContext.Clear();
        ProjectClosed?.Invoke();
    }

    private void ApplyProjectPaths(string projectDir)
    {
        projectContext.Apply(projectDir);
        var paths = new ProjectPaths(
            projectContext.Root!,
            projectContext.AssetsPath,
            projectContext.ScriptsDir!,
            projectContext.ScenesDir!);
        ProjectOpened?.Invoke(paths);
    }

    private void InitializeScripts()
    {
        if (projectContext.Root is null || projectContext.ScriptsDir is null)
            return;

        scriptWorkspace.SetScriptsDirectory(
            projectContext.ScriptsDir,
            GameScriptWorkspace.ResolveEditorDllPath(projectContext.Root));
    }
}
