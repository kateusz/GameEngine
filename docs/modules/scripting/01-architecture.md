# Architecture

## System Overview

The scripting engine uses Roslyn for runtime C# compilation and integrates with the ECS architecture through a dedicated system.

```
┌─────────────────────────────────────────────────────────────┐
│                     ScriptEngine (Singleton)                │
│  - Roslyn compilation pipeline                              │
│  - File monitoring & hot reload                             │
│  - Type registry & instantiation                            │
│  - Debug symbol generation                                  │
└──────────────────┬──────────────────────────────────────────┘
                   │
                   │ creates & manages
                   ↓
┌─────────────────────────────────────────────────────────────┐
│              ScriptableEntity (Abstract Base)               │
│  - Lifecycle hooks (OnCreate, OnUpdate, OnDestroy)          │
│  - Input/physics event handlers                             │
│  - Component access API                                     │
│  - Entity operations                                        │
└──────────────────┬──────────────────────────────────────────┘
                   │
                   │ referenced by
                   ↓
┌─────────────────────────────────────────────────────────────┐
│           NativeScriptComponent (ECS Component)             │
│  - Holds ScriptableEntity instance reference                │
│  - Serializes script name only                              │
└──────────────────┬──────────────────────────────────────────┘
                   │
                   │ processed by
                   ↓
┌─────────────────────────────────────────────────────────────┐
│          ScriptUpdateSystem (ECS System, Priority 110)      │
│  - Delegates to ScriptEngine.OnUpdate()                     │
│  - Runs after Physics (100), before Audio (120)             │
└─────────────────────────────────────────────────────────────┘
```

## Compilation Pipeline

```
File Monitoring → Syntax Parsing → Reference Resolution →
Roslyn Compilation → Assembly Emit → Type Discovery →
Instance Creation
```

**Detailed Flow:**

1. **File Monitoring** - `ScriptEngine` watches `assets/scripts/*.cs` for changes
2. **Syntax Parsing** - `CSharpSyntaxTree.ParseText()` creates syntax trees
3. **Reference Resolution** - Loads .NET runtime, Engine, ECS, Box2D assemblies
4. **Compilation** - `CSharpCompilation.Create()` with Debug/Release optimization
5. **Assembly Emit** - Generates in-memory assembly with optional PDB symbols
6. **Type Discovery** - Reflects assembly for `ScriptableEntity` subclasses
7. **Instance Creation** - `Activator.CreateInstance()` when script attached to entity

## ECS Integration

**Component Registration:**
```csharp
// Engine/Core/DI/EngineIoCContainer.cs:56
container.Register<IScriptEngine, ScriptEngine>(Reuse.Singleton);
```

**System Registration:**
```csharp
// Engine/Scene/SceneSystemRegistry.cs:29
systemManager.RegisterSystem(scriptUpdateSystem, isShared: true);
```

**Entity → Script Relationship:**
```
Entity (GUID)
  └─ NativeScriptComponent
       └─ ScriptableEntity instance
```

## System Priority

**ScriptUpdateSystem runs at priority 110:**
- **Before:** Audio (120), TileMap (130), Animation (140), Rendering (150+)
- **After:** Physics (100)

This ensures scripts can read physics results and modify state before rendering.

## Dependency Injection

**Pattern:** Primary constructor injection via DryIoc

```csharp
// ScriptEngine receives dependencies
public sealed class ScriptEngine(
    ISceneContext sceneContext,
    ILogger logger) : IScriptEngine
```

**No static singletons** - All services injected through constructor parameters.

## File Locations

| Component | Path |
|-----------|------|
| Interface | `Engine/Scripting/IScriptEngine.cs` |
| Implementation | `Engine/Scripting/ScriptEngine.cs` |
| Base Class | `Engine/Scene/ScriptableEntity.cs` |
| Component | `Engine/Scene/Components/NativeScriptComponent.cs` |
| System | `Engine/Scene/Systems/ScriptUpdateSystem.cs` |
| Editor UI | `Editor/ComponentEditors/ScriptComponentEditor.cs` |
| System Priorities | `Engine/Scene/Systems/SystemPriorities.cs` |

## Key Design Decisions

**Why Roslyn?** - Full C# language support with debugging, no external tools required

**Why ECS integration?** - Scripts access components via standardized ECS API

**Why singleton ScriptEngine?** - Single compilation context, shared type registry, centralized file monitoring

**Why abstract base class?** - Enforces lifecycle contracts, provides component access, enables reflection for editor
