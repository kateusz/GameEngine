using System.Numerics;
using ECS;

namespace SceneComponents.Rendering;

public class SpriteRendererComponent : IComponent
{
    internal string? ResolvedTexturePath;

    private string? _texturePath;

    public Vector4 Color { get; set; }

    public string? TexturePath
    {
        get => _texturePath;
        set
        {
            if (string.Equals(_texturePath, value, StringComparison.Ordinal))
                return;

            _texturePath = value;
            ResolvedTexturePath = null;
        }
    }

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
