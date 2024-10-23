using Engine.Core.Input;
using Engine.Scene.Components;

namespace Engine.Scene;

public class CameraController : ScriptableEntity
{
    public override void OnUpdate(TimeSpan ts)
    {
        var translation = GetComponent<TransformComponent>().Translation;
        var speed = 1.0f;

        if (InputState.Instance.Keyboard.IsKeyPressed(KeyCodes.A))
            translation.X -= speed * (float)ts.TotalSeconds;
        if (InputState.Instance.Keyboard.IsKeyPressed(KeyCodes.D))
            translation.X += speed * (float)ts.TotalSeconds;
        if (InputState.Instance.Keyboard.IsKeyPressed(KeyCodes.W))
            translation.Y += speed * (float)ts.TotalSeconds;
        if (InputState.Instance.Keyboard.IsKeyPressed(KeyCodes.S))
            translation.Y -= speed * (float)ts.TotalSeconds;
        
        GetComponent<TransformComponent>().Translation = translation;
    }

    
}