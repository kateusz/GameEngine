using System.Numerics;
using ECS;
using Engine.Renderer.Textures;

namespace Engine.Scene.Components;

public class SpriteRendererComponent : Component
{
    public SpriteRendererComponent()
    {
        Color = Vector4.One;
    }
    
    public SpriteRendererComponent(Vector4 color)
    {
        Color = color;
    }

    public Vector4 Color { get; set; }
    public Texture2D? Texture { get; set; }
    public float TilingFactor { get; set; } = 1.0f;
}