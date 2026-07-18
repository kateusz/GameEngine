# Physically Based Rendering (Core Metal/Rough) — Introduction

## Problem

3D meshes already load and draw, but they are shaded with Blinn-Phong: diffuse/specular maps and a shininess exponent. That model does not match modern art pipelines (glTF metal/rough) and produces the wrong look on assets like the Amazon Lumberyard Bistro — chalky metals, uniform hotspots, and no consistent roughness response.

The engine already has ambient + one directional light and a forward mesh draw path. The missing piece is a material and BRDF language artists and importers already speak.

## What this feature delivers

- **Metal/rough materials** — albedo (base color), packed metallic-roughness, optional normals, plus scalar metallic/roughness and albedo tint when maps are missing.
- **Cook-Torrance shading** — energy-aware microfacet specular + diffuse split under the existing directional light, with a simple metal-aware ambient fill.
- **Replace Phong** — Blinn-Phong mesh shading, specular maps, and shininess are removed; one shading model for meshes.
- **glTF-aligned import** — Assimp fills the new fields from PBR materials; legacy diffuse/specular assets convert heuristically so old files still load.
- **Per-entity scalar overrides** — model renderer tint always applies; optional metallic/roughness overrides replace imported material scalars when set (maps stay file-authored).

## What this feature explicitly does not do (v1)

- **IBL / environment lighting** — no cubemaps, irradiance, prefiltered specular, or BRDF LUT.
- **HDR tone mapping / bloom / post stack** — display remains whatever the current framebuffer path does.
- **Shadows, AO, GI, SSR** — separate features.
- **Deferred or clustered multi-light** — still forward, ambient + one directional.
- **Separate metallic and roughness textures** — only packed metallic-roughness (G = roughness, B = metallic).
- **Persisted material assets** — overrides live on the component; no standalone material file save/load.
- **Map pickers in the editor** — no swapping textures in the inspector in v1.

## Key terminology

**Physically Based Rendering (PBR).** Shading that uses measurable material parameters and an energy-conserving light response so the same asset looks consistent under different lights.

**Metal/rough workflow.** Materials described by base color (albedo), metallic (dielectric ↔ metal), and roughness (smooth ↔ rough), instead of diffuse/specular/shininess.

**Albedo (base color).** The underlying color of the surface. For dielectrics this is the diffuse color; for metals it tints the specular reflectance.

**Metallic.** A 0–1 blend: 0 = non-metal (dielectric), 1 = metal. Controls whether the surface uses dielectric Fresnel behavior or metal-like specular colored by albedo.

**Roughness.** A 0–1 measure of microsurface irregularity. Low = sharp highlights and reflections; high = broad, dull highlights.

**Packed metallic-roughness map.** One texture where green stores roughness and blue stores metallic (glTF convention). Red is unused in v1 (no occlusion channel).

**Cook-Torrance BRDF.** The specular microfacet model used for the directional light: normal distribution (how many facets face the halfway vector), geometry (shadowing/masking), and Fresnel (view-dependent reflectance), combined with a diffuse term that leaves energy for specular.

**Scalar fallback.** When a map is absent, metallic and roughness come from float defaults or component overrides; albedo comes from tint (and white if no albedo map).

**Component override.** Tint always multiplies albedo at draw time. Optional metallic/roughness overrides, when present, replace imported material scalars for that draw only; when absent, imported factors are used. Textures are never rewritten.

**Ambient fill.** A cheap stand-in for missing environment light: ambient color × strength × albedo, dampened for metals so they do not receive a full chalky dielectric wash. Not a substitute for IBL.

## Patterns and principles

**One shading model.** Do not keep Phong beside PBR. Dual paths double maintenance and confuse import. Convert or approximate legacy materials into metal/rough once.

**Match the art format, not a custom packing.** Prefer glTF’s packed metallic-roughness layout so Assimp and Bistro-class assets map cleanly.

**Extend the forward path, don’t rebuild the renderer.** Keep `model → material → Graphics3D → lighting shader`. Only the material fields and the light equation change.

**Scalars for authoring, maps for fidelity.** Files supply maps; the inspector only tweaks floats. That avoids a material-asset system in v1 while still allowing scene tuning.

**Fail soft.** Missing maps or partial materials still draw. Clamp metallic/roughness to \[0, 1\]. Bad model paths keep the existing cube/fallback behavior.

**YAGNI against showcase features.** IBL, tone mapping, and deferred lighting close more of the Bistro beauty gap, but each is its own design. This feature only makes materials speak PBR under lights you already have.

## Architecture philosophy

**In-place forward PBR.** Replace Blinn-Phong inside the existing mesh lighting shader and mesh material type. No material interface hierarchy, no G-buffer, no second draw path.

**Importer owns file truth; component owns scene tweaks.** Assimp writes imported maps and factors into the cached material. The model renderer applies tint and metallic/roughness overrides per draw.

**Defaults that look “dielectric.”** Missing data should read as a rough non-metal (metallic 0, roughness around 0.5, albedo white × tint), not as a mirror or a Phong hotspot.

**Lazy senior default.** Ship metal/rough + Cook-Torrance + import + scalar overrides. Leave environment lighting and display mapping for later specs when the Bistro reference is the explicit goal again.
