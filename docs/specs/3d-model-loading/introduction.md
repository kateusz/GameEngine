# 3D Model Loading (bake-on-import / `.mesh`) — Introduction

## Problem

3D scenes need artist-authored meshes with materials, not only the lit unit-cube prototype. Authors work in DCC tools that export FBX / glTF / GLB; the engine needs a deterministic runtime asset that does not re-parse Assimp on every load or in published builds.

## What this feature delivers

- **Bake-on-import** — **File → Import 3D Model…** cooks supported interchange files (FBX / glTF / GLB) via Assimp into a versioned binary `.mesh`, with textures relocated beside the mesh.
- **Split-on-import** — **File → Import 3D Model…** packs Assimp mesh-bearing nodes into one `assets/models/<stem>.mesh` (textures under `models/textures/`), then spawns a parent entity plus one child per part (shared `ModelPath`, per-child submesh range) in the active scene.
- **`.mesh`-only runtime** — `ModelPath` on the model renderer points at a cooked `.mesh`; `ModelFactory` loads via `MeshReader` and caches by path. No Assimp on the runtime hot path.
- **Per-entity draw** — one child draws a submesh slice of the shared file under that entity’s transform; move a child to move one imported part.
- **PBR materials** — albedo / metallic-roughness / normal maps (see physically-based-rendering specs); component tint and optional metallic/roughness overrides.
- **Graceful failure** — missing, corrupt, or non-`.mesh` paths fall back to the unit cube and log; missing individual textures do not fail the whole model.
- **Editor authoring** — MeshDropTarget and Content Browser “Type: Model” accept `.mesh` only; import is the path from source formats into project assets.

## What this feature explicitly does not do (v1)

- **Animation and skins** — bones, clips, and morph targets are ignored at cook time. Animation/skinning is a **roadmap follow-on**, not part of this bake.
- **Deep Assimp hierarchy mirroring** — v1 flattens to one parent + N children (node world→local under root), not a full Assimp parent tree.
- **Runtime Assimp load of raw formats** — `.fbx` / `.gltf` / `.glb` are not valid `ModelPath` values after cutover.
- **Cameras, lights, and empties from the file** — only meshes and supported material maps.
- **Prefab export of the spawned tree** — import spawns into the open scene only; save a prefab yourself if needed.

## Legacy ModelPath breakage (hard cutover)

Scenes or prefabs that still store raw interchange paths (`.fbx` / `.gltf` / `.glb` / `.obj`, etc.) on `ModelPath` **no longer load those files**. Runtime rejects non-`.mesh` paths, logs, and draws the **unit cube** until you **re-import** via **File → Import 3D Model…** and point `ModelPath` at a resulting `models/<stem>.mesh` (or use the entities spawned on import).

## Key terminology

**Cook / bake.** Editor-time Assimp import → node split → texture relocate → write one versioned `.mesh` under `assets/models/`. Implemented by `MeshCreator.CreateSplit` (Assimp only here, not in `ModelFactory`).

**`.mesh`.** Versioned little-endian engine mesh container (GEMH magic, VERSION=1) holding submeshes, PBR material fields, and asset-relative texture path strings.

**Stem.** Filename without extension of the source file; destination is `<stem>.mesh`.

**Model path.** Project-relative path to a `.mesh` on `ModelRendererComponent` (e.g. `models/stachu-light.mesh`).

**Model factory.** Path-keyed cache and loader for `.mesh` only. Miss → `MeshReader` → GPU upload → cache. Rejects raw interchange.

**Mesh material.** Per-submesh PBR inputs: albedo / metallic-roughness / normal maps plus metallic and roughness scalars.

**Tint.** Color on the model renderer, multiplied with albedo.

**Cube fallback.** Empty path, failed load, or rejected non-`.mesh` path → shared lit unit cube.

## Patterns and principles

**Path on component, resource from factory.** ECS stores the authoring reference; GPU resources live in the factory cache.

**One entity, one file.** Authors attach a whole cooked asset to one transform.

**Import more, ignore the rest.** Source files may contain animation, cameras, and lights. Cook reads meshes and supported material maps only.

**Fail soft for iteration.** Bad paths degrade to the cube; missing maps degrade materials; play mode continues.

**Reuse the texture factory.** Re-homed texture paths resolve through `PathBuilder` + `TextureFactory`.

**Assimp is cook-only.** Runtime load is Assimp-free. The Assimp package may remain linked for cook; behavioral cutover does not require stripping the package in v1.

## Architecture philosophy

**Extend the cube prototype.** Keep the lit cube for empty / failed paths. Add model draw: factory → materials → draw each submesh.

**Bake once, load many.** Authors pay Assimp at import; play mode and published games read a stable binary.

**Materials match existing lighting.** Ambient + directional lights; PBR metal/rough shading on meshes.

**Lazy senior defaults.** Flat models/ layout, path cache, cube on failure, ignore animation, hard cutover to `.mesh`. Ship textured models on entities; grow animation when the roadmap demands it.

## Sample note

The tracked Editor sample under `Editor/assets/models/` is a cooked asset (`.mesh` + textures) and may be large (~94 MB). Source `.glb` is not committed. Workflow: **File → Import 3D Model…**, then assign the cooked path in the inspector.
