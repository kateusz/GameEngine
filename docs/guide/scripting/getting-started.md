# Scripting Getting Started

See [Scripting Tiers](scripting-tiers.md) for components vs systems.

Game logic is C# under `assets/scripts/`. The editor compiles it to `GameAssembly_{guid}.dll` under `.engine/` (`GameAssemblyCompiler.GetNextEditorBuildPath`). Recompile on project open, Content Browser create/edit, and **Play**. External `.cs` saves are not watched — [Scripting Lifecycle](../../architecture/scripting-lifecycle.md).

## Create a system

1. Content Browser → right-click `assets/scripts/` → **Add System**
2. Scaffold names the class `{Name}System`, with `[Register(typeof(IGameSystem))]`, `Priority => 100`, and `IContext` + `IKeyboardInput` injected
3. Put saved fields on an `IGameComponent` (**Add Component** → `{Name}Component`), not on the system

## Lifecycle

| Method | When |
|--------|------|
| `OnInit()` | Play starts (`RuntimeSceneStarter`) |
| `OnUpdate(TimeSpan deltaTime)` | Each frame — `(float)deltaTime.TotalSeconds` for delta |
| `OnShutdown()` | Play stops |

Input: inject `IKeyboardInput` — [Input](input.md).

## Inspector data

System fields are **not** serialized in scenes. Put tunable values on `[SerializableComponent]` game components.

## Debugging

`Console.WriteLine()` → editor Console panel.
