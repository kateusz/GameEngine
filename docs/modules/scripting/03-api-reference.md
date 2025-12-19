# API Reference

Complete reference for `ScriptableEntity` base class (`Engine/Scene/ScriptableEntity.cs`).

## Lifecycle Methods

### OnCreate()
```csharp
public virtual void OnCreate()
```
Called once when script instance is created. Use for initialization.

**Example:**
```csharp
public override void OnCreate()
{
    _startPosition = GetPosition();
    _health = 100;
}
```

### OnUpdate(TimeSpan)
```csharp
public virtual void OnUpdate(TimeSpan ts)
```
Called every frame. `ts` is delta time since last frame.

**Parameters:**
- `ts` - TimeSpan delta time

**Example:**
```csharp
public override void OnUpdate(TimeSpan ts)
{
    float deltaTime = (float)ts.TotalSeconds;
    // Update logic here
}
```

### OnDestroy()
```csharp
public virtual void OnDestroy()
```
Called when entity is destroyed or scene stops. Use for cleanup.

**Example:**
```csharp
public override void OnDestroy()
{
    // Release resources, save state, etc.
}
```

## Input Events

### OnKeyPressed(KeyCodes)
```csharp
public virtual void OnKeyPressed(KeyCodes key)
```
Called when a key is pressed down.

**Parameters:**
- `key` - The key code that was pressed

### OnKeyReleased(KeyCodes)
```csharp
public virtual void OnKeyReleased(KeyCodes keyCode)
```
Called when a key is released.

**Parameters:**
- `keyCode` - The key code that was released

### OnMouseButtonPressed(int)
```csharp
public virtual void OnMouseButtonPressed(int button)
```
Called when a mouse button is pressed.

**Parameters:**
- `button` - Mouse button index (0 = left, 1 = right, 2 = middle)

## Physics Events

### OnCollisionBegin(Entity)
```csharp
public virtual void OnCollisionBegin(Entity other)
```
Called when collision starts with another entity (requires `RigidBody2DComponent`).

**Parameters:**
- `other` - The entity this entity collided with

### OnCollisionEnd(Entity)
```csharp
public virtual void OnCollisionEnd(Entity other)
```
Called when collision ends with another entity.

**Parameters:**
- `other` - The entity collision ended with

### OnTriggerEnter(Entity)
```csharp
public virtual void OnTriggerEnter(Entity other)
```
Called when entering a trigger collider.

**Parameters:**
- `other` - The entity that entered the trigger

### OnTriggerExit(Entity)
```csharp
public virtual void OnTriggerExit(Entity other)
```
Called when exiting a trigger collider.

**Parameters:**
- `other` - The entity that exited the trigger

## Component Access

### GetComponent\<T\>()
```csharp
protected T GetComponent<T>() where T : IComponent
```
Retrieves component of type `T` from this entity.

**Returns:** Component instance

**Throws:** Exception if component not found

**Example:**
```csharp
var transform = GetComponent<TransformComponent>();
transform.Translation = new Vector3(10, 5, 0);
```

### HasComponent\<T\>()
```csharp
protected bool HasComponent<T>() where T : IComponent
```
Checks if entity has component of type `T`.

**Returns:** `true` if component exists, otherwise `false`

**Example:**
```csharp
if (HasComponent<Sprite2DComponent>())
{
    var sprite = GetComponent<Sprite2DComponent>();
    sprite.Color = Vector4.One;
}
```

### AddComponent\<T\>()
```csharp
protected T AddComponent<T>() where T : IComponent, new()
```
Adds new component of type `T` to entity.

**Returns:** Newly created component

**Example:**
```csharp
var rigidbody = AddComponent<RigidBody2DComponent>();
rigidbody.BodyType = BodyType.Dynamic;
```

### AddComponent\<T\>(T)
```csharp
protected void AddComponent<T>(T component) where T : IComponent
```
Adds existing component instance to entity.

**Parameters:**
- `component` - Component instance to add

### RemoveComponent\<T\>()
```csharp
protected void RemoveComponent<T>() where T : IComponent
```
Removes component of type `T` from entity.

**Example:**
```csharp
RemoveComponent<Sprite2DComponent>();
```

## Entity Operations

### FindEntity(string)
```csharp
protected Entity? FindEntity(string name)
```
Finds entity by name in current scene.

**Parameters:**
- `name` - Entity name to search for

**Returns:** Entity if found, otherwise `null`

**Example:**
```csharp
var player = FindEntity("Player");
if (player.HasValue)
{
    // Do something with player entity
}
```

### CreateEntity(string)
```csharp
protected Entity CreateEntity(string name)
```
Creates new entity in current scene.

**Parameters:**
- `name` - Name for the new entity

**Returns:** Newly created entity

**Example:**
```csharp
var projectile = CreateEntity("Projectile");
projectile.AddComponent<Sprite2DComponent>();
```

### DestroyEntity(Entity)
```csharp
protected void DestroyEntity(Entity entity)
```
Destroys specified entity.

**Parameters:**
- `entity` - Entity to destroy

## Transform Helpers

### GetPosition()
```csharp
protected Vector3 GetPosition()
```
Gets entity's world position from `TransformComponent`.

**Returns:** Position as Vector3

### SetPosition(Vector3)
```csharp
protected void SetPosition(Vector3 position)
```
Sets entity's world position.

**Parameters:**
- `position` - New position

### GetRotation()
```csharp
protected Vector3 GetRotation()
```
Gets entity's rotation (Euler angles).

**Returns:** Rotation as Vector3

### SetRotation(Vector3)
```csharp
protected void SetRotation(Vector3 rotation)
```
Sets entity's rotation (Euler angles).

**Parameters:**
- `rotation` - New rotation

### GetScale()
```csharp
protected Vector3 GetScale()
```
Gets entity's scale.

**Returns:** Scale as Vector3

### SetScale(Vector3)
```csharp
protected void SetScale(Vector3 scale)
```
Sets entity's scale.

**Parameters:**
- `scale` - New scale

### GetForward()
```csharp
protected Vector3 GetForward()
```
Gets entity's forward direction vector.

**Returns:** Normalized forward vector

### GetRight()
```csharp
protected Vector3 GetRight()
```
Gets entity's right direction vector.

**Returns:** Normalized right vector

### GetUp()
```csharp
protected Vector3 GetUp()
```
Gets entity's up direction vector.

**Returns:** Normalized up vector

## Serialization & Reflection

### GetExposedFields()
```csharp
public IEnumerable<(string Name, Type Type, object Value)> GetExposedFields()
```
Gets all public fields for editor display.

**Returns:** Enumerable of field metadata tuples

**Supported Types:**
- `int`, `float`, `double`, `bool`, `string`
- `Vector2`, `Vector3`, `Vector4`

### GetFieldValue(string)
```csharp
public object GetFieldValue(string name)
```
Gets value of public field by name.

**Parameters:**
- `name` - Field name

**Returns:** Field value as object

### SetFieldValue(string, object)
```csharp
public void SetFieldValue(string name, object value)
```
Sets value of public field by name.

**Parameters:**
- `name` - Field name
- `value` - New value

**Example:**
```csharp
// In script class
public float Speed = 5.0f;
public Vector3 TargetPosition = Vector3.Zero;

// Editor can get/set these values
var speed = GetFieldValue("Speed"); // Returns 5.0f
SetFieldValue("Speed", 10.0f);
```

## Properties

### Entity
```csharp
public Entity Entity { get; internal set; }
```
The entity this script is attached to. Read-only from script perspective.
