# Directional Shadows — Conceptual Introduction

## What Problem Does This Solve?

Directional light shades every surface as if the sun could see it. A cube sitting on a floor is bright on the lit side and dark on the back, but the floor behind the cube stays fully lit. Nothing blocks the light.

This feature adds the simplest real shadow: one hard shadow from the single directional light, covering whatever the camera can see. It does not change ambient light, point lights, or the directional light component itself.

## What the Feature Will Achieve

- The existing directional light casts a hard shadow onto cubes and models. One extra depth drawing of that same geometry produces the shadow.
- The shadow covers the whole camera view, from the near plane to the far plane, and moves with the camera.
- Only the directional contribution is darkened. Ambient light still fills the shadowed area. Point lights still shine there.
- A scene with no directional light (the directional color is black, as it is today) looks the same as it does now. The extra drawing is skipped.
- Moving the camera by less than one shadow texel does not swim the shadow edge across a surface.
- Nothing new appears in the inspector. Map size and the self-shadow bias are fixed constants.

## Benefits and Outcomes

| Outcome | Why it matters |
|---------|----------------|
| Occlusion | Objects block the sun instead of only turning their own faces away from it |
| Same receivers | Cubes and models both cast and receive, using the geometry the color pass already draws |
| Unchanged fill | Ambient and point lights keep working in shadow, so a shadowed corner is not crushed to black |
| Safe scenes | No directional light means no shadow pass and no change to the picture |
| No new authoring | The light the scene already has is the light that casts |

## Terminology

**Shadow map** — A square depth image of the scene as seen by the directional light. Each pixel stores how far the light traveled before it hit something. The color pass reads it. It is not a picture of colors.

**Light view-projection** — The orthographic camera aimed along the light's rays. Its box is the camera frustum, expressed in light space. The depth pass and the color pass share this one matrix.

**Frustum corner** — One of the eight corners of the pyramid the camera sees, from near to far. Inverting the camera's view-projection recovers them. The light's box is the smallest box, in light space, that contains all eight.

**Window depth** — The value actually stored in the depth image. This engine's projection uses a depth of 0 at the near plane and 1 at the far plane, and OpenGL then stores that as a window depth. The color shader must compare that stored value, not the raw clip depth.

**Shadow factor** — 0 when the fragment is behind something the light already hit, 1 when the light reaches it. It multiplies only the directional term.

**Depth bias** — A small constant subtracted from the fragment's light-space depth before the comparison. It stops a surface from shadowing itself because its own depth and the map's depth are never exactly equal.

**Texel snap** — Pulling the center of the light's box onto the shadow map's pixel grid. Without it, a tiny camera move shifts the box by a fraction of a pixel and the shadow edge crawls.

**Caster and receiver** — The same opaque cubes and models. A caster writes the depth map. A receiver reads it. Sprites, lines, and physics debug shapes are neither.

## Patterns and Principles

### A second drawing, not a new light

The directional component stays direction and color. The shadow is another drawing of geometry that already exists, from a camera derived from that direction and from the view the color pass is about to use.

### The pipeline decides, the graphics layer draws

The render pipeline builds the light matrix and walks the cubes and models. The graphics layer owns the depth image, the depth shader, and the upload into the color shaders. Scene code does not bind textures. The graphics layer does not search entities.

### One walk, two passes

Cubes, textured cubes, models, failed-model fallbacks, and suppressed draws are decided in one walk. The shadow pass and the color pass both call it. A fallback cube in the picture is a fallback cube in the shadow.

### One comparison, two shaders

Cubes and models use the same depth test: one sample, in shadow or not. The engine loads each shader as its own file, so the test is copied into both fragment shaders. They have to stay in agreement.

### Fail open

If the light matrix cannot be built, the frame is drawn as it is today: no depth pass, shadow factor 1. A shadow that cannot be computed does not black out the scene.

### The box is the view, not the world

The map covers the camera frustum and the space that box occupies along the light. An object outside that box does not cast a shadow into the view. That is the limit of fitting one map to the whole view, including a distant far plane. A large far plane also makes each texel coarse, so shadow edges look blocky. Both limits are accepted for this version.

## Architecture Philosophy

Directional shadows sit on the frame path that ambient and directional light already use:

1. **Fit** — eight frustum corners become one orthographic light matrix, snapped to texels
2. **Depth pass** — the shared walk draws into a depth-only image
3. **Color pass** — the existing drawing, with the light matrix and the depth image uploaded once
4. **Shaders** — a single comparison scales the directional term

The component does not know about the map. The shaders do not know about entities. The map size is shared knowledge: the fit uses it to snap, and the graphics layer allocates that size.

## Relationship to Existing Lights

Ambient remains a scene-wide floor and is not shadowed. Directional light remains the first matching component, still independent of any transform, and is the only light that casts. Point lights remain an added term and are not shadowed. With a black directional color, the depth pass does not run and the picture matches today's lighting.

## Out of Scope

- Soft edges (several depth samples, or any filter wider than one texel)
- More than one shadow map, including cascades that would keep the near field sharp when the far plane is far away
- Shadows from point lights
- Shadows on sprites, lines, or physics debug drawing
- A shadow toggle, distance, bias, or resolution field on the component
- Bias that changes with the surface angle
- Drawing front faces into the depth map to hide self-shadowing
- Casters that sit outside the camera's light-space box

These limits keep the first version to one map, one sample, and no new authoring.
