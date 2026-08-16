# Material System

PBR material data for 3D meshes: texture slots, scalar factors, and alpha routing. Materials are stored inside `.mesh` files and applied at draw time during the 3D render pass.

---

## Overview

Materials sit between mesh geometry and the shading pipeline (direct lights, shadows, IBL). There is no standalone material asset — each submesh in a `.mesh` file carries its own material definition. At runtime, texture paths are resolved once per load and cached by path; each visible submesh is drawn with its maps and scalars bound individually (no 3D batching).

Per-entity tint and optional metallic/roughness overrides come from `ModelRendererComponent`, not from the mesh file alone. IBL is scene-global — see [PBR / IBL System](pbr-ibl-system.md).

---

## Material Data Model

| Category | Contents |
|----------|----------|
| **Textures** | Albedo, metallic-roughness (ORM: R=AO, G=roughness, B=metallic), normal, emissive |
| **Scalars** | Metallic (default 0), roughness (default 0.5), base-color factor, emissive factor |
| **Routing** | Alpha mode (opaque / mask / blend), alpha cutoff, double-sided flag |

All texture maps are optional. Missing slots use shared engine fallbacks: white albedo, flat normal, black emissive. Without an ORM map, metallic and roughness come from the stored scalars only.

| Sharing | Behavior |
|---------|----------|
| Material definition | Embedded per submesh in `.mesh` |
| GPU textures | Shared when multiple materials reference the same file path |
| Loaded models | Shared when entities use the same `.mesh` path |
| Per-entity overrides | `ModelRendererComponent` only — see below |

### `ModelRendererComponent`

| Field | Role |
|-------|------|
| `ModelPath` | Asset-relative `.mesh` path |
| `Color` | Tint multiplied with the material base-color factor |
| `AlbedoTexturePath` | Optional albedo PNG/JPG; when set, replaces mesh/cube/sphere albedo at draw time |
| `MetallicOverride` / `RoughnessOverride` | Optional per-entity PBR overrides (fall back to mesh values when unset) |
| `SubmeshStart` / `SubmeshCount` | Draw a slice of submeshes from a shared file (`SubmeshCount = -1` → all) |

Albedo can be overridden from the component (`AlbedoTexturePath`). Other maps (ORM, normal, emissive) stay on the mesh material.

---

## Binding Flow at Draw Time

1. **Resolve** — Load the `.mesh`, select the submesh range, merge `ModelRendererComponent` tint and overrides with stored material scalars
2. **Pass** — Opaque and mask materials in the main pass; blend materials sorted back-to-front with depth write disabled
3. **Bind** — Upload transform, lights, shadows, and IBL cubemaps; bind material textures (albedo, ORM, normal, emissive) and scalar uniforms
4. **Draw** — One indexed draw per submesh; back-face culling disabled when the material is double-sided

Texture unit layout: [Rendering Pipeline — 3D Model Rendering](rendering-pipeline.md#3d-model-rendering).

---

## Model Import and IBL

| Topic | Document |
|-------|----------|
| Assimp import, ORM packing, texture relocation, `.mesh` serialization | [3D Model Loading Pipeline](model-loading-pipeline.md) |
| HDR environment, irradiance/prefilter/BRDF, skybox | [PBR / IBL System](pbr-ibl-system.md) |

IBL textures are not part of the per-mesh material — they come from `SkyLightComponent` and apply to every PBR draw.

---

## Serialization

- **`.mesh` binary** — texture paths, scalars, and alpha fields per submesh. GPU texture handles are runtime-only.
- **Scene JSON** — `ModelRendererComponent` stores model path, optional albedo path, color, metallic/roughness overrides, and submesh range.

---

## Known Limitations

| Limitation | Detail |
|------------|--------|
| No material editor | Inspector edits `ModelRendererComponent` fields (including albedo override), not other texture slots |
| No standalone material assets | Materials live inside `.mesh` files |
| Fixed texture slots | Nine sampler units; not extensible |
| No texture streaming | Full load at model load time |
| No GPU instancing | One draw per submesh per entity |
| No material variants | Single PBR lighting path for all meshes |
| Scene cannot override textures | Re-import or edit `.mesh` to change maps |

---

## Related Documents

| Document | Relevance |
|----------|-----------|
| [Rendering Pipeline](rendering-pipeline.md) | 3D draw order, texture units, shadows |
| [3D Model Loading Pipeline](model-loading-pipeline.md) | Import and runtime load |
| [PBR / IBL System](pbr-ibl-system.md) | Environment lighting |
| [Serialization](serialization.md) | Scene JSON |
