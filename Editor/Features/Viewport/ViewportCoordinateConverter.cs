using System.Numerics;
using Engine.Scene.Cameras;

namespace Editor.Features.Viewport;

/// <summary>
/// Editor viewport wrappers around <see cref="ScreenWorldConverter"/> plus world→screen for gizmos.
/// </summary>
public static class ViewportCoordinateConverter
{
    /// <param name="screenPos">Mouse position relative to viewport origin (logical pixels).</param>
    public static Vector3? ScreenToWorld(
        Vector2 screenPos,
        Vector2[] viewportBounds,
        Matrix4x4 viewProjectionMatrix)
    {
        var world = ScreenToWorld2D(screenPos, viewportBounds, viewProjectionMatrix);
        return world is { } w ? new Vector3(w.X, w.Y, 0f) : null;
    }

    public static Vector2? ScreenToWorld2D(
        Vector2 screenPos,
        Vector2[] viewportBounds,
        Matrix4x4 viewProjectionMatrix)
    {
        var origin = viewportBounds[0];
        var size = viewportBounds[1] - origin;
        // Tools pass viewport-local coords; converter expects window-space.
        return ScreenWorldConverter.ScreenToWorld2D(screenPos + origin, origin, size, viewProjectionMatrix);
    }

    /// <summary>
    /// Converts a world-space position to viewport-local screen coordinates (global ImGui coordinates).
    /// </summary>
    public static Vector2 WorldToScreen(
        Vector3 worldPos,
        Vector2[] viewportBounds,
        Matrix4x4 viewProjectionMatrix)
    {
        var clipPos = Vector4.Transform(new Vector4(worldPos, 1.0f), viewProjectionMatrix);

        if (MathF.Abs(clipPos.W) > 0.0001f)
            clipPos /= clipPos.W;

        var normalizedX = (clipPos.X + 1.0f) * 0.5f;
        var normalizedY = (clipPos.Y + 1.0f) * 0.5f;
        normalizedY = 1.0f - normalizedY;

        var viewportSize = viewportBounds[1] - viewportBounds[0];
        return new Vector2(
            viewportBounds[0].X + normalizedX * viewportSize.X,
            viewportBounds[0].Y + normalizedY * viewportSize.Y);
    }
}
