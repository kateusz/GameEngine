# Component Inspector

Reference for every component available in the engine.

## Overview

The **Properties** panel displays all components attached to the currently selected entity. To add a component, click the **Add Component** button at the bottom of the panel and select from the dropdown. To remove a component, right-click its header and choose **Remove Component**.

Each entity can hold any combination of components. The engine uses these components to drive rendering, physics, audio, scripting, and animation through its system pipeline.

---

## TransformComponent

Stores the position, rotation, and scale of an entity in world space. Every entity receives a TransformComponent automatically on creation; it cannot be removed.

| Property | Type | Default | Description |
|---|---|---|---|
| `Translation` | Vector3 | (0, 0, 0) | World-space position |
| `Rotation` | Vector3 | (0, 0, 0) | Euler angles in **radians** (X = pitch, Y = yaw, Z = roll) |
| `Scale` | Vector3 | (1, 1, 1) | Size multiplier per axis |

> **Rotation is in radians.** To convert: degrees × (π / 180). A 90-degree rotation is approximately 1.5708 radians.

**When to use:** Always present. Modify Translation, Rotation, and Scale to place and orient any entity in the scene.

---

## SpriteRendererComponent

Renders a 2D textured quad at the entity's transform. The quad is colored by the tint and optionally textured.

| Property | Type | Default | Description |
|---|---|---|---|
| `Color` | Vector4 (RGBA) | (1, 1, 1, 1) | Tint color applied to the sprite. White means no tint. |
| `TexturePath` | string | — | Path to the texture file. Drag from the Content Browser to assign. |
| `TilingFactor` | float | 1.0 | How many times the texture repeats across the quad. |

**When to use:** Any 2D visual — characters, backgrounds, props, UI elements. For sprite sheets, use SubTextureRendererComponent instead.

---

## SubTextureRendererComponent

Renders a rectangular region of a texture atlas (sprite sheet) rather than the whole texture. The region is defined by a grid-cell coordinate system.

| Property | Type | Default | Description |
|---|---|---|---|
| `TexturePath` | string | — | Path to the atlas texture file. |
| `Coords` | Vector2 | (0, 0) | Grid-cell coordinates of the desired sprite within the atlas. |
| `CellSize` | Vector2 | (16, 16) | Pixel dimensions of each cell in the atlas. |
| `SpriteSize` | Vector2 | (1, 1) | Size of the sprite in cells. Use (2, 2) for a sprite that spans four cells. |

**When to use:** Sprite sheets and tile-based graphics where multiple frames or tiles are packed into a single texture. Required by AnimationComponent — place both on the same entity.

---

## CameraComponent

Defines a viewpoint for rendering the scene. The scene is rendered from the perspective of the entity that holds the camera marked as Primary.

| Property | Type | Default | Description |
|---|---|---|---|
| `Primary` | bool | false | Designates this as the active game camera. Only one camera should be Primary at a time. |
| `FixedAspectRatio` | bool | false | When enabled, the camera maintains its aspect ratio regardless of viewport size. |

**Projection settings** are configured through the embedded SceneCamera:

- **Projection Type** — `Orthographic` (2D, no perspective) or `Perspective` (3D, vanishing point).

*Orthographic:*

| Property | Default | Description |
|---|---|---|
| `OrthographicSize` | 10.0 | Half-height of the view volume. Smaller values zoom in. |
| `OrthographicNear` | -100.0 | Near clip plane. |
| `OrthographicFar` | 100.0 | Far clip plane. |

*Perspective:*

| Property | Default | Description |
|---|---|---|
| `PerspectiveFOV` | 45° | Vertical field of view. Stored internally in radians; the editor displays and accepts degrees. |
| `PerspectiveNear` | 0.01 | Near clip plane. |
| `PerspectiveFar` | 1000.0 | Far clip plane. |

**When to use:** Every scene must have at least one entity with a CameraComponent where `Primary` is true, or nothing will render. Use Orthographic for 2D scenes and Perspective for 3D scenes.

See also: [Cameras and Rendering](../concepts/cameras-and-rendering.md)

---

## RigidBody2DComponent

Registers the entity with the 2D physics simulation. The physics system reads this component each frame to apply forces, velocity, and collision response.

| Property | Type | Default | Description |
|---|---|---|---|
| `BodyType` | enum | Static | Controls how physics acts on the body (see below). |
| `FixedRotation` | bool | false | When enabled, physics cannot rotate this entity. Useful for top-down characters. |

**BodyType values:**

| Value | Behavior |
|---|---|
| `Static` | Does not move. Use for walls, floors, and terrain. |
| `Dynamic` | Fully simulated — responds to gravity, forces, and collisions. Use for players and projectiles. |
| `Kinematic` | Moved by code only; not affected by gravity or forces. Use for moving platforms and scripted objects. |

**When to use:** Any entity that needs to participate in physics. Must be paired with a BoxCollider2DComponent on the same entity.

See also: [Physics Scripting](../scripting/physics.md)

---

## BoxCollider2DComponent

Defines a rectangular collision shape for the entity. Works in conjunction with RigidBody2DComponent to produce physical interactions.

| Property | Type | Default | Description |
|---|---|---|---|
| `Size` | Vector2 | (0.5, 0.5) | Half-extents of the box (width and height). |
| `Offset` | Vector2 | (0, 0) | Offset of the collider center relative to the entity's Transform position. |
| `Density` | float | 1.0 | Mass per unit area. Higher density produces a heavier body. |
| `Friction` | float | 0.5 | Surface friction coefficient. 0.0 is frictionless (ice); 1.0 is high friction (rubber). |
| `Restitution` | float | 0.7 | Bounciness. 0.0 = no bounce; 1.0 = perfectly elastic. |
| `RestitutionThreshold` | float | 0.5 | Minimum collision velocity required for bouncing to occur. |
| `IsTrigger` | bool | false | When true, the collider detects overlaps but does not produce physical collision response. |

**When to use:** Pair with RigidBody2DComponent on every entity that needs physical collision. Enable collider visualization in Debug Settings to see box bounds in the viewport. Use `IsTrigger` for pick-ups, zones, and sensors that should detect presence without blocking movement.

---

## NativeScriptComponent

Attaches a C# script class to the entity, enabling custom game logic driven by the engine's scripting lifecycle (`OnCreate`, `OnUpdate`, `OnDestroy`).

| Property | Type | Default | Description |
|---|---|---|---|
| `ScriptTypeName` | string | — | Fully qualified or simple name of the script class to instantiate. |

**When to use:** Any entity that requires custom behavior — player controllers, enemy AI, trigger logic, UI controllers. The script class must extend `ScriptableEntity`.

See also: [Scripting Getting Started](../scripting/getting-started.md)

---

## AudioSourceComponent

Emits audio from the entity's world position. Supports both 2D (non-positional) and 3D spatial audio with distance-based attenuation.

| Property | Type | Default | Description |
|---|---|---|---|
| `AudioClipPath` | string | — | Path to the audio file. WAV and OGG formats are supported. |
| `Volume` | float | 1.0 | Playback volume from 0.0 (silent) to 1.0 (full). |
| `Pitch` | float | 1.0 | Playback speed and pitch multiplier. 0.5 = half speed; 2.0 = double speed. |
| `Loop` | bool | false | Restart playback automatically when the clip finishes. |
| `PlayOnAwake` | bool | false | Begin playback automatically when the scene starts. |
| `Is3D` | bool | true | Enable spatial audio. When false, the sound plays at a fixed volume regardless of listener position. |
| `MinDistance` | float | 1.0 | Distance from the source at which audio is heard at full volume. |
| `MaxDistance` | float | 100.0 | Distance beyond which the audio is silent. |
| `Effects` | list | — | Audio effects applied to this source (see below). |

**Audio effects** — each entry in the `Effects` list represents one effect:

| Effect Type | Description |
|---|---|
| `Reverb` | Adds room reverberation. |
| `LowPass` | Attenuates high frequencies, producing a muffled sound. |
| `Echo` | Produces a repeating delay. |

Each effect has an `Enabled` toggle and an `Amount` slider (default 0.5).

**Note:** Programmatic Play, Pause, and Stop control from scripts is not yet available. Use `PlayOnAwake` and `Loop` to manage playback declaratively.

**When to use:** Sound effects, music, ambient audio, and positional sounds in a 3D environment. Disable `Is3D` for UI sounds and background music that should not attenuate with distance.

---

## AudioListenerComponent

Acts as the ears of the scene for 3D spatial audio. The audio system calculates volume and panning relative to the active listener's world position.

| Property | Type | Default | Description |
|---|---|---|---|
| `IsActive` | bool | true | Designates this as the active listener. Only one listener should be active per scene. |

**When to use:** Attach to the primary camera entity so the player hears audio from their viewpoint. If no listener is present, 3D spatial audio will not function correctly.

---

## AnimationComponent

Drives frame-by-frame sprite animation using a texture atlas. The AnimationSystem advances frames based on elapsed time and writes UV coordinates to the SubTextureRendererComponent on the same entity.

| Property | Type | Default | Description |
|---|---|---|---|
| `AssetPath` | string | — | Path to the animation asset file (`.anim`). |
| `CurrentClipName` | string | — | Name of the animation clip to play from the asset. |
| `Loop` | bool | true | Repeat the animation when it reaches the last frame. |
| `PlaybackSpeed` | float | 1.0 | Speed multiplier. 2.0 = double speed; 0.5 = half speed. |
| `ShowDebugInfo` | bool | false | Overlay debug information on the entity in the viewport. |

**Requirement:** A SubTextureRendererComponent must be present on the same entity. The animation system writes frame UV coordinates directly to it.

**When to use:** Animated characters, environmental animations, effects, and any sprite that cycles through multiple frames.

See also: [Animation Timeline](animation-timeline.md)

---

## MeshComponent

Holds a reference to a 3D mesh asset. The component stores the asset path for serialization; the mesh resource itself is loaded at runtime. Used together with ModelRendererComponent.

| Property | Type | Default | Description |
|---|---|---|---|
| `MeshPath` | string | — | Path to the model file. OBJ format is supported. |

**When to use:** Any entity that requires a 3D mesh. Pair with ModelRendererComponent for full 3D rendering with lighting.

---

## ModelRendererComponent

Renders the mesh referenced by MeshComponent using Phong shading and supports shadow casting and receiving.

| Property | Type | Default | Description |
|---|---|---|---|
| `Color` | Vector4 (RGBA) | (1, 1, 1, 1) | Tint color applied to the model. White means no tint. |
| `OverrideTexturePath` | string | — | Optional texture that replaces the model's embedded material texture. |
| `CastShadows` | bool | true | Whether this model contributes to shadow maps. |
| `ReceiveShadows` | bool | true | Whether this model displays shadows cast by other objects. |

**When to use:** Any 3D visual object in the scene. Requires a MeshComponent on the same entity. Use `OverrideTexturePath` to apply a custom texture without modifying the source model file.

---

## TagComponent

Stores a string label on the entity for identification and grouping. Tags are persisted with the scene and can be queried from scripts.

| Property | Type | Default | Description |
|---|---|---|---|
| `Tag` | string | "" | The label assigned to this entity (e.g., `"Player"`, `"Enemy"`, `"Pickup"`). |

**When to use:** Assign tags to categorize entities by role or type. Scripts can look up entities by tag to implement targeting, collision handling, or scene management logic.

---

## Coming Soon

The following components are planned and will be documented when available:

- **CircleCollider2D** — circular collision shape for physics.
- **ParticleSystemComponent** — GPU-accelerated particle effects.
- **UI components** — canvas, text, button, and image elements for in-game interfaces.
