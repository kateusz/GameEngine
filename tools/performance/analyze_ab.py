#!/usr/bin/env python3
"""Aggregate perf-run JSON repetitions, compare A vs B, optionally write report.md."""
from __future__ import annotations

import argparse
import json
import math
import platform
import statistics
import subprocess
import sys
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

_PERF_DIR = Path(__file__).resolve().parent
if str(_PERF_DIR) not in sys.path:
    sys.path.insert(0, str(_PERF_DIR))
import perf_report  # noqa: E402

PRIMARY = "avgFps"
GUARD_P99 = "p99FrameTimeMs"


def _normalize_run(data: dict[str, Any]) -> dict[str, Any]:
    out: dict[str, Any] = {}
    for k, v in data.items():
        out[k[0].lower() + k[1:] if k else k] = v
    if "success" not in out and "Success" in data:
        out["success"] = data["Success"]
    return out


def load_repetitions(dir_path: Path) -> list[dict[str, Any]]:
    files = sorted(dir_path.glob("repetition-*.json"))
    if not files:
        files = sorted(dir_path.glob("*.json"))
    runs = []
    for f in files:
        data = _normalize_run(json.loads(f.read_text()))
        if data.get("success"):
            runs.append(data)
    return runs


def aggregate(runs: list[dict[str, Any]], key: str) -> dict[str, float]:
    vals = [float(r[key]) for r in runs if key in r and r[key] is not None]
    if not vals:
        return {"mean": float("nan"), "stdev": float("nan"), "n": 0}
    stdev = statistics.stdev(vals) if len(vals) > 1 else 0.0
    return {
        "mean": statistics.mean(vals),
        "stdev": stdev,
        "n": len(vals),
        "min": min(vals),
        "max": max(vals),
    }


def delta_pct(before: float, after: float) -> float | None:
    if before == 0 or math.isnan(before) or math.isnan(after):
        return None
    return (after - before) / before * 100.0


def decide(
    before: dict[str, dict[str, float]],
    after: dict[str, dict[str, float]],
    primary_improve_pct: float,
    p99_regress_pct: float,
) -> str:
    b_fps = before.get(PRIMARY, {}).get("mean", float("nan"))
    a_fps = after.get(PRIMARY, {}).get("mean", float("nan"))
    dp = delta_pct(b_fps, a_fps)
    if dp is None or math.isnan(dp):
        return "INCONCLUSIVE"

    b_p99 = before.get(GUARD_P99, {}).get("mean", float("nan"))
    a_p99 = after.get(GUARD_P99, {}).get("mean", float("nan"))
    p99_dp = delta_pct(b_p99, a_p99)

    noise = before.get(PRIMARY, {}).get("stdev", 0.0) / b_fps * 100 if b_fps else 0.0
    if dp >= max(primary_improve_pct, noise * 0.5) and (p99_dp is None or p99_dp <= p99_regress_pct):
        return "KEEP"
    if dp < 0 and abs(dp) > primary_improve_pct:
        return "REVERT"
    return "INCONCLUSIVE"


def git(cmd: list[str]) -> str:
    try:
        return subprocess.check_output(["git"] + cmd, text=True).strip()
    except Exception:
        return "N/A"


def metric_row(name: str, before: dict, after: dict, deltas: dict) -> str:
    b = before.get("mean", float("nan"))
    a = after.get("mean", float("nan"))
    d = deltas.get(name)
    d_abs = a - b if not (isinstance(b, float) and b != b) else float("nan")
    d_s = "N/A" if d is None else f"{d:+.2f}%"
    better = ""
    if name == "avgFps" and d is not None:
        better = "better" if d > 0 else "worse" if d < 0 else "flat"
    if name in ("avgFrameTimeMs", "p95FrameTimeMs", "p99FrameTimeMs") and d is not None:
        better = "better" if d < 0 else "worse" if d > 0 else "flat"
    return f"| {name} | {b:.3f} | {a:.3f} | {d_abs:+.3f} | {d_s} | {better} |"


def write_report(
    analysis: dict[str, Any],
    scenario_path: Path,
    artifact_dir: Path,
    output: Path,
    hypothesis: str,
    tests_log: Path | None,
) -> None:
    scenario = json.loads(scenario_path.read_text())
    verdict = analysis.get("verdict", "INCONCLUSIVE")
    before = analysis.get("before", {})
    after = analysis.get("after", {})
    deltas = analysis.get("deltas_pct", {})

    tests_section = "Not run"
    if tests_log and tests_log.exists():
        tests_section = tests_log.read_text()[-4000:]

    lines = [
        f"# Performance run: {scenario.get('category')}:{scenario.get('testId')}",
        "",
        "## Werdykt",
        verdict,
        "",
        f"Rekomendacja: {verdict} — see policy thresholds in analysis.json.",
        "",
        "## Cel i hipoteza",
        hypothesis or "_Agent fills based on code review._",
        "",
        "## Środowisko",
        f"- Commit: `{git(['rev-parse', 'HEAD'])}`",
        f"- OS: {platform.platform()}",
        f"- CPU cores: {platform.processor() or 'unknown'}",
        f"- Python: {platform.python_version()}",
        "",
        "## Konfiguracja benchmarku",
        f"- Scene: `{scenario.get('category')}:{scenario.get('testId')}`",
        f"- Count: {scenario.get('count')}",
        f"- Resolution: {scenario.get('width')}x{scenario.get('height')}",
        f"- VSync: off (targetFrameRate={scenario.get('targetFrameRate', 0)})",
        f"- Warm-up: {scenario.get('warmupSeconds')}s, measure: {scenario.get('measureSeconds')}s",
        f"- Seed: {scenario.get('seed')}",
        "",
        "## Wyniki before / after",
    ]

    report_keys = analysis.get("metric_keys") or [
        "avgFps",
        "avgFrameTimeMs",
        "medianFrameTimeMs",
        "p95FrameTimeMs",
        "p99FrameTimeMs",
        "frameCount",
    ]
    lines.extend([
        "",
        "### Wszystkie metryki liczbowe z `result.json` (top-level + `customMetrics`)",
        "",
        "| Metryka | Before A | After B | Zmiana | Zmiana % | Ocena |",
        "|---------|----------|---------|--------|----------|-------|",
    ])
    for key in report_keys:
        lines.append(metric_row(key, before.get(key, {}), after.get(key, {}), deltas))

    lines.extend(
        [
            "",
            "## Powtarzalność i niepewność",
            f"- Before repetitions: {before.get('avgFps', {}).get('n', 0)} (stdev FPS: {before.get('avgFps', {}).get('stdev', 'N/A')})",
            f"- After repetitions: {after.get('avgFps', {}).get('n', 0)} (stdev FPS: {after.get('avgFps', {}).get('stdev', 'N/A')})",
            "",
            "## Poprawność",
            "```",
            tests_section,
            "```",
            "",
            "## Artefakty i odtworzenie",
            f"- Directory: `{artifact_dir}`",
            "- Re-run: `tools/performance/perf-improve.sh --preset 3d-instancing`",
            "",
            f"_Generated {datetime.now(timezone.utc).isoformat()}_",
        ]
    )

    output.write_text("\n".join(lines))


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--before", type=Path, required=True)
    ap.add_argument("--after", type=Path, required=True)
    ap.add_argument("--primary-improve-pct", type=float, default=3.0)
    ap.add_argument("--p99-regress-pct", type=float, default=2.0)
    ap.add_argument("--output", type=Path, required=True)
    ap.add_argument("--report", type=Path, help="Write report.md")
    ap.add_argument("--artifact-dir", type=Path)
    ap.add_argument("--scenario", type=Path)
    ap.add_argument("--hypothesis", default="")
    ap.add_argument("--tests-log", type=Path)
    args = ap.parse_args()

    before_runs = load_repetitions(args.before)
    after_runs = load_repetitions(args.after)
    if len(before_runs) < 1 or len(after_runs) < 1:
        summary = {
            "verdict": "INCONCLUSIVE",
            "error": "Not enough successful repetitions",
            "before_n": len(before_runs),
            "after_n": len(after_runs),
        }
        args.output.write_text(json.dumps(summary, indent=2))
        if args.report and args.scenario and args.artifact_dir:
            write_report(summary, args.scenario, args.artifact_dir, args.report, args.hypothesis, args.tests_log)
        return 2

    def flattened(runs: list[dict[str, Any]]) -> list[dict[str, float]]:
        return [perf_report.flatten_run(r) for r in runs]

    before_flat = flattened(before_runs)
    after_flat = flattened(after_runs)
    metric_keys = sorted(set().union(*(f.keys() for f in before_flat + after_flat)))

    def agg_flat(flat_runs: list[dict[str, float]], key: str) -> dict[str, float]:
        wrapped = [{key: r[key]} for r in flat_runs if key in r and not math.isnan(r[key])]
        return aggregate(wrapped, key) if wrapped else {"mean": float("nan"), "stdev": float("nan"), "n": 0}

    before_agg = {k: agg_flat(before_flat, k) for k in metric_keys}
    after_agg = {k: agg_flat(after_flat, k) for k in metric_keys}
    verdict = decide(before_agg, after_agg, args.primary_improve_pct, args.p99_regress_pct)

    summary = {
        "verdict": verdict,
        "metric_keys": metric_keys,
        "before": before_agg,
        "after": after_agg,
        "deltas_pct": {
            k: delta_pct(before_agg[k]["mean"], after_agg[k]["mean"]) for k in metric_keys
        },
        "policy": {
            "primary_improve_pct": args.primary_improve_pct,
            "p99_regress_pct": args.p99_regress_pct,
            "primary_metric": PRIMARY,
        },
    }
    args.output.write_text(json.dumps(summary, indent=2))
    print(json.dumps(summary, indent=2))

    if args.report:
        if not args.scenario or not args.artifact_dir:
            print("--report requires --scenario and --artifact-dir", file=sys.stderr)
            return 1
        write_report(summary, args.scenario, args.artifact_dir, args.report, args.hypothesis, args.tests_log)

    return 0 if verdict in ("KEEP", "INCONCLUSIVE", "REVERT") else 1


if __name__ == "__main__":
    sys.exit(main())
