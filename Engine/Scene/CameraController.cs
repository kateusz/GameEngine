using System.Numerics;
using Engine.Core.Input;
using Engine.Scene.Components;

namespace Engine.Scene;

public class CameraController : ScriptableEntity
{
    private float _cameraSpeed = 10.0f;

    public override void OnUpdate(TimeSpan ts)
    {
        if (HasComponent<TransformComponent>())
        {
            var transform = GetComponent<TransformComponent>();
            
            if (InputState.Instance.Keyboard.IsKeyPressed(KeyCodes.W))
                transform.Translation += Vector3.UnitY * _cameraSpeed * (float)ts.TotalSeconds;
            if (InputState.Instance.Keyboard.IsKeyPressed(KeyCodes.S))
                transform.Translation -= Vector3.UnitY * _cameraSpeed * (float)ts.TotalSeconds;
            if (InputState.Instance.Keyboard.IsKeyPressed(KeyCodes.A))
                transform.Translation -= Vector3.UnitX * _cameraSpeed * (float)ts.TotalSeconds;
            if (InputState.Instance.Keyboard.IsKeyPressed(KeyCodes.D))
                transform.Translation += Vector3.UnitX * _cameraSpeed * (float)ts.TotalSeconds;
        }
    }
}