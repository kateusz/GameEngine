using System.Runtime.InteropServices;
using Engine.Renderer.Cameras;
using System.Text.Json.Serialization;
using Engine.Math;
using Engine.Platform;
using Matrix4x4 = System.Numerics.Matrix4x4;

namespace Engine.Scene;

public enum ProjectionType
{
    Perspective = 0,
    Orthographic = 1
};

public class SceneCamera : Camera
{
    public ProjectionType ProjectionType { get; private set; } = ProjectionType.Orthographic;
    public float OrthographicSize { get; private set; } = 2.0f;
    public float OrthographicNear { get; private set; }
    public float OrthographicFar { get; private set; }
    public float AspectRatio { get; private set; } = 0.0f;

    public float PerspectiveFOV { get; set; } = MathHelpers.DegreesToRadians(45.0f);
    public float PerspectiveNear { get; set; } = 0.01f;
    public float PerspectiveFar { get; set; } = 1000.0f;

    public SceneCamera() : base(Matrix4x4.Identity)
    {
        RecalculateProjection();
        
        if (OSInfo.IsWindows)
        {
            OrthographicNear = -1.0f;
            OrthographicFar = 1.0f;
        }
        else if (OSInfo.IsMacOS)
        {
            OrthographicNear = 0.0f;
            OrthographicFar = 1.0f;
        }
    }

    public void SetOrthographic(float size, float nearClip, float farClip)
    {
        ProjectionType = ProjectionType.Orthographic;
        OrthographicSize = size;
        OrthographicNear = nearClip;
        OrthographicFar = farClip;
        RecalculateProjection();
    }

    public void SetPerspective(float verticalFov, float nearClip, float farClip)
    {
        ProjectionType = ProjectionType.Perspective; 
        PerspectiveFOV = verticalFov;
        PerspectiveNear = nearClip;
        PerspectiveFar = farClip;
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
        if (ProjectionType == ProjectionType.Perspective)
        { 
            Projection =
                Matrix4x4.CreatePerspectiveFieldOfView(PerspectiveFOV, AspectRatio, PerspectiveNear, PerspectiveFar);
        }
        else
        {
            var orthoLeft = -OrthographicSize * AspectRatio;
            var orthoRight = OrthographicSize * AspectRatio;
            var orthoBottom = -OrthographicSize;
            var orthoTop = OrthographicSize;

            Projection = Matrix4x4.CreateOrthographicOffCenter(orthoLeft, orthoRight, orthoBottom, orthoTop,
                OrthographicNear, OrthographicFar);
        }
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

    public void SetPerspectiveVerticalFOV(float verticalFov) { PerspectiveFOV = verticalFov; RecalculateProjection(); }
    public void SetPerspectiveNearClip(float nearClip) { PerspectiveNear = nearClip; RecalculateProjection(); }
    public void SetPerspectiveFarClip(float farClip) { PerspectiveFar = farClip; RecalculateProjection(); }
    public void SetOrthographicNearClip(float nearClip) { OrthographicNear = nearClip; RecalculateProjection(); }
    public void SetOrthographicFarClip(float farClip) { OrthographicFar = farClip; RecalculateProjection(); }
    public void SetProjectionType(ProjectionType type) { ProjectionType = type; RecalculateProjection(); }
}