using System.Numerics;

namespace Engine.Renderer.Cameras;

public class Camera(Matrix4x4 projection)
{
    public virtual Matrix4x4 Projection { get; protected set; } = projection;
}