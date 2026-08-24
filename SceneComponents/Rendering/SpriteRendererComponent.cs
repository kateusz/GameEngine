using System.Numerics;
using ECS;

namespace SceneComponents.Rendering;

public class SpriteRendererComponent : IComponent
{
    /// <summary>Runtime cache of resolved absolute texture path; not serialized.</summary>
    internal string? ResolvedTexturePath;

    internal string? ResolvedTexturePathSource;

    public Vector4 Color { get; set; }
    public string? TexturePath { get; set; }
    public float TilingFactor { get; set; }

    public SpriteRendererComponent()
    {
        Color = Vector4.One;
        TilingFactor = 1.0f;
    }

    public SpriteRendererComponent(Vector4 color)
    {
        Color = color;
        TilingFactor = 1.0f;
    }

    public IComponent Clone()
    {
        return new SpriteRendererComponent(Color)
        {
            TexturePath = TexturePath,
            TilingFactor = TilingFactor
        };
    }
}