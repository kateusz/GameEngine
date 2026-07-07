# Cameras and Rendering

Understand how the engine renders your game and how to set up cameras.

## How Rendering Works

The engine renders what the active camera sees. Every scene needs at least one entity with a `CameraComponent` marked as `Primary = true`. Without a primary camera, nothing is visible in play mode (the editor uses its own camera in edit mode).

## Setting Up a Camera

1. Create an entity (e.g., name it "Main Camera")
2. Add a `CameraComponent`
3. Set `Primary` to `true`
4. Position it with the `TransformComponent` (for 2D games, Z position is typically 0)

Only one camera should be marked Primary at a time. If multiple cameras are primary, the engine uses the first one it finds.

## Orthographic Projection (2D Games)

Orthographic projection renders a flat view with no depth perspective. Objects appear the same size regardless of their distance from the camera.

- **Size** controls how much of the world is visible (larger = more zoomed out)
- Best for: platformers, top-down games, puzzle games, 2D action games

To configure: set `ProjectionType` to `Orthographic` on the `CameraComponent`, then adjust `Size`, `NearClip`, and `FarClip`.

## Perspective Projection (3D Scenes)

Perspective projection renders with realistic depth. Distant objects appear smaller than nearby ones, creating a sense of depth.

- **PerspectiveFOV** controls the field of view (how wide the camera sees). Stored in radians internally; the editor displays degrees.
- Best for: 3D environments, first-person views, 3D action games

To configure: set `ProjectionType` to `Perspective`, then adjust `PerspectiveFOV`, `NearClip`, and `FarClip`.

## Sprite Rendering

For 2D visuals, the engine provides two components:

**SpriteRendererComponent** renders a textured quad. Assign a texture by dragging an image from the Content Browser, or set `TexturePath` directly. Use the `Color` property to tint the sprite. If no texture is set, a solid colored quad is rendered.

**SubTextureRendererComponent** renders a portion of a texture atlas. This is used for sprite sheet animations and tile-based graphics. You specify grid coordinates (`Coords`) and cell size (`CellSize`) to select which part of the atlas to display.

## 3D Rendering

For 3D visuals, add these components to an entity:

- **ModelRendererComponent** — `Color` tint for the drawn cube (default white)
- **TransformComponent** — position, rotation, and scale (the engine draws a 1×1×1 unit cube scaled by transform)

Optional scene lighting components (one of each is enough; the pipeline uses the first match):

- **AmbientLightComponent** — `Color` and `Strength` for base fill (default strength `0.1`)
- **DirectionalLightComponent** — `Direction` and `Color` for diffuse shading

Use **Perspective** projection on the primary camera for depth. Without a `DirectionalLightComponent`, cubes only receive ambient light.

Editor property reference: [AmbientLightComponent](../editor/component-inspector.md#ambientlightcomponent), [DirectionalLightComponent](../editor/component-inspector.md#directionallightcomponent), [ModelRendererComponent](../editor/component-inspector.md#modelrenderercomponent)

See [OpenGL 3D Rendering Workflow](../../opengl/opengl-3d-workflow.md) for shader and pipeline details.

## Render Order

For 2D games, sprites are sorted by their Z position (`Translation.Z` on the `TransformComponent`):

- **Lower Z** = rendered behind (background)
- **Higher Z** = rendered in front (foreground)

A typical layer setup:

| Layer | Z Position | Content |
|-------|-----------|---------|
| Background | -10 | Sky, distant scenery |
| Midground | 0 | Platforms, terrain |
| Characters | 1 | Player, enemies |
| Foreground | 10 | Overlays, near objects |

## Next Steps

- [Component Inspector](../editor/component-inspector.md) -- CameraComponent properties
- [Scripting Getting Started](../scripting/getting-started.md) -- control cameras from scripts
