# ECS Overview

**Entities** are named containers (ID + components). **Components** are data. **Systems** run each frame on matching component sets.

Build objects by composition — e.g. Player = `TransformComponent` + `SpriteRendererComponent` + `RigidBody2DComponent`.

## Built-in vs game types

| Kind | Interface | Defined in | Serialized |
|------|-----------|------------|------------|
| Engine components | `IComponent` | `SceneComponents/` (`TransformComponent`, `RigidBody2DComponent`, …) | Registered types in `.scene` / `.prefab` (see below) |
| Game components | `IGameComponent` | `assets/scripts/` | Yes, with `[SerializableComponent]` |
| Engine systems | `ISystem` | Engine (physics, rendering, audio) | — |
| Game systems | `IGameSystem` | `assets/scripts/` | — |

## Built-in component types

**16** engine component types are registered for scene/prefab serialization (`ComponentSerializerRegistry`): transform, parent, 2D/3D renderers (sprite, subtexture, model, visibility zone), camera, ambient/directional/point lights, rigid body, box/circle/edge colliders, audio listener/source.

`TagComponent` and `IDComponent` exist in `SceneComponents/` for tests and internal use but are **not** registered for save/load and are **not** in the Add Component menu. Entity display names in the hierarchy edit `entity.Name`, not `TagComponent` ([Component Inspector](../editor/component-inspector.md#entity-name-hierarchy)).

Full property reference: [Component Inspector](../editor/component-inspector.md).

## IGameComponent

Custom component data you author for a game. Mark with `[SerializableComponent]`, implement `IGameComponent` and `Clone()`. Attach via **Add Component → Game Component** in Properties (or scaffold from the Content Browser on `assets/scripts/` — `GameComponentTemplates`).

- **Singleton state** on one entity — score, phase, grid arrays: [`SnakeGameComponent`](../../../games/Snake/assets/scripts/SnakeGameComponent.cs), [`FlappyBirdGameComponent`](../../../games/FlappyBird/assets/scripts/FlappyBirdGameComponent.cs)
- **Per-entity markers** — cell index, pipe slot: [`GridCellComponent`](../../../games/Snake/assets/scripts/GridCellComponent.cs), [`PipePairComponent`](../../../games/FlappyBird/assets/scripts/PipePairComponent.cs)

## IGameSystem

Batch game logic registered with `[Register(typeof(IGameSystem))]`. Implements `ISystem`: `Priority`, `OnInit`, `OnUpdate`, `OnShutdown`. Inject `IContext` for queries, `IKeyboardInput` / `IAudio` / `IPhysicsContacts` as needed (scaffold: `GameSystemTemplates`).

- [`SnakeSystem`](../../../games/Snake/assets/scripts/SnakeSystem.cs) — reads `SnakeGameComponent`, polls `IKeyboardInput`, updates every `GridCellComponent` visual
- [`FlappyBirdSystem`](../../../games/FlappyBird/assets/scripts/FlappyBirdSystem.cs) — simulates bird/pipes from `FlappyBirdGameComponent`, syncs transforms and score digits

Open samples: `games/Snake/`, `games/FlappyBird/` via **Project → Open…**.

## Editor workflow

Create entities in **Scene Hierarchy** → attach components via **Add Component** in Properties. One component per type per entity. Multi-select several entities to edit shared fields in bulk ([Scene Editor](../editor/scene-editor.md#multi-select-and-properties)).
