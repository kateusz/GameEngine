using System.Numerics;
using System.Security.Cryptography;
using Engine.Renderer.Textures;
using Serilog;
using StbImageSharp;

namespace Engine.Tiles;

/// <summary>
/// Asset that defines a collection of tiles from a texture atlas
/// </summary>
public class TileSet
{
    private static readonly ILogger Logger = Log.ForContext<TileSet>();

    public required string TexturePath { get; init; }
    public required int Columns { get; init; }
    public required int Rows { get; init; }
    public Texture2D? Texture { get; private set; }

    public int TileWidth { get; set; }
    public int TileHeight { get; set; }
    public List<Tile> Tiles { get; } = [];

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

    private SubTexture2D? CreateSubTexture(int column, int row)
    {
        if (Texture == null)
            return null;

        float texWidth = Texture.Width;
        float texHeight = Texture.Height;

        float x = column * TileWidth;
        float y = row * TileHeight;

        var minU = x / texWidth;
        var minV = y / texHeight;

        var maxU = (x + TileWidth) / texWidth;
        var maxV = (y + TileHeight) / texHeight;

        var min = new Vector2(minU, minV);
        var max = new Vector2(maxU, maxV);

        return new SubTexture2D(Texture, min, max);
    }

    /// <summary>
    /// Gets texture coordinates for a specific tile ID
    /// </summary>
    public Vector2[] GetTileTextureCoords(int tileId)
    {
        var tile = GetTile(tileId);
        if (tile?.SubTexture is null)
        {
            // Return default quad texture coordinates
            return
            [
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(1, 1),
                new Vector2(0, 1)
            ];
        }

        return tile.SubTexture.TexCoords;
    }

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
        ImageResult? image;
        try
        {
            using var stream = File.OpenRead(TexturePath);
            image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
        }
        catch (Exception ex)
        {
            Logger.Warning(ex,
                "Failed to load image for pixel deduplication from {TexturePath}. Returning all tiles as unique.",
                TexturePath);
            
            // If we can't load the image, return all tiles as unique
            foreach (var tile in Tiles)
            {
                if (tile.SubTexture == null)
                    continue;
                uniqueTiles.Add(tile);
            }

            return uniqueTiles;
        }

        var seenPixelHashes = new HashSet<string>();
        foreach (var tile in Tiles)
        {
            if (tile.SubTexture == null)
                continue;

            var pixelHash = ComputeTilePixelHash(image, tile.Id);

            // Only add the tile if we haven't seen this pixel pattern before
            if (seenPixelHashes.Add(pixelHash))
            {
                uniqueTiles.Add(tile);
            }
        }

        return uniqueTiles;
    }

    private Tile? GetTile(int id) => Tiles.FirstOrDefault(t => t.Id == id);

    private string ComputeTilePixelHash(ImageResult image, int tileId)
    {
        var pixelData = GetPixelDataFromImage(tileId, image);
        var hashBytes = MD5.HashData(pixelData);
        return Convert.ToHexString(hashBytes);
    }

    private byte[] GetPixelDataFromImage(int tileId, ImageResult imageResult)
    {
        // Calculate the tile's position in the image
        var col = tileId % Columns;
        var row = tileId / Columns;

        var startX = col * TileWidth;
        var startY = row * TileHeight;

        var bytes = new byte[TileWidth * TileHeight * 4];
        var pixelDataIndex = 0;

        for (var y = startY; y < startY + TileHeight && y < imageResult.Height; y++)
        {
            for (var x = startX; x < startX + TileWidth && x < imageResult.Width; x++)
            {
                var sourceIndex = (y * imageResult.Width + x) * 4;
                if (sourceIndex + 3 >= imageResult.Data.Length)
                    continue; // next pixel

                // RGBA = 4 bytes per pixel
                bytes[pixelDataIndex++] = imageResult.Data[sourceIndex]; // R
                bytes[pixelDataIndex++] = imageResult.Data[sourceIndex + 1]; // G
                bytes[pixelDataIndex++] = imageResult.Data[sourceIndex + 2]; // B
                bytes[pixelDataIndex++] = imageResult.Data[sourceIndex + 3]; // A
            }
        }

        return bytes;
    }
}