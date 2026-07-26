using System.Numerics;
using System.Text;

namespace Engine.Renderer;

/// <summary>
/// Writes CPU Model/submesh data to versioned little-endian *.mesh binary (KULA / VERSION=1).
/// </summary>
public static class MeshWriter
{
    public const uint Version = 1;
    private static readonly byte[] Magic = [.. "KULA"u8];

    public static void Write(Stream stream, Model model)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(model);

        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
        writer.Write(Magic);
        writer.Write(Version);
        writer.Write((uint)model.Submeshes.Count);

        foreach (var submesh in model.Submeshes)
            WriteSubmesh(writer, submesh);
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
