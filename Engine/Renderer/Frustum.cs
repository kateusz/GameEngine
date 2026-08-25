using System.Numerics;

namespace Engine.Renderer;

public readonly record struct Aabb(Vector3 Min, Vector3 Max)
{
    public bool IsFinite =>
        float.IsFinite(Min.X) && float.IsFinite(Min.Y) && float.IsFinite(Min.Z)
        && float.IsFinite(Max.X) && float.IsFinite(Max.Y) && float.IsFinite(Max.Z);

    public Aabb Transform(in Matrix4x4 world)
    {
        var min = new Vector3(float.PositiveInfinity);
        var max = new Vector3(float.NegativeInfinity);
        for (var i = 0; i < 8; i++)
        {
            var corner = new Vector3(
                (i & 1) == 0 ? Min.X : Max.X,
                (i & 2) == 0 ? Min.Y : Max.Y,
                (i & 4) == 0 ? Min.Z : Max.Z);
            var transformed = Vector3.Transform(corner, world);
            min = Vector3.Min(min, transformed);
            max = Vector3.Max(max, transformed);
        }

        return new Aabb(min, max);
    }
}

public readonly record struct Frustum(Plane Left, Plane Right, Plane Bottom, Plane Top, Plane Near, Plane Far)
{
    public static Frustum FromViewProjection(in Matrix4x4 viewProjection)
    {
        if (!IsFinite(viewProjection))
            return default;

        // Clip = position * matrix (same as the 3D shaders). OpenGL clip volume: -w <= x,y,z <= w.
        var m = viewProjection;
        return new Frustum(
            MakePlane(m.M14 + m.M11, m.M24 + m.M21, m.M34 + m.M31, m.M44 + m.M41),
            MakePlane(m.M14 - m.M11, m.M24 - m.M21, m.M34 - m.M31, m.M44 - m.M41),
            MakePlane(m.M14 + m.M12, m.M24 + m.M22, m.M34 + m.M32, m.M44 + m.M42),
            MakePlane(m.M14 - m.M12, m.M24 - m.M22, m.M34 - m.M32, m.M44 - m.M42),
            MakePlane(m.M14 + m.M13, m.M24 + m.M23, m.M34 + m.M33, m.M44 + m.M43),
            MakePlane(m.M14 - m.M13, m.M24 - m.M23, m.M34 - m.M33, m.M44 - m.M43));
    }

    public bool Intersects(in Aabb world) =>
        !Outside(Left, world)
        && !Outside(Right, world)
        && !Outside(Bottom, world)
        && !Outside(Top, world)
        && !Outside(Near, world)
        && !Outside(Far, world);

    private static bool Outside(in Plane plane, in Aabb box)
    {
        var n = plane.Normal;
        var p = new Vector3(
            n.X >= 0 ? box.Max.X : box.Min.X,
            n.Y >= 0 ? box.Max.Y : box.Min.Y,
            n.Z >= 0 ? box.Max.Z : box.Min.Z);
        return Plane.DotCoordinate(plane, p) < 0;
    }

    private static Plane MakePlane(float a, float b, float c, float d)
    {
        var length = MathF.Sqrt(a * a + b * b + c * c);
        if (length < 1e-8f)
            return new Plane(0, 0, 0, 1);

        return new Plane(a / length, b / length, c / length, d / length);
    }

    private static bool IsFinite(in Matrix4x4 m) =>
        float.IsFinite(m.M11) && float.IsFinite(m.M12) && float.IsFinite(m.M13) && float.IsFinite(m.M14)
        && float.IsFinite(m.M21) && float.IsFinite(m.M22) && float.IsFinite(m.M23) && float.IsFinite(m.M24)
        && float.IsFinite(m.M31) && float.IsFinite(m.M32) && float.IsFinite(m.M33) && float.IsFinite(m.M34)
        && float.IsFinite(m.M41) && float.IsFinite(m.M42) && float.IsFinite(m.M43) && float.IsFinite(m.M44);
}
