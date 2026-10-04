# Image-Based Lighting — Conceptual Introduction

## What Problem Does This Solve?

A metal in shadow is black. The sun is multiplied by the shadow, and the fill that remains is a flat color times albedo. A metal has almost no diffuse response, so that fill does not light it, and there is no environment for the specular lobe to reflect.

The scene can already show one picture behind the geometry. That picture is display-encoded and 8-bit, so it cannot carry the range a reflection needs. This feature makes the sky path a radiance image and uses it as the indirect light.

## What the Feature Will Achieve

- `Skybox` is one equirectangular radiance file. The same image is the backdrop and the light.
- An empty path leaves the clear color and the flat ambient fill exactly as they are today.
- Where the file is set, indirect light replaces that fill. Diffuse comes from a blurred irradiance map. Specular comes from the split-sum pair: a roughness-blurred environment and a BRDF lookup.
- Ambient occlusion darkens that whole indirect term, diffuse and specular. The sun and the point lights stay on their own response and their own shadows.
- The scene target stays 8-bit. The sky and the lit color are Reinhard-compressed and gamma-encoded on the way out. FXAA still sees a display image.
- The editor and the player resolve the same path. Publishing a scene whose file is missing, or is not a radiance file, fails before the player starts.

## Terminology

**Radiance image** — An equirectangular picture whose texels are linear light and may be brighter than one. The scene path points at this file. It is not a display-encoded photo.

**Environment cubemap** — The radiance image projected onto six square faces. A direction from the center picks a color. This is the source the sky draws and the source the blurs read.

**Irradiance map** — A low-resolution cubemap. Each texel is the cosine-weighted average of the environment over the hemisphere facing that direction. Sampling it with the surface normal gives the indirect diffuse light.

**Prefiltered environment** — The same surroundings, blurred by roughness and stored in mip levels. Smooth surfaces read the sharp mip. Rough surfaces read a wide blur. The sample direction is the reflection vector.

**Split sum** — The split of the specular integral into those two pieces: the blurred environment, and a scale and bias on the Fresnel term. The scale and bias live in a lookup that does not depend on the picture.

**BRDF lookup** — That 2D table. Horizontal is the angle between normal and view. Vertical is roughness. Red is the scale, green is the bias.

**Indirect term** — Diffuse irradiance plus split-sum specular, multiplied by occlusion. It occupies the place where the flat ambient fill used to be added.

## How It Works

The scene keeps a path string beside the clear color. Before a frame clears, the owner of that frame hands the path to the 3D graphics layer. The first time a path is seen, the file is decoded as linear floating-point color and captured into an environment cubemap. From that cubemap the layer builds the irradiance map and the prefiltered environment. Later frames with the same path reuse all three. The lookup table is built once when the graphics layer starts, and a scene change does not rebuild it.

The sky is drawn first, with depth writes off, before sprites. It samples the environment cubemap and compresses the result into the 8-bit buffer. Sprites and meshes then overwrite the pixels they own. Sprites do not read the indirect light.

In the forward color pass, a cube or a mesh samples the irradiance map along the normal and the prefiltered environment along the reflection. The lookup supplies the Fresnel scale and bias. Metals drop the diffuse part. Occlusion multiplies both parts. The sun and the lamps are added afterwards, and the existing compression encodes the sum once.

A missing or rejected file does not replace a sky that is already on screen. The path is remembered so the next frames do not decode it again until the text changes. With no sky stored, the color pass uses the flat ambient fill.

## Out of Scope

- Reflection probes, parallax correction, and blending several environments across a scene.
- Storing the blurred cubemaps on disk.
- An HDR scene target. Float storage is the captured cubemaps and the lookup. The scene buffer stays 8-bit.
- A second path that lights from one image and draws another.
- Lighting for sprites and lines.
- Changing which light is the sun, how many point lights exist, or the order of the 2D and 3D passes.
