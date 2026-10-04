# Shadows

Depth maps for the forward color pass: one directional shadow map fitted to the camera, and up to eight point-light cubemaps with a CPU cache. Lighting equations and PBR factors are in [Lighting](lighting.md). Where these passes sit in the frame is in [Scene Rendering Pipeline](scene-rendering-pipeline.md).

---

## Design Decisions

### One fitted directional map, not cascades

`LightingMath.TryFitDirectionalShadow` builds one orthographic light matrix around the camera frustum, then clamps that fit to `ShadowDistance` (50). The map is 1024×1024. Casters farther than `SceneView.DirectionalShadowCasterMaxDistance` (default 50; `0` disables the cut) are skipped.

The 50-unit cap is deliberate. The editor far plane is 1000. Fitting that whole range makes one texel about one world unit, and the `0.002` window bias then erases contact shadows. Cascades are the upgrade path when a single 50-unit map is not enough.

| Option | Pros | Cons |
|--------|------|------|
| **Single map, 50-unit fit (chosen)** | Stable texels; contact shadows survive the bias | Casters and receivers beyond the fit are missing or unshadowed |
| Fit the whole camera far plane | One map covers the view | The bias becomes many world units; shadows shimmer and disappear |
| Cascaded shadow maps | Large views stay sharp | Several depth passes and a cascade lookup in the color shader |

### Point cubemaps are cached, and only near the camera

Each casting point light owns one depth cubemap, keyed by entity id, at 512×512 per face. `SceneRenderPipeline` redraws a light only when it is within `PointShadowDistance` (20) of the camera **and** the cache says the lamp or a nearby caster changed. Farther lights are not redrawn and are not sampled that frame (they still shade, unshadowed).

| Option | Pros | Cons |
|--------|------|------|
| **Cache + 20-unit redraw radius (chosen)** | Static lamps do not re-render six faces every frame | A light just outside 20 drops its shadow abruptly; moving casters inside the radius still cost six draws |
| Redraw every casting light every frame | No cache bugs | Six full depth passes per lamp per frame |
| Shadow every lamp regardless of distance | Distant occlusion stays correct | Cubemap fill grows with every lamp in the scene |

### Window Z for the sun, linear distance for lamps

The directional depth program leaves window depth in the map. The point-depth program writes `distance / range` into `gl_FragDepth`. The color shader compares in the same space it stored. Mixing them would make the `0.002` and `0.05` biases meaningless.

| Option | Pros | Cons |
|--------|------|------|
| **Match compare space to the pass (chosen)** | Bias constants stay in the units the map stores | Two depth programs and two compare functions |
| Linear depth for both | One compare | Directional fit would have to store a range, and texel snap is defined in window Z today |

### Point faces draw back faces

`BeginPointShadowFace` culls front faces so the cubemap records the inside of casters, which reduces self-shadow acne on the lamp. A double-sided mesh turns culling off for that draw. The directional pass does not flip the cull.

---

## Pass Ordering

Inside `Render3D`, after lights are resolved:

1. Clear the directional shadow bind (`SetDirectionalShadow(identity, false)`).
2. If `SceneView.DirectionalShadows`, the resolved sun color is not black, and the fit succeeds: depth pass, then bind the map.
3. If `SceneView.PointShadows`: for each uploaded light with `CastsShadow`, reuse, redraw six faces, or skip. If the flag is off, the cache is marked stale and this step is skipped.
4. Color pass. `BeginScene` uploads `u_ShadowsEnabled` and binds whatever maps the earlier steps left enabled.

A fit failure logs once and the color pass runs without a directional map. A cubemap create failure logs once and that light stays unshadowed. `TryBuildPointShadowFaces` failing (`Range` ≤ `PointShadowNear`, which is `0.1`) skips the light with no log.

Both shadow passes and the color pass share `DrawOpaque3D` (visibility, frustum, zones). The caster-distance cut applies to directional and point shadow draws, not to the color pass. Point-shadow faces also skip casters whose bounds contain the lamp.

**File**: `Engine/Scene/SceneRenderPipeline.cs`  
**File**: `Engine/Renderer/Pipeline/Graphics3D.cs`  
**File**: `Engine/Renderer/LightingMath.cs`

---

## Directional Shadows

### When the pass runs

All three: `SceneView.DirectionalShadows` (default true), resolved sun color ≠ black, `TryFitDirectionalShadow` returns true. The resolved color is RGB × `max(Intensity, 0)` ([Lighting](lighting.md)).

### Fit

1. Unproject the eight corners of the camera clip volume. Failure returns false.
2. If the near-to-far distance is greater than 50, pull the far four corners in so the fit depth is 50.
3. Look from the origin along the normalized light direction. The up axis is `+Z` when the light is nearly parallel to Y (`|dot| > 0.99`), otherwise `+Y`.
4. Bound those corners in light space. A degenerate XY extent returns false.
5. Snap the XY center onto the 1024-texel grid so the map does not shimmer as the camera moves.
6. Build an orthographic matrix over that light-space box. The color shader compares window Z against this matrix.

### Map

Created on first use and reused: 1024×1024, `FrameBufferTextureFormat.DepthComponent`, nearest filter, clamp-to-border wrap. `BeginShadowPass` binds it, clears it, and draws with `ShaderId.Depth`.

### Who is drawn

Same opaque cubes and meshes as the color pass, culled against the **light** view-projection, then dropped when the closest point of the world AABB is farther than `DirectionalShadowCasterMaxDistance` from the camera (default 50). Batches of two or more instances use the instanced depth path. `pointDepth` is not used here.

### Color-shader sample

`u_ShadowMap` is texture unit 3. `u_ShadowsEnabled == 0` returns fully lit. Otherwise the fragment projects world position by `u_LightViewProjection`, converts to a `[0,1]` UV and depth, and runs a 2×2 bilinear PCF. A sample is lit when `current - 0.002` is not greater than the stored depth. The shader does not reject UVs outside the map before sampling.

---

## Point Shadows and Cache

### Faces

`TryBuildPointShadowFaces` needs a destination of six matrices and `range > 0.1`. Each face is a 90° perspective (near `0.1`, far = the light’s range) looking from the lamp along `+X, -X, +Y, -Y, +Z, -Z`. Up vectors are `-Y, -Y, +Z, -Z, -Y, -Y`.

`BeginPointShadowFace` binds that entity’s cubemap, selects the face, clears, culls front faces, and sets `u_LightPosition` / `u_LightRange` on `ShaderId.PointDepth`. The fragment shader writes `length(worldPos - light) / range` into `gl_FragDepth`, after the same alpha-cutout discard as the directional depth program.

Each face draw skips casters whose AABB contains the lamp (a bulb mesh would fill every face) and applies the same camera-distance caster cut as the directional pass. Instances are **not** batched: each instance is its own depth draw. `pointDepth.vert` has no instance attributes.

### Cache

The cache stores last-frame caster poses and lamp position/range. `NeedsRedraw` is true when any of these hold:

- the cache was marked stale (`PointShadows` was off, or it has never been filled)
- this lamp is already marked dirty
- the lamp was not stored, or its position or range changed
- a caster was added, removed, or moved, and either pose has no bounds or the light’s sphere hits the old or new bounds

A new or removed caster that has bounds and sits outside the lamp sphere does not dirty that lamp.

Redraw policy for a light that has `CastsShadow` and was actually uploaded:

| State | Result |
|-------|--------|
| Clean, within 20, cubemap already exists | `UseCachedPointShadow` binds it. No geometry pass |
| Clean, within 20, no cubemap yet | Treated as dirty and redrawn |
| Dirty, within 20, face setup succeeds | Six face draws, then marked clean |
| Farther than 20 | No draw and no bind. A dirty lamp stays dirty. A clean lamp is counted as a cache hit and still does not sample |
| `PointShadows` false | Cache marked stale. Next enabled frame redraws |

Cubemaps live on `Graphics3D` until it is disposed, one per entity id, 512×512 `DepthCubemap`. A failed create disables that light’s shadow.

### Color-shader sample

Units 4–11, `u_PointShadowMaps[i]`, gated by `u_PointShadowsEnabled[i]`. The compare value is `distance / range`. Bias is `0.05 / range`. Nine taps in a tangent disk (3×3, step `distance * π / 512`) are averaged. Disabled or out-of-range lights do not take this path; out-of-range lights are skipped before shading ([Lighting](lighting.md)).

---

## Shader Contract

| Program | File | Writes | Reads |
|---------|------|--------|-------|
| `ShaderId.Depth` | `Engine/assets/shaders/OpenGL/depth.frag` | Window depth | `u_ViewProjection`, `u_Model` or instance matrix, `u_DiffuseMap` (unit 0) for cutout |
| `ShaderId.PointDepth` | `Engine/assets/shaders/OpenGL/pointDepth.frag` | `gl_FragDepth = distance / range` | Those, plus `u_LightPosition`, `u_LightRange` |
| `ShaderId.Cube`, `ShaderId.Model` | color shaders | Shadow factors into the sun and point terms | Directional unit 3, point units 4–11 |

Directional depth supports `u_Instanced`. Point depth does not. Neither depth program evaluates lights.

Color-side constants: directional bias `0.002` (window Z), point bias `0.05` (world units, divided by range). Map sizes: `LightingMath.ShadowMapResolution` 1024, `PointShadowFaceResolution` 512.

---

## Related Docs

- [Scene Rendering Pipeline](scene-rendering-pipeline.md)
- [Lighting](lighting.md)
