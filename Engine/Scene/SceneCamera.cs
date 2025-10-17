using System.Numerics;
using Engine.Renderer.Cameras;
using Engine.Math;
using Engine.Platform;
using NLog;
using Matrix4x4 = System.Numerics.Matrix4x4;

namespace Engine.Scene;

public enum ProjectionType
{
    Perspective = 0,
    Orthographic = 1
};

public class SceneCamera : Camera
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

    private bool _projectionDirty = true;
    private float _aspectRatio;
    private float _orthographicSize;
    private float _orthographicNear;
    private float _orthographicFar;
    private float _perspectiveFOV = MathHelpers.DegreesToRadians(CameraConfig.DefaultFOV);
    private float _perspectiveNear = CameraConfig.DefaultPerspectiveNear;
    private float _perspectiveFar = CameraConfig.DefaultPerspectiveFar;
    private ProjectionType _projectionType = ProjectionType.Orthographic;

    private Vector3 _cameraPosition = new(0.0f, 0.0f, CameraConfig.DefaultCameraZPosition);
    private Vector3 _cameraFront = new(0.0f, 0.0f, -1.0f);
    private Vector3 _cameraUp = Vector3.UnitY;

    public ProjectionType ProjectionType
    {
        get => _projectionType;
        set
        {
            if (_projectionType != value)
            {
                _projectionType = value;
                _projectionDirty = true;
            }
        }
    }

    public float OrthographicSize
    {
        get => _orthographicSize;
        set
        {
            if (System.Math.Abs(_orthographicSize - value) > float.Epsilon)
            {
                _orthographicSize = value;
                _projectionDirty = true;
            }
        }
    }

    public float OrthographicNear
    {
        get => _orthographicNear;
        set
        {
            if (System.Math.Abs(_orthographicNear - value) > float.Epsilon)
            {
                _orthographicNear = value;
                _projectionDirty = true;
            }
        }
    }

    public float OrthographicFar
    {
        get => _orthographicFar;
        set
        {
            if (System.Math.Abs(_orthographicFar - value) > float.Epsilon)
            {
                _orthographicFar = value;
                _projectionDirty = true;
            }
        }
    }

    public float AspectRatio
    {
        get => _aspectRatio;
        set
        {
            if (System.Math.Abs(_aspectRatio - value) > float.Epsilon)
            {
                _aspectRatio = value;
                _projectionDirty = true;
            }
        }
    }

    public float PerspectiveFOV
    {
        get => _perspectiveFOV;
        set
        {
            if (System.Math.Abs(_perspectiveFOV - value) > float.Epsilon)
            {
                _perspectiveFOV = value;
                _projectionDirty = true;
            }
        }
    }

    public float PerspectiveNear
    {
        get => _perspectiveNear;
        set
        {
            if (System.Math.Abs(_perspectiveNear - value) > float.Epsilon)
            {
                _perspectiveNear = value;
                _projectionDirty = true;
            }
        }
    }

    public float PerspectiveFar
    {
        get => _perspectiveFar;
        set
        {
            if (System.Math.Abs(_perspectiveFar - value) > float.Epsilon)
            {
                _perspectiveFar = value;
                _projectionDirty = true;
            }
        }
    }

    public override Matrix4x4 Projection
    {
        get
        {
            if (_projectionDirty)
            {
                RecalculateProjection();
                _projectionDirty = false;
            }
            return base.Projection;
        }
    }

    public SceneCamera() : base(Matrix4x4.Identity)
    {
        if (OSInfo.IsWindows)
        {
            _orthographicNear = 0.0f;
            _orthographicFar = 1.0f;
        }
        else if (OSInfo.IsMacOS)
        {
            _orthographicNear = CameraConfig.DefaultOrthographicNear;
            _orthographicFar = CameraConfig.DefaultOrthographicFar;
        }

        _projectionDirty = true;
    }

    public void SetOrthographic(float size, float nearClip, float farClip)
    {
        _projectionType = ProjectionType.Orthographic;
        _orthographicSize = size;
        _orthographicNear = nearClip;
        _orthographicFar = farClip;
        _projectionDirty = true;
    }

    public void SetPerspective(float verticalFov, float nearClip, float farClip)
    {
        _projectionType = ProjectionType.Perspective;
        _perspectiveFOV = verticalFov;
        _perspectiveNear = nearClip;
        _perspectiveFar = farClip;
        _projectionDirty = true;
    }

    public void SetViewportSize(uint width, uint height)
    {
        if (width == 0 || height == 0)
        {
            Logger.Warn("[SceneCamera] Invalid viewport size: {Width}x{Height}", width, height);
            return;
        }

        var newAspectRatio = (float)width / (float)height;

        // Validate aspect ratio
        if (float.IsNaN(newAspectRatio) || float.IsInfinity(newAspectRatio))
        {
            Logger.Warn("[SceneCamera] Invalid aspect ratio, using 16:9");
            newAspectRatio = CameraConfig.DefaultAspectRatio;
        }

        AspectRatio = newAspectRatio;
        // _projectionDirty is already set by AspectRatio setter
    }

    public void SetOrthographicSize(float size)
    {
        OrthographicSize = size;
        // _projectionDirty is already set by OrthographicSize setter
    }

    private void RecalculateProjection()
    {
        if (_projectionType == ProjectionType.Perspective)
        {
            var view = Matrix4x4.CreateLookAt(_cameraPosition, _cameraPosition + _cameraFront, _cameraUp);
            base.Projection = view * Matrix4x4.CreatePerspectiveFieldOfView(_perspectiveFOV, _aspectRatio, _perspectiveNear, _perspectiveFar);
        }
        else
        {
            var orthoLeft = -_orthographicSize * _aspectRatio;
            var orthoRight = _orthographicSize * _aspectRatio;
            var orthoBottom = -_orthographicSize;
            var orthoTop = _orthographicSize;

            base.Projection = Matrix4x4.CreateOrthographicOffCenter(orthoLeft, orthoRight, orthoBottom, orthoTop,
                _orthographicNear, _orthographicFar);
        }
    }

    public void SetPerspectiveVerticalFOV(float verticalFov)
    {
        PerspectiveFOV = verticalFov;
        // _projectionDirty is already set by PerspectiveFOV setter
    }

    public void SetPerspectiveNearClip(float nearClip)
    {
        PerspectiveNear = nearClip;
        // _projectionDirty is already set by PerspectiveNear setter
    }

    public void SetPerspectiveFarClip(float farClip)
    {
        PerspectiveFar = farClip;
        // _projectionDirty is already set by PerspectiveFar setter
    }

    public void SetOrthographicNearClip(float nearClip)
    {
        OrthographicNear = nearClip;
        // _projectionDirty is already set by OrthographicNear setter
    }

    public void SetOrthographicFarClip(float farClip)
    {
        OrthographicFar = farClip;
        // _projectionDirty is already set by OrthographicFar setter
    }

    public void SetProjectionType(ProjectionType type)
    {
        ProjectionType = type;
        // _projectionDirty is already set by ProjectionType setter
    }
}