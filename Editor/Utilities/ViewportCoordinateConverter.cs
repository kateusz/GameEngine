using System.Numerics;
using Engine.Renderer.Cameras;

namespace Editor.Utilities;

/// <summary>
/// Helper class for converting between screen space and world space coordinates in the editor viewport.
/// </summary>
public static class ViewportCoordinateConverter
{
    /// <summary>
    /// Converts screen coordinates (pixels) to world coordinates.
    /// </summary>
    /// <param name="screenPos">Screen position in pixels</param>
    /// <param name="viewportBounds">Viewport bounds [min, max] in screen space</param>
    /// <param name="camera">Orthographic camera</param>
    /// <returns>World position</returns>
    public static Vector2 ScreenToWorld(Vector2 screenPos, Vector2[] viewportBounds, OrthographicCamera camera)
    {
        // Calculate viewport dimensions
        var viewportMin = viewportBounds[0];
        var viewportMax = viewportBounds[1];
        var viewportSize = viewportMax - viewportMin;
        
        // Convert screen position to normalized viewport coordinates (0-1)
        var normalizedX = (screenPos.X - viewportMin.X) / viewportSize.X;
        var normalizedY = (screenPos.Y - viewportMin.Y) / viewportSize.Y;
        
        // Flip Y axis (screen space Y is inverted compared to world space)
        normalizedY = 1.0f - normalizedY;
        
        // Convert normalized coordinates to NDC space (-1 to 1)
        var ndcX = normalizedX * 2.0f - 1.0f;
        var ndcY = normalizedY * 2.0f - 1.0f;
        
        // Create NDC position (z = 0 for 2D)
        var ndcPos = new Vector4(ndcX, ndcY, 0.0f, 1.0f);
        
        // Get inverse view-projection matrix
        Matrix4x4.Invert(camera.ViewProjectionMatrix, out var invViewProj);
        
        // Transform from NDC to world space
        var worldPos4 = Vector4.Transform(ndcPos, invViewProj);
        
        // Perspective divide (not really needed for orthographic, but good practice)
        if (Math.Abs(worldPos4.W) > 0.0001f)
        {
            worldPos4 /= worldPos4.W;
        }
        
        return new Vector2(worldPos4.X, worldPos4.Y);
    }
    
    /// <summary>
    /// Converts world coordinates to screen coordinates (pixels).
    /// </summary>
    /// <param name="worldPos">World position</param>
    /// <param name="viewportBounds">Viewport bounds [min, max] in screen space</param>
    /// <param name="camera">Orthographic camera</param>
    /// <returns>Screen position in pixels</returns>
    public static Vector2 WorldToScreen(Vector2 worldPos, Vector2[] viewportBounds, OrthographicCamera camera)
    {
        // Create world position as Vector4
        var worldPos4 = new Vector4(worldPos.X, worldPos.Y, 0.0f, 1.0f);
        
        // Transform to clip space
        var clipPos = Vector4.Transform(worldPos4, camera.ViewProjectionMatrix);
        
        // Perspective divide
        if (Math.Abs(clipPos.W) > 0.0001f)
        {
            clipPos /= clipPos.W;
        }
        
        // Convert from NDC (-1 to 1) to normalized viewport coordinates (0 to 1)
        var normalizedX = (clipPos.X + 1.0f) * 0.5f;
        var normalizedY = (clipPos.Y + 1.0f) * 0.5f;
        
        // Flip Y axis
        normalizedY = 1.0f - normalizedY;
        
        // Convert to screen coordinates
        var viewportMin = viewportBounds[0];
        var viewportMax = viewportBounds[1];
        var viewportSize = viewportMax - viewportMin;
        
        var screenX = viewportMin.X + normalizedX * viewportSize.X;
        var screenY = viewportMin.Y + normalizedY * viewportSize.Y;
        
        return new Vector2(screenX, screenY);
    }
}

