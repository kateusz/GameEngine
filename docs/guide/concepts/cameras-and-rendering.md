# Cameras and Rendering

Play mode renders through the **Primary** `CameraComponent`. No primary camera → nothing draws (edit mode uses the editor camera).

Adding the first camera in a scene auto-sets Primary. Only one should be primary; the editor enforces this.

## Projections

| | Orthographic | Perspective |
|---|--------------|-------------|
| Use | 2D | 3D |
| Key field | `OrthographicSize` (**Size**) | `PerspectiveFOV` (**Vertical FOV**, degrees in editor) |

`FixedAspectRatio` is available for orthographic only in the inspector.

## 2D sprites

- **SpriteRendererComponent** — full texture quad; optional `TexturePath`, `Color` tint
- **SubTextureRendererComponent** — atlas cell via `Coords` / `CellSize` / `SpriteSize`

## 3D

Models, lights, PBR, shadows, transparency, skeletal animation: [3D Rendering](3d-rendering.md). Pipeline details: [Rendering Pipeline](../../architecture/rendering-pipeline.md).

## Draw order (2D)

Sprites draw in **entity iteration order**; depth test is off — **Z does not sort**. `SortingOrder` is planned ([Roadmap](../roadmap.md)).

Property details: [Component Inspector](../editor/component-inspector.md#cameracomponent).
