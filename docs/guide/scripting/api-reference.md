# Game scripting API

Game C# is **data** (`IGameComponent`) plus **systems** (`IGameSystem`). Compile/load: [Scripting Lifecycle](../../architecture/scripting-lifecycle.md). When to use which: [Scripting Tiers](scripting-tiers.md).

## Systems

```csharp
[Register(typeof(IGameSystem))]
public class MySystem(IContext context, IKeyboardInput keyboard) : IGameSystem
{
    public int Priority => 115;

    public void OnInit() { }

    public void OnUpdate(TimeSpan deltaTime)
    {
        if (keyboard.WasKeyPressed(KeyCodes.Space))
            Console.WriteLine("jump");
    }

    public void OnShutdown() { }
}
```

Scaffold: Content Browser **Add System**. Injected services:

| Service | Use |
|---------|-----|
| `IContext` | `GetByName`, `View<T>()` |
| `IKeyboardInput` / `IMouseInput` | [Input](input.md) |
| `IPhysicsContacts` | `DrainContacts()` — [Physics](physics.md) |
| `IPhysicsQueries` | `Raycast` / `OverlapCircle` |
| `ICameraQueries` | `ScreenToWorld2D` |
| `IAudio` / `IAudioPlayback` | One-shots and `AudioSourceComponent` |

`IEntityHierarchy` (the active scene) for parent/child: `GetParent`, `GetChildren`, `SetParent`, `GetWorldPosition`.

## Components

`[SerializableComponent]` types implementing `IGameComponent` + `Clone()`. Fields show in the inspector and save in scene JSON.

## Queries

`RaycastHit2D`: `Entity`, `Point`, `Normal`, `Distance`, `IsTrigger`. World-space; default hits solids only (`includeTriggers: true` for triggers).
