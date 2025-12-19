# Appendix

## File Locations

### Core Components

| Component | Path |
|-----------|------|
| Script Engine Interface | `Engine/Scripting/IScriptEngine.cs` |
| Script Engine Implementation | `Engine/Scripting/ScriptEngine.cs` |
| Script Base Class | `Engine/Scene/ScriptableEntity.cs` |
| Script Component | `Engine/Scene/Components/NativeScriptComponent.cs` |
| Script Update System | `Engine/Scene/Systems/ScriptUpdateSystem.cs` |
| System Priorities | `Engine/Scene/Systems/SystemPriorities.cs` |

### Editor Integration

| Component | Path |
|-----------|------|
| Script Component Editor | `Editor/ComponentEditors/ScriptComponentEditor.cs` |
| Field Editor Registry | `Editor/UI/FieldEditors/FieldEditorRegistry.cs` |
| Component Editor Registry | `Editor/ComponentEditors/ComponentEditorRegistry.cs` |

### Dependency Injection

| Component | Path |
|-----------|------|
| Engine IoC Container | `Engine/Core/DI/EngineIoCContainer.cs` |
| Scene System Registry | `Engine/Scene/SceneSystemRegistry.cs` |

### Scene Management

| Component | Path |
|-----------|------|
| Scene Context | `Engine/Scene/SceneContext.cs` |
| Scene Factory | `Engine/Scene/SceneFactory.cs` |
| Scene Manager | `Editor/Features/Scene/SceneManager.cs` |

### Example Scripts

| Component | Path |
|-----------|------|
| Camera Controller | `Sandbox/assets/scripts/CameraController.cs` |

## System Architecture Flowcharts

### Script Compilation Flow

```
Start
  ↓
Monitor File Changes (every frame)
  ↓
File Modified?
  ↓ Yes
Parse All .cs Files → Syntax Trees
  ↓
Load Assembly References
  ↓
Create CSharpCompilation
  ↓
Check Diagnostics
  ↓
Errors?
  ↓ No
Emit Assembly + PDB
  ↓
Load Assembly into Memory
  ↓
Discover ScriptableEntity Types
  ↓
Update Type Registry
  ↓
Replace Script Instances
  ↓
Call OnCreate() on New Instances
  ↓
End
```

### Script Execution Flow

```
Frame Start
  ↓
ECS Update Loop
  ↓
ScriptUpdateSystem (Priority 110)
  ↓
ScriptEngine.OnUpdate()
  ↓
For Each Entity with NativeScriptComponent:
  ↓
  Script Not Created?
    ↓ Yes → CreateScriptInstance()
    ↓       Call OnCreate()
  ↓
  Call OnUpdate(deltaTime)
  ↓
Next Entity
  ↓
Frame End
```

### Hot Reload Flow

```
File Save
  ↓
CheckForScriptChanges() Detects Change
  ↓
CompileAllScripts()
  ↓
New Assembly Created
  ↓
For Each Active Script Instance:
  ↓
  Get Script Type Name
  ↓
  Find Type in New Assembly
  ↓
  Create New Instance
  ↓
  Set Entity Reference
  ↓
  Call OnCreate()
  ↓
  Replace Old Instance
  ↓
Next Instance
  ↓
Scripts Updated
```

## Quick Reference

### Common Methods

```csharp
// Lifecycle
OnCreate()                      // Initialization
OnUpdate(TimeSpan ts)           // Per-frame logic
OnDestroy()                     // Cleanup

// Component Access
GetComponent<T>()               // Get component (throws if missing)
HasComponent<T>()               // Check component existence
AddComponent<T>()               // Add new component
RemoveComponent<T>()            // Remove component

// Entity Operations
FindEntity(string name)         // Find entity by name
CreateEntity(string name)       // Create new entity
DestroyEntity(Entity entity)    // Destroy entity

// Transform Helpers
GetPosition() / SetPosition()   // World position
GetRotation() / SetRotation()   // Euler rotation
GetScale() / SetScale()         // Scale
GetForward() / GetRight() / GetUp()  // Direction vectors

// Input Events
OnKeyPressed(KeyCodes key)
OnKeyReleased(KeyCodes key)
OnMouseButtonPressed(int button)

// Physics Events
OnCollisionBegin(Entity other)
OnCollisionEnd(Entity other)
OnTriggerEnter(Entity other)
OnTriggerExit(Entity other)
```

### System Priority Order

```
Physics System             100
Script Update System       110  ← Scripts run here
Audio System              120
TileMap System            130
Animation System          140
Renderer2D System         150
UI Renderer System        160
```

## Troubleshooting

### Compilation Issues

**Problem:** Script not found after creation

**Solutions:**
- Verify file in `assets/scripts/` directory
- Check file has `.cs` extension
- Ensure class inherits `ScriptableEntity`
- Look for compilation errors in console
- Restart editor if hot reload fails

---

**Problem:** CS0246: Type or namespace not found

**Solutions:**
- Add missing `using` statement
- Verify assembly is loaded (check available assemblies)
- Confirm type name spelling

---

**Problem:** CS1002: ; expected

**Solutions:**
- Check for missing semicolons
- Verify brackets are balanced
- Look at line/column in error message

### Runtime Issues

**Problem:** NullReferenceException in OnUpdate

**Solutions:**
- Check component existence with `HasComponent<>()`
- Verify entity reference is valid
- Ensure `OnCreate()` initialized fields
- Cache null-checks: `_field ??= GetComponent<>()`

---

**Problem:** Script state resets unexpectedly

**Solutions:**
- Move state to components, not script fields
- Use persistent component data
- See [Best Practices - Component Over State](./09-best-practices.md#component-over-state)

---

**Problem:** Component reference becomes null

**Solutions:**
- Re-cache after hot reload
- Use null-conditional caching: `_comp ??= GetComponent<>()`
- Validate reference before use

### Hot Reload Issues

**Problem:** Changes not taking effect

**Solutions:**
- Check for compilation errors
- Verify file was saved
- Call `ForceRecompile()` manually
- Restart editor for structural changes

---

**Problem:** Script instance not updating

**Solutions:**
- Check `OnUpdate()` is spelled correctly
- Verify script attached to entity
- Ensure entity is active in scene
- Look for exceptions in console

### Performance Issues

**Problem:** Editor freezes when saving scripts

**Solutions:**
- Reduce number of scripts
- Simplify complex scripts
- Check for infinite loops in `OnCreate()`
- Disable hot reload temporarily

---

**Problem:** Low frame rate with many scripts

**Solutions:**
- Profile `OnUpdate()` methods
- Minimize allocations in hot paths
- Cache component references
- Use early returns for inactive scripts
- Consider moving logic to dedicated ECS systems

## Useful Commands

### Debug Mode
```csharp
scriptEngine.EnableHybridDebugging(true);
scriptEngine.SaveDebugSymbols("debug/output", "DynamicScripts");
scriptEngine.PrintDebugInfo();
```

### Force Recompilation
```csharp
scriptEngine.ForceRecompile();
```

### Script Directory
```csharp
scriptEngine.SetScriptsDirectory("assets/scripts");
```

## Version Compatibility

**Engine Version:** Designed for .NET 10.0
**C# Version:** 12
**Roslyn Version:** Microsoft.CodeAnalysis (latest compatible)

**Assembly Compatibility:**
- Engine.dll
- ECS.dll
- Box2D.NET.dll
- Silk.NET (via Engine)
- ImGui.NET (via Editor)

## Additional Resources

- [Architecture](./01-architecture.md) - System design
- [API Reference](./03-api-reference.md) - Complete API
- [Examples](./07-examples.md) - Code samples
- [Best Practices](./09-best-practices.md) - Recommended patterns
