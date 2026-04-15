---
name: system-creation
description: Guide the creation of new ECS systems following established architectural patterns including ISystem implementation, priority assignment, scene interaction, and dependency injection registration. Use when adding new system types to the engine or extending existing system functionality.
---

# System Creation Workflow

## Overview
This skill provides step-by-step guidance for adding new ECS systems to the game engine, ensuring consistency with architectural patterns.

**Current Architecture**: Priority-based `ISystem` implementations registered via `SystemManager`. Systems are singleton services resolved through DryIoc. Components are data-only — all logic lives in systems.

## When to Use
Invoke this skill when:
- Adding per-frame logic that operates on entities
- Implementing a new gameplay or engine feature that requires update cycles
- Extending existing systems with new behavior
- Understanding SystemManager registration and priority ordering

## Table of Contents
1. [System Creation Workflow](#system-creation-workflow-steps)
   - [Step 1: Create System Class](#step-1-create-system-class)
   - [Step 2: Implement ISystem Interface](#step-2-implement-isystem-interface)
   - [Step 3: Register in DI](#step-3-register-in-di)
   - [Step 4: Add to SystemManager](#step-4-add-to-systemmanager)
2. [Priority Reference](#priority-reference)
3. [Common Mistakes](#common-mistakes)

## System Creation Workflow Steps

### Step 1: Create System Class
**Location**: `Engine/Scene/Systems/`

**Guidelines**:
- One responsibility per system (single concern)
- No game data stored in the system — data belongs in components
- Dependencies injected via primary constructor
- Name with "System" suffix: `PhysicsSystem`, `AnimationSystem`

### Step 2: Implement ISystem Interface

```csharp
namespace Engine.Scene.Systems;

public class MySystem(IMyDependency dependency) : ISystem
{
    // Priority ranges — see SystemPriorities.cs for all registered values.
    // Lower executes first. Current range: 100 (physics) to 180 (debug render).
    // Physics/simulation: ~100–120
    // Game logic / scripts: ~110–145
    // Rendering: ~150–180
    public int Priority => 150;

    public void OnAttach(Scene scene) { }

    public void OnDetach(Scene scene) { }

    public void OnUpdate(Scene scene, TimeSpan deltaTime)
    {
        foreach (var entity in scene.GetEntitiesWithComponent<MyComponent>())
        {
            var component = entity.GetComponent<MyComponent>();
            // Process component data
        }
    }

    public void OnEvent(Scene scene, Event e) { }
}
```

### Step 3: Register in DI
**Location**: `Engine/Program.cs` (for runtime) or `Editor/Program.cs` (for editor-only systems)

```csharp
container.Register<MySystem>(Reuse.Singleton);
```

Register any new dependencies the system introduces at the same time.

### Step 4: Add to SystemManager
**Location**: `Engine/Scene/SceneSystemRegistry.cs` (or equivalent registration point)

```csharp
systemManager.AddSystem(container.Resolve<MySystem>());
```

Systems are sorted by `Priority` automatically — insertion order doesn't matter.

**Verify**:
1. `dotnet build` — zero warnings
2. Launch editor and open a scene
3. Confirm system behavior activates for entities with the relevant component

## Priority Reference

Authoritative source: `Engine/Scene/Systems/SystemPriorities.cs`

| Value | System |
|-------|--------|
| 100 | PhysicsSimulationSystem |
| 110 | ScriptUpdateSystem |
| 120 | AudioSystem |
| 145 | PrimaryCameraSystem |
| 150 | SpriteRenderSystem |
| 160 | SubTextureRenderSystem |
| 165 | LightingSystem |
| 170 | ModelRenderSystem |
| 180 | PhysicsDebugRenderSystem |

When adding a new system, pick a value that fits between existing ones and register it in `SystemPriorities.cs`. When in doubt: physics/simulation ~100–130, game logic/scripts ~110–145, rendering ~150–180.

## Common Mistakes

### ❌ Storing state in the system
**Problem**: System fields hold per-entity data, breaking ECS separation.
**Solution**: All entity data lives in components. Systems only hold stateless services.

### ❌ Wrong priority causing order-dependent bugs
**Problem**: System reads a value updated by another system in the same frame, but runs first.
**Solution**: Check existing system priorities before choosing yours. Physics systems run ~100, rendering ~200.

### ❌ Not unsubscribing events in OnDetach
**Problem**: Memory leak / callbacks fire on disposed scenes.
**Solution**: Mirror every subscription in `OnAttach` with an unsubscription in `OnDetach`.

### ❌ Putting logic in components
**Problem**: Violates ECS architecture — components must be data-only.
**Solution**: Move all per-frame logic into `OnUpdate`. Components store data, systems process behavior.
