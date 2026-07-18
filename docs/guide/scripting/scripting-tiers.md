# Scripting Tiers

| Tier | Type | Use for |
|------|------|---------|
| **Data** | `IGameComponent` + `[SerializableComponent]` | Serializable state; inspector fields |
| **Glue** | `ScriptableEntity` + `NativeScriptComponent` | Per-entity wiring, local reactions |
| **Logic** | `IGameSystem` + `[Register]` | Batch rules, queries, shared input/physics |

**Rule:** Data in components, glue in scripts, batch logic in systems.

## Snake pattern

```
SnakeGameComponent / GridCellComponent  →  game state
SnakeSystem                             →  input, tick, visuals, audio
```

For per-entity input callbacks instead of system polling, use a thin `ScriptableEntity` that writes intent flags onto a component; keep batch rules in an `IGameSystem`.

## Components

Health, score, inventory — anything saved in scene JSON. **Add Game Component** scaffolds via `GameComponentTemplates` (`Clone()`, `[SerializableComponent]`).

## Scripts

Camera controller, door trigger, event→component glue. **NativeScriptComponent → Create New Script**.

## Systems

Turn order, win conditions, multi-entity updates. Register with `[Register(typeof(IGameSystem))]` (`GameIocLifetime`: `Singleton` default). Common injections:

| Service | Use |
|---------|-----|
| `IContext` | Entity/component queries |
| `IKeyboardInput` | `IsKeyDown` / `WasKeyPressed` |
| `IPhysicsContacts` | `DrainContacts()` per frame |
| `IAudio` | Play sounds |

Scaffold: `GameSystemTemplates`.

## See also

- [Getting Started](getting-started.md)
- [API Reference](api-reference.md)
- [Scripting Lifecycle](../../architecture/scripting-lifecycle.md)
