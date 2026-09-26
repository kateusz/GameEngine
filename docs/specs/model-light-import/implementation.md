# Model Light Import — Implementation

Step-by-step code for the design in `introduction.md` and `developer-guide.md`. Paste the C# into the existing types. Do not add a component, a shader uniform, or a serializer registration.

Assimp already multiplies glTF color by intensity into `MColorDiffuse`. `PBR_LightRange` is float or double metadata on the node. Quadratic attenuation is not a range.

## 1. Payload on the scene-graph node

Create `Engine/Renderer/Models/ImportedLight.cs`.

```csharp
using System.Numerics;

namespace Engine.Renderer.Models;

public abstract record ImportedLight;

public sealed record ImportedPointLight(Vector4 Color, float Intensity, float Range) : ImportedLight;

public sealed record ImportedDirectionalLight(Vector4 Color, Vector3 Direction) : ImportedLight;
```

Add an optional parameter to `ModelSceneNode`. Existing callers keep compiling.

```csharp
public ModelSceneNode(
    string name,
    IReadOnlyList<int> meshIndices,
    IReadOnlyList<ModelSceneNode> children,
    Matrix4x4? localTransform = null,
    ImportedLight? light = null)
{
    Name = name;
    MeshIndices = meshIndices;
    Children = children;
    LocalTransform = localTransform ?? Matrix4x4.Identity;
    Light = light;
}

public ImportedLight? Light { get; }
```

`ShouldUnpack` stays `TotalMeshCount > 1`.

**Why:** The spawner copies this payload onto a component. The node never stores candela.

## 2. Conversion

Create `Engine/Renderer/Models/ModelLightConversion.cs`. `Engine.Tests` already sees internals.

```csharp
using System.Numerics;

namespace Engine.Renderer.Models;

internal static class ModelLightConversion
{
    private const float DirectionEpsilonSquared = 1e-8f;

    public static bool TryPoint(
        Vector3 diffuse,
        float? fileRange,
        out Vector4 color,
        out float intensity,
        out float range)
    {
        color = default;
        intensity = 0f;
        range = 0f;

        var brightness = Brightness(diffuse);
        if (brightness is not float b)
            return false;

        color = new Vector4(diffuse / b, 1f);
        intensity = Math.Clamp(MathF.Sqrt(b / 4f), 0.5f, 2f);
        range = fileRange is float file && float.IsFinite(file) && file > 0f
            ? file
            : Math.Clamp(3f * MathF.Sqrt(b), 1f, 16f);
        return true;
    }

    public static bool TryDirectional(Vector3 diffuse, Vector3 direction, out Vector4 color, out Vector3 baked)
    {
        color = default;
        baked = default;

        var brightness = Brightness(diffuse);
        if (brightness is not float b)
            return false;

        color = b > 1f ? new Vector4(diffuse / b, 1f) : new Vector4(diffuse, 1f);
        baked = BakeDirection(direction);
        return true;
    }

    public static Vector3 BakeDirection(Vector3 direction) =>
        direction.LengthSquared() < DirectionEpsilonSquared
            ? new Vector3(0f, -1f, 0f)
            : Vector3.Normalize(direction);

    private static float? Brightness(Vector3 diffuse)
    {
        var b = MathF.Max(diffuse.X, MathF.Max(diffuse.Y, diffuse.Z));
        return float.IsFinite(b) && b > 0f ? b : null;
    }
}
```

**Why:** B = 4 lands on intensity 1 and range 6. The caps are the whole brightness policy.

## 3. Read Assimp lights

In `AssimpModelImporter`, build the graph only after lights are collected, and only while `scene` is alive. Add this inside the existing `try`, after the mesh loop and instead of the bare `WalkNode` call.

```csharp
var pending = CollectLights(scene);
var skipped = pending.Skipped;
if (skipped > 0)
    Logger.Debug("Skipped unsupported lights count={Count} path={Path}", skipped, path);

sceneGraph = WalkNode(scene->MRootNode, meshIndexMap, pending, Matrix4x4.Identity);
sceneGraph = WithUnmatchedLights(sceneGraph, pending);
```

`CollectLights` keeps point and directional lights in name order. Anything else increments `Skipped`.

```csharp
private readonly record struct RawLight(
    string Name,
    LightSourceType Type,
    Vector3 Diffuse,
    Vector3 Position,
    Vector3 Direction);

private sealed class PendingLights
{
    private readonly Dictionary<string, Queue<RawLight>> _byName = new(StringComparer.Ordinal);
    public int Skipped { get; private set; }

    public void Add(string name, RawLight light)
    {
        if (!_byName.TryGetValue(name, out var queue))
        {
            queue = new Queue<RawLight>();
            _byName[name] = queue;
        }

        queue.Enqueue(light);
    }

    public void Skip() => Skipped++;

    public bool TryTake(string name, out RawLight light)
    {
        light = default;
        if (!_byName.TryGetValue(name, out var queue) || queue.Count == 0)
            return false;

        light = queue.Dequeue();
        return true;
    }

    public List<RawLight> TakeRemaining()
    {
        var left = new List<RawLight>();
        foreach (var queue in _byName.Values)
        {
            while (queue.Count > 0)
                left.Add(queue.Dequeue());
        }

        return left;
    }
}
```

```csharp
private unsafe PendingLights CollectLights(Silk.NET.Assimp.Scene* scene)
{
    var pending = new PendingLights();
    for (uint i = 0; i < scene->MNumLights; i++)
    {
        var light = scene->MLights[i];
        if (light->MType == LightSourceType.LightSourcePoint ||
            light->MType == LightSourceType.LightSourceDirectional)
        {
            pending.Add(light->MName.AsString, new RawLight(
                light->MName.AsString,
                light->MType,
                light->MColorDiffuse,
                light->MPosition,
                light->MDirection));
            continue;
        }

        pending.Skip();
    }

    return pending;
}
```

`WalkNode` takes the pending set and the parent matrix. Accumulate with `local * parentWorld`, the same order as `TryGetMeshWorldTransform`.

```csharp
private static unsafe ModelSceneNode WalkNode(
    AssimpNode* node,
    IReadOnlyDictionary<uint, int> meshIndexMap,
    PendingLights pending,
    Matrix4x4 parentWorld)
{
    var meshIndices = new List<int>();
    for (uint i = 0; i < node->MNumMeshes; i++)
    {
        var assimpMeshIndex = node->MMeshes[i];
        if (meshIndexMap.TryGetValue(assimpMeshIndex, out var compactIndex))
            meshIndices.Add(compactIndex);
    }

    var name = string.IsNullOrWhiteSpace(node->MName.AsString) ? "Node" : node->MName.AsString;
    var nodeLocal = ToEngineMatrix(node->MTransformation);
    var world = nodeLocal * parentWorld;
    var local = nodeLocal;
    ImportedLight? imported = null;
    if (pending.TryTake(name, out var raw))
        imported = ConvertLight(node, raw, world, meshIndices.Count > 0, ref local);

    var children = new List<ModelSceneNode>((int)node->MNumChildren);
    for (uint i = 0; i < node->MNumChildren; i++)
        children.Add(WalkNode(node->MChildren[i], meshIndexMap, pending, world));

    return new ModelSceneNode(name, meshIndices, children, local, imported);
}
```

`world` stays on `nodeLocal`. `ConvertLight` may set `local` to `CreateTranslation(position) * nodeLocal` for a meshless point light. Children must not inherit that offset.

`ConvertLight` returns null when conversion rejects the color. A rejected light is consumed and logged at debug with the node name. It does not go back to the pending set.

```csharp
private static unsafe ImportedLight? ConvertLight(
    AssimpNode* node,
    RawLight raw,
    Matrix4x4 world,
    bool nodeHasMesh,
    ref Matrix4x4 local)
{
    if (raw.Type == LightSourceType.LightSourcePoint)
    {
        if (!ModelLightConversion.TryPoint(raw.Diffuse, ReadRange(node), out var color, out var intensity, out var range))
        {
            Logger.Debug("Skipped point light with no brightness name={Name}", node->MName.AsString);
            return null;
        }

        if (!nodeHasMesh)
            local = Matrix4x4.CreateTranslation(raw.Position) * local;
        else if (raw.Position.LengthSquared() > 1e-8f)
            Logger.Debug("Ignoring light position offset on a mesh node name={Name}", node->MName.AsString);

        return new ImportedPointLight(color, intensity, range);
    }

    var direction = Vector3.TransformNormal(raw.Direction, world);
    if (!ModelLightConversion.TryDirectional(raw.Diffuse, direction, out var sunColor, out var baked))
    {
        Logger.Debug("Skipped directional light with no brightness name={Name}", node->MName.AsString);
        return null;
    }

    return new ImportedDirectionalLight(sunColor, baked);
}
```

`TryDirectional` normalizes direction once. Pass the transformed vector. Do not call `BakeDirection` before it.

Range metadata:

```csharp
private static unsafe float? ReadRange(AssimpNode* node)
{
    var meta = node->MMetaData;
    if (meta == null)
        return null;

    for (uint i = 0; i < meta->MNumProperties; i++)
    {
        if (meta->MKeys[i].AsString != "PBR_LightRange")
            continue;

        var entry = meta->MValues[i];
        if (entry.MData == null)
            return null;

        if (entry.MType == MetadataType.Float)
            return *(float*)entry.MData;
        if (entry.MType == MetadataType.Double)
            return (float)*(double*)entry.MData;
    }

    return null;
}
```

`MetadataType.Float` and `MetadataType.Double` are the Silk.NET.Assimp members. If a build names them differently, use the float and double members of that enum. Do not treat any other metadata type as a range.

Unmatched lights become children of the graph root. One debug line covers the whole leftover list.

```csharp
private static ModelSceneNode WithUnmatchedLights(ModelSceneNode root, PendingLights pending)
{
    var left = pending.TakeRemaining();
    if (left.Count == 0)
        return root;

    Logger.Debug("Lights without a matching node count={Count}", left.Count);
    var children = new List<ModelSceneNode>(root.Children.Count + left.Count);
    children.AddRange(root.Children);
    foreach (var raw in left)
    {
        var local = Matrix4x4.CreateTranslation(raw.Position);
        ImportedLight? imported = raw.Type == LightSourceType.LightSourcePoint
            ? PointOrNull(raw)
            : DirectionalOrNull(raw, raw.Direction);
        if (imported == null)
            continue;

        var name = string.IsNullOrWhiteSpace(raw.Name) ? "Light" : raw.Name;
        children.Add(new ModelSceneNode(name, [], [], local, imported));
    }

    return new ModelSceneNode(root.Name, root.MeshIndices, children, root.LocalTransform, root.Light);
}
```

Unmatched children use `raw.Name`. An empty name falls back to `"Light"`. Do not reuse the graph root's name.

`PointOrNull` / `DirectionalOrNull` call the same conversion with a null file range and with no node matrix.

**Why:** `ReleaseImport` in the existing `finally` frees the lights and the metadata. Matching by name is the Assimp link between `mLights` and the graph.

## 4. Unpacked spawn

In `ModelHierarchySpawner.SpawnNode`, replace the empty-leaf return.

```csharp
if (node.MeshIndices.Count == 0 && node.Children.Count == 0 && node.Light is null)
    return;
```

After the entity for that node exists (`leaf` or `host`), add the light if this node is not the graph root. `SpawnNode` only visits children, so every node it sees is already parented under the scene root or a descendant. Add the component on that entity.

```csharp
private static void AddLight(Entity entity, ImportedLight? light)
{
    switch (light)
    {
        case ImportedPointLight point:
            entity.AddComponent(new PointLightComponent
            {
                Color = point.Color,
                Intensity = point.Intensity,
                Range = point.Range
            });
            break;
        case ImportedDirectionalLight sun:
            entity.AddComponent(new DirectionalLightComponent
            {
                Color = sun.Color,
                Direction = sun.Direction
            });
            break;
    }
}
```

At the end of `SpawnChildren`, a light on the graph root becomes a new child. Do not call `AddLight` on `root`.

```csharp
if (graphRoot.Light is not null)
{
    var lamp = CreateEntity(scene, root, graphRoot.Name, Matrix4x4.Identity);
    AddLight(lamp, graphRoot.Light);
}
```

**Why:** Child entities already carry the node local transform, including a position baked for a meshless point light. The scene root must stay free of the component so hierarchy undo removes the lamp.

## 5. Packed spawn

`ImportModelHierarchyCommand.Execute`, in the branch that returns early because `ShouldUnpack` is false, keep the mesh bake and then spawn lamps. Do not set `SuppressDraw` from the presence of lights. That flag is already set from `ShouldUnpack` above this branch.

```csharp
if (!graph.ShouldUnpack)
{
    if (graph.FirstMeshIndex is int meshIndex &&
        graph.TryGetMeshWorldTransform(meshIndex, out var meshWorld) &&
        root.TryGetComponent<TransformComponent>(out var transform))
    {
        var combined = meshWorld * transform.GetTransform();
        ModelHierarchySpawner.ApplyLocalTransform(root, combined);
    }

    ModelHierarchySpawner.SpawnPackedLights(scene, root, graph);
    return true;
}
```

`SpawnPackedLights` walks the whole graph. There is no parent chain in the scene, so each lamp's local matrix is relative to the mesh world that was just baked into the root.

```csharp
public static void SpawnPackedLights(IScene scene, Entity root, ModelSceneNode graphRoot)
{
    var meshWorld = Matrix4x4.Identity;
    if (graphRoot.FirstMeshIndex is int meshIndex &&
        graphRoot.TryGetMeshWorldTransform(meshIndex, out var found))
        meshWorld = found;

    if (!Matrix4x4.Invert(meshWorld, out var inverseMesh))
    {
        Log.Debug("Skipped packed lights because the mesh transform could not be inverted");
        return;
    }

    SpawnPackedWalk(scene, root, graphRoot, Matrix4x4.Identity, inverseMesh);
}

private static void SpawnPackedWalk(
    IScene scene,
    Entity root,
    ModelSceneNode node,
    Matrix4x4 parentWorld,
    Matrix4x4 inverseMesh)
{
    var world = node.LocalTransform * parentWorld;
    if (node.Light is not null)
    {
        var lamp = CreateEntity(scene, root, node.Name, world * inverseMesh);
        AddPackedLight(lamp, node.Light, inverseMesh);
    }

    foreach (var child in node.Children)
        SpawnPackedWalk(scene, root, child, world, inverseMesh);
}
```

`AddPackedLight` writes a point light as stored. A directional direction is rotated into the root entity's axes after the mesh bake, then normalized. `ModelLightConversion.BakeDirection` does that normalize and the zero fallback.

```csharp
private static void AddPackedLight(Entity entity, ImportedLight light, Matrix4x4 inverseMesh)
{
    if (light is ImportedDirectionalLight sun)
    {
        var direction = ModelLightConversion.BakeDirection(
            Vector3.TransformNormal(sun.Direction, inverseMesh));
        AddLight(entity, sun with { Direction = direction });
        return;
    }

    AddLight(entity, light);
}
```

Use the Serilog logger the editor project already uses if `Log.Debug` is not in scope. One line is enough. Do not fail the mesh import.

**Why:** `world * inverseMesh` cancels the translation and rotation the command just wrote onto the root. A pure translation mesh world leaves directional direction unchanged because its inverse has no rotation.

## 6. Tests

Add `tests/Engine.Tests/Renderer/ModelLightConversionTests.cs`.

```csharp
[Fact]
public void TryPoint_TorchBrightness_IsUnitIntensityAndSixMeters()
{
    var diffuse = new Vector3(4f, 2f, 1f);
    ModelLightConversion.TryPoint(diffuse, null, out var color, out var intensity, out var range).ShouldBeTrue();
    intensity.ShouldBe(1f);
    range.ShouldBe(6f);
    color.ShouldBe(new Vector4(1f, 0.5f, 0.25f, 1f));
}

[Fact]
public void TryPoint_FireBrightness_HitsBothCaps()
{
    ModelLightConversion.TryPoint(new Vector3(100f, 40f, 10f), null, out _, out var intensity, out var range)
        .ShouldBeTrue();
    intensity.ShouldBe(2f);
    range.ShouldBe(16f);
}

[Fact]
public void TryPoint_FileRange_ReplacesDerivedRange()
{
    ModelLightConversion.TryPoint(new Vector3(4f, 4f, 4f), 40f, out _, out var intensity, out var range)
        .ShouldBeTrue();
    intensity.ShouldBe(1f);
    range.ShouldBe(40f);
}

[Fact]
public void TryPoint_ZeroBrightness_ReturnsFalse()
{
    ModelLightConversion.TryPoint(Vector3.Zero, 10f, out _, out _, out _).ShouldBeFalse();
}

[Fact]
public void TryDirectional_LuxAboveOne_ScalesBrightestChannelToOne()
{
    ModelLightConversion.TryDirectional(new Vector3(2f, 1f, 0f), new Vector3(0f, 0f, -1f), out var color, out var direction)
        .ShouldBeTrue();
    color.ShouldBe(new Vector4(1f, 0.5f, 0f, 1f));
    direction.ShouldBe(new Vector3(0f, 0f, -1f));
}

[Fact]
public void TryDirectional_ZeroDirection_UsesDefault()
{
    ModelLightConversion.TryDirectional(Vector3.One, Vector3.Zero, out _, out var direction).ShouldBeTrue();
    direction.ShouldBe(new Vector3(0f, -1f, 0f));
}
```

Also assert B = 0.5: intensity 0.5 and range `3 * MathF.Sqrt(0.5f)`. Assert file range 0 uses the derived range. Assert diffuse `(0.2, 0.1, 0)` stays unscaled.

Extend `ModelSceneNodeTests`:

```csharp
[Fact]
public void ShouldUnpack_SingleMeshWithLight_StaysFalse()
{
    var root = new ModelSceneNode("Root", [0], [], Matrix4x4.Identity,
        new ImportedPointLight(Vector4.One, 1f, 6f));
    root.ShouldUnpack.ShouldBeFalse();
    root.Light.ShouldBeOfType<ImportedPointLight>();
}
```

Extend `ModelHierarchySpawnerTests`. Build scenes the way `CreateScene` already does.

- Graph `Room` with no meshes, child `Torch` with no meshes and an `ImportedPointLight`. `SpawnChildren` creates `Torch` with a transform and a `PointLightComponent`, and without a `ModelRendererComponent`.
- Child with one mesh index and a point payload has both components. Intensity and range match the payload.
- Graph root with one mesh and a point payload: `SpawnChildren` puts a child on the root, and the root entity has no `PointLightComponent`. For a packed graph, call `SpawnPackedLights` instead. The root's `ModelRendererComponent.SuppressDraw` stays false. Assert that in the command test below, where suppress-draw is actually set.
- Packed graph: root local translation `(1, 0, 0)`, mesh index `[0]` on the root, child `Torch` local translation `(4, 0, 0)` and a point payload. After `SpawnPackedLights`, the torch child's translation is `(4, 0, 0)`. The mesh world is the root translation, and `(child * root) * inverse(root)` is the child translation.

Undo: build a `Model` whose graph unpacks (two mesh nodes) and whose child has a point payload. Execute `ImportModelHierarchyCommand`, assert the child exists, `Undo`, assert the root has no children. Follow `SpawnModelEntityCommandTests` for how a model and a scene are built.

Do not load a `.glb` and do not draw a frame.

## Pitfalls

**Baking the position offset into the matrix passed to children.** Children of a meshless light node must keep the node's own matrix. Only the light entity's local matrix gains `CreateTranslation(position)`.

**Second `BakeDirection` on a vector that was already transformed.** Normalize once. `BakeDirection` on a unit vector is safe. `BakeDirection` on a zero vector replaces it, so do not skip the call on the packed inverse.

**`MetadataEntry.MData` layout.** It is the `mData` pointer on `aiMetadataEntry`. Read a float or a double only. A missing key means derived range, not a failed import.

**Unmatched-light names.** Leftover children need the light's name. Reusing the root name makes every orphan look like the model.

## Done When

- [ ] Conversion tests cover the torch, the fire caps, a file range, zero brightness, lux scaling, and the default direction
- [ ] A single mesh with a light still does not unpack
- [ ] Unpacked spawn creates a light entity for an empty node and keeps a mesh node as one entity
- [ ] A payload on the graph root is a child
- [ ] Packed spawn parents lamps relative to the baked mesh and leaves the root drawing
- [ ] Hierarchy undo removes the lamps
- [ ] No change under `SceneRenderPipeline`, the shaders, or the light component editors
