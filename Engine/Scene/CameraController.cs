using System.Numerics;
using Engine.Core.Input;
using Engine.Scene.Components;

namespace Engine.Scene;

public class CameraController : ScriptableEntity
{
    private const float CameraSpeed = 0.5f;
    private Vector3 _inputDirection = Vector3.Zero;

    public override void OnUpdate(TimeSpan ts)
    {
        if (_inputDirection != Vector3.Zero && HasComponent<TransformComponent>())
        {
            var transform = GetComponent<TransformComponent>();
            transform.Translation += _inputDirection * CameraSpeed * (float)ts.TotalSeconds;
        }
    }

    public override void OnKeyPressed(KeyCodes key)
    {
        switch (key)
        {
            case KeyCodes.W: _inputDirection += Vector3.UnitY; break;
            case KeyCodes.S: _inputDirection -= Vector3.UnitY; break;
            case KeyCodes.A: _inputDirection -= Vector3.UnitX; break;
            case KeyCodes.D: _inputDirection += Vector3.UnitX; break;
        }
    }

    public override void OnKeyReleased(KeyCodes key)
    {
        switch (key)
        {
            case KeyCodes.W: _inputDirection -= Vector3.UnitY; break;
            case KeyCodes.S: _inputDirection += Vector3.UnitY; break;
            case KeyCodes.A: _inputDirection += Vector3.UnitX; break;
            case KeyCodes.D: _inputDirection -= Vector3.UnitX; break;
        }
    }
}