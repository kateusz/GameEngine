# Best Practices

## Component Over State

**Principle:** Store persistent data in components, not script fields.

### Why?
- Hot reload resets script instances
- Components survive scene save/load
- ECS architecture encourages data separation

### Example

**Bad:**
```csharp
public class Player : ScriptableEntity
{
    private int _health = 100;
    private int _score = 0;

    // Lost on hot reload!
}
```

**Good:**
```csharp
// Create custom components
public class HealthComponent : IComponent
{
    public int CurrentHealth { get; set; } = 100;
    public int MaxHealth { get; set; } = 100;
}

public class Player : ScriptableEntity
{
    public override void OnCreate()
    {
        if (!HasComponent<HealthComponent>())
            AddComponent(new HealthComponent());
    }

    public override void OnUpdate(TimeSpan ts)
    {
        var health = GetComponent<HealthComponent>();
        // Health persists across hot reloads
    }
}
```

## Avoid Static State

**Principle:** No static singletons, use dependency injection.

### Why?
- Breaks hot reload (static state persists)
- Violates engine architecture (DI everywhere)
- Hard to test and reason about

### Example

**Bad:**
```csharp
public class GameManager : ScriptableEntity
{
    public static GameManager Instance; // NO!

    public override void OnCreate()
    {
        Instance = this;
    }
}
```

**Good:**
```csharp
// Use FindEntity or scene context
public class GameManager : ScriptableEntity
{
    public int CurrentLevel { get; set; }
}

public class Player : ScriptableEntity
{
    public override void OnUpdate(TimeSpan ts)
    {
        var manager = FindEntity("GameManager");
        if (manager.HasValue && manager.Value.HasComponent<NativeScriptComponent>())
        {
            var script = manager.Value.GetComponent<NativeScriptComponent>().ScriptableEntity as GameManager;
            // Access manager data
        }
    }
}
```

## Use Constants

**Principle:** No magic numbers, use named constants.

### Example

**Bad:**
```csharp
public override void OnUpdate(TimeSpan ts)
{
    SetPosition(GetPosition() + Vector3.UnitX * 5.0f); // What is 5.0?
}
```

**Good:**
```csharp
private const float MoveSpeed = 5.0f;

public override void OnUpdate(TimeSpan ts)
{
    SetPosition(GetPosition() + Vector3.UnitX * MoveSpeed);
}
```

**Better (Editor-Editable):**
```csharp
public float MoveSpeed = 5.0f; // Exposed in inspector

public override void OnUpdate(TimeSpan ts)
{
    SetPosition(GetPosition() + Vector3.UnitX * MoveSpeed);
}
```

## Resource Management

**Principle:** Dispose resources in `OnDestroy()`.

### Example

```csharp
public class AudioPlayer : ScriptableEntity
{
    private IDisposable? _customResource;

    public override void OnCreate()
    {
        // Acquire resources
        _customResource = CreateSomeResource();
    }

    public override void OnDestroy()
    {
        // Clean up
        _customResource?.Dispose();
        _customResource = null;
    }
}
```

**Note:** Most engine resources (textures, audio) are managed by factories. Only dispose custom allocations.

## Cache Component References

**Principle:** Avoid repeated `GetComponent<T>()` calls.

### Performance Impact

**Slow:**
```csharp
public override void OnUpdate(TimeSpan ts)
{
    // Called 60 times per second!
    var transform = GetComponent<TransformComponent>();
    transform.Translation += Vector3.UnitX;
}
```

**Fast:**
```csharp
private TransformComponent? _transform;

public override void OnUpdate(TimeSpan ts)
{
    _transform ??= GetComponent<TransformComponent>();
    _transform.Translation += Vector3.UnitX;
}
```

**Trade-off:** Reference invalid after hot reload, but 60x faster.

## Minimize OnUpdate Allocations

**Principle:** Avoid allocations in per-frame methods.

### Common Pitfalls

**Allocating:**
```csharp
public override void OnUpdate(TimeSpan ts)
{
    var entities = _scene.Entities.Where(e => e.Name.StartsWith("Enemy")).ToList(); // Allocation!
    string message = $"Entities: {entities.Count}"; // String allocation!
}
```

**Zero-Allocation:**
```csharp
private int _enemyCount = 0;

public override void OnUpdate(TimeSpan ts)
{
    _enemyCount = 0;
    foreach (var entity in _scene.Entities)
    {
        if (entity.Name.StartsWith("Enemy"))
            _enemyCount++;
    }
}
```

### Avoid LINQ in Hot Paths
```csharp
// Bad (allocates enumerators)
var enemies = entities.Where(e => e.HasComponent<EnemyAI>()).ToList();

// Good (manual iteration)
foreach (var entity in entities)
{
    if (entity.HasComponent<EnemyAI>())
    {
        // Process enemy
    }
}
```

## Check Component Existence

**Principle:** Always verify components exist before access.

### Example

**Unsafe:**
```csharp
var sprite = GetComponent<Sprite2DComponent>(); // Throws if missing!
sprite.Color = Vector4.One;
```

**Safe:**
```csharp
if (HasComponent<Sprite2DComponent>())
{
    var sprite = GetComponent<Sprite2DComponent>();
    sprite.Color = Vector4.One;
}
```

**Pattern:**
```csharp
public override void OnCreate()
{
    // Ensure required components exist
    if (!HasComponent<TransformComponent>())
        AddComponent<TransformComponent>();

    if (!HasComponent<Sprite2DComponent>())
        AddComponent<Sprite2DComponent>();
}
```

## Initialization in OnCreate

**Principle:** Use `OnCreate()` for initialization, not constructors.

### Why?
- `Entity` property not set in constructor
- Cannot access components in constructor
- Constructor exceptions harder to debug

### Example

**Bad:**
```csharp
public class Player : ScriptableEntity
{
    private Vector3 _startPosition;

    public Player()
    {
        _startPosition = GetPosition(); // NRE! Entity not set yet
    }
}
```

**Good:**
```csharp
public class Player : ScriptableEntity
{
    private Vector3 _startPosition;

    public override void OnCreate()
    {
        _startPosition = GetPosition(); // Safe
    }
}
```

## Keep OnCreate Lightweight

**Principle:** Avoid expensive operations in `OnCreate()`.

### Why?
- Called on every hot reload
- Blocks script initialization
- Compilation already takes time

### Example

**Bad:**
```csharp
public override void OnCreate()
{
    // Expensive!
    for (int i = 0; i < 1000; i++)
    {
        CreateEntity($"Particle{i}");
    }
}
```

**Good:**
```csharp
private bool _initialized = false;

public override void OnUpdate(TimeSpan ts)
{
    if (!_initialized)
    {
        // Expensive initialization once, not on every reload
        _initialized = true;
    }
}
```

## Use Early Returns

**Principle:** Exit early from `OnUpdate()` if no work needed.

### Example

**Less Efficient:**
```csharp
public override void OnUpdate(TimeSpan ts)
{
    if (_isActive)
    {
        // 100 lines of logic
    }
}
```

**More Efficient:**
```csharp
public override void OnUpdate(TimeSpan ts)
{
    if (!_isActive) return; // Early exit

    // 100 lines of logic
}
```

## Composition Over Inheritance

**Principle:** Favor small, focused scripts over large hierarchies.

### Example

**Bad:**
```csharp
public class Character : ScriptableEntity { /* base logic */ }
public class Player : Character { /* player logic */ }
public class Enemy : Character { /* enemy logic */ }
// Deep inheritance hierarchy, hard to hot reload
```

**Good:**
```csharp
public class MovementController : ScriptableEntity { /* movement only */ }
public class HealthSystem : ScriptableEntity { /* health only */ }
public class InputHandler : ScriptableEntity { /* input only */ }

// Entity has multiple small scripts, each focused
```

## Validate Entity References

**Principle:** Check entity validity before use.

### Example

```csharp
private Entity? _target;

public override void OnCreate()
{
    _target = FindEntity("Target");
}

public override void OnUpdate(TimeSpan ts)
{
    // Validate before use
    if (_target == null || !_target.HasValue)
    {
        _target = FindEntity("Target"); // Re-search
        if (_target == null) return; // Still not found
    }

    // Safe to use _target
}
```

## Script Organization

**Principle:** One script per file, clear naming.

### File Structure
```
assets/scripts/
├── Player/
│   ├── PlayerController.cs
│   ├── PlayerHealth.cs
│   └── PlayerInventory.cs
├── Enemy/
│   ├── EnemyAI.cs
│   └── EnemySpawner.cs
└── Systems/
    ├── GameManager.cs
    └── ScoreTracker.cs
```

**Naming:**
- Use descriptive names (`PlayerController` not `PC`)
- Match file name to class name
- Suffix with behavior (`Controller`, `Manager`, `Handler`)
