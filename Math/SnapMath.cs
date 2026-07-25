using System.Diagnostics;
using System.Numerics;

namespace Math;

public static class SnapMath
{
    /// <summary>Absolute world XY lattice quantize for masked axes; Z unchanged. gridStep &lt;= 0 → no-op.</summary>
    public static Vector3 QuantizeWorldXY(Vector3 worldPosition, float gridStep, bool snapX, bool snapY)
    {
        if (gridStep <= 0f)
            return worldPosition;

        var x = snapX ? RoundToLattice(worldPosition.X, gridStep) : worldPosition.X;
        var y = snapY ? RoundToLattice(worldPosition.Y, gridStep) : worldPosition.Y;
        return new Vector3(x, y, worldPosition.Z);
    }

    private static float RoundToLattice(float value, float gridStep)
        => MathF.Round(value / gridStep) * gridStep;

    /// <summary>Debug-only self-check (3C / D2A). Host once from Editor startup.</summary>
    [Conditional("DEBUG")]
    public static void SelfCheck()
    {
        // Lattice round, GridStep = 1.0, both axes (Free ≡ XY)
        var both = QuantizeWorldXY(new Vector3(1.4f, 2.6f, 5.0f), 1.0f, snapX: true, snapY: true);
        Debug.Assert(both == new Vector3(1.0f, 3.0f, 5.0f), "SnapMath: both-axes lattice round failed");

        // Axis mask: unconstrained axis + Z unchanged
        var xOnly = QuantizeWorldXY(new Vector3(1.4f, 2.6f, 5.0f), 1.0f, snapX: true, snapY: false);
        Debug.Assert(xOnly == new Vector3(1.0f, 2.6f, 5.0f), "SnapMath: X-only mask left Y/Z unchanged failed");

        var yOnly = QuantizeWorldXY(new Vector3(1.4f, 2.6f, 5.0f), 1.0f, snapX: false, snapY: true);
        Debug.Assert(yOnly == new Vector3(1.4f, 3.0f, 5.0f), "SnapMath: Y-only mask left X/Z unchanged failed");

        // GridStep <= 0 → no-op (zero and negative)
        var input = new Vector3(1.4f, 2.6f, 5.0f);
        var noopZero = QuantizeWorldXY(input, 0f, snapX: true, snapY: true);
        Debug.Assert(noopZero == input, "SnapMath: GridStep == 0 must be a no-op");
        var noopNeg = QuantizeWorldXY(input, -1.0f, snapX: true, snapY: true);
        Debug.Assert(noopNeg == input, "SnapMath: GridStep < 0 must be a no-op");

        // Free ≡ XY: both axes snap
        var free = QuantizeWorldXY(new Vector3(-0.4f, 0.6f, -3.0f), 1.0f, snapX: true, snapY: true);
        Debug.Assert(free == new Vector3(0.0f, 1.0f, -3.0f), "SnapMath: Free/both-axes XY snap failed");

        // Non-unit GridStep (AC1 multiples of step, not only 1.0)
        var half = QuantizeWorldXY(new Vector3(0.74f, 1.26f, 9.0f), 0.5f, snapX: true, snapY: true);
        Debug.Assert(half == new Vector3(0.5f, 1.5f, 9.0f), "SnapMath: GridStep 0.5 lattice round failed");

        // Neither-axis mask: leave XY (and Z) unchanged even with valid step
        var none = QuantizeWorldXY(new Vector3(1.4f, 2.6f, 5.0f), 1.0f, snapX: false, snapY: false);
        Debug.Assert(none == new Vector3(1.4f, 2.6f, 5.0f), "SnapMath: neither-axis mask must leave input unchanged");
    }
}
