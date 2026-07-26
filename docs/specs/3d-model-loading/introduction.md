# 3D Model Loading (FBX / glTF) — Introduction

## Problem

3D rendering in the engine is a lit unit-cube prototype. Entities with a model renderer only tint that shared cube. There is no way to bring artist-authored meshes into a scene: no file importers, no textured 3D materials, and no path-based asset flow for models comparable to how sprites already load textures from disk.

Prior Assimp-based FBX work existed and was removed. The gap remains: without loading real meshes and materials, 3D stays a demo, not a content path.

## What this feature delivers

- **Runtime load from path** — a model file path on the model renderer component; first use parses and uploads, later uses hit a factory cache (same idea as textures).
- **FBX and glTF in v1** — both formats through one multi-format importer (Assimp via the existing Silk.NET stack).
- **Whole-file draw** — one path means the entire file: all submeshes drawn under that entity’s transform.
- **Textured lit materials** — diffuse and specular maps, shininess, optional normal maps, plus the existing color tint.
- **Graceful failure** — bad or missing files fall back to the unit cube and log; missing individual textures do not fail the whole model.
- **Editor authoring** — path field (and drag-drop) plus content-browser recognition of model extensions; no bake UI.

## What this feature explicitly does not do (v1)

- **Animation and skins** — bones, clips, and morph targets in the file are ignored.
- **Hierarchy as child entities** — node trees are not exploded into ECS children; one entity owns the whole file.
- **Editor bake / engine-native mesh format** — no import pipeline to a custom binary; source formats load at runtime.
- **PBR metal/rough workflow** — superseded by `docs/specs/physically-based-rendering/`; materials are now albedo / metallic-roughness / normal.
- **Cameras, lights, and empties from the file** — only meshes and the material maps above.
- **Per-submesh entity picking** — no mesh-index component API in v1.

## Key terminology

**Model.** The runtime result of loading one file: an ordered collection of submeshes, each with geometry and material data, keyed by path in a factory cache.

**Submesh.** One draw unit inside a model: vertex/index data uploaded to the GPU, plus a material describing how to shade it.

**Model path.** A project-relative (or otherwise resolvable) filesystem path to an `.fbx`, `.gltf`, or `.glb` file stored on the model renderer component.

**Model factory.** A path-keyed cache and loader. Callers ask for a path; on a miss the factory imports, builds meshes and materials, caches the model, and returns it.

**Importer.** The Assimp-backed step that reads source formats, applies fixed post-process rules (triangulate, normals/tangents as needed, PreTransformVertices), and maps file materials into the engine’s mesh material fields. UVs are left in OpenGL space (no FlipUVs); texture upload flips image rows for GL.

**Mesh material.** Per-submesh shading inputs: diffuse texture (optional), specular texture (optional), normal texture (optional), shininess, and a fallback diffuse color when no diffuse map exists.

**Tint.** The color on the model renderer component, multiplied with diffuse so existing color-only authoring still has meaning on textured meshes.

**Cube fallback.** Behavior when the model path is empty or load fails: draw the shared unit cube with the component tint so scenes remain editable and playable.

**Texture resolution.** Mapping texture references inside a model file to paths the texture factory can load — typically relative to the model file’s directory first.

## Patterns and principles

**Path on component, resource from factory.** Matches sprites and textures: ECS stores the authoring reference; GPU resources live in a shared cache owned by a factory, not on the component.

**One entity, one file.** Authors attach a whole asset to one transform. Splitting meshes into hierarchy or mesh-index components is a later feature if demand appears.

**Import more, ignore the rest.** Files often contain animation, cameras, and lights. v1 reads meshes and the supported material maps only; unsupported chunks are silent skips, not errors.

**Fail soft for iteration.** Asset pipelines are messy. A missing file or map should not take down play mode: log, degrade (cube or untextured material), continue.

**Reuse the texture factory.** Model materials must not grow a second texture cache. Diffuse/specular/normal paths go through the existing texture loading path.

**Defer bake without painting into a corner.** Runtime Assimp is the v1 contract. A future editor bake can still sit behind the same path/factory idea without changing how authors attach models to entities.

## Architecture philosophy

**Extend the cube prototype, don’t replace the 3D stack.** Keep the current lit cube path for empty paths. Add model draw beside it: factory → materials → draw each submesh under the entity transform.

**Silk.NET.Assimp as the single format bridge.** One dependency covers FBX and glTF (and more). Format-specific managed stacks are deferred unless Assimp quality for a format becomes a proven blocker.

**Materials match lighting that already exists.** Ambient and directional lights already shade the cube. Model materials feed that same lit path with maps and shininess rather than introducing a separate PBR pipeline in this feature.

**Lazy senior defaults.** Whole-file path, runtime cache, cube on failure, ignore animation, no bake, no hierarchy explosion. Ship “textured model on an entity”; grow the content pipeline when shipping builds demand it.
