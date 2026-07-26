using System.Numerics;
using System.Text;

namespace Engine.Renderer;

/// <summary>Reads *.skel binary (SKEL).</summary>
public static class SkeletonReader
{
    public const uint MinBones = 1;
    public const uint MaxBones = 100;
    public const uint MaxStringBytes = 4096;

    private const string ExpectedMagic = "SKEL";

    public static SkeletonAsset Read(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

        var magic = Encoding.ASCII.GetString(reader.ReadBytes(4));
        if (magic != ExpectedMagic)
            throw new InvalidDataException($"Invalid skeleton magic '{magic}', expected '{ExpectedMagic}'");

        var boneCount = reader.ReadUInt32();
        if (boneCount < MinBones || boneCount > MaxBones)
            throw new InvalidDataException($"BONE_COUNT {boneCount} must be in {MinBones}..{MaxBones}");

        var bones = new List<SkeletonBone>((int)boneCount);
        for (var i = 0; i < boneCount; i++)
        {
            var name = ReadString(reader) ?? string.Empty;
            var parentIndex = reader.ReadInt32();
            var inverseBind = ReadMatrix(reader);
            bones.Add(new SkeletonBone(name, parentIndex, inverseBind));
        }

        ValidateBoneTopology(bones);
        return new SkeletonAsset(bones);
    }

    private static void ValidateBoneTopology(IReadOnlyList<SkeletonBone> bones)
    {
        for (var i = 0; i < bones.Count; i++)
        {
            var parentIndex = bones[i].ParentIndex;
            if (parentIndex < -1 || parentIndex >= bones.Count)
                throw new InvalidDataException($"Bone {i} parentIndex {parentIndex} out of range [-1, {bones.Count - 1}]");
            if (parentIndex == i)
                throw new InvalidDataException($"Bone {i} cannot be its own parent");

            var visited = new HashSet<int>();
            var current = i;
            while (true)
            {
                var parent = bones[current].ParentIndex;
                if (parent < 0)
                    break;
                if (!visited.Add(parent))
                    throw new InvalidDataException($"Bone hierarchy cycle detected at bone {i}");
                current = parent;
            }
        }
    }

    private static Matrix4x4 ReadMatrix(BinaryReader reader) =>
        new(
            reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
            reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
            reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
            reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

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
