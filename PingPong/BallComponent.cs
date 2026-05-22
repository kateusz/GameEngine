using System.Numerics;
using ECS;

namespace PingPong;

public sealed class BallComponent : IGameComponent
{
    public Vector2 Velocity { get; set; } = Vector2.Zero;
    public float Speed { get; set; } = 8.0f;
    public bool ReadyToLaunch { get; set; }
    public int LaunchDirection { get; set; } = 1;

    public IComponent Clone()
    {
        return new BallComponent
        {
            Velocity = Velocity,
            Speed = Speed,
            ReadyToLaunch = ReadyToLaunch,
            LaunchDirection = LaunchDirection
        };
    }
}
