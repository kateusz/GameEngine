using System.Numerics;

namespace Engine.Renderer;

/// <summary>
/// Rotation-only inverse VP for equirect skybox ray reconstruction (no cube mesh).
/// </summary>
internal static class SkyboxMath
{
    public static bool TryInvertRotationViewProjection(Matrix4x4 view, Matrix4x4 projection, out Matrix4x4 inverseVp)
    {
        var rotView = view;
        rotView.M41 = 0f;
        rotView.M42 = 0f;
        rotView.M43 = 0f;
        return Matrix4x4.Invert(rotView * projection, out inverseVp);
    }

    /// <summary>
    /// World direction for an NDC pixel (row-vector: clip * invVP). Matches skyboxShader.vert.
    /// </summary>
    public static Vector3 DirectionFromNdc(Matrix4x4 inverseVp, float ndcX, float ndcY)
    {
        var world = Vector4.Transform(new Vector4(ndcX, ndcY, 1f, 1f), inverseVp);
        var w = System.MathF.Max(world.W, 1e-6f);
        return Vector3.Normalize(new Vector3(world.X / w, world.Y / w, world.Z / w));
    }
}
