#!/usr/bin/env bash
# Builds the engine Runtime + GameAssembly.dll, stages them with game.config.json and assets/, cooks model meshes,
# and launches the game.
#   ./run.sh            Release build + run
#   CONFIG=Debug ./run.sh
#   ./run.sh --no-run   build/stage only
set -euo pipefail

GAME_DIR="$(cd "$(dirname "$0")" && pwd)"
ENGINE_DIR="$(cd "$GAME_DIR/../.." && pwd)"
CONFIG="${CONFIG:-Release}"
STAGE="$GAME_DIR/.build/$CONFIG"
BUILD_FLAGS=(-c "$CONFIG" --nologo -v quiet "-clp:ErrorsOnly;NoSummary")

dotnet build "$ENGINE_DIR/Runtime/Runtime.csproj" "${BUILD_FLAGS[@]}" -o "$STAGE"
dotnet build "$GAME_DIR/IslandDash.csproj" "${BUILD_FLAGS[@]}" -o "$GAME_DIR/.build/game-$CONFIG"
dotnet build "$GAME_DIR/tools/MeshCook/MeshCook.csproj" "${BUILD_FLAGS[@]}" -o "$GAME_DIR/.build/meshcook-$CONFIG"

cp "$GAME_DIR/.build/game-$CONFIG/GameAssembly.dll" "$GAME_DIR/.build/game-$CONFIG/GameAssembly.pdb" "$STAGE/"
cp "$GAME_DIR/game.config.json" "$STAGE/"
for dir in "$GAME_DIR"/assets/*/; do
    name="$(basename "$dir")"
    [ "$name" = "scripts" ] && continue
    mkdir -p "$STAGE/assets/$name"
    cp -R "$dir." "$STAGE/assets/$name/"
done

# Linux: Silk.NET only probes system paths for GLFW/Assimp, so point it at the natives shipped in runtimes/.
if [ "$(uname -s)" = "Linux" ]; then
    case "$(uname -m)" in
        aarch64|arm64) rid=linux-arm64 ;;
        armv7l) rid=linux-arm ;;
        *) rid=linux-x64 ;;
    esac
    export LD_LIBRARY_PATH="$STAGE/runtimes/$rid/native:$GAME_DIR/.build/meshcook-$CONFIG/runtimes/$rid/native:${LD_LIBRARY_PATH:-}"
fi

# The Runtime loads cooked "<model>.mesh" siblings only; (re)cook any that are missing or stale.
dotnet "$GAME_DIR/.build/meshcook-$CONFIG/MeshCook.dll" "$STAGE/assets/models"

if [ "${1:-}" = "--no-run" ]; then
    echo "Staged to $STAGE"
    exit 0
fi

cd "$STAGE"
exec dotnet Runtime.dll
