using System.Numerics;
using ECS;
using ECS.Systems;
using Input;
using Math;
using SceneComponents;
using SceneComponents.Camera;
using Scripting;

namespace Editor.assets.scripting;

[Register(typeof(IGameSystem))]
public class CameraController(Context context, IKeyboardInput keyboard, IMouseInput mouse) : IGameSystem
{
    private const float MoveSpeed = 10.0f;
    private const float LookSensitivity = 0.003f;
    private const float ScrollSpeedMultiplier = 1.0f;

    private Entity? _camera;
    private bool _isPerspective;
    private Vector3 _position;
    private float _yaw;
    private float _pitch;
    private float _speedMultiplier = 1.0f;

    public int Priority => 140;

    public void OnInit() => BindCamera();

    public void OnShutdown() { }

    public void OnUpdate(TimeSpan deltaTime)
    {
        if (_camera is null
            || !_camera.TryGetComponent<CameraComponent>(out var camera)
            || !_camera.TryGetComponent<TransformComponent>(out var transform))
        {
            BindCamera();
            return;
        }

        var dt = (float)deltaTime.TotalSeconds;
        if (!_isPerspective)
        {
            var move = Vector3.Zero;
            if (keyboard.IsKeyDown(KeyCodes.W)) move += Vector3.UnitY;
            if (keyboard.IsKeyDown(KeyCodes.S)) move -= Vector3.UnitY;
            if (keyboard.IsKeyDown(KeyCodes.A)) move -= Vector3.UnitX;
            if (keyboard.IsKeyDown(KeyCodes.D)) move += Vector3.UnitX;
            transform.Translation += move * MoveSpeed * dt;
            return;
        }

        _speedMultiplier = System.Math.Clamp(
            _speedMultiplier + mouse.Scroll.Y * ScrollSpeedMultiplier, 0.1f, 50.0f);

        if (mouse.IsButtonDown(MouseButtons.Right))
        {
            _yaw -= mouse.Delta.X * LookSensitivity;
            _pitch += -mouse.Delta.Y * LookSensitivity;
            _pitch = System.Math.Clamp(_pitch, -MathF.PI / 2f + 0.01f, MathF.PI / 2f - 0.01f);
        }

        var speed = MoveSpeed * _speedMultiplier * dt;
        var rotation = MathHelpers.MatrixFromQuaternion(
            MathHelpers.QuaternionFromEuler(new Vector3(_pitch, _yaw, 0)));
        var forward = Vector3.Normalize(Vector3.TransformNormal(-Vector3.UnitZ, rotation));
        var right = Vector3.Normalize(Vector3.TransformNormal(Vector3.UnitX, rotation));

        if (keyboard.IsKeyDown(KeyCodes.W)) _position += forward * speed;
        if (keyboard.IsKeyDown(KeyCodes.S)) _position -= forward * speed;
        if (keyboard.IsKeyDown(KeyCodes.A)) _position -= right * speed;
        if (keyboard.IsKeyDown(KeyCodes.D)) _position += right * speed;
        if (keyboard.IsKeyDown(KeyCodes.E) || keyboard.IsKeyDown(KeyCodes.Space))
            _position += Vector3.UnitY * speed;
        if (keyboard.IsKeyDown(KeyCodes.Q) || keyboard.IsKeyDown(KeyCodes.LeftShift))
            _position -= Vector3.UnitY * speed;

        camera.CameraViewTransform = null;
        transform.Translation = _position;
        transform.Rotation = new Vector3(_pitch, _yaw, 0);
    }

    private void BindCamera()
    {
        _camera = null;
        foreach (var (entity, camera) in context.View<CameraComponent>())
        {
            if (!camera.Primary)
                continue;

            _camera = entity;
            _isPerspective = camera.ProjectionType == CameraProjectionTypeData.Perspective;
            if (_isPerspective && entity.TryGetComponent<TransformComponent>(out var transform))
            {
                _position = transform.Translation;
                _pitch = transform.Rotation.X;
                _yaw = transform.Rotation.Y;
                camera.CameraViewTransform = null;
            }

            return;
        }
    }
}
