namespace Engine.Animation;

/// <summary>
/// Represents a rectangular region in pixel coordinates.
/// Used for defining sprite frame boundaries in texture atlases.
/// </summary>
/// <param name="X">X coordinate of top-left corner in pixels</param>
/// <param name="Y">Y coordinate of top-left corner in pixels</param>
/// <param name="Width">Width of the rectangle in pixels</param>
/// <param name="Height">Height of the rectangle in pixels</param>
public record struct Rectangle(int X, int Y, int Width, int Height);