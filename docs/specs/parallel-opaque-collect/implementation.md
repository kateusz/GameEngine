# Parallel Opaque Collect — Implementation Sketch

C# sketch for this engine. Design only — do not treat these snippets as the finished source tree. Paste into `SceneRenderPipeline` / partials; no new ECS component, serializer, or DryIoc registration.

## Why this shape

- Collect is the measured hotspot; flush stays serial and tiny.
- `Parallel.For` + thread-local buffers beats a new job abstraction for one walk.
- Factory misses and GL stay on the render thread.

## 1. Candidate and worker buffer types

```csharp
private struct OpaqueCandidate
{
    public int EntityId;
    public Matrix4x4 World;
    // Copy fields the serial loop reads from ModelRendererComponent
    // (path, mesh index, color, pbr, lod bias, zone id, pivot, tiling, texture path, …).
}

private sealed class CollectBuffers
{
    public readonly Dictionary<MeshBatchKey, List<MeshDrawInstance>> MeshBatches = new();
    public readonly List<DeferredResolve> Deferred = new();
    public readonly List<ImmediateCubeDraw> ImmediateCubes = new();
    // Merge member lists: same keys as today's merge path, local to this buffer.
    public PassStats Stats;
}

private struct ImmediateCubeDraw
{
    public int EntityId;
    public Matrix4x4 World;
    public Vector4 Color;
    public PbrFactors Factors;
    public Vector3 Emissive;
    public string? TexturePath;
    public Vector2 Tiling;
}

private struct DeferredResolve
{
    public int CandidateIndex;
}
```

Reuse a `List<OpaqueCandidate> OpaqueCandidates` and a `CollectBuffers[] WorkerBuffers` (sized to `MaxDegreeOfParallelism`) as static or scene-pipeline fields: `Clear()` each pass, keep capacity.

## 2. Threshold and entry in `DrawOpaque3D`

```csharp
private const int ParallelCollectThreshold = 64; // tune against IslandDash vs empty scenes
private static bool ForceSerialOpaqueCollect; // tests / debug

private static PassStats DrawOpaque3D(/* existing params */)
{
    var stats = new PassStats();
    var start = Stopwatch.GetTimestamp();
    var hasFrustum = Frustum.TryFromClip(cullMatrix, out var frustum);
    var canMerge = vertexArrayFactory is not null
        && vertexBufferFactory is not null
        && indexBufferFactory is not null;

    var collectStart = Stopwatch.GetTimestamp();
    try
    {
        MaterializeOpaqueCandidates(context);
        var collectCtx = new OpaqueCollectContext(
            hasFrustum, frustum, lodView, shadowCasterMaxDistanceSq,
            shadowCasterViewPosition, skipCastersContainingLight,
            modelFactory, textureFactory, canMerge);

        if (OpaqueCandidates.Count < ParallelCollectThreshold || ForceSerialOpaqueCollect)
            CollectOpaqueSerial(collectCtx, ref stats);
        else
            CollectOpaqueParallel(collectCtx, ref stats);

        ResolveDeferredOnRenderThread(collectCtx, ref stats);
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
```

`MaterializeOpaqueCandidates` fills `OpaqueCandidates` from `context.View<ModelRendererComponent, TransformComponent>()` (world matrix via `ModelMeshPivot.ToDrawTransform` as today). Zone prep (`PrepareActiveVisibilityZones`) stays before materialize, still on the render thread.

## 3. Serial collect

Move the body of today’s `foreach` into `CollectOpaqueSerial`, writing into the existing shared `MeshBatches` / merge state. Behavior must remain the reference for equivalence tests.

## 4. Parallel collect

```csharp
private static void CollectOpaqueParallel(in OpaqueCollectContext ctx, ref PassStats stats)
{
    var n = OpaqueCandidates.Count;
    var degree = Math.Clamp(Environment.ProcessorCount, 1, WorkerBuffers.Length);
    EnsureWorkerBuffers(degree);

    try
    {
        Parallel.For(0, degree, worker =>
        {
            var buffer = WorkerBuffers[worker];
            buffer.ClearForPass();
            var start = worker * n / degree;
            var end = (worker + 1) * n / degree;
            for (var i = start; i < end; i++)
                CollectOne(in OpaqueCandidates.GetRef(i), i, in ctx, buffer);
        });
    }
    catch (Exception ex)
    {
        Logger.Warning(ex, "Opaque parallel collect failed — falling back to serial");
        ClearSharedCollectState();
        CollectOpaqueSerial(in ctx, ref stats);
        return;
    }

    MergeWorkerBuffersIntoShared(degree, ref stats);
}
```

`CollectOne` mirrors serial branches (`TryResolveOpaqueSurface` **cache-only**, `ShouldSkipOpaqueDraw`, `QueueSubmesh` / `AppendMergeMember` into `buffer`). Non-merge unit cubes append `ImmediateCubeDraw` instead of calling `graphics3D`. After `MergeWorkerBuffersIntoShared`, sort immediate cubes by `EntityId` and draw them on the render thread before `FlushMeshBatches`. On cache miss:

```csharp
buffer.Deferred.Add(new DeferredResolve { CandidateIndex = index });
return;
```

Do not call `modelFactory.Create` or texture upload from `CollectOne` in the parallel path.

## 5. Join and deferred resolve

```csharp
private static void MergeWorkerBuffersIntoShared(int degree, ref PassStats stats)
{
    for (var w = 0; w < degree; w++)
    {
        var buffer = WorkerBuffers[w];
        foreach (var (key, list) in buffer.MeshBatches)
            AppendOrGetSharedBatch(key).AddRange(list);
        MergeLocalMergeMembers(buffer);
        Accumulate(ref stats, buffer.Stats);
        // Drain buffer.Deferred into a render-thread DeferredList
    }
}

private static void ResolveDeferredOnRenderThread(in OpaqueCollectContext ctx, ref PassStats stats)
{
    // For each deferred candidate: Create/resolve on this thread (warnings, cube fallback),
    // then queue into shared MeshBatches / merge as the serial path would.
}
```

`WarnedFailedModels` updates only here or in serial collect.

## 6. System priority, DI, serialization

| Concern | Action |
|---------|--------|
| Component | None |
| System priority | Unchanged — still inside `SceneRenderSystem` (150) / editor viewport calling `RenderScene` |
| DryIoc | None |
| Serialization | None |
| Pipeline seat | `DrawOpaque3D` only; 2D path untouched |
| Editor vs Runtime | Shared; no ImGui dependency |

## 7. Stats

Keep `ColorCollectCpuMs` / `CollectCpuMs` as wall time around materialize + collect + join + deferred. Do not sum per-worker times into that field (would look like a regression while the frame got faster).

## 8. Tests worth adding

| Test | Assert |
|------|--------|
| `ParallelCollect_MatchesSerial_BatchAndCullCounts` | Force serial vs force parallel on a synthetic context with N ≥ threshold |
| `ParallelCollect_BelowThreshold_DoesNotParallelize` | Optional: hook or degree==1 path; counts match |
| `ParallelCollect_DeferredMiss_ResolvesOnCallerThread` | Thread-aware fake `IModelFactory` |
| `ParallelCollect_WorkerFault_FallsBackToSerial` | Inject fail flag in test-only hook |
| `ParallelCollect_ShadowCasterParams_MatchSerial` | Distance / containing-light skips |
| Existing `SceneRenderPipeline*Tests` | Still pass with parallel enabled by default above threshold |

## 9. Verification on IslandDash

After implementation: compare `ColorCollectCpuMs`, `ShadowCpuMs`, frame average/median against the baseline report. Expect collect wall time down and frame FPS up; do not gate merge on p99 hitch reduction.
