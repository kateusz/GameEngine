namespace Engine.Core;

/// <summary>
/// Centralized display and window configuration constants.
/// Provides default values for window sizes, framebuffer dimensions, and viewport settings.
/// </summary>
public static class DisplayConfig
{
    // Default window dimensions
    /// <summary>
    /// Default window width in pixels.
    /// Standard 720p HD resolution width.
    /// </summary>
    public const uint DefaultWindowWidth = 1280;

    /// <summary>
    /// Default window height in pixels.
    /// Standard 720p HD resolution height.
    /// </summary>
    public const uint DefaultWindowHeight = 720;

    /// <summary>
    /// Default aspect ratio (16:9 widescreen).
    /// Calculated from DefaultWindowWidth / DefaultWindowHeight.
    /// </summary>
    public const float DefaultAspectRatio = (float)DefaultWindowWidth / DefaultWindowHeight;

    // Editor-specific defaults
    /// <summary>
    /// Default editor viewport/framebuffer width in pixels.
    /// Should match window width for consistent aspect ratio.
    /// </summary>
    public const uint DefaultEditorViewportWidth = DefaultWindowWidth;

    /// <summary>
    /// Default editor viewport/framebuffer height in pixels.
    /// Should match window height for consistent aspect ratio.
    /// </summary>
    public const uint DefaultEditorViewportHeight = DefaultWindowHeight;

    // UI window sizes
    /// <summary>
    /// Standard size for modal popup windows.
    /// </summary>
    public static readonly (float Width, float Height) StandardPopupSize = (600, 400);
}
