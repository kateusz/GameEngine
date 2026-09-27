using Editor.UI.Drawers;
using ImGuiNET;

namespace Editor.Features.Scene;

/// <summary>
/// Prompts to save the current scene before a project switch when the scene is dirty.
/// </summary>
public class UnsavedSceneGuard(ISceneManager sceneManager)
{
    private bool _show;
    private Action? _pending;

    /// <summary>
    /// Runs <paramref name="continueAction"/> immediately if the scene is clean;
    /// otherwise shows Save / Don't Save and continues only after a choice.
    /// </summary>
    public void Run(Action continueAction)
    {
        if (!sceneManager.IsDirty)
        {
            continueAction();
            return;
        }

        _pending = continueAction;
        _show = true;
    }

    public void Render()
    {
        if (!_show)
            return;

        if (!ModalDrawer.BeginCenteredModal("Unsaved Scene", ref _show, ModalDrawer.FormModalFlags))
        {
            if (!_show)
                Cancel();
            return;
        }

        ImGui.TextWrapped("Save changes to the current scene before leaving this project?");
        LayoutDrawer.DrawSeparatorWithSpacing();

        ButtonDrawer.DrawCenteredModalButtonRow(
            ("Save", () =>
            {
                sceneManager.Save();
                Complete();
            }),
            ("Don't Save", Complete));

        if (ImGui.IsKeyPressed(ImGuiKey.Escape))
            Cancel();

        ModalDrawer.EndModal();
    }

    private void Complete()
    {
        var action = _pending;
        _pending = null;
        _show = false;
        action?.Invoke();
    }

    private void Cancel()
    {
        _pending = null;
        _show = false;
    }
}
