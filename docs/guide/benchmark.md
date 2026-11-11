# Benchmark harness

Standalone app for timing rendering: four categories (2D, 3D, lighting, shadows), session results, optional baseline compare, and Markdown export. Design notes live under [Benchmark categories](../specs/benchmark-categories/introduction.md).

## Run the interactive app

```bash
cd Benchmark && dotnet run
```

The window opens on a category picker. Each category lists tests; **Start** builds a scene and runs the clock for the duration slider. **Back** aborts a run without recording it. Camera in 3D pipeline tests: LMB orbit, MMB pan, scroll zoom.

## 3D — open a project scene (like the editor)

Use this to benchmark a real `.scene` from your game project, not only synthetic cubes.

1. Choose **3D**.
2. **Project folder** — path to the game project root (the folder that contains `assets/`), same as **Project → Open…** in the editor. On Windows use **Browse…** for the native folder picker, then **Open project** (fills the scene list only — nothing loads yet).
3. **Scene** — combo lists every `*.scene` under `assets/scenes/` (recursive). Pick one; the scene **renders in the viewport** (live preview, no timer).
4. Run **Project scene** to measure that file. Result name: `3D_GlbModels` (both directional and point shadows on in the view, matching imported content).

If the scene has a **Primary** camera, the benchmark uses its transform for the initial eye position; otherwise the default orbit camera applies.

**macOS / Linux:** no native folder picker — paste the project path, then **Open project**.

**Assets:** opening the project sets `IProjectContext` so mesh and texture paths resolve the same way as in the editor. Scripts are not started; the timed path uses `SceneRenderPipeline` only.

## CLI and automation

`Benchmark --perf-run` opens a short GL window, measures, writes JSON, then exits. Wrapper: `tools/performance/perf-run --scene 3D:InstancedMesh --output …`.

Domyślnie (`perf-run`, `perf-improve.sh`): **0 s** warm-up (**10 s** dla `3D:GlbModels` / preset `3d-loaded-scene`), **5 s** pomiar, **1** powtórzenie. Nadpisz `--warmup-seconds`, `--measure-seconds`, `--repetitions`.

### Project scene from the CLI

For `3D:GlbModels`, pass the project and scene file:

```bash
tools/performance/perf-run \
  --scene "3D:GlbModels" \
  --project-root "/path/to/game-project" \
  --scene-file "/path/to/game-project/assets/scenes/level.scene" \
  --output /tmp/level.json
```

Or directly:

```bash
dotnet exec Benchmark/bin/Release/net10.0/Benchmark.dll --perf-run \
  --category 3D --test GlbModels \
  --project-root "/path/to/game-project" \
  --scene-file "/path/to/game-project/assets/scenes/level.scene" \
  --output /tmp/level.json
```

If only `--scene-file` is given, the harness walks parents until it finds a directory containing `assets/` and calls `Apply` on that root.

Outputs: metrics JSON (`gameengine-perf/1`) and a sibling `.frames.csv` per run.

### Sceny performance (CLI)

Presety: `tools/performance/perf-scenes.json`. **Cały wymiar:** `tools/performance/perf 2d` / `perf 3d`.  
Pojedynczy: `perf-run --preset 2d-batching --output /tmp/2d.json`.  
Poprawność: `perf-tests 2d` / `3d`.

## Category summary

| Category | What it measures |
|----------|------------------|
| **2D** | Sprite batching, textures, physics/runtime path (unchanged legacy tests) |
| **3D** | Geometry cost (cubes, instancing, unique meshes) with shadows off, or **Project scene** from your project |
| **Lighting** | District scene, lights on, shadows off |
| **Shadows** | Same district, shadow passes on |

See [Developer guide](../specs/benchmark-categories/developer-guide.md) for staging rules and result names.
