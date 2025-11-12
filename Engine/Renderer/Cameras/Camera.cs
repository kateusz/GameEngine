using System.Numerics;

namespace Engine.Renderer.Cameras;

/// <summary>
/// Abstract base class for all camera types in the engine.
/// Provides unified interface for projection and view matrices.
/// </summary>
public abstract class Camera
{
    /// <summary>
    /// Gets the projection matrix for this camera.
    /// </summary>
    public abstract Matrix4x4 GetProjectionMatrix();

    /// <summary>
    /// Gets the view matrix for this camera.
    /// </summary>
    public abstract Matrix4x4 GetViewMatrix();
}