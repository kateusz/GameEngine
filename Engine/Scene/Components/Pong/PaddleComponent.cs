using ECS;

namespace Engine.Scene.Components.Pong;

public sealed class PaddleComponent : IComponent
{
    public bool IsPlayer { get; set; }
    public float MoveSpeed { get; set; } = 6.0f;

    public IComponent Clone()
    {
        return new PaddleComponent
        {
            IsPlayer = IsPlayer,
            MoveSpeed = MoveSpeed
        };
    }
}
