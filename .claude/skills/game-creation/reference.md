# Game Creation Reference

Companion to [SKILL.md](SKILL.md). Read when scaffolding or reviewing a game project.

## Sample maps

Open the **game root** (folder with `assets/`), not any leftover `project/` copy.

### Snake (`games/Snake/`)

| File | Role |
|------|------|
| `assets/scripts/SnakeGameComponent.cs` | Serializable game state (grid, body, food, score, tick) |
| `assets/scripts/GridCellComponent.cs` | Per-cell index on flat entities |
| `assets/scripts/SnakeSystem.cs` | `[Register]` system: input, step, audio, visual sync |
| `assets/scenes/snake.scene` | Startup scene |
| `game.config.json` | Runtime/publish config |
| `Snake.csproj` | Optional IDE project refs into engine |

**Lessons:** Fixed timestep in system (`TickAccumulator` / `TickInterval`). Banners = sprite entities toggled by texture path. No `ScriptableEntity` required when the system injects `IKeyboardInput`.

### FlappyBird (`games/FlappyBird/`)

| File | Role |
|------|------|
| `assets/scripts/FlappyBirdGameComponent.cs` | Serializable game state |
| `assets/scripts/PipePairComponent.cs` | Per-pipe-pair index/state |
| `assets/scripts/ScoreDigitComponent.cs` | Score digit display |
| `assets/scripts/FlappyBirdSystem.cs` | `[Register]` system: input, physics, scoring, visuals |
| `assets/scenes/flappybird.scene` | Startup scene |
| `game.config.json` | Runtime/publish config |

**Lessons:** Physics-driven arcade loop in one `IGameSystem`. Sprite/quad UI for score and banners. Audio under `assets/audio/`.

### TicTacToe (`games/TicTacToe/`)

| File | Role |
|------|------|
| `assets/scripts/BoardComponent.cs` | Board / game state |
| `assets/scripts/CellComponent.cs` | Per-cell index on flat entities |
| `assets/scripts/TicTacToeSystem.cs` | `[Register]` system: turn rules, win, visuals |
| `assets/scripts/GameControllerScript.cs` | Optional script glue |
| `assets/scenes/main.scene` | Startup scene |
| `game.config.json` | Runtime/publish config |

**Lessons:** Turn-based board on flat cell entities — same Pattern A as Snake.

## Genre fit (readiness)

Ship-shaped today: grid arcade, turn-based board, Pong/Breakout-class, simple lane shooters (contacts + raycasts), idle/clicker with sprite UI.

Defer or redesign: anything needing runtime UI widgets, hierarchy, particles, tilemaps, circle colliders, gamepad, or sorting layers.

Source: [docs/readiness-analysis-2026-07.md](../../../docs/readiness-analysis-2026-07.md) (raycasts since shipped — prefer [physics.md](../../../docs/guide/scripting/physics.md) + [api-reference.md](../../../docs/guide/scripting/api-reference.md) over the analysis for query APIs).

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
- `IPhysicsContacts` — `DrainContacts()` when using physics contacts in systems
- `IPhysicsQueries` — `Raycast` / `OverlapCircle` when systems need queries (scripts use protected helpers on `ScriptableEntity`)

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
- [docs/architecture/scripting-lifecycle.md](../../../docs/architecture/scripting-lifecycle.md)
- [docs/architecture/game-loop.md](../../../docs/architecture/game-loop.md) — runtime load of `game.config.json`
