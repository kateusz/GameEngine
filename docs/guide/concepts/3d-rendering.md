# 3D Rendering

**Formats (runtime):** imported `.mesh` only — see [3D Model Loading Pipeline](../../architecture/model-loading-pipeline.md). **Import:** **File → Import 3D Model…** or drop `.fbx` / `.gltf` / `.glb` from the Content Browser onto the Viewport — Assimp writes `assets/models/<stem>_<part>.mesh` (textures under models/textures/), spawning parent+children in the open scene. Empty, bad, or legacy raw `ModelPath` → lit 1×1 cube until you re-import and assign the `.mesh`.

**Lights:** first `AmbientLightComponent` + first `DirectionalLightComponent` (2D shadow map) + first `PointLightComponent` (cubemap shadows, position from transform) per frame. No directional light → white default directional (metals need it; ambient alone leaves metals black).

**Not supported (v1):** animation/skins (roadmap follow-on), per-submesh entities.

## Setup

1. Scene **3D** (Properties, no selection) or **Create 3D Entity**
2. Primary **Perspective** camera (included in Create 3D Entity)
3. **Ambient Light** + **Directional Light** entities
4. **File → Import 3D Model…** (or drop `.fbx` / `.gltf` / `.glb` from the Content Browser onto the Viewport) to import sources into `assets/models/`
5. **Model Renderer** → drag a `.mesh` onto **Model** (or leave empty for cube)

Example: `Editor/assets/scenes/3d.scene` → `models/stachu-light.mesh`. Imported sample under `Editor/assets/models/` may be large (~94 MB).

## Models

One entity draws all submeshes under its transform. Textures resolve relative to the project via `PathBuilder` (paths stored relative inside `.mesh`). Parsed once from binary, then cached. Missing maps use defaults — whole load still succeeds.

Inspector tuning: **Color** tint, optional **Metallic** / **Roughness Override** (0–1, all submeshes). No per-map swap in editor yet.

### Legacy paths

If `ModelPath` still points at `.fbx` / `.gltf` / `.glb` (etc.), Runtime rejects it and shows the **unit cube**. Re-import the source, then set `ModelPath` to the nested `.mesh` path.

## PBR

Metal/rough workflow: albedo, metallic-roughness packed map (G=roughness, B=metallic), optional normal. Legacy Phong → heuristic conversion at import time.

## Troubleshooting

| Symptom | Check |
|---------|-------|
| Nothing visible | Primary camera, frustum, scene 3D + perspective |
| Black / flat | Directional `Color` non-zero; raise ambient `Strength`; check Metallic isn’t 1 without an MR map |
| Cube fallback | `ModelPath` is `.mesh` under `assets/`; re-import if still a raw format; console for load/reject logs |
| Wrong scale | Transform **Scale** |
| Bad metals | Overrides on component, or fix maps in DCC and re-import |
| Atlas / face-on-wrong-body | UV double-flip at import (engine must not FlipUVs when textures use stbi flip). Restart editor after import fixes to clear model cache. |

Details: [Component Inspector](../editor/component-inspector.md#modelrenderercomponent), [Rendering Pipeline](../../architecture/rendering-pipeline.md), [3D Model Loading Pipeline](../../architecture/model-loading-pipeline.md), [3D model loading specs](../../specs/3d-model-loading/introduction.md).
