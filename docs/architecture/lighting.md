# Lighting

Forward shading for the 3D color pass: which lights are uploaded, how PBR factors are chosen, and what the color shaders evaluate. Shadow maps are produced earlier in the frame; their fit, cache, and depth programs are in [Shadows](shadows.md). Pass order is in [Scene Rendering Pipeline](scene-rendering-pipeline.md).

2D sprites and lines do not read these lights.

---

## Design Decisions

### Direct Cook-Torrance, image-based fill when a sky is captured

`cube.frag` and `modelShader.frag` shade each fragment with a Cook-Torrance specular lobe (GGX distribution, Smith geometry, Schlick Fresnel) plus a Lambertian diffuse term. When `Graphics3D` has an irradiance cubemap and a prefilter cubemap, `u_Ibl` replaces the flat ambient term with that image-based fill. The sun and the point lights stay direct Cook-Torrance.

A device with at least 17 fragment texture units also samples the BRDF LUT on unit 15 (`USE_BRDF_LUT`, injected when the fragment shader is compiled). At 16 units that sampler is left out and `EnvBrdfApprox` supplies the scale and bias. macOS OpenGL is the 16-unit case.

| Option | Pros | Cons |
|--------|------|------|
| **IBL when the sky capture exists (chosen)** | Metals pick up the sky; flat ambient remains the fallback | Needs the irradiance and prefilter maps |
| BRDF LUT on every device | One specular path | The model shader would declare 17 samplers and fail to link on macOS |
| Blinn-Phong | Smaller shader | Does not match the metallic-roughness factors already on imported meshes |

### One ambient, one sun, eight point lights

`SceneRenderPipeline` keeps the first `AmbientLightComponent`, the first `DirectionalLightComponent`, and the first eight `PointLightComponent`s in scene view order. Later lights of the same kind are ignored. Eight matches `LightingMath.MaxPointLights` and the shader arrays.

| Option | Pros | Cons |
|--------|------|------|
| **First-wins + hard cap (chosen)** | Stable uniforms; no sort or priority scheme | A hidden light earlier in the view can steal the slot; the ninth lamp never shades |
| Distance-picked points | Nearby lamps win | Selection pops when the camera moves |
| Unlimited lights | Authoring is obvious | The forward shader loops every light on every fragment |

### Ambient is not a BRDF lobe

Flat ambient is `strength * color * albedo * ao`. With IBL on, that product is the image-based fill, still multiplied by material `ao`. Either result is then multiplied by `mix(1, ssao, u_SsaoStrength)`. The sun, the point lights, and emissive are added after that. SSAO does not shadow them.

| Option | Pros | Cons |
|--------|------|------|
| **Indirect term only (chosen)** | Creases darken without fighting the shadow maps | Needs a normal prepass before the color draw |
| Darken the whole pixel | Easier to see | Direct light goes muddy in the same creases the shadow maps already cover |

### Imported file lights are not the frame light list

Assimp point and directional lights are converted into `ImportedPointLight` / `ImportedDirectionalLight` and stored on the model graph (and in the `.mesh` sidecar, up to 256). `SceneRenderPipeline` never reads them. Spot lights are skipped at import. A scene shades only the ECS light components.

| Option | Pros | Cons |
|--------|------|------|
| **ECS components only (chosen)** | One resolution path for editor and play | Lights authored in a DCC do not illuminate until something creates components from the import |
| Auto-spawn components at draw time | File opens looking lit | Draw path would mutate the scene; duplicates on every load |

### Linear color, tonemap outside this pass

`cube.frag` and `modelShader.frag` write `ambient + sun + lamps + emissive` with the tint alpha. They do not run Reinhard. `TonemapPass` is a later fullscreen pass and is not called from `SceneRenderPipeline`.

| Option | Pros | Cons |
|--------|------|------|
| **Linear sum in the color shader (chosen)** | SSAO and bloom can read light that has not been compressed | The scene color attachment has to be float |
| Reinhard inside the fragment shader | Output matches an 8-bit target immediately | A later occlusion or bloom pass would scale an already compressed picture |

---

## Pass Placement

`SceneRenderPipeline.Render3D` resolves lights on the CPU before either shadow pass, then `IGraphics3D.BeginScene` uploads the same values to the cube and model programs. Shadow draws do not sample them. Details of when a map is produced: [Shadows](shadows.md).

**File**: `Engine/Scene/SceneRenderPipeline.cs`  
**File**: `Engine/Renderer/Pipeline/Graphics3D.cs`

---

## Light Sources and Resolution Rules

**File**: `Engine/Scene/SceneRenderPipeline.cs` (`ResolveAmbient`, `ResolveDirectional`, `ResolvePointLights`)  
**File**: `Engine/Renderer/PointLightData.cs`  
**File**: `Engine/Renderer/LightingMath.cs`

| Source | Rule | If missing or skipped |
|--------|------|------------------------|
| `AmbientLightComponent` | First in the view. RGB from `Color`, plus `Strength` | White RGB, strength `0.1` (also the `Graphics3D` field default) |
| `DirectionalLightComponent` | First in the view. Direction through `LightingMath.NormalizeDirection`. RGB × `max(Intensity, 0)` | Direction `(0, -1, 0)`, color black. Black sun adds no light and skips the directional shadow pass |
| `PointLightComponent` | Up to 8, view order, each with a `TransformComponent`. World translation, plus `Offset` when `ApplyOffset` is set. `Intensity` clamped at 0. `Range`, `CastsShadow`, and the entity id are copied through | `Range <= 0` is skipped. Past 8, the rest are dropped. `Range <= 0.1` cannot build a point-shadow projection ([Shadows](shadows.md)) |

`NormalizeDirection` replaces a direction whose length squared is below `1e-6` with `(0, -1, 0)`. The color shaders treat that vector as the direction light travels: they shade with `normalize(-u_LightDirection)`.

`Graphics3D` stores at most eight `PointLightData` values and clears the unused slots. Defaults before the first upload are ambient white at `0.1`, direction `(0, -1, 0)`, and sun color black.

### Import conversion (not used while drawing)

**File**: `Engine/Renderer/Models/ModelLightConversion.cs`  
**File**: `Engine/Renderer/Models/ImportedLight.cs`  
**File**: `Engine/Renderer/Models/AssimpModelImporter.cs`

Brightness `b` is the largest finite RGB channel. `b <= 0` drops the light.

| Kind | Color | Other fields |
|------|-------|----------------|
| Point | `diffuse / b` | Intensity `clamp(sqrt(b / 4), 0.5, 2)`. Range is metadata `PBR_LightRange` when it is finite and `> 0`; otherwise `clamp(3 * sqrt(b), 1, 16)` |
| Directional | `diffuse / b` when `b > 1`, else `diffuse` | Direction normalized, or `(0, -1, 0)` when the squared length is below `1e-8` |

The runtime-mesh sidecar stores the same records (`LightKindPoint = 1`, `LightKindDirectional = 2`, at most 256). That is import data for the model graph, not a second runtime light loop.

---

## Shader Contract

Programs: `ShaderId.Cube` (`cube.frag`) and `ShaderId.Model` (`modelShader.frag`), loaded from `Engine/assets/shaders/OpenGL/`. Both write color to attachment 0 and an entity id to attachment 1. Cube writes `u_EntityID`. Model writes the instance entity id when `u_Instanced != 0`, otherwise `u_EntityID`.

Shared lighting uniforms, set from `Graphics3D.UploadFrame`:

| Uniform | Meaning |
|---------|---------|
| `u_ViewProjection`, `u_ViewPosition` | Camera, from `SceneView` |
| `u_AmbientColor`, `u_AmbientStrength` | Resolved ambient |
| `u_LightDirection`, `u_LightColor` | Resolved sun (color already includes intensity) |
| `u_PointLightCount` | 0–8 |
| `u_PointLightPositions[i]`, `u_PointLightColors[i]`, `u_PointLightIntensities[i]`, `u_PointLightRanges[i]` | Point lights. Unused slots are still written |
| `u_PointShadowsEnabled[i]`, `u_PointShadowMaps[i]` | Per-light cubemap enable. See [Shadows](shadows.md) |
| `u_LightViewProjection`, `u_ShadowsEnabled`, `u_ShadowMap` | Directional map |
| `u_Ssao`, `u_SsaoStrength` | Blurred occlusion. Strength 0 leaves indirect light unchanged |

Per-draw surface uniforms:

| Uniform | Cube | Model |
|---------|------|-------|
| `u_Color` | Tint | Tint (one value per batch; the batch key includes it) |
| `u_Metallic`, `u_Roughness`, `u_Ao` | Factors | Factors, then multiplied by maps |
| `u_TilingFactor`, `u_UseTexture`, `u_Texture` | Optional albedo | — |
| `u_BaseColor` | — | `Mesh.BaseColorFactor` (default white) |
| `u_HasDiffuseMap`, `u_HasMetallicRoughnessMap`, `u_HasNormalMap`, `u_HasOcclusionMap` | — | 0/1 |
| `u_AlphaTest`, `u_AlphaCutoff` | — | Cutout discard when the mesh has a diffuse map and `AlphaCutout` |

### Sampler units

Bound by `Graphics3D`. Point-shadow cubes occupy units 4–11 (eight lights). Do not put another sampler on those units.

| Unit | Cube | Model |
|------|------|-------|
| 0 | Albedo when `u_UseTexture` is set | Diffuse, or the white texture |
| 1 | — | Metallic-roughness, or white |
| 2 | — | Normal, or the flat normal |
| 3 | Directional shadow (`u_ShadowMap`) | Directional shadow |
| 4–11 | `u_PointShadowMaps[0..7]` | Same |
| 12 | — | Occlusion, or white |
| 13 | Irradiance cubemap when `u_Ibl` is set | Same |
| 14 | Prefilter cubemap when `u_Ibl` is set | Same |
| 15 | BRDF LUT when the device has 17 fragment units; otherwise the SSAO map | Same |
| 16 | SSAO map when the device has 17 fragment units | Same |

Model map channels: roughness is green, metallic is blue, occlusion is red. A missing map is treated as 1 before multiplying by the scalar factor, so the factor alone still applies. The normal map is tangent-space; the vertex shader builds TBN from the tangent and the normal.

### Evaluation

Constants in both color shaders: minimum roughness `0.045`, dielectric F0 `0.04`, specular epsilon `0.0001`.

1. Albedo is the texture (or white) times tint. Model also multiplies `u_BaseColor`.
2. Roughness is at least `0.045`. Model multiplies the green map channel first.
3. **Sun.** Cook-Torrance with radiance `u_LightColor`, times the directional shadow factor. A black sun contributes nothing.
4. **Point lights.** Skip when distance ≥ range. Radiance is `color * intensity * (1 - distance / range)²`. Inside `0.0001` units the contribution is `radiance * albedo` with no BRDF and no shadow. Otherwise Cook-Torrance times the point-shadow factor.
5. **Indirect.** Flat ambient, or image-based light when `u_Ibl` is set. Material AO multiplies that term. `mix(1, u_Ssao, u_SsaoStrength)` multiplies it again. Neither factor scales the sun or the lamps.
6. **Output.** `ambient + sun + lamps + emissive`. Alpha is `u_Color.a`.

Alpha-cutout meshes discard in the model (and depth) fragment shaders when diffuse alpha is below `Mesh.AlphaCutoff` (default `0.5`). That is not a sorted transparency pass. Cube draws have no alpha test.

### Screen-space ambient occlusion

**File**: `Engine/Renderer/Pipeline/SsaoPass.cs`  
**File**: `Engine/Renderer/LightingMath.cs` (`SsaoKernelSize` 64, `SsaoBias` 0.025)

The pass runs only when `SceneView.Ssao` is set, `SsaoRadius` is finite and `> 0`, `SsaoStrength` is finite and clamps above 0, and `TargetWidth` / `TargetHeight` are non-zero. `SsaoPass.TryOcclude` draws the opaque set into its own normal-and-depth target (`ViewNormal` / `ViewNormalModel`), writes an `RGBA8` occlusion map, then blurs it 4×4. The kernel is 64 samples, built once. A 4×4 noise texture is uploaded once through `CreateFromRgba`.

If the pass cannot be built, the projection does not invert, or a resize fails, the color pass still runs. It samples the white texture at strength 0. Fewer than 16 fragment texture units leaves the pass unavailable.

The normal prepass does not apply the shadow caster-distance cut.

---

## PBR Factor Resolution

**File**: `Engine/Scene/SceneRenderPipeline.cs` (`ResolvePbr`, `AdoptSubmeshFactors`)  
**File**: `Engine/Renderer/Meshes/Mesh.cs`

`ResolvePbr` clamps `ModelRendererComponent.Metallic`, `Roughness`, and `Ao` to 0–1. A non-finite value becomes 0.

Imported submeshes store `MetallicFactor` (default 0) and `RoughnessFactor` (default 0.5). Those mesh values are **not** read on every draw. They are copied onto the component once, and only when all of this is true:

- `MeshIndex` is set (an unpacked child, not the catch-all multi-submesh draw)
- `FactorsSeeded` is still false
- the component is still at the defaults: metallic `0`, roughness `0.5`, ao `1`

The copy writes metallic and roughness. It does not write AO. If any of the three component fields already differs from those defaults, the component values are kept. Unit cubes never adopt mesh factors; they use the component only.

After that, the model shader multiplies maps as in the [shader contract](#shader-contract). A component metallic of `1` with a metallic-roughness map means “use the map’s blue channel,” not “force fully metallic,” because the shader multiplies.

---

## Related Docs

- [Scene Rendering Pipeline](scene-rendering-pipeline.md)
- [Shadows](shadows.md)
- [Cameras and Rendering](../guide/concepts/cameras-and-rendering.md)
