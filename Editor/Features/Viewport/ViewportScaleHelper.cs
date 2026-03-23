namespace Editor.Features.Viewport;

/// <summary>
/// Shared coordinate math for viewport overlays (ruler, grid).
/// </summary>
public static class ViewportScaleHelper
{
    private const float TargetPixelSpacing = 50.0f;

    /// <summary>
    /// Calculates a "nice" tick interval (1, 2, 5, 10, ...) such that
    /// ticks appear roughly <see cref="TargetPixelSpacing"/> pixels apart at the given zoom.
    /// </summary>
    public static float CalculateTickSpacing(float zoom)
    {
        if (zoom <= 0)
            return TargetPixelSpacing;

        var rawSpacing = TargetPixelSpacing / zoom;
        var magnitude = (float)Math.Pow(10, Math.Floor(Math.Log10(rawSpacing)));
        var normalizedSpacing = rawSpacing / magnitude;

        float niceSpacing;
        if (normalizedSpacing < 2.0f)
            niceSpacing = 1.0f;
        else if (normalizedSpacing < 5.0f)
            niceSpacing = 2.0f;
        else
            niceSpacing = 5.0f;

        return niceSpacing * magnitude;
    }

    /// <summary>
    /// Maps a world X coordinate to a screen X position within the viewport.
    /// </summary>
    public static float WorldToScreenX(float worldX, float cameraX, float zoom,
        float viewportMinX, float viewportWidth)
    {
        if (zoom <= 0)
            return viewportMinX + viewportWidth / 2.0f;

        var worldWidth = viewportWidth / zoom;
        var worldLeft = cameraX - worldWidth / 2.0f;
        var normalizedX = (worldX - worldLeft) / worldWidth;
        return viewportMinX + normalizedX * viewportWidth;
    }

    /// <summary>
    /// Maps a world Y coordinate to a screen Y position within the viewport.
    /// </summary>
    public static float WorldToScreenY(float worldY, float cameraY, float zoom,
        float viewportMinY, float viewportHeight)
    {
        if (zoom <= 0)
            return viewportMinY + viewportHeight / 2.0f;

        var worldHeight = viewportHeight / zoom;
        var worldTop = cameraY + worldHeight / 2.0f;
        var normalizedY = (worldTop - worldY) / worldHeight;
        return viewportMinY + normalizedY * viewportHeight;
    }
}
