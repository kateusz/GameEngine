using System.Numerics;
using ECS;

namespace Engine.Scene.Components;

public class SpriteRendererComponent : Component
{
    public SpriteRendererComponent()
    {
        Color = Vector4.Zero;
    }
    
    public SpriteRendererComponent(Vector4 color)
    {
        Color = color;
    }

    public Vector4 Color { get; set; }
}