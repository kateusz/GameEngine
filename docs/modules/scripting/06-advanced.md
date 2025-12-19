# Advanced Topics

## Exposed Fields System

Scripts can expose public fields to the editor via reflection.

### Reflection Caching
```csharp
// ScriptableEntity.cs
private Dictionary<string, FieldInfo>? _fieldCache;

public IEnumerable<(string Name, Type Type, object Value)> GetExposedFields()
{
    if (_fieldCache == null)
    {
        _fieldCache = GetType()
            .GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Where(f => IsSupportedFieldType(f.FieldType))
            .ToDictionary(f => f.Name);
    }

    return _fieldCache.Select(kvp => (
        kvp.Key,
        kvp.Value.FieldType,
        kvp.Value.GetValue(this)!
    ));
}
```

**Cached:** Field metadata cached on first access, reused for subsequent calls

### Supported Types
- **Primitives:** `int`, `float`, `double`, `bool`
- **String:** `string`
- **Vectors:** `Vector2`, `Vector3`, `Vector4`

**Extension:** To support additional types, modify `IsSupportedFieldType()` and `FieldEditorRegistry`

### Editor Integration
```csharp
public class PlayerController : ScriptableEntity
{
    public float Speed = 5.0f;            // Visible in inspector
    public Vector3 SpawnPoint = Vector3.Zero;
    private int _health = 100;            // NOT visible (private)
}
```

**Editor Display:** ScriptComponentEditor renders fields using `UIPropertyRenderer`

## Performance Considerations

### System Priority (110)
Scripts run **after Physics (100)**, **before Audio (120)**.

**Implications:**
- Physics results available in `OnUpdate()`
- Can modify audio sources before Audio system processes
- Transform changes visible to Animation (140) and Rendering (150+)

### Per-Frame Execution
**Every frame:**
- `ScriptUpdateSystem.OnUpdate()` called
- `ScriptEngine.OnUpdate()` delegates to all script instances
- Each `ScriptableEntity.OnUpdate(TimeSpan)` executed

**Optimization:**
- Keep `OnUpdate()` lightweight
- Avoid allocations (LINQ, string concatenation)
- Cache component references
- Use early exits for inactive scripts

### Component Query Optimization

**Bad:**
```csharp
public override void OnUpdate(TimeSpan ts)
{
    // GetComponent called every frame!
    var transform = GetComponent<TransformComponent>();
    transform.Translation += Vector3.UnitX;
}
```

**Good:**
```csharp
private TransformComponent? _transform;

public override void OnUpdate(TimeSpan ts)
{
    _transform ??= GetComponent<TransformComponent>();
    _transform.Translation += Vector3.UnitX;
}
```

**Trade-off:** Cached reference invalid after hot reload, but performance gain is significant.

### Memory Management

**Script Instance Lifetime:**
- Created when `NativeScriptComponent` added
- Destroyed when entity removed or scene stopped
- Replaced on hot reload

**No Automatic Cleanup:**
```csharp
public override void OnDestroy()
{
    // Manually dispose resources if needed
    _customResource?.Dispose();
}
```

**GC Pressure:** Minimize allocations in `OnUpdate()` to reduce garbage collection.

## Scene Context Access

Scripts access scene via injected `ISceneContext` (internal to `ScriptEngine`).

### Entity Queries
```csharp
protected Entity? FindEntity(string name)
{
    return _sceneContext.ActiveScene.Entities
        .FirstOrDefault(e => e.Name == name);
}
```

**Performance:** Linear search - cache results if called frequently.

### Creating Entities
```csharp
protected Entity CreateEntity(string name)
{
    return _sceneContext.ActiveScene.CreateEntity(name);
}
```

**Use Cases:**
- Spawning projectiles
- Instantiating enemies
- Creating particle effects

### Multi-Scene Scenarios
**Current:** Scripts operate on `ActiveScene` only.

**Future:** Multi-scene support would require scene parameter in API.

## Custom Serialization

### Current Approach
`NativeScriptComponent` stores script name as string:
```csharp
public string ScriptName { get; set; } = string.Empty;
```

**Serialization:** Only script name saved, not instance state.

### Serializing Script Fields
To persist script field values:

1. Extend `NativeScriptComponent` to store field data
2. Add `Dictionary<string, object>` for field values
3. Serialize/deserialize field values on scene save/load
4. Apply values via `SetFieldValue()` after instantiation

**Example:**
```csharp
// Extended component (hypothetical)
public Dictionary<string, object> FieldValues { get; set; } = new();

// On scene save
scriptComponent.FieldValues["Speed"] = script.GetFieldValue("Speed");

// On scene load
script.SetFieldValue("Speed", scriptComponent.FieldValues["Speed"]);
```

## Event Dispatching

### Input Events
```csharp
// ScriptEngine.cs
public void ProcessEvent(Event evt)
{
    if (evt is KeyPressedEvent keyPressed)
    {
        foreach (var script in activeScripts)
            script.OnKeyPressed(keyPressed.KeyCode);
    }
}
```

**Flow:** Input → Window → Editor/Runtime → ScriptEngine → All Scripts

### Physics Events
```csharp
// Physics system dispatches to scripts via ScriptEngine
scriptEngine.NotifyCollision(entityA, entityB, CollisionType.Begin);
```

**Note:** Requires `RigidBody2DComponent` on entity.

## Type Discovery

### Finding Script Types
```csharp
// ScriptEngine.cs
private void UpdateScriptTypes()
{
    _scriptTypes = _dynamicAssembly
        .GetTypes()
        .Where(t => t.IsSubclassOf(typeof(ScriptableEntity)) && !t.IsAbstract)
        .ToDictionary(t => t.Name);
}
```

**Registry:** `_scriptTypes` maps script name → `Type`

### Instantiation
```csharp
public Result<ScriptableEntity> CreateScriptInstance(string scriptName)
{
    if (!_scriptTypes.TryGetValue(scriptName, out var scriptType))
        return Result.Failure<ScriptableEntity>($"Script '{scriptName}' not found");

    var instance = Activator.CreateInstance(scriptType) as ScriptableEntity;
    instance.Entity = targetEntity;
    return Result.Success(instance);
}
```

**Error Handling:** Returns `Result<T>` for type safety.
