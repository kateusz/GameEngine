namespace Engine.Renderer.Cameras;

/// <summary>
/// Centralized camera configuration constants.
/// Provides default values and limits for all camera systems to ensure consistency.
/// </summary>
public static class CameraConfig
{
    // Movement speeds
    /// <summary>
    /// Default translation speed in units per second.
    /// This is the base speed before zoom multipliers are applied.
    /// </summary>
    public const float DefaultTranslationSpeed = 0.5f;
    
    /// <summary>
    /// Default rotation speed in degrees per second.
    /// Used for camera rotation controls (Q/E keys).
    /// </summary>
    public const float DefaultRotationSpeed = 10.0f;
    
    /// <summary>
    /// Fine control multiplier for camera movement.
    /// Applied to translation speed to allow precise control.
    /// Lower values = slower, more precise control.
    /// </summary>
    public const float DefaultSpeedMultiplier = 0.1f;

    // Zoom settings
    /// <summary>
    /// Default zoom level in world units.
    /// Represents the half-height of the orthographic view in world space.
    /// </summary>
    public const float DefaultZoomLevel = 30.0f;
    
    /// <summary>
    /// Zoom sensitivity - how much each scroll wheel tick changes the zoom.
    /// Larger values = faster zoom changes.
    /// </summary>
    public const float ZoomSensitivity = 0.25f;
    
    /// <summary>
    /// Minimum zoom level to prevent zooming to zero or negative values.
    /// Prevents degenerate camera projections.
    /// </summary>
    public const float MinZoomLevel = 0.25f;
    
    /// <summary>
    /// Maximum zoom level to prevent excessive zoom out.
    /// Prevents performance issues and floating-point precision problems.
    /// </summary>
    public const float MaxZoomLevel = 100.0f;

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
    /// OpenGL standard is -1.0, but may vary by platform.
    /// </summary>
    public const float DefaultOrthographicNear = -1.0f;
    
    /// <summary>
    /// Default far clip plane for orthographic cameras.
    /// OpenGL standard is 1.0, but may vary by platform.
    /// </summary>
    public const float DefaultOrthographicFar = 1.0f;
    
    /// <summary>
    /// Default orthographic size (half-height in world units).
    /// Determines how much of the world is visible vertically.
    /// </summary>
    public const float DefaultOrthographicSize = 10.0f;

    // Aspect ratio defaults
    /// <summary>
    /// Default aspect ratio (16:9 widescreen).
    /// Used as fallback when window dimensions are invalid.
    /// </summary>
    public const float DefaultAspectRatio = 16.0f / 9.0f;
    
    /// <summary>
    /// Minimum aspect ratio to prevent extreme portrait orientations.
    /// Allows up to 1:10 portrait aspect ratio.
    /// </summary>
    public const float MinAspectRatio = 0.1f;
    
    /// <summary>
    /// Maximum aspect ratio to prevent extreme landscape orientations.
    /// Allows up to 10:1 landscape aspect ratio.
    /// </summary>
    public const float MaxAspectRatio = 10.0f;

    // Camera position defaults
    /// <summary>
    /// Default camera Z position for 3D perspective cameras.
    /// Positioned 3 units back from origin to have a good view of the scene.
    /// </summary>
    public const float DefaultCameraZPosition = 3.0f;

    // Physics simulation
    /// <summary>
    /// Target physics timestep in seconds (60 FPS).
    /// Used for fixed timestep physics simulation.
    /// </summary>
    public const float PhysicsTimestep = 1.0f / 60.0f;
}
