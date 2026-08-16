using System.Numerics;
using System.Text;
using Engine.Renderer.Meshes;

namespace Engine.Renderer.Models;

/// <summary>
/// Reads versioned little-endian *.mesh binary (KULA / VERSION=1, 2, or 3).
/// </summary>
public static class MeshReader
{
    public const uint SupportedVersion = 3;

    /// <summary>Hard caps against hostile/corrupt size fields (verification hardening).</summary>
    // ponytail: one GLB → one .mesh packs every Assimp mesh-bearing node; village packs exceed 2k.
    // Ceiling: still bounds DoS from a hostile COUNT; raise if a real asset needs more.
    public const uint MaxSubmeshes = 65_536;
    public const uint MaxVerticesPerSubmesh = 5_000_000;
    public const uint MaxIndicesPerSubmesh = 15_000_000;
    public const uint MaxStringBytes = 4096;

    private const string ExpectedMagic = "KULA";

    public static Model Read(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

        var magic = Encoding.ASCII.GetString(reader.ReadBytes(4));
        if (magic != ExpectedMagic)
            throw new InvalidDataException($"Invalid mesh magic '{magic}', expected '{ExpectedMagic}'");

        var version = reader.ReadUInt32();
        if (version is not 1 and not 2 and not SupportedVersion)
            throw new NotSupportedException($"Unsupported mesh VERSION {version}; supported: 1, 2, or {SupportedVersion}");

        var submeshCount = reader.ReadUInt32();
        if (submeshCount > MaxSubmeshes)
            throw new InvalidDataException($"SUBMESH_COUNT {submeshCount} exceeds max {MaxSubmeshes}");

        var submeshes = new List<ModelSubmesh>((int)submeshCount);

        for (var i = 0; i < submeshCount; i++)
            submeshes.Add(ReadSubmesh(reader, version));

        var bones = version >= 3 ? ReadBones(reader) : [];
        var clips = version >= 3 ? ReadClips(reader, bones.Count) : [];

        return new Model(submeshes, bones, clips);
    }

    private static ModelSubmesh ReadSubmesh(BinaryReader reader, uint version)
    {
        var name = ReadString(reader) ?? string.Empty;
        var vertexCount = reader.ReadUInt32();
        var indexCount = reader.ReadUInt32();

        if (vertexCount > MaxVerticesPerSubmesh)
            throw new InvalidDataException($"VERTEX_COUNT {vertexCount} exceeds max {MaxVerticesPerSubmesh}");
        if (indexCount > MaxIndicesPerSubmesh)
            throw new InvalidDataException($"INDEX_COUNT {indexCount} exceeds max {MaxIndicesPerSubmesh}");

        EnsureReadable(reader, vertexCount * VertexStrideBytes(version) + indexCount * sizeof(uint));

        var mesh = new Mesh(name);
        mesh.Vertices.Capacity = (int)vertexCount;
        for (var i = 0; i < vertexCount; i++)
            mesh.Vertices.Add(ReadVertex(reader, version));

        mesh.Indices.Capacity = (int)indexCount;
        for (var i = 0; i < indexCount; i++)
        {
            var index = reader.ReadUInt32();
            if (index >= vertexCount)
                throw new InvalidDataException(
                    $"Index {index} out of range for VERTEX_COUNT {vertexCount} in mesh '{name}'");
            mesh.Indices.Add(index);
        }

        var material = new MeshMaterial
        {
            Metallic = reader.ReadSingle(),
            Roughness = reader.ReadSingle(),
            AlbedoTexturePath = ReadString(reader),
            MetallicRoughnessTexturePath = ReadString(reader),
            NormalTexturePath = ReadString(reader)
        };

        if (version >= 2)
        {
            material.BaseColorFactor = ReadVector4(reader);
            material.EmissiveFactor = ReadVector3(reader);
            material.EmissiveTexturePath = ReadString(reader);
            material.AlphaMode = (MaterialAlphaMode)reader.ReadByte();
            material.AlphaCutoff = reader.ReadSingle();
            material.DoubleSided = reader.ReadBoolean();
        }

        return new ModelSubmesh(mesh, material);
    }

    private static uint VertexStrideBytes(uint version) =>
        version >= 3
            ? 14u * sizeof(float) + 4u * sizeof(int) + 4u * sizeof(float)
            : 14u * sizeof(float);

    private static List<SkeletonBone> ReadBones(BinaryReader reader)
    {
        var count = reader.ReadUInt32();
        if (count > SkeletalLimits.MaxBones)
            throw new InvalidDataException($"BONE_COUNT {count} exceeds max {SkeletalLimits.MaxBones}");

        var bones = new List<SkeletonBone>((int)count);
        for (var i = 0; i < count; i++)
        {
            var name = ReadString(reader) ?? string.Empty;
            var parentIndex = reader.ReadInt32();
            if (parentIndex < -1 || parentIndex >= (int)count || parentIndex == i)
                throw new InvalidDataException($"Bone '{name}' has invalid parent index {parentIndex}");
            bones.Add(new SkeletonBone(name, parentIndex, ReadMatrix(reader)));
        }

        return bones;
    }

    private static List<AnimationClip> ReadClips(BinaryReader reader, int boneCount)
    {
        var clipCount = reader.ReadUInt32();
        var clips = new List<AnimationClip>((int)clipCount);
        for (var c = 0; c < clipCount; c++)
        {
            var name = ReadString(reader) ?? string.Empty;
            var duration = reader.ReadSingle();
            var channelCount = reader.ReadUInt32();
            var channels = new List<BoneChannel>((int)channelCount);
            for (var ch = 0; ch < channelCount; ch++)
            {
                var boneIndex = reader.ReadInt32();
                if (boneIndex < 0 || (boneCount > 0 && boneIndex >= boneCount))
                    throw new InvalidDataException($"Clip '{name}' channel bone index {boneIndex} out of range");

                var positions = ReadVectorKeys(reader);
                var rotations = ReadRotationKeys(reader);
                var scales = ReadVectorKeys(reader);
                channels.Add(new BoneChannel(boneIndex, positions, rotations, scales));
            }

            clips.Add(new AnimationClip(name, duration, channels));
        }

        return clips;
    }

    private static List<VectorKey> ReadVectorKeys(BinaryReader reader)
    {
        var count = reader.ReadUInt32();
        var keys = new List<VectorKey>((int)count);
        for (var i = 0; i < count; i++)
            keys.Add(new VectorKey(reader.ReadSingle(), ReadVector3(reader)));
        return keys;
    }

    private static List<RotationKey> ReadRotationKeys(BinaryReader reader)
    {
        var count = reader.ReadUInt32();
        var keys = new List<RotationKey>((int)count);
        for (var i = 0; i < count; i++)
        {
            var time = reader.ReadSingle();
            var rotation = new Quaternion(
                reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            keys.Add(new RotationKey(time, rotation));
        }

        return keys;
    }

    private static Matrix4x4 ReadMatrix(BinaryReader reader) =>
        new(
            reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
            reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
            reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
            reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

    private static Vector4 ReadVector4(BinaryReader reader) =>
        new(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

    private static void EnsureReadable(BinaryReader reader, ulong byteCount)
    {
        var stream = reader.BaseStream;
        if (!stream.CanSeek)
            return;

        var remaining = (ulong)System.Math.Max(0L, stream.Length - stream.Position);
        if (byteCount > remaining)
            throw new EndOfStreamException(
                $"Mesh payload needs {byteCount} bytes but only {remaining} remain in stream");
    }

    private static Mesh.Vertex ReadVertex(BinaryReader reader, uint version)
    {
        var position = ReadVector3(reader);
        var normal = ReadVector3(reader);
        var texCoord = new Vector2(reader.ReadSingle(), reader.ReadSingle());
        var tangent = ReadVector3(reader);
        var bitangent = ReadVector3(reader);
        if (version < 3)
            return new Mesh.Vertex(position, normal, texCoord, tangent, bitangent);

        return new Mesh.Vertex(
            position, normal, texCoord, tangent, bitangent,
            reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(),
            new Vector4(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()));
    }

    private static Vector3 ReadVector3(BinaryReader reader) =>
        new(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

    /// <summary>Length-prefixed UTF-8. Length 0 → null (absent path).</summary>
    private static string? ReadString(BinaryReader reader)
    {
        var length = reader.ReadUInt32();
        if (length == 0)
            return null;
        if (length > MaxStringBytes)
            throw new InvalidDataException($"String length {length} exceeds max {MaxStringBytes}");

        EnsureReadable(reader, length);

        var bytes = reader.ReadBytes((int)length);
        if (bytes.Length != length)
            throw new EndOfStreamException($"Expected {length} string bytes, got {bytes.Length}");

        return Encoding.UTF8.GetString(bytes);
    }
}
