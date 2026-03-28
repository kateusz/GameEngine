using System.Numerics;
using ECS;

namespace Engine.Scene.Components;

public class LightingComponent : IComponent
{
    public Vector3 Position { get; set; }
    public Vector3 Color { get; set; } = Vector3.One;
    
    public IComponent Clone() => new LightingComponent { Position = Position, Color = Color };
}