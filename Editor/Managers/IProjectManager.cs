namespace Editor.Managers;

public interface IProjectManager
{
    /// <summary>Absolute path to the current project directory or null if none.</summary>
    string? CurrentProjectDirectory { get; }

    /// <summary>Absolute path to the project's /assets directory (null if no project).</summary>
    string? AssetsDir { get; }

    /// <summary>Absolute path to /assets/scripts (null if no project).</summary>
    string? ScriptsDir { get; }

    /// <summary>Absolute path to /assets/scenes (null if no project).</summary>
    string? ScenesDir { get; }

    /// <summary>Validate a project name for creation UI.</summary>
    bool IsValidProjectName(string? name);

    /// <summary>Create a new project under the given name in the current working directory.</summary>
    bool TryCreateNewProject(string projectName, out string error);

    /// <summary>Open an existing project directory (absolute or relative to current working directory).</summary>
    bool TryOpenProject(string projectDir, out string error);
}