using System.Numerics;

namespace Engine.Renderer;

internal static class ModelMeshPivot
{
    public static Matrix4x4 ToDrawTransform(Matrix4x4 world, Vector3 pivot) =>
        pivot == Vector3.Zero ? world : Matrix4x4.CreateTranslation(-pivot) * world;
}
