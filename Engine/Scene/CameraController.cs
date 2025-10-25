using System;
using System.Numerics;
using Engine.Core.Input;
using Engine.Renderer.Cameras;
using Engine.Scene.Components;

namespace Engine.Scene;

/// <summary>
/// Deprecated: Move camera control into your project scripts.
/// Will be removed from Engine in a future release.
/// </summary>
[Obsolete("Move CameraController to your project scripts; this engine type will be removed.")]
public class CameraController : ScriptableEntity
{
    private const float CameraSpeed = CameraConfig.DefaultTranslationSpeed;
    private Vector3 _inputDirection = Vector3.Zero;

    public override void OnUpdate(TimeSpan ts)
    {
        if (_inputDirection != Vector3.Zero && HasComponent<TransformComponent>())
        {
            var transform = GetComponent<TransformComponent>();
            transform.Translation += _inputDirection * CameraSpeed * (float)ts.TotalSeconds;
        }
    }

    public override void OnKeyPressed(KeyCodes keyCode)
    {
        switch (keyCode)
        {
            case KeyCodes.W: _inputDirection += Vector3.UnitY; break;
            case KeyCodes.S: _inputDirection -= Vector3.UnitY; break;
            case KeyCodes.A: _inputDirection -= Vector3.UnitX; break;
            case KeyCodes.D: _inputDirection += Vector3.UnitX; break;
        }
    }

    public override void OnKeyReleased(KeyCodes keyCode)
    {
        switch (keyCode)
        {
            case KeyCodes.W: _inputDirection -= Vector3.UnitY; break;
            case KeyCodes.S: _inputDirection += Vector3.UnitY; break;
            case KeyCodes.A: _inputDirection += Vector3.UnitX; break;
            case KeyCodes.D: _inputDirection -= Vector3.UnitX; break;
        }
    }
}