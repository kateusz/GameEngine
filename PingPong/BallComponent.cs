using System;
using System.Numerics;
using ECS;

namespace PingPong;

public sealed class BallComponent : IGameComponent
{
    public Vector2 Velocity { get; set; } = Vector2.Zero;
    public float Speed { get; set; } = 8.0f;

    public IComponent Clone()
    {
        return new BallComponent
        {
            Velocity = Velocity,
            Speed = Speed
        };
    }
}
