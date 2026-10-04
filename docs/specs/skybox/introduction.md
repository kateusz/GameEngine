# Skybox — Conceptual Introduction

## What Problem Does This Solve?

A 3D view clears to `BackgroundColor` and then draws sprites and meshes. Nothing fills the gap around the geometry, so the world ends at a flat color. A sky is the picture that belongs in that gap. The picture is one environment image, stored as a cubemap so a later lighting pass can sample the same object. Image-based lighting itself is not part of this feature.

## What the Feature Will Achieve

- A scene names one equirectangular image. The 3D view shows that image as the background.
- An empty path leaves the clear color exactly as it is today.
- Sprites and meshes paint over the sky. The sky does not cover the HUD, and a click on the sky selects nothing.
- The editor and the standalone player show the same sky. Publishing a scene whose image is missing fails before the player starts.
- The image is 8-bit. There is no HDR load, no irradiance map, no prefiltered specular map, and no reflection on materials.

## Terminology

**Equirectangular image** — A single 8-bit picture of a sphere unwrapped onto a rectangle that is twice as wide as it is tall. The scene path points at this file.

**Color cubemap** — Six square images that meet at the edges of a cube. A direction from the center picks a color. This one stores color, not shadow depth.

**Capture** — The one-time draw that projects the equirectangular image onto those six faces. It runs when the path changes, not every frame.

**Skybox** — The cube drawn around the camera, sampled from the color cubemap, so the empty part of the view shows the environment.

**Sky view** — The camera view with its translation removed, then multiplied by the projection. Turning the camera moves the sky. Moving the camera does not.

## How It Works

The scene keeps a path string beside the clear color. Before a frame clears, the owner of that frame hands the path to the 3D graphics layer. The first time a path is seen, the image is decoded as display-encoded 8-bit color and captured into a cubemap. Later frames with the same path reuse that cubemap.

The frame still clears to `BackgroundColor`. The sky is drawn next, into that cleared buffer, with depth writes off, before any sprite. Sprites and opaque meshes then overwrite the pixels they own. Where nothing else draws, the sky remains. Where the path is empty, the sky draw is skipped and the clear color remains.

The cubemap is sampled as stored. It is not tonemapped and it is not run through a second gamma encode, because the source is already an 8-bit picture and the framebuffer is 8-bit.

## Out of Scope

- Image-based lighting, and reflections of the sky on meshes.
- HDR or floating-point environment images.
- A cubemap authored as six separate face files.
- A sky on a sprite. A 2D scene that has a path still gets the backdrop behind its sprites; the 2D pass does not grow a sky of its own.
- Reordering sprites and meshes. Sprites stay before the 3D pass. The sky is a new draw in front of both.
