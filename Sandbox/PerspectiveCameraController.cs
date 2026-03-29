using System.Collections.Concurrent;
using System.Numerics;
using Engine.Core.Input;
using Engine.Events;
using Engine.Events.Input;
using Engine.Math;

namespace Sandbox;

public class PerspectiveCameraController
{
    private readonly ConcurrentDictionary<KeyCodes, byte> _pressedKeys = new();

    private Vector3 _position;
    private float _yaw;
    private float _pitch;

    private float _lastMouseX;
    private float _lastMouseY;
    private bool _firstMouse = true;
    private bool _mouseLookActive;

    private const float MoveSpeed = 10.0f;
    private const float MouseSensitivity = 0.003f;
    private const float ScrollSpeedMultiplier = 2.0f;
    private float _speedMultiplier = 10.0f;

    public Vector3 Position => _position;
    public float Pitch => _pitch;
    public float Yaw => _yaw;

    public PerspectiveCameraController(Vector3 initialPosition, float initialYaw = 0f, float initialPitch = 0f)
    {
        _position = initialPosition;
        _yaw = initialYaw;
        _pitch = initialPitch;
    }

    public void OnUpdate(TimeSpan deltaTime)
    {
        var dt = (float)deltaTime.TotalSeconds;
        var speed = MoveSpeed * _speedMultiplier * dt;

        var euler = new Vector3(_pitch, _yaw, 0);
        var q = MathHelpers.QuaternionFromEuler(euler);
        var rotationMatrix = MathHelpers.MatrixFromQuaternion(q);

        var forward = Vector3.Normalize(Vector3.TransformNormal(-Vector3.UnitZ, rotationMatrix));
        var right = Vector3.Normalize(Vector3.TransformNormal(Vector3.UnitX, rotationMatrix));

        if (_pressedKeys.ContainsKey(KeyCodes.W))
            _position += forward * speed;
        if (_pressedKeys.ContainsKey(KeyCodes.S))
            _position -= forward * speed;
        if (_pressedKeys.ContainsKey(KeyCodes.A))
            _position -= right * speed;
        if (_pressedKeys.ContainsKey(KeyCodes.D))
            _position += right * speed;
        if (_pressedKeys.ContainsKey(KeyCodes.E) || _pressedKeys.ContainsKey(KeyCodes.Space))
            _position += Vector3.UnitY * speed;
        if (_pressedKeys.ContainsKey(KeyCodes.Q) || _pressedKeys.ContainsKey(KeyCodes.LeftShift))
            _position -= Vector3.UnitY * speed;
    }

    public void OnEvent(Event @event)
    {
        switch (@event)
        {
            case KeyPressedEvent kpe:
                _pressedKeys.TryAdd(kpe.KeyCode, 0);
                break;
            case KeyReleasedEvent kre:
                _pressedKeys.TryRemove(kre.KeyCode, out _);
                break;
            case MouseButtonPressedEvent { Button: 1 }:
                _mouseLookActive = true;
                _firstMouse = true;
                break;
            case MouseButtonReleasedEvent { Button: 1 }:
                _mouseLookActive = false;
                break;
            case MouseMovedEvent mme when _mouseLookActive:
                OnMouseMoved(mme.X, mme.Y);
                break;
            case MouseScrolledEvent mse:
                _speedMultiplier =
                    System.Math.Clamp(_speedMultiplier + mse.YOffset * ScrollSpeedMultiplier, 0.1f, 50.0f);
                break;
        }
    }

    private void OnMouseMoved(float x, float y)
    {
        if (_firstMouse)
        {
            _lastMouseX = x;
            _lastMouseY = y;
            _firstMouse = false;
            return;
        }

        var deltaX = x - _lastMouseX;
        var deltaY = _lastMouseY - y;
        _lastMouseX = x;
        _lastMouseY = y;

        _yaw += deltaX * MouseSensitivity;
        _pitch += deltaY * MouseSensitivity;

        _pitch = System.Math.Clamp(_pitch, -MathF.PI / 2f + 0.01f, MathF.PI / 2f - 0.01f);
    }
}
