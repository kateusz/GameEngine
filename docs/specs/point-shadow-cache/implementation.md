# Point Shadow Cache — Implementation

Step-by-step code for the design in `introduction.md` and `developer-guide.md`. Paste the C# into the existing types. The directional shadow pass is not part of this change.

A lamp stays cached until its pose changes or a caster pose hits its previous or current sphere. The cubemap is owned by the lamp entity id. The uniform slot is still 0–7 for the current frame.

## 1. Carry the entity id on the lamp

`PointLightData` gains the id at the end so existing positional calls keep compiling.

```csharp
public readonly record struct PointLightData(
    Vector3 Position, Vector3 Color, float Intensity, float Range,
    bool CastsShadow = false, int EntityId = 0);
```

In `ResolvePointLights`, the view element that is currently discarded is the entity. Pass `entity.Id` into the constructor.

**Why:** The pose memory and the cubemap both key off that id. The slot index is not stable.

## 2. Store cubemaps by entity

`IGraphics3D` replaces the face begin signature and adds the cache enable. Every recording substitute of `IGraphics3D` must follow.

```csharp
bool BeginPointShadowFace(
    int lightIndex, int entityId, int face,
    Matrix4x4 viewProjection, Vector3 lightPosition, float range);
bool UseCachedPointShadow(int lightIndex, int entityId);
```

In `Graphics3D`, replace the per-slot framebuffer array with a dictionary and remember which entity is bound to each slot this frame.

```csharp
private readonly Dictionary<int, IFrameBuffer> _pointShadowMapsByEntity = new();
private readonly int[] _pointShadowEntity = new int[LightingMath.MaxPointLights];
```

`SetPointLights` still clears `_pointShadowEnabled`. It also fills `_pointShadowEntity` with `-1`. It does not dispose maps.

```csharp
public bool BeginPointShadowFace(
    int lightIndex, int entityId, int face,
    Matrix4x4 viewProjection, Vector3 lightPosition, float range)
{
    if ((uint)lightIndex >= LightingMath.MaxPointLights || (uint)face >= LightingMath.PointShadowFaceCount)
        return false;

    IFrameBuffer map;
    try
    {
        map = PointShadowMap(entityId);
    }
    catch (Exception)
    {
        _pointShadowEnabled[lightIndex] = false;
        return false;
    }

    _pointShadowEntity[lightIndex] = entityId;
    _shadowPass = true;
    _pointShadowPass = true;
    _activePointMap = map;
    map.Bind();
    map.BindDepthCubemapFace(face);
    rendererApi.SetDepthTest(true);
    rendererApi.SetDepthWrite(true);
    rendererApi.Clear();
    rendererApi.SetCullFrontFaces(true);

    _pointDepthShader.Bind();
    _pointDepthShader.SetMat4(ViewProjectionUniform, viewProjection);
    _pointDepthShader.SetFloat3("u_LightPosition", lightPosition);
    _pointDepthShader.SetFloat("u_LightRange", range);
    _pointShadowEnabled[lightIndex] = true;
    return true;
}

public bool UseCachedPointShadow(int lightIndex, int entityId)
{
    if ((uint)lightIndex >= LightingMath.MaxPointLights)
        return false;
    if (!_pointShadowMapsByEntity.ContainsKey(entityId))
        return false;

    _pointShadowEntity[lightIndex] = entityId;
    _pointShadowEnabled[lightIndex] = true;
    return true;
}

private IFrameBuffer PointShadowMap(int entityId)
{
    if (_pointShadowMapsByEntity.TryGetValue(entityId, out var existing))
        return existing;

    var size = (uint)LightingMath.PointShadowFaceResolution;
    var spec = new FrameBufferSpecification(size, size)
    {
        AttachmentsSpec = new FrameBufferAttachmentSpecification([
            new FrameBufferTextureSpecification(FrameBufferTextureFormat.DepthCubemap)
        ])
    };
    var map = frameBuffers.Create(spec);
    _pointShadowMapsByEntity[entityId] = map;
    return map;
}
```

`UploadFrame` binds the map for `_pointShadowEntity[i]` when that slot is enabled. `Dispose` disposes every value in `_pointShadowMapsByEntity` and clears it.

**Why:** `UseCachedPointShadow` is the path that samples yesterday's faces. Begin still clears and redraws one face, and it reuses the same framebuffer when the entity already has one.

## 3. Sphere test

Add this next to the other point-shadow helpers in `LightingMath`.

```csharp
internal static bool PointShadowSphereHits(Vector3 center, float range, Matrix4x4 world, Aabb local)
{
    var min = new Vector3(float.PositiveInfinity);
    var max = new Vector3(float.NegativeInfinity);
    for (var z = 0; z < 2; z++)
    for (var y = 0; y < 2; y++)
    for (var x = 0; x < 2; x++)
    {
        var corner = new Vector3(
            x == 0 ? local.Min.X : local.Max.X,
            y == 0 ? local.Min.Y : local.Max.Y,
            z == 0 ? local.Min.Z : local.Max.Z);
        var transformed = Vector3.Transform(corner, world);
        min = Vector3.Min(min, transformed);
        max = Vector3.Max(max, transformed);
    }

    var closest = Vector3.Clamp(center, min, max);
    return Vector3.DistanceSquared(closest, center) <= range * range;
}
```

**Why:** Eight corners become a world axis-aligned box, which is the same conservative box frustum culling uses. The closest-point test catches a sphere that overlaps a face without containing a corner.

## 4. Pose memory

Add `Engine/Scene/PointShadowCache.cs`. The pipeline is already a static type, and this memory has the same lifetime.

```csharp
using System.Numerics;
using Engine.Renderer;

namespace Engine.Scene;

internal static class PointShadowCache
{
    internal readonly record struct CasterPose(Matrix4x4 World, Aabb Bounds, bool HasBounds);
    internal readonly record struct LampPose(Vector3 Position, float Range);

    private static readonly Dictionary<int, CasterPose> Casters = new();
    private static readonly Dictionary<int, LampPose> Lamps = new();
    private static readonly HashSet<int> Dirty = new();
    private static readonly HashSet<int> Seen = new();
    private static bool _stale = true;

    public static void Clear()
    {
        Casters.Clear();
        Lamps.Clear();
        Dirty.Clear();
        Seen.Clear();
        _stale = true;
    }

    public static void MarkStale() => _stale = true;

    public static bool NeedsRedraw(
        int lampId, Vector3 position, float range,
        IReadOnlyList<(int Id, CasterPose Pose)> current)
    {
        if (_stale || Dirty.Contains(lampId) || !Lamps.TryGetValue(lampId, out var previousLamp))
            return true;
        if (previousLamp.Position != position || previousLamp.Range != range)
            return true;

        Seen.Clear();
        foreach (var (id, pose) in current)
        {
            Seen.Add(id);
            if (!Casters.TryGetValue(id, out var previous))
            {
                if (!pose.HasBounds || Sphere(position, range, pose))
                    return true;
                continue;
            }

            var moved = previous.World != pose.World
                || previous.HasBounds != pose.HasBounds
                || previous.Bounds != pose.Bounds;
            if (!moved)
                continue;
            if (!pose.HasBounds || !previous.HasBounds)
                return true;
            if (Sphere(previousLamp.Position, previousLamp.Range, previous)
                || Sphere(position, range, pose))
                return true;
        }

        foreach (var (id, previous) in Casters)
        {
            if (Seen.Contains(id))
                continue;
            if (!previous.HasBounds || Sphere(previousLamp.Position, previousLamp.Range, previous))
                return true;
        }

        return false;
    }

    public static void RememberDirty(int lampId) => Dirty.Add(lampId);

    public static void RememberClean(int lampId) => Dirty.Remove(lampId);

    public static void Replace(
        IReadOnlyList<(int Id, CasterPose Pose)> casters,
        IReadOnlyList<(int Id, LampPose Pose)> lamps)
    {
        Casters.Clear();
        foreach (var (id, pose) in casters)
            Casters[id] = pose;

        Lamps.Clear();
        foreach (var (id, pose) in lamps)
            Lamps[id] = pose;

        _stale = false;
    }

    private static bool Sphere(Vector3 position, float range, CasterPose pose) =>
        LightingMath.PointShadowSphereHits(position, range, pose.World, pose.Bounds);
}
```

Call `PointShadowCache.Clear()` from `SceneContext.SetScene` before the new scene is published.

**Why:** `NeedsRedraw` is a pure decision over two pose lists, so the tests can call it without a framebuffer. `RememberDirty` survives a distance skip because `Replace` does not clear `Dirty`. A new unbounded caster returns true immediately. A removed unbounded caster hits the last loop and returns true.

## 5. Collect casters

Walk `ModelRendererComponent` plus `TransformComponent` once, before the face loop, and fill a reused list of `(entity.Id, CasterPose)`.

- No model path: pose is the world matrix, `Aabb.UnitCube`, bounds present. This covers solid cubes, textured cubes, and the fallback cube.
- SuppressDraw, a missing model factory, or a mesh index that draws nothing: not a caster.
- Loaded model: if any drawn submesh has no bounds, `HasBounds` is false. Otherwise `Bounds` is the union of those local bounds (`Vector3.Min` / `Vector3.Max` over their min and max) and `HasBounds` is true.

Do not draw in this walk. The face loop still calls `DrawOpaque3D`.

**Why:** The cache must name the same objects the depth pass would rasterize. Union keeps one pose per entity, which is what the sphere test consumes.

## 6. Replace the face loop

When `view.PointShadows` is false, call `PointShadowCache.MarkStale()` and skip the loop.

When it is true, build the caster list and a lamp list of every resolved light with `CastsShadow` (id, position, range), including lamps beyond the shadow distance. Then:

```csharp
Span<Matrix4x4> pointFaces = stackalloc Matrix4x4[LightingMath.PointShadowFaceCount];
for (var i = 0; i < pointCount; i++)
{
    var light = PointLightBuffer[i];
    if (!light.CastsShadow)
        continue;

    var dirty = PointShadowCache.NeedsRedraw(light.EntityId, light.Position, light.Range, casterList);
    var near = Vector3.Distance(view.ViewPosition, light.Position) <= LightingMath.PointShadowDistance;
    if (!dirty)
    {
        if (near)
            PointShadowCache.RememberClean(light.EntityId);
        if (near && !graphics3D.UseCachedPointShadow(i, light.EntityId))
            dirty = true;
        else
            continue;
    }

    PointShadowCache.RememberDirty(light.EntityId);
    if (!near || !LightingMath.TryBuildPointShadowFaces(light.Position, light.Range, pointFaces))
        continue;

    var drew = true;
    for (var face = 0; face < LightingMath.PointShadowFaceCount; face++)
    {
        if (!graphics3D.BeginPointShadowFace(
                i, light.EntityId, face, pointFaces[face], light.Position, light.Range))
        {
            drew = false;
            break;
        }

        try
        {
            DrawOpaque3D(
                context, graphics3D, textureFactory, modelFactory,
                meshDrawCounts: null, pointFaces[face]);
        }
        finally
        {
            graphics3D.EndPointShadowFace();
        }
    }

    if (drew)
        PointShadowCache.RememberClean(light.EntityId);
}

PointShadowCache.Replace(casterList, lampList);
```

`BeginPointShadowFace` failing on a later face has already drawn earlier faces. `drew` stays false, so the lamp remains dirty and the next frame starts all six again. The existing one-time warning stays on that failure.

**Why:** `Replace` runs only on an enabled frame, after the decisions. A disabled view never reaches it. A clean lamp that is far does not enable the map, which keeps today's unshadowed result past the shadow distance.

## 7. Tests

Sphere, in `LightingMathTests`:

```csharp
[Fact]
public void PointShadowSphereHits_UnitCubeAtOrigin_HitsRange10()
{
    LightingMath.PointShadowSphereHits(Vector3.Zero, 10f, Matrix4x4.Identity, Aabb.UnitCube)
        .ShouldBeTrue();
}

[Fact]
public void PointShadowSphereHits_CubeTranslatedBy30_MissesRange10()
{
    LightingMath.PointShadowSphereHits(
            Vector3.Zero, 10f, Matrix4x4.CreateTranslation(30f, 0f, 0f), Aabb.UnitCube)
        .ShouldBeFalse();
}

[Fact]
public void PointShadowSphereHits_JustOutsideAndJustInside()
{
    var outside = Matrix4x4.CreateTranslation(11f, 0f, 0f);
    var inside = Matrix4x4.CreateTranslation(9f, 0f, 0f);
    LightingMath.PointShadowSphereHits(Vector3.Zero, 10f, outside, Aabb.UnitCube).ShouldBeFalse();
    LightingMath.PointShadowSphereHits(Vector3.Zero, 10f, inside, Aabb.UnitCube).ShouldBeTrue();
}
```

Dirty decision, calling `PointShadowCache.Clear()` at the start of each test so the static memory does not leak across tests:

- After `Clear`, `NeedsRedraw` for a lamp id with one cube at the origin and range 10 is true.
- `Replace` with that caster and that lamp, then `NeedsRedraw` with the same lists is false.
- `Replace`, then a caster world translated by 1, is true.
- `Replace` with the cube inside, then current pose translated by 30, is true.
- `Replace`, then the same caster and a lamp position moved by 1, is true.
- `Replace` including an unbounded caster, then the same id with a different matrix and `HasBounds` false, is true.

Pipeline, on the recording substitute. `UseCachedPointShadow` appends the entity id and returns true. `BeginPointShadowFace` records the entity id along with the matrix.

- One casting lamp at the origin, range 10, one cube. First `RenderScene` begins 6 faces. Second `RenderScene` with the same world begins 0 faces and records one cached use.
- Third `RenderScene` after `SetWorldTransform` moves the cube by 1 begins 6 faces.
- A view with `PointShadows: false` begins 0. The next default view begins 6.
- A casting lamp at x = `PointShadowDistance + 1` begins 0. Move a cube that sits on that lamp by 1, keep the lamp far, and it still begins 0. Move the lamp to the origin and it begins 6.

Call `PointShadowCache.Clear()` at the start of these pipeline tests too. The static cache would otherwise make the existing six-face test depend on order.

**Why:** The second frame is the cache. The moved cube is the invalidation. The far lamp that stays dirty is the distance skip. Clearing the static cache keeps the tests independent.
