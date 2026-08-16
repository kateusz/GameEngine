# 3D Rendering

**Formats (runtime):** imported `.mesh` only — see [3D Model Loading Pipeline](../../architecture/model-loading-pipeline.md).

**Import:** **File → Import 3D Model…** or drop `.fbx` / `.gltf` / `.glb` from the Content Browser onto the Viewport. Assimp writes `assets/models/<stem>_<part>.mesh` (textures under `models/textures/`) and spawns a parent entity plus child entities for multi-part models. Empty, bad, or legacy raw `ModelPath` → lit unit cube until you re-import and assign the `.mesh`.

**Lights (first match per type each frame):** `AmbientLightComponent`, `DirectionalLightComponent` (with shadow map), `PointLightComponent` (position from transform, cubemap shadows), and optional `SkyLightComponent` for HDR sky + image-based lighting ([PBR / IBL System](../../architecture/pbr-ibl-system.md)).

**Defaults when lights are missing:** ambient fill uses the engine built-in default (white, moderate strength). With no directional light entity, **direct sun contribution is zero** — raise ambient or add a directional light so metals and normals read clearly.

**Animation:** skeletal clips baked into `.mesh` v3 at import; playback via `SkeletalPlaybackComponent` paired with `ModelRendererComponent` on the same mesh path. See [Animation System](../../architecture/animation-system.md).

## Setup

1. Scene **3D** (Properties, no selection) or **Create 3D Entity**
2. Primary **Perspective** camera (included in Create 3D Entity)
3. **Ambient Light** + **Directional Light** entities (add **Sky Light** for HDR/IBL scenes)
4. **File → Import 3D Model…** (or viewport drop) into `assets/models/`
5. **Model Renderer** → assign `.mesh` on parent or child (or leave empty for cube)

Example: `Editor/assets/scenes/3d.scene` → `models/stachu-light.mesh`.

## Models

One `ModelRendererComponent` draws a submesh range from a `.mesh` file under its transform. Import may create **multiple entities** (parent + children) sharing one `.mesh` with different `SubmeshStart` / `SubmeshCount` slices.

Textures resolve relative to the project; missing maps use defaults. Inspector tuning: **Albedo** drop (PNG/JPG, overrides mesh/cube/sphere albedo), **Color** tint, optional **Metallic** / **Roughness Override** (all submeshes in range).

### Legacy paths

If `ModelPath` still points at `.fbx` / `.gltf` / `.glb`, runtime rejects it and shows the **unit cube**. Re-import, then assign the generated `.mesh` path.

## PBR

Metal/rough workflow: albedo, metallic-roughness packed map (G=roughness, B=metallic), optional normal and emissive. Legacy Phong materials convert heuristically at import. Transparent materials (`Blend` alpha mode) sort back-to-front after opaque draws.

## Troubleshooting

| Symptom | Check |
|---------|-------|
| Nothing visible | Primary camera, frustum, scene 3D + perspective |
| Black / flat | Directional `Color` and `Strength` non-zero; raise ambient `Strength`; metals need directional or IBL |
| Cube fallback | `ModelPath` is `.mesh` under `assets/`; re-import if still a raw format; console for load logs |
| Wrong scale | Transform **Scale** |
| Bad metals | Component overrides, or fix maps in DCC and re-import |
| Animation frozen | `SkeletalPlaybackComponent.Playing`, matching `MeshPath`, clip name in file |
| Dull environment | `SkyLightComponent` HDR path + `Intensity`; see IBL doc |

Details: [Component Inspector](../editor/component-inspector.md#modelrenderercomponent), [Rendering Pipeline](../../architecture/rendering-pipeline.md), [3D Model Loading Pipeline](../../architecture/model-loading-pipeline.md).
