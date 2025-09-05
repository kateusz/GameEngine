using System.Numerics;
using Engine.Platform;
using Engine.Renderer;
using Engine.Renderer.Textures;
using Engine.Platform.SilkNet;
using StbTrueTypeSharp;

namespace Engine.UI.Rendering;

/// <summary>
/// Handles font loading, glyph atlas generation, and text rendering
/// </summary>
public class FontRenderer
{
    private readonly IGraphics2D _graphics2D;
    private readonly Dictionary<string, Font> _loadedFonts;
    private Font? _defaultFont;

    // Font atlas generation constants
    private const int AtlasWidth = 1024;
    private const int AtlasHeight = 1024;
    private const int Padding = 2; // Padding between glyphs in atlas

    public FontRenderer(IGraphics2D graphics2D)
    {
        _graphics2D = graphics2D;
        _loadedFonts = new Dictionary<string, Font>();
    }

    /// <summary>
    /// Loads a font from a TTF file and generates a glyph atlas
    /// </summary>
    public Font LoadFont(string fontPath, float fontSize, string fontName = "")
    {
        if (string.IsNullOrEmpty(fontName))
        {
            fontName = Path.GetFileNameWithoutExtension(fontPath);
        }

        var fontKey = $"{fontName}_{fontSize}";
        if (_loadedFonts.TryGetValue(fontKey, out var existingFont))
        {
            return existingFont;
        }

        if (!File.Exists(fontPath))
        {
            throw new FileNotFoundException($"Font file not found: {fontPath}");
        }

        try
        {
            var font = LoadFontFromFile(fontPath, fontSize, fontName);
            _loadedFonts[fontKey] = font;

            // Set as default font if it's the first one loaded
            _defaultFont ??= font;

            return font;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to load font from {fontPath}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Gets the default font, or loads a fallback if none exists
    /// </summary>
    public Font GetDefaultFont()
    {
        if (_defaultFont != null)
            return _defaultFont;

        // Try to load a system font as fallback
        var fallbackPaths = GetSystemFontPaths();
        foreach (var path in fallbackPaths)
        {
            if (!File.Exists(path))
                continue;
            
            try
            {
                _defaultFont = LoadFont(path, 16.0f, "SystemDefault");
                return _defaultFont;
            }
            catch
            {
                // Continue to next fallback
            }
        }

        // Create a minimal fallback font if no system fonts are available
        _defaultFont = CreateFallbackFont();
        return _defaultFont;
    }

    /// <summary>
    /// Renders text using the specified font
    /// </summary>
    public void RenderText(string text, Vector2 position, Font font, Vector4 color, float scale = 1.0f)
    {
        if (string.IsNullOrEmpty(text) || font == null)
            return;

        var currentX = position.X;
        var currentY = position.Y;

        foreach (char character in text)
        {
            if (character == '\n')
            {
                currentX = position.X;
                currentY += font.LineHeight * scale;
                continue;
            }

            var glyph = font.GetGlyph(character);

            // Calculate glyph position
            var glyphPosition = new Vector3(
                currentX + glyph.Bearing.X * scale,
                currentY - (glyph.Size.Y - glyph.Bearing.Y) * scale,
                0.0f
            );

            var glyphSize = glyph.Size * scale;

            // Render the glyph as a textured quad
            if (glyph.AtlasSize.X > 0 && glyph.AtlasSize.Y > 0)
            {
                _graphics2D.DrawQuad(
                    glyphPosition,
                    glyphSize,
                    font.AtlasTexture,
                    glyph.TextureCoords,
                    1.0f,
                    color
                );
            }

            // Advance to next character
            currentX += glyph.Advance * scale;
        }
    }

    /// <summary>
    /// Measures the size of text when rendered
    /// </summary>
    public Vector2 MeasureText(string text, Font font, float scale = 1.0f)
    {
        if (string.IsNullOrEmpty(text) || font == null)
            return Vector2.Zero;

        var lines = text.Split('\n');
        var maxWidth = 0.0f;
        var totalHeight = 0.0f;

        foreach (var line in lines)
        {
            var lineWidth = font.MeasureText(line) * scale;
            maxWidth = System.Math.Max(maxWidth, lineWidth);
            totalHeight += font.LineHeight * scale;
        }

        return new Vector2(maxWidth, totalHeight);
    }

    private Font LoadFontFromFile(string fontPath, float fontSize, string fontName)
    {
        var fontData = File.ReadAllBytes(fontPath);
        var fontInfo = new StbTrueType.stbtt_fontinfo();

        unsafe
        {
            fixed (byte* fontDataPtr = fontData)
            {
                if (StbTrueType.stbtt_InitFont(fontInfo, fontDataPtr, 0) == 0)
                {
                    throw new InvalidOperationException("Failed to initialize font");
                }

                // Get font metrics
                var scale = StbTrueType.stbtt_ScaleForPixelHeight(fontInfo, fontSize);
                int ascent, descent, lineGap;
                StbTrueType.stbtt_GetFontVMetrics(fontInfo, &ascent, &descent, &lineGap);

                var font = new Font(fontName, fontSize)
                {
                    LineHeight = (ascent - descent + lineGap) * scale,
                    Ascender = ascent * scale,
                    Descender = descent * scale
                };

                // Generate glyph atlas
                GenerateGlyphAtlas(font, fontInfo, scale);

                return font;
            }
        }
    }

    private void GenerateGlyphAtlas(Font font, StbTrueType.stbtt_fontinfo fontInfo, float scale)
    {
        unsafe
        {
            // Character set to include in atlas
            var characters = GetCharacterSet();

            // First pass: calculate required atlas size
            var totalWidth = 0;
            var totalHeight = 0;
            var maxHeight = 0;

            unsafe
            {
                foreach (var character in characters)
                {
                    var glyphIndex = StbTrueType.stbtt_FindGlyphIndex(fontInfo, character);
                    if (glyphIndex == 0) continue;

                    int x0, y0, x1, y1;
                    StbTrueType.stbtt_GetGlyphBitmapBox(fontInfo, glyphIndex, scale, scale, &x0, &y0, &x1, &y1);

                    var width = x1 - x0;
                    var height = y1 - y0;

                    if (width > 0 && height > 0)
                    {
                        totalWidth += width + Padding;
                        maxHeight = System.Math.Max(maxHeight, height + Padding);
                    }
                }
            }

            // Use fixed atlas size for simplicity
            var atlasWidth = AtlasWidth;
            var atlasHeight = AtlasHeight;

            // Create atlas texture data
            var atlasData = new byte[atlasWidth * atlasHeight * 4]; // RGBA
            var currentX = 0;
            var currentY = 0;
            var rowHeight = 0;

            // Second pass: render glyphs into atlas
            unsafe
            {
                foreach (var character in characters)
                {
                    var glyphIndex = StbTrueType.stbtt_FindGlyphIndex(fontInfo, character);
                    if (glyphIndex == 0) continue;

                    int x0, y0, x1, y1;
                    StbTrueType.stbtt_GetGlyphBitmapBox(fontInfo, glyphIndex, scale, scale, &x0, &y0, &x1, &y1);

                    var width = x1 - x0;
                    var height = y1 - y0;

                    if (width <= 0 || height <= 0)
                    {
                        // Create empty glyph for spacing characters
                        var glyph = new Glyph(character)
                        {
                            Size = Vector2.Zero,
                            Bearing = Vector2.Zero,
                            Advance = StbTrueType.stbtt_GetCodepointKernAdvance(fontInfo, character, character) * scale
                        };
                        font.Glyphs[character] = glyph;
                        continue;
                    }

                    // Check if we need to move to next row
                    if (currentX + width > atlasWidth)
                    {
                        currentX = 0;
                        currentY += rowHeight;
                        rowHeight = 0;
                    }

                    // Render glyph bitmap
                    var bitmap = new byte[width * height];
                    fixed (byte* bitmapPtr = bitmap)
                    {
                        StbTrueType.stbtt_MakeGlyphBitmap(fontInfo, bitmapPtr, width, height, width, scale, scale,
                            glyphIndex);
                    }

                    // Copy bitmap to atlas
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            var atlasIndex = ((currentY + y) * atlasWidth + (currentX + x)) * 4;
                            var bitmapIndex = y * width + x;
                            var alpha = bitmap[bitmapIndex];

                            atlasData[atlasIndex + 0] = 255; // R
                            atlasData[atlasIndex + 1] = 255; // G
                            atlasData[atlasIndex + 2] = 255; // B
                            atlasData[atlasIndex + 3] = alpha; // A
                        }
                    }

                    // Create glyph
                    var glyphData = new Glyph(character)
                    {
                        Size = new Vector2(width, height),
                        Bearing = new Vector2(x0, y0),
                        Advance = StbTrueType.stbtt_GetCodepointKernAdvance(fontInfo, character, character) * scale,
                        AtlasPosition = new Vector2(currentX, currentY),
                        AtlasSize = new Vector2(width, height)
                    };

                    glyphData.CalculateTextureCoords(atlasWidth, atlasHeight);
                    font.Glyphs[character] = glyphData;

                    currentX += width + Padding;
                    rowHeight = System.Math.Max(rowHeight, height + Padding);
                }
            }

            // Create texture from atlas data
            font.AtlasTexture = CreateTextureFromData(atlasData, atlasWidth, atlasHeight);
        }
    }

    private Texture2D CreateTextureFromData(byte[] data, int width, int height)
    {
        var texture = TextureFactory.Create(width, height);
        
        // Cast to SilkNetTexture2D to use the new SetDataFromBytes method
        if (texture is SilkNetTexture2D silkNetTexture)
        {
            silkNetTexture.SetDataFromBytes(data, width, height);
        }
        else
        {
            throw new InvalidOperationException("Expected SilkNetTexture2D for font atlas creation");
        }
        
        return texture;
    }

    private char[] GetCharacterSet()
    {
        // Basic ASCII character set plus common extended characters
        var characters = new List<char>();

        // ASCII printable characters (32-126)
        for (int i = 32; i <= 126; i++)
        {
            characters.Add((char)i);
        }

        // Common extended characters
        var extendedChars = "ÀÁÂÃÄÅÆÇÈÉÊËÌÍÎÏÐÑÒÓÔÕÖØÙÚÛÜÝÞßàáâãäåæçèéêëìíîïðñòóôõöøùúûüýþÿ";
        foreach (char c in extendedChars)
        {
            characters.Add(c);
        }

        return characters.ToArray();
    }

    private static string[] GetSystemFontPaths()
    {
        // Platform-specific font paths
        if (OSInfo.IsWindows)
        {
            return
            [
                @"C:\Windows\Fonts\arial.ttf",
                @"C:\Windows\Fonts\calibri.ttf"
            ];
        }
        else
        {
            if (OSInfo.IsLinux)
            {
                return
                [
                    "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
                    "/usr/share/fonts/truetype/liberation/LiberationSans-Regular.ttf"
                ];
            }

            if (OSInfo.IsMacOS)
            {
                return
                [
                    "/System/Library/Fonts/NewYork.ttf",
                    "/System/Library/Fonts/Helvetica.ttc"
                ];
            }
        }

        return [];
    }
    
    private Font CreateFallbackFont()
    {
        // Create a minimal fallback font with basic characters
        var font = new Font("Fallback", 16.0f)
        {
            LineHeight = 20.0f,
            Ascender = 16.0f,
            Descender = -4.0f
        };
        
        // Create a simple 1x1 white texture for all characters
        var atlasData = new byte[4] { 255, 255, 255, 255 }; // Single white pixel
        font.AtlasTexture = CreateTextureFromData(atlasData, 1, 1);
        
        // Create basic glyphs for common characters
        var basicChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 .,!?";
        foreach (char c in basicChars)
        {
            var glyph = new Glyph(c)
            {
                Size = new Vector2(8, 16), // Fixed size for fallback
                Bearing = new Vector2(0, 16),
                Advance = 8,
                AtlasPosition = Vector2.Zero,
                AtlasSize = new Vector2(1, 1)
            };
            
            glyph.CalculateTextureCoords(1, 1);
            font.Glyphs[c] = glyph;
        }
        
        return font;
    }
}