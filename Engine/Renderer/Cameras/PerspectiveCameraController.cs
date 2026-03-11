using System.Collections.Concurrent;
using System.Numerics;
using Engine.Events;
using Engine.Events.Input;
using Engine.Events.Window;
using Engine.Math;

namespace Engine.Renderer.Cameras;

/// <summary>
/// Free-fly perspective camera controller with WASD movement and right-click mouse look.
/// Drives a camera entity's position and rotation vectors directly.
/// </summary>
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
    private float _speedMultiplier = 1.0f;

    public Vector3 Position => _position;

    /// <summary>
    /// Pitch angle (up/down look). Maps to TransformComponent.Rotation.X (roll slot = rotation around X axis).
    /// </summary>
    public float Pitch => _pitch;

    /// <summary>
    /// Yaw angle (left/right look). Maps to TransformComponent.Rotation.Y (pitch slot = rotation around Y axis).
    /// </summary>
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

        // Use the engine's quaternion math for the forward/right vectors
        // to stay consistent with how TransformComponent.GetTransform() works.
        // Euler convention: X=rotation around X (pitch), Y=rotation around Y (yaw), Z=rotation around Z (roll)
        var euler = new Vector3(_pitch, _yaw, 0);
        var q = MathHelpers.QuaternionFromEuler(euler);
        var rotationMatrix = MathHelpers.MatrixFromQuaternion(q);

        // Default forward is -Z, default right is +X
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
                _pressedKeys.TryAdd((KeyCodes)kpe.KeyCode, 0);
                break;
            case KeyReleasedEvent kre:
                _pressedKeys.TryRemove((KeyCodes)kre.KeyCode, out _);
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
                _speedMultiplier = System.Math.Clamp(_speedMultiplier + mse.YOffset * ScrollSpeedMultiplier, 0.1f, 50.0f);
                break;
        }
    }

    private void OnMouseMoved(uint x, uint y)
    {
        if (_firstMouse)
        {
            _lastMouseX = x;
            _lastMouseY = y;
            _firstMouse = false;
            return;
        }

        var deltaX = x - _lastMouseX;
        var deltaY = _lastMouseY - y; // Inverted: mouse up = look up
        _lastMouseX = x;
        _lastMouseY = y;

        _yaw += deltaX * MouseSensitivity;
        _pitch += deltaY * MouseSensitivity;

        // Clamp pitch to prevent flipping
        _pitch = System.Math.Clamp(_pitch, -MathF.PI / 2f + 0.01f, MathF.PI / 2f - 0.01f);
    }
}
