using System.Numerics;
using ECS;
using Engine.Renderer.Textures;

namespace Engine.Scene.Components;

public record struct SubTextureRendererComponent(Vector2 Coords, Texture2D? Texture) : IComponent
{
    public SubTextureRendererComponent() : this(Vector2.Zero, null) { }
}