using System.Numerics;

namespace Engine.Renderer.Models.RuntimeMesh;

internal static class RuntimeMeshReader
{
    public static bool TryReadStamp(ReadOnlySpan<byte> file, out RuntimeMeshStamp stamp)
    {
        stamp = default;
        if (!TryReadHeader(file, out var header))
            return false;

        stamp = new RuntimeMeshStamp(
            header.SourceSize,
            header.SourceSha256.ToArray(),
            header.ImporterVersion,
            header.PostProcessFlags);
        return true;
    }

    public static bool TryRead(byte[] file, out SourceModel? model)
    {
        model = null;
        var span = file.AsSpan();
        if (!TryReadHeader(span, out var header))
            return false;

        if (!ValidateHeader(span, header))
            return false;

        if (!ValidateTableOffsets(span, header))
            return false;

        var meshes = new List<SourceSubmesh>((int)header.MeshCount);
        if (!TryReadMeshTable(file, header, meshes))
            return false;

        var nodes = new List<SourceNode>((int)header.NodeCount);
        if (!TryReadNodeTable(span, header, nodes, (int)header.MeshCount, (int)header.LightCount))
            return false;

        var lights = new List<SourceLight>((int)header.LightCount);
        if (!TryReadLightTable(span, header, lights))
            return false;

        if (!ValidateNodeTree(nodes))
            return false;

        model = new SourceModel(meshes, nodes, lights);
        return true;
    }

    public static bool TryRead(ReadOnlySpan<byte> file, out SourceModel? model) =>
        TryRead(file.ToArray(), out model);

    private readonly record struct Header(
        ulong FileSize,
        uint MeshCount,
        uint NodeCount,
        uint LightCount,
        uint ImporterVersion,
        uint PostProcessFlags,
        ulong SourceSize,
        byte[] SourceSha256,
        ulong MeshTableOffset,
        ulong NodeTableOffset,
        ulong LightTableOffset);

    private static bool TryReadHeader(ReadOnlySpan<byte> file, out Header header)
    {
        header = default;
        if (file.Length < RuntimeMeshFormat.HeaderSize)
            return false;

        if (file.Length < RuntimeMeshFormat.Magic.Length
            || !file.Slice(0, RuntimeMeshFormat.Magic.Length).SequenceEqual(RuntimeMeshFormat.Magic))
            return false;

        var reader = new RuntimeMeshBinaryReader(file);
        reader.Seek(RuntimeMeshFormat.Magic.Length);
        if (!reader.TryReadUInt32(out var version) || version != RuntimeMeshFormat.Version)
            return false;
        if (!reader.TryReadUInt64(out var fileSize))
            return false;
        if (!reader.TryReadUInt32(out var flags) || flags != RuntimeMeshFormat.Flags)
            return false;
        if (!reader.TryReadUInt32(out var layoutId) || layoutId != RuntimeMeshFormat.VertexLayoutId)
            return false;
        if (!reader.TryReadUInt32(out var indexType) || indexType != RuntimeMeshFormat.IndexType)
            return false;
        if (!reader.TryReadUInt32(out var meshCount))
            return false;
        if (!reader.TryReadUInt32(out var nodeCount))
            return false;
        if (!reader.TryReadUInt32(out var lightCount))
            return false;
        if (!reader.TryReadUInt32(out var importerVersion))
            return false;
        if (!reader.TryReadUInt32(out var postProcessFlags))
            return false;
        if (!reader.TryReadUInt64(out var sourceSize))
            return false;
        if (!reader.TryReadBytes(32, out var sha256))
            return false;
        if (!reader.TryReadUInt64(out var meshOffset))
            return false;
        if (!reader.TryReadUInt64(out var nodeOffset))
            return false;
        if (!reader.TryReadUInt64(out var lightOffset))
            return false;
        if (!reader.TryReadBytes(RuntimeMeshFormat.HeaderReservedBytes, out var reserved)
            || !reserved.SequenceEqual(new byte[RuntimeMeshFormat.HeaderReservedBytes]))
            return false;

        header = new Header(
            fileSize,
            meshCount,
            nodeCount,
            lightCount,
            importerVersion,
            postProcessFlags,
            sourceSize,
            sha256.ToArray(),
            meshOffset,
            nodeOffset,
            lightOffset);
        return true;
    }

    private static bool ValidateHeader(ReadOnlySpan<byte> file, Header header)
    {
        if (file.Length < RuntimeMeshFormat.MinFileBytes || file.Length > RuntimeMeshFormat.MaxFileBytes)
            return false;

        if ((ulong)file.Length != header.FileSize)
            return false;

        if (header.MeshCount is 0 or > RuntimeMeshFormat.MaxMeshCount)
            return false;

        if (header.NodeCount is 0 or > RuntimeMeshFormat.MaxNodeCount)
            return false;

        if (header.LightCount > RuntimeMeshFormat.MaxLightCount)
            return false;

        return true;
    }

    private static bool ValidateTableOffsets(ReadOnlySpan<byte> file, Header header)
    {
        ulong previousEnd = RuntimeMeshFormat.HeaderSize;

        if (!ValidateTableOffset(header.MeshCount, header.MeshTableOffset, file.Length, ref previousEnd))
            return false;
        if (!ValidateTableOffset(header.NodeCount, header.NodeTableOffset, file.Length, ref previousEnd))
            return false;
        if (!ValidateTableOffset(header.LightCount, header.LightTableOffset, file.Length, ref previousEnd))
            return false;

        return true;
    }

    private static bool ValidateTableOffset(uint count, ulong offset, int fileLength, ref ulong previousEnd)
    {
        if (count == 0)
            return offset == 0;

        if (offset == 0)
            return false;

        if (offset < RuntimeMeshFormat.HeaderSize || offset % 8 != 0 || offset >= (ulong)fileLength)
            return false;

        if (offset < previousEnd)
            return false;

        previousEnd = offset;
        return true;
    }

    private static bool TryReadMeshTable(byte[] file, Header header, List<SourceSubmesh> meshes)
    {
        var span = file.AsSpan();
        var meshCount = (int)header.MeshCount;
        var recordStarts = new int[meshCount];
        var recordLengths = new uint[meshCount];

        var reader = new RuntimeMeshBinaryReader(span, (int)header.MeshTableOffset);
        for (var i = 0; i < meshCount; i++)
        {
            if (reader.Position % 8 != 0)
                return false;

            var recordStart = reader.Position;
            if (!reader.TryReadUInt32(out var byteLength))
                return false;

            if (!IsValidRecordLength(byteLength, recordStart, span.Length))
                return false;

            recordStarts[i] = recordStart;
            recordLengths[i] = byteLength;
            reader.Seek(recordStart + (int)byteLength);
        }

        var decoded = new SourceSubmesh?[meshCount];
        if (meshCount >= RuntimeMeshFormat.MeshDecodeParallelThreshold)
        {
            var failed = 0;
            Parallel.For(0, meshCount, (i, state) =>
            {
                if (Volatile.Read(ref failed) != 0)
                {
                    state.Stop();
                    return;
                }

                if (!TryParseMeshRecord(file, recordStarts[i], recordLengths[i], out var mesh))
                {
                    Interlocked.Exchange(ref failed, 1);
                    state.Stop();
                    return;
                }

                decoded[i] = mesh;
            });

            if (failed != 0)
                return false;
        }
        else
        {
            for (var i = 0; i < meshCount; i++)
            {
                if (!TryParseMeshRecord(span, recordStarts[i], recordLengths[i], out var mesh))
                    return false;

                decoded[i] = mesh;
            }
        }

        for (var i = 0; i < meshCount; i++)
            meshes.Add(decoded[i]!);

        return true;
    }

    private static bool TryParseMeshRecord(
        ReadOnlySpan<byte> file,
        int recordStart,
        uint byteLength,
        out SourceSubmesh? mesh)
    {
        mesh = null;
        if (recordStart % 8 != 0)
            return false;

        var reader = new RuntimeMeshBinaryReader(file, recordStart);
        if (!reader.TryReadUInt32(out var recordByteLength) || recordByteLength != byteLength)
            return false;

        if (!reader.TryReadString(out var name))
            return false;
        if (!reader.TryReadUInt32(out var vertexCount) || vertexCount == 0 ||
            vertexCount > RuntimeMeshFormat.MaxVertexCount)
            return false;
        if (!reader.TryReadUInt32(out var indexCount) || indexCount == 0 ||
            indexCount > RuntimeMeshFormat.MaxIndexCount || indexCount % 3 != 0)
            return false;
        if (!reader.TryReadUInt32(out var hasBounds) || hasBounds != 1)
            return false;
        if (!reader.TryReadVector3(out var boundsMin) || !reader.TryReadVector3(out var boundsMax))
            return false;
        if (!IsFinite(boundsMin) || !IsFinite(boundsMax) || boundsMin.X > boundsMax.X ||
            boundsMin.Y > boundsMax.Y || boundsMin.Z > boundsMax.Z)
            return false;
        if (!reader.TryReadSingle(out var metallic) || !reader.TryReadSingle(out var roughness))
            return false;
        if (!IsFactor(metallic) || !IsFactor(roughness))
            return false;
        if (!reader.TryReadVector3(out var baseColor) || !IsFinite(baseColor))
            return false;
        if (!reader.TryReadString(out var diffuse) || !reader.TryReadString(out var normal) ||
            !reader.TryReadString(out var metallicRoughness) || !reader.TryReadString(out var occlusion))
            return false;

        var vertexBytes = (int)vertexCount * RuntimeMeshFormat.VertexLayoutStride;
        if (!reader.TryReadBytes(vertexBytes, out var vertexData))
            return false;

        var indexBytes = (int)indexCount * 4;
        if (!reader.TryReadBytes(indexBytes, out var indexData))
            return false;

        if (reader.Position > recordStart + byteLength)
            return false;

        var parsed = new SourceSubmesh
        {
            Name = name,
            BoundsMin = boundsMin,
            BoundsMax = boundsMax,
            Metallic = metallic,
            Roughness = roughness,
            BaseColorFactor = baseColor,
            DiffusePath = diffuse,
            NormalPath = normal,
            MetallicRoughnessPath = metallicRoughness,
            OcclusionPath = occlusion
        };

        for (var v = 0; v < vertexCount; v++)
        {
            var slice = vertexData.Slice(v * RuntimeMeshFormat.VertexLayoutStride, RuntimeMeshFormat.VertexLayoutStride);
            var vertex = RuntimeMeshVertexLayout.ReadVertex(slice);
            if (!IsFinite(vertex.Position) || !IsFinite(vertex.Normal) || !IsFinite(vertex.TexCoord) ||
                !IsFinite(vertex.Tangent) || !IsFinite(vertex.Bitangent))
                return false;

            parsed.Vertices.Add(vertex);
        }

        if (!RuntimeMeshVertexLayout.BoundsMatchPositions(boundsMin, boundsMax, parsed.Vertices))
            return false;

        for (var idx = 0; idx < indexCount; idx++)
        {
            var index = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(indexData.Slice(idx * 4, 4));
            if (index >= vertexCount)
                return false;

            parsed.Indices.Add(index);
        }

        mesh = parsed;
        return true;
    }

    private static bool TryReadNodeTable(
        ReadOnlySpan<byte> file,
        Header header,
        List<SourceNode> nodes,
        int meshCount,
        int lightCount)
    {
        var reader = new RuntimeMeshBinaryReader(file, (int)header.NodeTableOffset);
        for (var i = 0; i < header.NodeCount; i++)
        {
            if (reader.Position % 8 != 0)
                return false;

            var recordStart = reader.Position;
            if (!reader.TryReadUInt32(out var byteLength))
                return false;

            if (!IsValidRecordLength(byteLength, recordStart, file.Length))
                return false;

            if (!reader.TryReadString(out var name))
                return false;
            if (!reader.TryReadMatrix(out var matrix) || !IsFiniteMatrix(matrix))
                return false;
            if (!reader.TryReadUInt32(out var meshIndexCount))
                return false;

            var node = new SourceNode
            {
                Name = name,
                LocalTransform = matrix
            };
            for (var m = 0; m < meshIndexCount; m++)
            {
                if (!reader.TryReadUInt32(out var meshIndex) || meshIndex >= meshCount)
                    return false;

                node.MeshIndices.Add((int)meshIndex);
            }

            if (!reader.TryReadInt32(out var lightIndex) || lightIndex < -1 || lightIndex >= lightCount)
                return false;

            node.LightIndex = lightIndex;
            if (!reader.TryReadUInt32(out var childCount))
                return false;

            for (var c = 0; c < childCount; c++)
            {
                if (!reader.TryReadUInt32(out var childIndex) || childIndex >= header.NodeCount)
                    return false;

                node.ChildIndices.Add((int)childIndex);
            }

            if (reader.Position > recordStart + byteLength)
                return false;

            reader.Seek(recordStart + (int)byteLength);
            nodes.Add(node);
        }

        return true;
    }

    private static bool TryReadLightTable(ReadOnlySpan<byte> file, Header header, List<SourceLight> lights)
    {
        if (header.LightCount == 0)
            return true;

        var reader = new RuntimeMeshBinaryReader(file, (int)header.LightTableOffset);
        for (var i = 0; i < header.LightCount; i++)
        {
            if (reader.Position % 8 != 0)
                return false;

            var recordStart = reader.Position;
            if (!reader.TryReadUInt32(out var byteLength))
                return false;

            if (!IsValidRecordLength(byteLength, recordStart, file.Length))
                return false;

            if (!reader.TryReadUInt32(out var kind))
                return false;

            SourceLight? light = null;
            switch (kind)
            {
                case RuntimeMeshFormat.LightKindPoint:
                    if (!reader.TryReadVector4(out var color) || !reader.TryReadSingle(out var intensity) ||
                        !reader.TryReadSingle(out var range))
                        return false;

                    if (!IsFinite(color) || !float.IsFinite(intensity) || !float.IsFinite(range))
                        return false;

                    light = new SourcePointLight(color, intensity, range);
                    break;
                case RuntimeMeshFormat.LightKindDirectional:
                    if (!reader.TryReadVector4(out var dirColor) || !reader.TryReadVector3(out var direction))
                        return false;

                    if (!IsFinite(dirColor) || !IsFinite(direction))
                        return false;

                    light = new SourceDirectionalLight(dirColor, direction);
                    break;
                default:
                    return false;
            }

            if (reader.Position > recordStart + byteLength)
                return false;

            reader.Seek(recordStart + (int)byteLength);
            lights.Add(light);
        }

        return true;
    }

    private static bool ValidateNodeTree(IReadOnlyList<SourceNode> nodes)
    {
        if (nodes.Count == 0)
            return false;

        var parentCount = new int[nodes.Count];
        foreach (var node in nodes)
        {
            foreach (var childIndex in node.ChildIndices)
            {
                if (childIndex == 0)
                    return false;

                parentCount[childIndex]++;
                if (parentCount[childIndex] > 1)
                    return false;
            }
        }

        for (var i = 1; i < parentCount.Length; i++)
        {
            if (parentCount[i] != 1)
                return false;
        }

        var visited = 0;
        var stack = new Stack<int>();
        stack.Push(0);
        while (stack.Count > 0)
        {
            var index = stack.Pop();
            visited++;
            foreach (var child in nodes[index].ChildIndices)
                stack.Push(child);
        }

        return visited == nodes.Count;
    }

    private static bool IsValidRecordLength(uint byteLength, int recordStart, int fileLength)
    {
        if (byteLength < 8 || byteLength % 8 != 0)
            return false;

        return recordStart + byteLength <= fileLength;
    }

    private static bool IsFactor(float value) => float.IsFinite(value) && value >= 0f && value <= 1f;

    private static bool IsFinite(Vector2 value) =>
        float.IsFinite(value.X) && float.IsFinite(value.Y);

    private static bool IsFinite(Vector3 value) =>
        float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);

    private static bool IsFinite(Vector4 value) =>
        float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z) &&
        float.IsFinite(value.W);

    private static bool IsFiniteMatrix(Matrix4x4 matrix) =>
        float.IsFinite(matrix.M11) && float.IsFinite(matrix.M12) && float.IsFinite(matrix.M13) &&
        float.IsFinite(matrix.M14) &&
        float.IsFinite(matrix.M21) && float.IsFinite(matrix.M22) && float.IsFinite(matrix.M23) &&
        float.IsFinite(matrix.M24) &&
        float.IsFinite(matrix.M31) && float.IsFinite(matrix.M32) && float.IsFinite(matrix.M33) &&
        float.IsFinite(matrix.M34) &&
        float.IsFinite(matrix.M41) && float.IsFinite(matrix.M42) && float.IsFinite(matrix.M43) &&
        float.IsFinite(matrix.M44);
}
