using System.Buffers.Binary;
using System.Numerics;
using Engine.Renderer.Meshes;

namespace Engine.Renderer.Models.RuntimeMesh;

/// <summary>
/// Vertex packing for <see cref="RuntimeMeshFormat.VertexLayoutId"/> (layout 1 in the GEM1 spec).
/// </summary>
internal static class RuntimeMeshVertexLayout
{
    public static void WriteVertex(Span<byte> destination, in Mesh.Vertex vertex)
    {
        if (destination.Length < RuntimeMeshFormat.VertexLayoutStride)
            throw new ArgumentException("Destination too small.", nameof(destination));

        WriteVector3(destination, 0, vertex.Position);
        WriteVector3(destination, 12, vertex.Normal);
        WriteVector2(destination, 24, vertex.TexCoord);
        WriteVector3(destination, 32, vertex.Tangent);
        WriteVector3(destination, 44, vertex.Bitangent);
    }

    public static Mesh.Vertex ReadVertex(ReadOnlySpan<byte> source)
    {
        if (source.Length < RuntimeMeshFormat.VertexLayoutStride)
            throw new ArgumentException("Source too small.", nameof(source));

        return new Mesh.Vertex
        {
            Position = ReadVector3(source, 0),
            Normal = ReadVector3(source, 12),
            TexCoord = ReadVector2(source, 24),
            Tangent = ReadVector3(source, 32),
            Bitangent = ReadVector3(source, 44)
        };
    }

    public static bool TryComputeBounds(IReadOnlyList<Mesh.Vertex> vertices, out Vector3 min, out Vector3 max)
    {
        min = default;
        max = default;
        if (vertices.Count == 0)
            return false;

        min = new Vector3(float.PositiveInfinity);
        max = new Vector3(float.NegativeInfinity);
        foreach (var vertex in vertices)
        {
            var position = vertex.Position;
            if (!float.IsFinite(position.X) || !float.IsFinite(position.Y) || !float.IsFinite(position.Z))
                return false;

            min = Vector3.Min(min, position);
            max = Vector3.Max(max, position);
        }

        return true;
    }

    public static bool BoundsMatchPositions(Vector3 boundsMin, Vector3 boundsMax, IReadOnlyList<Mesh.Vertex> vertices) =>
        TryComputeBounds(vertices, out var min, out var max)
        && min == boundsMin
        && max == boundsMax;

    private static void WriteVector3(Span<byte> buffer, int offset, Vector3 value)
    {
        BinaryPrimitives.WriteSingleLittleEndian(buffer.Slice(offset, 4), value.X);
        BinaryPrimitives.WriteSingleLittleEndian(buffer.Slice(offset + 4, 4), value.Y);
        BinaryPrimitives.WriteSingleLittleEndian(buffer.Slice(offset + 8, 4), value.Z);
    }

    private static void WriteVector2(Span<byte> buffer, int offset, Vector2 value)
    {
        BinaryPrimitives.WriteSingleLittleEndian(buffer.Slice(offset, 4), value.X);
        BinaryPrimitives.WriteSingleLittleEndian(buffer.Slice(offset + 4, 4), value.Y);
    }

    private static Vector3 ReadVector3(ReadOnlySpan<byte> buffer, int offset) =>
        new(
            BinaryPrimitives.ReadSingleLittleEndian(buffer.Slice(offset, 4)),
            BinaryPrimitives.ReadSingleLittleEndian(buffer.Slice(offset + 4, 4)),
            BinaryPrimitives.ReadSingleLittleEndian(buffer.Slice(offset + 8, 4)));

    private static Vector2 ReadVector2(ReadOnlySpan<byte> buffer, int offset) =>
        new(
            BinaryPrimitives.ReadSingleLittleEndian(buffer.Slice(offset, 4)),
            BinaryPrimitives.ReadSingleLittleEndian(buffer.Slice(offset + 4, 4)));
}
