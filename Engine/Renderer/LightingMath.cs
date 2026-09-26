using System.Numerics;

namespace Engine.Renderer;

internal static class LightingMath
{
    private const float DirectionEpsilon = 1e-6f;
    public static readonly Vector3 DefaultDirection = new(0, -1, 0);
    public const int MaxPointLights = 8;
    public const float PointLightDistanceEpsilon = 0.0001f;

    public static Vector3 NormalizeDirection(Vector3 direction) =>
        direction.LengthSquared() < DirectionEpsilon ? DefaultDirection : Vector3.Normalize(direction);
    
    public static float PointAttenuation(float distance, float range)
    {
        if (range <= 0f || distance >= range)
            return 0f;
        var remaining = 1f - distance / range;
        return remaining * remaining;
    }

    public const int ShadowMapResolution = 1024;

    // ponytail: fit only this far in front of the near plane. Ceiling: casters beyond it do not shadow the view.
    // The editor far plane is 1000. Fitting that makes one texel ~1 unit and turns the 0.002 window bias into ~4 world
    // units, which erases contact shadows and pops them when the camera crosses a texel. Upgrade path: cascades.
    public const float ShadowDistance = 50f;
    private const float ShadowExtentEpsilon = 1e-4f;
    private const float ShadowUpParallel = 0.99f;

    public static bool TryFitDirectionalShadow(
        Matrix4x4 cameraViewProjection,
        Vector3 lightDirection,
        out Matrix4x4 lightViewProjection)
    {
        lightViewProjection = Matrix4x4.Identity;
        if (!Matrix4x4.Invert(cameraViewProjection, out var inverseViewProjection))
            return false;

        Span<Vector3> corners = stackalloc Vector3[8];
        var corner = 0;
        for (var z = 0f; z <= 1f; z += 1f)
        for (var y = -1f; y <= 1f; y += 2f)
        for (var x = -1f; x <= 1f; x += 2f)
        {
            if (!TryUnproject(inverseViewProjection, x, y, z, out corners[corner]))
                return false;
            corner++;
        }

        if (!TryUnproject(inverseViewProjection, 0f, 0f, 0f, out var nearCenter) ||
            !TryUnproject(inverseViewProjection, 0f, 0f, 1f, out var farCenter))
            return false;

        var depth = Vector3.Distance(nearCenter, farCenter);
        if (depth > ShadowDistance)
        {
            var keep = ShadowDistance / depth;
            for (var i = 0; i < 4; i++)
                corners[i + 4] = corners[i] + (corners[i + 4] - corners[i]) * keep;
        }

        var direction = NormalizeDirection(lightDirection);
        var up = MathF.Abs(Vector3.Dot(direction, Vector3.UnitY)) > ShadowUpParallel
            ? Vector3.UnitZ
            : Vector3.UnitY;
        var lightView = Matrix4x4.CreateLookAt(Vector3.Zero, direction, up);

        var min = new Vector3(float.PositiveInfinity);
        var max = new Vector3(float.NegativeInfinity);
        foreach (var world in corners)
        {
            var lightSpace = Vector3.Transform(world, lightView);
            min = Vector3.Min(min, lightSpace);
            max = Vector3.Max(max, lightSpace);
        }

        if (max.X - min.X <= ShadowExtentEpsilon || max.Y - min.Y <= ShadowExtentEpsilon)
            return false;

        SnapAxis(ref min.X, ref max.X);
        SnapAxis(ref min.Y, ref max.Y);

        lightViewProjection = lightView * BuildLightOrtho(min, max);
        return true;
    }

    private static void SnapAxis(ref float min, ref float max)
    {
        var size = max - min;
        var texel = size / ShadowMapResolution;
        var center = MathF.Floor(((min + max) * 0.5f) / texel) * texel;
        var half = size * 0.5f;
        min = center - half;
        max = center + half;
    }

    private static Matrix4x4 BuildLightOrtho(Vector3 min, Vector3 max)
    {
        var ortho = Matrix4x4.Identity;
        ortho.M11 = 2f / (max.X - min.X);
        ortho.M22 = 2f / (max.Y - min.Y);
        ortho.M33 = 1f / (min.Z - max.Z);
        ortho.M41 = -1f - min.X * ortho.M11;
        ortho.M42 = -1f - min.Y * ortho.M22;
        ortho.M43 = -max.Z * ortho.M33;
        return ortho;
    }

    private static bool TryUnproject(Matrix4x4 inverseViewProjection, float x, float y, float z, out Vector3 world)
    {
        var clip = Vector4.Transform(new Vector4(x, y, z, 1f), inverseViewProjection);
        if (MathF.Abs(clip.W) < ShadowExtentEpsilon)
        {
            world = default;
            return false;
        }

        world = new Vector3(clip.X, clip.Y, clip.Z) / clip.W;
        return true;
    }
}