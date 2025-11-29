using Editor.UI.Constants;
using Editor.UI.Drawers;

namespace Editor.Features.Scene;

/// <summary>
/// Handles scene-related UI popups and modals in the editor.
/// </summary>
public class SceneSettingsPopup(ISceneManager sceneManager)
{
    private bool _showNewScenePopup;
    private string _newSceneName = string.Empty;
    private string _newSceneError = string.Empty;

    /// <summary>
    /// Shows the new scene popup.
    /// </summary>
    public void ShowNewScenePopup() => _showNewScenePopup = true;

    /// <summary>
    /// Renders all scene-related modals.
    /// Must be called from the main render loop.
    /// </summary>
    public void Render()
    {
        RenderNewScenePopup();
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
                    sceneManager.New();

                    // TODO: Set scene path when saving is implemented
                    // Scene will be saved when the user explicitly saves

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

    /// <summary>
    /// Validates a scene name.
    /// </summary>
    private bool IsValidSceneName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        // Allow letters, numbers, spaces, dashes, and underscores
        return name.All(c => char.IsLetterOrDigit(c) || c == ' ' || c == '-' || c == '_');
    }
}