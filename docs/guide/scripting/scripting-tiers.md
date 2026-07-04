# Scripting Tiers

Game logic in this engine uses three tiers, similar to Unity's components, `MonoBehaviour` scripts, and ECS systems.

| Tier | Type | Use for |
|------|------|---------|
| **Data** | `IGameComponent` + `[SerializableComponent]` | Serializable state on entities; inspector-edited fields |
| **Glue** | `ScriptableEntity` + `NativeScriptComponent` | Per-entity wiring, entity-local reactions, bridging to components |
| **Logic** | `IGameSystem` + `[Register]` | Batch rules, queries over `IContext`, input via `IKeyboardInput`, physics via `IPhysicsContacts` |

**Rule:** Put data in game components, not on script fields. Scripts are glue; systems own batch logic.

## Reference: TicTacToe

```
BoardComponent / CellComponent  →  serializable board state (data)
TicTacToeSystem                 →  rules, visuals, keyboard input (logic)
```

Input is handled in `TicTacToeSystem` with `IKeyboardInput.WasKeyPressed` — no script shim forwarding keys into component mailboxes.

## When to use each tier

### Game components (`IGameComponent`)

- Health, score, inventory, AI state
- Anything saved in the scene JSON
- Fields edited in the Properties panel

Create via **Add Game Component** in the editor.

### Scripts (`ScriptableEntity`)

- Camera controller on one entity
- Door that reacts to a trigger on itself
- Small glue between engine events and component state

Create via **NativeScriptComponent → Create New Script**.

Scripts receive input through lifecycle overrides (`OnKeyPressed`, etc.). For gameplay that spans many entities, prefer a game system.

### Game systems (`IGameSystem`)

- Game rules (turn order, win conditions)
- Systems that update many entities from one query
- Input that affects global or shared state

Register with `[Register(typeof(IGameSystem))]` and inject:

- `IContext` — entity/component queries
- `IKeyboardInput` — `IsKeyDown` / `WasKeyPressed`
- `IPhysicsContacts` — `DrainContacts()` for collision/trigger events each frame
- `IAudio` — play sounds from systems

## Further reading

- [Getting Started](getting-started.md) — create your first script
- [API Reference](api-reference.md) — `ScriptableEntity` methods
- [Architecture: Scripting Lifecycle](../../architecture/scripting-lifecycle.md)
