# Animation Timeline

Create and edit sprite sheet animations.

---

## Overview

The Animation Timeline window provides frame-by-frame authoring and preview for sprite animations. It reads animation data from a `.anim` asset file and lets you inspect every clip, step through individual frames, and preview playback without leaving the editor.

To open the timeline, select an entity that has an `AnimationComponent` attached. The panel will populate with the asset's clips and frames. If no entity is selected, or if the selected entity does not have an `AnimationComponent`, the panel displays a placeholder message.

---

## Animation Asset Structure

An animation asset (`.anim` file) groups everything needed for a sprite sheet animation into one place.

- **Atlas path** - the relative path (under `assets/`) to the sprite sheet texture that contains all frames.
- **Cell size** - the pixel dimensions of each cell in the atlas, expressed as a width/height pair. This is used by the engine for atlas bookkeeping; individual frame rectangles may differ from the cell size.
- **Clips** - one or more named animation sequences stored in the asset. A single asset can hold all animations for a character (for example, `idle`, `run`, and `attack`) by defining each as a separate clip.

---

## Creating Clips

Each clip in the asset defines an independent animation sequence.

| Property | Description |
|----------|-------------|
| **Name** | The identifier used to play the clip at runtime (e.g., `"idle"`). Must be unique within the asset. |
| **FPS** | Frames per second. Controls how quickly the engine advances through frames during playback. |
| **Loop** | Whether the clip repeats when it reaches its last frame. Can be overridden at runtime via `AnimationComponent.Loop`. |
| **Frames** | An ordered list of frame definitions. Each frame is defined by a pixel rectangle, not a grid coordinate. |

### Frame Definition

Frames use pixel-space rectangles referenced against the top-left origin of the atlas image.

| Property | Description |
|----------|-------------|
| **Rect** | `[x, y, width, height]` in pixels. Locates the frame within the atlas. |
| **Pivot** | Normalized origin point `[0..1, 0..1]` relative to the frame rectangle. The default is `[0.5, 0.0]` (bottom-center). |
| **Flip** | Horizontal and/or vertical mirroring flags. A non-zero X value flips horizontally; a non-zero Y value flips vertically. |
| **Rotation** | Rotation in degrees, clockwise-positive. |
| **Scale** | Per-frame scale multiplier as `[scaleX, scaleY]`. Defaults to `[1.0, 1.0]`. |
| **Events** | A list of string event names fired when the engine enters this frame during playback. |

---

## Frame Events

Frame events let you attach named triggers to specific frames. A common use is playing a footstep sound on the frame when a foot contacts the ground, or spawning a particle effect on the frame an attack lands.

- Add event names to the `Events` list of any frame in your `.anim` file.
- During playback the engine fires each event name when the animation enters that frame.
- Frames that carry events are marked with a **`[E]`** label above their thumbnail in the timeline strip.
- Hovering over a frame in the timeline shows a tooltip listing all event names attached to it.

Your scripts listen for these event names via the scripting API to trigger the corresponding game logic.

---

## Playback Controls

The playback toolbar sits above the timeline strip and controls editor preview. These controls affect preview only; they do not modify the asset or the entity's runtime state when the simulation is stopped.

| Control | Behavior |
|---------|----------|
| **Play / Pause** | Toggles preview playback. When playing, the playhead advances automatically at the clip's FPS. |
| **Stop** | Pauses playback and resets the playhead to frame 0. |
| **Loop** | When enabled, preview restarts from frame 0 after the last frame. When disabled, preview stops on the last frame. |
| **Speed slider** | Adjusts preview playback speed from 0.1x to 3.0x. Does not affect the `PlaybackSpeed` stored on the component. |

---

## Timeline View

The timeline strip is a horizontally scrollable row of frame thumbnails for the currently selected clip.

- Each cell shows a thumbnail cropped from the atlas at the frame's pixel rectangle, scaled to fit.
- Click any frame cell to select it. The selected frame is highlighted in blue.
- A red vertical line with a triangle at the top (the playhead) marks the current frame position. During preview playback the playhead moves automatically.
- Frames that have events attached show a **`[E]`** marker above their cell.
- Hovering over a frame shows a tooltip with its index, pixel rectangle, and any event names.

---

## Frame Details Panel

Selecting a frame in the timeline opens its details in the panel below the strip.

**Left side — thumbnail preview**

A preview of the frame rendered at its actual aspect ratio (up to 128 px on the longest edge). The atlas texture is cropped to the frame's rectangle, with flip applied if present.

**Right side — frame metadata**

| Field | Description |
|-------|-------------|
| **Rect** | Pixel rectangle `[x, y, width, height]` in the atlas. |
| **Pivot** | Normalized pivot `[x, y]`. |
| **Flip** | H and V flip state for the frame. |
| **Rotation** | Rotation value in degrees. |
| **Scale** | Scale multiplier `[x, y]`. |
| **Events** | Comma-separated list of event names, or `(none)` if the frame has no events. |

---

## Statistics

Below the frame details panel, the Statistics section reports metrics for the currently selected clip.

- **Total Frames** - the number of frames in the clip.
- **Duration** - total clip length in seconds, calculated as `frames / FPS`.
- **Memory** - estimated memory usage combining the atlas texture size (width x height x 4 bytes) and per-frame data (~256 bytes per frame).

These figures update immediately when you switch clips.

---

## Linking to an Entity

To drive an entity's sprite with an animation asset:

1. Add an `AnimationComponent` to the entity via the Component Inspector.
2. Set **Asset Path** to the path of your `.anim` file, relative to `assets/` (for example, `Animations/Characters/player.anim`).
3. Set **Current Clip Name** to the name of the clip that should play first.
4. The entity also requires a `SubTextureRendererComponent`. The animation system writes the current frame's UV coordinates to this component each tick.

Once `AssetPath` is set and the asset loads, selecting the entity in the Scene Hierarchy will populate the Animation Timeline panel automatically.

See also: [Component Inspector](component-inspector.md) — AnimationComponent section.
