# 3D Model Loading — Re-cook Checklist (`.mesh` VERSION 2)

Hard cutover: `MeshReader` accepts **VERSION 2 only** (magic **`KULA`**). There is **no** dual-read of VERSION 1 and **no** one-shot migrator CLI. Re-import every authoring source after the skeletal / mesh-v2 ship.

## Why

- VERSION 2 always embeds bone index/weight attrs (88-byte vertex stride).
- Skinned sources also need companion **`.skel`** + **`.anim3d`** beside the `.mesh`.
- Stale VERSION 1 files fail load with a clear **re-import** message.

## Steps

1. **Inventory**
   - Search the repo and local projects for `*.mesh`.
   - Search tests for assertions on VERSION 1 or stride 56 (should already be gone after Group 1).

2. **Re-import sources**
   - For each FBX / glTF / GLB authoring file: **File → Import 3D Model…** (or call `MeshCreator.CreateSplit` / `CreateSkinned`).
   - Prefer overwrite of the existing `assets/models/<stem>.mesh`, or **delete** the stale v1 `.mesh` first so nothing keeps pointing at a dead file.

3. **Confirm companions (skinned)**
   - Skinned imports must produce sibling files:
     - `assets/models/<stem>.mesh`
     - `assets/models/<stem>.skel`
     - `assets/models/<stem>.anim3d`
   - Static imports write `.mesh` only (bone attrs = zeros).

4. **Fix scene / prefab references**
   - Open scenes that reference models; update `ModelPath` and playback `SkeletonPath` / `ClipPath` if stems changed.
   - Raw interchange paths on `ModelPath` still draw the unit cube until pointed at a cooked `.mesh`.

5. **Verify Reader rejection**
   - Opening a VERSION ≠ 2 `.mesh` must fail with a message that includes the unsupported VERSION and **re-import** guidance (e.g. `Unsupported mesh VERSION 1; expected VERSION 2. Re-import the model in the editor.`).

6. **Play-mode smoke**
   - Static mesh draws in edit and play mode.
   - Skinned mesh without Playing (or no playback component) draws **bind pose** (identity palette).
   - With Playing=true in **play mode**, the clip advances (pose system does not run in edit-mode scrub).

7. **Publish dry-run**
   - Scene with playback paths fails publish if companions are missing.
   - Static-only scene still publishes without `.skel` / `.anim3d`.

## Known sample paths to re-cook

These paths are used by Editor samples / local 3D projects. Re-import their source FBX/glTF/GLB (or delete and re-cook) after VERSION 2:

| Cooked path (typical) | Notes |
|----------------------|--------|
| `Editor/assets/models/stachu-light.mesh` | Documented lit sample; source often `stachu-light.glb` (may be untracked / large) |
| `Editor/assets/models/trees.mesh` | Local/bin sample under 3d project assets |
| `Editor/assets/models/stachu.mesh` | If present from prior imports of `stachu.glb` |
| Any `*.mesh` under `Editor/assets/models/` or project `assets/models/` | Treat as stale until re-imported |

Also check scene references such as `Editor/assets/scenes/3d.scene` → `models/…`.

Sandbox / Runtime may carry raw interchange under `assets/models/` for experiments (e.g. Bistro FBX); those are **not** valid `ModelPath` values — cook via Import before assigning to components.

## Deferred automated coverage

GPU upload tests for `IShader.SetMat4Array` live in `Engine.GraphicsTests` (`OpenGLShaderSetMat4ArrayTests`). They need a display or CI xvfb host; do not treat a headless local skip as format/cook failure. Four-host `lightingShader.vert` sync is covered by a file-presence unit test (no GL).
