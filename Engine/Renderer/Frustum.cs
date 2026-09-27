using System.Numerics;

namespace Engine.Renderer;

internal readonly struct Frustum
{
    private const float NormalEpsilon = 1e-8f;
    private const float OutsideEpsilon = 1e-4f;

    private readonly Plane[] _planes;

    private Frustum(Plane[] planes) => _planes = planes;

    public static bool TryFromClip(Matrix4x4 clip, out Frustum frustum)
    {
        frustum = default;
        var planes = new Plane[6];
        if (!TryPlane(clip.M11 + clip.M14, clip.M21 + clip.M24, clip.M31 + clip.M34, clip.M41 + clip.M44, out planes[0])
            || !TryPlane(clip.M14 - clip.M11, clip.M24 - clip.M21, clip.M34 - clip.M31, clip.M44 - clip.M41, out planes[1])
            || !TryPlane(clip.M12 + clip.M14, clip.M22 + clip.M24, clip.M32 + clip.M34, clip.M42 + clip.M44, out planes[2])
            || !TryPlane(clip.M14 - clip.M12, clip.M24 - clip.M22, clip.M34 - clip.M32, clip.M44 - clip.M42, out planes[3])
            || !TryPlane(clip.M13, clip.M23, clip.M33, clip.M43, out planes[4])
            || !TryPlane(clip.M14 - clip.M13, clip.M24 - clip.M23, clip.M34 - clip.M33, clip.M44 - clip.M43, out planes[5]))
            return false;

        frustum = new Frustum(planes);
        return true;
    }

    public bool IsOutside(Matrix4x4 world, Aabb bounds)
    {
        Span<Vector3> corners = stackalloc Vector3[8];
        Aabb.TransformCorners(world, bounds, corners);
        foreach (var transformed in corners)
        {
            if (!float.IsFinite(transformed.X) || !float.IsFinite(transformed.Y) || !float.IsFinite(transformed.Z))
                return false;
        }

        foreach (var plane in _planes)
        {
            var outside = true;
            foreach (var point in corners)
            {
                if (Plane.DotCoordinate(plane, point) >= -OutsideEpsilon)
                {
                    outside = false;
                    break;
                }
            }

            if (outside)
                return true;
        }

        return false;
    }

    private static bool TryPlane(float x, float y, float z, float d, out Plane plane)
    {
        var length = MathF.Sqrt(x * x + y * y + z * z);
        if (length <= NormalEpsilon)
        {
            plane = default;
            return false;
        }

        var inverse = 1f / length;
        plane = new Plane(new Vector3(x * inverse, y * inverse, z * inverse), d * inverse);
        return true;
    }
}
