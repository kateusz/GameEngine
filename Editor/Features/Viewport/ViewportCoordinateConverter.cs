using System.Numerics;
using Engine.Renderer.Cameras;

namespace Editor.Features.Viewport;

/// <summary>
/// Helper class for converting between screen space and world space coordinates in the editor viewport.
/// </summary>
public static class ViewportCoordinateConverter
{
    /// <summary>
    /// Converts screen coordinates (pixels) to world coordinates.
    /// </summary>
    /// <param name="screenPos">Screen position in pixels (LOCAL to the viewport: [0..viewportWidth], [0..viewportHeight])</param>
    /// <param name="viewportBounds">Viewport bounds [min, max] in screen space</param>
    /// <param name="camera">Orthographic camera</param>
    /// <returns>World position</returns>
    public static Vector2 ScreenToWorld(Vector2 screenPos, Vector2[] viewportBounds, OrthographicCamera camera)
    {
        // Calculate viewport dimensions
        var viewportMin = viewportBounds[0];
        var viewportMax = viewportBounds[1];
        var viewportSize = viewportMax - viewportMin;

        // screenPos is expected to be LOCAL to the viewport (origin at top-left of the viewport image).
        // Convert screen position to normalized viewport coordinates (0-1)
        var normalizedX = screenPos.X / viewportSize.X;
        var normalizedY = screenPos.Y / viewportSize.Y;

        // The framebuffer texture is rendered into the ImGui "Viewport" using inverted UVs
        // (ImGui.Image(texture, size, new Vector2(0,1), new Vector2(1,0))). To map a mouse position on
        // the ImGui window to the camera's clip-space correctly we must invert the Y here so the top of
        // the ImGui region corresponds to the top of the framebuffer.
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
    /// <returns>Screen position in pixels (GLOBAL ImGui coordinates)</returns>
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
        
        // Invert Y to account for ImGui image UV flip so coordinates map to ImGui window space
        normalizedY = 1.0f - normalizedY;
        
        // Convert to screen coordinates (GLOBAL ImGui coords)
        var viewportMin = viewportBounds[0];
        var viewportMax = viewportBounds[1];
        var viewportSize = viewportMax - viewportMin;
        
        var screenX = viewportMin.X + normalizedX * viewportSize.X;
        var screenY = viewportMin.Y + normalizedY * viewportSize.Y;
        
        return new Vector2(screenX, screenY);
    }
}
