using System.Numerics;
using Engine.Core.Input;
using Engine.Renderer.Cameras;
using Engine.Scene;
using Engine.Scene.Components;

namespace Editor.assets.scripts;

// TODO: this must be removed from the engine and implemented in the user project
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