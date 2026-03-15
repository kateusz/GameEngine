using System.Numerics;
using Engine.Core;
using Engine.Math;

namespace Engine.Renderer.Cameras;

public class EditorCamera : Camera, IViewCamera
{
    private Vector3 _focalPoint = Vector3.Zero;
    private float _distance = CameraConfig.DefaultEditorDistance;
    private float _pitch;
    private float _yaw;
    private float _fov;
    private float _aspectRatio;
    private float _nearClip;
    private float _farClip;

    private Matrix4x4 _viewMatrix = Matrix4x4.Identity;
    private bool _viewDirty = true;
    private bool _projectionDirty = true;

    private float _viewportWidth = DisplayConfig.DefaultEditorViewportWidth;
    private float _viewportHeight = DisplayConfig.DefaultEditorViewportHeight;

    private Vector2 _previousMousePosition;

    public float Distance => _distance;
    public float FOV => _fov;
    public float Pitch => _pitch;
    public float Yaw => _yaw;
    public Vector3 FocalPoint => _focalPoint;

    public EditorCamera(float fov, float aspectRatio, float nearClip, float farClip)
    {
        _fov = fov;
        _aspectRatio = aspectRatio;
        _nearClip = nearClip;
        _farClip = farClip;
        UpdateProjection();
        UpdateView();
    }

    public EditorCamera() : this(
        CameraConfig.DefaultEditorFOV,
        CameraConfig.DefaultAspectRatio,
        CameraConfig.DefaultEditorNearClip,
        CameraConfig.DefaultEditorFarClip)
    {
    }

    public Quaternion GetOrientation() =>
        Quaternion.CreateFromYawPitchRoll(-_yaw, -_pitch, 0.0f);

    public Vector3 GetForwardDirection() =>
        Vector3.Transform(-Vector3.UnitZ, GetOrientation());

    public Vector3 GetRightDirection() =>
        Vector3.Transform(Vector3.UnitX, GetOrientation());

    public Vector3 GetUpDirection() =>
        Vector3.Transform(Vector3.UnitY, GetOrientation());

    public Vector3 GetPosition() =>
        _focalPoint - GetForwardDirection() * _distance;

    public Matrix4x4 GetViewMatrix()
    {
        if (_viewDirty) UpdateView();
        return _viewMatrix;
    }

    public override Matrix4x4 GetProjectionMatrix()
    {
        if (_projectionDirty) UpdateProjection();
        return _projection;
    }

    public Matrix4x4 GetViewProjectionMatrix() =>
        GetViewMatrix() * GetProjectionMatrix();

    public void SetViewportSize(float width, float height)
    {
        if (width <= 0 || height <= 0) return;
        _viewportWidth = width;
        _viewportHeight = height;
        _aspectRatio = _viewportWidth / _viewportHeight;
        _projectionDirty = true;
    }

    public void SetFocalPoint(Vector3 focalPoint)
    {
        _focalPoint = focalPoint;
        _viewDirty = true;
    }

    public void SetDistance(float distance)
    {
        _distance = System.Math.Clamp(distance, CameraConfig.MinEditorDistance, CameraConfig.MaxEditorDistance);
        _viewDirty = true;
    }

    public void SetPitch(float pitch)
    {
        _pitch = pitch;
        _viewDirty = true;
    }

    public void SetYaw(float yaw)
    {
        _yaw = yaw;
        _viewDirty = true;
    }

    public void Pan(Vector2 delta)
    {
        var (xSpeed, ySpeed) = CalculatePanSpeed();
        _focalPoint += -GetRightDirection() * delta.X * xSpeed * _distance;
        _focalPoint += GetUpDirection() * delta.Y * ySpeed * _distance;
        _viewDirty = true;
    }

    public void Orbit(Vector2 delta)
    {
        var yawSign = GetUpDirection().Y < 0 ? -1.0f : 1.0f;
        _yaw += yawSign * delta.X * CameraConfig.EditorRotationSpeed;
        _pitch += delta.Y * CameraConfig.EditorRotationSpeed;
        _viewDirty = true;
    }

    public void Zoom(float delta)
    {
        _distance -= delta * CalculateZoomSpeed();
        if (_distance < CameraConfig.MinEditorDistance)
        {
            _focalPoint += GetForwardDirection();
            _distance = CameraConfig.MinEditorDistance;
        }
        _distance = MathF.Min(_distance, CameraConfig.MaxEditorDistance);
        _viewDirty = true;
    }

    public void OnMouseScroll(float yOffset)
    {
        Zoom(yOffset * CameraConfig.EditorZoomSensitivity);
    }

    public void OnMouseMove(Vector2 currentMousePosition, bool pan, bool orbit, bool zoomDrag)
    {
        var delta = (currentMousePosition - _previousMousePosition) * CameraConfig.EditorMouseSensitivity;

        if (pan)
            Pan(delta);
        else if (orbit)
            Orbit(delta);
        else if (zoomDrag)
            Zoom(delta.Y);

        _previousMousePosition = currentMousePosition;
    }

    public void SetPreviousMousePosition(Vector2 position)
    {
        _previousMousePosition = position;
    }

    private void UpdateProjection()
    {
        _projection = Matrix4x4.CreatePerspectiveFieldOfView(
            MathHelpers.DegreesToRadians(_fov),
            _aspectRatio,
            _nearClip,
            _farClip);
        _projectionDirty = false;
    }

    private void UpdateView()
    {
        var position = GetPosition();
        var orientation = GetOrientation();

        var transform = Matrix4x4.CreateFromQuaternion(orientation)
                      * Matrix4x4.CreateTranslation(position);

        if (!Matrix4x4.Invert(transform, out _viewMatrix))
            _viewMatrix = Matrix4x4.Identity;

        _viewDirty = false;
    }

    private (float X, float Y) CalculatePanSpeed()
    {
        float x = MathF.Min(_viewportWidth / 1000.0f, 2.4f);
        float xFactor = 0.0366f * x * x - 0.1778f * x + 0.3021f;

        float y = MathF.Min(_viewportHeight / 1000.0f, 2.4f);
        float yFactor = 0.0366f * y * y - 0.1778f * y + 0.3021f;

        return (xFactor, yFactor);
    }

    private float CalculateZoomSpeed()
    {
        float distance = _distance * 0.2f;
        distance = MathF.Max(distance, 0.0f);
        float speed = distance * distance;
        return MathF.Min(speed, 100.0f);
    }
}
