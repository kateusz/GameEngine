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
    private static readonly ILogger Logger = Log.ForContext<SceneCamera>();

    private bool _projectionDirty = true;
    private float _aspectRatio;

    private ProjectionType _projectionType = ProjectionType.Orthographic;
    private float _orthographicSize;
    private float _orthographicNear;
    private float _orthographicFar;
    private float _perspectiveFOV = MathHelpers.DegreesToRadians(CameraConfig.DefaultFOV);
    private float _perspectiveNear = CameraConfig.DefaultPerspectiveNear;
    private float _perspectiveFar = CameraConfig.DefaultPerspectiveFar;

    private Matrix4x4 _projection = Matrix4x4.Identity;

    /// <summary>
    /// Gets the projection matrix, lazily recalculating it only when needed.
    /// The matrix is recalculated only when camera properties change and this property is accessed.
    /// </summary>
    public Matrix4x4 Projection
    {
        get
        {
            if (_projectionDirty)
            {
                RecalculateProjection();
                _projectionDirty = false;
            }
            return _projection;
        }
        protected set => _projection = value;
    }

    /// <summary>
    /// Gets the projection matrix for this camera.
    /// </summary>
    public override Matrix4x4 GetProjectionMatrix() => Projection;

    /// <summary>
    /// Gets the view matrix for this camera.
    /// SceneCamera returns Identity because the view transform is handled
    /// by the camera entity's TransformComponent in the rendering system.
    /// </summary>
    public override Matrix4x4 GetViewMatrix() => Matrix4x4.Identity;

    /// <summary>
    /// Gets or sets the projection type (Perspective or Orthographic).
    /// Marks the projection matrix as needing recalculation.
    /// </summary>
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
    
    /// <summary>
    /// Gets or sets the orthographic size (half-height of the orthographic view volume).
    /// Marks the projection matrix as needing recalculation when changed.
    /// </summary>
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
    
    /// <summary>
    /// Gets or sets the near clip plane for orthographic projection.
    /// Marks the projection matrix as needing recalculation when changed.
    /// </summary>
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
    
    /// <summary>
    /// Gets or sets the far clip plane for orthographic projection.
    /// Marks the projection matrix as needing recalculation when changed.
    /// </summary>
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
    
    /// <summary>
    /// Gets or sets the aspect ratio (width / height).
    /// Marks the projection matrix as needing recalculation when changed.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the vertical field of view for perspective projection (in radians).
    /// Marks the projection matrix as needing recalculation when changed.
    /// </summary>
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
    
    /// <summary>
    /// Gets or sets the near clip plane for perspective projection.
    /// Marks the projection matrix as needing recalculation when changed.
    /// </summary>
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
    
    /// <summary>
    /// Gets or sets the far clip plane for perspective projection.
    /// Marks the projection matrix as needing recalculation when changed.
    /// </summary>
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

    public SceneCamera()
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

        // _projectionDirty is already true by default, projection will be calculated on first access
    }

    /// <summary>
    /// Configures the camera for orthographic projection with multiple parameters.
    /// This method efficiently sets all parameters and marks the projection for recalculation only once.
    /// </summary>
    public void SetOrthographic(float size, float nearClip, float farClip)
    {
        _projectionType = ProjectionType.Orthographic;
        _orthographicSize = size;
        _orthographicNear = nearClip;
        _orthographicFar = farClip;
        _projectionDirty = true;
    }

    /// <summary>
    /// Configures the camera for perspective projection with multiple parameters.
    /// This method efficiently sets all parameters and marks the projection for recalculation only once.
    /// </summary>
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
            Logger.Warning("[SceneCamera] Invalid viewport size: {Width}x{Height}", width, height);
            return;
        }

        var newAspectRatio = (float)width / (float)height;

        // Validate aspect ratio
        if (float.IsNaN(newAspectRatio) || float.IsInfinity(newAspectRatio))
        {
            Logger.Warning("[SceneCamera] Invalid aspect ratio, using 16:9");
            newAspectRatio = CameraConfig.DefaultAspectRatio;
        }

        // Use the AspectRatio property which has change detection
        AspectRatio = newAspectRatio;
    }

    public void SetOrthographicSize(float size)
    {
        // Use the property which has change detection and projection type check
        OrthographicSize = size;
    }

    private void RecalculateProjection()
    {
        if (_projectionType == ProjectionType.Perspective)
        {
            // Only store the projection matrix; view transform is handled by camera entity's TransformComponent
            _projection = Matrix4x4.CreatePerspectiveFieldOfView(_perspectiveFOV, _aspectRatio, _perspectiveNear, _perspectiveFar);
        }
        else
        {
            var orthoLeft = -_orthographicSize * _aspectRatio;
            var orthoRight = _orthographicSize * _aspectRatio;
            var orthoBottom = -_orthographicSize;
            var orthoTop = _orthographicSize;

            _projection = Matrix4x4.CreateOrthographicOffCenter(orthoLeft, orthoRight, orthoBottom, orthoTop,
                _orthographicNear, _orthographicFar);
        }
    }

}