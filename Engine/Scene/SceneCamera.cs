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
    private Vector3 _cameraPosition = new(0.0f, 0.0f, CameraConfig.DefaultCameraZPosition);
    private Vector3 _cameraFront = new(0.0f, 0.0f, -1.0f);
    private Vector3 _cameraUp = Vector3.UnitY;
    
    public ProjectionType ProjectionType { get; set; } = ProjectionType.Orthographic;
    public float OrthographicSize { get; set; }
    public float OrthographicNear { get; set; }
    public float OrthographicFar { get; set; }
    public float AspectRatio
    {
        get => _aspectRatio;
        set
        {
            _aspectRatio = value;
            RecalculateProjection();
        }
    }

    public float PerspectiveFOV { get; set; } = MathHelpers.DegreesToRadians(CameraConfig.DefaultFOV);
    public float PerspectiveNear { get; set; } = CameraConfig.DefaultPerspectiveNear;
    public float PerspectiveFar { get; set; } = CameraConfig.DefaultPerspectiveFar;

    public SceneCamera() : base(Matrix4x4.Identity)
    {
        if (OSInfo.IsWindows)
        {
            OrthographicNear = 0.0f;
            OrthographicFar = 1.0f;
        }
        else if (OSInfo.IsMacOS)
        {
            OrthographicNear = CameraConfig.DefaultOrthographicNear;
            OrthographicFar = CameraConfig.DefaultOrthographicFar;
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
        if (width == 0 || height == 0)
        {
            Console.WriteLine($"[SceneCamera] Invalid viewport size: {width}x{height}");
            return;
        }

        AspectRatio = (float)width / (float)height;

        // Validate aspect ratio
        if (float.IsNaN(AspectRatio) || float.IsInfinity(AspectRatio))
        {
            Console.WriteLine("[SceneCamera] Invalid aspect ratio, using 16:9");
            AspectRatio = CameraConfig.DefaultAspectRatio;
        }

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