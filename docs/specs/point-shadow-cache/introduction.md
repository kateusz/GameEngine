# Point Shadow Cache — Conceptual Introduction

## What Problem Does This Solve?

A point-shadow cubemap is a picture of the world around one lamp. Nothing in that picture depends on where the camera is. The frame still redraws all six faces of every casting lamp, every frame. A static warehouse pays that cost again when the camera merely turns.

This feature keeps a cubemap that is still true and redraws a lamp only when something that can change its picture has moved.

## What the Feature Will Achieve

- The first frame that draws point shadows renders every casting lamp once.
- Later frames reuse that cubemap while the lamp and the casters around it stay put. Moving the camera does not redraw it.
- Moving a caster redraws only the lamps whose range contained that caster before the move, and the lamps whose range contains it after the move.
- Moving a lamp, or changing its range, redraws that lamp alone.
- A caster that moves every frame redraws only the lamps it intersects. Other lamps in the same scene stay cached.
- A lamp farther than the existing shadow distance is not drawn. If something in its range moved while it was skipped, it redraws once when the camera comes back.
- The editor viewport still draws no point shadows. The first played frame after that redraws every casting lamp once.
- Color, intensity, ambient light, and the directional shadow are unchanged.

## Benefits and Outcomes

| Outcome | Why it matters |
|---------|----------------|
| Static scenes stay cheap | Six faces are paid once per lamp, not once per frame |
| Camera is free | Turning and dollying do not touch cubemaps |
| Local updates | One moving crate does not redraw lamps on the other side of the room |
| No stale exit | A crate that leaves a lamp takes its old shadow with it |
| Distant lamps wait | A skipped lamp remembers that it is dirty instead of showing an old map later |

## Terminology

**Cubemap** — The six depth faces already drawn around one lamp. This feature does not change what a face stores. It decides whether those faces are drawn again.

**Caster** — An opaque cube or model that the point-shadow pass already draws, including the fallback cube used when a model fails to load. Sprites, lines, and physics debug shapes are not casters.

**Pose** — Where a caster or a lamp was. For a caster that is the world matrix and the local bounds. For a lamp that is the position and the range.

**Mover** — A caster whose pose differs from the pose stored after the previous shadow frame, a caster that just appeared, or a caster that just disappeared.

**Dirty lamp** — A lamp whose cubemap must be drawn before it can be sampled. It stays dirty until that draw succeeds.

**Clean lamp** — A lamp whose stored cubemap still matches the world. The color pass samples it. No face is cleared or drawn.

**Sphere** — The ball around a lamp with radius equal to its range. A caster affects that lamp when its bounds, placed by its world matrix, touch that ball.

## Patterns and Principles

### The map is about the lamp, not the camera

The camera decides which lamps are near enough to show a shadow. It does not decide whether a stored map is still true. Truth comes from the lamp's pose and from the casters that touch its sphere.

### Remember the previous pose

A caster that leaves a sphere is invisible to a test that only looks at where it is now. The previous pose is what makes the lamp it left dirty. The same memory catches a caster that enters.

### One lamp, one map

The list of lamps can change order. The cubemap stays tied to the lamp entity, so a shift in the list does not attach yesterday's map to a different lamp.

### Dirty until drawn

Skipping a lamp because it is far from the camera does not make it clean. A failed map does not make it clean. Only a finished draw of all six faces does.

### Fail the same way as today

A lamp with no map still lights the scene. Ambient light and the directional shadow do not depend on this cache.

## Architecture Philosophy

The cache sits in the point-shadow portion of the existing frame:

1. **Remember** — poses from the last shadow frame
2. **Compare** — current casters and lamps against that memory
3. **Draw or reuse** — dirty lamps inside the shadow distance get six faces; clean lamps inside it only enable the stored map
4. **Store** — the current poses replace the memory after that decision

The editor's choice to skip point shadows leaves the memory unrefreshed and marks every casting lamp dirty for the next frame that does draw them. Loading another scene drops the memory, because entity ids from the previous scene must not be treated as the same objects.

## Relationship to Existing Shadows

Directional shadows still redraw every frame from the camera frustum. Point lights that do not cast, and point lights beyond the shadow distance, still light without a cubemap sample. A cached lamp uses the same cubemap, the same distance comparison, and the same soft edge as a lamp drawn this frame.

## Out of Scope

- Caching the directional shadow map
- Skipping a redraw when only the lamp's color or intensity changes, beyond the fact that those fields are already ignored
- A geometry shader, a two-hemisphere map, or a shared shadow atlas
- Redrawing a subset of the six faces
- Detecting animation that does not change the world matrix or the local bounds
