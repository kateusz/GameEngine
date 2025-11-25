namespace Engine.Core;

/// <summary>
/// Centralized debug settings for runtime configuration of debug features.
/// Provides toggleable debug visualization options that can be controlled through the editor UI.
/// These settings are synchronized with EditorPreferences for persistence across sessions.
/// </summary>
public class DebugSettings
{
    /// <summary>
    /// Gets or sets whether collider bounds should be visualized.
    /// When changed in editor, this is automatically synchronized with EditorPreferences.
    /// </summary>
    public bool ShowColliderBounds { get; set; } = false;

    /// <summary>
    /// Gets or sets whether FPS counter should be displayed.
    /// When changed in editor, this is automatically synchronized with EditorPreferences.
    /// </summary>
    public bool ShowFPS { get; set; } = true;
}
