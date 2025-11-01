using System.Numerics;
using Engine.Renderer.Textures;

namespace Engine.Scene.Components;

/// <summary>
/// Represents a single tile definition in a tileset
/// </summary>
public class Tile
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public SubTexture2D? SubTexture { get; set; }
    
    // Optional properties for advanced features
    public bool IsCollidable { get; set; } = false;
    public Dictionary<string, object> CustomProperties { get; set; } = new();
}

/// <summary>
/// Asset that defines a collection of tiles from a texture atlas
/// </summary>
public class TileSet
{
    public string Name { get; set; } = "New TileSet";
    public string TexturePath { get; set; } = string.Empty;
    public Texture2D? Texture { get; private set; }
    
    public int TileWidth { get; set; } = 16;
    public int TileHeight { get; set; } = 16;
    public int Columns { get; set; } = 8;
    public int Rows { get; set; } = 8;
    public int Spacing { get; set; } = 0;
    public int Margin { get; set; } = 0;
    
    public List<Tile> Tiles { get; set; } = new();

    /// <summary>
    /// Loads the tileset texture
    /// </summary>
    public void LoadTexture()
    {
        if (!string.IsNullOrEmpty(TexturePath) && File.Exists(TexturePath))
        {
            Texture = TextureFactory.Create(TexturePath);
        }
    }

    /// <summary>
    /// Generates tiles from the texture atlas
    /// </summary>
    public void GenerateTiles()
    {
        Tiles.Clear();
        
        if (Texture == null) return;
        
        int tileId = 0;
        for (int row = 0; row < Rows; row++)
        {
            for (int col = 0; col < Columns; col++)
            {
                var tile = new Tile
                {
                    Id = tileId++,
                    Name = $"Tile_{row}_{col}",
                    SubTexture = CreateSubTexture(col, row)
                };
                Tiles.Add(tile);
            }
        }
    }

    /// <summary>
    /// Creates a SubTexture2D for a tile at the specified position
    /// </summary>
    private SubTexture2D? CreateSubTexture(int column, int row)
    {
        if (Texture == null) return null;

        float texWidth = Texture.Width;
        float texHeight = Texture.Height;
        
        float x = Margin + column * (TileWidth + Spacing);
        float y = Margin + row * (TileHeight + Spacing);
        
        float minU = x / texWidth;
        float minV = y / texHeight;
        
        float maxU = (x + TileWidth) / texWidth;
        float maxV = (y + TileHeight) / texHeight;
        
        var min = new Vector2(minU, minV);
        var max = new Vector2(maxU, maxV);
        
        return new SubTexture2D(Texture, min, max);
    }

    /// <summary>
    /// Gets a tile by its ID
    /// </summary>
    public Tile? GetTile(int id)
    {
        return Tiles.FirstOrDefault(t => t.Id == id);
    }

    /// <summary>
    /// Gets texture coordinates for a specific tile ID
    /// </summary>
    public Vector2[] GetTileTextureCoords(int tileId)
    {
        var tile = GetTile(tileId);
        if (tile?.SubTexture == null)
        {
            // Return default quad texture coordinates
            return new[]
            {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(1, 1),
                new Vector2(0, 1)
            };
        }

        return tile.SubTexture.TexCoords;
    }
    
    /// <summary>
    /// Gets the SubTexture2D for a specific tile ID
    /// </summary>
    public SubTexture2D? GetTileSubTexture(int tileId)
    {
        var tile = GetTile(tileId);
        return tile?.SubTexture;
    }
}

