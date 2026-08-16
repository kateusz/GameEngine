using System.Numerics;
using System.Text;

namespace Engine.Renderer;

/// <summary>
/// Writes CPU Model/submesh data to versioned little-endian *.mesh binary (KULA / VERSION=3).
/// </summary>
public static class MeshWriter
{
    public const uint Version = 3;
    private static readonly byte[] Magic = [.. "KULA"u8];

    public static void Write(Stream stream, Model model)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(model);

        if (model.Bones.Count > SkeletalLimits.MaxBones)
            throw new InvalidDataException($"Bone count {model.Bones.Count} exceeds max {SkeletalLimits.MaxBones}");

        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
        writer.Write(Magic);
        writer.Write(Version);
        writer.Write((uint)model.Submeshes.Count);

        foreach (var submesh in model.Submeshes)
            WriteSubmesh(writer, submesh);

        WriteSkeleton(writer, model);
        WriteClips(writer, model);
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
        WriteVector4(writer, material.BaseColorFactor);
        WriteVector3(writer, material.EmissiveFactor);
        WriteString(writer, material.EmissiveTexturePath);
        writer.Write((byte)material.AlphaMode);
        writer.Write(material.AlphaCutoff);
        writer.Write(material.DoubleSided);
    }

    private static void WriteVertex(BinaryWriter writer, Mesh.Vertex vertex)
    {
        WriteVector3(writer, vertex.Position);
        WriteVector3(writer, vertex.Normal);
        writer.Write(vertex.TexCoord.X);
        writer.Write(vertex.TexCoord.Y);
        WriteVector3(writer, vertex.Tangent);
        WriteVector3(writer, vertex.Bitangent);
        writer.Write((int)MathF.Round(vertex.BoneId0));
        writer.Write((int)MathF.Round(vertex.BoneId1));
        writer.Write((int)MathF.Round(vertex.BoneId2));
        writer.Write((int)MathF.Round(vertex.BoneId3));
        writer.Write(vertex.Weights.X);
        writer.Write(vertex.Weights.Y);
        writer.Write(vertex.Weights.Z);
        writer.Write(vertex.Weights.W);
    }

    private static void WriteSkeleton(BinaryWriter writer, Model model)
    {
        writer.Write((uint)model.Bones.Count);
        foreach (var bone in model.Bones)
        {
            WriteString(writer, bone.Name);
            writer.Write(bone.ParentIndex);
            WriteMatrix(writer, bone.InverseBind);
        }
    }

    private static void WriteClips(BinaryWriter writer, Model model)
    {
        writer.Write((uint)model.Clips.Count);
        foreach (var clip in model.Clips)
        {
            WriteString(writer, clip.Name);
            writer.Write(clip.Duration);
            writer.Write((uint)clip.Channels.Count);
            foreach (var channel in clip.Channels)
            {
                writer.Write(channel.BoneIndex);
                writer.Write((uint)channel.Positions.Count);
                foreach (var key in channel.Positions)
                {
                    writer.Write(key.Time);
                    WriteVector3(writer, key.Value);
                }

                writer.Write((uint)channel.Rotations.Count);
                foreach (var key in channel.Rotations)
                {
                    writer.Write(key.Time);
                    writer.Write(key.Value.X);
                    writer.Write(key.Value.Y);
                    writer.Write(key.Value.Z);
                    writer.Write(key.Value.W);
                }

                writer.Write((uint)channel.Scales.Count);
                foreach (var key in channel.Scales)
                {
                    writer.Write(key.Time);
                    WriteVector3(writer, key.Value);
                }
            }
        }
    }

    private static void WriteMatrix(BinaryWriter writer, Matrix4x4 m)
    {
        writer.Write(m.M11); writer.Write(m.M12); writer.Write(m.M13); writer.Write(m.M14);
        writer.Write(m.M21); writer.Write(m.M22); writer.Write(m.M23); writer.Write(m.M24);
        writer.Write(m.M31); writer.Write(m.M32); writer.Write(m.M33); writer.Write(m.M34);
        writer.Write(m.M41); writer.Write(m.M42); writer.Write(m.M43); writer.Write(m.M44);
    }

    private static void WriteVector4(BinaryWriter writer, Vector4 v)
    {
        writer.Write(v.X);
        writer.Write(v.Y);
        writer.Write(v.Z);
        writer.Write(v.W);
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
