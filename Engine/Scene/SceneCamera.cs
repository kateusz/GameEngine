using System.Numerics;
using Engine.Renderer.Cameras;
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
    private float _aspectRatio;
    private Vector3 _cameraPosition = new Vector3(0.0f, 0.0f, 3.0f);
    private Vector3 _cameraFront = new Vector3(0.0f, 0.0f, -1.0f);
    private Vector3 _cameraUp = Vector3.UnitY;
    
    public ProjectionType ProjectionType { get; private set; } = ProjectionType.Orthographic;
    public float OrthographicSize { get; private set; } = 2.0f;
    public float OrthographicNear { get; private set; }
    public float OrthographicFar { get; private set; }
    public float AspectRatio
    {
        get => _aspectRatio;
        set
        {
            _aspectRatio = value;
            RecalculateProjection();
        }
    }

    public float PerspectiveFOV { get; set; } = MathHelpers.DegreesToRadians(45.0f);
    public float PerspectiveNear { get; set; } = 0.01f;
    public float PerspectiveFar { get; set; } = 1000.0f;

    public SceneCamera() : base(Matrix4x4.Identity)
    {
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

        RecalculateProjection();
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
            var view = Matrix4x4.CreateLookAt(_cameraPosition, _cameraPosition + _cameraFront, _cameraUp);
            Projection = view * Matrix4x4.CreatePerspectiveFieldOfView(PerspectiveFOV, AspectRatio, PerspectiveNear, PerspectiveFar);
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

    public void SetPerspectiveVerticalFOV(float verticalFov) { PerspectiveFOV = verticalFov; RecalculateProjection(); }
    public void SetPerspectiveNearClip(float nearClip) { PerspectiveNear = nearClip; RecalculateProjection(); }
    public void SetPerspectiveFarClip(float farClip) { PerspectiveFar = farClip; RecalculateProjection(); }
    public void SetOrthographicNearClip(float nearClip) { OrthographicNear = nearClip; RecalculateProjection(); }
    public void SetOrthographicFarClip(float farClip) { OrthographicFar = farClip; RecalculateProjection(); }
    public void SetProjectionType(ProjectionType type) { ProjectionType = type; RecalculateProjection(); }
}