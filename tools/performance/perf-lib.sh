# shellcheck shell=bash
# Source from perf-run / perf / perf-improve (requires ROOT).
PERF_SCENES_JSON="${ROOT}/tools/performance/perf-scenes.json"

perf_default_3d_paths() {
  export GAMEENGINE_ROOT="$ROOT"
  python3 - "$PERF_SCENES_JSON" <<'PY'
import json, os, sys
catalog = json.load(open(sys.argv[1]))
d = catalog.get("defaults", {})
repo = os.environ["GAMEENGINE_ROOT"]
rel_root = d.get("project3dRoot", "Editor/bin/Debug/net10.0/3d")
pr = os.path.join(repo, rel_root)
rel_scene = d.get("scene3d", "assets/scenes/sponza.scene")
sf = rel_scene if os.path.isabs(rel_scene) else os.path.join(pr, rel_scene)
print(f"{pr}|{sf}")
PY
}

perf_resolve_preset() {
  export GAMEENGINE_ROOT="$ROOT"
  python3 - "$PERF_SCENES_JSON" "$1" <<'PY'
import json, os, sys
p = json.load(open(sys.argv[1]))["presets"][sys.argv[2]]
root = os.environ["GAMEENGINE_ROOT"]
pr = os.path.join(root, p["projectRoot"]) if p.get("projectRoot") else ""
sf = p.get("sceneFile", "")
if sf:
    if p.get("projectRoot"):
        sf = sf if os.path.isabs(sf) else os.path.join(pr, sf)
    else:
        sf = os.path.join(root, sf)
count = p.get("count", 1000)
print(f"{p['scene']}|{count}|{pr}|{sf}|{p.get('dimension', '3d')}")
PY
}

perf_presets_for_dimension() {
  python3 - "$PERF_SCENES_JSON" "$1" <<'PY'
import json, sys
for name, p in json.load(open(sys.argv[1]))["presets"].items():
    if p.get("dimension") == sys.argv[2]:
        print(name)
PY
}

perf_preset_warmup_seconds() {
  python3 - "$PERF_SCENES_JSON" "$1" <<'PY'
import json, sys
e = json.load(open(sys.argv[1]))["presets"][sys.argv[2]]
if "warmupSeconds" in e:
    print(e["warmupSeconds"])
elif e.get("scene") == "3D:GlbModels" or e.get("projectRoot"):
    print(10)
else:
    print(5)
PY
}

perf_write_scenario_snapshot() {
  python3 - "$1" "$PERF_SCENES_JSON" "$2" <<'PY'
import json, sys
out, catalog, preset = sys.argv[1:4]
e = json.load(open(catalog))["presets"][preset]
scene = e["scene"]
cat, tid = scene.split(":", 1)
if "warmupSeconds" in e:
    warmup = e["warmupSeconds"]
elif scene == "3D:GlbModels" or e.get("projectRoot"):
    warmup = 10
else:
    warmup = 5
json.dump({
    "preset": preset,
    "category": cat, "testId": tid,
    "count": e.get("count", 1000),
    "width": 1280, "height": 720,
    "warmupSeconds": warmup, "measureSeconds": 5,
    "seed": 12345, "targetFrameRate": 0,
}, open(out, "w"), indent=2)
PY
}
