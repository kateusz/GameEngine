using System.Numerics;
using System.Text;

namespace Engine.Renderer;

/// <summary>Writes *.skel binary (SKEL).</summary>
public static class SkeletonWriter
{
    private static readonly byte[] Magic = [.. "SKEL"u8];

    public static void Write(Stream stream, SkeletonAsset asset)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(asset);

        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
        writer.Write(Magic);
        writer.Write((uint)asset.Bones.Count);

        foreach (var bone in asset.Bones)
        {
            WriteString(writer, bone.Name);
            writer.Write(bone.ParentIndex);
            WriteMatrix(writer, bone.InverseBind);
        }
    }

    private static void WriteMatrix(BinaryWriter writer, Matrix4x4 m)
    {
        writer.Write(m.M11); writer.Write(m.M12); writer.Write(m.M13); writer.Write(m.M14);
        writer.Write(m.M21); writer.Write(m.M22); writer.Write(m.M23); writer.Write(m.M24);
        writer.Write(m.M31); writer.Write(m.M32); writer.Write(m.M33); writer.Write(m.M34);
        writer.Write(m.M41); writer.Write(m.M42); writer.Write(m.M43); writer.Write(m.M44);
    }

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
