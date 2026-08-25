using System.Numerics;

namespace Editor.Features.Viewport.Gizmos;

internal static class BillboardGizmoHelper
{
    public static readonly Vector2[] TextureCoords =
    [
        new(0.0f, 0.0f),
        new(1.0f, 0.0f),
        new(1.0f, 1.0f),
        new(0.0f, 1.0f)
    ];

    public static Matrix4x4 BuildBillboard(
        Vector3 position,
        Vector3 editorPos,
        Vector3 right,
        Vector3 up,
        Vector3 face,
        float iconDistanceScale = 0.06f,
        float minIconSize = 0.15f)
    {
        var distance = Vector3.Distance(editorPos, position);
        var size = MathF.Max(minIconSize, distance * iconDistanceScale);

        return new Matrix4x4(
            right.X * size, right.Y * size, right.Z * size, 0.0f,
            up.X * size, up.Y * size, up.Z * size, 0.0f,
            face.X * size, face.Y * size, face.Z * size, 0.0f,
            position.X, position.Y, position.Z, 1.0f);
    }
}
