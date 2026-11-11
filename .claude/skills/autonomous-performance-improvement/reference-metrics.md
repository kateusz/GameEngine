# Performance metrics available in benchmarks

## Frame timing (all scenarios)

Collected in `Benchmark/Cli` during the **measure** window only (after warm-up).

| Field | Unit | Source | Better |
|-------|------|--------|--------|
| `frameCount` | frames | counted ticks | higher (more samples in fixed wall time) |
| `measuredWallSeconds` | s | wall clock in measure phase | — |
| `avgFps` | FPS | `1000 / mean(frameTimeMs)` | higher |
| `avgFrameTimeMs` | ms | mean CPU frame time (update+render) | lower |
| `medianFrameTimeMs` | ms | p50 of frame times | lower |
| `p95FrameTimeMs` | ms | 95th percentile | lower |
| `p99FrameTimeMs` | ms | 99th percentile | lower |
| `minFrameTimeMs` / `maxFrameTimeMs` | ms | per-repetition extrema | — |

Process metrics (approximate, not GPU-isolated):

| Field | Unit | Source | Better |
|-------|------|--------|--------|
| `AverageCpuUsage` (in JSON via custom path) | % | `Process.TotalProcessorTime` delta | lower |
| Working set in `BenchmarkResult` | MiB | `Process.WorkingSet64` | context-dependent |

Raw per-frame samples: `*.frames.csv` (`frame_index,frame_time_ms`).

## 3D renderer (`rendererMode: "3d"`)

Per-frame samples aggregated into `customMetrics` on each repetition (mean unless noted).

| Custom metric | Maps from `Statistics` | Better |
|---------------|------------------------|--------|
| Avg Color Draw Calls | `ColorDrawCalls` | lower (if same scene) |
| Avg Total Draw Calls | `DrawCalls` (shadows+color) | lower |
| Avg Dir Shadow Draw Calls | `DirectionalShadowDrawCalls` | lower |
| Avg Point Shadow Draw Calls | `PointShadowDrawCalls` | lower |
| Avg Mesh Draws | `MeshDraws` | lower |
| Avg Instanced Draws | `InstancedDraws` | lower |
| Avg Instances | `Instances` | informational |
| Avg Triangles | `Triangles` | informational |
| Avg Frustum Culled | `FrustumCulled` | higher when culling helps |
| Avg Zone Culled | `ZoneCulled` | higher when zones used |
| Avg Color CPU Ms | `ColorCpuMs` from `DrawOpaque3D` | lower |
| Avg Shadow CPU Ms | `ShadowCpuMs` | lower |
| Max Color CPU Ms | max `ColorCpuMs` | lower |

`PipelineBenchmarkRun.Contribute` also adds per-frame snapshot strings: Color draw calls, Instanced draws, Instances, Point lights, Directional shadow on/off.

**Not available in automated perf JSON today:** per-pass GPU timer (OpenGL timer queries are DEBUG-only in 2D; 3D has no stable GPU frame split in CLI). Mark **N/A** in reports.

## 2D renderer (`rendererMode: "2d"`)

| Custom metric | Maps from `Graphics2DStats` | Better |
|---------------|----------------------------|--------|
| Avg Quad Draw Calls | `DrawCalls` | lower |
| Avg Line Draw Calls | `LineDrawCalls` | lower |
| Avg Quads | `QuadCount` | informational |
| Avg Batch Count | `BatchCount` | lower |
| Avg Texture Binds | `TextureBinds` | lower |
| Avg Program Switches | `ProgramSwitches` | lower |
| Avg Upload KB | `UploadBytes` | lower |
| Avg BatchFill Ms | `BatchFillMs` CPU | lower |
| Avg Flush Ms | `FlushMs` CPU | lower |
| Max Flush Ms | max flush | lower |
| Avg GPU Quad Ms | `GpuQuadPassMs` (prev frame, DEBUG builds) | lower |
| Avg GPU Line Ms | `GpuLinePassMs` (DEBUG only) | lower |

Release CLI runs typically show `0` for GPU quad/line ms — document as N/A unless building Benchmark `-c Debug`.

## Primary metric selection

| Optimization area | Primary | Guards |
|-------------------|---------|--------|
| Opaque / instancing / sort | `avgFps` or `Avg Color CPU Ms` | `p99FrameTimeMs`, draw calls |
| 2D batching | `avgFps` | `Avg Flush Ms`, `Avg Texture Binds` |
| Shadows | `avgFps` or `Avg Shadow CPU Ms` | `Avg Total Draw Calls`, `p99` |

## Acceptance policy (default)

- Primary improvement ≥ **3%** FPS (or equivalent CPU ms drop).
- Effect greater than ~half of FPS stdev across repetitions.
- p99 frame time regression ≤ **+2%**.
- Required tests pass.
- Else **INCONCLUSIVE** → revert optimization paths only.
