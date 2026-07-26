using System.Numerics;
using System.Text;

namespace Engine.Renderer;

/// <summary>
/// Writes CPU Model/submesh data to versioned little-endian *.mesh binary (KULA).
/// </summary>
public static class MeshWriter
{
    private static readonly byte[] Magic = [.. "KULA"u8];

    public static void Write(Stream stream, Model model)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(model);

        ValidateForWrite(model);

        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
        writer.Write(Magic);
        writer.Write((uint)model.Submeshes.Count);

        foreach (var submesh in model.Submeshes)
            WriteSubmesh(writer, submesh);
    }

    /// <summary>Same caps as <see cref="MeshReader"/> — fail at cook time, not load time.</summary>
    internal static void ValidateForWrite(Model model)
    {
        var submeshCount = (uint)model.Submeshes.Count;
        if (submeshCount > MeshReader.MaxSubmeshes)
            throw new InvalidOperationException(
                $"Cannot write mesh: SUBMESH_COUNT {submeshCount} exceeds max {MeshReader.MaxSubmeshes}");

        foreach (var submesh in model.Submeshes)
        {
            var vertexCount = (uint)submesh.Mesh.Vertices.Count;
            var indexCount = (uint)submesh.Mesh.Indices.Count;
            if (vertexCount > MeshReader.MaxVerticesPerSubmesh)
                throw new InvalidOperationException(
                    $"Cannot write mesh '{submesh.Mesh.Name}': VERTEX_COUNT {vertexCount} exceeds max {MeshReader.MaxVerticesPerSubmesh}. " +
                    "Simplify the source model or enable vertex welding in the DCC export.");
            if (indexCount > MeshReader.MaxIndicesPerSubmesh)
                throw new InvalidOperationException(
                    $"Cannot write mesh '{submesh.Mesh.Name}': INDEX_COUNT {indexCount} exceeds max {MeshReader.MaxIndicesPerSubmesh}");
        }
    }

    private static void WriteSubmesh(BinaryWriter writer, ModelSubmesh submesh)
    {
        var mesh = submesh.Mesh;
        var material = submesh.Material;

        WriteString(writer, mesh.Name);
        writer.Write((uint)mesh.Vertices.Count);
        writer.Write((uint)mesh.Indices.Count);

        foreach (var vertex in mesh.Vertices)
            WriteVertex(writer, vertex);

        foreach (var index in mesh.Indices)
            writer.Write(index);

        writer.Write(material.Metallic);
        writer.Write(material.Roughness);
        WriteString(writer, material.AlbedoTexturePath);
        WriteString(writer, material.MetallicRoughnessTexturePath);
        WriteString(writer, material.NormalTexturePath);
    }

    private static void WriteVertex(BinaryWriter writer, Mesh.Vertex vertex)
    {
        WriteVector3(writer, vertex.Position);
        WriteVector3(writer, vertex.Normal);
        writer.Write(vertex.TexCoord.X);
        writer.Write(vertex.TexCoord.Y);
        WriteVector3(writer, vertex.Tangent);
        WriteVector3(writer, vertex.Bitangent);
        writer.Write((int)(vertex.BoneIndex.X + 0.5f));
        writer.Write((int)(vertex.BoneIndex.Y + 0.5f));
        writer.Write((int)(vertex.BoneIndex.Z + 0.5f));
        writer.Write((int)(vertex.BoneIndex.W + 0.5f));
        writer.Write(vertex.BoneWeight.X);
        writer.Write(vertex.BoneWeight.Y);
        writer.Write(vertex.BoneWeight.Z);
        writer.Write(vertex.BoneWeight.W);
    }

    private static void WriteVector3(BinaryWriter writer, Vector3 v)
    {
        writer.Write(v.X);
        writer.Write(v.Y);
        writer.Write(v.Z);
    }

    /// <summary>Length-prefixed UTF-8 (uint32 length + bytes). Null/empty → length 0.</summary>
    private static void WriteString(BinaryWriter writer, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            writer.Write(0u);
            return;
        }

        var bytes = Encoding.UTF8.GetBytes(value);
        writer.Write((uint)bytes.Length);
        writer.Write(bytes);
    }
}
