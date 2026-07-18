# Cameras and Rendering

Understand how the engine renders your game and how to set up cameras.

## How Rendering Works

The engine renders what the active camera sees. Every scene needs at least one entity with a `CameraComponent` marked as `Primary = true`. Without a primary camera, nothing is visible in play mode (the editor uses its own camera in edit mode).

## Setting Up a Camera

1. Create an entity (e.g., name it "Main Camera")
2. Add a `CameraComponent`
3. Set `Primary` to `true`
4. Position it with the `TransformComponent` (for 2D games, Z position is typically 0)

Only one camera should be marked Primary at a time. The editor enforces a single primary camera; at runtime the first primary camera in scene iteration order is used.

## Orthographic Projection (2D Games)

Orthographic projection renders a flat view with no depth perspective. Objects appear the same size regardless of their distance from the camera.

- **Size** controls how much of the world is visible (larger = more zoomed out)
- Best for: platformers, top-down games, puzzle games, 2D action games

To configure: set `ProjectionType` to `Orthographic` on the `CameraComponent`, then adjust `OrthographicSize`, `OrthographicNear`, and `OrthographicFar` (the editor labels these **Size**, **Near**, and **Far**).

## Perspective Projection (3D Scenes)

Perspective projection renders with realistic depth. Distant objects appear smaller than nearby ones, creating a sense of depth.

- **PerspectiveFOV** controls the field of view (how wide the camera sees). Stored in radians internally; the editor displays degrees.
- Best for: 3D environments, first-person views, 3D action games

To configure: set `ProjectionType` to `Perspective`, then adjust `PerspectiveFOV`, `PerspectiveNear`, and `PerspectiveFar`.

## Sprite Rendering

For 2D visuals, the engine provides two components:

**SpriteRendererComponent** renders a textured quad. Assign a texture by dragging an image from the Content Browser, or set `TexturePath` directly. Use the `Color` property to tint the sprite. If no texture is set, a solid colored quad is rendered.

**SubTextureRendererComponent** renders a portion of a texture atlas (sprite sheet). You specify grid coordinates (`Coords`) and cell size (`CellSize`) to select which part of the atlas to display. Update `Coords` from scripts to switch frames manually.

## 3D Rendering

For models, lights, and PBR materials, see **[3D Rendering](3d-rendering.md)**. Short version: add `ModelRendererComponent` + perspective camera + ambient/directional lights; leave `ModelPath` empty for a lit cube.

## Render Order

2D sprites are drawn in entity iteration order with depth testing disabled. **Z position does not currently control draw order.**

`SortingOrder` on sprite render components is planned (see [Roadmap](../roadmap.md)) but not implemented yet. Until then, rely on entity creation order or split content across layers/cameras if draw order matters.

## Next Steps

- [3D Rendering](3d-rendering.md) -- models, lights, and PBR in the editor
- [Component Inspector](../editor/component-inspector.md) -- CameraComponent properties
- [Scripting Getting Started](../scripting/getting-started.md) -- control cameras from scripts
