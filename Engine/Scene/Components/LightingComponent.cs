using System.Numerics;
using ECS;

namespace Engine.Scene.Components;

public enum LightType { Point, Directional }

public class LightingComponent : IComponent
{
    public LightType Type { get; set; } = LightType.Point;
    public Vector3 Position { get; set; }
    public Vector3 Direction { get; set; } = new Vector3(0f, -1f, 0f);
    public Vector3 Color { get; set; } = Vector3.One;

    public IComponent Clone() => new LightingComponent
        { Type = Type, Position = Position, Direction = Direction, Color = Color };
}
