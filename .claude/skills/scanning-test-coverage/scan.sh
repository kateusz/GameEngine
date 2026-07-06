#!/usr/bin/env bash
# Scan a source directory for public types and report missing 1:1 test files.
# Usage: scan.sh <source-dir> <test-dir> [source-project-root]
#   source-dir: e.g. Engine/Scene/Systems
#   test-dir: e.g. tests/Engine.Tests
#   source-project-root: e.g. Engine (default: first path segment of source-dir)
#
# Requires: rg (ripgrep). Cross-platform when rg is on PATH.
# Exit 1 if rg is missing or source-dir does not exist.

set -euo pipefail

SOURCE_DIR="${1:?usage: scan.sh <source-dir> <test-dir> [source-project-root]}"
TEST_DIR="${2:?usage: scan.sh <source-dir> <test-dir> [source-project-root]}"
SOURCE_ROOT="${3:-${SOURCE_DIR%%/*}}"

if ! command -v rg >/dev/null 2>&1; then
  echo "error: rg (ripgrep) not found on PATH" >&2
  exit 1
fi

if [[ ! -d "$SOURCE_DIR" ]]; then
  echo "error: source directory not found: $SOURCE_DIR" >&2
  exit 1
fi

if [[ ! -d "$TEST_DIR" ]]; then
  echo "error: test directory not found: $TEST_DIR" >&2
  exit 1
fi

# Strip source-project prefix; Engine/Scene/* flattens to tests/Engine.Tests/{module}/
rel="${SOURCE_DIR#"$SOURCE_ROOT"/}"
rel="${rel#"$SOURCE_ROOT"}"
rel="${rel#/}"
if [[ "$rel" == Scene/* ]]; then
  rel="${rel#Scene/}"
fi
TEST_SUB=""
if [[ -n "$rel" ]]; then
  TEST_SUB="/$rel"
fi

missing=0
covered=0
grouped=0
excluded=0

while IFS= read -r line; do
  file="${line%%:*}"
  rest="${line#*:}"
  type_name=""
  if [[ "$rest" =~ public\ ([A-Za-z_]+\ )*(class|record|struct)\ ([A-Za-z_][A-Za-z0-9_]*) ]]; then
    type_name="${BASH_REMATCH[3]}"
  else
    continue
  fi

  if [[ "$rest" =~ public\ interface\ ]] || [[ "$rest" =~ public\ enum\ ]]; then
    ((excluded++)) || true
    continue
  fi

  expected="${TEST_DIR}${TEST_SUB}/${type_name}Tests.cs"
  one_to_one=$(rg --files "$TEST_DIR" -g "${type_name}Tests.cs" 2>/dev/null | head -n 1 || true)
  if [[ -n "$one_to_one" ]]; then
    echo "covered|${file}|${type_name}|${one_to_one}"
    ((covered++)) || true
    continue
  fi

  match=$(rg -l -g '*.cs' "\\b${type_name}\\b" "$TEST_DIR" 2>/dev/null | grep -v "${type_name}Tests\.cs$" | head -n 1 || true)
  if [[ -n "$match" ]]; then
    echo "grouped|${file}|${type_name}|${match}"
    ((grouped++)) || true
    continue
  fi

  echo "missing|${file}|${type_name}|${expected}"
  ((missing++)) || true
done < <(rg --no-heading -n "public (?:\w+ )*(class|record|struct) " "$SOURCE_DIR" -g '*.cs' 2>/dev/null || true)

echo "---"
echo "summary|missing=${missing}|covered=${covered}|grouped=${grouped}|excluded=${excluded}"
