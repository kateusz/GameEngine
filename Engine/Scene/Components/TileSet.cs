using System.Numerics;
using System.Security.Cryptography;
using Engine.Renderer.Textures;
using StbImageSharp;

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
    /// Gets a list of unique tiles by deduplicating tiles with identical pixel content.
    /// Tiles are considered duplicates if their pixel data is identical.
    /// </summary>
    /// <returns>List of unique tiles (first occurrence of each unique visual)</returns>
    public List<Tile> GetUniqueTiles()
    {
        var uniqueTiles = new List<Tile>();
        
        // If we can't access pixel data, fall back to returning all tiles as unique
        if (string.IsNullOrEmpty(TexturePath) || !File.Exists(TexturePath))
        {
            foreach (var tile in Tiles)
            {
                if (tile.SubTexture == null) continue;
                uniqueTiles.Add(tile);
            }
            return uniqueTiles;
        }
        
        // Load the image to access pixel data for comparison
        ImageResult? image = null;
        try
        {
            using var stream = File.OpenRead(TexturePath);
            image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
        }
        catch
        {
            // If we can't load the image, return all tiles as unique
            foreach (var tile in Tiles)
            {
                if (tile.SubTexture == null) continue;
                uniqueTiles.Add(tile);
            }
            return uniqueTiles;
        }
        
        var seenPixelHashes = new HashSet<string>();
        
        foreach (var tile in Tiles)
        {
            if (tile.SubTexture == null) continue;
            
            // Compute the pixel hash for this tile region
            var pixelHash = ComputeTilePixelHash(image, tile.Id);
            
            // Only add the tile if we haven't seen this pixel pattern before
            if (seenPixelHashes.Add(pixelHash))
            {
                uniqueTiles.Add(tile);
            }
        }
        
        return uniqueTiles;
    }
    
    /// <summary>
    /// Computes a hash of the pixel data for a specific tile region.
    /// </summary>
    private string ComputeTilePixelHash(ImageResult image, int tileId)
    {
        // Calculate the tile's position in the image
        var col = tileId % Columns;
        var row = tileId / Columns;
        
        var startX = Margin + col * (TileWidth + Spacing);
        var startY = Margin + row * (TileHeight + Spacing);
        
        // Extract pixel data for this tile region
        var pixelData = new byte[TileWidth * TileHeight * 4]; // RGBA = 4 bytes per pixel
        var index = 0;
        
        for (var y = startY; y < startY + TileHeight && y < image.Height; y++)
        {
            for (var x = startX; x < startX + TileWidth && x < image.Width; x++)
            {
                var sourceIndex = (y * image.Width + x) * 4;
                if (sourceIndex + 3 < image.Data.Length)
                {
                    pixelData[index++] = image.Data[sourceIndex];     // R
                    pixelData[index++] = image.Data[sourceIndex + 1]; // G
                    pixelData[index++] = image.Data[sourceIndex + 2]; // B
                    pixelData[index++] = image.Data[sourceIndex + 3]; // A
                }
            }
        }
        
        // Compute MD5 hash of the pixel data
        var hashBytes = MD5.HashData(pixelData);
        return Convert.ToHexString(hashBytes);
    }
}

