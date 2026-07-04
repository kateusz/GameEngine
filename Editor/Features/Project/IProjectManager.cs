namespace Editor.Features.Project;

public interface IProjectManager
{
    event Action? ProjectOpened;

    event Action? ProjectClosing;

    event Action? ProjectClosed;

    bool IsValidProjectName(string? name);

    bool TryCreateNewProject(string parentDirectory, string projectName, out string error);

    bool TryOpenProject(string projectDir, out string error);

    void CloseProject();
}
