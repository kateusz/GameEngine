using System.Numerics;
using ECS;
using Engine.Renderer.Textures;

namespace Engine.Scene.Components;

public class SpriteRendererComponent : IComponent
{
    public Vector4 Color { get; set; }
    public Texture2D? Texture { get; set; }
    public float TilingFactor { get; set; }

    public SpriteRendererComponent()
    {
        Color = Vector4.One;
        Texture = null;
        TilingFactor = 1.0f;
    }

    public SpriteRendererComponent(Vector4 color)
    {
        Color = color;
        Texture = null;
        TilingFactor = 1.0f;
    }

    public SpriteRendererComponent(Vector4 color, Texture2D? texture, float tilingFactor)
    {
        Color = color;
        Texture = texture;
        TilingFactor = tilingFactor;
    }

    public IComponent Clone()
    {
        return new SpriteRendererComponent(Color, Texture, TilingFactor);
    }
}