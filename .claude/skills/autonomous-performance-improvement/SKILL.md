---
name: autonomous-performance-improvement
description: >-
  Autonomously run A/B performance experiments on this game engine: build the
  Benchmark CLI, warm-up and measure scenes, collect 2D/3D metrics, implement one
  optimization, compare results, write artifacts/performance report, and apply
  KEEP/REVERT/INCONCLUSIVE policy without manual editor interaction. Use when the
  user asks for performance optimization with evidence, perf-run, or A/B benchmarks.
---

# Autonomous performance improvement

End-to-end workflow for this repository. **Do not** ask the user to open the editor, press Play, or click benchmarks.

## Default 3D project

Unless the user names another game folder or scene, **3D project-scene benchmarks** use the editor layout project at:

`Editor/bin/Debug/net10.0/3d` (repo-relative; on this machine: `/Users/mateuszkulesza/projects/GameEngine/Editor/bin/Debug/net10.0/3d`).

Configured in `tools/performance/perf-scenes.json` → `defaults.project3dRoot` and `defaults.scene3d` (default scene: `assets/scenes/sponza.scene`). Preset **`3d-loaded-scene`** (`3D:GlbModels`) uses that project. `perf-improve.sh` defaults to **`3d-loaded-scene`**. For `3D:GlbModels`, `perf-run --scene 3D:GlbModels` without `--project-root` picks the same defaults.

Other scenes under that project: `assets/scenes/castle.scene`, `bistro.scene`, etc. — pass `--scene-file` with an absolute path or override `--preset` / preset fields in JSON.

## Repository map

| Piece | Location |
|-------|----------|
| Interactive benchmark app | `Benchmark/` (ImGui UI) |
| **Perf 2D/3D (sceny GL)** | `tools/performance/perf 2d\|3d` |
| **Pojedynczy pomiar** | `perf-run --preset …` (`perf-scenes.json`) |
| **Poprawność dotnet** | `perf-tests 2d\|3d` |
| Render metrics (3D) | `Engine/Renderer/Statistics.cs`, filled by `Graphics3D` + `SceneRenderPipeline` |
| Render metrics (2D) | `Engine/Renderer/Graphics2DStats.cs`, `Graphics2D` |
| Sceny perf | `tools/performance/perf-scenes.json` |
| A/B orchestrator | `tools/performance/perf-improve.sh` |
| Analysis + report | `tools/performance/analyze_ab.py` (`--report` for `report.md`) |
| Branch A/B tables | `tools/performance/perf_report.py` (`comparison.json` + `report.md`, **all** JSON metrics) |
| Artifacts | `artifacts/performance/<run-id>/` |
| Metric reference | [reference-metrics.md](reference-metrics.md) |
| Correctness filters (2D / 3D) | `tools/performance/test-suites.json` |

## One-shot commands

**Single measurement (5s warm-up + 5s measure, 1 repetition by default):**

```bash
tools/performance/perf-run \
  --preset 3d-loaded-scene \
  --run-dir artifacts/performance/manual/before
```

**Full A/B with policy (agent):**

```bash
tools/performance/perf-improve.sh \
  --preset 3d-loaded-scene \
  --focus "describe bottleneck" \
  --optimization-path Engine/Scene/SceneRenderPipeline.cs
```

**Wszystkie sceny benchmarkowe dla wymiaru:**

```bash
tools/performance/perf 2d
tools/performance/perf 3d
tools/performance/perf 3d --output-dir artifacts/performance/manual/3d-suite --with-tests
```

Pass every file that belongs to the **single** optimization via repeated `--optimization-path`. Infrastructure under `tools/performance/` and this skill must **not** be reverted.

## Agent checklist (strict order)

1. **Recon** — Read `reference-metrics.md`, grep the focus area, pick a scenario (default real scene: **`3d-loaded-scene`** / `3D:GlbModels` on the default 3D project; synthetic: `3D:InstancedMesh`, `2D:Profiling2D`, `Shadows:directional`).
2. **Hypothesis** — One bottleneck, one primary metric (default: `avgFps`), guard metrics `p99FrameTimeMs` and renderer-specific CPU ms from `customMetrics`.
3. **Baseline A** — `perf-improve.sh` or `perf-run` with optimization files **stashed** for variant A (script stashes `--optimization-path` or full dirty tree when `--optimization-path` is set / tree is dirty).
4. **Change** — One cohesive optimization only; no quality cuts; no unrelated refactors.
5. **Variant B** — Same scenario, same repetitions, same build configuration (`Release`).
6. **Correctness** — `tools/performance/perf-tests 2d` or `3d` (wymiar z presetu w A/B). Filtry w `test-suites.json`. Failed tests → prefer REVERT.
7. **Analyze** — `analyze_ab.py` thresholds: +3% primary FPS, p99 regression ≤ +2% frame time (pass `--report` for markdown).
8. **Report** — `artifacts/performance/<run-id>/report.md` must list **every numeric metric** from each `result.json`: all top-level scalars (FPS, frame times, CPU %, memory MB, `frameCount`, …) **plus every key** under `customMetrics` (same names as [reference-metrics.md](reference-metrics.md)). Use `analyze_ab.py --report` (before/after) or `perf_report.py RUN_DIR` (main vs feature folder). Never summarize to FPS-only if the JSON contains more.
9. **Policy** — `KEEP` keep code; `REVERT` / `INCONCLUSIVE` revert only `--optimization-path` files; `BLOCKED` revert unverified optimization, document cause.

Never: `git reset --hard`, force push, commit/push unless user asks.

## CLI contract (`--perf-run`)

Wrapper: `perf-run --preset NAME --output PATH` (preset z `perf-scenes.json`; projektowe sceny mają `projectRoot` / `sceneFile` w JSON).

Exit codes: `0` ok, `1` args, `2` benchmark/crash, `3` timeout.

Outputs: `result.json` (schema `gameengine-perf/1`) + `result.frames.csv`.

## macOS / CI

Graphics benchmarks need a working GL context (not headless on macOS CI). If run fails with context/window errors → **BLOCKED**, say so in `report.md`. Do not invent numbers.

## When no good candidate

Run baseline only, document in report that no optimization met cost/risk bar — do not land speculative changes.
