using System.Numerics;
using System.Text;

namespace Engine.Renderer;

/// <summary>
/// Reads versioned little-endian *.mesh binary (KULA / VERSION=1)
/// </summary>
public static class MeshReader
{
    public const uint SupportedVersion = 1;

    /// <summary>Hard caps against hostile/corrupt size fields (verification hardening).</summary>
    public const uint MaxSubmeshes = 256;
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
        if (version != SupportedVersion)
            throw new NotSupportedException($"Unsupported mesh VERSION {version}; supported: {SupportedVersion}");

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

        EnsureReadable(reader, vertexCount * 14u * sizeof(float) + indexCount * sizeof(uint));

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
        return new Mesh.Vertex(
            ReadVector3(reader),
            ReadVector3(reader),
            new Vector2(reader.ReadSingle(), reader.ReadSingle()),
            ReadVector3(reader),
            ReadVector3(reader));
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
