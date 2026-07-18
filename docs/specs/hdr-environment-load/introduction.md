# HDR Environment File Load — Introduction

## Problem

Physically based materials need bright, high-dynamic-range environment images as input for later image-based lighting. The engine’s texture path today assumes low-dynamic-range images: 8-bit color uploaded as ordinary color textures. Radiance `.hdr` probes (equirectangular skies and studios) cannot be loaded without clamping or destroying their brightness range.

This feature solves only the first gap: get a `.hdr` file onto the GPU as a float texture that preserves HDR values. Cubemap conversion, irradiance, specular prefiltering, and IBL shading are separate later designs.

## What this feature delivers

- **Radiance `.hdr` load** — decode standard HDR environment probes through the existing texture factory.
- **Equirectangular float Texture2D** — lat-long layout kept as a 2D texture (not a cubemap yet).
- **RGBA16F GPU storage** — half-float color with alpha set to 1 when the file is RGB-only.
- **Same call path as LDR** — `Create(path)` auto-routes `.hdr` to the float path; callers do not need a special API.
- **Shared caching** — the same path returns the same cached texture instance.

## What this feature explicitly does not do (v1)

- **Cubemap conversion** — no equirect → cube at load time.
- **IBL** — no irradiance map, prefiltered specular, or BRDF LUT; no lighting shader changes.
- **Tone mapping / HDR display** — sampling the texture in a LDR view will look wrong (blown or washed); that is expected until a later display path.
- **`.exr` and other float formats** — Radiance `.hdr` only.
- **Editor skybox UI or environment component** — load API only; scene wiring comes with IBL.
- **Automatic downsampling** of huge probes.

## Key terminology

**HDR (High Dynamic Range).** Pixel values that can exceed the 0–1 display range, representing real relative brightness (sun vs shadow) without clipping at load time.

**Radiance `.hdr`.** A common HDR image format (RGBE) used for environment probes. Typically authored as an equirectangular panorama.

**Equirectangular map.** A 2D image that wraps a full sphere: horizontal angle around, vertical angle from floor to zenith. The usual downloadable form of IBL “HDRIs.”

**LDR (Low Dynamic Range).** Ordinary 8-bit textures (PNG, JPEG, etc.) with values intended for 0–1 display color.

**RGBA16F.** GPU texture format: four half-float channels. Enough precision for environment maps at half the memory of 32-bit float.

**Texture factory cache.** Path-keyed store of loaded textures so repeated loads share one GPU resource.

## Patterns and principles

**Extend the existing branch, don’t invent a parallel texture system.** DDS/TGA already diverge inside OpenGL texture creation; `.hdr` is another extension branch, not a new factory type.

**Preserve range; don’t author for the screen yet.** The job of this feature is faithful float upload. Display mapping belongs with HDR tone mapping / IBL.

**One texture type for bind.** HDR and LDR both remain `Texture2D` so bind slots and caching stay simple. Format lives on the GPU object, not a separate subclass.

**Fail loud.** Corrupt or fake `.hdr` files must not be silently reinterpreted as 8-bit LDR or cached as broken resources.

**YAGNI for IBL machinery.** No cubemaps, convolutions, or scene components until something actually samples the env map for lighting.

## Architecture philosophy

**Input asset first.** IBL needs a float equirectangular source. Deliver that source through the path artists and tools already use: a file path into the texture factory.

**Keep the factory as the single entry.** Auto-detect `.hdr` inside `Create(path)` so future environment components and tools do not grow a second load API.

**Leave conversion to the feature that needs it.** Cubemap and split-sum maps are derived assets; they belong with IBL, not with “can we open this file.”

**Lazy senior default.** Smallest change that loads `.hdr` as `RGBA16F` and caches it. Everything else waits.
