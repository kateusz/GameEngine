using System.Numerics;
using Engine.Renderer;
using Engine.Renderer.Meshes;
using Engine.Renderer.Pipeline;

namespace Engine.Scene;

internal static class MeshLod
{
    internal static int Select(
        Mesh mesh, Matrix4x4 world, Vector3 camera, in SceneView view, float bias)
    {
        int count = mesh.LodCount;
        if (count <= 1)
            return 0;
        if (mesh.Bounds is not Aabb box)
            return 0;
        if (view.ViewportHeight <= 0f || !float.IsFinite(view.FrustumHeightAtUnit) || view.FrustumHeightAtUnit <= 0f)
            return 0;

        float scale = MaxAxisScale(world);
        if (!float.IsFinite(scale) || scale <= 0f)
            return 0;

        float distanceSq = view.Orthographic
            ? 1f
            : Aabb.ClosestPointDistanceSquared(camera, world, box);
        if (!float.IsFinite(distanceSq))
            return 0;

        float factor = view.FrustumHeightAtUnit * (1f / bias) / view.ViewportHeight;
        float limitSq = distanceSq * factor * factor;
        int chosen = 0;
        for (int level = 1; level < count; level++)
        {
            float worldError = mesh.LodError(level) * scale;
            if (worldError * worldError <= limitSq)
                chosen = level;
            else
                break;
        }
        return chosen;
    }

    // System.Numerics uses row vectors (v * M); local-axis scales are row lengths.
    private static float MaxAxisScale(Matrix4x4 world)
    {
        var x = MathF.Sqrt(world.M11 * world.M11 + world.M12 * world.M12 + world.M13 * world.M13);
        var y = MathF.Sqrt(world.M21 * world.M21 + world.M22 * world.M22 + world.M23 * world.M23);
        var z = MathF.Sqrt(world.M31 * world.M31 + world.M32 * world.M32 + world.M33 * world.M33);
        return MathF.Max(x, MathF.Max(y, z));
    }
}
