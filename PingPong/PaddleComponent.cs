using ECS;

namespace PingPong;

public sealed class PaddleComponent : IGameComponent
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
