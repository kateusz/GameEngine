using System.Numerics;
using Engine.Core.Input;
using Engine.Scene.Components;

namespace Engine.Scene;

public class CameraController : ScriptableEntity
{
    public override void OnUpdate(TimeSpan ts)
    {
        var transform = GetComponent<TransformComponent>().Transform;
        var speed = 1.0f;

        Matrix4x4 translationMatrix = Matrix4x4.Identity;
        float translationAmount = 0;
        
        if (InputState.Instance.Keyboard.IsKeyPressed(KeyCodes.A))
            translationMatrix = Matrix4x4.CreateTranslation(new Vector3(-speed * (float)ts.TotalSeconds, 0, 0));
        if (InputState.Instance.Keyboard.IsKeyPressed(KeyCodes.D))
            translationMatrix = Matrix4x4.CreateTranslation(new Vector3(speed * (float)ts.TotalSeconds, 0, 0));
        if (InputState.Instance.Keyboard.IsKeyPressed(KeyCodes.W))
            translationMatrix = Matrix4x4.CreateTranslation(new Vector3(0, speed * (float)ts.TotalSeconds, 0));
        if (InputState.Instance.Keyboard.IsKeyPressed(KeyCodes.S))
            translationMatrix = Matrix4x4.CreateTranslation(new Vector3(0, -speed * (float)ts.TotalSeconds, 0));

        transform *= translationMatrix;
        GetComponent<TransformComponent>().Transform = transform;
    }

    
}