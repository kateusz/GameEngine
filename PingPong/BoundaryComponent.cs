using System;
using ECS;

namespace PingPong;

public enum BoundaryPosition
{
    Top,
    Bottom
}

public sealed class BoundaryComponent : IGameComponent
{
    public BoundaryPosition Position { get; set; } = BoundaryPosition.Top;

    public IComponent Clone()
    {
        return new BoundaryComponent
        {
            Position = Position
        };
    }
}
