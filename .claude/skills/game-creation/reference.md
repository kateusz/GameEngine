# Game Creation Reference

Companion to [SKILL.md](SKILL.md). Read when scaffolding or reviewing a game project.

## Sample maps

### Snake (`games/Snake/`)

| File | Role |
|------|------|
| `project/assets/scripts/SnakeGameComponent.cs` | Serializable game state (grid, body, food, score, tick) |
| `project/assets/scripts/GridCellComponent.cs` | Per-cell index on flat entities |
| `project/assets/scripts/SnakeSystem.cs` | `[Register]` system: input, step, audio, visual sync |
| `project/assets/scenes/snake.scene` | Startup scene |
| `project/game.config.json` | Runtime/publish config |
| `Snake.csproj` | Optional IDE project refs into engine |

**Lessons:** Fixed timestep in system (`TickAccumulator` / `TickInterval`). Banners = sprite entities toggled by texture path. No `ScriptableEntity` required when the system injects `IKeyboardInput`.

### TicTacToe (`games/TicTacToe/`)

| File | Role |
|------|------|
| `project/assets/scripts/BoardComponent.cs` | Board state + mailboxes (`PendingCellIndex`, `ResetRequested`) |
| `project/assets/scripts/CellComponent.cs` | Cell index |
| `project/assets/scripts/TicTacToeSystem.cs` | Rules + sprite sync |
| `project/assets/scripts/GameControllerScript.cs` | Glue: keys → mailboxes + one-shot audio |
| `project/assets/scripts/tictactoe.csproj` | Script SDK refs via `.engine/sdk/*.dll` (editor layout) |
| `project/game.config.json` | Startup → `assets/scenes/main.scene` |

**Lessons:** Script writes intent; system applies rules. Win/draw banners via texture swaps. Prefer this split when one entity should own input callbacks.

## Genre fit (readiness)

Ship-shaped today: grid arcade, turn-based board, Pong/Breakout-class, simple lane shooters (contact-based), idle/clicker with sprite UI.

Defer or redesign: anything needing runtime UI widgets, raycasts, hierarchy, particles, tilemaps, gamepad, or real 3D meshes.

Source: [docs/readiness-analysis-2026-07.md](../../../docs/readiness-analysis-2026-07.md).

## Create flows (editor)

From [content-browser.md](../../../docs/guide/editor/content-browser.md):

1. Right-click `assets/scripts/` → **Add Component** / **Add System** / **Add Script**
2. Or Properties → `NativeScriptComponent` → **Create New Script** / **Add Existing Script**
3. Hot-reload: save under `assets/scripts/` → new `GameAssembly_{guid}.dll` under `.engine/`

## Injectables for `IGameSystem`

Typical sample set:

- `IContext` — `View<T>()`, entity/component queries
- `IKeyboardInput` — `IsKeyDown` / `WasKeyPressed`
- `IAudio` — `PlayOneShot(path)`
- `IPhysicsContacts` — `DrainContacts()` when using physics in systems

Priority: samples use `115` (game logic band; see engine `SystemPriorities` / `system-creation` skill).

## Publish smoke

After editor publish, assert:

- Executable present
- `game.config.json` beside it
- `GameAssembly.dll` (or configured `GameAssemblyPath`)
- Startup scene file exists at configured path

No automated publish tests yet — manual smoke for alpha drops.

## Doc index

- [docs/guide/index.md](../../../docs/guide/index.md)
- [docs/guide/roadmap.md](../../../docs/guide/roadmap.md)
- [docs/guide/scripting/api-reference.md](../../../docs/guide/scripting/api-reference.md)
- [docs/architecture/scripting-lifecycle.md](../../../docs/architecture/scripting-lifecycle.md) (if present)
- [docs/architecture/game-loop.md](../../../docs/architecture/game-loop.md) — runtime load of `game.config.json`
