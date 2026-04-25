using ECS;

namespace Engine.Scene.Components.Pong;

public enum BoundaryPosition
{
    Top,
    Bottom
}

public sealed class BoundaryComponent : IComponent
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
