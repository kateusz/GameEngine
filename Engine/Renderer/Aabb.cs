using System.Numerics;
using Engine.Renderer.Meshes;

namespace Engine.Renderer;

internal readonly record struct Aabb(Vector3 Min, Vector3 Max)
{
    public const float UnitCubeHalfExtent = 0.5f;

    public static Aabb UnitCube { get; } = new(
        new Vector3(-UnitCubeHalfExtent),
        new Vector3(UnitCubeHalfExtent));

    public static Aabb? FromPositions(List<Mesh.Vertex> vertices)
    {
        if (vertices.Count == 0)
            return null;

        var min = new Vector3(float.PositiveInfinity);
        var max = new Vector3(float.NegativeInfinity);
        foreach (var vertex in vertices)
        {
            var position = vertex.Position;
            if (!float.IsFinite(position.X) || !float.IsFinite(position.Y) || !float.IsFinite(position.Z))
                return null;

            min = Vector3.Min(min, position);
            max = Vector3.Max(max, position);
        }

        return new Aabb(min, max);
    }
}
