using System.Numerics;
using System.Text;
using Engine.Renderer.Skeletal.Serialization;

namespace Engine.Renderer.Serialization;

/// <summary>
/// Reads versioned little-endian *.mesh binary (KULA / FormatVersion=2). Always-present bone attrs.
/// Bone index -1 is the unused-influence sentinel and is only valid when the corresponding bone weight is zero.
/// </summary>
public static class MeshReader
{
    public const uint FormatVersion = 2;
    public const uint MaxSubmeshes = 65_536;
    public const uint MaxVerticesPerSubmesh = 5_000_000;
    public const uint MaxIndicesPerSubmesh = 15_000_000;
    public const uint MaxStringBytes = 4096;

    private const string ExpectedMagic = "KULA";
    // FormatVersion 2 vertex layout: pos/nrm/uv/tan/bitan + 4×int32 bone ids + 4×float weights.
    private const uint VertexStrideBytes = 88;

    public static Model Read(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

        var magic = Encoding.ASCII.GetString(reader.ReadBytes(4));
        if (magic != ExpectedMagic)
            throw new InvalidDataException($"Invalid mesh magic '{magic}', expected '{ExpectedMagic}'");

        var version = reader.ReadUInt32();
        if (version != FormatVersion)
            throw new InvalidDataException(
                $"Invalid mesh version {version}, expected {FormatVersion}");

        var submeshCount = reader.ReadUInt32();
        if (submeshCount > MaxSubmeshes)
            throw new InvalidDataException($"SUBMESH_COUNT {submeshCount} exceeds max {MaxSubmeshes}");

        var submeshes = new List<ModelSubmesh>((int)submeshCount);

        for (var i = 0; i < submeshCount; i++)
            submeshes.Add(ReadSubmesh(reader));

        return new Model(submeshes);
    }

    private static ModelSubmesh ReadSubmesh(BinaryReader reader)
    {
        var name = ReadString(reader) ?? string.Empty;
        var vertexCount = reader.ReadUInt32();
        var indexCount = reader.ReadUInt32();

        if (vertexCount > MaxVerticesPerSubmesh)
            throw new InvalidDataException($"VERTEX_COUNT {vertexCount} exceeds max {MaxVerticesPerSubmesh}");
        if (indexCount > MaxIndicesPerSubmesh)
            throw new InvalidDataException($"INDEX_COUNT {indexCount} exceeds max {MaxIndicesPerSubmesh}");

        EnsureReadable(reader, vertexCount * VertexStrideBytes + indexCount * sizeof(uint));

        var mesh = new Mesh(name);
        mesh.Vertices.Capacity = (int)vertexCount;
        for (var i = 0; i < vertexCount; i++)
            mesh.Vertices.Add(ReadVertex(reader));

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

        return new ModelSubmesh(mesh, material);
    }

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

    private static Mesh.Vertex ReadVertex(BinaryReader reader)
    {
        var position = ReadVector3(reader);
        var normal = ReadVector3(reader);
        var texCoord = new Vector2(reader.ReadSingle(), reader.ReadSingle());
        var tangent = ReadVector3(reader);
        var bitangent = ReadVector3(reader);

        // File stores int32 indices; CPU/GPU vertex uses float4.
        // Unused influence sentinel: -1 only when the corresponding weight is zero.
        var i0 = reader.ReadInt32();
        var i1 = reader.ReadInt32();
        var i2 = reader.ReadInt32();
        var i3 = reader.ReadInt32();
        var w0 = reader.ReadSingle();
        var w1 = reader.ReadSingle();
        var w2 = reader.ReadSingle();
        var w3 = reader.ReadSingle();

        ValidateBoneInfluence(i0, w0);
        ValidateBoneInfluence(i1, w1);
        ValidateBoneInfluence(i2, w2);
        ValidateBoneInfluence(i3, w3);

        return new Mesh.Vertex(
            position,
            normal,
            texCoord,
            tangent,
            bitangent,
            new Vector4(i0, i1, i2, i3),
            new Vector4(w0, w1, w2, w3));
    }

    private static void ValidateBoneInfluence(int boneIndex, float boneWeight)
    {
        if (!float.IsFinite(boneWeight))
            throw new InvalidDataException($"Bone weight {boneWeight} is not finite");

        if (boneIndex == -1)
        {
            if (boneWeight != 0f)
                throw new InvalidDataException(
                    $"Bone index sentinel -1 requires zero weight, got {boneWeight}");
            return;
        }

        if (boneIndex < 0 || boneIndex >= SkeletonReader.MaxBones)
            throw new InvalidDataException(
                $"Bone index {boneIndex} out of range [0, {SkeletonReader.MaxBones - 1}] (or -1 with zero weight)");
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
