using System.Numerics;

namespace Engine.Scene.Cameras;

/// <summary>
/// Window/surface screen → world conversion for 2D (Z=0 plane).
/// </summary>
public static class ScreenWorldConverter
{
    /// <param name="windowPosition">Cursor in window/logical pixels.</param>
    /// <param name="surfaceOrigin">Game view origin in the same space.</param>
    /// <param name="surfaceSize">Game view size in the same space.</param>
    /// <param name="viewProjectionMatrix">Camera view * projection (same as the renderer).</param>
    public static Vector2? ScreenToWorld2D(
        Vector2 windowPosition,
        Vector2 surfaceOrigin,
        Vector2 surfaceSize,
        Matrix4x4 viewProjectionMatrix)
    {
        if (surfaceSize.X <= 0f || surfaceSize.Y <= 0f)
            return null;

        var local = windowPosition - surfaceOrigin;
        var normalizedX = local.X / surfaceSize.X;
        var normalizedY = local.Y / surfaceSize.Y;
        // Flip Y (framebuffer / ImGui convention)
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

        if (MathF.Abs(rayDir.Z) < 0.0001f)
            return null;

        var t = -rayOrigin.Z / rayDir.Z;
        var world = rayOrigin + rayDir * t;
        return new Vector2(world.X, world.Y);
    }
}
