using System.Numerics;

namespace Engine.Renderer.Textures;

public sealed record SubTexture2D
{
    public Texture2D Texture { get; }
    public Vector2[] TexCoords { get; } = new Vector2[RenderingConstants.QuadVertexCount];

    public SubTexture2D(Texture2D texture, Vector2 min, Vector2 max)
    {
        Texture = texture;
        TexCoords[0] = new Vector2(min.X, min.Y);
        TexCoords[1] = new Vector2(max.X, min.Y);
        TexCoords[2] = new Vector2(max.X, max.Y);
        TexCoords[3] = new Vector2(min.X, max.Y);
    }
    
    public static SubTexture2D CreateFromCoords(Texture2D texture, Vector2 coords, Vector2 cellSize, Vector2 spriteSize)
    {
        var min = MinUv(texture, coords, cellSize);
        var max = MaxUv(texture, coords, cellSize, spriteSize);
        return new SubTexture2D(texture, min, max);
    }

    /// <summary>Fills <paramref name="dest"/> (4 corners) without allocating a <see cref="SubTexture2D"/>.</summary>
    public static void FillTexCoordsFromCoords(
        Texture2D texture,
        Vector2 coords,
        Vector2 cellSize,
        Vector2 spriteSize,
        Vector2[] dest)
    {
        var min = MinUv(texture, coords, cellSize);
        var max = MaxUv(texture, coords, cellSize, spriteSize);
        dest[0] = new Vector2(min.X, min.Y);
        dest[1] = new Vector2(max.X, min.Y);
        dest[2] = new Vector2(max.X, max.Y);
        dest[3] = new Vector2(min.X, max.Y);
    }

    private static Vector2 MinUv(Texture2D texture, Vector2 coords, Vector2 cellSize) =>
        new((coords.X * cellSize.X) / texture.Width, (coords.Y * cellSize.Y) / texture.Height);

    private static Vector2 MaxUv(Texture2D texture, Vector2 coords, Vector2 cellSize, Vector2 spriteSize) =>
        new(((coords.X + spriteSize.X) * cellSize.X) / texture.Width,
            ((coords.Y + spriteSize.Y) * cellSize.Y) / texture.Height);
    
    public void Deconstruct(out Texture2D texture, out Vector2[] texCoords)
    {
        texture = Texture;
        texCoords = TexCoords;
    }
}