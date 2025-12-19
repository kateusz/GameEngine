# Limitations & Caveats

## Inheritance Requirements

### Must Inherit ScriptableEntity

**Limitation:** All scripts must derive from `ScriptableEntity`.

**Reason:** Type discovery filters for `ScriptableEntity` subclasses during compilation.

```csharp
// ScriptEngine.cs
_scriptTypes = _dynamicAssembly.GetTypes()
    .Where(t => t.IsSubclassOf(typeof(ScriptableEntity)) && !t.IsAbstract)
    .ToDictionary(t => t.Name);
```

**Workaround:** Create intermediate base classes that inherit `ScriptableEntity`.

```csharp
// Valid
public class CustomBase : ScriptableEntity { }
public class MyScript : CustomBase { } // Discovered

// Invalid
public class MyScript : MonoBehaviour { } // Not discovered
```

## Assembly References

### Fixed at Compile Time

**Limitation:** Cannot add new assembly references at runtime.

**Reason:** Metadata references loaded once during `GetReferencesFromRuntimeDirectory()`.

**Available Assemblies:**
- .NET Core (System.Runtime, System.Numerics, etc.)
- Engine, ECS, Editor
- Box2D.NET
- Silk.NET, ImGui.NET (via Engine)

**Workaround:** Modify `GetReferencesFromRuntimeDirectory()` to include additional assemblies.

```csharp
// Add custom assembly
var customAssembly = MetadataReference.CreateFromFile("path/to/CustomLibrary.dll");
references.Add(customAssembly);
```

## Hot Reload Constraints

### Cannot Hot Reload Structural Changes

**Cannot reload:**
- Base class changes (`ScriptableEntity` → custom base)
- Namespace modifications
- Script file renames/moves
- Assembly reference additions

**Reason:** Type identity changes break instance replacement.

**Workaround:** Restart editor for structural changes.

### Script State Resets

**Limitation:** All instance fields reset to defaults on hot reload.

**Example:**
```csharp
public class Player : ScriptableEntity
{
    private int _score = 0;

    public override void OnUpdate(TimeSpan ts)
    {
        _score++; // Resets to 0 on every hot reload!
    }
}
```

**Workaround:** Store state in components (see [Best Practices](./09-best-practices.md#component-over-state)).

### Component References Invalidated

**Limitation:** Cached component references may become stale after hot reload.

**Example:**
```csharp
private TransformComponent? _transform;

public override void OnCreate()
{
    _transform = GetComponent<TransformComponent>(); // Valid reference
}
// After hot reload, _transform may be stale
```

**Workaround:** Use null-conditional caching (`_transform ??= GetComponent<>()`).

## Editor-Only Assembly Access

### Limited Access to Editor Types

**Limitation:** Scripts compiled with Editor assembly, but may have limited access depending on build configuration.

**Available in Scripts:**
- Engine types (ISceneContext, components, systems)
- ECS types (Entity, IComponent, ISystem)
- Editor types (limited, may vary)

**Workaround:** Keep scripts engine-focused, avoid editor-specific logic.

## Cross-Platform Considerations

### Platform-Specific Code

**Limitation:** Scripts must be cross-platform compatible.

**Avoid:**
```csharp
// Windows-specific
[DllImport("user32.dll")]
private static extern bool SetCursorPos(int x, int y);
```

**Use:**
```csharp
// Engine abstractions
var input = /* access input system via engine */;
```

### File Path Separators

**Limitation:** Hardcoded path separators break cross-platform.

**Bad:**
```csharp
var path = "assets\\textures\\player.png"; // Windows only
```

**Good:**
```csharp
var path = Path.Combine("assets", "textures", "player.png"); // Cross-platform
```

## Performance Constraints

### Per-Frame File Monitoring

**Limitation:** File change detection runs every frame.

**Impact:** Minimal (file stat syscalls), but scales with script count.

**Performance:**
```csharp
// ScriptEngine.OnUpdate called 60 times/second
CheckForScriptChanges(); // File.GetLastWriteTime() for each script
```

**Workaround:** Disable in release builds or reduce check frequency.

### Compilation Freeze

**Limitation:** Compilation blocks editor briefly (typically <500ms).

**Impact:** Noticeable pause when saving scripts.

**Workaround:**
- Keep scripts small
- Avoid saving multiple scripts simultaneously
- Use incremental compilation (future enhancement)

## Serialization Limitations

### Script Name Only

**Limitation:** Only script name serialized with scene, not field values.

**Example:**
```csharp
public class Player : ScriptableEntity
{
    public int MaxHealth = 100; // NOT saved with scene
}
```

**Workaround:** Extend `NativeScriptComponent` to serialize fields (see [Integration](./10-integration.md#script-serialization)).

### Supported Field Types

**Limitation:** Only specific types exposed to editor.

**Supported:**
- Primitives: `int`, `float`, `double`, `bool`
- `string`
- Vectors: `Vector2`, `Vector3`, `Vector4`

**Not Supported:**
- Custom classes/structs
- Collections (List, Dictionary)
- Enums (without extension)

**Workaround:** Extend `IsSupportedFieldType()` and `FieldEditorRegistry`.

## Debugging Constraints

### External Debugger Required

**Limitation:** No built-in step-through debugging in editor.

**Reason:** Scripts compiled to in-memory assembly, requires external debugger attachment.

**Workaround:**
- Enable hybrid debugging
- Save debug symbols
- Attach Visual Studio/Rider to editor process

### Limited Stack Traces Without PDB

**Limitation:** Stack traces lack file/line info without debug symbols.

**Example (without PDB):**
```
at PlayerController.OnUpdate()
at ScriptEngine.OnUpdate()
```

**Example (with PDB):**
```
at PlayerController.OnUpdate() in PlayerController.cs:line 42
at ScriptEngine.OnUpdate() in ScriptEngine.cs:line 67
```

**Workaround:** Enable debug mode in development builds.

## Multi-Scene Limitations

### Single Active Scene

**Limitation:** Scripts operate on `ActiveScene` only.

**Reason:** `ISceneContext` provides single active scene reference.

```csharp
// ScriptableEntity
protected Entity? FindEntity(string name)
{
    return _sceneContext.ActiveScene.Entities.FirstOrDefault(e => e.Name == name);
}
```

**Workaround:** Modify API to accept scene parameter for multi-scene support.

## Type Discovery Limitations

### Scripts Must Be in `assets/scripts/`

**Limitation:** Only `.cs` files in configured scripts directory are compiled.

**Reason:** `SetScriptsDirectory()` defines search path.

**Workaround:** Call `SetScriptsDirectory()` with additional paths.

### Script Name Collisions

**Limitation:** Script names must be unique (case-sensitive).

**Reason:** Type registry uses `Dictionary<string, Type>` keyed by type name.

```csharp
_scriptTypes.Add(type.Name, type); // Will throw if duplicate
```

**Workaround:** Use unique class names or namespaces (currently ignored).

## Memory Management

### No Automatic Cleanup

**Limitation:** Scripts responsible for their own resource disposal.

**Reason:** `ScriptableEntity` doesn't implement `IDisposable` by default.

**Workaround:** Override `OnDestroy()` for cleanup.

```csharp
public override void OnDestroy()
{
    _customResource?.Dispose();
}
```

## Language Feature Constraints

### C# 12 Features Only

**Limitation:** Roslyn compilation limited to C# 12 features available in .NET 10.

**Not Available:**
- C# 13+ features (if any)
- Experimental language features
- Unsafe context (unless enabled)

**Available:**
- Records, pattern matching
- Nullable reference types
- Primary constructors
- Collection expressions

### Unsafe Code

**Limitation:** Unsafe code allowed but discouraged.

**Reason:** Compilation options set `allowUnsafe: true`, but unsafe code can crash editor.

**Recommendation:** Avoid pointers, use `Span<T>` for performance-critical code instead.
