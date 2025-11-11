namespace Editor.Core;

/// <summary>
/// Base interface for all dockable editor panels.
/// Panels are persistent UI elements that remain part of the editor layout.
/// Examples: Scene Hierarchy, Properties, Console, Content Browser
/// </summary>
public interface IEditorPanel
{
    /// <summary>
    /// Unique identifier for this panel (used for docking, menu items, and state persistence)
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Display name shown in title bar and menus
    /// </summary>
    string Title { get; }

    /// <summary>
    /// Whether this panel is currently visible
    /// </summary>
    bool IsVisible { get; set; }

    /// <summary>
    /// Render the panel's ImGui content. Called every frame when visible.
    /// Implementation should call ImGui.Begin()/End() to create the window.
    /// </summary>
    void OnImGuiRender();

    /// <summary>
    /// Optional: Called when panel gains focus
    /// </summary>
    void OnFocus() { }

    /// <summary>
    /// Optional: Called when panel loses focus
    /// </summary>
    void OnUnfocus() { }
}
