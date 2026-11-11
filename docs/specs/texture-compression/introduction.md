# Texture compression (BC / DXT) — Conceptual Introduction

## What Problem Does This Solve?

The factory decodes PNG, JPEG, TGA, and DDS into 8-bit RGBA (or RGB) and uploads that with a full-size `TexImage2D`. A 4K albedo with mips is tens of megabytes on the GPU. Many such maps fill VRAM before the scene is interesting. DDS that is already DXT is decompressed on the CPU first, so the GPU never sees a block-compressed format.

Publish copies the source tree as-is. A shipped game therefore repeats the same uncompressed upload.

## What the Feature Will Achieve

- Scene and component paths still point at the artist file (PNG, JPEG, TGA, or a source DDS).
- A sibling `.dds` next to that file holds GPU-ready BC blocks and a mip chain.
- The editor and publish cook the sibling when it is missing or older than the source.
- Runtime loads the sibling and uploads compressed mips. It does not encode and does not write files.
- Albedo, sprites, and emissive use sRGB DXT. Normal maps use BC5 (two channels; the color shader reconstructs Z). Metallic-roughness stays DXT5 so green and blue survive. Ambient occlusion uses BC4 when the source is a single channel.
- HDR, cubemaps, and solid factory textures stay uncompressed.
- Shaders keep sampling `sampler2D`. Compression is an upload format, not a new pass.

## Benefits and Outcomes

| Outcome | Why it matters |
|---------|----------------|
| 4K maps cost a fraction of RGBA8 VRAM | DXT1 is 8 bytes per 4×4 block; DXT5/BC5 are 16 |
| Runtime does not hitch on encode | Cook happened in the editor or at publish |
| Source path is unchanged | No scene migration, no new component |
| Mips live in the sibling | The GPU does not generate mips from a 4K base level at load |
| Already-compressed DDS is not unpacked | Pfim decompression is skipped on the GPU path |

## Terminology

**Source** — The file the scene already names.

**Sibling** — `{sourcePath}.srgb.dds` or `{sourcePath}.linear.dds` (e.g. `wall.png.srgb.dds`). Same stem with different extensions get separate siblings.

**Cook** — Decode the source to RGBA, pick a BC format, encode mips on the CPU, write the sibling. Editor and publish do this. Runtime does not.

**Compressed upload** — `CompressedTexImage2D` per mip with an S3TC or RGTC internal format.

**S3TC / DXT** — Block color formats. Core OpenGL 3.3 does not include them; the backend requires `GL_EXT_texture_compression_s3tc` (and the sRGB variants for albedo).

**RGTC / BC4 / BC5** — One- and two-channel formats in OpenGL 3.3 core. Used for AO and normals.

**Freshness** — Sibling `LastWriteTimeUtc` is greater than or equal to the source’s. No content hash in version 1.

## Principles

**The source is the editor’s authority. The sibling is the runtime’s authority.** Runtime never encodes to repair a missing sibling. It may fall back to uncompressed RGBA so a broken install still draws, with a warning. Publish must fail if cook fails.

**OpenGL 3.3+ stays the floor.** BC7 (BPTC) needs 4.2 and is out. OpenGL 4.1 adds nothing this feature needs.

**Pad to 4×4, keep UVs.** Block formats need dimensions that are multiples of four. The encoder pads by repeating the edge. The texture object still reports the source width and height so existing UV and sprite sizes do not change. The DDS stores both the GPU size and the source size.

**Replace the sibling, do not patch it.** Cook writes a temp file in the same folder, then replaces. A failed cook leaves the previous sibling in place.

**Preview stays RGBA.** Content-browser thumbnails decode the source, not the BC sibling.

## Design Approach

A CPU encoder in engine code (no GL) produces a DDS with mips. `TextureFactory.Create` prefers that file, then uploads through the OpenGL backend. Publish walks `assets/` and cooks before it copies the tree. The color shader’s only change is reconstructing a unit Z from a BC5 normal’s XY.
