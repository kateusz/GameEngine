using System.Numerics;

namespace Engine.Renderer.Cameras;

/// <summary>
/// Abstract base class for all camera types in the engine.
/// Every camera has a projection matrix; view matrix source varies by camera type.
/// </summary>
public abstract class Camera
{
    protected Matrix4x4 _projection = Matrix4x4.Identity;

    public virtual Matrix4x4 GetProjectionMatrix() => _projection;
}