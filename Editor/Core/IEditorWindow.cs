namespace Editor.Core;

/// <summary>
/// Interface for floating windows that can optionally dock to panels.
/// Windows are similar to panels but have more complex lifecycle needs (open/close).
/// Examples: Animation Timeline, Recent Projects, Tilemap Editor
/// </summary>
public interface IEditorWindow
{
    /// <summary>
    /// Unique identifier for this window
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Display name shown in title bar
    /// </summary>
    string Title { get; }

    /// <summary>
    /// Whether this window is currently open
    /// </summary>
    bool IsOpen { get; set; }

    /// <summary>
    /// Render the window using ImGui.Begin()/End()
    /// </summary>
    /// <param name="parentDockId">Optional dock ID to dock to on first show</param>
    void OnImGuiRender(uint parentDockId = 0);

    /// <summary>
    /// Optional: Called when window is opened
    /// </summary>
    void OnOpen() { }

    /// <summary>
    /// Optional: Called when window is closed
    /// </summary>
    void OnClose() { }
}
