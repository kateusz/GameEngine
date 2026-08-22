# Cameras and Rendering

Play mode renders through the **Primary** `CameraComponent`. No primary camera → nothing draws (edit mode uses the editor camera).

Adding the first camera in a scene auto-sets Primary. Only one should be primary; the editor enforces this.

## Orthographic projection

| Field | Role |
|---|---|
| `OrthographicSize` (**Size**) | Half-height of the view volume. Smaller values zoom in. |

`FixedAspectRatio` is available for orthographic cameras in the inspector.

## 2D sprites

- **SpriteRendererComponent** — full texture quad; optional `TexturePath`, `Color` tint
- **SubTextureRendererComponent** — atlas cell via `Coords` / `CellSize` / `SpriteSize`

## Draw order

Sprites draw in **entity iteration order**; depth test is off — **Z does not sort**. `SortingOrder` is planned ([Roadmap](../roadmap.md)).

Pipeline details: [Rendering Pipeline](../../architecture/rendering-pipeline.md). Property details: [Component Inspector](../editor/component-inspector.md#cameracomponent).
