using System.Numerics;

namespace Engine.Renderer.Cameras;

public class Camera(Matrix4x4 projection)
{
    public Matrix4x4 Projection { get; set; } = projection;
}