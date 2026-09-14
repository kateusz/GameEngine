# Scripting Tiers

| Tier | Type | Use for |
|------|------|---------|
| **Data** | `IGameComponent` + `[SerializableComponent]` | Serializable state; inspector fields |
| **Logic** | `IGameSystem` + `[Register]` | Input, physics, queries, win conditions, visual sync |

**Rule:** Data in components, rules in systems.

## Snake pattern

```
SnakeGameComponent / GridCellComponent  →  game state
SnakeSystem                             →  input, tick, visuals, audio
```

Per-entity reactions (door, pickup, water) are still systems: query components or `IPhysicsContacts.DrainContacts()`, write flags back onto components.

## Components

Health, score, inventory — anything saved in scene JSON. **Add Game Component** scaffolds via `GameComponentTemplates` (`{Name}Component`, `Clone()`, `[SerializableComponent]`).

## Systems

Turn order, win conditions, multi-entity updates. Register with `[Register(typeof(IGameSystem))]` (`GameIocLifetime`: `Singleton`, `Transient`, `Scoped`). Scaffold (`GameSystemTemplates`) uses `{Name}System` and `Priority => 100`. Common injections:

| Service | Use |
|---------|-----|
| `IContext` | Entity/component queries |
| `IKeyboardInput` | `IsKeyDown` / `WasKeyPressed` |
| `IPhysicsContacts` | `DrainContacts()` per frame |
| `IPhysicsQueries` | `Raycast` / `OverlapCircle` |
| `IAudio` | Play sounds |

Scaffold: Content Browser **Add System** (`GameSystemTemplates`).

## See also

- [Getting Started](getting-started.md)
- [API Reference](api-reference.md)
- [Scripting Lifecycle](../../architecture/scripting-lifecycle.md)
