# 3D Model Loading (bake-on-import / `.mesh`) — Introduction

## Problem

3D scenes need artist-authored meshes with materials, not only the lit unit-cube prototype. Authors work in DCC tools that export FBX / glTF / GLB; the engine needs a deterministic runtime asset that does not re-parse Assimp on every load or in published builds. Skinned characters need the same cook-only path for bones, weights, and clips.

## What this feature delivers

- **Bake-on-import** — **File → Import 3D Model…** cooks supported interchange files (FBX / glTF / GLB) via Assimp into a versioned binary `.mesh`, with textures relocated beside the mesh.
- **Split-on-import** — **File → Import 3D Model…** packs Assimp mesh-bearing nodes into one `assets/models/<stem>.mesh` (textures under `models/textures/`), then spawns a parent entity plus one child per part (shared `ModelPath`, per-child submesh range) in the active scene.
- **Skinned cook (skeletal v1)** — When any Assimp mesh has bones (`mBones > 0`), Import uses `CreateSkinned` (no `PreTransformVertices`) and writes companion `.skel` + `.anim3d` beside the `.mesh`. Bone indices/weights are always present on every vertex (static meshes write zeros).
- **`.mesh`-only runtime** — `ModelPath` on the model renderer points at a cooked `.mesh`; `ModelFactory` loads via `MeshReader` and caches by path. No Assimp on the runtime hot path. Skeleton / clip paths load via `SkeletonFactory` / `Anim3dFactory`.
- **GPU palette skinning** — Shared StaticDraw mesh + per-entity `mat4[100]` bone palette uniform; identity palette = bind pose when playback is absent or not playing.
- **Playback component** — `SkeletalPlaybackComponent` on the import parent (paths + Playing/Loop/Speed/Time/ClipName). Pose evaluates in **play mode / runtime only** (same lifecycle as Audio), not while scrubbing the edit-mode viewport.
- **Per-entity draw** — one child draws a submesh slice of the shared file under that entity’s transform; move a child to move one imported part.
- **PBR materials** — albedo / metallic-roughness / normal maps (see physically-based-rendering specs); component tint and optional metallic/roughness overrides.
- **Graceful failure** — missing, corrupt, or non-`.mesh` paths fall back to the unit cube and log; missing individual textures do not fail the whole model.
- **Editor authoring** — MeshDropTarget and Content Browser “Type: Model” accept `.mesh` only; import is the path from source formats into project assets. Publish validates non-empty `SkeletonPath` / `ClipPath`.

## What this feature explicitly does not do (v1)

- **Blend trees / multi-clip blending** — one clip at a time via `ClipPath` + optional `ClipName` (`null`/empty → first clip in the `.anim3d`).
- **Bone entities / hierarchy mirroring** — bones stay in the `.skel` parent-index table; v1 flattens mesh parts to one parent + N children (node world→local under root), not a full Assimp parent tree.
- **Runtime Assimp load of raw formats** — `.fbx` / `.gltf` / `.glb` are not valid `ModelPath` values after cutover.
- **Cameras, lights, and empties from the file** — only meshes, supported material maps, and (when skinned) skeleton/clips.
- **Prefab export of the spawned tree** — import spawns into the open scene only; save a prefab yourself if needed.
- **Dual-read of `.mesh` VERSION 1** — hard cutover to VERSION 2 only; re-import required (see [re-cook-checklist.md](./re-cook-checklist.md)).

## Legacy ModelPath / VERSION breakage (hard cutover)

Scenes or prefabs that still store raw interchange paths (`.fbx` / `.gltf` / `.glb` / `.obj`, etc.) on `ModelPath` **no longer load those files**. Runtime rejects non-`.mesh` paths, logs, and draws the **unit cube** until you **re-import** via **File → Import 3D Model…** and point `ModelPath` at a resulting `models/<stem>.mesh` (or use the entities spawned on import).

Cooked `.mesh` files with **VERSION ≠ 2** fail to load with a clear **re-import** message. There is **no** dual-read migrator — overwrite or delete stale v1 assets and re-cook. Skinned sources must also produce sibling `.skel` + `.anim3d`.

## Key terminology

**Cook / bake.** Editor-time Assimp import → node split (or skinned extract) → texture relocate → write versioned `.mesh` under `assets/models/`. Static: `MeshCreator.CreateSplit`. Skinned: `MeshCreator.CreateSkinned` (+ companions). Assimp only here, not in `ModelFactory`.

**`.mesh`.** Versioned little-endian engine mesh container (**magic `KULA`**, **VERSION=2**) holding submeshes, always-present bone index/weight attrs, PBR material fields, and asset-relative texture path strings.

**`.skel` / `.anim3d`.** Companion binaries for skeletal v1 (magics `SKEL` / `AN3D`, VERSION 1). Written beside the `.mesh` stem on skinned import.

**Stem.** Filename without extension of the source file; destination is `<stem>.mesh` (and companions when skinned).

**Model path.** Project-relative path to a `.mesh` on `ModelRendererComponent` (e.g. `models/stachu-light.mesh`).

**Model factory.** Path-keyed cache and loader for `.mesh` only. Miss → `MeshReader` → GPU upload → cache. Rejects raw interchange.

**Mesh material.** Per-submesh PBR inputs: albedo / metallic-roughness / normal maps plus metallic and roughness scalars.

**Tint.** Color on the model renderer, multiplied with albedo.

**Cube fallback.** Empty path, failed load, or rejected non-`.mesh` path → shared lit unit cube.

**Bone palette.** Transient `Matrix4x4[100]` on `SkeletalPlaybackComponent`; identity = bind pose.

## Patterns and principles

**Path on component, resource from factory.** ECS stores the authoring reference; GPU resources live in the factory cache.

**One entity, one file.** Authors attach a whole cooked asset to one transform (children share path + submesh windows).

**Import more, cook what we support.** Source files may contain cameras and lights. Cook reads meshes, supported material maps, and (when skinned) bones/clips.

**Fail soft for iteration.** Bad paths degrade to the cube; missing maps degrade materials; play mode continues.

**Reuse the texture factory.** Re-homed texture paths resolve through `PathBuilder` + `TextureFactory`.

**Assimp is cook-only.** Runtime load is Assimp-free. The Assimp package may remain linked for cook; behavioral cutover does not require stripping the package in v1.

## Architecture philosophy

**Extend the cube prototype.** Keep the lit cube for empty / failed paths. Add model draw: factory → materials → draw each submesh (with optional bone palette).

**Bake once, load many.** Authors pay Assimp at import; play mode and published games read stable binaries.

**Materials match existing lighting.** Ambient + directional lights; PBR metal/rough shading on meshes; one lighting VS path with skinning for all model draws.

**Lazy senior defaults.** Flat models/ layout, path cache, cube on failure, hard cutover to `.mesh` v2, identity bind pose without playback. Ship textured and skinned models on entities; grow blend trees later.

## Sample note

The Editor sample under `Editor/assets/models/` (e.g. `stachu-light.mesh`, `trees.mesh`) is a cooked asset and may be large. Source `.glb` / `.fbx` are often not committed. After the VERSION 2 cutover, **re-cook** those samples — see [re-cook-checklist.md](./re-cook-checklist.md). Workflow: **File → Import 3D Model…**, then assign the cooked path in the inspector.
