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
    public Vector2 CellSize { get; set; }

    /// <summary>
    /// Size of the sprite in cells (for multi-cell sprites).
    /// Default is 1x1 cells.
    /// </summary>
    public Vector2 SpriteSize { get; set; }

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
            SpriteSize = SpriteSize
        };
    }
}
