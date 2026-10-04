using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Engine.Renderer.Models.RuntimeMesh;

internal static class RuntimeMeshWriter
{
    public static bool TryWrite(string siblingPath, SourceModel model, RuntimeMeshStamp stamp)
    {
        try
        {
            var bytes = Serialize(model, stamp);
            var directory = Path.GetDirectoryName(siblingPath);
            if (string.IsNullOrEmpty(directory))
                return false;

            Directory.CreateDirectory(directory);
            var tempPath = siblingPath + ".tmp";
            File.WriteAllBytes(tempPath, bytes);
            File.Move(tempPath, siblingPath, overwrite: true);
            return true;
        }
        catch
        {
            var tempPath = siblingPath + ".tmp";
            if (File.Exists(tempPath))
            {
                try
                {
                    File.Delete(tempPath);
                }
                catch {}
            }

            return false;
        }
    }

    public static byte[] Serialize(SourceModel model, RuntimeMeshStamp stamp)
    {
        var body = new List<byte>(capacity: 4096);
        Align8(body);

        var meshOffset = body.Count;
        foreach (var mesh in model.Submeshes)
            WriteMeshRecord(body, mesh);

        Align8(body);
        var nodeOffset = body.Count;
        foreach (var node in model.Nodes)
            WriteNodeRecord(body, node);

        Align8(body);
        var lightOffset = body.Count;
        foreach (var light in model.Lights)
            WriteLightRecord(body, light);

        var fileSize = RuntimeMeshFormat.HeaderSize + body.Count;
        var file = new byte[fileSize];
        WriteHeader(
            file,
            stamp,
            (ulong)fileSize,
            meshCount: (uint)model.Submeshes.Count,
            nodeCount: (uint)model.Nodes.Count,
            lightCount: (uint)model.Lights.Count,
            meshOffset: model.Submeshes.Count == 0 ? 0UL : (ulong)(RuntimeMeshFormat.HeaderSize + meshOffset),
            nodeOffset: model.Nodes.Count == 0 ? 0UL : (ulong)(RuntimeMeshFormat.HeaderSize + nodeOffset),
            lightOffset: model.Lights.Count == 0 ? 0UL : (ulong)(RuntimeMeshFormat.HeaderSize + lightOffset));
        body.CopyTo(file, RuntimeMeshFormat.HeaderSize);
        return file;
    }

    private static void WriteHeader(
        Span<byte> file,
        RuntimeMeshStamp stamp,
        ulong fileSize,
        uint meshCount,
        uint nodeCount,
        uint lightCount,
        ulong meshOffset,
        ulong nodeOffset,
        ulong lightOffset)
    {
        var writer = new RuntimeMeshBinaryWriter(file);
        writer.WriteMagic(RuntimeMeshFormat.Magic);
        writer.WriteUInt32(RuntimeMeshFormat.Version);
        writer.WriteUInt64(fileSize);
        writer.WriteUInt32(RuntimeMeshFormat.Flags);
        writer.WriteUInt32(RuntimeMeshFormat.VertexLayoutId);
        writer.WriteUInt32(RuntimeMeshFormat.IndexType);
        writer.WriteUInt32(meshCount);
        writer.WriteUInt32(nodeCount);
        writer.WriteUInt32(lightCount);
        writer.WriteUInt32(stamp.ImporterVersion);
        writer.WriteUInt32(stamp.PostProcessFlags);
        writer.WriteUInt64(stamp.SourceSize);
        if (stamp.SourceSha256.Length != 32)
            throw new InvalidOperationException("Source SHA-256 must be 32 bytes.");

        writer.WriteBytes(stamp.SourceSha256);
        writer.WriteUInt64(meshOffset);
        writer.WriteUInt64(nodeOffset);
        writer.WriteUInt64(lightOffset);
        writer.WriteZeros(RuntimeMeshFormat.HeaderReservedBytes);
    }

    private static void WriteMeshRecord(List<byte> body, SourceSubmesh mesh)
    {
        Align8(body);
        var start = body.Count;
        AppendUInt32(body, 0);

        AppendString(body, mesh.Name);
        AppendUInt32(body, (uint)mesh.Vertices.Count);
        AppendUInt32(body, (uint)mesh.Indices.Count);
        AppendUInt32(body, 1);
        AppendVector3(body, mesh.BoundsMin);
        AppendVector3(body, mesh.BoundsMax);
        AppendSingle(body, mesh.Metallic);
        AppendSingle(body, mesh.Roughness);
        AppendVector3(body, mesh.BaseColorFactor);
        AppendString(body, mesh.DiffusePath);
        AppendString(body, mesh.NormalPath);
        AppendString(body, mesh.MetallicRoughnessPath);
        AppendString(body, mesh.OcclusionPath);
        var surface = 0u;
        if (mesh.AlphaCutout)
            surface |= RuntimeMeshFormat.SurfaceAlphaCutout;
        if (mesh.DoubleSided)
            surface |= RuntimeMeshFormat.SurfaceDoubleSided;
        AppendUInt32(body, surface);
        AppendSingle(body, mesh.AlphaCutoff);
        AppendVector3(body, mesh.Emissive);

        var vertexScratch = new byte[RuntimeMeshFormat.VertexLayoutStride];
        foreach (var vertex in mesh.Vertices)
        {
            RuntimeMeshVertexLayout.WriteVertex(vertexScratch, vertex);
            body.AddRange(vertexScratch);
        }

        foreach (var index in mesh.Indices)
            AppendUInt32(body, index);

        Align8(body);
        var length = body.Count - start;
        BinaryPrimitives.WriteUInt32LittleEndian(CollectionsMarshal.AsSpan(body).Slice(start, 4), (uint)length);
    }

    private static void WriteNodeRecord(List<byte> body, SourceNode node)
    {
        Align8(body);
        var start = body.Count;
        AppendUInt32(body, 0);

        AppendString(body, node.Name);
        AppendMatrix(body, node.LocalTransform);
        AppendUInt32(body, (uint)node.MeshIndices.Count);
        foreach (var meshIndex in node.MeshIndices)
            AppendUInt32(body, (uint)meshIndex);

        AppendInt32(body, node.LightIndex);
        AppendUInt32(body, (uint)node.ChildIndices.Count);
        foreach (var childIndex in node.ChildIndices)
            AppendUInt32(body, (uint)childIndex);

        Align8(body);
        var length = body.Count - start;
        BinaryPrimitives.WriteUInt32LittleEndian(CollectionsMarshal.AsSpan(body).Slice(start, 4), (uint)length);
    }

    private static void WriteLightRecord(List<byte> body, SourceLight light)
    {
        Align8(body);
        var start = body.Count;
        AppendUInt32(body, 0);

        switch (light)
        {
            case SourcePointLight point:
                AppendUInt32(body, RuntimeMeshFormat.LightKindPoint);
                AppendVector4(body, point.Color);
                AppendSingle(body, point.Intensity);
                AppendSingle(body, point.Range);
                break;
            case SourceDirectionalLight dir:
                AppendUInt32(body, RuntimeMeshFormat.LightKindDirectional);
                AppendVector4(body, dir.Color);
                AppendVector3(body, dir.Direction);
                break;
            default:
                throw new InvalidOperationException("Unsupported light record.");
        }

        Align8(body);
        var length = body.Count - start;
        BinaryPrimitives.WriteUInt32LittleEndian(CollectionsMarshal.AsSpan(body).Slice(start, 4), (uint)length);
    }

    private static void Align8(List<byte> body)
    {
        while (body.Count % 8 != 0)
            body.Add(0);
    }

    private static void AppendUInt32(List<byte> body, uint value)
    {
        Span<byte> scratch = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(scratch, value);
        body.Add(scratch[0]);
        body.Add(scratch[1]);
        body.Add(scratch[2]);
        body.Add(scratch[3]);
    }

    private static void AppendInt32(List<byte> body, int value) => AppendUInt32(body, (uint)value);

    private static void AppendSingle(List<byte> body, float value)
    {
        Span<byte> scratch = stackalloc byte[4];
        BinaryPrimitives.WriteSingleLittleEndian(scratch, value);
        body.Add(scratch[0]);
        body.Add(scratch[1]);
        body.Add(scratch[2]);
        body.Add(scratch[3]);
    }

    private static void AppendString(List<byte> body, string value)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(value);
        if (bytes.Length > RuntimeMeshFormat.MaxStringBytes)
            throw new InvalidOperationException("String too long.");

        AppendUInt32(body, (uint)bytes.Length);
        body.AddRange(bytes);
    }

    private static void AppendVector3(List<byte> body, Vector3 value)
    {
        AppendSingle(body, value.X);
        AppendSingle(body, value.Y);
        AppendSingle(body, value.Z);
    }

    private static void AppendVector4(List<byte> body, Vector4 value)
    {
        AppendSingle(body, value.X);
        AppendSingle(body, value.Y);
        AppendSingle(body, value.Z);
        AppendSingle(body, value.W);
    }

    private static void AppendMatrix(List<byte> body, Matrix4x4 matrix)
    {
        AppendSingle(body, matrix.M11);
        AppendSingle(body, matrix.M12);
        AppendSingle(body, matrix.M13);
        AppendSingle(body, matrix.M14);
        AppendSingle(body, matrix.M21);
        AppendSingle(body, matrix.M22);
        AppendSingle(body, matrix.M23);
        AppendSingle(body, matrix.M24);
        AppendSingle(body, matrix.M31);
        AppendSingle(body, matrix.M32);
        AppendSingle(body, matrix.M33);
        AppendSingle(body, matrix.M34);
        AppendSingle(body, matrix.M41);
        AppendSingle(body, matrix.M42);
        AppendSingle(body, matrix.M43);
        AppendSingle(body, matrix.M44);
    }
}
