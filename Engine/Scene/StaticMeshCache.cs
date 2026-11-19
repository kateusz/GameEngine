using System.Numerics;
using System.Runtime.InteropServices;
using Engine.Renderer;
using Engine.Renderer.Buffers;
using Engine.Renderer.Buffers.VertexArray;
using Engine.Renderer.Meshes;
using Engine.Renderer.Textures;
using Serilog;

namespace Engine.Scene;

internal sealed class StaticMeshCache : IDisposable
{
    private static readonly ILogger Logger = Log.ForContext<StaticMeshCache>();
    private const int UnitCubeVertexCount = 24;
    private const int UnitCubeIndexCount = 36;

    // Mesh.Vertex layout: Position, Normal, TexCoord, Tangent, Bitangent, EntityId.
    private const int VertexStrideBytes = 15 * 4;

    internal readonly record struct MergeMaterialKey(
        Texture2D? Diffuse,
        Texture2D? Normal,
        Texture2D? MetallicRoughness,
        Texture2D? Occlusion,
        Texture2D? Emissive,
        Vector4 Tint,
        float Metallic,
        float Roughness,
        float Ao,
        Vector3 EmissiveColor,
        Vector3 BaseColorFactor,
        bool AlphaCutout,
        float AlphaCutoff,
        bool DoubleSided);

    internal readonly record struct MergeMember(
        int EntityId,
        Mesh? Source,
        Matrix4x4 World,
        Texture2D? CubeTexture,
        float Tiling);

    internal sealed class CombinedMeshEntry
    {
        public Mesh Mesh = null!;
        public MergeMember[] Members = [];
    }

    private Dictionary<int, Matrix4x4> WorldHistory = new();
    private Dictionary<int, Matrix4x4> MergeFrameWorld = new();
    private readonly Dictionary<MergeMaterialKey, List<MergeMember>> _mergeGroups = new();
    private readonly Dictionary<MergeMaterialKey, List<CombinedMeshEntry>> _combinedMeshes = new();
    private readonly HashSet<MergeMaterialKey> _mergeTouchedKeys = new();
    private bool _mergeInitWarned;

    internal IReadOnlyDictionary<MergeMaterialKey, List<MergeMember>> PendingGroups => _mergeGroups;

    internal void ResetMergeState()
    {
        foreach (var entries in _combinedMeshes.Values)
            DisposeEntries(entries);

        _combinedMeshes.Clear();
        AbortFrame();
        WorldHistory.Clear();
    }

    internal void CommitMergeWorldHistory()
    {
        (WorldHistory, MergeFrameWorld) = (MergeFrameWorld, WorldHistory);
    }

    internal void EvictStaleCombinedMeshes()
    {
        foreach (var key in (MergeMaterialKey[])[.. _combinedMeshes.Keys])
        {
            if (_mergeTouchedKeys.Contains(key))
                continue;

            DisposeEntries(_combinedMeshes[key]);
            _combinedMeshes.Remove(key);
        }

        _mergeTouchedKeys.Clear();
    }

    internal List<CombinedMeshEntry>? FindMatching(MergeMaterialKey key, List<MergeMember> members)
    {
        if (!_combinedMeshes.TryGetValue(key, out var existing) || !CacheMatches(existing, members))
            return null;
        return existing;
    }

    internal void Touch(MergeMaterialKey key) => _mergeTouchedKeys.Add(key);

    internal void EndPass() => _mergeGroups.Clear();

    /// <summary>Clears per-frame merge collect state without disposing uploaded combined meshes.</summary>
    internal void AbortFrame()
    {
        _mergeGroups.Clear();
        _mergeTouchedKeys.Clear();
        MergeFrameWorld.Clear();
    }

    internal bool TrackAndClassifyForMerge(int entityId, bool hasMovingBody, Matrix4x4 world)
    {
        var finite = MatrixIsFinite(world);
        var idle = finite
                   && WorldHistory.TryGetValue(entityId, out var previous)
                   && world == previous;

        MergeFrameWorld[entityId] = world;

        return idle && !hasMovingBody;
    }

    internal MergeMaterialKey MakeMergeKey(
        Vector4 tint,
        Texture2D? diffuse,
        Mesh? source,
        SceneRenderDataBuilder.PbrFactors pbr,
        Vector3 emissive) =>
        new(
            diffuse,
            source?.NormalTexture,
            source?.MetallicRoughnessTexture,
            source?.OcclusionTexture,
            source?.EmissiveTexture,
            tint,
            pbr.Metallic,
            pbr.Roughness,
            pbr.Ao,
            emissive,
            source?.BaseColorFactor ?? Vector3.One,
            source?.AlphaCutout ?? false,
            source?.AlphaCutoff ?? 0.5f,
            source?.DoubleSided ?? false);

    internal void AppendMergeMember(MergeMaterialKey key, MergeMember member)
    {
        if (!_mergeGroups.TryGetValue(key, out var members))
        {
            members = [];
            _mergeGroups[key] = members;
        }

        members.Add(member);
    }

    internal bool TryGetLocalBounds(in MergeMember member, out Aabb bounds)
    {
        if (member.Source is { } source)
        {
            if (source.Bounds is { } sourceBounds)
            {
                bounds = sourceBounds;
                return true;
            }

            bounds = default;
            return false;
        }

        bounds = Aabb.UnitCube;
        return true;
    }

    internal bool TryGetGroupBox(List<MergeMember> members, out Aabb box)
    {
        var min = new Vector3(float.PositiveInfinity);
        var max = new Vector3(float.NegativeInfinity);

        foreach (var member in members)
        {
            if (!TryGetLocalBounds(member, out var local))
            {
                box = default;
                return false;
            }

            Aabb.WorldMinMax(member.World, local, out var worldMin, out var worldMax);
            min = Vector3.Min(min, worldMin);
            max = Vector3.Max(max, worldMax);
        }

        box = new Aabb(min, max);
        return true;
    }

    internal bool TryBuildCombined(
        MergeMaterialKey key,
        List<MergeMember> members,
        IVertexArrayFactory? vertexArrayFactory,
        IVertexBufferFactory? vertexBufferFactory,
        IIndexBufferFactory? indexBufferFactory,
        ref MeshPassStats stats,
        out List<CombinedMeshEntry> entries)
    {
        entries = [];

        if (_combinedMeshes.Remove(key, out var cached))
            DisposeEntries(cached);

        if (vertexArrayFactory is null || vertexBufferFactory is null || indexBufferFactory is null)
            return false;

        if (!TryBuildSlices(key, members, vertexArrayFactory, vertexBufferFactory, indexBufferFactory, out entries))
        {
            DisposeEntries(entries);
            entries = [];
            return false;
        }

        _combinedMeshes[key] = entries;
        stats.MergedRebuilds += entries.Count;
        return true;
    }

    internal bool CacheMatches(List<CombinedMeshEntry> entries, List<MergeMember> members)
    {
        var start = 0;
        foreach (var entry in entries)
        {
            var count = entry.Members.Length;
            if (start + count > members.Count
                || !entry.Members.AsSpan().SequenceEqual(CollectionsMarshal.AsSpan(members).Slice(start, count)))
                return false;

            start += count;
        }

        return start == members.Count;
    }

    internal bool TryBuildSlices(
        MergeMaterialKey key,
        List<MergeMember> members,
        IVertexArrayFactory vertexArrayFactory,
        IVertexBufferFactory vertexBufferFactory,
        IIndexBufferFactory indexBufferFactory,
        out List<CombinedMeshEntry> entries)
    {
        entries = [];

        var sliceMembers = new List<MergeMember>();
        var vertices = new List<Mesh.Vertex>();
        var indices = new List<uint>();
        long byteCount = 0;

        foreach (var member in members)
        {
            var incomingBytes = (long)IncomingVertexCount(member) * VertexStrideBytes;
            if (incomingBytes > RenderingConstants.MaxVertexBufferBytes)
                return false;

            if (sliceMembers.Count > 0
                && byteCount + incomingBytes > RenderingConstants.MaxVertexBufferBytes)
            {
                if (!BuildSlice(out var slice))
                    return false;

                entries.Add(slice);
                sliceMembers = [];
                vertices = [];
                indices = [];
                byteCount = 0;
            }

            AppendBakedMember(member, vertices, indices);
            byteCount = (long)vertices.Count * VertexStrideBytes;
            sliceMembers.Add(member);
        }

        if (sliceMembers.Count > 0)
        {
            if (!BuildSlice(out var lastSlice))
                return false;

            entries.Add(lastSlice);
        }

        return entries.Count > 0;

        bool BuildSlice(out CombinedMeshEntry slice)
        {
            slice = null!;
            var combined = new Mesh("merged");
            ConfigureCombinedMaterial(combined, key, sliceMembers[0]);
            combined.Vertices = vertices;
            combined.Indices = indices;

            try
            {
                combined.Initialize(vertexArrayFactory, vertexBufferFactory, indexBufferFactory, retainBakeGeometry: false);
            }
            catch (Exception ex)
            {
                combined.Dispose();
                if (!_mergeInitWarned)
                {
                    _mergeInitWarned = true;
                    Logger.Warning(
                        ex,
                        "Static mesh merge upload failed — drawing its members on the instance path");
                }

                return false;
            }

            slice = new CombinedMeshEntry
            {
                Mesh = combined,
                Members = [.. sliceMembers]
            };
            return true;
        }
    }

    internal void ConfigureCombinedMaterial(
        Mesh combined,
        MergeMaterialKey key,
        MergeMember first)
    {
        var source = first.Source;
        combined.DiffuseTexture = key.Diffuse;
        combined.NormalTexture = key.Normal;
        combined.MetallicRoughnessTexture = key.MetallicRoughness;
        combined.OcclusionTexture = key.Occlusion;
        combined.EmissiveTexture = key.Emissive;
        combined.MetallicFactor = key.Metallic;
        combined.RoughnessFactor = key.Roughness;
        combined.EmissiveFactor = source?.EmissiveFactor ?? Vector3.Zero;
        combined.AlphaCutout = key.AlphaCutout;
        combined.DoubleSided = key.DoubleSided;
        combined.AlphaCutoff = key.AlphaCutoff;
        combined.BaseColorFactor = key.BaseColorFactor;
    }

    internal int IncomingVertexCount(in MergeMember member) =>
        member.Source?.BakeVertices?.Length ?? UnitCubeVertexCount;

    internal void AppendBakedMember(
        in MergeMember member,
        List<Mesh.Vertex> vertices,
        List<uint> indices)
    {
        if (member.Source is { BakeVertices: { } bakedVertices, BakeIndices: { } bakedIndices })
        {
            AppendTemplate(member, bakedVertices, bakedIndices, vertices, indices);
            return;
        }

        var cube = MeshFactory.CubeTemplate;
        AppendTemplate(member, cube.Vertices, cube.Indices, vertices, indices);
    }

    internal void AppendTemplate(
        in MergeMember member,
        ReadOnlySpan<Mesh.Vertex> templateVertices,
        ReadOnlySpan<uint> templateIndices,
        List<Mesh.Vertex> vertices,
        List<uint> indices)
    {
        TransformNormalSpace(member.World, out var normalMatrix);
        var vertexOffset = vertices.Count;

        var texCoordScale = member.Source is null ? member.Tiling : 1f;

        foreach (var vertex in templateVertices)
        {
            vertices.Add(new Mesh.Vertex(
                Vector3.Transform(vertex.Position, member.World),
                SafeNormalize(Vector3.TransformNormal(vertex.Normal, normalMatrix)),
                vertex.TexCoord * texCoordScale,
                SafeNormalize(Vector3.TransformNormal(vertex.Tangent, normalMatrix)),
                SafeNormalize(Vector3.TransformNormal(vertex.Bitangent, normalMatrix)),
                member.EntityId));
        }

        foreach (var index in templateIndices)
            indices.Add((uint)(vertexOffset + index));
    }

    internal void TransformNormalSpace(in Matrix4x4 world, out Matrix4x4 normalMatrix)
    {
        if (!Matrix4x4.Invert(world, out var inverse))
            inverse = Matrix4x4.Identity;

        normalMatrix = Matrix4x4.Transpose(inverse);
    }

    internal Vector3 SafeNormalize(Vector3 value)
    {
        var lengthSquared = value.LengthSquared();
        return lengthSquared > 1e-20f && float.IsFinite(lengthSquared)
            ? value / MathF.Sqrt(lengthSquared)
            : Vector3.Zero;
    }

    internal bool MatrixIsFinite(in Matrix4x4 matrix) =>
        float.IsFinite(matrix.M11) && float.IsFinite(matrix.M12)
        && float.IsFinite(matrix.M13) && float.IsFinite(matrix.M14)
        && float.IsFinite(matrix.M21) && float.IsFinite(matrix.M22)
        && float.IsFinite(matrix.M23) && float.IsFinite(matrix.M24)
        && float.IsFinite(matrix.M31) && float.IsFinite(matrix.M32)
        && float.IsFinite(matrix.M33) && float.IsFinite(matrix.M34)
        && float.IsFinite(matrix.M41) && float.IsFinite(matrix.M42)
        && float.IsFinite(matrix.M43) && float.IsFinite(matrix.M44);

    internal void DisposeEntries(List<CombinedMeshEntry> entries)
    {
        foreach (var entry in entries)
            entry.Mesh.Dispose();
        entries.Clear();
    }

    internal void BeginFrame()
    {
        // Leftovers from a previous aborted frame must not pollute this one.
        AbortFrame();
    }

    public void Dispose() => ResetMergeState();
}
