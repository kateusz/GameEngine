using Engine.Renderer.Cameras;
using System.Numerics;

namespace Engine.Scene;

public class SceneCamera : Camera
{
    public float OrthographicSize { get; private set; } = 2.0f;
    public float OrthographicNear { get; private set; } = -1.0f;
    public float OrthographicFar { get; private set; } = 1.0f;
    public float AspectRatio { get; private set; } = 0.0f;

    public SceneCamera() : base(Matrix4x4.Identity)
    {
        RecalculateProjection();
    }

    public void SetOrthographic(float size, float nearClip, float farClip)
    {
        OrthographicSize = size;
        OrthographicNear = nearClip;
        OrthographicFar = farClip;
        RecalculateProjection();
    }

    public void SetViewportSize(uint width, uint height)
    {
        AspectRatio = (float)width / (float)height;
        RecalculateProjection();
    }

    public void SetOrthographicSize(float size)
    {
        OrthographicSize = size;
        RecalculateProjection();
    } 

    private void RecalculateProjection()
    {
        var orthoLeft = -OrthographicSize * AspectRatio;
        var orthoRight = OrthographicSize * AspectRatio;
        var orthoBottom = -OrthographicSize;
        var orthoTop = OrthographicSize;
        
        Projection = Matrix4x4.CreateOrthographicOffCenter(orthoLeft, orthoRight, orthoBottom, orthoTop, OrthographicNear, OrthographicFar);
    }

    private static Matrix4x4 CreateOrthographic(float left, float right, float bottom, float top, float zNear,
        float zFar)
    {
        return new Matrix4x4(
            2.0f / (right - left), 0.0f, 0.0f, -(right + left) / (right - left),
            0.0f, 2.0f / (top - bottom), 0.0f, -(top + bottom) / (top - bottom),
            0.0f, 0.0f, -2.0f / (zFar - zNear), -(zFar + zNear) / (zFar - zNear),
            0.0f, 0.0f, 0.0f, 1.0f
        );
    }
}