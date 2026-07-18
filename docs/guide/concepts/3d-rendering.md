# 3D Rendering

Place models in a scene, light them, and tune materials.

## What You Get

- **Model import** — `.fbx`, `.gltf`, `.glb` via Assimp (drag from Content Browser onto **Model Renderer**)
- **PBR shading** — albedo / metallic-roughness / normal maps from the file; metal/rough scalars when maps are missing
- **Cube fallback** — empty or failed `ModelPath` draws a lit 1×1×1 unit cube (handy for blockout)
- **Lights** — one ambient + one directional light per scene (first of each type wins)

Not in this path yet: IBL/environment lighting, shadows, or splitting a file into child entities.

**Skeletal animation (v1):** add an **Animator** component beside **Model Renderer**. From scripts: `GetComponent<AnimatorComponent>().Play("Walk")` (also `Stop` / `Pause` / `Resume`; set `Loop` / `ApplyRootMotion` on the component). Clips must be embedded in the same model file. No blending or separate anim files yet.

## Quick Start

1. Set the scene to **3D** in the Properties panel (no entity selected), or use **Create 3D Entity** in the Scene Hierarchy.
2. Ensure a primary camera with **Perspective** projection (Create 3D Entity already does this).
3. Add light entities if needed:
   - **Ambient Light** — base fill (`Strength` default `0.1`)
   - **Directional Light** — sun-like light (`Direction` `(0, -1, 0)` = from above)
4. Create an entity → **Add Component** → **Model Renderer** (adds `Transform` if missing).
5. Drop a model onto the **Model** field, or leave it empty for a cube.
6. Move / rotate / scale with the viewport gizmos.

Example scene: `Editor/assets/scenes/3d.scene`.

## Camera

3D scenes need a **Perspective** primary camera so objects shrink with distance. Orthographic still works for flat “2.5D” views.

See [Cameras and Rendering](cameras-and-rendering.md) for projection settings.

## Models

Put model files under your project `assets/` folder (e.g. `assets/models/`). Supported drag-and-drop extensions: `.fbx`, `.gltf`, `.glb`.

| Behavior | Detail |
|----------|--------|
| Whole file | One entity draws every submesh in the file under that entity’s transform |
| Textures | Resolved relative to the model file’s directory |
| Cache | First load parses and uploads; later uses hit a factory cache |
| Failure | Missing file or bad import → log + cube fallback |

Keep textures next to the model (or on paths the file references). Missing individual maps do not fail the whole model — those slots use white / flat-normal defaults.

### Model Renderer properties

| Property | Purpose |
|----------|---------|
| **Model** (`ModelPath`) | Path to the model file |
| **Color** | Tint multiplied with albedo (and with the cube color) |
| **Metallic Override** | Optional 0–1; replaces imported metallic for all submeshes |
| **Roughness Override** | Optional 0–1; replaces imported roughness for all submeshes |

Full property table: [ModelRendererComponent](../editor/component-inspector.md#modelrenderercomponent).

## Lighting

Add lights as separate entities (or reuse the ones from **Create 3D Entity**).

| Component | Role |
|-----------|------|
| `AmbientLightComponent` | Scene-wide fill — `Color` × `Strength` |
| `DirectionalLightComponent` | One sun — `Direction` (from) and `Color` |

Without a directional light, directional contribution is **zero** — models look flat or black aside from ambient. Raise ambient `Strength` for night/indoor fill, or give the directional light a bright `Color`.

Only the **first** ambient and **first** directional component in the scene are used each frame.

Property reference: [AmbientLightComponent](../editor/component-inspector.md#ambientlightcomponent), [DirectionalLightComponent](../editor/component-inspector.md#directionallightcomponent).

## Materials (PBR)

Imported materials use a metal/rough workflow:

- **Albedo** — base color map (or tint when missing)
- **Metallic-roughness** — packed map (G = roughness, B = metallic) when the file provides it
- **Normal** — optional tangent-space normals
- Legacy Phong files convert heuristically (diffuse → albedo, shininess → roughness)

You cannot swap texture maps in the inspector yet. Scene tuning is limited to **Color** tint and optional metallic/roughness overrides on the component.

## Common Issues

| Symptom | Fix |
|---------|-----|
| Nothing visible | Primary camera? Entity in frustum? Scene dimension / perspective set? |
| Black / flat models | Add directional light with non-zero `Color`; raise ambient `Strength` |
| Cube instead of model | Check `ModelPath`, file under `assets/`, and editor console for Assimp/texture warnings |
| Huge or tiny model | Author scale differs; use Transform **Scale** |
| Wrong look on metals | Enable **Metallic Override** / **Roughness Override**, or fix maps in the DCC tool |

## Next Steps

- [Cameras and Rendering](cameras-and-rendering.md) — perspective vs orthographic
- [Content Browser](../editor/content-browser.md) — placing and dragging model assets
- [Component Inspector](../editor/component-inspector.md) — full property lists
- [OpenGL 3D Workflow](../../opengl/opengl-3d-workflow.md) — shaders, import pipeline, internals
