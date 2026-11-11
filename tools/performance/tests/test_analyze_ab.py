import json
import subprocess
import tempfile
from pathlib import Path


def write_rep(d: Path, i: int, fps: float, p99: float) -> None:
    (d / f"repetition-{i}.json").write_text(
        json.dumps(
            {
                "success": True,
                "avgFps": fps,
                "avgFrameTimeMs": 1000 / fps,
                "medianFrameTimeMs": 1000 / fps,
                "p95FrameTimeMs": 1000 / fps,
                "p99FrameTimeMs": p99,
                "frameCount": 600,
            }
        )
    )


def test_keep_on_fps_gain():
    root = Path(__file__).resolve().parents[1]
    with tempfile.TemporaryDirectory() as tmp:
        before = Path(tmp) / "before"
        after = Path(tmp) / "after"
        before.mkdir()
        after.mkdir()
        for i in range(3):
            write_rep(before, i + 1, 100, 20)
            write_rep(after, i + 1, 110, 20)
        out = Path(tmp) / "analysis.json"
        subprocess.check_call(
            [
                "python3",
                str(root / "analyze_ab.py"),
                "--before",
                str(before),
                "--after",
                str(after),
                "--output",
                str(out),
            ]
        )
        data = json.loads(out.read_text())
        assert data["verdict"] == "KEEP"
