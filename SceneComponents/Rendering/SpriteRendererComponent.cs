using System.Numerics;
using ECS;

namespace SceneComponents.Rendering;

public class SpriteRendererComponent : IComponent
{
    internal string? ResolvedTexturePath;

    public Vector4 Color { get; set; }

    public string? TexturePath
    {
        get;
        set
        {
            if (string.Equals(field, value, StringComparison.Ordinal))
                return;

            field = value;
            ResolvedTexturePath = null;
        }
    }

    public float TilingFactor { get; set; }

    /// <summary>
    /// Draw order for the 2D batch. Higher values are submitted later and appear in front.
    /// Depth test stays off. Equal values tie-break by entity id.
    /// Encode a side-view Y-sort as <c>layer - worldY</c> so lower actors draw in front.
    /// </summary>
    public float SortOrder { get; set; }

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
            TilingFactor = TilingFactor,
            SortOrder = SortOrder
        };
    }
}
