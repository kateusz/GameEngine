namespace Editor.Features.Project;

public interface IProjectManager
{
    string? CurrentProjectDirectory { get; }

    string? ScriptsDir { get; }

    string? ScenesDir { get; }

    bool IsProjectLoaded { get; }

    event Action<ProjectPaths>? ProjectOpened;

    event Action? ProjectClosed;

    bool IsValidProjectName(string? name);

    bool TryCreateNewProject(string parentDirectory, string projectName, out string error);

    bool TryOpenProject(string projectDir, out string error);

    void CloseProject();
}
