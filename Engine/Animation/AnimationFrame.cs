using System.Numerics;

namespace Engine.Animation;

/// <summary>
/// Represents a single frame in an animation clip.
/// </summary>
public record AnimationFrame
{
    /// <summary>
    /// Pixel rectangle [x, y, width, height] in texture atlas.
    /// Origin: bottom-left (OpenGL convention).
    /// </summary>
    public required Rectangle Rect { get; init; }

    /// <summary>
    /// Normalized pivot point [0..1] relative to frame rectangle.
    /// Default: [0.5, 0.0] (bottom-center)
    /// </summary>
    public required Vector2 Pivot { get; init; }

    /// <summary>
    /// Flip flags [flipH, flipV] for horizontal and vertical mirroring.
    /// </summary>
    public Vector2? Flip { get; init; }

    /// <summary>
    /// Rotation in degrees (clockwise positive).
    /// </summary>
    public float? Rotation { get; init; }

    /// <summary>
    /// Per-frame scale multiplier [scaleX, scaleY].
    /// </summary>
    public Vector2 Scale { get; init; } = Vector2.One;

    /// <summary>
    /// List of event names to fire when entering this frame.
    /// </summary>
    public string[] Events { get; init; } = [];

    /// <summary>
    /// Pre-calculated UV coordinates for this frame (4 vertices).
    /// Calculated during asset load to avoid runtime cost.
    /// Order: [bottom-left, bottom-right, top-right, top-left]
    /// </summary>
    public Vector2[] TexCoords { get; init; } = new Vector2[4];

    /// <summary>
    /// Calculates UV coordinates from pixel rect and texture size.
    /// Applies flip flags during calculation.
    /// NOTE: Rect uses pixel coordinates, not grid coordinates.
    /// </summary>
    public void CalculateUvCoords(int atlasWidth, int atlasHeight)
    {
        // Convert pixel rect to normalized UV coordinates (0..1 range)
        // Rect.X and Rect.Y are in pixels
        float uvMinX = Rect.X / (float)atlasWidth;
        float uvMaxX = (Rect.X + Rect.Width) / (float)atlasWidth;

        float uvMinY = Rect.Y / (float)atlasHeight;
        float uvMaxY = (Rect.Y + Rect.Height) / (float)atlasHeight;

        // Base UV coordinates (counter-clockwise from bottom-left)
        TexCoords[0] = new Vector2(uvMinX, uvMinY);  // Bottom-left
        TexCoords[1] = new Vector2(uvMaxX, uvMinY);  // Bottom-right
        TexCoords[2] = new Vector2(uvMaxX, uvMaxY);  // Top-right
        TexCoords[3] = new Vector2(uvMinX, uvMaxY);  // Top-left

        // Apply flip flags
        if (Flip?.X > 0.5f) // Horizontal flip
        {
            // Swap X coordinates
            (TexCoords[0].X, TexCoords[1].X) = (TexCoords[1].X, TexCoords[0].X);
            (TexCoords[2].X, TexCoords[3].X) = (TexCoords[3].X, TexCoords[2].X);
        }

        if (Flip?.Y > 0.5f) // Vertical flip
        {
            // Swap Y coordinates
            (TexCoords[0].Y, TexCoords[3].Y) = (TexCoords[3].Y, TexCoords[0].Y);
            (TexCoords[1].Y, TexCoords[2].Y) = (TexCoords[2].Y, TexCoords[1].Y);
        }
    }
}