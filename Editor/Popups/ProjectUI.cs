using Editor.Popups;

namespace Editor.Panels;

/// <summary>
/// DEPRECATED: This class has been split into NewProjectPopup and OpenProjectPopup.
/// Kept for backward compatibility during migration.
/// </summary>
public class ProjectUI
{
    private readonly NewProjectPopup _newProjectPopup;
    private readonly OpenProjectPopup _openProjectPopup;

    public ProjectUI(NewProjectPopup newProjectPopup, OpenProjectPopup openProjectPopup)
    {
        _newProjectPopup = newProjectPopup;
        _openProjectPopup = openProjectPopup;
    }

    public void ShowNewProjectPopup() => _newProjectPopup.Show();
    public void ShowOpenProjectPopup() => _openProjectPopup.Show();

    public void Render()
    {
        _newProjectPopup.OnImGuiRender();
        _openProjectPopup.OnImGuiRender();
    }
}
