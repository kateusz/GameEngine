# Scenes and Prefabs

Understand how scenes organize your game world and how prefabs let you reuse entity templates.

## What is a Scene

A scene is a level, menu, or world. It contains all the entities that make up that part of your game. Each scene is saved as a human-readable JSON file with a `.scene` extension.

You might have separate scenes for your main menu, each game level, and a game over screen.

Scene files store `BackgroundColor`, `Dimension` (2D or 3D), and an `Entities` array. Each entity has an `Id`, `Name`, and a `Components` array. Prefabs use the same component JSON format without scene-level fields. See [Serialization](../../architecture/serialization.md) for the full schema.

## Scene Lifecycle

Scenes operate in two modes:

### Edit Mode

This is the default state when you open a scene in the editor.

- Manipulate entities freely (move, add components, delete)
- No physics simulation runs
- No scripts execute
- The editor camera controls the viewport

### Play Mode

Press the Play button to enter play mode.

- Physics simulation activates (gravity, collisions)
- Scripts execute (`OnCreate` is called, then `OnUpdate` every frame)
- The game camera (the entity with `CameraComponent` marked Primary) takes over the viewport
- This is what your game looks and feels like to players

Play requires an open project with an `assets/scripts/` directory. If no camera is marked Primary, the engine assigns the first camera it finds.

### Stopping

Press Stop to return to edit mode. The scene **reloads from the last saved file on disk**, discarding all runtime changes (physics movement, script state, spawned entities).

**Important:** Play snapshots the current editor state (including unsaved edits) into a temporary file before the simulation runs. Stop always reloads the **saved** scene path — not the in-memory state from before Play. Always save your scene (Ctrl+S) before pressing Play if you want Stop to return to that version.

## Scene Operations

| Action | How |
|--------|-----|
| New scene | Ctrl+N (opens a name/settings popup) |
| Save scene | Ctrl+S |
| Open scene | Drag a `.scene` file from the Content Browser onto the viewport |

## What is a Prefab

A prefab is a reusable entity template saved to disk as a `.prefab` file. Think of it as a blueprint: define an entity once with all its components and settings, then reuse it as many times as you need.

## Creating Prefabs

1. Select an entity with the components and settings you want
2. Click **Save as Prefab** in the Properties panel
3. Enter a name — the prefab is saved to `assets/prefabs/{name}.prefab`

## Using Prefabs

Drag a `.prefab` file from the Content Browser onto an **existing entity** in the Scene Hierarchy to apply the prefab's component data to that entity.

> **Planned:** Dragging a prefab to empty hierarchy space to spawn a new entity from the editor. The engine API `CreateEntityFromPrefab` exists in `PrefabSerializer` but is not wired to the hierarchy panel yet. See [Roadmap](../roadmap.md).

Prefab instantiation is currently an editor-only feature. There is no scripting API for spawning prefabs at runtime yet.

## When to Use Prefabs

Prefabs are ideal for anything you place multiple times:

- **Enemies** -- same components, same default settings, placed in different positions
- **Collectibles** -- coins, health packs, power-ups
- **Projectiles** -- bullets, arrows (template for spawning)
- **Environmental props** -- trees, rocks, crates

Using prefabs ensures consistency: every instance starts with the same components and default values.

## Next Steps

- [Scene Editor](../editor/scene-editor.md) -- working with the editor panels
- [Content Browser](../editor/content-browser.md) -- managing asset files
