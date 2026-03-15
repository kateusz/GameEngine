using System.Numerics;

namespace Editor.Features.Viewport;

/// <summary>
/// Converts between screen space and world space coordinates in the editor viewport.
/// Works with any camera type via the view-projection matrix.
/// </summary>
public static class ViewportCoordinateConverter
{
    /// <summary>
    /// Converts viewport-local screen coordinates to world space.
    /// Intersects the unprojected ray with the Z=0 plane for 2D operations.
    /// </summary>
    /// <param name="screenPos">Mouse position relative to viewport origin (logical pixels).</param>
    /// <param name="viewportBounds">Viewport [min, max] bounds in screen space.</param>
    /// <param name="viewProjectionMatrix">Camera's combined view-projection matrix.</param>
    public static Vector3? ScreenToWorld(
        Vector2 screenPos,
        Vector2[] viewportBounds,
        Matrix4x4 viewProjectionMatrix)
    {
        var viewportSize = viewportBounds[1] - viewportBounds[0];
        if (viewportSize.X <= 0 || viewportSize.Y <= 0)
            return null;

        var normalizedX = screenPos.X / viewportSize.X;
        var normalizedY = screenPos.Y / viewportSize.Y;
        // Flip Y (ImGui UV inversion)
        normalizedY = 1.0f - normalizedY;

        var ndcX = normalizedX * 2.0f - 1.0f;
        var ndcY = normalizedY * 2.0f - 1.0f;

        if (!Matrix4x4.Invert(viewProjectionMatrix, out var invVP))
            return null;

        var nearPoint4 = Vector4.Transform(new Vector4(ndcX, ndcY, -1.0f, 1.0f), invVP);
        var farPoint4 = Vector4.Transform(new Vector4(ndcX, ndcY, 1.0f, 1.0f), invVP);

        if (MathF.Abs(nearPoint4.W) > 0.0001f) nearPoint4 /= nearPoint4.W;
        if (MathF.Abs(farPoint4.W) > 0.0001f) farPoint4 /= farPoint4.W;

        var rayOrigin = new Vector3(nearPoint4.X, nearPoint4.Y, nearPoint4.Z);
        var rayEnd = new Vector3(farPoint4.X, farPoint4.Y, farPoint4.Z);
        var rayDir = rayEnd - rayOrigin;

        // Ray is parallel to Z=0 plane — no intersection
        if (MathF.Abs(rayDir.Z) < 0.0001f)
            return null;

        var t = -rayOrigin.Z / rayDir.Z;
        return rayOrigin + rayDir * t;
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

        // Flip Y for ImGui
        normalizedY = 1.0f - normalizedY;

        var viewportSize = viewportBounds[1] - viewportBounds[0];
        return new Vector2(
            viewportBounds[0].X + normalizedX * viewportSize.X,
            viewportBounds[0].Y + normalizedY * viewportSize.Y);
    }

    /// <summary>
    /// 2D convenience overload — returns XY on the Z=0 plane.
    /// </summary>
    public static Vector2? ScreenToWorld2D(
        Vector2 screenPos,
        Vector2[] viewportBounds,
        Matrix4x4 viewProjectionMatrix)
    {
        var world3D = ScreenToWorld(screenPos, viewportBounds, viewProjectionMatrix);
        if (world3D is null) return null;
        return new Vector2(world3D.Value.X, world3D.Value.Y);
    }
}
