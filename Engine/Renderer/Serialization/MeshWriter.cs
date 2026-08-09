using System.Numerics;
using System.Text;
using Engine.Renderer.Skeletal.Serialization;

namespace Engine.Renderer.Serialization;

/// <summary>
/// Writes CPU Model/submesh data to versioned little-endian *.mesh binary (KULA / FormatVersion=2).
/// Bone index -1 is the unused-influence sentinel and is only valid when the corresponding bone weight is zero.
/// </summary>
public static class MeshWriter
{
    public const uint FormatVersion = MeshReader.FormatVersion;

    private static readonly byte[] Magic = [.. "KULA"u8];

    public static void Write(Stream stream, Model model)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(model);

        ValidateForWrite(model);

        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
        writer.Write(Magic);
        writer.Write(FormatVersion);
        writer.Write((uint)model.Submeshes.Count);

        foreach (var submesh in model.Submeshes)
            WriteSubmesh(writer, submesh);
    }

    /// <summary>Same caps as <see cref="MeshReader"/> — fail at cook time, not load time.</summary>
    private static void ValidateForWrite(Model model)
    {
        var submeshCount = (uint)model.Submeshes.Count;
        if (submeshCount > MeshReader.MaxSubmeshes)
            throw new InvalidOperationException(
                $"Cannot write mesh: SUBMESH_COUNT {submeshCount} exceeds max {MeshReader.MaxSubmeshes}");

        foreach (var submesh in model.Submeshes)
        {
            var mesh = submesh.Mesh;
            var material = submesh.Material;
            var vertexCount = (uint)mesh.Vertices.Count;
            var indexCount = (uint)mesh.Indices.Count;
            if (vertexCount > MeshReader.MaxVerticesPerSubmesh)
                throw new InvalidOperationException(
                    $"Cannot write mesh '{mesh.Name}': VERTEX_COUNT {vertexCount} exceeds max {MeshReader.MaxVerticesPerSubmesh}. " +
                    "Simplify the source model or enable vertex welding in the DCC export.");
            if (indexCount > MeshReader.MaxIndicesPerSubmesh)
                throw new InvalidOperationException(
                    $"Cannot write mesh '{mesh.Name}': INDEX_COUNT {indexCount} exceeds max {MeshReader.MaxIndicesPerSubmesh}");

            EnsureUtf8WithinMax(mesh.Name, $"mesh '{mesh.Name}' name");
            EnsureUtf8WithinMax(material.AlbedoTexturePath, $"mesh '{mesh.Name}' AlbedoTexturePath");
            EnsureUtf8WithinMax(material.MetallicRoughnessTexturePath, $"mesh '{mesh.Name}' MetallicRoughnessTexturePath");
            EnsureUtf8WithinMax(material.NormalTexturePath, $"mesh '{mesh.Name}' NormalTexturePath");
        }
    }

    private static void EnsureUtf8WithinMax(string? value, string fieldLabel)
    {
        if (string.IsNullOrEmpty(value))
            return;

        var byteCount = Encoding.UTF8.GetByteCount(value);
        if (byteCount > MeshReader.MaxStringBytes)
            throw new InvalidOperationException(
                $"Cannot write {fieldLabel}: UTF-8 byte count {byteCount} exceeds max {MeshReader.MaxStringBytes}");
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
        WriteBoneIndex(writer, vertex.BoneIndex.X, vertex.BoneWeight.X);
        WriteBoneIndex(writer, vertex.BoneIndex.Y, vertex.BoneWeight.Y);
        WriteBoneIndex(writer, vertex.BoneIndex.Z, vertex.BoneWeight.Z);
        WriteBoneIndex(writer, vertex.BoneIndex.W, vertex.BoneWeight.W);
        writer.Write(vertex.BoneWeight.X);
        writer.Write(vertex.BoneWeight.Y);
        writer.Write(vertex.BoneWeight.Z);
        writer.Write(vertex.BoneWeight.W);
    }

    /// <summary>
    /// Writes a rounded bone index. Unused influence sentinel: -1 only when the corresponding weight is zero.
    /// Accepted range: 0..<see cref="SkeletonReader.MaxBones"/>-1, or -1 with zero weight.
    /// </summary>
    private static void WriteBoneIndex(BinaryWriter writer, float boneIndex, float boneWeight)
    {
        if (!float.IsFinite(boneIndex))
            throw new InvalidOperationException($"Cannot write mesh: bone index {boneIndex} is not finite");
        if (!float.IsFinite(boneWeight))
            throw new InvalidOperationException($"Cannot write mesh: bone weight {boneWeight} is not finite");

        var rounded = (int)MathF.Round(boneIndex);
        if (rounded == -1)
        {
            if (boneWeight != 0f)
                throw new InvalidOperationException(
                    $"Cannot write mesh: bone index sentinel -1 requires zero weight, got {boneWeight}");
            writer.Write(-1);
            return;
        }

        if (rounded < 0 || rounded >= SkeletonReader.MaxBones)
            throw new InvalidOperationException(
                $"Cannot write mesh: bone index {rounded} out of range [0, {SkeletonReader.MaxBones - 1}] (or -1 with zero weight)");

        writer.Write(rounded);
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
