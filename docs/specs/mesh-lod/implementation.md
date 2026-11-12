# Mesh LOD — Implementation

C# sketch for the simplifier, the `.mesh` version, the selector, and the opaque walk. Paste into the existing types. Do not add an `ISystem`. Do not register the simplifier in DryIoc: it calls the Evergine binding directly, and the selector is a static function.

Package, on `Engine/Engine.csproj` only:

```xml
<PackageReference Include="Evergine.Bindings.MeshOptimizer" Version="2026.10.1.20" />
```

`RuntimeMeshFormat.Version` becomes 2. `ImporterVersion` becomes 7 so the editor stamp misses and rewrites siblings. The reader accepts 1 and 2.

Option bits match meshoptimizer 1.0, which that package vendors: lock border `1 << 0`, sparse `1 << 1`.

## 1. Component field

On `ModelRendererComponent`, next to the other material floats. The JSON serializer and `Clone` already copy public properties; add the field to the clone initializer the type already writes by hand.

```csharp
/// <summary>Scales how soon a coarser index list is chosen. 1 matches a one-pixel error.</summary>
public float LodBias
{
    get;
    set => field = float.IsFinite(value) && value > 0f ? value : 1f;
} = 1f;
```

Inspector, inside `ModelRendererComponentEditor.DrawMaterialFields`, same drawer as Metallic:

```csharp
propertyRenderer.DrawPropertyField("LOD Bias", entity,
    e => e.GetComponent<ModelRendererComponent>().LodBias,
    (e, v) => e.GetComponent<ModelRendererComponent>().LodBias = v,
    MultiField.SameFloat);
```

## 2. Scene view

Add three fields to `SceneView`, defaulting so existing call sites keep selecting level 0 until they fill them:

```csharp
float ViewportHeight = 0f,
float FrustumHeightAtUnit = 0f,
bool Orthographic = false
```

`IGameWindow` gains the framebuffer pixel size. `SilkNetGameWindow` already reads `window.FramebufferSize`; client size is the logical size and is the wrong height on a retina display.

```csharp
Vector2 FramebufferSize { get; }
```

`CameraViews.TryFrom` takes that height and the projection it already has:

```csharp
var projection = camera.GetProjectionMatrix();
var frustumHeight = MathF.Abs(projection.M22) > float.Epsilon ? 2f / MathF.Abs(projection.M22) : 0f;
var orthographic = projection.M44 != 0f;
view = new SceneView(
    viewMatrix * projection,
    new Vector3(transform.M41, transform.M42, transform.M43),
    SkyViewProjection: skyView * projection,
    ViewportHeight: viewportHeight,
    FrustumHeightAtUnit: frustumHeight,
    Orthographic: orthographic);
```

`SceneRenderSystem` already receives its dependencies from DryIoc. Add `IGameWindow` and pass `FramebufferSize.Y` into `CameraQueries.TryGetPrimaryView`. The editor viewport sets the same three fields from `_editorCamera.GetProjectionMatrix()` and `_frameBuffer.GetSpecification()` height. No new container registration.

## 3. Levels on Mesh

CPU lists live until `Initialize`. After upload, only the GPU buffers and the errors remain.

```csharp
internal List<uint[]> LodIndices { get; } = [];
internal List<float> LodErrors { get; } = [];

private IIndexBuffer[] _lodBuffers = [];

internal int LodCount => _initialized ? _lodBuffers.Length : 1 + LodIndices.Count;

internal bool HasCoarserLevel => LodCount > 1;

internal void BindLevel(int level)
{
    Bind();
    var index = Math.Clamp(level, 0, _lodBuffers.Length - 1);
    _vertexArray.SetIndexBuffer(_lodBuffers[index]);
}

internal int IndexCountAt(int level) =>
    _initialized
        ? _lodBuffers[Math.Clamp(level, 0, _lodBuffers.Length - 1)].Count
        : Indices.Count;

internal float LodError(int level) =>
    level <= 0 || level > LodErrors.Count ? 0f : LodErrors[level - 1];
```

Level 0 is the original index buffer and is not stored in `LodErrors`. Entry `i` of `LodErrors` and `LodIndices` is level `i + 1`. Buffer `i` of `_lodBuffers` is level `i`, with buffer 0 the original list.

`Initialize` uploads `Indices` as buffer 0, then each `LodIndices` entry. `SetIndexBuffer` binds the vertex array and then the element buffer, which is the core-profile rule. A failed create drops that level and the levels after it. `GetIndexCount` stays the level-0 count so merge baking and stats that mean “source mesh” do not silently shrink.

## 4. Simplifier

`RuntimeMeshLodSimplifier.Simplify`, called from `RuntimeMeshBuilder` after a successful Assimp import, per submesh, before `RuntimeMeshWriter.TryWrite`. Positions are a `float*` at the start of each 56-byte vertex. Attribute stream is normal (3) then UV (2), weights all 1. `resultError` is relative. `meshopt_simplifyScale` is the mesh size that turns it into mesh units.

```csharp
const uint LockBorder = 1u << 0;
const uint Sparse = 1u << 1;
const float MaxRelativeError = 1f;

float scale = MeshOptNative.meshopt_simplifyScale(positions, (UIntPtr)vertexCount, (UIntPtr)56);
var current = (uint[])indices.Clone();
float relative = 0f;

while (current.Length > 12)
{
    uint target = (uint)Math.Max(12, (current.Length / 3 / 2) * 3);
    var destination = new uint[current.Length];
    float step = 0f;
    UIntPtr written;
    try
    {
        written = MeshOptNative.meshopt_simplifyWithAttributes(
            destination, current, (UIntPtr)current.Length,
            positions, (UIntPtr)vertexCount, (UIntPtr)56,
            attributes, (UIntPtr)20, weights, (UIntPtr)5,
            vertexLock: null,
            (UIntPtr)target, MaxRelativeError,
            LockBorder | Sparse, &step);
    }
    catch (Exception)
    {
        LogOnceMissingLibrary();
        return;
    }

    int count = (int)written;
    if (count == 0 || count % 3 != 0 || count > current.Length * 0.75f)
        break;

    relative = MathF.Max(relative * 1.5f, step);
    if (relative > MaxRelativeError)
        break;

    var kept = destination.AsSpan(0, count).ToArray();
    var cacheOptimized = new uint[count];
    MeshOptNative.meshopt_optimizeVertexCache(
        cacheOptimized, kept, (UIntPtr)count, (UIntPtr)vertexCount);
    kept = cacheOptimized;
    submesh.LodIndices.Add(kept);
    submesh.LodErrors.Add(MathF.Max(relative * scale, 1e-8f));
    current = kept;
}
```

`catch` covers the missing dylib on an Intel Mac and any other load failure. The submesh is still written. Indices that fall outside `vertexCount` are rejected with the same `break`. The writer appends, for version 2 only, `uint count` then for each level `float error` and `uint indexCount` plus the indices. Version 1 has no tail. A short tail on one submesh logs and leaves that submesh’s lists empty.

## 5. Selector

New static type `Engine/Scene/MeshLod.cs`. No per-frame allocation. Shadow walks pass the same camera fields they were given on `SceneView`, not the light matrix.

```csharp
internal static int Select(
    Mesh mesh, Matrix4x4 world, Vector3 camera, in SceneView view, float bias)
{
    int count = mesh.LodCount;
    if (count <= 1)
        return 0;
    if (mesh.Bounds is not Aabb box)
        return 0;
    if (view.ViewportHeight <= 0f || !float.IsFinite(view.FrustumHeightAtUnit) || view.FrustumHeightAtUnit <= 0f)
        return 0;

    float scale = MaxAxisScale(world);
    if (!float.IsFinite(scale) || scale <= 0f)
        return 0;

    float distanceSq = view.Orthographic
        ? 1f
        : Aabb.ClosestPointDistanceSquared(camera, world, box);
    if (!float.IsFinite(distanceSq))
        return 0;

    float factor = view.FrustumHeightAtUnit * (1f / bias) / view.ViewportHeight;
    float limitSq = distanceSq * factor * factor;
    int chosen = 0;
    for (int level = 1; level < count; level++)
    {
        float worldError = mesh.LodError(level) * scale;
        if (worldError * worldError <= limitSq)
            chosen = level;
        else
            break;
    }
    return chosen;
}
```

`MaxAxisScale` is the max of the absolute lengths of the three basis axes. Errors are stored in order, so the first failure ends the scan.

## 6. Walk, batch key, draw

`MeshBatchKey` gains `int Level`. In the gather, before the merge split:

```csharp
int level = MeshLod.Select(submesh, transform, view.ViewPosition, view, modelRenderer.LodBias);
if (submesh.HasCoarserLevel)
{
    QueueSubmesh(modelRenderer, transform, tint, entityId, submesh, level, ref stats);
    continue;
}
```

`QueueSubmesh` puts `level` on the key and adds `IndexCountAt(level)` to the pass index stat. The existing merge path is the `else` and still sees only meshes with `HasCoarserLevel == false`.

`DrawMeshInstances` takes the level. `UploadAndDraw` calls `mesh.BindLevel(level)` before the indexed draw. Point-shadow faces already draw instances one by one; they bind that same level once for the batch. Color and directional shadow keep instancing.

`SceneRenderPipeline` stays one type. The selector is the new file. There is no new priority: `SceneRenderSystem` remains 150.

## 7. Tests

`MeshLodTests` calls `Select` with a mesh that has errors 0, 0.01, and 0.05. No GL and no native library.

- Distance 0 returns 0.
- A limit between the two positive errors returns the coarser of those two, and a tighter limit returns the finer one.
- Bias 2 returns a finer level than bias 1 for the same distance, height, and frustum.
- `Orthographic: true` ignores camera distance and uses distance 1.
- Viewport height 0, an empty extra-level list, and a non-finite scale return 0.

`RuntimeMeshLodSimplifierTests` builds a manifold grid, runs `Simplify`, and expects a shorter in-range index list, a multiple of three, a positive error, and a following error at least 1.5× the first. A second case feeds a step that cannot cut 25% and expects no extra list. The test project references the same Evergine package.

`RuntimeMeshReader` tests: a version-1 byte buffer loads with `LodCount == 1`. A version-2 buffer with a truncated level block loads the base indices and no extra level.

A pipeline test on the existing recording `IGraphics3D` places two entities on one mesh. Different distances record two batches with different index counts. The same distance records one batch. A mesh with a coarser level is absent from the merge group. A cube draw is unchanged.
