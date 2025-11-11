namespace Editor.Core;

/// <summary>
/// Interface for modal dialogs and temporary popups.
/// Popups are ephemeral and appear centered over the editor.
/// Examples: New Project, Open Project, Editor Settings
/// </summary>
public interface IEditorPopup
{
    /// <summary>
    /// Unique identifier for this popup
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Whether the popup is currently open
    /// </summary>
    bool IsOpen { get; }

    /// <summary>
    /// Show the popup (can be called multiple times safely)
    /// </summary>
    void Show();

    /// <summary>
    /// Render the popup using ImGui.BeginPopupModal()/EndPopup()
    /// </summary>
    void OnImGuiRender();

    /// <summary>
    /// Optional: Called when popup is closed
    /// </summary>
    void OnClose() { }
}
