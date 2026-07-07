# Scripting Getting Started

Get from zero to a working game script.

See [Scripting Tiers](scripting-tiers.md) for when to use scripts vs game components vs game systems.

## What is a Script

A script is a C# class that extends `ScriptableEntity`. Scripts are **per-entity glue**: input on one object, wiring between components, small local behavior. Put shared state in `IGameComponent` types and batch logic in `IGameSystem` classes.

Scripts compile into a versioned `GameAssembly_{guid}.dll` under your project's `.engine/` folder (`GameAssemblyCompiler.GetNextEditorBuildPath`). Saving or creating a script in the editor triggers a recompile and reload — see [Scripting Lifecycle](../../architecture/scripting-lifecycle.md) for the full pipeline.

New scripts use the `ScriptableEntity` scaffold from `ScriptableEntityTemplates` (constructor: `IComponentAccessor`, `IAudio`, `IAudioPlayback`).

## Creating a Script

1. Select an entity in the Scene Hierarchy
2. In the Properties panel, add `NativeScriptComponent`
3. Click **Create New Script** and enter a name
4. The engine writes a template under `assets/scripts/`

## Attaching Scripts to Entities

Set `ScriptTypeName` on `NativeScriptComponent` to your class name (e.g. `MyScript`). The engine instantiates the script when play mode starts.

## Lifecycle Methods

| Method | When Called | Use For |
|--------|------------|---------|
| `OnCreate()` | Once when play starts | Cache components, one-time setup |
| `OnUpdate(TimeSpan ts)` | Every frame | Per-entity update glue |
| `OnDestroy()` | When play stops | Cleanup |

Use `(float)ts.TotalSeconds` for delta time in seconds.

## Input on Scripts

Override `OnKeyPressed`, `OnMouseButtonPressed`, etc. See [Input Handling](input.md).

For input that drives game rules across the scene, use `IGameSystem` with `IKeyboardInput` instead.

## Hot Reload

Save a `.cs` file under `assets/scripts/` and the editor recompiles to a new `GameAssembly_{guid}.dll` and reloads it. Use **Force Recompile** from the script component UI if needed.

## Data and the Inspector

Script fields are **not** serialized in scenes. Put tunable data on `[SerializableComponent]` game components and edit them in the Properties panel.

## Debugging

Use `Console.WriteLine()` — output appears in the editor Console panel.

## Next Steps

- [Scripting Tiers](scripting-tiers.md) — components, scripts, systems
- [Input Handling](input.md)
- [Physics](physics.md)
- [API Reference](api-reference.md)
