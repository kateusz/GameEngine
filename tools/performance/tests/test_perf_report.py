import math
import sys
import tempfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT))
import perf_report  # noqa: E402


def test_metric_keys_always_include_memory_and_exclude_config():
    keys = perf_report.metric_keys_for_report({"avgFps": 60.0}, {"avgFps": 61.0})
    assert "avgMemoryUsageMb" in keys
    assert "avgCpuUsagePct" in keys
    assert "count" not in keys
    assert "width" not in keys


def test_markdown_lists_memory_even_when_json_lacks_it():
    with tempfile.TemporaryDirectory() as tmp:
        run_dir = Path(tmp)
        main = {
            "schema": "gameengine-perf/1",
            "scene": "3D:InstancedMesh",
            "success": True,
            "avgFps": 800.0,
            "avgFrameTimeMs": 1.25,
        }
        feature = {**main, "avgFps": 900.0}
        (run_dir / "main__demo.json").write_text(__import__("json").dumps(main))
        (run_dir / "feature__demo.json").write_text(__import__("json").dumps(feature))
        _, _, report = perf_report.compare_directory(run_dir)
        text = report.read_text()
        assert "avgMemoryUsageMb" in text
        mem_lines = [ln for ln in text.splitlines() if "avgMemoryUsageMb" in ln]
        assert mem_lines and "—" in mem_lines[0]
