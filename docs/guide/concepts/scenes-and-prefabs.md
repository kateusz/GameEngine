# Scenes and Prefabs

A **scene** is a `.scene` JSON file: `BackgroundColor`, `Dimension` (`TwoD` / `ThreeD`), and `Entities[]`. Schema: [Serialization](../../architecture/serialization.md). Each scene owns its entities and systems. Play (`RuntimeSceneStarter`) sets `SceneState.Play` and runs `OnRuntimeStart` with the project's game systems.

## Edit vs Play

| | Edit | Play |
|---|------|------|
| Physics / scripts | Off | On (`OnRuntimeStart` then `OnUpdateRuntime`) |
| Viewport camera | Editor | Primary `CameraComponent` |

Play needs a project with `assets/scripts/`. No Primary camera → engine picks the first camera found. No camera at all → nothing renders.

**Stop reloads the saved file on disk** — runtime changes are discarded. Play snapshots in-memory state (including unsaved edits) into a temp file first; Stop still reloads the **saved** path. **Ctrl+S before Play** if you want Stop to return to what you see.

| Action | Shortcut |
|--------|----------|
| New | Ctrl+N |
| Save | Ctrl+S |
| Open | Drag `.scene` onto viewport |

## Hierarchy

Entities can have a parent (`ParentComponent` in scene JSON). Child local transforms multiply with the parent world matrix (row-vector: `local * parentWorld`). After load, `RebuildHierarchyIndex` rebuilds the children index and detaches orphans and cycles to root.

| Scene API | Behavior |
|-----------|----------|
| `GetRootEntities` / `GetChildren` / `GetParent` | Tree walk |
| `SetParent` | Rejects missing entities, self-parent, and cycles; same parent is a no-op (keeps sibling order) |
| `DestroyEntity` | Destroys descendants first |
| `DuplicateEntity` | Clones the subtree and remaps internal parent ids |
| `UpdateWorldTransforms` | Depth-first world matrices from roots — call before reading world position in edit mode |
| `CollectSubtree` | Root plus descendants, parent-before-child |

The editor hierarchy panel shows the tree and supports drag-reparent. Scripts use `IEntityHierarchy` on the scene — [API Reference](../scripting/api-reference.md).

## Cameras and viewport

`IScene.CameraQueries.ScreenToWorld2D` maps a window position into the primary camera's Z=0 plane. `OnViewportResize` updates each camera's aspect ratio unless `FixedAspectRatio` is set. `SetPrimaryCamera` / `GetPrimaryCameraEntity` keep a single Primary flag.

## Prefabs

`.prefab` files under `assets/prefabs/` — same component JSON as scenes, no scene-level fields. Parent references serialize with the entity tree.

1. Select entity → **Save as Prefab** → name
2. Drag `.prefab` onto an **existing** hierarchy entity to apply its components

`CreateEntityFromPrefab` exists in code but hierarchy drag-to-spawn is not wired yet. No runtime prefab API yet — editor only.
