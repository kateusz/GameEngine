using System.Numerics;
using ECS;
using Engine.Renderer.Textures;

namespace Engine.Scene.Components;

public class SubTextureRendererComponent : IComponent
{
    public Vector2 Coords { get; set; }
    public Texture2D? Texture { get; set; }

    /// <summary>
    /// Size of each cell in the sprite atlas (in pixels).
    /// Default is 16x16 pixels.
    /// </summary>
    public Vector2 CellSize { get; set; } = new(16, 16);

    /// <summary>
    /// Size of the sprite in cells (for multi-cell sprites).
    /// Default is 1x1 cells.
    /// </summary>
    public Vector2 SpriteSize { get; set; } = new(1, 1);

    /// <summary>
    /// Optional pre-calculated texture coordinates (4 vertices).
    /// If set, these will be used directly instead of calculating from Coords/CellSize/SpriteSize.
    /// This is useful for animations with pre-calculated UV coords.
    /// Order: [bottom-left, bottom-right, top-right, top-left]
    /// </summary>
    public Vector2[]? TexCoords { get; set; }

    public SubTextureRendererComponent()
    {
        Coords = Vector2.Zero;
        Texture = null;
        CellSize = new Vector2(16, 16);
        SpriteSize = new Vector2(1, 1);
    }

    public SubTextureRendererComponent(Vector2 coords, Texture2D? texture)
    {
        Coords = coords;
        Texture = texture;
        CellSize = new Vector2(16, 16);
        SpriteSize = new Vector2(1, 1);
    }

    public IComponent Clone()
    {
        return new SubTextureRendererComponent(Coords, Texture)
        {
            CellSize = CellSize,
            SpriteSize = SpriteSize,
            TexCoords = TexCoords != null ? (Vector2[])TexCoords.Clone() : null
        };
    }
}
