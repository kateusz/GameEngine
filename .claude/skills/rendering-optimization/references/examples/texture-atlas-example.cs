// Texture Atlas Implementation Example
// Combines multiple textures into single atlas to improve batching efficiency

using System.Numerics;
using Engine.Renderer;

namespace Engine.Optimization.Examples;

/// <summary>
/// Simple texture atlas for combining sprite textures.
/// Reduces texture switches and improves batch efficiency.
/// </summary>
public class TextureAtlas : IDisposable
{
    private readonly int _width;
    private readonly int _height;
    private readonly List<AtlasRegion> _regions = new();
    private Texture? _atlasTexture;
    private int _currentX = 0;
    private int _currentY = 0;
    private int _rowHeight = 0;

    public Texture Texture => _atlasTexture ?? throw new InvalidOperationException("Atlas not built");

    public TextureAtlas(int width, int height)
    {
        _width = width;
        _height = height;
    }

    /// <summary>
    /// Add texture to atlas. Call Build() after adding all textures.
    /// </summary>
    public void AddTexture(string name, Texture texture)
    {
        // Simple shelf-packing algorithm
        if (_currentX + texture.Width > _width)
        {
            // Move to next row
            _currentX = 0;
            _currentY += _rowHeight;
            _rowHeight = 0;
        }

        if (_currentY + texture.Height > _height)
        {
            throw new InvalidOperationException($"Atlas too small for texture '{name}'");
        }

        // Store region info
        _regions.Add(new AtlasRegion
        {
            Name = name,
            X = _currentX,
            Y = _currentY,
            Width = texture.Width,
            Height = texture.Height,
            Texture = texture
        });

        _currentX += texture.Width;
        _rowHeight = Math.Max(_rowHeight, texture.Height);
    }

    /// <summary>
    /// Build atlas texture from added textures.
    /// </summary>
    public void Build()
    {
        // Create empty atlas texture
        var pixels = new byte[_width * _height * 4]; // RGBA

        // Copy each texture into atlas
        foreach (var region in _regions)
        {
            CopyTextureToAtlas(region, pixels);
        }

        // Upload to GPU
        _atlasTexture = Texture.Create(_width, _height, pixels);
    }

    /// <summary>
    /// Get texture coordinates for a named region.
    /// </summary>
    public Vector4 GetRegion(string name)
    {
        var region = _regions.FirstOrDefault(r => r.Name == name)
            ?? throw new KeyNotFoundException($"Region '{name}' not found in atlas");

        // Return normalized UV coordinates (x, y, width, height)
        return new Vector4(
            (float)region.X / _width,
            (float)region.Y / _height,
            (float)region.Width / _width,
            (float)region.Height / _height
        );
    }

    private void CopyTextureToAtlas(AtlasRegion region, byte[] atlasPixels)
    {
        // Get source texture pixels
        var srcPixels = region.Texture.GetPixels();

        // Copy row by row
        for (int y = 0; y < region.Height; y++)
        {
            for (int x = 0; x < region.Width; x++)
            {
                int srcIndex = (y * region.Width + x) * 4;
                int dstIndex = ((region.Y + y) * _width + (region.X + x)) * 4;

                // Copy RGBA
                atlasPixels[dstIndex + 0] = srcPixels[srcIndex + 0]; // R
                atlasPixels[dstIndex + 1] = srcPixels[srcIndex + 1]; // G
                atlasPixels[dstIndex + 2] = srcPixels[srcIndex + 2]; // B
                atlasPixels[dstIndex + 3] = srcPixels[srcIndex + 3]; // A
            }
        }
    }

    public void Dispose()
    {
        _atlasTexture?.Dispose();
    }

    private class AtlasRegion
    {
        public required string Name { get; init; }
        public int X { get; init; }
        public int Y { get; init; }
        public int Width { get; init; }
        public int Height { get; init; }
        public required Texture Texture { get; init; }
    }
}

/// <summary>
/// Example usage in a sprite rendering system.
/// </summary>
public class OptimizedSpriteRenderer
{
    private readonly TextureAtlas _atlas;
    private readonly Graphics2D _graphics;

    public OptimizedSpriteRenderer(Graphics2D graphics)
    {
        _graphics = graphics;

        // Create atlas (2048x2048 fits many sprites)
        _atlas = new TextureAtlas(2048, 2048);

        // Load all sprite textures
        var playerTex = Texture.Load("assets/player.png");
        var enemyTex = Texture.Load("assets/enemy.png");
        var bulletTex = Texture.Load("assets/bullet.png");

        // Add to atlas
        _atlas.AddTexture("player", playerTex);
        _atlas.AddTexture("enemy", enemyTex);
        _atlas.AddTexture("bullet", bulletTex);

        // Build atlas
        _atlas.Build();

        // Original textures can now be disposed (data is in atlas)
        playerTex.Dispose();
        enemyTex.Dispose();
        bulletTex.Dispose();
    }

    public void RenderSprites(IEnumerable<Entity> entities)
    {
        // ✅ All sprites use same atlas texture - perfect batching!
        foreach (var entity in entities)
        {
            var sprite = entity.GetComponent<SpriteRendererComponent>();
            var transform = entity.GetComponent<TransformComponent>();

            // Get UV region for this sprite
            var region = _atlas.GetRegion(sprite.SpriteName);

            // Draw using atlas texture
            _graphics.DrawQuad(
                position: transform.Translation,
                size: transform.Scale,
                texture: _atlas.Texture,
                texCoords: region
            );
        }

        // Result: All sprites drawn in single batch (1 draw call)
        // Without atlas: 3+ draw calls (one per unique texture)
    }
}
