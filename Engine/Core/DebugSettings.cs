namespace Engine.Core;

/// <summary>
/// Centralized debug settings for runtime configuration of debug features.
/// Provides toggleable debug visualization options that can be controlled through the editor UI.
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
    }

    /// <summary>
    /// Gets or sets whether physics debug rendering is enabled.
    /// When enabled, displays wireframe overlays for collision shapes.
    /// </summary>
    public bool ShowPhysicsDebug { get; set; } = false;

    /// <summary>
    /// Gets or sets whether collider bounds should be visualized.
    /// </summary>
    public bool ShowColliderBounds { get; set; } = false;

    /// <summary>
    /// Gets or sets whether FPS counter should be displayed.
    /// </summary>
    public bool ShowFPS { get; set; } = true;
}
