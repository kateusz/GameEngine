using System.Numerics;
using Audio;
using ECS;
using Input;
using Math;
using SceneComponents;
using SceneComponents.Camera;
using Scripting;

namespace Editor.assets.scripts;

// TODO: this must be removed from the engine and implemented in the user project
public class CameraController : ScriptableEntity
{
    private const float MoveSpeed = 10.0f;
    private const float ScrollSpeedMultiplier = 1.0f;

    private bool _isPerspective;

    // Perspective (FPS-style)
    private Vector3 _position;
    private float _yaw;
    private float _pitch;
    private float _speedMultiplier = 1.0f;
    private bool _mouseLookActive;
    private float _lastMouseX;
    private float _lastMouseY;
    private bool _firstMouseSample = true;
    private readonly HashSet<KeyCodes> _pressedKeys = [];

    // Orthographic movement accumulator
    private Vector3 _orthoInput = Vector3.Zero;

    public CameraController(IComponentAccessor componentAccessor, IAudio audio, IAudioPlayback audioPlayback) : base(componentAccessor, audio, audioPlayback)
    {
    }

    public override void OnCreate()
    {
        if (!HasComponent<CameraComponent>())
            return;

        _isPerspective = GetComponent<CameraComponent>().ProjectionType == CameraProjectionTypeData.Perspective;

        if (_isPerspective && HasComponent<TransformComponent>())
            _position = GetComponent<TransformComponent>().Translation;

        if (_isPerspective)
            GetComponent<CameraComponent>().CameraViewTransform = null;
    }

    public override void OnUpdate(TimeSpan ts)
    {
        if (!HasComponent<CameraComponent>())
            return;

        if (!_isPerspective)
        {
            UpdateOrthographic((float)ts.TotalSeconds);
            return;
        }

        if (!HasComponent<TransformComponent>())
            return;

        var dt = (float)ts.TotalSeconds;
        var speed = MoveSpeed * _speedMultiplier * dt;

        var euler = new Vector3(_pitch, _yaw, 0);
        var q = MathHelpers.QuaternionFromEuler(euler);
        var rotationMatrix = MathHelpers.MatrixFromQuaternion(q);

        var forward = Vector3.Normalize(Vector3.TransformNormal(-Vector3.UnitZ, rotationMatrix));
        var right = Vector3.Normalize(Vector3.TransformNormal(Vector3.UnitX, rotationMatrix));

        if (_pressedKeys.Contains(KeyCodes.W)) _position += forward * speed;
        if (_pressedKeys.Contains(KeyCodes.S)) _position -= forward * speed;
        if (_pressedKeys.Contains(KeyCodes.A)) _position -= right * speed;
        if (_pressedKeys.Contains(KeyCodes.D)) _position += right * speed;
        if (_pressedKeys.Contains(KeyCodes.E) || _pressedKeys.Contains(KeyCodes.Space))
            _position += Vector3.UnitY * speed;
        if (_pressedKeys.Contains(KeyCodes.Q) || _pressedKeys.Contains(KeyCodes.LeftShift))
            _position -= Vector3.UnitY * speed;

        GetComponent<CameraComponent>().CameraViewTransform = null;
        var transform = GetComponent<TransformComponent>();
        transform.Translation = _position;
        transform.Rotation = new Vector3(_pitch, _yaw, 0);
    }

    public override void OnDestroy()
    {
        if (_isPerspective && HasComponent<CameraComponent>())
            GetComponent<CameraComponent>().CameraViewTransform = null;
    }

    private void UpdateOrthographic(float dt)
    {
        if (_orthoInput == Vector3.Zero || !HasComponent<TransformComponent>())
            return;
        GetComponent<TransformComponent>().Translation += _orthoInput * MoveSpeed * dt;
    }

    public override void OnMouseMoved(float x, float y)
    {
        if (!_isPerspective || !_mouseLookActive)
            return;

        if (_firstMouseSample)
        {
            _lastMouseX = x;
            _lastMouseY = y;
            _firstMouseSample = false;
            return;
        }

        var deltaX = x - _lastMouseX;
        var deltaY = _lastMouseY - y;
        _lastMouseX = x;
        _lastMouseY = y;

        _yaw -= deltaX * 0.003f;
        _pitch += deltaY * 0.003f;
        _pitch = System.Math.Clamp(_pitch, -MathF.PI / 2f + 0.01f, MathF.PI / 2f - 0.01f);
    }

    public override void OnMouseScrolled(float xOffset, float yOffset)
    {
        if (!_isPerspective)
            return;
        _speedMultiplier = System.Math.Clamp(_speedMultiplier + yOffset * ScrollSpeedMultiplier, 0.1f, 50.0f);
    }

    public override void OnMouseButtonPressed(int button)
    {
        if (button == 1 && _isPerspective)
        {
            _mouseLookActive = true;
            _firstMouseSample = true;
        }
    }

    public override void OnMouseButtonReleased(int button)
    {
        if (button == 1)
            _mouseLookActive = false;
    }

    public override void OnKeyPressed(KeyCodes key)
    {
        if (_isPerspective)
        {
            _pressedKeys.Add(key);
        }
        else
        {
            switch (key)
            {
                case KeyCodes.W: _orthoInput += Vector3.UnitY; break;
                case KeyCodes.S: _orthoInput -= Vector3.UnitY; break;
                case KeyCodes.A: _orthoInput -= Vector3.UnitX; break;
                case KeyCodes.D: _orthoInput += Vector3.UnitX; break;
            }
        }
    }

    public override void OnKeyReleased(KeyCodes keyCode)
    {
        if (_isPerspective)
        {
            _pressedKeys.Remove(keyCode);
        }
        else
        {
            switch (keyCode)
            {
                case KeyCodes.W: _orthoInput -= Vector3.UnitY; break;
                case KeyCodes.S: _orthoInput += Vector3.UnitY; break;
                case KeyCodes.A: _orthoInput += Vector3.UnitX; break;
                case KeyCodes.D: _orthoInput -= Vector3.UnitX; break;
            }
        }
    }
}
