"""Flatten perf-run JSON for A/B tables (all numeric fields + customMetrics)."""
from __future__ import annotations

import json
import math
from pathlib import Path
from typing import Any

# Top-level keys that are not scalar metrics in reports
_META_KEYS = frozenset({
    "schema",
    "scene",
    "category",
    "testId",
    "rendererMode",
    "customMetrics",
    "frameSamplesCsv",
    "success",
    "error",
    "gitCommitSha",
    "iterationAvgFps",
    "iterationAvgFrameTimeMs",
})


def parse_metric_value(value: Any) -> float:
    if value is None:
        return float("nan")
    if isinstance(value, bool):
        return float(value)
    if isinstance(value, (int, float)):
        return float(value)
    s = str(value).strip().replace(",", ".")
    if not s:
        return float("nan")
    try:
        return float(s)
    except ValueError:
        return float("nan")


def flatten_run(data: dict[str, Any]) -> dict[str, float]:
    out: dict[str, float] = {}
    for key, value in data.items():
        if key in _META_KEYS:
            continue
        if isinstance(value, (int, float)):
            out[key] = float(value)
    for key, value in (data.get("customMetrics") or {}).items():
        out[f"custom.{key}"] = parse_metric_value(value)
    return out


def metric_keys(main: dict[str, float], feature: dict[str, float]) -> list[str]:
    keys = sorted(set(main) | set(feature))
    return keys


def delta_pct(before: float, after: float) -> float | None:
    if math.isnan(before) or math.isnan(after) or before == 0:
        return None
    return (after - before) / before * 100.0


def compare_pair(main_path: Path, feature_path: Path) -> dict[str, Any]:
    main_raw = json.loads(main_path.read_text())
    feature_raw = json.loads(feature_path.read_text())
    main = flatten_run(main_raw)
    feature = flatten_run(feature_raw)
    rows = []
    for key in metric_keys(main, feature):
        mb, fb = main.get(key, float("nan")), feature.get(key, float("nan"))
        rows.append({
            "metric": key,
            "main": mb,
            "feature": fb,
            "delta_abs": fb - mb if not (math.isnan(mb) or math.isnan(fb)) else None,
            "delta_pct": delta_pct(mb, fb),
        })
    return {
        "scenario": main_path.stem.split("__", 1)[-1],
        "scene": main_raw.get("scene"),
        "main_file": str(main_path),
        "feature_file": str(feature_path),
        "metrics": rows,
    }


def write_markdown_report(comparisons: list[dict[str, Any]], out: Path, title: str) -> None:
    lines = [f"# {title}", ""]
    for block in comparisons:
        lines.extend([
            f"## {block['scenario']}",
            "",
            f"- Scene: `{block.get('scene', '')}`",
            "",
            "| Metric | Main | Feature | Δ abs | Δ % |",
            "|--------|------|---------|-------|-----|",
        ])
        for row in block["metrics"]:
            m, f = row["main"], row["feature"]
            if math.isnan(m) and math.isnan(f):
                continue
            ms = "—" if math.isnan(m) else f"{m:.4g}"
            fs = "—" if math.isnan(f) else f"{f:.4g}"
            da = row["delta_abs"]
            dp = row["delta_pct"]
            da_s = "—" if da is None else f"{da:+.4g}"
            dp_s = "—" if dp is None else f"{dp:+.2f}%"
            lines.append(f"| {row['metric']} | {ms} | {fs} | {da_s} | {dp_s} |")
        lines.append("")
    out.write_text("\n".join(lines))


def compare_directory(
    run_dir: Path,
    main_prefix: str = "main__",
    feature_prefix: str = "feature__",
) -> tuple[list[dict[str, Any]], Path, Path]:
    scenarios = sorted(
        p.name[len(main_prefix) : -5]
        for p in run_dir.glob(f"{main_prefix}*.json")
    )
    comparisons = []
    for scenario in scenarios:
        comparisons.append(
            compare_pair(
                run_dir / f"{main_prefix}{scenario}.json",
                run_dir / f"{feature_prefix}{scenario}.json",
            )
        )
    comparison_json = run_dir / "comparison.json"
    comparison_json.write_text(json.dumps(comparisons, indent=2))
    report_md = run_dir / "report.md"
    write_markdown_report(
        comparisons,
        report_md,
        "Performance A/B (all benchmark metrics)",
    )
    return comparisons, comparison_json, report_md


if __name__ == "__main__":
    import sys

    if len(sys.argv) != 2:
        print("Usage: perf_report.py RUN_DIR", file=sys.stderr)
        raise SystemExit(1)
    d = Path(sys.argv[1])
    _, cj, rm = compare_directory(d)
    print("Wrote", cj)
    print("Wrote", rm)
