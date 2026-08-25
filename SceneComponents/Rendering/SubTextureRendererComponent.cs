using System.Numerics;
using ECS;

namespace SceneComponents.Rendering;

public class SubTextureRendererComponent : IComponent
{
    internal const int ManualTexCoordsKey = int.MinValue;

    /// <summary>Runtime cache key; 0 = stale, <see cref="ManualTexCoordsKey"/> = explicit UVs.</summary>
    internal int TexCoordsCacheKey;

    public Vector2 Coords
    {
        get;
        set
        {
            if (field == value)
                return;
            field = value;
            InvalidateTexCoordCache();
        }
    } = Vector2.Zero;

    public string? TexturePath
    {
        get;
        set
        {
            if (field == value)
                return;
            field = value;
            InvalidateTexCoordCache();
        }
    }

    /// <summary>
    /// Size of each cell in the sprite atlas (in pixels).
    /// Default is 16x16 pixels.
    /// </summary>
    public Vector2 CellSize
    {
        get;
        set
        {
            if (field == value)
                return;
            field = value;
            InvalidateTexCoordCache();
        }
    } = new(16, 16);

    /// <summary>
    /// Size of the sprite in cells (for multi-cell sprites).
    /// Default is 1x1 cells.
    /// </summary>
    public Vector2 SpriteSize
    {
        get;
        set
        {
            if (field == value)
                return;
            field = value;
            InvalidateTexCoordCache();
        }
    } = new(1, 1);

    /// <summary>
    /// Optional pre-calculated texture coordinates (4 vertices).
    /// If set, these will be used directly instead of calculating from Coords/CellSize/SpriteSize.
    /// Order: [bottom-left, bottom-right, top-right, top-left]
    /// </summary>
    public Vector2[]? TexCoords
    {
        get;
        set
        {
            field = value;
            TexCoordsCacheKey = value != null ? ManualTexCoordsKey : 0;
        }
    }

    internal void InvalidateTexCoordCache() => TexCoordsCacheKey = 0;

    public IComponent Clone()
    {
        return new SubTextureRendererComponent
        {
            Coords = Coords,
            TexturePath = TexturePath,
            CellSize = CellSize,
            SpriteSize = SpriteSize,
            TexCoords = (Vector2[])TexCoords?.Clone(),
            TexCoordsCacheKey = TexCoordsCacheKey,
        };
    }
}
