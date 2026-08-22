# Scenes and Prefabs

A **scene** is a `.scene` JSON file: `BackgroundColor`, `Dimension`, and `Entities[]`. Schema: [Serialization](../../architecture/serialization.md).

## Edit vs Play

| | Edit | Play |
|---|------|------|
| Physics / scripts | Off | On |
| Viewport camera | Editor | Primary `CameraComponent` |

Play needs a project with `assets/scripts/`. No Primary camera → engine picks the first camera found.

**Stop reloads the saved file on disk** — runtime changes are discarded. Play snapshots in-memory state (including unsaved edits) into a temp file first; Stop still reloads the **saved** path. **Ctrl+S before Play** if you want Stop to return to what you see.

| Action | Shortcut |
|--------|----------|
| New | Ctrl+N |
| Save | Ctrl+S |
| Open | Drag `.scene` onto viewport |

## Prefabs

`.prefab` files under `assets/prefabs/` — same component JSON as scenes, no scene-level fields. Parent references serialize with the entity tree.

1. Select entity → **Save as Prefab** → name
2. Drag `.prefab` onto an **existing** hierarchy entity to apply its components

`CreateEntityFromPrefab` exists in code but hierarchy drag-to-spawn is not wired yet ([Roadmap](../roadmap.md)). No runtime prefab API yet — editor only.

## Hierarchy

Entities can have a parent (`ParentComponent` in scene JSON). Child transforms compose with the parent world matrix. The editor hierarchy panel shows the tree and supports drag-reparent. Scripts can use `Parent`, `Children`, and `SetParent` on `ScriptableEntity` — [API Reference](../scripting/api-reference.md#hierarchy).
