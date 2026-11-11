#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
# shellcheck source=perf-lib.sh
source "$ROOT/tools/performance/perf-lib.sh"

PRESET="${PRESET:-3d-instancing}"
REPETITIONS=1
FOCUS=""
RUN_ID="$(date -u +%Y%m%dT%H%M%SZ)-$$"
ARTIFACT_ROOT="${ROOT}/artifacts/performance"
OPTIMIZATION_PATHS=()

while [[ $# -gt 0 ]]; do
  case "$1" in
    --preset) PRESET="$2"; shift 2 ;;
    --repetitions) REPETITIONS="$2"; shift 2 ;;
    --focus) FOCUS="$2"; shift 2 ;;
    --run-id) RUN_ID="$2"; shift 2 ;;
    --optimization-path) OPTIMIZATION_PATHS+=("$2"); shift 2 ;;
    --skip-tests) SKIP_TESTS=1; shift ;;
    --no-revert) NO_REVERT=1; shift ;;
    -h|--help)
      echo "perf-improve.sh [--preset 3d-instancing] [--repetitions N] [--optimization-path FILE]..."
      exit 0 ;;
    *) echo "Unknown: $1" >&2; exit 1 ;;
  esac
done

SKIP_TESTS="${SKIP_TESTS:-0}"
NO_REVERT="${NO_REVERT:-0}"

RUN_DIR="${ARTIFACT_ROOT}/${RUN_ID}"
mkdir -p "$RUN_DIR/before" "$RUN_DIR/after" "$RUN_DIR/logs"

perf_write_scenario_snapshot "$RUN_DIR/scenario.json" "$PRESET"
IFS='|' read -r _ _ _ _ TEST_DIM <<< "$(perf_resolve_preset "$PRESET")"

git -C "$ROOT" diff > "$RUN_DIR/logs/working-tree.diff" || true
git -C "$ROOT" rev-parse HEAD > "$RUN_DIR/logs/head.txt"

PERF_RUN="${ROOT}/tools/performance/perf-run"
chmod +x "$PERF_RUN" "${ROOT}/tools/performance/perf-tests"
RUN_ARGS=(--preset "$PRESET")

STASHED=0
PERF_STASH_SHA=""
pop_stash() {
  [[ $STASHED -ne 1 ]] && return 0
  set +e
  local rc=0
  if [[ -n "$PERF_STASH_SHA" ]]; then
    git -C "$ROOT" stash apply -q "$PERF_STASH_SHA"
    rc=$?
    if [[ $rc -eq 0 ]]; then
      git -C "$ROOT" stash drop -q "$PERF_STASH_SHA" 2>/dev/null || true
    fi
  else
    git -C "$ROOT" stash pop -q
    rc=$?
  fi
  set -e
  STASHED=0
  PERF_STASH_SHA=""
  if [[ $rc -ne 0 ]]; then
    echo "perf-improve: could not restore stashed optimization (exit $rc)" >&2
    return 1
  fi
}
push_optimization_stash() {
  if [[ ${#OPTIMIZATION_PATHS[@]} -eq 0 ]]; then
    if [[ -s "$RUN_DIR/logs/working-tree.diff" ]]; then
      git -C "$ROOT" stash push -u -m "perf-improve-$$" >/dev/null
      PERF_STASH_SHA="$(git -C "$ROOT" rev-parse stash@{0})"
      STASHED=1
    fi
    return
  fi
  git -C "$ROOT" stash push -m "perf-improve-$$" -- "${OPTIMIZATION_PATHS[@]}" >/dev/null
  PERF_STASH_SHA="$(git -C "$ROOT" rev-parse stash@{0})"
  STASHED=1
}

push_optimization_stash

"$PERF_RUN" "${RUN_ARGS[@]}" --repetitions "$REPETITIONS" --run-dir "$RUN_DIR/before" || { pop_stash || true; echo BLOCKED > "$RUN_DIR/verdict.txt"; exit 4; }
pop_stash || { echo BLOCKED > "$RUN_DIR/verdict.txt"; exit 4; }

"$PERF_RUN" "${RUN_ARGS[@]}" --repetitions "$REPETITIONS" --run-dir "$RUN_DIR/after" || { echo BLOCKED > "$RUN_DIR/verdict.txt"; exit 4; }

if [[ $SKIP_TESTS -eq 0 ]]; then
  : > "$RUN_DIR/logs/tests.log"
  "${ROOT}/tools/performance/perf-tests" "$TEST_DIM" >>"$RUN_DIR/logs/tests.log" 2>&1 || echo TESTS_FAILED >> "$RUN_DIR/verdict.txt"
fi

python3 "${ROOT}/tools/performance/analyze_ab.py" \
  --before "$RUN_DIR/before" --after "$RUN_DIR/after" \
  --output "$RUN_DIR/analysis.json" --report "$RUN_DIR/report.md" \
  --artifact-dir "$RUN_DIR" --scenario "$RUN_DIR/scenario.json" \
  --hypothesis "${FOCUS:-Automated performance pass}" --tests-log "$RUN_DIR/logs/tests.log"

VERDICT="$(python3 -c "import json; print(json.load(open('$RUN_DIR/analysis.json'))['verdict'])")"
echo "$VERDICT" > "$RUN_DIR/verdict.txt"

if [[ "$VERDICT" == "REVERT" || "$VERDICT" == "INCONCLUSIVE" ]] && [[ $NO_REVERT -eq 0 ]] && [[ ${#OPTIMIZATION_PATHS[@]} -gt 0 ]]; then
  git -C "$ROOT" checkout HEAD -- "${OPTIMIZATION_PATHS[@]}" 2>/dev/null || true
fi

echo "Report: $RUN_DIR/report.md  Verdict: $VERDICT"
