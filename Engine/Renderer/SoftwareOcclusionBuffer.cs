using System.Numerics;

namespace Engine.Renderer;

// ponytail: 90-row grid + 2000-triangle budget under-cull once a frame has more useful
// occluder triangles than that. Raise the budget, or lower the 0.02 screen fraction,
// before adding a simplified occluder mesh.
internal sealed class SoftwareOcclusionBuffer
{
    public const int Rows = 90;
    public const int MaxColumns = 256;
    public const float MinScreenFraction = 0.02f;
    public const int TriangleBudget = 2000;
    public const float DepthSlack = 1e-5f;
    private const float MinClipW = 1e-8f;

    private float[] _cells = [];
    private Matrix4x4 _viewProjection;
    private int _columns;
    private int _rows;
    private int _submittedTriangles;
    private bool _active;

    public bool IsActive => _active;
    public int SubmittedTriangles => _submittedTriangles;

    public bool TryBegin(Matrix4x4 viewProjection)
    {
        _active = false;
        _submittedTriangles = 0;
        if (!TryAspect(viewProjection, out var aspect))
            return false;

        _columns = System.Math.Clamp((int)MathF.Round(Rows * aspect), 1, MaxColumns);
        _rows = Rows;
        var count = _columns * _rows;
        if (_cells.Length < count)
            _cells = new float[count];
        Array.Fill(_cells, float.PositiveInfinity, 0, count);
        _viewProjection = viewProjection;
        _active = true;
        return true;
    }

    public void End() => _active = false;

    public bool ScreenFractionAtLeastMinimum(Matrix4x4 world, Aabb local)
    {
        if (!TryProjectBox(world, local, out var ndc, out _))
            return true;

        var min = new Vector2(float.PositiveInfinity);
        var max = new Vector2(float.NegativeInfinity);
        foreach (var point in ndc)
        {
            min = Vector2.Min(min, new Vector2(point.X, point.Y));
            max = Vector2.Max(max, new Vector2(point.X, point.Y));
        }

        var width = MathF.Max(0f, max.X - min.X);
        var height = MathF.Max(0f, max.Y - min.Y);
        return width * height / 4f >= MinScreenFraction;
    }

    /// <summary>Nearest projected corner depth, or false when the box does not fully project.</summary>
    public bool TryNearestDepth(Matrix4x4 world, Aabb local, out float nearest) =>
        TryProjectBox(world, local, out _, out nearest);

    public bool IsOccluded(Matrix4x4 world, Aabb local)
    {
        if (!_active || !TryProjectBox(world, local, out var ndc, out var nearest))
            return false;

        PixelRect(ndc, out var x0, out var y0, out var x1, out var y1);
        if (x1 <= x0 || y1 <= y0)
            return false;

        for (var y = y0; y < y1; y++)
        for (var x = x0; x < x1; x++)
        {
            var stored = _cells[y * _columns + x];
            if (!float.IsFinite(stored) || nearest <= stored + DepthSlack)
                return false;
        }

        return true;
    }

    public void RasterizeTriangle(Vector3 world0, Vector3 world1, Vector3 world2)
    {
        if (!_active || _submittedTriangles >= TriangleBudget)
            return;
        _submittedTriangles++;
        if (!TryProject(_viewProjection, world0, out var a)
            || !TryProject(_viewProjection, world1, out var b)
            || !TryProject(_viewProjection, world2, out var c)
            || a.Z < 0f || b.Z < 0f || c.Z < 0f)
            return;

        var far = MathF.Max(a.Z, MathF.Max(b.Z, c.Z));
        Span<Vector3> triangle = [a, b, c];
        PixelRect(triangle, out var x0, out var y0, out var x1, out var y1);
        for (var y = y0; y < y1; y++)
        for (var x = x0; x < x1; x++)
        {
            if (!CellFullyInside(x, y, a, b, c))
                continue;
            var index = y * _columns + x;
            if (far < _cells[index])
                _cells[index] = far;
        }
    }

    private bool TryProjectBox(Matrix4x4 world, Aabb local, out Vector3[] ndc, out float nearest)
    {
        ndc = new Vector3[8];
        nearest = float.PositiveInfinity;
        Span<Vector3> corners = stackalloc Vector3[8];
        Aabb.TransformCorners(world, local, corners);
        for (var i = 0; i < 8; i++)
        {
            if (!TryProject(_viewProjection, corners[i], out ndc[i]) || ndc[i].Z < 0f)
            {
                ndc = [];
                nearest = 0f;
                return false;
            }

            nearest = MathF.Min(nearest, ndc[i].Z);
        }

        return true;
    }

    private void PixelRect(ReadOnlySpan<Vector3> ndc, out int x0, out int y0, out int x1, out int y1)
    {
        var minX = float.PositiveInfinity;
        var minY = float.PositiveInfinity;
        var maxX = float.NegativeInfinity;
        var maxY = float.NegativeInfinity;
        foreach (var point in ndc)
        {
            var px = (point.X * 0.5f + 0.5f) * _columns;
            var py = (point.Y * 0.5f + 0.5f) * _rows;
            minX = MathF.Min(minX, px);
            minY = MathF.Min(minY, py);
            maxX = MathF.Max(maxX, px);
            maxY = MathF.Max(maxY, py);
        }

        x0 = System.Math.Clamp((int)MathF.Floor(minX), 0, _columns);
        y0 = System.Math.Clamp((int)MathF.Floor(minY), 0, _rows);
        x1 = System.Math.Clamp((int)MathF.Ceiling(maxX), 0, _columns);
        y1 = System.Math.Clamp((int)MathF.Ceiling(maxY), 0, _rows);
    }

    private bool CellFullyInside(int x, int y, Vector3 a, Vector3 b, Vector3 c)
    {
        var x0 = x / (float)_columns * 2f - 1f;
        var x1 = (x + 1) / (float)_columns * 2f - 1f;
        var y0 = y / (float)_rows * 2f - 1f;
        var y1 = (y + 1) / (float)_rows * 2f - 1f;
        return BaryInside(x0, y0, a, b, c)
               && BaryInside(x1, y0, a, b, c)
               && BaryInside(x0, y1, a, b, c)
               && BaryInside(x1, y1, a, b, c);
    }

    private static bool BaryInside(float px, float py, Vector3 a, Vector3 b, Vector3 c)
    {
        var v0x = c.X - a.X;
        var v0y = c.Y - a.Y;
        var v1x = b.X - a.X;
        var v1y = b.Y - a.Y;
        var v2x = px - a.X;
        var v2y = py - a.Y;
        var dot00 = v0x * v0x + v0y * v0y;
        var dot01 = v0x * v1x + v0y * v1y;
        var dot02 = v0x * v2x + v0y * v2y;
        var dot11 = v1x * v1x + v1y * v1y;
        var dot12 = v1x * v2x + v1y * v2y;
        var denom = dot00 * dot11 - dot01 * dot01;
        if (MathF.Abs(denom) <= 1e-12f)
            return false;
        var inv = 1f / denom;
        var u = (dot11 * dot02 - dot01 * dot12) * inv;
        var v = (dot00 * dot12 - dot01 * dot02) * inv;
        return u >= 0f && v >= 0f && u + v <= 1f;
    }

    private static bool TryAspect(Matrix4x4 clip, out float aspect)
    {
        aspect = 0f;
        if (!float.IsFinite(clip.M11) || !float.IsFinite(clip.M22) || MathF.Abs(clip.M11) <= MinClipW)
            return false;
        aspect = clip.M22 / clip.M11;
        return float.IsFinite(aspect) && aspect > 0f;
    }

    private static bool TryProject(Matrix4x4 viewProjection, Vector3 world, out Vector3 ndc)
    {
        var clip = Vector4.Transform(new Vector4(world, 1f), viewProjection);
        ndc = default;
        if (!float.IsFinite(clip.W) || clip.W <= MinClipW)
            return false;
        var inverseW = 1f / clip.W;
        ndc = new Vector3(clip.X * inverseW, clip.Y * inverseW, clip.Z * inverseW);
        return float.IsFinite(ndc.X) && float.IsFinite(ndc.Y) && float.IsFinite(ndc.Z);
    }
}
