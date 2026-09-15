namespace Engine.Scene.Cameras;

/// <summary>
/// Centralized camera configuration constants.
/// Provides default values and limits for all camera systems to ensure consistency.
/// </summary>
public static class CameraConfig
{
    // Perspective defaults
    /// <summary>
    /// Default perspective field of view in degrees.
    /// 45 degrees is a standard FOV that closely matches human vision.
    /// </summary>
    public const float DefaultFOV = 45.0f;

    /// <summary>
    /// Default near clip plane for perspective cameras.
    /// Objects closer than this won't be rendered.
    /// Set to 0.01f to allow very close objects while preventing z-fighting.
    /// </summary>
    public const float DefaultPerspectiveNear = 0.01f;

    /// <summary>
    /// Default far clip plane for perspective cameras.
    /// Objects farther than this won't be rendered.
    /// 1000 units provides good range for most game scenarios.
    /// </summary>
    public const float DefaultPerspectiveFar = 1000.0f;

    // Orthographic defaults
    /// <summary>
    /// Default near clip plane for orthographic cameras.
    /// Set to -100 to provide adequate depth range for 2D scenes with layering.
    /// </summary>
    public const float DefaultOrthographicNear = -100.0f;

    /// <summary>
    /// Default far clip plane for orthographic cameras.
    /// Set to 100 to provide adequate depth range for 2D scenes with layering.
    /// </summary>
    public const float DefaultOrthographicFar = 100.0f;

    /// <summary>
    /// Default orthographic size (half-height in world units).
    /// Determines how much of the world is visible vertically.
    /// </summary>
    public const float DefaultOrthographicSize = 10.0f;

    /// <summary>
    /// Default aspect ratio (16:9 widescreen).
    /// Used as fallback when window dimensions are invalid.
    /// </summary>
    public const float DefaultAspectRatio = 16.0f / 9.0f;

    // Editor camera defaults
    public const float DefaultEditorFOV = 45.0f;
    public const float DefaultEditorDistance = 10.0f;
    public const float DefaultEditorNearClip = 0.1f;
    public const float DefaultEditorFarClip = 1000.0f;
    public const float MinEditorDistance = 0.5f;
    public const float MaxEditorDistance = 500.0f;
    public const float EditorRotationSpeed = 0.8f;
    public const float EditorZoomSensitivity = 0.1f;
    public const float EditorMouseSensitivity = 0.003f;
    public const float EditorFlySpeed = 50.0f;
    public const float DefaultEditorFlySpeedMultiplier = 2.0f;
    public const float MinEditorFlySpeedMultiplier = 0.1f;
    public const float MaxEditorFlySpeedMultiplier = 50.0f;
    public const float EditorFlySpeedScrollStep = 2.0f;
}
