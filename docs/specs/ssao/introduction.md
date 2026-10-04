# SSAO — Conceptual Introduction

## What Problem Does This Solve?

Indirect light fills every surface the same way. Flat ambient and image-based lighting do not know that a crease, a corner, or two faces pressed together see less of the surroundings. A material occlusion map darkens only the texels an artist painted. Surfaces that merely sit next to each other stay evenly lit, and the contact looks flat.

## What the Feature Will Achieve

- Creases and close faces on 3D meshes darken in the indirect term only: flat ambient and image-based lighting.
- The sun, point lights, and emissive color stay as they are.
- Sprites are unchanged. They still draw before the 3D sequence, with the depth test off.
- The effect runs in every rendered frame, in the editor and in the player.
- The primary camera stores an enable flag, a radius, and a strength. Those three values survive save and load. Existing scenes stay off, so they keep today's picture and do not pay the extra mesh draw.
- Sample count, depth bias, and the blur size stay in code.

## Terminology

**Occlusion factor** — A value from 0 to 1 for one pixel. 1 means the hemisphere is open and indirect light is unchanged. 0 means the hemisphere is blocked and indirect light is removed.

**Hemisphere kernel** — A fixed set of offsets that sit on the camera side of a surface. The surface normal aims the hemisphere. Samples behind the surface do not count.

**View space** — Positions and normals relative to the camera. The kernel and the depth comparison both happen here.

**Geometry target** — A framebuffer that holds the view-space normal and the window depth of the opaque 3D meshes only. It is not the scene framebuffer. Sprites never draw into it.

**Noise tile** — A 4×4 pattern of rotation vectors, repeated across the screen, that turns the kernel so a small sample count does not form bands.

**Range check** — A soft falloff that ignores a sample when the surface it hits is farther from the fragment than the kernel radius. The cleared far plane then cannot darken a silhouette.

## How It Works

The scene still draws the sky, then the sprites, then the shadow maps, then the lit meshes. SSAO is inserted after the shadow maps and before the lit meshes.

Opaque meshes draw a second time into the geometry target. That draw stores a view-space normal and a depth. It does not evaluate lights. The position of a pixel is rebuilt from that depth and the camera projection, so the target does not store a position.

A fullscreen draw reads the normal, the depth, and the noise tile. For each pixel it places the hemisphere on the normal, projects each sample back onto the screen, and compares the sample's view-space depth with the depth already stored there. Samples that fall inside nearby geometry raise the occlusion. The result is a grayscale value. A second fullscreen draw blurs that map just enough to hide the repeated noise.

The existing cube and model shaders then multiply ambient and image-based lighting by a mix of 1 and the blurred sample. The mix weight is the camera strength. Direct light is added afterwards, untouched. Tonemap, bloom, FXAA, and the selection outline read the combined color as they do today.

When the flag is off, the radius is unusable, the strength is zero, or the pass cannot be built, the lit shaders still sample a white texture. Indirect light is unchanged and the extra mesh draw does not run.

## Out of Scope

- A deferred lighting pipeline, and a stored position buffer.
- SSAO on sprites, and any change to the 2D-then-3D order or the depth policy.
- Darkening the sun or the point lights.
- Half-resolution occlusion, and an author-facing sample count.
- Copying the geometry depth over the scene depth.
