# ScriptableEntity API Reference

Per-entity glue scripts. Batch logic: `IGameSystem` — [Scripting Tiers](scripting-tiers.md). Compile/load pipeline: [Scripting Lifecycle](../../architecture/scripting-lifecycle.md).

## Setup

Subclass `ScriptableEntity`. Required constructor (scaffolded by `ScriptableEntityTemplates`):

```csharp
public MyScript(
    IComponentAccessor componentAccessor,
    IAudio audio,
    IAudioPlayback audioPlayback,
    IPhysicsQueries physicsQueries,
    IEntityHierarchy hierarchy)
    : base(componentAccessor, audio, audioPlayback, physicsQueries, hierarchy) { }
```

Attach via `NativeScriptComponent.ScriptTypeName` = class name (e.g. `MyScript`). Only the type name is saved in scene JSON.

| Member | Description |
|--------|-------------|
| `bool IsInitialized` | `true` after the engine calls `SetEntity` on first play frame |

## Lifecycle

| Method | When |
|--------|------|
| `void OnCreate()` | First play frame, before first `OnUpdate` |
| `void OnUpdate(TimeSpan ts)` | Each frame — `(float)ts.TotalSeconds` for delta |
| `void OnDestroy()` | Play stops or scene unloads |

Exceptions in overrides are logged; play continues. After hot reload, instances are recreated and `OnCreate` runs again.

## Input

| Method | When |
|--------|------|
| `void OnKeyPressed(KeyCodes key)` | Key down |
| `void OnKeyReleased(KeyCodes keyCode)` | Key up |
| `void OnMouseButtonPressed(int button)` | Button down (0/1/2) |
| `void OnMouseButtonReleased(int button)` | Button up |
| `void OnMouseMoved(float x, float y)` | Cursor move (window coords) |
| `void OnMouseScrolled(float xOffset, float yOffset)` | Scroll |

**Every** `ScriptableEntity` on the active scene receives each input event (broadcast). Filter in your handler or use `IKeyboardInput` in a system — [Input](input.md).

## Physics events

| Method | When |
|--------|------|
| `void OnCollisionBegin(Entity other)` | Solid contact starts |
| `void OnCollisionEnd(Entity other)` | Solid contact ends |
| `void OnTriggerEnter(Entity other)` | Trigger overlap starts |
| `void OnTriggerExit(Entity other)` | Trigger overlap ends |

`other` is the other entity (`Name`, `Id`). Callbacks fire only on entities with `NativeScriptComponent` and a live script instance. Setup: [Physics](physics.md).

## Components

Host entity only — `T : IComponent` (engine components and `IGameComponent`). No `CreateEntity`, `FindEntity`, or `other.GetComponent<T>()`.

| Method | Description |
|--------|-------------|
| `T GetComponent<T>()` | Get; throws if missing |
| `bool HasComponent<T>()` | Presence check |
| `T AddComponent<T>()` | Add (`new()` required) |
| `void AddComponent<T>(T component)` | Add instance |
| `void RemoveComponent<T>()` | Remove |

Position/rotation/scale: `GetComponent<TransformComponent>()`. One component per type per entity.

## Hierarchy

Requires an active scene (`IEntityHierarchy`). No-ops / empty when the script is not initialized.

| Member | Description |
|--------|-------------|
| `Entity? Parent` | Parent entity, or null if scene root |
| `IReadOnlyList<Entity> Children` | Direct children |
| `bool SetParent(Entity? parent)` | Reparent; null detaches to root |
| `Entity? GetChild(string name)` | First direct child with the given name |
| `Vector3 WorldPosition` | World-space translation from the cached world transform |

## Audio

Protected properties from constructor injection:

| API | Description |
|-----|-------------|
| `Audio.PlayOneShot(string clipPath, float volume = 1.0f)` | One-shot by asset path |
| `AudioPlayback.Play(Entity entity)` | Play `AudioSourceComponent` on entity |
| `AudioPlayback.Pause(Entity entity)` | Pause |
| `AudioPlayback.Stop(Entity entity)` | Stop |

Clip and source settings on `AudioSourceComponent` in the editor.

## Physics queries

World-space coordinates (not offset by entity transform). Returns `null` when no hit, no physics world, or script not initialized.

| Method | Description |
|--------|-------------|
| `RaycastHit2D? Raycast(Vector2 origin, Vector2 direction, float maxDistance, bool includeTriggers = false)` | Closest hit along ray; ignores self |
| `RaycastHit2D? OverlapCircle(Vector2 center, float radius, bool includeTriggers = false)` | One overlap; order unspecified if several |

Default: solids only. `includeTriggers: true` for trigger colliders. Synchronous — no collision callbacks. Details: [Physics](physics.md#queries).

`RaycastHit2D`: `Entity`, `Point`, `Normal`, `Distance`, `IsTrigger`.

## Not available on scripts

| Limit | Workaround |
|-------|------------|
| Script fields not serialized | `[SerializableComponent]` game components |
| No entity create/destroy/find | `IGameSystem` + `IContext` |
| No `other.GetComponent<T>()` in physics callbacks | Compare `other.Name` or use a game component/system |
| Cannot destroy entities | Remove/hide components (see [Physics](physics.md#example-pickup)) |
