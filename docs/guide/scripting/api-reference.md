# ScriptableEntity API Reference

Complete reference for all methods available in your game scripts.

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
| `void OnMouseButtonPressed(int button)` | Mouse button clicked (0=left, 1=right, 2=middle) |

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

**Example:**

```csharp
if (HasComponent<SpriteRendererComponent>())
{
    var sprite = GetComponent<SpriteRendererComponent>();
    sprite.Color = new Vector4(1, 0, 0, 1); // Turn red
}
```

## Entity Access

These protected methods let you find, create, and destroy entities in the scene.

| Method | Description |
|--------|-------------|
| `Entity? FindEntity(string name)` | Find an entity by name. Returns `null` if not found. |
| `Entity CreateEntity(string name)` | Create a new entity in the scene. |
| `void DestroyEntity(Entity entity)` | Destroy an entity and remove it from the scene. |

**Example:**

```csharp
var enemy = FindEntity("Boss");
if (enemy != null)
{
    DestroyEntity(enemy);
}
```

## Transform Helpers

These protected methods provide convenient access to the entity's `TransformComponent`.

| Method | Description |
|--------|-------------|
| `Vector3 GetPosition()` | Get world position. |
| `void SetPosition(Vector3 position)` | Set world position. |
| `Vector3 GetRotation()` | Get rotation in **radians** (X=pitch, Y=yaw, Z=roll). |
| `void SetRotation(Vector3 rotation)` | Set rotation in **radians**. |
| `Vector3 GetScale()` | Get scale. |
| `void SetScale(Vector3 scale)` | Set scale. |
| `Vector3 GetForward()` | Calculate forward direction vector from current rotation. |
| `Vector3 GetRight()` | Calculate right direction vector. |
| `Vector3 GetUp()` | Calculate up direction vector. |

**Important:** Rotation values are in **radians**, not degrees. To convert degrees to radians:

```csharp
float radians = MathF.PI / 180f * degrees;
```

## Audio Control

Audio playback is currently controlled through component properties in the editor (`PlayOnAwake`, `Loop`, `Volume`, etc.). Programmatic Play/Pause/Stop from scripts is not yet available as a public API.

## Exposed Field Types

Public fields and properties with public get/set of these types are automatically shown in the editor inspector:

`int`, `float`, `double`, `bool`, `string`, `Vector2`, `Vector3`, `Vector4`

**Example:**

```csharp
public class MyScript : ScriptableEntity
{
    public float speed = 5.0f;       // Editable in inspector
    public bool isEnabled = true;    // Editable in inspector
    public Vector3 offset;           // Editable in inspector
    private float _timer;            // NOT shown (private)
}
```

## Coming Soon

- Coroutine support for time-delayed execution
- Script-accessible audio playback API (Play/Pause/Stop)
- Physics raycasting queries
