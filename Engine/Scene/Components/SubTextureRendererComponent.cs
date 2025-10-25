using System.Numerics;
using ECS;
using Engine.Renderer.Textures;

namespace Engine.Scene.Components;

public class SubTextureRendererComponent : IComponent
{
    public Vector2 Coords { get; set; }
    public Texture2D? Texture { get; set; }

    public SubTextureRendererComponent()
    {
        Coords = Vector2.Zero;
        Texture = null;
    }

    public SubTextureRendererComponent(Vector2 coords, Texture2D? texture)
    {
        Coords = coords;
        Texture = texture;
    }

    public IComponent Clone()
    {
        return new SubTextureRendererComponent(Coords, Texture);
    }
}
