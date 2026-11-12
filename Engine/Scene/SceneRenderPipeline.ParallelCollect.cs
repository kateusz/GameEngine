using System.Numerics;
using ECS;
using Engine.Project;
using Engine.Renderer;
using Engine.Renderer.Meshes;
using Engine.Renderer.Models;
using Engine.Renderer.Pipeline;
using Engine.Renderer.Textures;
using SceneComponents;
using SceneComponents.Rendering;

namespace Engine.Scene;

internal static partial class SceneRenderPipeline
{
    /// <summary>Below this candidate count, collect stays single-threaded.</summary>
    internal static int ParallelOpaqueCollectThreshold = 64;

    /// <summary>Tests / debug: always take the serial collect path.</summary>
    internal static bool ForceSerialOpaqueCollect;

    private static readonly List<OpaqueCandidate> OpaqueCandidates = new(1024);
    private static readonly List<ImmediateCubeDraw> ImmediateCubes = new(256);
    private static readonly List<int> DeferredCandidateIndices = new(64);
    private static CollectBuffers[]? _workerBuffers;

    private struct OpaqueCandidate
    {
        public Entity Entity;
        public ModelRendererComponent Renderer;
        public Matrix4x4 World;
        public bool MergeCandidate;
    }

    private struct ImmediateCubeDraw
    {
        public Entity Entity;
        public ModelRendererComponent Renderer;
        public Matrix4x4 World;
        public Vector4 Color;
        public PbrFactors Factors;
        public Vector3 Emissive;
    }

    private sealed class CollectBuffers
    {
        public readonly Dictionary<MeshBatchKey, List<MeshDrawInstance>> MeshBatches = new();
        public readonly Dictionary<MergeMaterialKey, List<MergeMember>> MergeGroups = new();
        public readonly List<ImmediateCubeDraw> ImmediateCubes = new();
        public readonly List<int> Deferred = new();
        public PassStats Stats;

        public void ClearForPass()
        {
            foreach (var batch in MeshBatches.Values)
                batch.Clear();
            MeshBatches.Clear();
            foreach (var members in MergeGroups.Values)
                members.Clear();
            MergeGroups.Clear();
            ImmediateCubes.Clear();
            Deferred.Clear();
            Stats = default;
        }
    }

    private readonly struct OpaqueCollectContext
    {
        public readonly bool HasFrustum;
        public readonly Frustum Frustum;
        public readonly SceneView LodView;
        public readonly float ShadowCasterMaxDistanceSq;
        public readonly Vector3 ShadowCasterViewPosition;
        public readonly Vector3? SkipCastersContainingLight;
        public readonly IModelFactory? ModelFactory;
        public readonly ITextureFactory TextureFactory;
        public readonly bool CanMerge;
        public readonly bool CacheOnly;

        public OpaqueCollectContext(
            bool hasFrustum,
            in Frustum frustum,
            in SceneView lodView,
            float shadowCasterMaxDistanceSq,
            Vector3 shadowCasterViewPosition,
            Vector3? skipCastersContainingLight,
            IModelFactory? modelFactory,
            ITextureFactory textureFactory,
            bool canMerge,
            bool cacheOnly)
        {
            HasFrustum = hasFrustum;
            Frustum = frustum;
            LodView = lodView;
            ShadowCasterMaxDistanceSq = shadowCasterMaxDistanceSq;
            ShadowCasterViewPosition = shadowCasterViewPosition;
            SkipCastersContainingLight = skipCastersContainingLight;
            ModelFactory = modelFactory;
            TextureFactory = textureFactory;
            CanMerge = canMerge;
            CacheOnly = cacheOnly;
        }
    }

    private static void MaterializeOpaqueCandidates(Context context, bool canMerge, ref PassStats stats)
    {
        OpaqueCandidates.Clear();
        foreach (var (entity, modelRenderer, transformComponent) in
                 context.View<ModelRendererComponent, TransformComponent>())
        {
            stats.Renderers++;
            if (ShouldSkipForEntityVisibility(entity))
                continue;

            if (ShouldSkipForVisibilityZone(modelRenderer.VisibilityZoneEntityId, context, ref stats))
                continue;

            var world = ModelMeshPivot.ToDrawTransform(
                transformComponent.GetWorldTransform(),
                modelRenderer.Pivot);
            var mergeCandidate = canMerge && TrackAndClassifyForMerge(entity, world);
            OpaqueCandidates.Add(new OpaqueCandidate
            {
                Entity = entity,
                Renderer = modelRenderer,
                World = world,
                MergeCandidate = mergeCandidate
            });
        }
    }

    private static void CollectOpaqueSerial(
        Context context,
        IGraphics3D graphics3D,
        OpaqueCollectContext ctx,
        ref PassStats stats)
    {
        for (var i = 0; i < OpaqueCandidates.Count; i++)
        {
            var candidate = OpaqueCandidates[i];
            CollectCandidate(i, in candidate, in ctx, context, graphics3D, buffers: null, ref stats);
        }
    }

    private static void CollectOpaqueParallel(
        Context context,
        IGraphics3D graphics3D,
        OpaqueCollectContext ctx,
        ref PassStats stats)
    {
        var n = OpaqueCandidates.Count;
        var degree = System.Math.Clamp(Environment.ProcessorCount, 1, 16);
        EnsureWorkerBuffers(degree);
        var parallelCtx = ctx;

        try
        {
            Parallel.For(0, degree, worker =>
            {
                var buffer = _workerBuffers![worker];
                buffer.ClearForPass();
                var start = worker * n / degree;
                var end = (worker + 1) * n / degree;
                for (var i = start; i < end; i++)
                {
                    var candidate = OpaqueCandidates[i];
                    var localStats = buffer.Stats;
                    CollectCandidate(i, in candidate, in parallelCtx, context: null, graphics3D: null, buffer,
                        ref localStats);
                    buffer.Stats = localStats;
                }
            });
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Opaque parallel collect failed — falling back to serial");
            ClearSharedCollectQueues();
            ImmediateCubes.Clear();
            DeferredCandidateIndices.Clear();
            var serialCtx = new OpaqueCollectContext(
                ctx.HasFrustum, ctx.Frustum, ctx.LodView, ctx.ShadowCasterMaxDistanceSq,
                ctx.ShadowCasterViewPosition, ctx.SkipCastersContainingLight,
                ctx.ModelFactory, ctx.TextureFactory, ctx.CanMerge, cacheOnly: false);
            CollectOpaqueSerial(context, graphics3D, serialCtx, ref stats);
            return;
        }

        MergeWorkerBuffers(degree, ref stats);
        ReplayImmediateCubes(graphics3D, ctx.TextureFactory, ref stats);
        ResolveDeferredCandidates(context, graphics3D, ctx, ref stats);
    }

    private static void EnsureWorkerBuffers(int degree)
    {
        if (_workerBuffers is { Length: var len } && len >= degree)
            return;

        var next = new CollectBuffers[degree];
        for (var i = 0; i < degree; i++)
            next[i] = _workerBuffers is not null && i < _workerBuffers.Length
                ? _workerBuffers[i]
                : new CollectBuffers();
        _workerBuffers = next;
    }

    private static void ClearSharedCollectQueues()
    {
        foreach (var batch in MeshBatches.Values)
        {
            batch.Clear();
            BatchLists.Push(batch);
        }

        MeshBatches.Clear();
        foreach (var members in MergeGroups.Values)
            members.Clear();
        // MergeGroups keys stay; FlushMergeGroups clears members — keep consistent with serial
        MergeGroups.Clear();
    }

    private static void MergeWorkerBuffers(int degree, ref PassStats stats)
    {
        ImmediateCubes.Clear();
        DeferredCandidateIndices.Clear();
        for (var w = 0; w < degree; w++)
        {
            var buffer = _workerBuffers![w];
            foreach (var (key, list) in buffer.MeshBatches)
            {
                if (list.Count == 0)
                    continue;
                if (!MeshBatches.TryGetValue(key, out var shared))
                {
                    shared = BatchLists.Count > 0 ? BatchLists.Pop() : [];
                    MeshBatches[key] = shared;
                }

                shared.AddRange(list);
            }

            foreach (var (key, members) in buffer.MergeGroups)
            {
                if (members.Count == 0)
                    continue;
                if (!MergeGroups.TryGetValue(key, out var shared))
                {
                    shared = [];
                    MergeGroups[key] = shared;
                }

                shared.AddRange(members);
            }

            ImmediateCubes.AddRange(buffer.ImmediateCubes);
            DeferredCandidateIndices.AddRange(buffer.Deferred);
            stats = Accumulate(stats, buffer.Stats);
        }

        if (ImmediateCubes.Count > 1)
            ImmediateCubes.Sort(static (a, b) => a.Entity.Id.CompareTo(b.Entity.Id));
    }

    private static void ReplayImmediateCubes(
        IGraphics3D graphics3D,
        ITextureFactory textureFactory,
        ref PassStats stats)
    {
        for (var i = 0; i < ImmediateCubes.Count; i++)
        {
            var draw = ImmediateCubes[i];
            if (!string.IsNullOrWhiteSpace(draw.Renderer.TexturePath))
                DrawCubeWithTexture(graphics3D, textureFactory, draw.Renderer, draw.World, draw.Entity,
                    draw.Factors, draw.Emissive);
            else
                graphics3D.DrawCube(draw.World, draw.Color, draw.Entity.Id,
                    metallic: draw.Factors.Metallic, roughness: draw.Factors.Roughness, ao: draw.Factors.Ao,
                    emissive: draw.Emissive);
            stats.CubeDraws++;
        }

        ImmediateCubes.Clear();
    }

    private static void ResolveDeferredCandidates(
        Context context,
        IGraphics3D graphics3D,
        OpaqueCollectContext parallelCtx,
        ref PassStats stats)
    {
        if (DeferredCandidateIndices.Count == 0)
            return;

        var serialCtx = new OpaqueCollectContext(
            parallelCtx.HasFrustum, parallelCtx.Frustum, parallelCtx.LodView,
            parallelCtx.ShadowCasterMaxDistanceSq, parallelCtx.ShadowCasterViewPosition,
            parallelCtx.SkipCastersContainingLight, parallelCtx.ModelFactory, parallelCtx.TextureFactory,
            parallelCtx.CanMerge, cacheOnly: false);

        // Stable order for determinism vs serial when only misses defer.
        DeferredCandidateIndices.Sort();
        for (var i = 0; i < DeferredCandidateIndices.Count; i++)
        {
            var index = DeferredCandidateIndices[i];
            var candidate = OpaqueCandidates[index];
            CollectCandidate(index, in candidate, in serialCtx, context, graphics3D, buffers: null, ref stats);
        }

        DeferredCandidateIndices.Clear();
    }

    private static void CollectCandidate(
        int candidateIndex,
        in OpaqueCandidate candidate,
        in OpaqueCollectContext ctx,
        Context? context,
        IGraphics3D? graphics3D,
        CollectBuffers? buffers,
        ref PassStats stats)
    {
        var modelRenderer = candidate.Renderer;
        var transform = candidate.World;
        var entity = candidate.Entity;
        var mergeCandidate = candidate.MergeCandidate;

        if (!TryResolveOpaqueSurface(
                modelRenderer, transform, ctx.ModelFactory, ctx.CacheOnly,
                out _, out var kind, out var model, out var submeshIndex, out var needsLoad))
        {
            if (needsLoad && buffers is not null)
            {
                buffers.Deferred.Add(candidateIndex);
                return;
            }

            if (modelRenderer.MeshIndex is int meshIndex
                && !string.IsNullOrWhiteSpace(modelRenderer.ModelPath)
                && ctx.ModelFactory != null)
            {
                var resolved = ResolveModelAssetPath(modelRenderer.ModelPath);
                var loaded = ctx.CacheOnly
                    ? (ctx.ModelFactory.TryGetCached(resolved, out var cached) ? cached : null)
                    : ctx.ModelFactory.Create(resolved);
                if (loaded != null && (meshIndex < 0 || meshIndex >= loaded.Submeshes.Count))
                    stats.MissingMeshIndex++;
            }

            return;
        }

        if (needsLoad && buffers is not null)
        {
            buffers.Deferred.Add(candidateIndex);
            return;
        }

        var tint = modelRenderer.Color;
        if (kind == OpaqueSurfaceKind.UnitCube)
        {
            if (!string.IsNullOrWhiteSpace(modelRenderer.ModelPath) && !ctx.CacheOnly)
            {
                var resolvedPath = ResolveModelAssetPath(modelRenderer.ModelPath);
                if (WarnedFailedModels.Add(resolvedPath))
                    Logger.Warning(
                        "Failed to load model assetPath={ModelPath} resolved={ResolvedPath} — drawing unit cube instead",
                        modelRenderer.ModelPath, resolvedPath);
            }

            var factors = ResolvePbr(modelRenderer);
            var emissive = CombineEmissive(Vector3.Zero, modelRenderer);
            if (mergeCandidate)
            {
                // Texture create is GL — never on a worker.
                if (buffers is not null && !string.IsNullOrWhiteSpace(modelRenderer.TexturePath))
                {
                    buffers.Deferred.Add(candidateIndex);
                    return;
                }

                if (graphics3D is null && buffers is null)
                    return;

                if (!TryResolveCubeTexture(ctx.TextureFactory, modelRenderer, out var cubeTexture))
                    return;

                var key = MakeMergeKey(tint, cubeTexture, null, factors, emissive);
                var member = new MergeMember(
                    entity.Id, null, transform, cubeTexture, modelRenderer.TilingFactor);
                if (buffers is not null)
                    AppendMergeMemberLocal(buffers, key, member);
                else
                    AppendMergeMember(key, member);
                stats.Vertices += UnitCubeVertexCount;
                stats.Indices += UnitCubeIndexCount;
                return;
            }

            if (ShouldSkipOpaqueDraw(ctx.HasFrustum, ctx.Frustum, transform, Aabb.UnitCube,
                    ctx.ShadowCasterMaxDistanceSq, ctx.ShadowCasterViewPosition, ctx.SkipCastersContainingLight,
                    ref stats))
                return;

            var immediate = new ImmediateCubeDraw
            {
                Entity = entity,
                Renderer = modelRenderer,
                World = transform,
                Color = modelRenderer.Color,
                Factors = factors,
                Emissive = emissive
            };

            if (buffers is not null)
            {
                buffers.ImmediateCubes.Add(immediate);
                return;
            }

            if (graphics3D is null)
                return;

            if (!string.IsNullOrWhiteSpace(modelRenderer.TexturePath))
                DrawCubeWithTexture(graphics3D, ctx.TextureFactory, modelRenderer, transform, entity, factors,
                    emissive);
            else
                graphics3D.DrawCube(transform, modelRenderer.Color, entity.Id,
                    metallic: factors.Metallic, roughness: factors.Roughness, ao: factors.Ao,
                    emissive: emissive);
            stats.CubeDraws++;
            return;
        }

        if (kind == OpaqueSurfaceKind.SingleSubmesh)
        {
            var submesh = model!.Submeshes[submeshIndex];
            QueueOrMergeSubmesh(modelRenderer, transform, tint, entity.Id, submesh, mergeCandidate, in ctx, buffers,
                ref stats);
            return;
        }

        foreach (var submesh in model!.Submeshes)
            QueueOrMergeSubmesh(modelRenderer, transform, tint, entity.Id, submesh, mergeCandidate, in ctx, buffers,
                ref stats);
    }

    private static void QueueOrMergeSubmesh(
        ModelRendererComponent modelRenderer,
        Matrix4x4 transform,
        Vector4 tint,
        int entityId,
        Mesh submesh,
        bool mergeCandidate,
        in OpaqueCollectContext ctx,
        CollectBuffers? buffers,
        ref PassStats stats)
    {
        var level = MeshLod.Select(submesh, transform, ctx.LodView.ViewPosition, ctx.LodView, modelRenderer.LodBias);
        if (mergeCandidate && submesh.LodCount <= 1 && submesh.BakeVertices is not null)
        {
            if (buffers is not null)
                AppendMergeSubmeshLocal(buffers, modelRenderer, transform, tint, entityId, submesh, ref stats);
            else
                AppendMergeSubmesh(modelRenderer, transform, tint, entityId, submesh, ref stats);
            return;
        }

        if (submesh.Bounds is { } bounds
            && ShouldSkipOpaqueDraw(ctx.HasFrustum, ctx.Frustum, transform, bounds, ctx.ShadowCasterMaxDistanceSq,
                ctx.ShadowCasterViewPosition, ctx.SkipCastersContainingLight, ref stats))
            return;

        if (buffers is not null)
            QueueSubmeshLocal(buffers, modelRenderer, transform, tint, entityId, submesh, level, ref stats);
        else
            QueueSubmesh(modelRenderer, transform, tint, entityId, submesh, level, ref stats);
    }

    private static void AppendMergeMemberLocal(CollectBuffers buffers, MergeMaterialKey key, MergeMember member)
    {
        if (!buffers.MergeGroups.TryGetValue(key, out var members))
        {
            members = [];
            buffers.MergeGroups[key] = members;
        }

        members.Add(member);
    }

    private static void AppendMergeSubmeshLocal(
        CollectBuffers buffers,
        ModelRendererComponent modelRenderer,
        Matrix4x4 transform,
        Vector4 tint,
        int entityId,
        Mesh submesh,
        ref PassStats stats)
    {
        AdoptSubmeshFactors(modelRenderer, submesh);
        var pbr = ResolvePbr(modelRenderer);
        var emissive = CombineEmissive(submesh.EmissiveFactor, modelRenderer);
        var key = MakeMergeKey(tint, submesh.DiffuseTexture, submesh, pbr, emissive);
        AppendMergeMemberLocal(buffers, key, new MergeMember(entityId, submesh, transform, null, 0f));
        stats.Vertices += submesh.VertexCount;
        stats.Indices += submesh.GetIndexCount();
    }

    private static void QueueSubmeshLocal(
        CollectBuffers buffers,
        ModelRendererComponent modelRenderer,
        Matrix4x4 transform,
        Vector4 tint,
        int entityId,
        Mesh submesh,
        int level,
        ref PassStats stats)
    {
        stats.Instances++;
        stats.Vertices += submesh.VertexCount;
        stats.Indices += submesh.IndexCountAt(level);

        AdoptSubmeshFactors(modelRenderer, submesh);
        var pbr = ResolvePbr(modelRenderer);
        var emissive = CombineEmissive(submesh.EmissiveFactor, modelRenderer);
        var key = new MeshBatchKey(submesh, tint, pbr.Metallic, pbr.Roughness, pbr.Ao, emissive, level);
        if (!buffers.MeshBatches.TryGetValue(key, out var batch))
        {
            batch = [];
            buffers.MeshBatches[key] = batch;
        }

        batch.Add(new MeshDrawInstance
        {
            Transform = transform,
            EntityId = entityId,
            Tint = tint,
            Metallic = pbr.Metallic,
            Roughness = pbr.Roughness,
            Ao = pbr.Ao,
            Emissive = emissive
        });
    }

    /// <summary>
    /// PathBuilder.Resolve needs a project context for relative paths. Unit tests often pass
    /// rooted Unix paths or Windows drive letters on macOS — those must not touch AssetsPath.
    /// </summary>
    private static string ResolveModelAssetPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return path;

        var normalized = path.Replace('\\', '/');
        if (Path.IsPathRooted(normalized))
            return Path.GetFullPath(normalized);

        // Windows-style absolute path on a non-Windows host (common in pipeline unit tests).
        if (normalized.Length >= 3
            && char.IsAsciiLetter(normalized[0])
            && normalized[1] == ':'
            && normalized[2] == '/')
            return normalized;

        return PathBuilder.Resolve(path);
    }

    private static bool TryResolveOpaqueSurface(
        ModelRendererComponent modelRenderer,
        Matrix4x4 transform,
        IModelFactory? modelFactory,
        bool cacheOnly,
        out PointShadowCache.CasterPose casterPose,
        out OpaqueSurfaceKind kind,
        out Model? model,
        out int submeshIndex,
        out bool needsLoad)
    {
        needsLoad = false;
        casterPose = default;
        kind = default;
        model = null;
        submeshIndex = -1;

        if (string.IsNullOrWhiteSpace(modelRenderer.ModelPath))
        {
            casterPose = new PointShadowCache.CasterPose(transform, Aabb.UnitCube, true);
            kind = OpaqueSurfaceKind.UnitCube;
            return true;
        }

        if (modelFactory == null)
            return false;

        var resolvedPath = ResolveModelAssetPath(modelRenderer.ModelPath);
        if (cacheOnly)
        {
            if (!modelFactory.TryGetCached(resolvedPath, out model))
            {
                needsLoad = true;
                return false;
            }
        }
        else
        {
            model = modelFactory.Create(resolvedPath);
        }

        if (model == null)
        {
            casterPose = new PointShadowCache.CasterPose(transform, Aabb.UnitCube, true);
            kind = OpaqueSurfaceKind.UnitCube;
            return true;
        }

        if (modelRenderer.MeshIndex is int meshIndex)
        {
            if (meshIndex < 0 || meshIndex >= model.Submeshes.Count)
                return false;

            submeshIndex = meshIndex;
            var submesh = model.Submeshes[meshIndex];
            casterPose = submesh.Bounds is { } singleBounds
                ? new PointShadowCache.CasterPose(transform, singleBounds, true)
                : new PointShadowCache.CasterPose(transform, default, false);
            kind = OpaqueSurfaceKind.SingleSubmesh;
            return true;
        }

        if (modelRenderer.SuppressDraw)
            return false;

        var hasBounds = true;
        var min = new Vector3(float.PositiveInfinity);
        var max = new Vector3(float.NegativeInfinity);
        foreach (var submesh in model.Submeshes)
        {
            if (submesh.Bounds is not { } bounds)
            {
                hasBounds = false;
                break;
            }

            min = Vector3.Min(min, bounds.Min);
            max = Vector3.Max(max, bounds.Max);
        }

        casterPose = hasBounds
            ? new PointShadowCache.CasterPose(transform, new Aabb(min, max), true)
            : new PointShadowCache.CasterPose(transform, default, false);
        kind = OpaqueSurfaceKind.AllSubmeshes;
        return true;
    }
}
