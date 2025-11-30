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
/// Represents a unique tile in a tileset palette, potentially mapping to multiple original tile IDs
/// </summary>
public class UniqueTile
{
    /// <summary>
    /// The first tile ID with this visual (used for painting)
    /// </summary>
    public int PrimaryTileId { get; set; }
    
    /// <summary>
    /// All tile IDs that share this visual
    /// </summary>
    public List<int> AllTileIds { get; set; } = new();
    
    /// <summary>
    /// Reference to the SubTexture for rendering in the palette
    /// </summary>
    public SubTexture2D? SubTexture { get; set; }
}

/// <summary>
/// Asset that defines a collection of tiles from a texture atlas
/// </summary>
public class TileSet
{
    /// <summary>
    /// Epsilon value for floating-point UV coordinate comparison
    /// </summary>
    private const float UvComparisonEpsilon = 0.0001f;
    
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
    /// Loads the tileset texture using the provided texture factory
    /// </summary>
    /// <param name="textureFactory">Factory for creating textures</param>
    public void LoadTexture(ITextureFactory textureFactory)
    {
        if (textureFactory == null)
            throw new ArgumentNullException(nameof(textureFactory));

        if (!string.IsNullOrEmpty(TexturePath) && File.Exists(TexturePath))
        {
            Texture = textureFactory.Create(TexturePath);
        }
    }

    /// <summary>
    /// Generates tiles from the texture atlas
    /// </summary>
    public void GenerateTiles()
    {
        Tiles.Clear();
        
        if (Texture == null) return;
        
        var tileId = 0;
        for (var row = 0; row < Rows; row++)
        {
            for (var col = 0; col < Columns; col++)
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
        
        var minU = x / texWidth;
        var minV = y / texHeight;
        
        var maxU = (x + TileWidth) / texWidth;
        var maxV = (y + TileHeight) / texHeight;
        
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
    
    /// <summary>
    /// Gets a list of unique tiles by deduplicating tiles with identical UV coordinates.
    /// Tiles are considered duplicates if their UV coordinates match within a small epsilon.
    /// </summary>
    /// <returns>List of unique tiles with mappings to all original tile IDs sharing the same visual</returns>
    public List<UniqueTile> GetUniqueTiles()
    {
        var uniqueTiles = new List<UniqueTile>();
        var uvKeyToUniqueTile = new Dictionary<string, UniqueTile>();
        
        foreach (var tile in Tiles)
        {
            if (tile.SubTexture == null) continue;
            
            // Generate a hash key based on quantized UV coordinates for O(1) lookup
            var uvKey = GenerateUvKey(tile.SubTexture.TexCoords);
            
            if (uvKeyToUniqueTile.TryGetValue(uvKey, out var existingUnique))
            {
                // Add this tile ID to the existing unique tile's list
                existingUnique.AllTileIds.Add(tile.Id);
            }
            else
            {
                // Create a new unique tile entry
                var uniqueTile = new UniqueTile
                {
                    PrimaryTileId = tile.Id,
                    AllTileIds = new List<int> { tile.Id },
                    SubTexture = tile.SubTexture
                };
                uniqueTiles.Add(uniqueTile);
                uvKeyToUniqueTile[uvKey] = uniqueTile;
            }
        }
        
        return uniqueTiles;
    }
    
    /// <summary>
    /// Generates a hash key for UV coordinates by quantizing them based on epsilon tolerance.
    /// This enables O(1) dictionary lookups for deduplication.
    /// </summary>
    private static string GenerateUvKey(Vector2[] coords)
    {
        // Quantize coordinates to epsilon precision to ensure matching within tolerance
        var quantizeFactor = 1.0f / UvComparisonEpsilon;
        var parts = new int[coords.Length * 2];
        
        for (var i = 0; i < coords.Length; i++)
        {
            parts[i * 2] = (int)MathF.Round(coords[i].X * quantizeFactor);
            parts[i * 2 + 1] = (int)MathF.Round(coords[i].Y * quantizeFactor);
        }
        
        return string.Join(",", parts);
    }
}

