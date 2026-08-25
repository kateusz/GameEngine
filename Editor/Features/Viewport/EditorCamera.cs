using System.Numerics;
using Engine.Core;
using Engine.Scene.Cameras;
using Math;

namespace Editor.Features.Viewport;

public class EditorCamera : Camera, IViewCamera
{
    private float _aspectRatio;
    private readonly float _nearClip;
    private readonly float _farClip;

    private Matrix4x4 _viewMatrix = Matrix4x4.Identity;
    private bool _viewDirty = true;
    private bool _projectionDirty = true;

    private float _viewportWidth = DisplayConfig.DefaultEditorViewportWidth;
    private float _viewportHeight = DisplayConfig.DefaultEditorViewportHeight;

    private Vector2 _previousMousePosition;

    public float Distance { get; private set; } = CameraConfig.DefaultEditorDistance;

    public float FlySpeedMultiplier { get; private set; } = CameraConfig.DefaultEditorFlySpeedMultiplier;

    public float FOV { get; }

    public float Pitch { get; private set; }

    public float Yaw { get; private set; }

    public Vector3 FocalPoint { get; private set; } = Vector3.Zero;

    public EditorCamera(float fov, float aspectRatio, float nearClip, float farClip)
    {
        FOV = fov;
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
        Quaternion.CreateFromYawPitchRoll(-Yaw, -Pitch, 0.0f);

    public Vector3 GetForwardDirection() =>
        Vector3.Transform(-Vector3.UnitZ, GetOrientation());

    public Vector3 GetRightDirection() =>
        Vector3.Transform(Vector3.UnitX, GetOrientation());

    public Vector3 GetUpDirection() =>
        Vector3.Transform(Vector3.UnitY, GetOrientation());

    public Vector3 GetPosition() =>
        FocalPoint - GetForwardDirection() * Distance;

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
        FocalPoint = focalPoint;
        _viewDirty = true;
    }

    public void SetDistance(float distance)
    {
        Distance = System.Math.Clamp(distance, CameraConfig.MinEditorDistance, CameraConfig.MaxEditorDistance);
        _viewDirty = true;
    }

    public void SetPitch(float pitch)
    {
        Pitch = pitch;
        _viewDirty = true;
    }

    public void SetYaw(float yaw)
    {
        Yaw = yaw;
        _viewDirty = true;
    }

    public void Pan(Vector2 delta)
    {
        var (xSpeed, ySpeed) = CalculatePanSpeed();
        FocalPoint += -GetRightDirection() * delta.X * xSpeed * Distance;
        FocalPoint += GetUpDirection() * delta.Y * ySpeed * Distance;
        _viewDirty = true;
    }

    public void Orbit(Vector2 delta)
    {
        ApplyRotation(delta);
        _viewDirty = true;
    }

    public void Look(Vector2 delta)
    {
        var eye = GetPosition();
        ApplyRotation(delta);
        FocalPoint = eye + GetForwardDirection() * Distance;
        _viewDirty = true;
    }

    public void Fly(Vector3 move, float dt)
    {
        if (move == Vector3.Zero)
            return;

        var speed = CameraConfig.EditorFlySpeed * FlySpeedMultiplier * dt;
        FocalPoint += GetForwardDirection() * move.Z * speed;
        FocalPoint += GetRightDirection() * move.X * speed;
        FocalPoint += Vector3.UnitY * move.Y * speed;
        _viewDirty = true;
    }

    public void Slide(Vector2 delta)
    {
        var (xSpeed, ySpeed) = CalculatePanSpeed();
        FocalPoint += GetRightDirection() * delta.X * xSpeed * Distance;
        FocalPoint += GetForwardDirection() * delta.Y * ySpeed * Distance;
        _viewDirty = true;
    }

    public void AdjustFlySpeed(float scrollDelta)
    {
        FlySpeedMultiplier = System.Math.Clamp(
            FlySpeedMultiplier + scrollDelta * CameraConfig.EditorFlySpeedScrollStep,
            CameraConfig.MinEditorFlySpeedMultiplier,
            CameraConfig.MaxEditorFlySpeedMultiplier);
    }

    public void ResetFlySpeedMultiplier() =>
        FlySpeedMultiplier = CameraConfig.DefaultEditorFlySpeedMultiplier;

    public void Zoom(float delta)
    {
        Distance -= delta * CalculateZoomSpeed();
        if (Distance < CameraConfig.MinEditorDistance)
        {
            FocalPoint += GetForwardDirection();
            Distance = CameraConfig.MinEditorDistance;
        }
        Distance = MathF.Min(Distance, CameraConfig.MaxEditorDistance);
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

    public Vector2 GetPreviousMousePosition() => _previousMousePosition;

    private void UpdateProjection()
    {
        _projection = Matrix4x4.CreatePerspectiveFieldOfView(
            MathHelpers.DegreesToRadians(FOV),
            _aspectRatio,
            _nearClip,
            _farClip);
        _projectionDirty = false;
    }

    private void ApplyRotation(Vector2 delta)
    {
        var yawSign = GetUpDirection().Y < 0 ? -1.0f : 1.0f;
        Yaw += yawSign * delta.X * CameraConfig.EditorRotationSpeed;
        Pitch += delta.Y * CameraConfig.EditorRotationSpeed;
        Pitch = System.Math.Clamp(Pitch, -MathF.PI / 2f + 0.01f, MathF.PI / 2f - 0.01f);
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
        float distance = Distance * 0.2f;
        distance = MathF.Max(distance, 0.0f);
        float speed = distance * distance;
        return MathF.Min(speed, 100.0f);
    }
}
