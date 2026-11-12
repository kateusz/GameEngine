using System.Numerics;
using System.Runtime.InteropServices;
using ECS;
using Engine.Project;
using Engine.Renderer;
using Engine.Renderer.Buffers;
using Engine.Renderer.Buffers.VertexArray;
using Engine.Renderer.Meshes;
using Engine.Renderer.Pipeline;
using Engine.Renderer.Textures;
using SceneComponents.Physics;
using SceneComponents.Rendering;
using Serilog;

namespace Engine.Scene;

internal static partial class SceneRenderPipeline
{
    private const int UnitCubeVertexCount = 24;
    private const int UnitCubeIndexCount = 36;

    // Mesh.Vertex layout: Position, Normal, TexCoord, Tangent, Bitangent, EntityId.
    private const int VertexStrideBytes = 15 * 4;

    private readonly record struct MergeMaterialKey(
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

    private readonly record struct MergeMember(
        int EntityId,
        Mesh? Source,
        Matrix4x4 World,
        Texture2D? CubeTexture,
        float Tiling);

    private sealed class CombinedMeshEntry
    {
        public Mesh Mesh = null!;
        public MergeMember[] Members = [];
    }

    private static Dictionary<int, Matrix4x4> WorldHistory = new();
    private static Dictionary<int, Matrix4x4> MergeFrameWorld = new();
    private static readonly Dictionary<MergeMaterialKey, List<MergeMember>> MergeGroups = new();
    private static readonly Dictionary<MergeMaterialKey, List<CombinedMeshEntry>> CombinedMeshes = new();
    private static readonly HashSet<MergeMaterialKey> MergeTouchedKeys = new();
    private static Context? _mergeContext;
    private static bool _mergeInitWarned;

    /// <summary>
    /// Drops every combined mesh, material group, and world history. Called when the scene changes.
    /// </summary>
    internal static void ResetMergeState()
    {
        foreach (var entries in CombinedMeshes.Values)
            DisposeEntries(entries);

        CombinedMeshes.Clear();
        MergeGroups.Clear();
        WorldHistory.Clear();
        MergeFrameWorld.Clear();
        MergeTouchedKeys.Clear();
        _mergeContext = null;
    }

    private static void CommitMergeWorldHistory()
    {
        (WorldHistory, MergeFrameWorld) = (MergeFrameWorld, WorldHistory);
    }

    private static void EvictStaleCombinedMeshes()
    {
        foreach (var key in (MergeMaterialKey[])[.. CombinedMeshes.Keys])
        {
            if (MergeTouchedKeys.Contains(key))
                continue;

            DisposeEntries(CombinedMeshes[key]);
            CombinedMeshes.Remove(key);
        }

        MergeTouchedKeys.Clear();
    }

    private static void EnsureMergeState(Context context)
    {
        if (ReferenceEquals(_mergeContext, context))
            return;

        ResetMergeState();
        _mergeContext = context;
    }

    /// <summary>
    /// Stores this renderer's draw world and reports whether it may join a merged group.
    /// First seen, moved, non-finite, and moving 2D bodies all stay on the instance path.
    /// </summary>
    private static bool TrackAndClassifyForMerge(Entity entity, Matrix4x4 world)
    {
        var finite = MatrixIsFinite(world);
        var idle = finite
                   && WorldHistory.TryGetValue(entity.Id, out var previous)
                   && world == previous;

        MergeFrameWorld[entity.Id] = world;

        var hasMovingBody = entity.TryGetComponent<RigidBody2DComponent>(out var body)
                            && body.BodyType is RigidBodyType.Dynamic or RigidBodyType.Kinematic;
        return idle && !hasMovingBody;
    }

    private static MergeMaterialKey MakeMergeKey(
        Vector4 tint,
        Texture2D? diffuse,
        Mesh? source,
        PbrFactors pbr,
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

    private static void AppendMergeMember(MergeMaterialKey key, MergeMember member) =>
        AppendMergeMember(MergeGroups, key, member);

    private static void AppendMergeMember(
        Dictionary<MergeMaterialKey, List<MergeMember>> groups,
        MergeMaterialKey key,
        MergeMember member)
    {
        if (!groups.TryGetValue(key, out var members))
        {
            members = [];
            groups[key] = members;
        }

        members.Add(member);
    }

    private static void AppendMergeSubmesh(
        ModelRendererComponent modelRenderer,
        Matrix4x4 transform,
        Vector4 tint,
        int entityId,
        Mesh submesh,
        ref PassStats stats,
        Dictionary<MergeMaterialKey, List<MergeMember>>? groups = null)
    {
        AdoptSubmeshFactors(modelRenderer, submesh);
        var pbr = ResolvePbr(modelRenderer);
        var emissive = CombineEmissive(submesh.EmissiveFactor, modelRenderer);
        var key = MakeMergeKey(tint, submesh.DiffuseTexture, submesh, pbr, emissive);

        AppendMergeMember(groups ?? MergeGroups, key, new MergeMember(entityId, submesh, transform, null, 0f));

        stats.Vertices += submesh.VertexCount;
        stats.Indices += submesh.GetIndexCount();
    }

    private static bool TryResolveCubeTexture(
        ITextureFactory textureFactory,
        ModelRendererComponent modelRenderer,
        out Texture2D? texture)
    {
        texture = null;
        if (string.IsNullOrWhiteSpace(modelRenderer.TexturePath))
            return true;

        try
        {
            texture = textureFactory.Create(PathBuilder.Resolve(modelRenderer.TexturePath), sRgb: true);
            return true;
        }
        catch (Exception ex)
        {
            Logger.Warning(
                ex,
                "Failed to load cube texture '{TexturePath}' — skipping merged cube",
                modelRenderer.TexturePath);
            return false;
        }
    }

    private static void FlushMergeGroups(
        IGraphics3D graphics3D,
        bool hasFrustum,
        in Frustum frustum,
        float shadowCasterMaxDistanceSq,
        Vector3 shadowCasterViewPosition,
        Vector3? skipCastersContainingLight,
        ref PassStats stats,
        IVertexArrayFactory? vertexArrayFactory,
        IVertexBufferFactory? vertexBufferFactory,
        IIndexBufferFactory? indexBufferFactory)
    {
        try
        {
            foreach (var (key, members) in MergeGroups)
            {
                if (members.Count == 0)
                    continue;

                Aabb groupBox = default;
                var hasGroupBox = members.Count >= 2 && TryGetGroupBox(members, out groupBox);
                List<CombinedMeshEntry>? cached = null;
                if (members.Count >= 2 && CombinedMeshes.TryGetValue(key, out var existing)
                    && CacheMatches(existing, members))
                {
                    cached = existing;
                    MergeTouchedKeys.Add(key);
                }

                if (hasGroupBox
                    && ShouldSkipOpaqueDraw(hasFrustum, frustum, Matrix4x4.Identity, groupBox,
                        shadowCasterMaxDistanceSq, shadowCasterViewPosition, null, ref stats))
                {
                    continue;
                }

                var checkCastersIndividually = skipCastersContainingLight is { } lamp
                    && (!hasGroupBox || Aabb.ContainsPoint(lamp, Matrix4x4.Identity, groupBox));
                var entries = cached;
                if (members.Count < 2 || checkCastersIndividually || (entries is null && !TryBuildCombined(key, members,
                        vertexArrayFactory, vertexBufferFactory, indexBufferFactory,
                        ref stats, out entries)))
                {
                    foreach (var member in members)
                    {
                        if (ShouldSkipMergeMember(member, hasFrustum, in frustum, shadowCasterMaxDistanceSq,
                                shadowCasterViewPosition, skipCastersContainingLight, ref stats))
                        {
                            continue;
                        }

                        ReplayAsInstance(graphics3D, key, member, ref stats);
                    }

                    continue;
                }

                MergeTouchedKeys.Add(key);
                foreach (var entry in entries)
                {
                    if (!hasGroupBox
                        && entry.Mesh.Bounds is { } meshBounds
                        && ShouldSkipOpaqueDraw(hasFrustum, frustum, Matrix4x4.Identity, meshBounds,
                            shadowCasterMaxDistanceSq, shadowCasterViewPosition, null, ref stats))
                    {
                        continue;
                    }

                    graphics3D.DrawMesh(Matrix4x4.Identity, entry.Mesh, key.Tint, entityId: -1,
                        metallic: key.Metallic, roughness: key.Roughness, ao: key.Ao,
                        emissive: key.EmissiveColor);
                    stats.MeshDraws++;
                    stats.MergedGroups++;
                    stats.MergedVertices += entry.Mesh.VertexCount;
                }
            }
        }
        finally
        {
            MergeGroups.Clear();
        }
    }

    private static bool ShouldSkipMergeMember(
        in MergeMember member,
        bool hasFrustum,
        in Frustum frustum,
        float shadowCasterMaxDistanceSq,
        Vector3 shadowCasterViewPosition,
        Vector3? skipCastersContainingLight,
        ref PassStats stats) =>
        TryGetLocalBounds(member, out var bounds)
        && ShouldSkipOpaqueDraw(hasFrustum, frustum, member.World, bounds, shadowCasterMaxDistanceSq,
            shadowCasterViewPosition, skipCastersContainingLight, ref stats);

    private static bool TryGetLocalBounds(in MergeMember member, out Aabb bounds)
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

    private static void ReplayAsInstance(
        IGraphics3D graphics3D,
        in MergeMaterialKey key,
        in MergeMember member,
        ref PassStats stats)
    {
        if (member.Source is null)
        {
            graphics3D.DrawCube(member.World, key.Tint, member.EntityId, member.CubeTexture, member.Tiling,
                key.Metallic, key.Roughness, key.Ao, key.EmissiveColor);
            stats.CubeDraws++;
            return;
        }

        Span<MeshDrawInstance> one = stackalloc MeshDrawInstance[1];
        one[0] = new MeshDrawInstance
        {
            Transform = member.World,
            EntityId = member.EntityId,
            Tint = key.Tint,
            Metallic = key.Metallic,
            Roughness = key.Roughness,
            Ao = key.Ao,
            Emissive = key.EmissiveColor
        };

        graphics3D.DrawMeshInstances(member.Source, one);
        stats.MeshDraws++;
        stats.Instances++;
        stats.SingleMaterialDraws++;
    }

    private static bool TryGetGroupBox(List<MergeMember> members, out Aabb box)
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

    private static bool TryBuildCombined(
        MergeMaterialKey key,
        List<MergeMember> members,
        IVertexArrayFactory? vertexArrayFactory,
        IVertexBufferFactory? vertexBufferFactory,
        IIndexBufferFactory? indexBufferFactory,
        ref PassStats stats,
        out List<CombinedMeshEntry> entries)
    {
        entries = [];

        if (CombinedMeshes.Remove(key, out var cached))
            DisposeEntries(cached);

        if (vertexArrayFactory is null || vertexBufferFactory is null || indexBufferFactory is null)
            return false;

        if (!TryBuildSlices(key, members, vertexArrayFactory, vertexBufferFactory, indexBufferFactory, out entries))
        {
            DisposeEntries(entries);
            entries = [];
            return false;
        }

        CombinedMeshes[key] = entries;
        stats.MergedRebuilds += entries.Count;
        return true;
    }

    private static bool CacheMatches(List<CombinedMeshEntry> entries, List<MergeMember> members)
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

    private static bool TryBuildSlices(
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

    private static void ConfigureCombinedMaterial(
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

    private static int IncomingVertexCount(in MergeMember member) =>
        member.Source?.BakeVertices?.Length ?? UnitCubeVertexCount;

    private static void AppendBakedMember(
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

    private static void AppendTemplate(
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

    private static void TransformNormalSpace(in Matrix4x4 world, out Matrix4x4 normalMatrix)
    {
        if (!Matrix4x4.Invert(world, out var inverse))
            inverse = Matrix4x4.Identity;

        normalMatrix = Matrix4x4.Transpose(inverse);
    }

    private static Vector3 SafeNormalize(Vector3 value)
    {
        var lengthSquared = value.LengthSquared();
        return lengthSquared > 1e-20f && float.IsFinite(lengthSquared)
            ? value / MathF.Sqrt(lengthSquared)
            : Vector3.Zero;
    }

    private static bool MatrixIsFinite(in Matrix4x4 matrix) =>
        float.IsFinite(matrix.M11) && float.IsFinite(matrix.M12)
        && float.IsFinite(matrix.M13) && float.IsFinite(matrix.M14)
        && float.IsFinite(matrix.M21) && float.IsFinite(matrix.M22)
        && float.IsFinite(matrix.M23) && float.IsFinite(matrix.M24)
        && float.IsFinite(matrix.M31) && float.IsFinite(matrix.M32)
        && float.IsFinite(matrix.M33) && float.IsFinite(matrix.M34)
        && float.IsFinite(matrix.M41) && float.IsFinite(matrix.M42)
        && float.IsFinite(matrix.M43) && float.IsFinite(matrix.M44);

    private static void DisposeEntries(List<CombinedMeshEntry> entries)
    {
        foreach (var entry in entries)
            entry.Mesh.Dispose();
        entries.Clear();
    }
}
