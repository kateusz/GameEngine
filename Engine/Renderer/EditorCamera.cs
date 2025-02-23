using System.Numerics;
using Engine.Core.Input;
using Engine.Events;
using Engine.Math;
using Engine.Renderer.Cameras;
using Silk.NET.Input;

namespace Engine.Renderer;

public class EditorCamera : Camera
{
    private float _fov = 90.0f;
    private float _aspectRatio = 1.778f;
    private float _nearClip = 0.1f;
    private float _farClip = 1000.0f;

    private Matrix4x4 _viewMatrix;
    private Vector3 _position = Vector3.Zero;
    private Vector3 _focalPoint = Vector3.Zero;

    private Vector2 _initialMousePosition = Vector2.Zero;

    private float _distance = 10.0f;
    private float _pitch = 0.0f;
    private float _yaw = 0.0f;

    private float _viewportWidth = 1280;
    private float _viewportHeight = 720;

    public EditorCamera() : base(Matrix4x4.Identity)
    {
    }

    public EditorCamera(float fov, float aspectRatio, float nearClip, float farClip) : base(Matrix4x4.Identity)
    {
        _fov = fov;
        _aspectRatio = aspectRatio;
        _nearClip = nearClip;
        _farClip = farClip;

        UpdateProjection();
        UpdateView();
    }

    public void OnUpdate(Vector2 mousePosition)
    {
        var keyboard = InputState.Instance.Keyboard;
        var mouse = InputState.Instance.Mouse;
        
        if (keyboard.IsKeyPressed(KeyCodes.LeftAlt))
        {
            var delta = (mousePosition - _initialMousePosition) * 0.003f;
            _initialMousePosition = mousePosition;

            if (mouse.IsMouseButtonPressed((int)MouseButton.Middle))
            {
                MousePan(delta);
            }
            else if (mouse.IsMouseButtonPressed((int)MouseButton.Left))
            {
                MouseRotate(delta);
            }
            else if (mouse.IsMouseButtonPressed((int)MouseButton.Right))
            {
                MouseZoom(delta.Y);
            }
        }

        UpdateView();
    }

    public void OnEvent(Event evt)
    {
        if (evt is MouseScrolledEvent mouseScrolledEvent)
        {
            OnMouseScroll(mouseScrolledEvent);
        }
    }

    public float GetDistance() => _distance;

    public void SetDistance(float distance) => _distance = distance;

    public void SetViewportSize(float width, float height)
    {
        _viewportWidth = width;
        _viewportHeight = height;
        UpdateProjection();
    }
    
    public void CenterToPos(Vector3 position)
    {
        _focalPoint = position;
        _distance = 100.0f;
        UpdateView();
    }


    public Matrix4x4 GetViewMatrix() => _viewMatrix;

    //public Matrix4x4 GetViewProjection() => Matrix4x4.Multiply(Projection, _viewMatrix);
    public Matrix4x4 GetViewProjection() => Matrix4x4.Multiply(_viewMatrix, Projection);

    public Vector3 GetUpDirection() => Vector3.Transform(Vector3.UnitY, GetOrientation());

    public Vector3 GetRightDirection() => Vector3.Transform(Vector3.UnitX, GetOrientation());

    public Vector3 GetForwardDirection() => Vector3.Transform(-Vector3.UnitZ, GetOrientation());

    public Vector3 GetPosition() => _position;

    public Quaternion GetOrientation() => Quaternion.CreateFromYawPitchRoll(_yaw, _pitch, 0.0f);

    public float GetPitch() => _pitch;

    public float GetYaw() => _yaw;

    private void UpdateProjection()
    {
        _aspectRatio = _viewportWidth / _viewportHeight;
        Projection =
            Matrix4x4.CreatePerspectiveFieldOfView(MathHelpers.DegreesToRadians(_fov), _aspectRatio, _nearClip,
                _farClip);
    }

    private void UpdateView()
    {
        _position = CalculatePosition();
        var orientation = GetOrientation();
        _viewMatrix = Matrix4x4.CreateTranslation(_position) * Matrix4x4.CreateFromQuaternion(orientation);
        Matrix4x4.Invert(_viewMatrix, out var invertedViewMatrix);
        _viewMatrix = invertedViewMatrix;
    }

    private bool OnMouseScroll(MouseScrolledEvent e)
    {
        var delta = e.YOffset * 0.1f;
        MouseZoom(delta);
        UpdateView();
        return false;
    }

    private void MousePan(Vector2 delta)
    {
        var (xSpeed, ySpeed) = PanSpeed();
        _focalPoint += -GetRightDirection() * delta.X * xSpeed * _distance;
        _focalPoint += GetUpDirection() * delta.Y * ySpeed * _distance;
    }

    private void MouseRotate(Vector2 delta)
    {
        float yawSign = GetUpDirection().Y < 0 ? -1.0f : 1.0f;
        _yaw += yawSign * delta.X * RotationSpeed();
        _pitch += delta.Y * RotationSpeed();
    }

    private void MouseZoom(float delta)
    {
        _distance -= delta * ZoomSpeed();
        if (_distance < 1.0f)
        {
            _focalPoint += GetForwardDirection();
            _distance = 1.0f;
        }
    }

    private Vector3 CalculatePosition() => _focalPoint - GetForwardDirection() * _distance;

    private (float, float) PanSpeed()
    {
        float x = MathF.Min(_viewportWidth / 1000.0f, 2.4f);
        float xFactor = 0.0366f * (x * x) - 0.1778f * x + 0.3021f;

        float y = MathF.Min(_viewportHeight / 1000.0f, 2.4f);
        float yFactor = 0.0366f * (y * y) - 0.1778f * y + 0.3021f;

        return (xFactor, yFactor);
    }

    private float RotationSpeed() => 0.8f;

    private float ZoomSpeed()
    {
        float distance = _distance * 0.2f;
        distance = MathF.Max(distance, 0.0f);
        float speed = distance * distance;
        return MathF.Min(speed, 100.0f);
    }
}