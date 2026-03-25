# Scenes and Prefabs

Understand how scenes organize your game world and how prefabs let you reuse entity templates.

## What is a Scene

A scene is a level, menu, or world. It contains all the entities that make up that part of your game. Each scene is saved as a human-readable JSON file with a `.scene` extension.

You might have separate scenes for your main menu, each game level, and a game over screen.

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

### Stopping

Press Stop to return to edit mode. The scene **reloads from the last saved file**, discarding all runtime changes (physics movement, script state, spawned entities).

**Important:** Always save your scene (Ctrl+S) before pressing Play. Stop reverts to the saved state on disk.

## Scene Operations

| Action | How |
|--------|-----|
| New scene | Ctrl+N |
| Save scene | Ctrl+S |
| Open scene | Double-click a `.scene` file in the Content Browser |

## What is a Prefab

A prefab is a reusable entity template saved to disk as a `.prefab` file. Think of it as a blueprint: define an entity once with all its components and settings, then reuse it as many times as you need.

## Creating Prefabs

1. Set up an entity in the editor with all the components and settings you want
2. Right-click the entity in the Scene Hierarchy
3. Select "Save as Prefab"
4. The prefab is saved to your project's `assets/prefabs/` directory

## Using Prefabs

Drag a `.prefab` file from the Content Browser into the Scene Hierarchy to instantiate it as a new entity.

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
