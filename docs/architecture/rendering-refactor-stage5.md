# Rendering refactor: stage 5

Implemented 2026-11-19 on branch `perf-impr` above stages 2–4. Structural change only; `CaptureVersion=2` inputs unchanged.

## What landed

| Piece | Owner | Responsibility |
|---|---|---|
| `Engine/Renderer/Pipeline/EnvironmentLighting.cs` | Graphics3D | HDRI capture, equirect → cubemap, irradiance/prefilter bake, BRDF LUT, sky draw. `Graphics3D.SetSkybox` / `DrawSkybox` delegate to it. |
| `Engine/Renderer/Pipeline/ShadowMapStore.cs` | render host | Directional cascade atlas and per-entity point cubemaps. `SyncQuality` retires maps whose resolution/cascades changed. |
| `Engine/Scene/ShadowRenderer.cs` | SceneRenderPipeline | Directional fit + cascade depth passes, point-light dirty/cache logic, facet drawing through `MeshPassRenderer`. Only binds targets through `IGraphics3D`. |
| `IGraphics3D` | interface | New `BindShadowMapStore(ShadowMapStore?)`. `ReleaseSceneResources` now retires only the fallback store (unit recorders without a factory). |

## Wiring

- `SceneRenderPipeline` gets an optional `IFrameBufferFactory`. When present, one `ShadowMapStore` is owned by `ShadowRenderer` and bound for the whole frame (`_shadows.Bind()` … `Unbind()`), so shadow passes and the color pass sample the same maps.
- Without a factory (unit recorders), `Graphics3D` lazily builds a fallback store; behavior matches the pre-split code.
- Quality changes that happen while no store is bound (editor preferences, benchmark) are replayed by `BindShadowMapStore` → `SyncQuality`.
- Scene identity change goes through `_shadows.OnSceneChanged()` (clears maps and the point-shadow cache), replacing the old split between `ActivateScene` and `ReleaseSceneResources`.
- Factory plumbing added to `SceneSystemsFactory`, `SceneRenderSystem`, `EditorViewport`, `BenchmarkLayer`, `BenchmarkRunnerFactory`, `Benchmark3DRunner`, `BenchmarkGridRunner`, `PipelineBenchmarkRun`.

## Verification (this session, Debug)

| Suite | Result |
|---|---|
| Engine.Tests | **763 / 763** |
| Editor.Tests | **134 / 134** |
| Engine.GraphicsTests | **40 / 40** |
| Reference compare vs `HEAD` (`f4f70d56`), built in a clean worktree | **24 / 24** identical HDR + display + IDs (`--exact`) |

The first capture differed in `quality-change-2` only: the quality change happened before the store was bound, so the 2048 map was never created. Fixed with `ShadowMapStore.SyncQuality` reconciliation at bind time.

## Out of scope / deferred

- Stage 6.
- Interactive app smoke, macOS GPU.
- Untracked IslandDash/Snake assets and the concurrent model-loading changes in the working tree.
