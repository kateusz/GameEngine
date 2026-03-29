using System;
using System.Numerics;
using Engine.Core.Input;
using Engine.Math;
using Engine.Renderer.Cameras;
using Engine.Scene;
using Engine.Scene.Components;

namespace Editor.assets.scripts;

// TODO: this must be removed from the engine and implemented in the user project
public class CameraController : ScriptableEntity
{
    private const float MoveSpeed = 10.0f;
    private const float ScrollSpeedMultiplier = 2.0f;

    private bool _isPerspective;

    // Perspective (FPS-style)
    private Vector3 _position;
    private float _yaw;
    private float _pitch;
    private float _speedMultiplier = 10.0f;
    private bool _mouseLookActive;
    private float _lastMouseX;
    private float _lastMouseY;
    private bool _firstMouseSample = true;
    private readonly HashSet<KeyCodes> _pressedKeys = new();

    // Orthographic movement accumulator
    private Vector3 _orthoInput = Vector3.Zero;

    public override void OnCreate()
    {
        if (!HasComponent<CameraComponent>())
            return;

        _isPerspective = GetComponent<CameraComponent>().Camera.ProjectionType == ProjectionType.Perspective;

        if (_isPerspective && HasComponent<TransformComponent>())
            _position = GetComponent<TransformComponent>().Translation;
    }

    public override void OnUpdate(TimeSpan ts)
    {
        if (!_isPerspective)
        {
            UpdateOrthographic((float)ts.TotalSeconds);
            return;
        }

        if (!HasComponent<CameraComponent>())
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

        var orientation = Quaternion.CreateFromYawPitchRoll(-_yaw, -_pitch, 0f);
        GetComponent<CameraComponent>().CameraViewTransform =
            Matrix4x4.CreateFromQuaternion(orientation) * Matrix4x4.CreateTranslation(_position);
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

        _yaw += deltaX * CameraConfig.EditorMouseSensitivity;
        _pitch += deltaY * CameraConfig.EditorMouseSensitivity;
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

    public override void OnKeyReleased(KeyCodes key)
    {
        if (_isPerspective)
        {
            _pressedKeys.Remove(key);
        }
        else
        {
            switch (key)
            {
                case KeyCodes.W: _orthoInput -= Vector3.UnitY; break;
                case KeyCodes.S: _orthoInput += Vector3.UnitY; break;
                case KeyCodes.A: _orthoInput += Vector3.UnitX; break;
                case KeyCodes.D: _orthoInput -= Vector3.UnitX; break;
            }
        }
    }
}
