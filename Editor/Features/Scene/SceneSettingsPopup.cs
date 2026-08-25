using Editor.UI.Constants;
using Editor.UI.Drawers;
using ImGuiNET;

namespace Editor.Features.Scene;

/// <summary>
/// Handles scene-related UI popups and modals in the editor.
/// </summary>
public class SceneSettingsPopup(ISceneManager sceneManager)
{
    private bool _showNewScenePopup;
    private bool _showCloseConfirmation;
    private string _newSceneName = string.Empty;
    private string _newSceneError = string.Empty;

    /// <summary>
    /// Shows the new scene popup.
    /// </summary>
    public void ShowNewScenePopup() => _showNewScenePopup = true;

    public void ShowCloseConfirmation() => _showCloseConfirmation = true;

    /// <summary>
    /// Renders all scene-related modals.
    /// Must be called from the main render loop.
    /// </summary>
    public void Render()
    {
        RenderNewScenePopup();
        RenderCloseConfirmationModal();
    }

    private void RenderNewScenePopup()
    {
        var isValid = IsValidSceneName(_newSceneName);
        var validationMessage = (!isValid && !string.IsNullOrEmpty(_newSceneName))
            ? "Scene name must be non-empty and contain only letters, numbers, spaces, dashes, or underscores."
            : null;

        ModalDrawer.RenderInputModal(
            title: "New Scene",
            showModal: ref _showNewScenePopup,
            promptText: "Enter Scene Name:",
            inputValue: ref _newSceneName,
            maxLength: EditorUIConstants.MaxNameLength,
            validationMessage: validationMessage,
            errorMessage: _newSceneError,
            isValid: isValid,
            onOk: () =>
            {
                try
                {
                    // Create new scene
                    sceneManager.New(_newSceneName);
                    _newSceneName = string.Empty;
                    _newSceneError = string.Empty;
                }
                catch (Exception ex)
                {
                    _newSceneError = $"Failed to create scene: {ex.Message}";
                    _showNewScenePopup = true; // Keep modal open on error
                }
            },
            onCancel: () =>
            {
                _newSceneName = string.Empty;
                _newSceneError = string.Empty;
            },
            okLabel: "Create");
    }

    private void RenderCloseConfirmationModal()
    {
        const string title = "Close Scene";
        if (!ModalDrawer.BeginCenteredModal(title, ref _showCloseConfirmation))
            return;

        ImGui.TextWrapped("Save changes to the current scene before closing?");
        ImGui.Separator();

        if (ButtonDrawer.DrawModalButton("Save", () => { sceneManager.Save(); sceneManager.Close(); }))
            _showCloseConfirmation = false;
        ImGui.SameLine();
        if (ButtonDrawer.DrawModalButton("Don't Save", sceneManager.Close))
            _showCloseConfirmation = false;
        ImGui.SameLine();
        if (ButtonDrawer.DrawModalButton("Cancel") || ImGui.IsKeyPressed(ImGuiKey.Escape))
            _showCloseConfirmation = false;

        ModalDrawer.EndModal();
    }

    /// <summary>
    /// Validates a scene name.
    /// </summary>
    private static bool IsValidSceneName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        // Allow letters, numbers, spaces, dashes, and underscores
        return name.All(c => char.IsLetterOrDigit(c) || c == ' ' || c == '-' || c == '_');
    }
}