---
name: game-creation
description: Guardrails and workflow for creating games on this engine using sample patterns (Snake, FlappyBird), scripting tiers, project layout, and readiness limits. Use when scaffolding a new game, adding gameplay scripts/systems/components under games/ or assets/scripts/, publishing a game, or when the user asks how to build a game with the engine.
---

# Game Creation

Create **small 2D games** the way `games/Snake` and `games/FlappyBird` do — not like Unity/Godot full-stack projects.

**Hard gate:** Before scaffolding or writing gameplay code, run the [feasibility check](#0-feasibility-check). If the design needs a missing capability, stop and redesign or defer.

## When to use

- New game under `games/` or a new editor project
- New `IGameComponent` / `IGameSystem` / `ScriptableEntity` for gameplay
- Menus/HUD/scoring UX decisions
- Publish / `game.config.json` setup

For **engine** features (new built-in components, editor panels), use `component-workflow`, `system-creation`, or `brainstorming` instead.

## Docs to read (do not invent APIs)

| Topic | Doc |
|-------|-----|
| Project layout | [docs/guide/editor/project-setup.md](../../../docs/guide/editor/project-setup.md) |
| Scripting tiers | [docs/guide/scripting/scripting-tiers.md](../../../docs/guide/scripting/scripting-tiers.md) |
| Scripts | [docs/guide/scripting/getting-started.md](../../../docs/guide/scripting/getting-started.md) |
| Input | [docs/guide/scripting/input.md](../../../docs/guide/scripting/input.md) |
| Physics | [docs/guide/scripting/physics.md](../../../docs/guide/scripting/physics.md) |
| ECS | [docs/guide/concepts/ecs-overview.md](../../../docs/guide/concepts/ecs-overview.md) |
| Content Browser create flows | [docs/guide/editor/content-browser.md](../../../docs/guide/editor/content-browser.md) |
| What ships today | [docs/readiness-analysis-2026-07.md](../../../docs/readiness-analysis-2026-07.md) |

Canonical samples: `games/Snake/`, `games/FlappyBird/`. Sample anatomy → [reference.md](reference.md).

---

## 0. Feasibility check

Engine is ~70% ready for small **2D** prototypes. Confirm the design fits:

| OK today | Not ready — redesign |
|----------|----------------------|
| Grid / puzzle / turn-based / Pong-class arcade | Real menus, HUD text, settings (no runtime UI) |
| Keyboard + mouse | Gamepad / rebinding |
| Sprites, colored quads, texture-swap banners | Particles, tilemaps, sorting layers |
| Box colliders + contact/trigger callbacks | Raycasts, shape queries, circle colliders |
| Flat entity list | Parent/child hierarchy (weapons-on-player, vehicles) |
| OpenAL one-shots / spatial audio | — |
| Publish via editor (`game.config.json`) | Assume undo/redo or asset GUIDs |

**UI rule:** No ImGui in published games. Fake UI with sprites/quads (see Snake `SyncBanners`). Do not invent a UI framework in the game project.

If blocked, say so and propose a Snake/FlappyBird-shaped redesign.

---

## 1. Project shape

Prefer editor **New Project**, or mirror samples:

```
GameName/
├── project/                    # editor project root (samples use this)
│   ├── game.config.json
│   └── assets/
│       ├── scenes/
│       ├── textures/
│       ├── scripts/            # all game C# lives here
│       ├── sounds/             # optional
│       └── prefabs/
└── GameName.csproj             # optional: IDE refs to engine projects (see Snake.csproj)
```

`game.config.json` (required for publish/runtime):

```json
{
  "GameAssemblyPath": "GameAssembly.dll",
  "StartupScenePath": "assets/scenes/main.scene",
  "WindowWidth": 1280,
  "WindowHeight": 720,
  "Fullscreen": false,
  "GameTitle": "My Game",
  "TargetFrameRate": 60
}
```

Asset paths in code/scenes are **project-relative** (`textures/X.png`, `assets/sounds/eat.wav`). Renames break silently (no GUID DB).

---

## 2. Scripting tiers (mandatory)

From [scripting-tiers.md](../../../docs/guide/scripting/scripting-tiers.md):

| Tier | Type | Put here |
|------|------|----------|
| **Data** | `IGameComponent` + `[SerializableComponent]` | Score, board, grid state — inspector + scene JSON |
| **Glue** | `ScriptableEntity` + `NativeScriptComponent` | Per-entity input → write flags/mailboxes on components |
| **Logic** | `IGameSystem` + `[Register(typeof(IGameSystem))]` | Rules, queries, sync visuals, global keyboard via `IKeyboardInput` |

**Rules:**

1. Tunable/shared state → game components, **not** script fields (script fields do not serialize).
2. Batch rules / win conditions / tick loops → `IGameSystem`.
3. Scripts are thin glue; systems own gameplay.
4. Create via Content Browser on `assets/scripts/`: **Add Component / Add System / Add Script** (templates).

### Pattern A — system owns input (Snake / FlappyBird)

`SnakeGameComponent` = state · `SnakeSystem` = input + tick + `SyncCellVisuals` / banners · inject `IContext`, `IKeyboardInput`, `IAudio`. Same shape in FlappyBird (`FlappyBirdGameComponent` + `FlappyBirdSystem`).

### Pattern B — script mailbox + system rules

Thin `ScriptableEntity` writes intent flags/mailboxes on a component · `IGameSystem` consumes them and syncs visuals. Prefer this when one entity should own input callbacks.

Pick one; do not scatter the same rule in both.

---

## 3. Implementation checklist

Copy and track:

```
Game Progress:
- [ ] Feasibility OK (no runtime UI / raycasts / hierarchy / gamepad required)
- [ ] Project + scene + camera
- [ ] `[SerializableComponent]` state component(s) + `Clone()`
- [ ] `[Register(typeof(IGameSystem))]` rules system (Priority ~115 like samples)
- [ ] Visuals: SpriteRendererComponent texture/color sync from system
- [ ] Input: IKeyboardInput in system OR ScriptableEntity → component mailbox
- [ ] Audio (optional): IAudio.PlayOneShot with stable relative path
- [ ] Physics only if needed: RigidBody2D + BoxCollider2D; contacts via script callbacks or IPhysicsContacts
- [ ] game.config.json StartupScenePath correct
- [ ] Play in editor; then publish smoke (exe + config + GameAssembly.dll + scene)
```

### Visual sync (samples)

Systems drive presentation each frame — e.g. set `SpriteRendererComponent.TexturePath` / `Color` from component state. Grid/board cells are **separate flat entities** with an index component (`GridCellComponent`, `CellComponent`), not children.

### Physics (optional)

See [physics.md](../../../docs/guide/scripting/physics.md). Both `RigidBody2DComponent` and `BoxCollider2DComponent` required. Use contacts/triggers — **no raycast API**. Prefer grid/arcade logic when possible (Snake style).

---

## 4. Common mistakes

| Mistake | Do instead |
|---------|------------|
| Build menus with ImGui or a custom UI kit | Sprite/quad banners; accept minimal UX |
| Put score/board on `ScriptableEntity` fields | `[SerializableComponent]` game component |
| One giant `ScriptableEntity` for all rules | `IGameSystem` + components |
| Assume parent/child transforms | Flat entities; manual sync if attached parts needed |
| Raycast for aim/ground/LoS | Contacts, sensors, or grid math |
| Hardcode absolute disk paths | Project-relative asset paths |
| Skip `Clone()` on game components | Implement `IComponent.Clone()` like samples |
| New engine systems in `Engine/` for one game | Keep logic in `assets/scripts/` with `[Register]` |

---

## 5. Related skills

- `brainstorming` — design docs before large engine features (not required for cloning Snake-shaped games)
- `system-creation` / `component-workflow` — **engine** code, not game assemblies
- `serialization-review` — if adding custom serializable game component shapes

## Additional resources

- [reference.md](reference.md) — sample file maps, genre fit, publish notes
