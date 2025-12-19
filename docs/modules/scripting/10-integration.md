# Integration & Extension

## Custom Script Base Classes

You can extend `ScriptableEntity` to create specialized base classes for common functionality.

### Example: LoggableScript

```csharp
public abstract class LoggableScript : ScriptableEntity
{
    protected ILogger Logger { get; private set; } = null!;

    public void SetLogger(ILogger logger)
    {
        Logger = logger;
    }

    protected void LogInfo(string message)
    {
        Logger.Information(message);
    }

    protected void LogError(string message, Exception? ex = null)
    {
        if (ex != null)
            Logger.Error(ex, message);
        else
            Logger.Error(message);
    }
}

// Usage
public class Player : LoggableScript
{
    public override void OnCreate()
    {
        LogInfo("Player initialized");
    }
}
```

### Example: StatefulScript

```csharp
public abstract class StatefulScript<TState> : ScriptableEntity
    where TState : IComponent, new()
{
    protected TState State { get; private set; } = default!;

    public override void OnCreate()
    {
        if (!HasComponent<TState>())
            AddComponent(new TState());

        State = GetComponent<TState>();
        OnStateInitialized();
    }

    protected virtual void OnStateInitialized() { }
}

// Usage
public class PlayerState : IComponent
{
    public int Health { get; set; } = 100;
    public int Score { get; set; } = 0;
}

public class Player : StatefulScript<PlayerState>
{
    protected override void OnStateInitialized()
    {
        // State component guaranteed to exist
        State.Health = 100;
    }

    public override void OnUpdate(TimeSpan ts)
    {
        // Access persistent state
        if (State.Health <= 0)
        {
            // Handle death
        }
    }
}
```

## Editor Integration

### ScriptComponentEditor

The editor provides `ScriptComponentEditor` for script management in the inspector.

**Features:**
- Create new scripts via modal
- Select existing scripts from dropdown
- Delete scripts with confirmation
- Edit public fields (int, float, Vector2/3/4, etc.)

**Location:** `Editor/ComponentEditors/ScriptComponentEditor.cs`

### Extending Field Support

To support new field types in the inspector:

1. **Update IsSupportedFieldType:**
```csharp
// ScriptableEntity.cs
private static bool IsSupportedFieldType(Type type)
{
    return type == typeof(int) ||
           type == typeof(float) ||
           type == typeof(double) ||
           type == typeof(bool) ||
           type == typeof(string) ||
           type == typeof(Vector2) ||
           type == typeof(Vector3) ||
           type == typeof(Vector4) ||
           type == typeof(YourCustomType); // Add here
}
```

2. **Register Field Editor:**
```csharp
// In FieldEditorRegistry or similar
public static void RegisterCustomEditors()
{
    FieldEditorRegistry.Register<YourCustomType>(new YourCustomTypeEditor());
}
```

3. **Implement IFieldEditor:**
```csharp
public class YourCustomTypeEditor : IFieldEditor
{
    public bool CanEdit(Type fieldType) => fieldType == typeof(YourCustomType);

    public object Draw(string label, object value, Type fieldType)
    {
        var typedValue = (YourCustomType)value;
        // Render ImGui controls
        // Return modified value
        return typedValue;
    }
}
```

## Script Templates

### Customizing Template Generation

```csharp
// ScriptEngine.cs:856
public static string GenerateScriptTemplate(string scriptName)
{
    return $@"using System;
using System.Numerics;
using GameEngine.Core;
using GameEngine.Scene;

namespace GameEngine.Scripts
{{
    public class {scriptName} : ScriptableEntity
    {{
        public override void OnCreate()
        {{
            // Custom template content here
        }}

        public override void OnUpdate(TimeSpan ts)
        {{
            float deltaTime = (float)ts.TotalSeconds;
        }}

        public override void OnDestroy()
        {{
        }}
    }}
}}";
}
```

**Modification:** Edit template string to include custom using statements, base classes, or boilerplate code.

## Accessing Custom Components

Scripts can access any ECS component, including custom ones.

### Example: Custom Component

```csharp
// Define custom component
public class InventoryComponent : IComponent
{
    public List<string> Items { get; set; } = new();
    public int MaxSlots { get; set; } = 10;

    public IComponent Clone()
    {
        return new InventoryComponent
        {
            Items = new List<string>(Items),
            MaxSlots = MaxSlots
        };
    }
}

// Register in DI container (if needed)
// EngineIoCContainer.cs or similar

// Use in script
public class InventoryScript : ScriptableEntity
{
    public override void OnCreate()
    {
        if (!HasComponent<InventoryComponent>())
            AddComponent(new InventoryComponent());
    }

    public void AddItem(string item)
    {
        var inventory = GetComponent<InventoryComponent>();
        if (inventory.Items.Count < inventory.MaxSlots)
        {
            inventory.Items.Add(item);
        }
    }
}
```

## Extending the Script System

### Adding Script Lifecycle Events

To add new lifecycle methods (e.g., `OnSceneStart`, `OnPause`):

1. **Add virtual method to ScriptableEntity:**
```csharp
// ScriptableEntity.cs
public virtual void OnSceneStart() { }
public virtual void OnPause() { }
```

2. **Call from ScriptEngine:**
```csharp
// ScriptEngine.cs
public void NotifySceneStart()
{
    foreach (var entity in _sceneContext.ActiveScene.Entities)
    {
        if (entity.HasComponent<NativeScriptComponent>())
        {
            var script = entity.GetComponent<NativeScriptComponent>().ScriptableEntity;
            script?.OnSceneStart();
        }
    }
}
```

3. **Invoke from scene manager:**
```csharp
// SceneManager.cs or similar
scriptEngine.NotifySceneStart();
```

### Adding Global Script Access

To provide scripts access to global services (e.g., audio manager, input):

```csharp
public abstract class ScriptableEntity
{
    // Existing Entity property
    public Entity Entity { get; internal set; }

    // Add global service properties
    protected IAudioManager Audio { get; internal set; } = null!;
    protected IInputManager Input { get; internal set; } = null!;

    // Set by ScriptEngine during instantiation
}

// In ScriptEngine.CreateScriptInstance:
instance.Entity = entity;
instance.Audio = _audioManager;
instance.Input = _inputManager;
```

## Custom Script Discovery

**Current:** Scripts must be in `assets/scripts/` and inherit `ScriptableEntity`.

**Extension:** Modify `SetScriptsDirectory()` to search multiple directories:

```csharp
public void SetScriptsDirectory(params string[] directories)
{
    _scriptDirectory.Clear();
    _scriptDirectory.AddRange(directories);

    foreach (var dir in directories)
    {
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
    }
}
```

## Editor-Only Scripts

To mark scripts as editor-only:

```csharp
[EditorOnly]
public class EditorDebugScript : ScriptableEntity
{
    // Only compiled in editor, excluded from runtime builds
}

// In ScriptEngine compilation:
#if !EDITOR
// Skip types with [EditorOnly] attribute
scriptTypes = scriptTypes.Where(t => !t.IsDefined(typeof(EditorOnlyAttribute)));
#endif
```

## Script Serialization

### Serializing Script Field Values

**Current:** Only script name is serialized.

**Extension:** Serialize public field values with scene:

```csharp
// NativeScriptComponent
public Dictionary<string, object> SerializedFields { get; set; } = new();

// On scene save
public void SerializeFields()
{
    if (ScriptableEntity == null) return;

    SerializedFields.Clear();
    foreach (var (name, type, value) in ScriptableEntity.GetExposedFields())
    {
        SerializedFields[name] = value;
    }
}

// On scene load
public void DeserializeFields()
{
    if (ScriptableEntity == null) return;

    foreach (var (name, value) in SerializedFields)
    {
        ScriptableEntity.SetFieldValue(name, value);
    }
}
```

**Integration:** Call `SerializeFields()` before scene save, `DeserializeFields()` after script instantiation.

## Multi-Script Components

**Current:** One script per `NativeScriptComponent`.

**Extension:** Support multiple scripts on single entity:

```csharp
public class NativeScriptComponent : IComponent
{
    public List<string> ScriptNames { get; set; } = new();
    public List<ScriptableEntity> Scripts { get; set; } = new();

    // ScriptEngine.OnUpdate handles all scripts in list
}
```

**Trade-off:** More complex lifecycle management, but more flexible composition.
