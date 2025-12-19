# Debugging Guide

## Enabling Debug Mode

### Hybrid Debugging
```csharp
scriptEngine.EnableHybridDebugging(enable: true);
```

**Effect:**
- Enables PDB symbol generation
- Allows external debuggers to attach
- Includes line numbers in stack traces

**Editor Setup:**
```csharp
// Editor/Program.cs:40-48
#if DEBUG
scriptEngine.EnableHybridDebugging();
Logger.Information("Hybrid debugging enabled. Debug symbols will be generated.");
#else
Logger.Information("Hybrid debugging disabled in release mode");
#endif
```

## Saving Debug Symbols

### Export PDB Files
```csharp
scriptEngine.SaveDebugSymbols(
    outputPath: "path/to/debug",
    assemblyName: "DynamicScripts"
);
```

**Output:**
- `DynamicScripts.dll` - Compiled assembly
- `DynamicScripts.pdb` - Portable debug database

**Use Case:** Attach Visual Studio or Rider to editor process for step-through debugging.

### Attaching External Debugger

1. **Enable hybrid debugging** in editor
2. **Save debug symbols** to known location
3. **Launch editor** (with debugger enabled)
4. **Attach debugger** to editor process
5. **Load symbols** from saved PDB location
6. **Set breakpoints** in script files
7. **Trigger script execution** (run scene)

## Logging from Scripts

Scripts inherit Serilog logger via `ScriptableEntity` base class (if exposed).

### Console Logging
```csharp
public override void OnUpdate(TimeSpan ts)
{
    // Use System.Console for simple logging
    Console.WriteLine($"Position: {GetPosition()}");
}
```

### Accessing Engine Logger
**Current:** Scripts don't have direct access to `ILogger`.

**Workaround:** Add static logger reference or inject via custom base class.

```csharp
// Custom approach (requires modification)
public abstract class LoggableScript : ScriptableEntity
{
    protected static ILogger Logger { get; set; } = null!;
}
```

## Common Errors

### Compilation Errors

**Error:** CS1002: ; expected
```
ERROR: CS1002: ; expected
  Location: PlayerController.cs(15,10)
```
**Fix:** Check line 15, column 10 for missing semicolon.

---

**Error:** CS0103: Name does not exist in current context
```
ERROR: CS0103: The name 'Vctor3' does not exist in the current context
  Location: EnemyAI.cs(42,20)
```
**Fix:** Typo - should be `Vector3` not `Vctor3`.

---

**Error:** CS0246: Type or namespace not found
```
ERROR: CS0246: The type or namespace name 'Box2D' could not be found
```
**Fix:** Missing `using` statement or assembly not loaded. Verify `Box2D.NET.dll` is referenced.

### Runtime Errors

**NullReferenceException:**
```csharp
// Problem:
var transform = GetComponent<TransformComponent>();
transform.Translation = Vector3.Zero; // NRE if component doesn't exist

// Solution:
if (HasComponent<TransformComponent>())
{
    var transform = GetComponent<TransformComponent>();
    transform.Translation = Vector3.Zero;
}
```

---

**InvalidOperationException: Component not found**
```csharp
// Problem:
var sprite = GetComponent<Sprite2DComponent>(); // Throws if not found

// Solution:
if (!HasComponent<Sprite2DComponent>())
{
    AddComponent<Sprite2DComponent>();
}
var sprite = GetComponent<Sprite2DComponent>();
```

---

**Hot Reload Issues:**
```
Error initializing script on entity Player
```
**Cause:** Script constructor threw exception.

**Fix:**
- Move initialization from constructor to `OnCreate()`
- Avoid complex logic in constructors

## Editor Diagnostics

### Print Debug Info
```csharp
scriptEngine.PrintDebugInfo();
```

**Output:**
```
Script Engine Debug Info:
- Loaded Assemblies: 15
- Script Types: 8
- Registered Scripts: PlayerController, EnemyAI, CameraController, ...
- Compilation Warnings: 0
- Compilation Errors: 0
```

**Use Case:** Verify script discovery, check compilation status.

### Console Panel

**Editor Console** displays:
- Compilation errors with file locations
- Runtime exceptions with stack traces
- Script initialization messages
- Hot reload notifications

**Severity Levels:**
- **Error** (red) - Compilation failures, runtime exceptions
- **Warning** (yellow) - Compilation warnings, deprecations
- **Info** (white) - General messages, script loading

## Stack Traces

### With Debug Symbols
```
Error initializing script on entity Player
  at PlayerController.OnCreate() in PlayerController.cs:line 15
  at ScriptEngine.OnUpdate(TimeSpan deltaTime) in ScriptEngine.cs:line 67
```

**Includes:** File name, line number, method name

### Without Debug Symbols
```
Error initializing script on entity Player
  at PlayerController.OnCreate()
  at ScriptEngine.OnUpdate(TimeSpan deltaTime)
```

**Missing:** File locations, line numbers

## Performance Profiling

### Measuring Script Performance
```csharp
public override void OnUpdate(TimeSpan ts)
{
    var startTime = DateTime.UtcNow;

    // Your script logic here

    var elapsed = DateTime.UtcNow - startTime;
    if (elapsed.TotalMilliseconds > 1.0)
    {
        Console.WriteLine($"Script took {elapsed.TotalMilliseconds:F2}ms");
    }
}
```

### Using Stopwatch
```csharp
private Stopwatch _stopwatch = Stopwatch.StartNew();

public override void OnUpdate(TimeSpan ts)
{
    _stopwatch.Restart();

    // Script logic

    _stopwatch.Stop();
    if (_stopwatch.ElapsedMilliseconds > 1)
    {
        Console.WriteLine($"Update: {_stopwatch.ElapsedMilliseconds}ms");
    }
}
```

## Troubleshooting Hot Reload

**Issue:** Scripts not recompiling

**Solutions:**
- Check file is in `assets/scripts/` directory
- Verify file has `.cs` extension
- Ensure file is actually saved (check modification time)
- Look for compilation errors in console

---

**Issue:** Script state resets unexpectedly

**Cause:** Hot reload replaces script instances.

**Solution:** Store persistent state in components, not script fields.

---

**Issue:** Changes not taking effect

**Cause:** Assembly cached, stale types.

**Solution:** Call `ForceRecompile()` or restart editor.

## Best Debugging Practices

1. **Enable debug symbols** during development
2. **Use logging liberally** in complex logic
3. **Check component existence** before access
4. **Validate entity references** after hot reload
5. **Profile hot paths** in `OnUpdate()`
6. **Test compilation frequently** to catch errors early
7. **Keep OnCreate() simple** to avoid initialization errors
