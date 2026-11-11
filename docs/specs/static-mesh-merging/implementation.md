# Static Mesh Merging — Implementation

Step-by-step C# for the design in `introduction.md` and `developer-guide.md`. Paste into existing types. No new component, serializer, DryIoc registration, or `ISystem`. Runtime and editor both enter through `SceneRenderPipeline.DrawOpaque3D`.

Vertex-buffer cap remains `RenderingConstants.MaxVertexBufferBytes` (256 MB). Combined meshes use `Mesh.Vertex` as-is (entity id at location 5). Draw with `Matrix4x4.Identity`.

## 1. Keep CPU geometry after upload

`Engine/Renderer/Meshes/Mesh.cs` — after bounds are captured, copy into a snapshot instead of only clearing.

```csharp
public readonly record struct BakeVertex(
    Vector3 Position, Vector3 Normal, Vector2 TexCoord, Vector3 Tangent, Vector3 Bitangent);

internal BakeVertex[]? BakeVertices { get; private set; }
internal uint[]? BakeIndices { get; private set; }

// at end of Initialize, before or instead of discarding lists:
BakeVertices = Vertices.Select(v => new BakeVertex(
    v.Position, v.Normal, v.TexCoord, v.Tangent, v.Bitangent)).ToArray();
BakeIndices = Indices.ToArray();
Vertices.Clear();
Indices.Clear();
```

Prefer arrays allocated once over LINQ in the real patch. Unit cubes have no snapshot; the baker uses `Aabb.UnitCube` corners and the cube index pattern already used by the cube mesh builder.

## 2. Keys, history, and cache beside the pipeline

`Engine/Scene/SceneRenderPipeline.cs` (or a helper type in the same folder constructed as a static field — no DryIoc unless this file is split later).

```csharp
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
    bool AlphaCutout,
    bool DoubleSided);

private struct MergeMember
{
    public int EntityId;
    public Mesh? Source; // null = unit cube
    public Matrix4x4 World;
}

private sealed class CombinedMeshEntry
{
    public Mesh Mesh = null!;
    public int[] EntityIds = [];
    public Matrix4x4[] Worlds = [];
    public Mesh?[] Sources = [];
    public int Rebuilds;
}

private static readonly Dictionary<int, Matrix4x4> WorldHistory = new();
private static readonly Dictionary<MergeMaterialKey, List<MergeMember>> MergeGroups = new();
private static readonly Dictionary<MergeMaterialKey, CombinedMeshEntry> CombinedMeshes = new();
```

Clear and dispose `CombinedMeshes` when the scene context changes (same place other pipeline statics reset). `WorldHistory.Clear()` there too.

## 3. Classify inside the opaque walk

After `ShouldSkipForEntityVisibility` / zones / resolve surface, before `QueueSubmesh` / cube draw:

```csharp
var world = transform; // already pivot-adjusted
var hasBody = entity.TryGetComponent<RigidBody2DComponent>(out var body)
              && body.BodyType is RigidBodyType.Dynamic or RigidBodyType.Kinematic;
var finite = MatrixIsFinite(world);
var seen = WorldHistory.TryGetValue(entity.Id, out var previous);
var idle = finite && seen && world == previous;

WorldHistory[entity.Id] = world;

if (!idle || hasBody || (kind != OpaqueSurfaceKind.UnitCube && mesh?.BakeVertices is null))
{
    QueueSubmesh(/* existing */); // cubes keep existing cube path when not merged
    continue;
}

var key = MakeMergeKey(modelRenderer, mesh, tint, pbr, emissive);
if (!MergeGroups.TryGetValue(key, out var members))
{
    members = [];
    MergeGroups[key] = members;
}
members.Add(new MergeMember { EntityId = entity.Id, Source = mesh, World = world });
```

Cubes that are idle and share a key append with `Source = null`. They must not call `DrawCube` in that pass.

`==` on `Matrix4x4` is exact. That matches “did not move,” including gizmo drags.

## 4. Bake and draw after the walk, before or after instance flush

Order: flush instance batches first (movers), then merge groups. Both write to the same framebuffer.

```csharp
private static void FlushMergeGroups(
    IGraphics3D graphics3D,
    IVertexArrayFactory vaos,
    IVertexBufferFactory vbos,
    IIndexBufferFactory ibos,
    bool hasFrustum,
    in Frustum frustum,
    float shadowCasterMaxDistanceSq,
    Vector3 shadowCasterViewPosition,
    Vector3? skipCastersContainingLight,
    ref PassStats stats)
{
    foreach (var (key, members) in MergeGroups)
    {
        if (members.Count < 2)
        {
            foreach (var m in members)
                ReplayAsInstance(graphics3D, key, m, ref stats);
            continue;
        }

        if (!TryGetCombined(key, members, vaos, vbos, ibos, out var entry, ref stats))
        {
            foreach (var m in members)
                ReplayAsInstance(graphics3D, key, m, ref stats);
            continue;
        }

        var bounds = entry.Mesh.Bounds ?? default;
        if (ShouldSkipOpaqueDraw(
                hasFrustum, frustum, Matrix4x4.Identity, bounds,
                shadowCasterMaxDistanceSq, shadowCasterViewPosition,
                skipCastersContainingLight, ref stats))
            continue;

        graphics3D.DrawMesh(entry.Mesh, Matrix4x4.Identity, Vector4.One, entityId: -1);
        stats.MeshDraws++;
        stats.MergedGroups++;
        stats.MergedVertices += entry.Mesh.VertexCount;
    }

    foreach (var list in MergeGroups.Values)
        list.Clear();
    MergeGroups.Clear();
}

private static bool SignaturesMatch(CombinedMeshEntry entry, List<MergeMember> members)
{
    if (entry.EntityIds.Length != members.Count)
        return false;
    for (var i = 0; i < members.Count; i++)
    {
        if (entry.EntityIds[i] != members[i].EntityId
            || entry.Worlds[i] != members[i].World
            || entry.Sources[i] != members[i].Source)
            return false;
    }
    return true;
}
```

`TryGetCombined` rebuilds when `SignaturesMatch` is false or the cache misses. Bake loop:

```csharp
static bool TryBake(List<MergeMember> members, out Mesh combined)
{
    combined = new Mesh("merged");
    var verts = new List<Mesh.Vertex>();
    var inds = new List<uint>();
    var byteCount = 0;
    const int vertexStride = 15 * 4; // Mesh.Vertex layout; keep in sync with BufferLayout

    foreach (var member in members)
    {
        if (!Matrix4x4.Invert(member.World, out var inv))
            inv = Matrix4x4.Identity;
        var normalMatrix = Matrix4x4.Transpose(inv);

        AppendMember(member, member.World, normalMatrix, verts, inds);
        byteCount = verts.Count * vertexStride;
        if (byteCount > RenderingConstants.MaxVertexBufferBytes)
            return false; // caller starts a new Mesh with remaining members
    }

    combined.Vertices = verts;
    combined.Indices = inds;
    return true;
}

static Vertex TransformVertex(in MergeMember member, in BakeVertex v, in Matrix4x4 world, in Matrix4x4 normalMatrix)
{
    var p = Vector3.Transform(v.Position, world);
    var n = Vector3.Normalize(Vector3.TransformNormal(v.Normal, normalMatrix));
    var t = Vector3.Normalize(Vector3.TransformNormal(v.Tangent, normalMatrix));
    var b = Vector3.Normalize(Vector3.TransformNormal(v.Bitangent, normalMatrix));
    return new Mesh.Vertex(p, n, v.TexCoord, t, b, member.EntityId);
}
```

On cap overflow, `Initialize` the current mesh, cache it as slice 0, and continue with a new `Mesh` for the rest (same material key, extra draws). `Initialize` throw: log, dispose partial, return false, replay members.

`DrawMesh` with `entityId: -1` is correct: picking reads per-vertex `a_EntityID`, not the uniform.

Point-shadow faces call the same flush; they already draw non-instanced meshes.

## 5. Stats

Extend `PassStats` with `MergedGroups`, `MergedVertices`, `MergedRebuilds`. Fold them in `Accumulate` and the existing 1 Hz log line next to instance batch counts.

## 6. Tests

`tests/Engine.Tests/Scene/StaticMeshMergingTests.cs` against `MeshDrawRecordingGraphics3D` (or the shadow-test fake):

1. Two idle submeshes, same key, frame 2 → one `DrawMesh`, no instance batch of those two.
2. Frame 1 → instance/cube path only.
3. Frame 3 one world changes → both back on instance path (group size 1 or split).
4. Dynamic `RigidBody2D` never merges; `RigidBodyType.Static` may.
5. One member outside camera frustum, both idle → combined mesh still built; color skip depends on group box vs camera.
6. Different tint → two groups / no combine.
7. Direct bake assert: transformed position and `EntityId`.
8. Failed `Initialize` → sources drawn that frame.

No graphics-integration test required beyond the fake 3D recorder.

## 7. Pipeline, editor, Runtime

| Hook | Change |
|------|--------|
| `DrawOpaque3D` | Classify, flush merge groups |
| Shadow faces | Same classify; shared cache |
| Editor stats | Existing 3D stats window / log |
| Runtime | No extra path |
| Serialization | None |
| DryIoc | None |

Factories needed for `Mesh.Initialize` on rebuild: resolve `IVertexArrayFactory`, `IVertexBufferFactory`, `IIndexBufferFactory` the same way `ModelFactory` already does, or pass them into `DrawOpaque3D` from the existing `Context` / renderer scope. Do not construct GL objects in core except through those factories.
