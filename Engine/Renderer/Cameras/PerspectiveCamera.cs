using System.Numerics;
using Engine.Math;

namespace Engine.Renderer.Cameras;

public class PerspectiveCamera : Camera, IViewCamera
{
    private Vector3 _focalPoint = Vector3.Zero;
    private float _distance;
    private float _pitch;
    private float _yaw;
    private float _fov;
    private float _aspectRatio;
    private float _nearClip;
    private float _farClip;

    private Matrix4x4 _viewMatrix = Matrix4x4.Identity;
    private bool _viewDirty = true;
    private bool _projectionDirty = true;

    private Vector2 _previousMousePosition;

    private const float MinDistance = 0.01f;
    private const float MaxDistance = 1_000_000f;
    private const float OrbitSpeed = 0.8f;
    private const float ZoomSensitivity = 0.1f;
    private const float MouseSensitivity = 0.003f;

    public PerspectiveCamera(
        float distance = 100f,
        float fov = 60f,
        float nearClip = 0.1f,
        float farClip = 100_000f)
    {
        _distance = System.Math.Clamp(distance, MinDistance, MaxDistance);
        _fov = fov;
        _aspectRatio = 16f / 9f;
        _nearClip = nearClip;
        _farClip = farClip;
        UpdateProjection();
        UpdateView();
    }

    public Vector3 GetPosition() => _focalPoint - GetForwardDirection() * _distance;

    public Matrix4x4 GetViewProjectionMatrix() => GetViewMatrix() * GetProjectionMatrix();

    public override Matrix4x4 GetProjectionMatrix()
    {
        if (_projectionDirty) UpdateProjection();
        return _projection;
    }

    public void SetViewportSize(float width, float height)
    {
        if (width <= 0 || height <= 0) return;
        _aspectRatio = width / height;
        _projectionDirty = true;
    }

    public void SetFocalPoint(Vector3 point)
    {
        _focalPoint = point;
        _viewDirty = true;
    }

    public void SetDistance(float distance)
    {
        _distance = System.Math.Clamp(distance, MinDistance, MaxDistance);
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

    public void OnMouseScroll(float yOffset)
    {
        _distance -= yOffset * _distance * ZoomSensitivity;
        _distance = System.Math.Clamp(_distance, MinDistance, MaxDistance);
        _viewDirty = true;
    }

    public void OnMouseMove(Vector2 currentPosition, bool pan, bool orbit, bool zoomDrag)
    {
        var delta = (currentPosition - _previousMousePosition) * MouseSensitivity;

        if (pan)
        {
            _focalPoint += -GetRightDirection() * delta.X * _distance;
            _focalPoint += GetUpDirection() * delta.Y * _distance;
            _viewDirty = true;
        }
        else if (orbit)
        {
            var yawSign = GetUpDirection().Y < 0 ? -1f : 1f;
            _yaw += yawSign * delta.X * OrbitSpeed;
            _pitch += delta.Y * OrbitSpeed;
            _viewDirty = true;
        }
        else if (zoomDrag)
        {
            OnMouseScroll(delta.Y);
        }

        _previousMousePosition = currentPosition;
    }

    public void SetPreviousMousePosition(Vector2 position) => _previousMousePosition = position;

    private Quaternion GetOrientation() => Quaternion.CreateFromYawPitchRoll(-_yaw, -_pitch, 0f);
    private Vector3 GetForwardDirection() => Vector3.Transform(-Vector3.UnitZ, GetOrientation());
    private Vector3 GetRightDirection() => Vector3.Transform(Vector3.UnitX, GetOrientation());
    private Vector3 GetUpDirection() => Vector3.Transform(Vector3.UnitY, GetOrientation());

    private Matrix4x4 GetViewMatrix()
    {
        if (_viewDirty) UpdateView();
        return _viewMatrix;
    }

    private void UpdateProjection()
    {
        _projection = Matrix4x4.CreatePerspectiveFieldOfView(
            MathHelpers.DegreesToRadians(_fov), _aspectRatio, _nearClip, _farClip);
        _projectionDirty = false;
    }

    private void UpdateView()
    {
        var position = GetPosition();
        var orientation = GetOrientation();
        var transform = Matrix4x4.CreateFromQuaternion(orientation) * Matrix4x4.CreateTranslation(position);
        if (!Matrix4x4.Invert(transform, out _viewMatrix))
            _viewMatrix = Matrix4x4.Identity;
        _viewDirty = false;
    }
}
