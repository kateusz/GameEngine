# Rendering refactor: stages 2, 3 and 4

Implemented 2026-11-19 on branch `perf-impr` above stages 0/1 (`fd28c259`). Structural change only; `CaptureVersion=2` inputs unchanged. No commit of stages 2–4 in this write-up.

## What landed

| Stage | Outcome |
|---|---|
| **2** | `SceneRenderPipeline` is a public `IDisposable` with graphics/factories in the constructor. Per-host `SceneRenderState` owns `StaticMeshCache` and `PointShadowCache`. `SceneContext.SetScene` no longer resets renderer caches. `IGraphics3D.ActivateScene` / `ReleaseSceneResources` retire GPU shadow backing; Application disposes the active scene before Graphics. Runtime/editor/benchmark each `new` their own pipeline and Dispose it. |
| **3** | Persistent merge state lives in `StaticMeshCache` (history, build/upload, eviction, Dispose). API: `FindMatching` / `GetOrBuild` / `Touch` / `EndPass` / `AbortFrame`; dictionaries are private. `MeshPassRenderer` batches, LODs, culls, draws and replays merges from `RenderSceneData` without ECS or asset factories. |
| **4** | `SceneRenderDataBuilder` resolves models once per frame into `RenderSceneData`. Geometry pass does not read components/paths. Render uses pure `ResolveEffectivePbr` (legacy unpacked children stay compatible without mutating the component). Spawn-time seeding is `ImportedMaterialDefaults` in `ModelHierarchySpawner`. Stats: `PrepareCpuMs`, `SceneCpuMs`, `SceneAllocatedBytes` cover builder + execution. |

## Verification (this session)

| Suite | Result |
|---|---|
| Engine.Tests | **761 / 761** |
| Editor.Tests | **134 / 134** |
| Engine.GraphicsTests (Debug) | **40 / 40** |
| Reference compare vs `rendering-stage1-v2-debug` | **24 / 24** identical HDR + display + IDs (`--exact`) |

Headless unit/GPU runs are not a performance claim. Prepare/scene timings are recorded for later comparison; do not declare a speedup from recorder-only renders.

## Acceptance covered in tests

- Multi-frame merge/shadow tests reuse one recorder via `ClearFrame` (same pipeline host).
- Failed merge upload uses a throwing VAO factory on the same host from frame 1.
- Unpacked submesh draw uses mesh factors; component stays unseeded/default (no serialize mutation).
- `ResolveEffectivePbr` independent of later component edits; A/B pipelines do not share merge history; Dispose is idempotent; existing shadow test covers caster outside camera.

## Out of scope / deferred

- Stage 5 (ShadowRenderer / EnvironmentLighting extraction).
- Interactive app smoke, macOS GPU.
- Commit/push of stages 2–4 (wait for explicit request).
- Untracked IslandDash/Snake assets and the conditional `Engine.Tests` TFM remain as found.
- Concurrent `TextureFactory` / BC background-cook work in the tree is unrelated; left alone.
