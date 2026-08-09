using System.Numerics;
using System.Text;
using Engine.Renderer.Skeletal;

namespace Engine.Renderer.Skeletal.Serialization;

/// <summary>Writes *.anim3d binary (AN3D / FormatVersion=1).</summary>
public static class Anim3dWriter
{
    public const uint FormatVersion = Anim3dReader.FormatVersion;

    private static readonly byte[] Magic = [.. "AN3D"u8];

    public static void Write(Stream stream, Anim3dAsset asset)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(asset);

        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
        writer.Write(Magic);
        writer.Write(FormatVersion);
        writer.Write((uint)asset.Clips.Count);

        foreach (var clip in asset.Clips)
        {
            WriteString(writer, clip.Name);
            writer.Write(clip.DurationSeconds);
            writer.Write((uint)clip.Channels.Count);

            foreach (var channel in clip.Channels)
            {
                writer.Write(channel.BoneIndex);

                writer.Write((uint)channel.TranslationKeys.Count);
                foreach (var key in channel.TranslationKeys)
                {
                    writer.Write(key.Time);
                    writer.Write(key.Value.X);
                    writer.Write(key.Value.Y);
                    writer.Write(key.Value.Z);
                }

                writer.Write((uint)channel.RotationKeys.Count);
                foreach (var key in channel.RotationKeys)
                {
                    writer.Write(key.Time);
                    writer.Write(key.Value.X);
                    writer.Write(key.Value.Y);
                    writer.Write(key.Value.Z);
                    writer.Write(key.Value.W);
                }

                writer.Write((uint)channel.ScaleKeys.Count);
                foreach (var key in channel.ScaleKeys)
                {
                    writer.Write(key.Time);
                    writer.Write(key.Value.X);
                    writer.Write(key.Value.Y);
                    writer.Write(key.Value.Z);
                }
            }
        }
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
