namespace Engine.Core;

/// <summary>
/// Centralized debug settings for runtime configuration of debug features.
/// Provides toggleable debug visualization options that can be controlled through the editor UI.
/// These settings are synchronized with EditorPreferences for persistence across sessions.
/// </summary>
public class DebugSettings
{
    private static readonly Lazy<DebugSettings> _instance = new(() => new DebugSettings());

    /// <summary>
    /// Gets the singleton instance of DebugSettings.
    /// </summary>
    public static DebugSettings Instance => _instance.Value;

    private DebugSettings()
    {
        // Default values - will be overridden by editor when preferences are loaded
    }

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
