# 3D Import Lights & Cameras Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** On **File → Import 3D Model…**, spawn Assimp directional lights and cameras as children of the import parent, mapped to existing `DirectionalLightComponent` / `CameraComponent` (in-memory DTOs only; no `.mesh` changes).

**Architecture:** Same Assimp cook pass extracts extras into public DTOs; `MeshCreator` surfaces them on split/skinned results; `Import3DModelBatch.SpawnHierarchy` creates child entities. Runtime lighting/camera selection rules unchanged (first directional, primary camera).

**Tech Stack:** C# / .NET, Silk.NET.Assimp, existing ECS components, xUnit + Shouldly.

**Spec:** [`docs/specs/3d-import-lights-cameras/design.md`](./design.md)

## Global Constraints

- Only `LightSourceDirectional` → `DirectionalLightComponent`; point/spot/ambient → log + skip (count skipped).
- No new light types, no multi-light shading, no `.mesh` VERSION change, no sidecar files.
- Cameras: Assimp index order; first successfully spawned camera gets `Primary=true` only if active scene has no primary; never clear existing primary.
- Hierarchy: lights/cameras are children of the import parent (same as mesh parts).
- Assimp stays cook-only; extras are spawn-time DTOs, not persisted assets.
- `DirectionalLightComponent.Direction` stays component-stored (not driven by Transform); accepted v1 limit.
- No new Import popup checkbox (always on when spawning into active scene).

## File structure

| File | Responsibility |
|------|----------------|
| `Engine/Renderer/ImportedSceneExtras.cs` | Public DTOs + cook-result bag (`Lights`, `Cameras`, `SkippedNonDirectionalLights`) |
| `Engine/Renderer/AssimpSceneExtrasExtractor.cs` | Unsafe Assimp `mLights` / `mCameras` → DTOs (node lookup, transforms, FOV convert) |
| `Engine/Renderer/AssimpModelImporter.cs` | Call extractor inside `ImportParts` / `ImportSkinned` before `ReleaseImport` |
| `Engine/Renderer/Skeletal/AssimpSkinnedImport.cs` | Carry extras on skinned cook result |
| `Engine/Renderer/MeshCreator.cs` | Extend `SplitResult` / `SkinnedResult`; allow extras-only success (no mesh nodes) |
| `Editor/Features/Import/Import3DModelBatch.cs` | `SourceImport` + `SpawnHierarchy` spawn lights/cameras; summary note |
| `tests/Engine.Tests/Fixtures/LightsCamerasGltfFixture.cs` | Minimal glTF with mesh + directional + camera (+ optional point) |
| `tests/Engine.Tests/Renderer/AssimpSceneExtrasExtractorTests.cs` | Extract unit tests |
| `tests/Engine.Tests/Renderer/MeshCreatorExtrasTests.cs` | CreateSplit surfaces extras / extras-only |
| `tests/Editor.Tests/Import/Import3DModelSceneSpawnTests.cs` | Spawn + Primary policy tests |
| `docs/specs/3d-model-loading/introduction.md` | Remove “cameras, lights ignored” from v1 non-goals; point to this feature |

---

### Task 1: DTOs + Assimp extras extractor (TDD)

**Files:**
- Create: `Engine/Renderer/ImportedSceneExtras.cs`
- Create: `Engine/Renderer/AssimpSceneExtrasExtractor.cs`
- Create: `tests/Engine.Tests/Fixtures/LightsCamerasGltfFixture.cs`
- Create: `tests/Engine.Tests/Renderer/AssimpSceneExtrasExtractorTests.cs`
- Test: run `dotnet test tests/Engine.Tests --filter AssimpSceneExtrasExtractorTests`

**Interfaces:**
- Consumes: Assimp `Scene*`, existing `ImportSpawnTransform.FromLocalToRoot`, `AssimpPartNaming.UniqueSanitize`, node world matrix pattern from `AssimpModelImporter` (column accumulate → `Matrix4x4.Transpose` for Numerics).
- Produces:
  - `public readonly record struct ImportedDirectionalLight(string Name, Vector3 Translation, Vector3 Rotation, Vector3 Scale, Vector3 Direction, Vector3 Color)`
  - `public readonly record struct ImportedCamera(string Name, Vector3 Translation, Vector3 Rotation, Vector3 Scale, float PerspectiveFOV, float PerspectiveNear, float PerspectiveFar, float AspectRatio)`
  - `public readonly record struct ImportedSceneExtras(IReadOnlyList<ImportedDirectionalLight> Lights, IReadOnlyList<ImportedCamera> Cameras, int SkippedNonDirectionalLights)`
  - `internal static class AssimpSceneExtrasExtractor` with `public static unsafe ImportedSceneExtras Extract(Scene* scene, float unitDownscaleFactor)`

**Assimp field mapping (Silk.NET):**
- Lights: `MName`, `MType` (`LightSourceDirectional` only), `MDirection`, `MColorDiffuse` (× intensity if present on struct; else color as-is), resolve node by light name via `CollectNodesByName`-style walk.
- Cameras: `MName`, `MHorizontalFOV`, `MClipPlaneNear`, `MClipPlaneFar`, `MAspect` (if ≤ 0 use `16/9`).
- Vertical FOV: `perspectiveFov = 2 * atan(tan(horizontalFov * 0.5f) / aspect)`.
- Direction into root space: take Assimp light direction as Numerics vector, transform by node’s Numerics local-to-root rotation (from transposed world matrix); if length ~0 use `(0,-1,0)`.
- Transform TRS: `ImportSpawnTransform.FromLocalToRoot(transposedWorld, unitDownscaleFactor)` — for extractor unit tests pass `1f` unless fixture needs otherwise.
- Skip non-directional: increment `SkippedNonDirectionalLights`, Serilog Information.
- Missing node for light/camera name: skip + log; do not throw.

- [ ] **Step 1: Write failing extract tests + glTF fixture**

`LightsCamerasGltfFixture.WriteMeshDirectionalCamera(dir, stem)` writes a tiny glTF 2.0 with:
1. One triangle mesh node (copy pattern from `ModelLoadingTests.EnsureGltfTranslatedTriangle`).
2. One `KHR_lights_punctual` directional light + node referencing it (translation optional).
3. One perspective camera node (`yfov`, `znear`, `zfar`, `aspectRatio`).

Optional second helper `WriteMeshPointLight(...)` for skip test (point light only, no directional).

Test skeleton:

```csharp
public class AssimpSceneExtrasExtractorTests
{
    [Fact]
    public void Extract_GltfDirectionalAndCamera_YieldsOneEach()
    {
        var dir = Path.Combine(Path.GetTempPath(), "ge-extras-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var path = LightsCamerasGltfFixture.WriteMeshDirectionalCamera(dir, "lit");
            using var assimp = Assimp.GetApi();
            unsafe
            {
                var scene = AssimpSceneImport.Import(assimp, path, (uint)(
                    PostProcessSteps.Triangulate | PostProcessSteps.GenerateNormals));
                scene.ShouldNotBeNull();
                try
                {
                    var extras = AssimpSceneExtrasExtractor.Extract(scene, unitDownscaleFactor: 1f);
                    extras.Lights.Count.ShouldBe(1);
                    extras.Cameras.Count.ShouldBe(1);
                    extras.SkippedNonDirectionalLights.ShouldBe(0);
                    extras.Lights[0].Color.Length().ShouldBeGreaterThan(0f);
                    extras.Cameras[0].PerspectiveFOV.ShouldBeGreaterThan(0f);
                    extras.Cameras[0].PerspectiveNear.ShouldBeGreaterThan(0f);
                }
                finally { assimp.ReleaseImport(scene); }
            }
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void Extract_PointLightOnly_IncrementsSkippedAndNoDirectionals()
    {
        // WriteMeshPointLight fixture; assert Lights empty, SkippedNonDirectionalLights >= 1
    }
}
```

- [ ] **Step 2: Run tests — expect fail (types missing)**

```bash
dotnet test tests/Engine.Tests --filter AssimpSceneExtrasExtractorTests
```

Expected: compile errors or missing type.

- [ ] **Step 3: Implement DTOs + extractor**

Create `ImportedSceneExtras.cs` and `AssimpSceneExtrasExtractor.cs` as specified. Keep extractor `internal`; DTOs `public` in `Engine.Renderer`.

Reuse node-name collection logic (copy private helper into extractor or share a tiny `AssimpNodeLookup` internal helper — prefer copy-paste-minimal private methods on the extractor to avoid a drive-by refactor of `AssimpModelImporter`).

- [ ] **Step 4: Run tests — expect pass**

```bash
dotnet test tests/Engine.Tests --filter AssimpSceneExtrasExtractorTests
```

If Assimp does not expose the glTF light (extension quirks), adjust fixture until Assimp reports `MNumLights >= 1` (log `MNumLights`/`MNumCameras` in a temporary assert message while fixing). Do not widen scope to FBX-only.

- [ ] **Step 5: Commit**

```bash
git add Engine/Renderer/ImportedSceneExtras.cs \
  Engine/Renderer/AssimpSceneExtrasExtractor.cs \
  tests/Engine.Tests/Fixtures/LightsCamerasGltfFixture.cs \
  tests/Engine.Tests/Renderer/AssimpSceneExtrasExtractorTests.cs
git commit -m "$(cat <<'EOF'
Add Assimp extract for import lights and cameras.

EOF
)"
```

---

### Task 2: Wire cook path (`ImportParts` / skinned / `MeshCreator`)

**Files:**
- Modify: `Engine/Renderer/AssimpModelImporter.cs`
- Modify: `Engine/Renderer/Skeletal/AssimpSkinnedImport.cs`
- Modify: `Engine/Renderer/MeshCreator.cs` (`SplitResult`, `SkinnedResult`, `CreateSplit`, `CreateSkinned`)
- Modify: `tests/Engine.Tests/Renderer/ModelLoadingTests.cs` (call-site for new `ImportParts` return)
- Create: `tests/Engine.Tests/Renderer/MeshCreatorExtrasTests.cs`
- Update any other compile breaks from `SplitResult` shape change (`MeshCreatorTests`, `SkinnedCookTests`)

**Interfaces:**
- Consumes: `AssimpSceneExtrasExtractor.Extract`
- Produces:
  - `ImportParts` returns `AssimpPartsImport` (internal):

```csharp
internal readonly record struct AssimpPartsImport(
    IReadOnlyList<AssimpModelPart> Parts,
    ImportedSceneExtras Extras);
```

  - `AssimpSkinnedImport` gains `ImportedSceneExtras Extras { get; }` (constructor param).
  - `MeshCreator.SplitResult`:

```csharp
public readonly record struct SplitResult(
    bool Success,
    IReadOnlyList<SplitPart> Parts,
    ImportedSceneExtras Extras,
    string? Error)
{
    public static SplitResult Ok(IReadOnlyList<SplitPart> parts, ImportedSceneExtras extras) =>
        new(true, parts, extras, null);
    public static SplitResult Fail(string error) =>
        new(false, [], new ImportedSceneExtras([], [], 0), error);
}
```

  - Same extras field on `SkinnedResult` + `Ok(...)` overload.
  - `CreateSplit` behavior:
    - Call `ImportParts` → parts + extras.
    - If `parts.Count == 0` **and** extras have zero lights and zero cameras → Fail as today (`Assimp produced no mesh nodes`).
    - If `parts.Count == 0` but extras non-empty → **Success**, `Parts = []`, do **not** write `.mesh`, return extras (extras-only import).
    - If parts non-empty → existing mesh write path; attach extras (extractor uses same `unitFactor` as spawn transforms: call `Extract` with that factor — see note below).

**Unit-factor note:** Extract must run while `Scene*` is alive. Preferred order in `CreateSplit`:
1. `ImportParts` opens Assimp, walks meshes, extracts extras with `unitDownscaleFactor: 1f` into raw LocalToRoot matrices stored temporarily **or** extract stores raw transposed matrices and apply `ImportSpawnTransform` later in MeshCreator (cleaner).

**Preferred DTO tweak if needed during this task:** keep extractor returning raw `LocalToRoot` matrices + direction in root space **before** unit downscale; MeshCreator applies `ImportSpawnTransform.FromLocalToRoot` and scales translation of light/camera the same way as mesh parts. If Task 1 already baked TRS with factor `1f`, apply the same translation scaling in MeshCreator when `unitFactor != 1` (multiply Translation by factor / cm rules). Keep one place authoritative — document the choice in a one-line comment on MeshCreator.

Simplest consistent approach for implementers:
- Change Task 1 DTOs if still flexible: store `Matrix4x4 LocalToRoot` + `Direction` + `Color` (camera: fov/near/far/aspect) on internal cook structs; MeshCreator converts to public TRS DTOs with `ImportSpawnTransform`. **If Task 1 already shipped public TRS DTOs, apply unitFactor to Translation in MeshCreator identically to mesh parts.**

- [ ] **Step 1: Write failing `MeshCreatorExtrasTests`**

```csharp
[Fact]
public void CreateSplit_GltfWithLightAndCamera_SurfacesExtras()
{
    // assets root temp dir; fixture WriteMeshDirectionalCamera
    var result = MeshCreator.CreateSplit(gltfPath, assetsRoot, "lit");
    result.Success.ShouldBeTrue();
    result.Parts.Count.ShouldBeGreaterThan(0);
    result.Extras.Lights.Count.ShouldBe(1);
    result.Extras.Cameras.Count.ShouldBe(1);
}

[Fact]
public void CreateSplit_ExtrasOnly_SucceedsWithoutMeshFile()
{
    // Fixture: camera + directional, NO meshes (nodes without mesh).
    // If Assimp refuses empty mesh scenes, skip with clear message — else:
    result.Success.ShouldBeTrue();
    result.Parts.Count.ShouldBe(0);
    result.Extras.Cameras.Count.ShouldBe(1);
    File.Exists(Path.Combine(assetsRoot, "models", "camonly.mesh")).ShouldBeFalse();
}
```

- [ ] **Step 2: Run — expect fail**

```bash
dotnet test tests/Engine.Tests --filter MeshCreatorExtrasTests
```

- [ ] **Step 3: Wire importer + MeshCreator**

Update `ImportParts` / `ImportSkinned` to extract before `ReleaseImport`. Update `AssimpSkinnedImport`. Extend results. Fix compile errors in existing tests (`parts = importer.ImportParts` → `.Parts`).

- [ ] **Step 4: Run focused + existing mesh cook tests**

```bash
dotnet test tests/Engine.Tests --filter "MeshCreatorExtrasTests|MeshCreatorTests|ModelLoadingTests|SkinnedCookTests"
```

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add Engine/Renderer/AssimpModelImporter.cs \
  Engine/Renderer/Skeletal/AssimpSkinnedImport.cs \
  Engine/Renderer/MeshCreator.cs \
  tests/Engine.Tests/Renderer/ModelLoadingTests.cs \
  tests/Engine.Tests/Renderer/MeshCreatorExtrasTests.cs \
  tests/Engine.Tests/Renderer/MeshCreatorTests.cs \
  tests/Engine.Tests/Renderer/SkinnedCookTests.cs
git commit -m "$(cat <<'EOF'
Surface imported lights and cameras on mesh cook results.

EOF
)"
```

---

### Task 3: Editor spawn + Primary policy

**Files:**
- Modify: `Editor/Features/Import/Import3DModelBatch.cs`
- Modify: `tests/Editor.Tests/Import/Import3DModelSceneSpawnTests.cs`
- Modify: `Editor/Features/Import/Import3DModelPopup.cs` only if summary wiring needs the new note (prefer keep logic in Batch)

**Interfaces:**
- Consumes: `ImportedDirectionalLight`, `ImportedCamera`, `ImportedSceneExtras`
- Produces: updated

```csharp
public readonly record struct SourceImport(
    string Source,
    IReadOnlyList<MeshCreator.SplitPart> Parts,
    ImportedSceneExtras Extras,
    string? SkeletonRelativePath = null,
    string? ClipRelativePath = null);
```

`SpawnHierarchy` overloads:

```csharp
public static string SpawnHierarchy(IScene scene, string parentName, SourceImport source);

public static string SpawnHierarchy(
    IScene scene,
    string parentName,
    IReadOnlyList<MeshCreator.SplitPart> parts,
    ImportedSceneExtras extras,
    string? skeletonRelativePath = null,
    string? clipRelativePath = null);
```

Behavior:
1. If `parts.Count == 0` && extras lights/cameras empty → `"No parts to spawn."` (unchanged).
2. Else create parent (even if parts empty).
3. Spawn mesh children as today.
4. For each light in order: child entity + `TransformComponent` + `DirectionalLightComponent { Direction, Color }`.
5. Detect existing primary: any entity in `scene` with `CameraComponent.Primary == true`.
6. For each camera in order: child + transform + `CameraComponent` (Perspective, FOV/Near/Far/Aspect from DTO); first camera `Primary = !sceneHadPrimary`; rest `false`.
7. Return note including mesh count and `+ N directional light(s), M camera(s)` and skipped count when `SkippedNonDirectionalLights > 0`.

`TryImportBatch`: pass `result.Extras` / `skinned` extras into `SourceImport`.

Keep a thin overload `SpawnHierarchy(scene, name, parts, skeleton, clip)` for old tests by forwarding `extras: new ImportedSceneExtras([], [], 0)`.

- [ ] **Step 1: Write failing spawn tests**

```csharp
[Fact]
public void SpawnHierarchy_SpawnsDirectionalAndCameraChildren()
{
    using var scene = CreateScene();
    var parts = new List<MeshCreator.SplitPart> {
        new("Mesh", "models/a.mesh", 0, 1, Vector3.Zero, Vector3.Zero, Vector3.One)
    };
    var extras = new ImportedSceneExtras(
        [new ImportedDirectionalLight("Sun", Vector3.Zero, Vector3.Zero, Vector3.One,
            new Vector3(0, -1, 0), Vector3.One)],
        [new ImportedCamera("Cam", new Vector3(0, 1, 5), Vector3.Zero, Vector3.One,
            MathF.PI / 4f, 0.1f, 100f, 16f / 9f)],
        0);

    var note = Import3DModelBatch.SpawnHierarchy(scene, "house", parts, extras);
    note.ShouldContain("directional");
    var parent = scene.Entities.Single(e => e.Name == "house");
    var children = scene.GetChildren(parent).ToList();
    children.ShouldContain(c => c.HasComponent<DirectionalLightComponent>());
    children.ShouldContain(c => c.HasComponent<CameraComponent>());
    children.Single(c => c.HasComponent<CameraComponent>())
        .GetComponent<CameraComponent>().Primary.ShouldBeTrue();
}

[Fact]
public void SpawnHierarchy_DoesNotStealExistingPrimary()
{
    using var scene = CreateScene();
    var existing = scene.CreateEntity("MainCam");
    existing.AddComponent(new TransformComponent());
    existing.AddComponent(new CameraComponent { Primary = true, ProjectionType = CameraProjectionTypeData.Perspective });

    var extras = new ImportedSceneExtras(
        [],
        [new ImportedCamera("Imported", Vector3.Zero, Vector3.Zero, Vector3.One,
            MathF.PI / 4f, 0.1f, 100f, 16f / 9f)],
        0);

    Import3DModelBatch.SpawnHierarchy(scene, "prop",
        [new MeshCreator.SplitPart("P", "models/p.mesh", 0, 1, Vector3.Zero, Vector3.Zero, Vector3.One)],
        extras);

    scene.Entities.Single(e => e.Name == "Imported")
        .GetComponent<CameraComponent>().Primary.ShouldBeFalse();
    existing.GetComponent<CameraComponent>().Primary.ShouldBeTrue();
}

[Fact]
public void SpawnHierarchy_ExtrasOnly_CreatesParentWithoutMeshChildren()
{
    // parts empty, one camera in extras → parent + camera child, no ModelRenderer
}
```

Add usings for `SceneComponents.Lighting` and `SceneComponents.Camera`.

- [ ] **Step 2: Run — expect fail**

```bash
dotnet test tests/Editor.Tests --filter Import3DModelSceneSpawnTests
```

- [ ] **Step 3: Implement spawn + batch wiring**

Update `SourceImport`, `TryImportBatch`, `SpawnHierarchy`, summary formatter if present (`FormatSummaryMessage` — append spawn note already passed from popup).

Invalid camera params: if FOV ≤ 0 or Near ≤ 0 or Far ≤ Near, use `new CameraComponent()` defaults for those fields only.

- [ ] **Step 4: Run Editor + Engine import tests**

```bash
dotnet test tests/Editor.Tests --filter Import3DModel
dotnet test tests/Engine.Tests --filter "MeshCreatorExtrasTests|AssimpSceneExtrasExtractorTests"
```

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add Editor/Features/Import/Import3DModelBatch.cs \
  Editor/Features/Import/Import3DModelPopup.cs \
  tests/Editor.Tests/Import/Import3DModelSceneSpawnTests.cs
git commit -m "$(cat <<'EOF'
Spawn imported directional lights and cameras under import parent.

EOF
)"
```

---

### Task 4: Docs + intro non-goal update

**Files:**
- Modify: `docs/specs/3d-model-loading/introduction.md` — remove bullet “Cameras, lights, and empties from the file”; keep empties ignored; add short “Directional lights + cameras spawn on import (see `docs/specs/3d-import-lights-cameras/design.md`)”.
- Modify: `docs/specs/3d-model-loading/developer-guide.md` — replace “Ignore cameras, lights, and empties” with “Ignore empties; extract directional lights + cameras for spawn (not written to `.mesh`)”.
- Modify: `docs/guide/editor/component-inspector.md` ModelRenderer “When to use” sentence if it still says lights must be added manually after import — note import may spawn them when present in source.

- [ ] **Step 1: Edit the three doc files as above**

- [ ] **Step 2: Grep for stale claims**

```bash
rg -n "Ignore cameras, lights|cameras and lights|Cameras, lights, and empties" docs/
```

Expected: no stale “always ignored” claims (except historical review docs — leave reviews alone).

- [ ] **Step 3: Commit**

```bash
git add docs/specs/3d-model-loading/introduction.md \
  docs/specs/3d-model-loading/developer-guide.md \
  docs/guide/editor/component-inspector.md
git commit -m "$(cat <<'EOF'
Document import spawning of lights and cameras.

EOF
)"
```

---

### Task 5: End-to-end verification (manual + automated smoke)

**Files:**
- No new production files required.
- Optional: one Editor test that calls `SpawnHierarchy` with extras from a real `MeshCreator.CreateSplit` result (cook temp glTF → spawn) if not already covered.

- [ ] **Step 1: Automated smoke (add if missing)**

In `Import3DModelSceneSpawnTests` or Engine tests:

```csharp
[Fact]
public void CreateSplitThenSpawn_GltfFixture_RoundTripsLightAndCamera()
{
    // CreateSplit fixture → SpawnHierarchy(scene, stem, parts, extras)
    // Assert children counts
}
```

- [ ] **Step 2: Run full relevant suites**

```bash
dotnet test tests/Engine.Tests --filter "AssimpSceneExtrasExtractorTests|MeshCreatorExtrasTests|MeshCreatorTests|ModelLoadingTests"
dotnet test tests/Editor.Tests --filter Import3DModel
```

Expected: all PASS.

- [ ] **Step 3: Manual check (implementer)**

Import a known glTF/FBX with a directional light + camera into a scene with no primary camera. Confirm Hierarchy shows children, Play mode uses imported camera when Primary set, and mesh shading responds when imported directional is the first `DirectionalLightComponent` in the scene.

- [ ] **Step 4: Commit if smoke test file changed**

```bash
git add tests/Editor.Tests/Import/Import3DModelSceneSpawnTests.cs
git commit -m "$(cat <<'EOF'
Add cook-to-spawn smoke coverage for lights and cameras.

EOF
)"
```

---

## Spec coverage checklist

| Spec requirement | Task |
|------------------|------|
| Extract directional + cameras at cook (in-memory) | 1–2 |
| Skip point/spot/ambient with log | 1 |
| Spawn as children of import parent | 3 |
| Primary only if scene has none | 3 |
| No `.mesh` / VERSION change | 2 (explicit non-write of extras) |
| Extras-only source still spawns | 2–3 |
| Summary note counts | 3 |
| Tests listed in design | 1, 3, 5 |
| Docs update | 4 |

## Placeholder / consistency self-review

- No TBD steps; Assimp field names pinned to Silk.NET (`MDirection`, `MColorDiffuse`, `MHorizontalFOV`, …).
- `ImportedSceneExtras` empty factory used consistently: `new ImportedSceneExtras([], [], 0)`.
- Unit-factor handling called out so Direction/Translation stay consistent with mesh parts.
