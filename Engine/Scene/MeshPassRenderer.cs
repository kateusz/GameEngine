using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using Engine.Renderer;
using Engine.Renderer.Buffers;
using Engine.Renderer.Buffers.VertexArray;
using Engine.Renderer.Meshes;
using Engine.Renderer.Pipeline;

namespace Engine.Scene;

internal sealed class MeshPassRenderer(IGraphics3D graphics3D, StaticMeshCache cache,
    IVertexArrayFactory? vertexArrayFactory, IVertexBufferFactory? vertexBufferFactory, IIndexBufferFactory? indexBufferFactory)
{
    private readonly record struct MeshBatchKey(
        Mesh Mesh, Vector4 Tint, float Metallic, float Roughness, float Ao, Vector3 Emissive, int Level);

    private readonly Dictionary<MeshBatchKey, List<MeshDrawInstance>> MeshBatches = new();
    private readonly Stack<List<MeshDrawInstance>> BatchLists = new();

    internal MeshPassStats DrawOpaque3D(
        RenderSceneData data, Matrix4x4 cullMatrix, in SceneView lodView,
        float shadowCasterMaxDistanceSq = 0f, Vector3 shadowCasterViewPosition = default,
        Vector3? skipCastersContainingLight = null)
    {
        var stats = new MeshPassStats();
        var start = Stopwatch.GetTimestamp();
        var hasFrustum = Frustum.TryFromClip(cullMatrix, out var frustum);
        var canMerge = vertexArrayFactory is not null && vertexBufferFactory is not null && indexBufferFactory is not null;
        var collectStart = Stopwatch.GetTimestamp();
        try
        {
            foreach (var item in data.Objects)
            {
                stats.Renderers++;
                if (!item.Visible) continue;
                if (item.ZoneCulled) { stats.ZoneCulled++; continue; }
                if (!item.Valid) { if (item.MissingMeshIndex) stats.MissingMeshIndex++; continue; }
                var transform = item.World;
                var mergeCandidate = canMerge && cache.TrackAndClassifyForMerge(item.EntityId, item.MovingBody, transform);
                if (item.IsCube)
                {
                    if (item.TextureFailed) continue;
                    var factors = item.Pbr;
                    if (mergeCandidate)
                    {
                        cache.AppendMergeMember(cache.MakeMergeKey(item.Tint, item.Texture, null, factors, item.Emissive),
                            new StaticMeshCache.MergeMember(item.EntityId, null, transform, item.Texture, item.Tiling));
                        stats.Vertices += 24; stats.Indices += 36;
                        continue;
                    }
                    if (ShouldSkipOpaqueDraw(hasFrustum, frustum, transform, Aabb.UnitCube, shadowCasterMaxDistanceSq,
                            shadowCasterViewPosition, skipCastersContainingLight, ref stats)) continue;
                    graphics3D.DrawCube(transform, item.Tint, item.EntityId, item.Texture, item.Tiling,
                        factors.Metallic, factors.Roughness, factors.Ao, item.Emissive);
                    stats.CubeDraws++;
                    continue;
                }
                for (var i = item.SurfaceStart; i < item.SurfaceStart + item.SurfaceCount; i++)
                {
                    var surface = data.Surfaces[i];
                    var submesh = surface.Mesh;
                    var level = MeshLod.Select(submesh, transform, lodView.ViewPosition, lodView, item.LodBias);
                    if (mergeCandidate && submesh.LodCount <= 1 && submesh.BakeVertices is not null)
                    {
                        cache.AppendMergeMember(cache.MakeMergeKey(item.Tint, submesh.DiffuseTexture, submesh, surface.Pbr, surface.Emissive),
                            new StaticMeshCache.MergeMember(item.EntityId, submesh, transform, null, 0f));
                        stats.Vertices += submesh.VertexCount; stats.Indices += submesh.GetIndexCount();
                        continue;
                    }
                    if (submesh.Bounds is { } bounds && ShouldSkipOpaqueDraw(hasFrustum, frustum, transform, bounds,
                            shadowCasterMaxDistanceSq, shadowCasterViewPosition, skipCastersContainingLight, ref stats)) continue;
                    QueueSubmesh(surface, transform, item.Tint, item.EntityId, submesh, level, ref stats);
                }
            }
        }
        finally
        {
            stats.CollectCpuMs = Stopwatch.GetElapsedTime(collectStart).TotalMilliseconds;
            var meshFlushStart = Stopwatch.GetTimestamp();
            FlushMeshBatches(graphics3D, ref stats);
            stats.MeshFlushCpuMs = Stopwatch.GetElapsedTime(meshFlushStart).TotalMilliseconds;
            var mergeFlushStart = Stopwatch.GetTimestamp();
            FlushMergeGroups(graphics3D, hasFrustum, in frustum, shadowCasterMaxDistanceSq,
                shadowCasterViewPosition, skipCastersContainingLight, ref stats,
                vertexArrayFactory, vertexBufferFactory, indexBufferFactory);
            stats.MergeFlushCpuMs = Stopwatch.GetElapsedTime(mergeFlushStart).TotalMilliseconds;
        }
        stats.CpuMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        return stats;
    }

    private bool ShouldSkipOpaqueDraw(
        bool hasFrustum,
        in Frustum frustum,
        Matrix4x4 world,
        Aabb bounds,
        float shadowCasterMaxDistanceSq,
        Vector3 shadowCasterViewPosition,
        Vector3? skipCastersContainingLight,
        ref MeshPassStats stats)
    {
        if (hasFrustum && frustum.IsOutside(world, bounds))
        {
            stats.Culled++;
            return true;
        }

        if (shadowCasterMaxDistanceSq > 0f
            && Aabb.ClosestPointDistanceSquared(shadowCasterViewPosition, world, bounds) > shadowCasterMaxDistanceSq)
        {
            stats.ShadowCasterCulled++;
            return true;
        }

        // Marker/housing cubes sit on the lamp; casting them fills every cubemap face and kills the light.
        if (skipCastersContainingLight is { } lamp && Aabb.ContainsPoint(lamp, world, bounds))
        {
            stats.ShadowCasterCulled++;
            return true;
        }

        return false;
    }

    private void QueueSubmesh(
        SurfaceDrawData surface,
        Matrix4x4 transform,
        Vector4 tint,
        int entityId,
        Mesh submesh,
        int level,
        ref MeshPassStats stats)
    {
        stats.Instances++;
        stats.Vertices += submesh.VertexCount;
        stats.Indices += submesh.IndexCountAt(level);

        var pbr = surface.Pbr;
        var emissive = surface.Emissive;
        var key = new MeshBatchKey(submesh, tint, pbr.Metallic, pbr.Roughness, pbr.Ao, emissive, level);
        if (!MeshBatches.TryGetValue(key, out var batch))
        {
            batch = BatchLists.Count > 0 ? BatchLists.Pop() : [];
            MeshBatches[key] = batch;
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

    private void FlushMeshBatches(IGraphics3D graphics3D, ref MeshPassStats stats)
    {
        try
        {
            foreach (var (key, batch) in MeshBatches)
            {
                var count = batch.Count;
                if (count >= 2)
                {
                    stats.MultiMaterialDraws++;
                    stats.MaxBatchInstances = System.Math.Max(stats.MaxBatchInstances, count);
                }
                else
                    stats.SingleMaterialDraws++;

                graphics3D.DrawMeshInstances(key.Mesh, CollectionsMarshal.AsSpan(batch), key.Level);
                stats.MeshDraws++;
            }
        }
        finally
        {
            foreach (var batch in MeshBatches.Values)
            {
                batch.Clear();
                BatchLists.Push(batch);
            }

            MeshBatches.Clear();
        }
    }

    private void FlushMergeGroups(
        IGraphics3D graphics3D,
        bool hasFrustum,
        in Frustum frustum,
        float shadowCasterMaxDistanceSq,
        Vector3 shadowCasterViewPosition,
        Vector3? skipCastersContainingLight,
        ref MeshPassStats stats,
        IVertexArrayFactory? vertexArrayFactory,
        IVertexBufferFactory? vertexBufferFactory,
        IIndexBufferFactory? indexBufferFactory)
    {
        try
        {
            foreach (var (key, members) in cache.PendingGroups)
            {
                if (members.Count == 0)
                    continue;

                Aabb groupBox = default;
                var hasGroupBox = members.Count >= 2 && cache.TryGetGroupBox(members, out groupBox);
                var cached = members.Count >= 2 ? cache.FindMatching(key, members) : null;
                if (cached is not null)
                    cache.Touch(key);

                if (hasGroupBox
                    && ShouldSkipOpaqueDraw(hasFrustum, frustum, Matrix4x4.Identity, groupBox,
                        shadowCasterMaxDistanceSq, shadowCasterViewPosition, null, ref stats))
                {
                    continue;
                }

                var checkCastersIndividually = skipCastersContainingLight is { } lamp
                    && (!hasGroupBox || Aabb.ContainsPoint(lamp, Matrix4x4.Identity, groupBox));
                var entries = cached;
                if (members.Count < 2 || checkCastersIndividually || (entries is null && !cache.TryBuildCombined(key, members,
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

                cache.Touch(key);
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
            cache.EndPass();
        }
    }

    private bool ShouldSkipMergeMember(
        in StaticMeshCache.MergeMember member,
        bool hasFrustum,
        in Frustum frustum,
        float shadowCasterMaxDistanceSq,
        Vector3 shadowCasterViewPosition,
        Vector3? skipCastersContainingLight,
        ref MeshPassStats stats) =>
        cache.TryGetLocalBounds(member, out var bounds)
        && ShouldSkipOpaqueDraw(hasFrustum, frustum, member.World, bounds, shadowCasterMaxDistanceSq,
            shadowCasterViewPosition, skipCastersContainingLight, ref stats);

    private void ReplayAsInstance(
        IGraphics3D graphics3D,
        in StaticMeshCache.MergeMaterialKey key,
        in StaticMeshCache.MergeMember member,
        ref MeshPassStats stats)
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
}
