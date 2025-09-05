using System.Numerics;
using Engine.Renderer.Textures;

namespace Engine.UI.Rendering;

/// <summary>
/// Represents a loaded font with its glyph atlas and metrics
/// </summary>
public class Font
{
    public string Name { get; set; } = string.Empty;
    public float Size { get; set; }
    public Texture2D AtlasTexture { get; set; } = null!;
    public Dictionary<char, Glyph> Glyphs { get; set; } = new();
    public float LineHeight { get; set; }
    public float Ascender { get; set; }
    public float Descender { get; set; }
    
    public Font(string name, float size)
    {
        Name = name;
        Size = size;
    }
    
    /// <summary>
    /// Gets the glyph for a character, creating a fallback if not found
    /// </summary>
    public Glyph GetGlyph(char character)
    {
        if (Glyphs.TryGetValue(character, out var glyph))
        {
            return glyph;
        }
        
        // Return a fallback glyph (space or question mark)
        return Glyphs.TryGetValue('?', out var fallback) ? fallback : Glyphs[' '];
    }
    
    /// <summary>
    /// Measures the width of a text string
    /// </summary>
    public float MeasureText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0.0f;
            
        float width = 0.0f;
        foreach (char c in text)
        {
            var glyph = GetGlyph(c);
            width += glyph.Advance;
        }
        
        return width;
    }
    
    /// <summary>
    /// Measures the height of a text string (single line)
    /// </summary>
    public float MeasureTextHeight(string text)
    {
        return LineHeight;
    }
}

/// <summary>
/// Represents a single character glyph with its rendering information
/// </summary>
public class Glyph
{
    public char Character { get; set; }
    public Vector2 Size { get; set; } // Size in pixels
    public Vector2 Bearing { get; set; } // Offset from baseline
    public float Advance { get; set; } // Horizontal advance to next character
    public Vector2 AtlasPosition { get; set; } // Position in atlas texture
    public Vector2 AtlasSize { get; set; } // Size in atlas texture
    public Vector2[] TextureCoords { get; set; } = new Vector2[4]; // UV coordinates for rendering
    
    public Glyph(char character)
    {
        Character = character;
    }
    
    /// <summary>
    /// Calculates the texture coordinates for this glyph in the atlas
    /// </summary>
    public void CalculateTextureCoords(int atlasWidth, int atlasHeight)
    {
        var min = new Vector2(
            AtlasPosition.X / atlasWidth,
            AtlasPosition.Y / atlasHeight
        );
        
        var max = new Vector2(
            (AtlasPosition.X + AtlasSize.X) / atlasWidth,
            (AtlasPosition.Y + AtlasSize.Y) / atlasHeight
        );
        
        // OpenGL texture coordinates (bottom-left origin)
        TextureCoords[0] = new Vector2(min.X, min.Y); // Bottom-left
        TextureCoords[1] = new Vector2(max.X, min.Y); // Bottom-right
        TextureCoords[2] = new Vector2(max.X, max.Y); // Top-right
        TextureCoords[3] = new Vector2(min.X, max.Y); // Top-left
    }
}
