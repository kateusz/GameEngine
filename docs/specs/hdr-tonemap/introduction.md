# HDR Framebuffer and Tonemap — Conceptual Introduction

## What Problem Does This Solve?

The scene color is an 8-bit image, and the material shaders already compress it. Reinhard and gamma run before the frame is finished, so a later bloom or exposure pass would be scaling a picture that can no longer hold bright light. FXAA then smooths that compressed picture, because there is no linear image left to smooth.

The sky and the lit meshes encode themselves. Sprites do not. One attachment therefore mixes two kinds of color, and every new post effect has to know which kind it was given.

## What the Feature Will Achieve

- The scene color attachment stores linear light in a 16-bit float buffer.
- Sky, lit meshes, sprites, lines, and the clear color all land in that buffer without Reinhard and without gamma.
- One fullscreen pass, after the scene and before FXAA, applies Reinhard and gamma and writes an 8-bit image.
- FXAA smooths that 8-bit image. Turning FXAA off still shows the tonemapped image.
- The editor and the player use the same pass. Entity picking still reads the scene buffer.
- Bloom and exposure are not added. Their place is after the scene and before this pass.

## Terminology

**Scene color** — Attachment 0 of the framebuffer the scene is drawn into. It becomes linear light. It is not the picture on the monitor.

**Display image** — The 8-bit picture after Reinhard and gamma. This is what the window, ImGui, and FXAA are allowed to show.

**Tonemap pass** — The single fullscreen draw that turns scene color into the display image. It is not a material and not an anti-aliasing pass.

**Reinhard** — The existing compression `light / (light + 1)`. Bright values stay finite. A linear white of 1 becomes 0.5 before gamma, about 0.73 on the monitor.

## How It Works

The caller binds the scene framebuffer and clears it. The clear color is linear, so it is compressed with everything else. The frame then draws the sky, the sprites, the lines, and the lit meshes. None of those draws encode the color. The integer attachment still stores the entity id, and the depth attachment is unchanged.

When the scene is done, the tonemap pass samples attachment 0 and writes the display image. RGB is compressed and gamma-encoded. Alpha is copied. FXAA, when it runs, reads only that display image. The selection outline also reads the display image, and it still reads entity ids from the scene framebuffer. If the tonemap pass cannot be built, the float image is not shown.

## Out of Scope

- Bloom, exposure control, and any other post effect.
- A slider for the compression. Reinhard stays the formula already in the material shaders.
- Reordering sprites and meshes. Sprites stay before the 3D pass, and the tonemap covers them.
- Changing the depth test, the shadow passes, or the light list.
