# Cameras and Rendering

Play mode renders through the **Primary** `CameraComponent`. If none is marked Primary when Play starts, the engine promotes the first camera it finds (and logs a warning). No `CameraComponent` at all → nothing draws.

`SetPrimaryCamera` keeps a single Primary flag. Duplicate/add paths that copy a Primary camera re-run that rule.

## Orthographic vs perspective

| Field | Role |
|---|---|
| `ProjectionType` | `Orthographic` (default, 2D) or `Perspective` (3D) |
| `OrthographicSize` (**Size**) | Half-height of the view volume. Smaller values zoom in. Component default: size 10, near −1, far 1 (`CameraConfig` editor defaults use −100 / 100) |
| `PerspectiveFOV` | Vertical field of view (radians in data; degrees in the inspector). Default 45° |
| `PerspectiveNear` / `PerspectiveFar` | Clip planes for perspective (defaults 0.01 / 1000) |

`FixedAspectRatio` skips viewport-driven aspect updates (`OnViewportResize`) for that camera.

For 3D models, switch the primary camera to **Perspective** or they will look flattened.

## Screen to world (2D)

`scene.CameraQueries.ScreenToWorld2D(windowPosition)` converts a pointer in window space to the Z=0 plane of the primary camera. Positions outside the game view return null.

## 2D sprites

- **SpriteRendererComponent** — full texture quad; optional `TexturePath`, `Color` tint (alpha 0 skips the draw)
- **SubTextureRendererComponent** — atlas cell via `Coords` / `CellSize` / `SpriteSize`

Sprites draw in **entity iteration order**; depth test is off — **Z does not sort**. `SortingOrder` is not implemented. Entities with `EffectiveVisible == false` are skipped.

## 3D models and cubes

**ModelRendererComponent** on an entity with a transform:

| Setup | What draws |
|---|---|
| Empty `ModelPath` | Unit cube. Optional `TexturePath` (sRGB albedo) and `TilingFactor` |
| `.glb` / `.gltf` / `.fbx`, no `MeshIndex` | Every imported submesh at this entity's world transform |
| Same, with `MeshIndex` | That submesh only (use when a model is split across child entities) |
| `SuppressDraw` | Skip the all-submeshes draw |

`Color` tints both paths. The first draw of a model path loads via runtime-mesh sibling (`.mesh`) when available, otherwise Assimp, then uploads GPU buffers; later frames use a path cache. Failed import draws a unit cube instead (one warning per path).

Import keeps the Assimp / runtime-mesh node graph (transforms are not baked into vertices). Unreal collision mesh names (`UCX_`, `UBX_`, …) are skipped. FBX files often store absolute texture paths from the DCC; the importer also looks next to the model file by texture name.

Opaque 3D draws are **frustum-culled**. Optional **visibility zones**: add `VisibilityZoneComponent` (local AABB) on a zone entity; set `ModelRendererComponent.VisibilityZoneEntityId` to that entity’s id so the mesh only draws while the camera is inside the zone.

**Supported today:** triangle meshes, albedo, normal, metallic-roughness, and occlusion maps, Cook-Torrance direct lighting, image-based fill when a sky capture exists, screen-space ambient occlusion, mesh instancing, directional/point shadows. **Not supported:** skinning, animation clips, transparent mesh sort, spot lights.

Put models under `assets/models/`.

### Importing models in the editor

Dropping a `.glb` / `.gltf` / `.fbx` on the viewport or assigning a model on `ModelRendererComponent` can **spawn a hierarchy** (meshes as child entities, optional lights as entities with `PointLightComponent` / `DirectionalLightComponent`). Assimp imports **point** and **directional** lights from the file; **spot** lights are skipped. Add `AmbientLightComponent` manually if the scene needs ambient fill.

## Lights

3D shading combines an indirect term, a directional light, and point lights (Cook-Torrance). The indirect term is flat ambient, or image-based light when a sky capture is bound. The frame uses the first ambient light, the first directional light, and the first eight point lights it visits. Only entities with light **components** contribute — either placed by hand or created by model import.

| Component | Limit | Notes |
|-----------|-------|--------|
| **AmbientLightComponent** | First in scene | `Color`, `Strength`. If none → white at strength 0.1 |
| **DirectionalLightComponent** | First in scene | `Direction`, `Color`, `Intensity`. Resolved color is RGB × intensity (negative intensity counts as 0). A near-zero direction becomes (0, −1, 0). If there is no component, or the resolved color is black → no sun |
| **PointLightComponent** | Up to **8**, each with a transform | `Color`, `Intensity` (negative counts as 0), `Range` (≤ 0 skipped), `CastsShadow`. Position = world translation (+ `Offset` when `ApplyOffset`). Lights past the eighth are ignored |

2D sprites ignore these lights.

Metallic, roughness, and AO on `ModelRendererComponent` are clamped to 0–1. On an unpacked submesh (`MeshIndex` set) that is still at the defaults (metallic 0, roughness 0.5, AO 1), the first draw copies metallic and roughness from that submesh. AO is not copied. Unit cubes use the component values only.

### Shadows

- **Directional** — runs when `SceneView.DirectionalShadows` is on (the default), the resolved sun color is not black, and a shadow frustum fits the camera. Opaque cubes and models write the map. Casters farther than `DirectionalShadowCasterMaxDistance` from the camera are skipped (default 50; 0 disables that cut).
- **Point** — set **CastsShadow**. A cubemap is redrawn or reused only for lights within **20** units of the camera. Farther lights still shade, without a shadow, that frame. Unchanged nearby lamps reuse the previous cubemap.
- `SceneView.PointShadows` and `SceneView.DirectionalShadows` both default to on. A caller can turn either off for that frame.

### Screen-space ambient occlusion

`SceneView.Ssao` defaults off. `SsaoRadius` defaults to `0.5` (view units) and `SsaoStrength` defaults to `1`. The pass needs a non-zero `TargetWidth` and `TargetHeight` and the camera `Projection`. It darkens only the indirect term. Play mode copies the pixel size from `IGraphics3D` onto the view before `RenderScene`. Algorithm: [Lighting](../../architecture/lighting.md).

Algorithm and shader constants: [Lighting](../../architecture/lighting.md), [Shadows](../../architecture/shadows.md). Pass order: [Scene Rendering Pipeline](../../architecture/scene-rendering-pipeline.md). Property details: [Component Inspector](../editor/component-inspector.md#cameracomponent).
