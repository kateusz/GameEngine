# Scripting Getting Started

See [Scripting Tiers](scripting-tiers.md) for components vs scripts vs systems.

## Scripts

`ScriptableEntity` subclasses are per-entity glue — input on one object, wiring between components, small local behavior. Shared state belongs in `IGameComponent`; batch logic in `IGameSystem`.

Scripts compile to `GameAssembly_{guid}.dll` under `.engine/` (`GameAssemblyCompiler.GetNextEditorBuildPath`). The editor recompiles on project open, script create/edit/delete via editor UI, and before **Play**. External `.cs` saves are not watched — see [Scripting Lifecycle](../../architecture/scripting-lifecycle.md).

New scripts scaffold from `ScriptableEntityTemplates` (injects `IComponentAccessor`, `IAudio`, `IAudioPlayback`, `IPhysicsQueries`).

## Create and attach

1. Add `NativeScriptComponent` to an entity (Properties panel or Content Browser **Add Script**)
2. **Create New Script** → file under `assets/scripts/`
3. Set `ScriptTypeName` to the class name (e.g. `MyScript`)

## Lifecycle

| Method | When |
|--------|------|
| `OnCreate()` | Play starts |
| `OnUpdate(TimeSpan ts)` | Each frame — use `(float)ts.TotalSeconds` for delta |
| `OnDestroy()` | Play stops |

Input: override `OnKeyPressed`, `OnMouseButtonPressed`, etc. — [Input](input.md). Scene-wide input: `IGameSystem` + `IKeyboardInput`.

## Inspector data

Script fields are **not** serialized in scenes. Put tunable values on `[SerializableComponent]` game components.

## Debugging

`Console.WriteLine()` → editor Console panel.
