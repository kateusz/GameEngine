using System.Numerics;
using Math;

namespace Engine.Renderer;

internal static class ImportSpawnTransform
{
    private const float CentimeterScaleMin = 50f;
    private const float CentimeterScaleMax = 200f;

    public static (Vector3 Translation, Vector3 Rotation, Vector3 Scale) FromLocalToRoot(
        Matrix4x4 localToRoot,
        float unitDownscaleFactor)
    {
        if (!MathHelpers.DecomposeTransform(localToRoot, out var translation, out var rotation, out var scale))
            return (Vector3.Zero, Vector3.Zero, Vector3.One);

        // FbxConvertToMeters can leave cm translations on the node matrix while verts are already meters.
        if (IsUniformCentimeterNodeScale(scale))
        {
            translation *= CookUnitScale.CmToMeters;
            scale = Vector3.One;
        }
        else if (MathF.Abs(unitDownscaleFactor - 1f) > 1e-6f)
            translation *= unitDownscaleFactor;

        return (translation, rotation, scale);
    }

    private static bool IsUniformCentimeterNodeScale(Vector3 scale)
    {
        var ax = MathF.Abs(scale.X);
        var ay = MathF.Abs(scale.Y);
        var az = MathF.Abs(scale.Z);
        if (MathF.Abs(ax - ay) > 1f || MathF.Abs(ax - az) > 1f)
            return false;

        return ax is >= CentimeterScaleMin and <= CentimeterScaleMax;
    }
}
