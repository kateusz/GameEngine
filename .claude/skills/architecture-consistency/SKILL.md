---
name: architecture-consistency
description: Review code for architectural consistency including proper project structure organization (Engine vs Editor separation), namespace conventions, factory pattern usage, constants class patterns (EditorUIConstants, RenderingConstants), layer system adherence, and following established design patterns. Use when reviewing new features or refactoring existing code.
---

# Architecture Consistency

## Overview
This skill ensures new code follows the established architectural patterns, project organization, naming conventions, and design patterns documented in CLAUDE.md. It prevents architectural drift and maintains codebase consistency.

## When to Use
Invoke this skill when:
- Reviewing new feature implementations
- Refactoring existing code
- Questions about where code should live
- Validating architectural decisions
- Preventing pattern violations
- Ensuring consistency with existing codebase

## Architectural Principles

### 1. Project Structure Organization

**Engine Project** - Core runtime, platform-agnostic
```
Engine/
├── Animation/      # 2D sprite animation system
├── Audio/          # OpenAL audio system
├── Core/           # Application framework, layers
├── Events/         # Event system
├── ImGuiNet/       # ImGui integration (rendering only)
├── Math/           # Math utilities
├── Platform/       # Platform abstractions (SilkNet)
├── Renderer/       # OpenGL rendering pipeline
├── Scene/          # Scene management, ECS
│   ├── Components/ # All component definitions
│   ├── Systems/    # All system implementations
│   └── Serializer/ # JSON serialization
├── Scripting/      # Roslyn-based scripting
└── UI/             # UI system integration
```

**Editor Project** - Editor-specific tools
```
Editor/
├── Input/          # Editor input handling
├── Logging/        # Console panel integration
├── Managers/       # ProjectManager, SceneManager
├── Panels/         # All 17 editor panels
├── Popups/         # Dialogs and modals
├── Publisher/      # Build and publishing
├── Systems/        # Editor-specific systems
├── UI/             # EditorUIConstants
├── Utilities/      # Editor helpers
└── Windows/        # Specialized windows
```

**❌ WRONG** - Editor code in Engine:
```csharp
// Engine/Scene/SceneManager.cs
public class SceneManager
{
    public void ShowSceneInEditor() // WRONG - editor concept in engine!
    {
        ImGui.Begin("Scene");
        // ...
    }
}
```

**✅ CORRECT** - Separate editor and engine:
```csharp
// Engine/Scene/SceneManager.cs (engine)
public class SceneManager
{
    public Scene GetActiveScene() { }
    public void SetActiveScene(Scene scene) { }
}

// Editor/Panels/SceneHierarchyPanel.cs (editor)
public class SceneHierarchyPanel
{
    private readonly ISceneManager _sceneManager;

    public void OnImGuiRender()
    {
        var scene = _sceneManager.GetActiveScene();
        ImGui.Begin("Scene Hierarchy");
        // Render scene in editor
    }
}
```

### 2. Namespace Conventions

**Pattern**: `<ProjectName>.<FolderPath>`

Examples:
```csharp
// Engine project
namespace Engine.Scene.Components;          // ✅
namespace Engine.Renderer;                  // ✅
namespace Engine.Platform.SilkNet;          // ✅

// Editor project
namespace Editor.Panels;                    // ✅
namespace Editor.UI;                        // ✅
namespace Editor.Managers;                  // ✅

// ECS project
namespace ECS.System;                       // ✅

// ❌ WRONG
namespace MyRandomNamespace;                // WRONG
namespace Components;                       // WRONG - missing project prefix
namespace Engine.Editor.Panels;             // WRONG - Editor is separate project
```

### 3. Component Organization

**All components** live in `Engine/Scene/Components/`

```csharp
// ✅ CORRECT location
// Engine/Scene/Components/ParticleEmitterComponent.cs
namespace Engine.Scene.Components;

public class ParticleEmitterComponent
{
    // Data-only component
}

// ❌ WRONG - Component in wrong location
// Engine/Renderer/ParticleEmitterComponent.cs
```

**Component Naming**: Always suffix with "Component"
- `TransformComponent` ✅
- `SpriteRendererComponent` ✅
- `Transform` ❌ (missing suffix)
- `Sprite` ❌ (missing suffix)

### 4. System Organization

**All systems** live in `Engine/Scene/Systems/`

```csharp
// ✅ CORRECT
// Engine/Scene/Systems/AnimationSystem.cs
namespace Engine.Scene.Systems;

public class AnimationSystem : ISystem
{
    public int Priority => 198;

    public void OnUpdate(Scene scene, TimeSpan deltaTime)
    {
        // Update logic
    }
}

// ❌ WRONG - System outside Systems folder
// Engine/Animation/AnimationSystem.cs
```

**System Registration**: All systems registered in `SceneSystemRegistry`
```csharp
// Engine/Scene/SceneSystemRegistry.cs
public static class SceneSystemRegistry
{
    public static void RegisterDefaultSystems(
        SystemManager systemManager,
        IServiceProvider services)
    {
        systemManager.AddSystem(services.GetRequiredService<ScriptUpdateSystem>());
        systemManager.AddSystem(services.GetRequiredService<AnimationSystem>());
        systemManager.AddSystem(services.GetRequiredService<PhysicsSimulationSystem>());
        // ... all systems
    }
}
```

### 5. Factory Pattern Usage

**All resource loading** goes through factories

**✅ CORRECT**:
```csharp
public interface ITextureFactory : IDisposable
{
    Texture LoadTexture(string path);
}

public class TextureFactory : ITextureFactory
{
    private readonly Dictionary<string, Texture> _cache = new();

    public Texture LoadTexture(string path)
    {
        // Caching logic
    }

    public void Dispose()
    {
        // Cleanup
    }
}

// Usage
public class Renderer
{
    private readonly ITextureFactory _textureFactory;

    public Renderer(ITextureFactory textureFactory)
    {
        _textureFactory = textureFactory;
    }

    public void Render()
    {
        var texture = _textureFactory.LoadTexture("sprite.png");
    }
}
```

**❌ WRONG**:
```csharp
// Direct resource creation - no caching, no DI
public class Renderer
{
    public void Render()
    {
        var texture = new Texture("sprite.png"); // BAD
    }
}
```

**Existing Factories**:
- `ITextureFactory` / `TextureFactory`
- `IShaderFactory` / `ShaderFactory`
- `IAudioClipFactory` / `AudioClipFactory`
- `IRendererApiFactory` / `RendererApiFactory`
- `ISceneFactory` / `SceneFactory`

### 6. Constants Classes Pattern

**Never use magic numbers** - use constants classes

**✅ CORRECT**:
```csharp
// Editor/UI/EditorUIConstants.cs
public static class EditorUIConstants
{
    public const float StandardButtonWidth = 120f;
    public const float PropertyLabelRatio = 0.33f;
    public static readonly Vector4 ErrorColor = new(0.9f, 0.2f, 0.2f, 1.0f);
}

// Usage
if (ImGui.Button("Save", new Vector2(
    EditorUIConstants.StandardButtonWidth,
    EditorUIConstants.StandardButtonHeight)))
{
    Save();
}
```

**❌ WRONG**:
```csharp
// Magic numbers scattered throughout code
if (ImGui.Button("Save", new Vector2(120, 30)))  // BAD
{
    Save();
}
```

**Existing Constants Classes**:
- `EditorUIConstants` - All editor UI sizing, colors, spacing
- `RenderingConstants` - All rendering config (MaxQuads, MaxTextureSlots)

**These are the ONLY acceptable static classes!**

### 7. Layer System Adherence

**Engine**: Layer abstract class in `Engine/Core/Layer.cs`

```csharp
public abstract class Layer
{
    public virtual void OnAttach() { }
    public virtual void OnDetach() { }
    public virtual void OnUpdate(TimeSpan deltaTime) { }
    public virtual void OnEvent(Event e) { }
    public virtual void OnImGuiRender() { }
}
```

**Editor**: Uses layers for separation of concerns
```csharp
// Editor/EditorLayer.cs
public class EditorLayer : Layer
{
    public override void OnAttach()
    {
        // Initialize editor
    }

    public override void OnImGuiRender()
    {
        // Render all panels
        _sceneHierarchyPanel.OnImGuiRender();
        _propertiesPanel.OnImGuiRender();
    }
}

// Editor/ViewportLayer.cs
public class ViewportLayer : Layer
{
    public override void OnUpdate(TimeSpan deltaTime)
    {
        // Update viewport camera
    }
}
```

**LayerStack**: Manages layer order
```csharp
public class Application
{
    private LayerStack _layerStack = new();

    public void PushLayer(Layer layer)
    {
        _layerStack.PushLayer(layer);
    }

    public void Run()
    {
        foreach (var layer in _layerStack)
        {
            layer.OnUpdate(deltaTime);
        }
    }
}
```

### 8. Renderer API Abstraction

**All OpenGL calls** go through `IRendererAPI`

**✅ CORRECT**:
```csharp
// Engine/Renderer/Graphics2D.cs
public class Graphics2D
{
    private readonly IRendererAPI _rendererApi;

    public void DrawQuad(/* params */)
    {
        // Use renderer API abstraction
        _rendererApi.DrawIndexed(_vertexArray, indexCount);
    }
}
```

**❌ WRONG**:
```csharp
// Direct OpenGL calls outside Platform layer
public class Graphics2D
{
    public void DrawQuad(/* params */)
    {
        GL.DrawElements(/* ... */); // WRONG - not abstracted!
    }
}
```

**Abstraction Layers**:
```
Application Code (Graphics2D, Graphics3D)
    ↓
IRendererAPI (interface)
    ↓
SilkNetRendererApi (OpenGL implementation in Platform/)
    ↓
OpenGL (GL.* calls)
```

### 9. Interface Naming and Design

**Pattern**: `I<ClassName>`

```csharp
// ✅ CORRECT
public interface ISceneManager { }
public class SceneManager : ISceneManager { }

public interface ITextureFactory { }
public class TextureFactory : ITextureFactory { }

// ❌ WRONG
public interface SceneManagerInterface { }  // WRONG suffix
public interface SceneMgr { }               // WRONG abbreviation
```

**When to create interfaces**:
- Service will be injected via DI ✅
- Multiple implementations possible ✅
- Testing requires mocking ✅
- Public API needing abstraction ✅

**When to skip interfaces**:
- POCOs and data classes
- Internal implementation details
- Single concrete implementation with no abstraction value

### 10. Event System Patterns

**All events** inherit from `Event` base class

```csharp
// Engine/Events/Event.cs
public abstract class Event
{
    public bool Handled { get; set; }
}

// Specific event types
public class KeyPressedEvent : Event
{
    public KeyCode KeyCode { get; }
    public bool Repeat { get; }
}

public class WindowResizeEvent : Event
{
    public int Width { get; }
    public int Height { get; }
}
```

**Event Handling**: Via layer OnEvent
```csharp
public class EditorLayer : Layer
{
    public override void OnEvent(Event e)
    {
        if (e is KeyPressedEvent keyEvent)
        {
            if (keyEvent.KeyCode == KeyCode.S && IsCtrlPressed())
            {
                SaveScene();
                e.Handled = true;
            }
        }
    }
}
```

## Code Review Checklist

### Project Structure
- [ ] Code is in correct project (Engine vs Editor vs ECS)
- [ ] Code is in correct folder matching namespace
- [ ] Namespace follows `<Project>.<Folder>` convention
- [ ] No editor code in Engine project
- [ ] No engine implementation details in Editor (uses interfaces)

### Component/System Architecture
- [ ] Components in `Engine/Scene/Components/`
- [ ] Systems in `Engine/Scene/Systems/`
- [ ] Components are data-only (minimal logic)
- [ ] Systems implement `ISystem` interface
- [ ] System has appropriate priority value
- [ ] System registered in `SceneSystemRegistry`

### Dependency Injection
- [ ] No static singletons (except constants classes)
- [ ] Services use constructor injection
- [ ] Services registered in Program.cs
- [ ] Interfaces defined for injectable services

### Design Patterns
- [ ] Resource loading uses factory pattern
- [ ] Constants classes used instead of magic numbers
- [ ] IDisposable implemented for unmanaged resources
- [ ] Layer system used for application organization
- [ ] Event system used for decoupled communication

### Platform Abstraction
- [ ] No direct OpenGL calls outside Platform/ folder
- [ ] All GL calls go through IRendererAPI
- [ ] Platform-specific code isolated to Platform/SilkNet/

### Naming Conventions
- [ ] Classes use PascalCase
- [ ] Interfaces use IPascalCase
- [ ] Private fields use _camelCase
- [ ] Methods and properties use PascalCase
- [ ] Components suffixed with "Component"
- [ ] Systems suffixed with "System"

## Output Format

**Issue**: [Architectural inconsistency]
**Location**: [File path]
**Pattern Violation**: [Which architectural principle is violated]
**Recommendation**: [How to fix with code example or file move]
**Priority**: [High/Medium/Low]

### Example Output
```text
**Issue**: Component defined outside Components/ folder
**Location**: Engine/Animation/SpriteAnimatorComponent.cs
**Pattern Violation**: Component organization - all components must live in Engine/Scene/Components/
**Recommendation**:
Move file from:
  Engine/Animation/SpriteAnimatorComponent.cs
To:
  Engine/Scene/Components/SpriteAnimatorComponent.cs

Update namespace:
  namespace Engine.Scene.Components; // (was Engine.Animation)

**Priority**: High
```

## Reference Documentation
- **Architecture**: `CLAUDE.md` - Complete architectural guidelines
- **Module Docs**: `docs/modules/` - System-specific documentation
- **Project Structure**: `README.md` - Solution organization

## Integration with Agents
This skill works across all agents to ensure architectural consistency regardless of domain.

## Tool Restrictions
None - this skill may read code, analyze structure, and suggest refactoring.
