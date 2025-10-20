using System.Numerics;
using Engine.Renderer.Cameras;
using Engine.Math;
using Engine.Platform;
using Serilog;
using Matrix4x4 = System.Numerics.Matrix4x4;

namespace Engine.Scene;

public enum ProjectionType
{
    Perspective = 0,
    Orthographic = 1
};

public class SceneCamera : Camera
{
    private static readonly Serilog.ILogger Logger = Log.ForContext<SceneCamera>();
    
    private float _aspectRatio;
    private Vector3 _cameraPosition = new(0.0f, 0.0f, CameraConfig.DefaultCameraZPosition);
    private Vector3 _cameraFront = new(0.0f, 0.0f, -1.0f);
    private Vector3 _cameraUp = Vector3.UnitY;
    
    private ProjectionType _projectionType = ProjectionType.Orthographic;
    private float _orthographicSize;
    private float _orthographicNear;
    private float _orthographicFar;
    private float _perspectiveFOV = MathHelpers.DegreesToRadians(CameraConfig.DefaultFOV);
    private float _perspectiveNear = CameraConfig.DefaultPerspectiveNear;
    private float _perspectiveFar = CameraConfig.DefaultPerspectiveFar;
    
    /// <summary>
    /// Gets or sets the projection type (Perspective or Orthographic).
    /// Changing this property triggers a projection matrix recalculation.
    /// </summary>
    public ProjectionType ProjectionType
    {
        get => _projectionType;
        set
        {
            if (_projectionType != value)
            {
                _projectionType = value;
                RecalculateProjection();
            }
        }
    }
    
    /// <summary>
    /// Gets or sets the orthographic size (half-height of the orthographic view volume).
    /// Only triggers recalculation if the projection type is Orthographic and the value has changed.
    /// </summary>
    public float OrthographicSize
    {
        get => _orthographicSize;
        set
        {
            if (System.Math.Abs(_orthographicSize - value) > float.Epsilon)
            {
                _orthographicSize = value;
                if (ProjectionType == ProjectionType.Orthographic)
                    RecalculateProjection();
            }
        }
    }
    
    /// <summary>
    /// Gets or sets the near clip plane for orthographic projection.
    /// Only triggers recalculation if the projection type is Orthographic and the value has changed.
    /// </summary>
    public float OrthographicNear
    {
        get => _orthographicNear;
        set
        {
            if (System.Math.Abs(_orthographicNear - value) > float.Epsilon)
            {
                _orthographicNear = value;
                if (ProjectionType == ProjectionType.Orthographic)
                    RecalculateProjection();
            }
        }
    }
    
    /// <summary>
    /// Gets or sets the far clip plane for orthographic projection.
    /// Only triggers recalculation if the projection type is Orthographic and the value has changed.
    /// </summary>
    public float OrthographicFar
    {
        get => _orthographicFar;
        set
        {
            if (System.Math.Abs(_orthographicFar - value) > float.Epsilon)
            {
                _orthographicFar = value;
                if (ProjectionType == ProjectionType.Orthographic)
                    RecalculateProjection();
            }
        }
    }
    
    /// <summary>
    /// Gets or sets the aspect ratio (width / height).
    /// Changing this property triggers a projection matrix recalculation.
    /// </summary>
    public float AspectRatio
    {
        get => _aspectRatio;
        set
        {
            if (System.Math.Abs(_aspectRatio - value) > float.Epsilon)
            {
                _aspectRatio = value;
                RecalculateProjection();
            }
        }
    }

    /// <summary>
    /// Gets or sets the vertical field of view for perspective projection (in radians).
    /// Only triggers recalculation if the projection type is Perspective and the value has changed.
    /// </summary>
    public float PerspectiveFOV
    {
        get => _perspectiveFOV;
        set
        {
            if (System.Math.Abs(_perspectiveFOV - value) > float.Epsilon)
            {
                _perspectiveFOV = value;
                if (ProjectionType == ProjectionType.Perspective)
                    RecalculateProjection();
            }
        }
    }
    
    /// <summary>
    /// Gets or sets the near clip plane for perspective projection.
    /// Only triggers recalculation if the projection type is Perspective and the value has changed.
    /// </summary>
    public float PerspectiveNear
    {
        get => _perspectiveNear;
        set
        {
            if (System.Math.Abs(_perspectiveNear - value) > float.Epsilon)
            {
                _perspectiveNear = value;
                if (ProjectionType == ProjectionType.Perspective)
                    RecalculateProjection();
            }
        }
    }
    
    /// <summary>
    /// Gets or sets the far clip plane for perspective projection.
    /// Only triggers recalculation if the projection type is Perspective and the value has changed.
    /// </summary>
    public float PerspectiveFar
    {
        get => _perspectiveFar;
        set
        {
            if (System.Math.Abs(_perspectiveFar - value) > float.Epsilon)
            {
                _perspectiveFar = value;
                if (ProjectionType == ProjectionType.Perspective)
                    RecalculateProjection();
            }
        }
    }

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
            Logger.Warning("[SceneCamera] Invalid viewport size: {Width}x{Height}", width, height);
            return;
        }

        AspectRatio = (float)width / (float)height;

        // Validate aspect ratio
        if (float.IsNaN(AspectRatio) || float.IsInfinity(AspectRatio))
        {
            Logger.Warning("[SceneCamera] Invalid aspect ratio, using 16:9");
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

}