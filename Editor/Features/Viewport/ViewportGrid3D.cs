using System.Numerics;
using Engine.Renderer;
using Engine.Renderer.Cameras;

namespace Editor.Features.Viewport;

public class ViewportGrid3D
{
    private static readonly Vector4 MinorColor = new(0.55f, 0.55f, 0.55f, 0.30f);
    private static readonly Vector4 MajorColor = new(0.65f, 0.65f, 0.65f, 0.55f);
    private static readonly Vector4 AxisXColor = new(0.75f, 0.20f, 0.20f, 0.80f);
    private static readonly Vector4 AxisYColor = new(0.20f, 0.75f, 0.20f, 0.80f);
    private static readonly Vector4 AxisZColor = new(0.20f, 0.30f, 0.80f, 0.80f);

    private const int MajorEvery = 10;
    private const int HalfCount  = 10;

    public static void Render(IGraphics2D graphics2D, EditorCamera camera)
    {
        var spacing = CalculateSpacing(camera.Distance);
        var majorSpacing = spacing * MajorEvery;

        var focal = camera.FocalPoint;
        var centerX = MathF.Floor(focal.X / majorSpacing) * majorSpacing;
        var centerZ = MathF.Floor(focal.Z / majorSpacing) * majorSpacing;
        var halfExtent = HalfCount * majorSpacing;

        var minX = centerX - halfExtent;
        var maxX = centerX + halfExtent;
        var minZ = centerZ - halfExtent;
        var maxZ = centerZ + halfExtent;

        // Y axis line at origin (X=0, Z=0)
        graphics2D.DrawLine(new Vector3(0f, -halfExtent, 0f), new Vector3(0f, halfExtent, 0f), AxisYColor, -1);

        for (var i = -HalfCount * MajorEvery; i <= HalfCount * MajorEvery; i++)
        {
            var z       = centerZ + i * spacing;
            var isZAxis = MathF.Abs(z) < spacing * 0.01f;
            var isMajor = i % MajorEvery == 0;
            var color   = isZAxis ? AxisXColor : isMajor ? MajorColor : MinorColor;
            graphics2D.DrawLine(new Vector3(minX, 0f, z), new Vector3(maxX, 0f, z), color, -1);
        }

        for (var i = -HalfCount * MajorEvery; i <= HalfCount * MajorEvery; i++)
        {
            var x       = centerX + i * spacing;
            var isXAxis = MathF.Abs(x) < spacing * 0.01f;
            var isMajor = i % MajorEvery == 0;
            var color   = isXAxis ? AxisZColor : isMajor ? MajorColor : MinorColor;
            graphics2D.DrawLine(new Vector3(x, 0f, minZ), new Vector3(x, 0f, maxZ), color, -1);
        }
    }

    private static float CalculateSpacing(float distance)
    {
        var raw        = distance * 0.1f;
        var magnitude  = MathF.Pow(10f, MathF.Floor(MathF.Log(MathF.Max(raw, 1e-6f)) / MathF.Log(10f)));
        var normalized = raw / magnitude;
        var nice = normalized switch { < 2f => 1f, < 5f => 2f, _ => 5f };
        return nice * magnitude;
    }
}
