# ScriptableEntity API Reference

Reference for per-entity **glue** scripts (`ScriptableEntity`). For batch game logic, input in systems, and shared state, see [Scripting Tiers](scripting-tiers.md) (`IGameComponent`, `IGameSystem`, `IKeyboardInput`, `IPhysicsContacts`).

All scripts extend `ScriptableEntity`. The methods below are available to every script.

## Lifecycle Methods

Override these to hook into the engine's update loop.

| Method | When Called |
|--------|------------|
| `void OnCreate()` | Once when play mode starts |
| `void OnUpdate(TimeSpan ts)` | Every frame (`ts` = time since last frame) |
| `void OnDestroy()` | When play mode stops |

## Input Events

Override these to handle player input.

| Method | When Called |
|--------|------------|
| `void OnKeyPressed(KeyCodes key)` | Key pressed down |
| `void OnKeyReleased(KeyCodes keyCode)` | Key released |
| `void OnMouseButtonPressed(int button)` | Mouse button pressed (0=left, 1=right, 2=middle) |
| `void OnMouseButtonReleased(int button)` | Mouse button released |
| `void OnMouseMoved(float x, float y)` | Cursor moved (window coordinates) |
| `void OnMouseScrolled(float xOffset, float yOffset)` | Scroll wheel moved |

See [Input Handling](input.md) for examples and the KeyCodes reference.

## Physics Events

Override these to react to collisions and triggers.

| Method | When Called |
|--------|------------|
| `void OnCollisionBegin(Entity other)` | Physical collision starts |
| `void OnCollisionEnd(Entity other)` | Physical collision ends |
| `void OnTriggerEnter(Entity other)` | Entity enters a trigger zone |
| `void OnTriggerExit(Entity other)` | Entity exits a trigger zone |

See [Physics](physics.md) for setup instructions and examples.

## Component Access

These protected methods let you work with components on the script's entity.

| Method | Description |
|--------|-------------|
| `T GetComponent<T>()` | Get a component by type. Throws if the entity does not have it. |
| `bool HasComponent<T>()` | Check whether the entity has a component of this type. |
| `T AddComponent<T>()` | Add a new component (created with parameterless constructor). |
| `void AddComponent<T>(T component)` | Add a pre-constructed component instance. |
| `void RemoveComponent<T>()` | Remove a component by type. |

**Type constraint:** `T` must implement `IComponent`.

Use `GetComponent<TransformComponent>()` for position, rotation, and scale — there are no `GetPosition` / `SetPosition` helpers on `ScriptableEntity`.

**Example:**

```csharp
if (HasComponent<SpriteRendererComponent>())
{
    var sprite = GetComponent<SpriteRendererComponent>();
    sprite.Color = new Vector4(1, 0, 0, 1); // Turn red
}
```

## Audio

Scripts receive `IAudio` and `IAudioPlayback` via the constructor scaffold (`ScriptableEntityTemplates`). Use the protected `AudioPlayback` property for per-entity play/pause/stop:

| Method | Description |
|--------|-------------|
| `AudioPlayback.Play(Entity entity)` | Play `AudioSourceComponent` on the entity |
| `AudioPlayback.Pause(Entity entity)` | Pause playback |
| `AudioPlayback.Stop(Entity entity)` | Stop playback |

Clip selection and source settings remain on `AudioSourceComponent` in the editor.

## Serialized Data

Script fields are **not** persisted in scene JSON. Put tunable values on `[SerializableComponent]` game components (`GameComponentTemplates` scaffolds new types). See [Getting Started](getting-started.md#data-and-the-inspector).

## Coming Soon

- Coroutine support for time-delayed execution
- Physics raycasting queries
