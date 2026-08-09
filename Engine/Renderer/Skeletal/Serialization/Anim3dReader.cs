using System.Numerics;
using System.Text;
using Engine.Renderer.Skeletal;

namespace Engine.Renderer.Skeletal.Serialization;

/// <summary>Reads *.anim3d binary (AN3D / FormatVersion=1).</summary>
public static class Anim3dReader
{
    public const uint FormatVersion = 1;
    public const uint MaxClips = 1024;
    public const uint MaxChannelsPerClip = 100;
    public const uint MaxKeysPerTrack = 100_000;
    public const uint MaxStringBytes = 4096;

    private const string ExpectedMagic = "AN3D";
    private const ulong Vec3KeyBytes = 16; // float time + 3×float
    private const ulong QuatKeyBytes = 20; // float time + 4×float

    public static Anim3dAsset Read(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

        var magic = Encoding.ASCII.GetString(reader.ReadBytes(4));
        if (magic != ExpectedMagic)
            throw new InvalidDataException($"Invalid anim3d magic '{magic}', expected '{ExpectedMagic}'");

        var version = reader.ReadUInt32();
        if (version != FormatVersion)
            throw new InvalidDataException(
                $"Invalid anim3d version {version}, expected {FormatVersion}");

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
        EnsureReadable(reader, translationKeyCount * Vec3KeyBytes);
        var translationKeys = new List<Anim3dVec3Key>((int)translationKeyCount);
        float prevTranslationTime = float.NegativeInfinity;
        for (var i = 0; i < translationKeyCount; i++)
        {
            var time = reader.ReadSingle();
            if (time < prevTranslationTime)
                throw new InvalidDataException(
                    $"Translation key time {time} is earlier than preceding key {prevTranslationTime}");
            prevTranslationTime = time;
            translationKeys.Add(new Anim3dVec3Key(time, ReadVector3(reader)));
        }

        var rotationKeyCount = reader.ReadUInt32();
        if (rotationKeyCount > MaxKeysPerTrack)
            throw new InvalidDataException($"rotationKeyCount {rotationKeyCount} exceeds max {MaxKeysPerTrack}");
        EnsureReadable(reader, rotationKeyCount * QuatKeyBytes);
        var rotationKeys = new List<Anim3dQuatKey>((int)rotationKeyCount);
        float prevRotationTime = float.NegativeInfinity;
        for (var i = 0; i < rotationKeyCount; i++)
        {
            var time = reader.ReadSingle();
            if (time < prevRotationTime)
                throw new InvalidDataException(
                    $"Rotation key time {time} is earlier than preceding key {prevRotationTime}");
            prevRotationTime = time;
            rotationKeys.Add(new Anim3dQuatKey(time,
                new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle())));
        }

        var scaleKeyCount = reader.ReadUInt32();
        if (scaleKeyCount > MaxKeysPerTrack)
            throw new InvalidDataException($"scaleKeyCount {scaleKeyCount} exceeds max {MaxKeysPerTrack}");
        EnsureReadable(reader, scaleKeyCount * Vec3KeyBytes);
        var scaleKeys = new List<Anim3dVec3Key>((int)scaleKeyCount);
        float prevScaleTime = float.NegativeInfinity;
        for (var i = 0; i < scaleKeyCount; i++)
        {
            var time = reader.ReadSingle();
            if (time < prevScaleTime)
                throw new InvalidDataException(
                    $"Scale key time {time} is earlier than preceding key {prevScaleTime}");
            prevScaleTime = time;
            scaleKeys.Add(new Anim3dVec3Key(time, ReadVector3(reader)));
        }

        return new Anim3dChannel(boneIndex, translationKeys, rotationKeys, scaleKeys);
    }

    private static void EnsureReadable(BinaryReader reader, ulong byteCount)
    {
        var stream = reader.BaseStream;
        if (!stream.CanSeek)
            return;

        var remaining = (ulong)System.Math.Max(0L, stream.Length - stream.Position);
        if (byteCount > remaining)
            throw new EndOfStreamException(
                $"Anim3d payload needs {byteCount} bytes but only {remaining} remain in stream");
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
