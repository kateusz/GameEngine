using System.Numerics;
using ECS;
using Engine.Renderer.Textures;

namespace Engine.Scene.Components;

public record struct SpriteRendererComponent(Vector4 Color, Texture2D? Texture, float TilingFactor) : IComponent
{
    public SpriteRendererComponent() : this(Vector4.One, null, 1.0f) { }
    public SpriteRendererComponent(Vector4 color) : this(color, null, 1.0f) { }
}