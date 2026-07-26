using System.Numerics;
using System.Text;

namespace Engine.Renderer;

/// <summary>Reads *.anim3d binary (AN3D).</summary>
public static class Anim3dReader
{
    public const uint MaxClips = 1024;
    public const uint MaxChannelsPerClip = 100;
    public const uint MaxKeysPerTrack = 100_000;
    public const uint MaxStringBytes = 4096;

    private const string ExpectedMagic = "AN3D";

    public static Anim3dAsset Read(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

        var magic = Encoding.ASCII.GetString(reader.ReadBytes(4));
        if (magic != ExpectedMagic)
            throw new InvalidDataException($"Invalid anim3d magic '{magic}', expected '{ExpectedMagic}'");

        var clipCount = reader.ReadUInt32();
        if (clipCount > MaxClips)
            throw new InvalidDataException($"CLIP_COUNT {clipCount} exceeds max {MaxClips}");

        var clips = new List<Anim3dClip>((int)clipCount);
        for (var i = 0; i < clipCount; i++)
            clips.Add(ReadClip(reader));

        return new Anim3dAsset(clips);
    }

    private static Anim3dClip ReadClip(BinaryReader reader)
    {
        var name = ReadString(reader) ?? string.Empty;
        var durationSeconds = reader.ReadSingle();
        var channelCount = reader.ReadUInt32();
        if (channelCount > MaxChannelsPerClip)
            throw new InvalidDataException($"CHANNEL_COUNT {channelCount} exceeds max {MaxChannelsPerClip}");

        var channels = new List<Anim3dChannel>((int)channelCount);
        for (var i = 0; i < channelCount; i++)
            channels.Add(ReadChannel(reader));

        return new Anim3dClip(name, durationSeconds, channels);
    }

    private static Anim3dChannel ReadChannel(BinaryReader reader)
    {
        var boneIndex = reader.ReadUInt32();
        if (boneIndex >= SkeletonReader.MaxBones)
            throw new InvalidDataException($"boneIndex {boneIndex} exceeds max {SkeletonReader.MaxBones - 1}");

        var translationKeyCount = reader.ReadUInt32();
        if (translationKeyCount > MaxKeysPerTrack)
            throw new InvalidDataException($"translationKeyCount {translationKeyCount} exceeds max {MaxKeysPerTrack}");
        var translationKeys = new List<Anim3dVec3Key>((int)translationKeyCount);
        for (var i = 0; i < translationKeyCount; i++)
            translationKeys.Add(new Anim3dVec3Key(reader.ReadSingle(), ReadVector3(reader)));

        var rotationKeyCount = reader.ReadUInt32();
        if (rotationKeyCount > MaxKeysPerTrack)
            throw new InvalidDataException($"rotationKeyCount {rotationKeyCount} exceeds max {MaxKeysPerTrack}");
        var rotationKeys = new List<Anim3dQuatKey>((int)rotationKeyCount);
        for (var i = 0; i < rotationKeyCount; i++)
        {
            var time = reader.ReadSingle();
            rotationKeys.Add(new Anim3dQuatKey(time,
                new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle())));
        }

        var scaleKeyCount = reader.ReadUInt32();
        if (scaleKeyCount > MaxKeysPerTrack)
            throw new InvalidDataException($"scaleKeyCount {scaleKeyCount} exceeds max {MaxKeysPerTrack}");
        var scaleKeys = new List<Anim3dVec3Key>((int)scaleKeyCount);
        for (var i = 0; i < scaleKeyCount; i++)
            scaleKeys.Add(new Anim3dVec3Key(reader.ReadSingle(), ReadVector3(reader)));

        return new Anim3dChannel(boneIndex, translationKeys, rotationKeys, scaleKeys);
    }

    private static Vector3 ReadVector3(BinaryReader reader) =>
        new(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

    private static string? ReadString(BinaryReader reader)
    {
        var length = reader.ReadUInt32();
        if (length == 0)
            return null;
        if (length > MaxStringBytes)
            throw new InvalidDataException($"String length {length} exceeds max {MaxStringBytes}");

        var bytes = reader.ReadBytes((int)length);
        if (bytes.Length != length)
            throw new EndOfStreamException($"Expected {length} string bytes, got {bytes.Length}");

        return Encoding.UTF8.GetString(bytes);
    }
}
