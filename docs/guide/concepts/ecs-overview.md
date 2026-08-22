# ECS Overview

**Entities** are named containers (ID + components). **Components** are data. **Systems** run each frame on matching component sets.

Build objects by composition — e.g. Player = `TransformComponent` + `SpriteRendererComponent` + `RigidBody2DComponent` + `NativeScriptComponent`.

## Built-in vs game types

| Kind | Interface | Defined in | Serialized |
|------|-----------|------------|------------|
| Engine components | `IComponent` | Engine (`TransformComponent`, `RigidBody2DComponent`, …) | Most types in `.scene` / `.prefab` |
| Game components | `IGameComponent` | `assets/scripts/` | Yes, with `[SerializableComponent]` |
| Engine systems | `ISystem` | Engine (physics, rendering, audio) | — |
| Game systems | `IGameSystem` | `assets/scripts/` | — |

## IGameComponent

Custom component data you author for a game. Mark with `[SerializableComponent]`, implement `IGameComponent` and `Clone()`. Attach via **Add Game Component** in the editor (scaffold: `GameComponentTemplates`).

- **Singleton state** on one entity — score, phase, grid arrays: [`SnakeGameComponent`](../../../games/Snake/project/assets/scripts/SnakeGameComponent.cs), [`FlappyBirdGameComponent`](../../../games/FlappyBird/project/assets/scripts/FlappyBirdGameComponent.cs)
- **Per-entity markers** — cell index, pipe slot: [`GridCellComponent`](../../../games/Snake/project/assets/scripts/GridCellComponent.cs), [`PipePairComponent`](../../../games/FlappyBird/project/assets/scripts/PipePairComponent.cs)

## IGameSystem

Batch game logic registered with `[Register(typeof(IGameSystem))]`. Implements `ISystem`: `Priority`, `OnInit`, `OnUpdate`, `OnShutdown`. Inject `IContext` for queries, `IKeyboardInput` / `IAudio` / `IPhysicsContacts` as needed (scaffold: `GameSystemTemplates`).

- [`SnakeSystem`](../../../games/Snake/project/assets/scripts/SnakeSystem.cs) — reads `SnakeGameComponent`, polls `IKeyboardInput`, updates every `GridCellComponent` visual
- [`FlappyBirdSystem`](../../../games/FlappyBird/project/assets/scripts/FlappyBirdSystem.cs) — simulates bird/pipes from `FlappyBirdGameComponent`, syncs transforms and score digits

Open samples: `games/Snake/project/`, `games/FlappyBird/project/` via **Open Project**.

## Scripts

Per-entity glue: `ScriptableEntity` + `NativeScriptComponent`. `GetComponent<T>()` on the host entity only — no `CreateEntity` / `FindEntity`. Systems use `IContext` (e.g. `context.GetByName("Player")`). See [Scripting Tiers](../scripting/scripting-tiers.md).

## Editor workflow

Create entities in **Scene Hierarchy** → attach components via **Add Component** in Properties. One component per type per entity.

**12** built-in engine component types serialize to `.scene` / `.prefab` (including `ParentComponent` and 2D colliders). `TagComponent` and `IdComponent` exist in code but are not exposed in the editor or saved in scene files. Full list: [Component Inspector](../editor/component-inspector.md).
