# Hot Reload System

## How It Works

The script engine monitors file modification times and automatically recompiles changed scripts without restarting the editor.

**Detection Mechanism:**
```csharp
// ScriptEngine.cs:363
private void CheckForScriptChanges()
{
    foreach (var (scriptName, lastModified) in _scriptLastModified)
    {
        var currentModified = File.GetLastWriteTime(scriptPath);
        if (currentModified > lastModified)
            needsRecompile = true;
    }
}
```

**Execution:** Runs every frame during `ScriptEngine.OnUpdate()`

## Automatic vs Manual Reload

### Automatic Reload
- Triggered when saving script file in editor
- Detects changes via `File.GetLastWriteTime()`
- Recompiles all scripts in `assets/scripts/`
- Replaces existing script instances with new types

### Manual Reload
```csharp
scriptEngine.ForceRecompile();
```
Forces immediate recompilation regardless of file timestamps.

**Editor UI:** Accessible through ScriptComponentEditor (if exposed)

## Reload Process

1. **Detect Change** - File timestamp comparison
2. **Parse All Scripts** - Create new syntax trees
3. **Compile Assembly** - Generate new in-memory assembly
4. **Update Type Registry** - Refresh `_scriptTypes` dictionary
5. **Replace Instances** - For each entity with `NativeScriptComponent`:
   - Create new instance from updated type
   - Call `OnCreate()` on new instance
   - Replace old instance reference

**Code:**
```csharp
// ScriptEngine.cs:827
public void ForceRecompile()
{
    CompileAllScripts();

    foreach (var entity in _sceneContext.ActiveScene.Entities)
    {
        if (entity.HasComponent<NativeScriptComponent>())
        {
            var scriptComponent = entity.GetComponent<NativeScriptComponent>();
            var newInstance = CreateScriptInstance(scriptType.Name);
            scriptComponent.ScriptableEntity = newInstance.Value;
            scriptComponent.ScriptableEntity.OnCreate();
        }
    }
}
```

## State Preservation

### What Persists
- **Entity references** - `Entity` property maintained
- **Components** - Component data on entity persists
- **Scene state** - Other entities, systems unchanged

### What Resets
- **Script fields** - All instance fields reset to defaults
- **Local state** - Variables in `OnUpdate()` cleared
- **Cached references** - Component references need re-caching

## Best Practices

### Use Components for Persistence
**Bad:**
```csharp
public class Player : ScriptableEntity
{
    private int _health = 100; // Lost on reload!

    public override void OnUpdate(TimeSpan ts)
    {
        // Health resets to 100 on every reload
    }
}
```

**Good:**
```csharp
// Create custom HealthComponent
public class Player : ScriptableEntity
{
    public override void OnCreate()
    {
        if (!HasComponent<HealthComponent>())
            AddComponent<HealthComponent>();
    }

    public override void OnUpdate(TimeSpan ts)
    {
        var health = GetComponent<HealthComponent>();
        // Health persists across reloads
    }
}
```

### Avoid Expensive OnCreate() Operations
```csharp
public override void OnCreate()
{
    // Runs on EVERY reload - keep it lightweight
    _cachedTransform = GetComponent<TransformComponent>();
}
```

### Cache Component References
```csharp
private TransformComponent? _transform;

public override void OnUpdate(TimeSpan ts)
{
    _transform ??= GetComponent<TransformComponent>();
    // Use _transform
}
```

## Limitations

### Cannot Hot Reload
- **Base class changes** - Changing `ScriptableEntity` inheritance
- **Assembly references** - Adding new using statements for assemblies not already loaded
- **Script file moves** - Renaming or moving script files
- **Namespace changes** - Modifying the namespace

### Workarounds
- **Restart editor** for structural changes
- **Keep interfaces stable** during development
- **Use composition** instead of inheritance when possible

## Compilation Errors

**Behavior:** If compilation fails, previous assembly remains active.

**Error Output:**
```
❌ COMPILATION ERRORS DETECTED:
ERROR: CS1002: ; expected
  Location: PlayerController.cs(15,10)
```

**Recovery:**
- Fix compilation errors
- Save file
- Automatic retry on next file change

## Performance Considerations

**File Monitoring Cost:**
- Runs every frame
- Minimal overhead (file stat calls)
- Only triggers compilation on actual changes

**Compilation Time:**
- Typically <500ms for small projects
- Scales with number of scripts
- Editor may freeze briefly during compilation

**Optimization:** Use `#if DEBUG` to disable hot reload in release builds if needed.
