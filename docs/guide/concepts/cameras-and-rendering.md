# Cameras and Rendering

Play mode renders through the **Primary** `CameraComponent`. No primary camera → nothing draws (edit mode uses the editor camera).

Adding the first camera in a scene auto-sets Primary. Only one should be primary; the editor enforces this.

## Orthographic vs perspective

| Field | Role |
|---|---|
| `ProjectionType` | `Orthographic` (default, 2D) or `Perspective` (3D) |
| `OrthographicSize` (**Size**) | Half-height of the view volume. Smaller values zoom in. |
| `PerspectiveFOV` | Vertical field of view (radians in data; degrees in the inspector) |
| `PerspectiveNear` / `PerspectiveFar` | Clip planes for perspective (defaults 0.01 / 1000) |

`FixedAspectRatio` is available for both projections in the inspector.

For 3D models, switch the primary camera to **Perspective** or they will look flattened.

## 2D sprites

- **SpriteRendererComponent** — full texture quad; optional `TexturePath`, `Color` tint
- **SubTextureRendererComponent** — atlas cell via `Coords` / `CellSize` / `SpriteSize`

Sprites draw in **entity iteration order**; depth test is off — **Z does not sort**. `SortingOrder` is planned ([Roadmap](../roadmap.md)).

## 3D models and cubes

**ModelRendererComponent** on an entity with a transform:

| `ModelPath` | What draws |
|---|---|
| Empty | Unit cube. Optional `TexturePath` (sRGB albedo) and `TilingFactor` |
| `.glb` / `.gltf` / `.fbx` | Static imported mesh (drag from Content Browser) |

`Color` tints both paths. The first draw of a model path imports via Assimp and uploads GPU buffers; later frames use a path cache. Failed import draws a unit cube instead.

**Supported today:** triangle meshes, diffuse / specular / normal maps, Blinn-Phong lighting. **Not supported:** skinning, animation clips, PBR metallic-roughness as a lighting model, transparent mesh sort.

Put models under `assets/models/`. FBX files often store absolute texture paths from the DCC; the importer also looks next to the model file by texture file name.

## Lights

3D shading reads **one** ambient and **one** directional light from the scene (first component of each type):

- **AmbientLightComponent** — `Color`, `Strength` (default strength 0.1 if none)
- **DirectionalLightComponent** — `Direction`, `Color` (no directional light → specular/diffuse from the sun is black)

2D sprites ignore these lights.

Pipeline details: [Rendering Pipeline](../../architecture/rendering-pipeline.md). Property details: [Component Inspector](../editor/component-inspector.md#cameracomponent).
