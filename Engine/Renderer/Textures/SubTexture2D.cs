using System.Numerics;

namespace Engine.Renderer.Textures;

public record SubTexture2D
{
    public Texture2D Texture { get; }
    public Vector2[] TexCoords { get; } = new Vector2[4];

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
        // bottom left
        var min = new Vector2((coords.X * cellSize.X) / texture.Width, (coords.Y * cellSize.Y) / texture.Height);

        // top right
        var max = new Vector2(((coords.X + spriteSize.X) * cellSize.X) / texture.Width,
            ((coords.Y + spriteSize.Y) * cellSize.Y) / texture.Height);

        return new SubTexture2D(texture, min, max);
    }
    
    public void Deconstruct(out Texture2D texture, out Vector2[] texCoords)
    {
        texture = Texture;
        texCoords = TexCoords;
    }
}