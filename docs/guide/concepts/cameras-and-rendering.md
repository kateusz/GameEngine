# Cameras and Rendering

Play mode renders through the **Primary** `CameraComponent`. If none is marked Primary when Play starts, the engine promotes the first camera it finds (and logs a warning). No `CameraComponent` at all → nothing draws.

`SetPrimaryCamera` keeps a single Primary flag. Duplicate/add paths that copy a Primary camera re-run that rule.

## Orthographic vs perspective

| Field | Role |
|---|---|
| `ProjectionType` | `Orthographic` (default, 2D) or `Perspective` (3D) |
| `OrthographicSize` (**Size**) | Half-height of the view volume. Smaller values zoom in. Defaults: size 10, near −100, far 100 |
| `PerspectiveFOV` | Vertical field of view (radians in data; degrees in the inspector). Default 45° |
| `PerspectiveNear` / `PerspectiveFar` | Clip planes for perspective (defaults 0.01 / 1000) |

`FixedAspectRatio` skips viewport-driven aspect updates (`OnViewportResize`) for that camera.

For 3D models, switch the primary camera to **Perspective** or they will look flattened.

## Screen to world (2D)

`scene.CameraQueries.ScreenToWorld2D(windowPosition)` converts a pointer in window space to the Z=0 plane of the primary camera. Positions outside the game view return null.

## 2D sprites

- **SpriteRendererComponent** — full texture quad; optional `TexturePath`, `Color` tint (alpha 0 skips the draw)
- **SubTextureRendererComponent** — atlas cell via `Coords` / `CellSize` / `SpriteSize`

Sprites draw in **entity iteration order**; depth test is off — **Z does not sort**. `SortingOrder` is not implemented.

## 3D models and cubes

**ModelRendererComponent** on an entity with a transform:

| Setup | What draws |
|---|---|
| Empty `ModelPath` | Unit cube. Optional `TexturePath` (sRGB albedo) and `TilingFactor` |
| `.glb` / `.gltf` / `.fbx`, no `MeshIndex` | Each imported submesh at the Assimp node transform × this entity's world transform |
| Same, with `MeshIndex` | That submesh only (use when a model is split across child entities) |
| `SuppressDraw` | Skip the packed all-submeshes draw |
| `TexturePath` with a model | Optional albedo override for every drawn submesh |

`Color` tints both paths. `IModelFactory.Create` fills a path cache (including before the 3D pass); the draw uses `TryGet`. Failed import draws a unit cube instead (one warning per path). Missing files retry if they appear later.

Import keeps the Assimp node graph (transforms are not baked into vertices). Editor drop and `ModelHierarchySpawner.Instantiate` unpack multi-mesh graphs onto child entities. Unreal collision mesh names (`UCX_`, `UBX_`, …) are skipped. FBX files often store absolute texture paths from the DCC; the importer also looks next to the model file by texture name.

**Supported today:** triangle meshes, diffuse / specular / normal maps, Blinn-Phong lighting. **Not supported:** skinning, animation clips, PBR metallic-roughness as a lighting model, transparent mesh sort.

Put models under `assets/models/`.

## Lights

3D shading reads **one** ambient and **one** directional light from the scene (first component of each type):

- **AmbientLightComponent** — `Color`, `Strength` (default strength 0.1 if none)
- **DirectionalLightComponent** — `Direction`, `Color` (no directional light → specular/diffuse from the sun is black)

2D sprites ignore these lights.

Pipeline details: [Rendering Pipeline](../../architecture/rendering-pipeline.md). Property details: [Component Inspector](../editor/component-inspector.md#cameracomponent).
