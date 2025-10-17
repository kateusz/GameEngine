# Claude Code Repository Guidelines

This document provides comprehensive guidelines for Claude Code agents working with this C#/.NET 9.0 game engine project. These guidelines ensure consistent, high-quality contributions that align with the project's architecture and development practices.

## Table of Contents

- [Project Overview](#project-overview)
- [Architecture & Organization](#architecture--organization)
- [Specialized Agents](#specialized-agents)
- [Development Guidelines](#development-guidelines)
- [Code Standards](#code-standards)
- [Testing & Quality Assurance](#testing--quality-assurance)
- [Documentation Requirements](#documentation-requirements)
- [Common Workflows](#common-workflows)

---

## Project Overview

### Core Technology Stack

- **Language**: C# with .NET 9.0
- **Rendering**: OpenGL 3.3+ via Silk.NET
- **UI Framework**: ImGui.NET for editor interface
- **Physics**: Box2D.NetStandard
- **Audio**: OpenAL via Silk.NET
- **Asset Loading**: StbImageSharp, Silk.NET.Assimp
- **Logging**: NLog

### Project Goals

- Modern, component-based game engine with ECS architecture
- Cross-platform support (Windows, macOS, Linux)
- Hot-reloadable C# scripting system
- Comprehensive visual editor with ImGui
- High-performance 2D/3D rendering pipeline
- Developer-friendly APIs and tooling

---

## Architecture & Organization

### Solution Structure

```
GameEngine/
├── Engine/              # Core engine runtime
│   ├── Audio/          # OpenAL audio system
│   ├── Core/           # Application framework, layer system
│   ├── Events/         # Event system (input, window)
│   ├── ImGuiNet/       # ImGui integration layer
│   ├── Math/           # Vector, matrix, transforms
│   ├── Platform/       # Platform-specific abstractions
│   ├── Renderer/       # OpenGL rendering pipeline
│   ├── Scene/          # Scene management, serialization
│   ├── Scripting/      # Roslyn-based script engine
│   └── UI/             # UI system integration
│
├── ECS/                # Entity Component System implementation
│
├── Editor/             # Visual editor application
│   ├── Managers/       # Editor state management
│   ├── Panels/         # UI panels (hierarchy, inspector, etc.)
│   ├── Popups/         # Dialogs and modal windows
│   ├── Publisher/      # Build and publishing tools
│   └── Resources/      # Editor-specific assets
│
├── Runtime/            # Standalone game runtime
├── Sandbox/            # Testing and experimentation
├── Benchmark/          # Performance benchmarking tools
├── games scripts/      # Sample game projects
└── docs/               # Technical documentation
```

### Key Architectural Patterns

#### Entity Component System (ECS)
- **Entity**: Lightweight container with GUID identifier
- **Component**: Data-only structs/classes (Transform, Sprite, Mesh, Script, etc.)
- **System**: Logic processors that operate on entities with specific component combinations
- **Scene**: Container for entities with hierarchical relationships

#### Layer System
- Event propagation through ordered layers
- Each layer can consume or pass events to next layer
- Editor uses ImGuiLayer for UI, ViewportLayer for scene rendering

#### Renderer Architecture
- **Renderer2D**: Batched quad rendering with texture atlasing
- **Renderer3D**: Model rendering with material support
- **RenderCommand**: Abstraction over OpenGL state management
- **Shader**: Managed GLSL shader compilation and uniforms

---

## Specialized Agents

This project uses specialized Claude Code agents for different development domains:

### game-engine-expert (Red)
**Use for:**
- OpenGL rendering implementation and optimization
- ECS architecture design and performance tuning
- Audio system integration and spatial audio
- Physics integration and collision optimization
- Low-level performance engineering
- Memory management and allocation strategies
- Cross-platform compatibility issues

**Expertise:**
- Modern OpenGL (VAO, VBO, FBO, shaders)
- Data-oriented design for ECS
- .NET performance optimization (spans, stackalloc, unsafe)
- SilkNet API integration
- Multithreading and parallel processing

### game-editor-architect (Blue)
**Use for:**
- ImGui panel and window development
- Asset pipeline and import systems
- Serialization (JSON, binary formats)
- Project management workflows
- Build system and publishing tools
- Editor UI/UX improvements
- Tool development for artist/designer workflows

**Expertise:**
- ImGui immediate-mode UI patterns
- Asset management architectures
- File I/O and project structure
- Editor state management
- Integration with external tools

---

## Development Guidelines

### When Making Changes

1. **Understand Context First**
   - Read relevant module documentation in `docs/modules/`
   - Review existing implementations before proposing changes
   - Check for similar patterns elsewhere in codebase

2. **Choose the Right Agent**
   - Engine runtime code → `game-engine-expert`
   - Editor tools and UI → `game-editor-architect`
   - Ask if uncertain which agent is appropriate

3. **Maintain Architectural Consistency**
   - Follow existing patterns for similar features
   - Respect component/system separation in ECS
   - Keep rendering code in Renderer namespace
   - Keep editor-specific code in Editor project

4. **Consider Performance**
   - Minimize allocations in hot paths (OnUpdate, render loops)
   - Use object pooling for frequently created/destroyed objects
   - Profile before and after significant changes
   - Document performance characteristics in code comments

5. **Ensure Cross-Platform Compatibility**
   - Test on multiple platforms when possible
   - Use platform abstractions from `Platform/` namespace
   - Avoid platform-specific file paths or APIs
   - Use `Path.Combine()` for path construction

### Code Organization Principles

#### Engine Project
- **Core**: Application lifecycle, layer management, time stepping
- **Events**: Event definitions and dispatching
- **Renderer**: All OpenGL code, shaders, render commands
- **Scene**: Entity management, scene graphs, serialization
- **Scripting**: Script compilation, hot reload, debugging
- **Audio**: Sound loading, playback, 3D positioning

#### Editor Project
- **Panels**: Reusable ImGui panels (SceneHierarchy, Properties, Console)
- **Managers**: Singleton managers for editor state (ProjectManager, SceneManager)
- **Popups**: Modal dialogs and temporary windows
- **Publisher**: Game building and export functionality

#### ECS Project
- Pure ECS implementation
- No engine-specific dependencies
- Reusable across different projects

---

## Code Standards

### C# Style Guidelines

```csharp
// Use modern C# features appropriately
public record struct TransformComponent(Vector3 Position, Vector3 Rotation, Vector3 Scale);

// Prefer nullable reference types
public class Entity
{
    public string? Name { get; set; }
    private Scene? _parentScene;
}

// Use pattern matching
if (entity.GetComponent<SpriteRendererComponent>() is { } sprite)
{
    sprite.Color = Vector4.One;
}

// Implement proper disposal for unmanaged resources
public class Texture : IDisposable
{
    private uint _rendererID;

    public void Dispose()
    {
        if (_rendererID != 0)
        {
            GL.DeleteTexture(_rendererID);
            _rendererID = 0;
        }
        GC.SuppressFinalize(this);
    }
}
```

### OpenGL Best Practices

```csharp
// Always check for errors in development builds
private static void CheckGLError(string context)
{
    #if DEBUG
    var error = GL.GetError();
    if (error != GLEnum.NoError)
    {
        Logger.Error($"OpenGL Error in {context}: {error}");
    }
    #endif
}

// Batch state changes
public class RenderCommand
{
    private static BlendMode _currentBlendMode = BlendMode.None;

    public static void SetBlendMode(BlendMode mode)
    {
        if (_currentBlendMode == mode) return;

        // Apply OpenGL state change
        _currentBlendMode = mode;
    }
}

// Use vertex array objects correctly
public class Mesh : IDisposable
{
    private uint _vao, _vbo, _ebo;

    public void Bind()
    {
        GL.BindVertexArray(_vao);
    }

    public void Unbind()
    {
        GL.BindVertexArray(0);
    }
}
```

### ECS Component Design

```csharp
// Components should be data-only
public class TransformComponent
{
    public Vector3 Translation { get; set; }
    public Vector3 Rotation { get; set; }
    public Vector3 Scale { get; set; } = Vector3.One;

    // Matrix calculation is acceptable
    public Matrix4x4 GetTransform()
    {
        return Matrix4x4.CreateScale(Scale)
             * Matrix4x4.CreateFromQuaternion(Quaternion.CreateFromYawPitchRoll(
                   Rotation.Y, Rotation.X, Rotation.Z))
             * Matrix4x4.CreateTranslation(Translation);
    }
}

// Avoid logic in components
// Instead, create systems that operate on components
public class PhysicsSystem
{
    public void Update(Scene scene, TimeSpan deltaTime)
    {
        foreach (var entity in scene.GetEntitiesWith<PhysicsComponent>())
        {
            var physics = entity.GetComponent<PhysicsComponent>();
            var transform = entity.GetComponent<TransformComponent>();

            // Update transform based on physics simulation
            transform.Translation += physics.Velocity * (float)deltaTime.TotalSeconds;
        }
    }
}
```

### Performance Optimization Patterns

```csharp
// Use Span<T> for stack allocations
Span<Vertex> vertices = stackalloc Vertex[4];
vertices[0] = new Vertex { Position = new Vector3(-0.5f, -0.5f, 0) };

// Object pooling for frequently created objects
public class EntityPool
{
    private readonly Queue<Entity> _pool = new();

    public Entity Rent()
    {
        return _pool.Count > 0 ? _pool.Dequeue() : new Entity();
    }

    public void Return(Entity entity)
    {
        entity.Reset();
        _pool.Enqueue(entity);
    }
}

// Avoid boxing in hot paths
public class ComponentArray<T> where T : class
{
    private T[] _components;
    // Direct array access, no boxing
}
```

### Naming Conventions

- **Classes/Structs**: PascalCase (`TransformComponent`, `Renderer2D`)
- **Interfaces**: IPascalCase (`IDisposable`, `ISerializable`)
- **Methods**: PascalCase (`UpdateCamera`, `GetComponent`)
- **Properties**: PascalCase (`IsEnabled`, `EntityCount`)
- **Private Fields**: _camelCase (`_rendererID`, `_entityMap`)
- **Local Variables**: camelCase (`deltaTime`, `worldMatrix`)
- **Constants**: PascalCase (`MaxEntities`, `DefaultBufferSize`)

### Editor UI Constants

The Editor project uses a centralized constants class to maintain consistent UI styling and avoid magic numbers:

```csharp
using Editor.UI;

// Always import the EditorUIConstants namespace for UI code
namespace Editor.Panels;

public class MyPanel
{
    public void Render()
    {
        // Use named constants instead of magic numbers
        if (ImGui.Button("Save", new Vector2(EditorUIConstants.StandardButtonWidth, 
                                              EditorUIConstants.StandardButtonHeight)))
        {
            Save();
        }
        
        // Use standard colors for consistent UX
        ImGui.PushStyleColor(ImGuiCol.Text, EditorUIConstants.ErrorColor);
        ImGui.Text("Error message");
        ImGui.PopStyleColor();
        
        // Use standard padding for consistent spacing
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, 
            new Vector2(EditorUIConstants.StandardPadding, EditorUIConstants.StandardPadding));
        
        // Use layout ratios for property editors
        ImGui.SetColumnWidth(0, totalWidth * EditorUIConstants.PropertyLabelRatio);
    }
}
```

**Available Constants:**
- **Button Sizes**: `StandardButtonWidth`, `WideButtonWidth`, `MediumButtonWidth`, `SmallButtonSize`
- **Layout Ratios**: `PropertyLabelRatio` (0.33f), `PropertyInputRatio` (0.67f)
- **Column Widths**: `DefaultColumnWidth`, `WideColumnWidth`, `FilterInputWidth`
- **Spacing**: `StandardPadding`, `LargePadding`, `SmallPadding`
- **Input Buffers**: `MaxTextInputLength`, `MaxNameLength`, `MaxPathLength`
- **Colors**: `ErrorColor`, `WarningColor`, `SuccessColor`, `InfoColor`
- **Axis Colors**: `AxisXColor` (red), `AxisYColor` (green), `AxisZColor` (blue)
- **UI Sizes**: `SelectorListBoxWidth`, `MaxVisibleListItems`

**Benefits:**
- Consistent UI appearance across all editor panels
- Easy to adjust styling globally by changing constants
- Self-documenting code with descriptive constant names
- Prevents typos and inconsistencies from duplicated literals

---

## Testing & Quality Assurance

### Testing Strategy

1. **Unit Tests**
   - Test ECS operations (add/remove components, entity lifecycle)
   - Test math utilities (vector operations, matrix transforms)
   - Test serialization/deserialization
   - Located in test projects (to be created)

2. **Integration Tests**
   - Scene loading/saving
   - Script compilation and hot reload
   - Asset loading pipeline
   - Run via Benchmark project

3. **Performance Benchmarks**
   - Render performance (draw calls, batch efficiency)
   - ECS iteration performance
   - Memory allocation tracking
   - Use Benchmark project with BenchmarkDotNet

### Manual Testing Checklist

When making significant changes:

- [ ] Test in Editor (play mode)
- [ ] Test in standalone Runtime
- [ ] Verify hot reload still works (if touching scripting system)
- [ ] Check console for warnings/errors
- [ ] Profile frame time if touching rendering or ECS
- [ ] Test with sample projects in `games scripts/`

### Build Validation

```bash
# Always ensure solution builds cleanly
dotnet clean
dotnet restore
dotnet build

# Run editor to verify no runtime issues
cd Editor
dotnet run
```

---

## Documentation Requirements

### Code Documentation

```csharp
/// <summary>
/// Renders a batch of quads with the specified texture.
/// </summary>
/// <param name="texture">Texture to bind for rendering.</param>
/// <param name="quadCount">Number of quads in the batch.</param>
/// <remarks>
/// This method assumes vertex data has already been uploaded to the GPU.
/// Maximum batch size is limited by MaxQuadsPerBatch constant.
/// </remarks>
public void RenderBatch(Texture texture, int quadCount)
{
    // Implementation
}
```

### When to Update Documentation

Update `docs/modules/` when:
- Adding new major systems or features
- Changing architecture of existing systems
- Modifying public APIs significantly
- Adding new design patterns

Update `docs/opengl-rendering/` when:
- Changing rendering pipeline
- Adding new shader techniques
- Modifying batching strategies

### Documentation Structure

Each module doc should include:
1. **Overview**: High-level description
2. **Architecture**: Key classes and relationships
3. **Usage Examples**: Code snippets showing common scenarios
4. **API Reference**: Important public methods
5. **Performance Considerations**: Optimization notes
6. **Future Improvements**: Known limitations and plans

---

## Common Workflows

### Adding a New Component

1. Create component class in `Engine/Scene/Components/`
2. Add to component registry if needed
3. Implement serialization support
4. Add inspector UI in `Editor/Panels/PropertiesPanel.cs`
5. Update documentation in `docs/modules/ecs-gameobject.md`

Example:
```csharp
// Engine/Scene/Components/MyComponent.cs
public class MyComponent
{
    public float Value { get; set; }
    public bool IsEnabled { get; set; } = true;
}

// Editor/Panels/PropertiesPanel.cs
private void DrawMyComponent(MyComponent component)
{
    ImGui.DragFloat("Value", ref component.Value);
    ImGui.Checkbox("Enabled", ref component.IsEnabled);
}
```

### Adding a New Renderer Feature

1. Implement rendering logic in `Engine/Renderer/`
2. Create shader files if needed
3. Add render command abstraction
4. Update `Renderer2D` or `Renderer3D` class
5. Test with Sandbox project
6. Document in `docs/opengl-rendering/`

### Creating a New Editor Panel

1. Create panel class in `Editor/Panels/`
2. Inherit from base panel interface
3. Implement `OnImGuiRender()` method
4. Register in `EditorLayer.cs`
5. Add menu item for showing/hiding panel
6. Document in `docs/modules/editor.md`

Example:
```csharp
public class MyNewPanel
{
    private bool _isOpen = true;

    public void OnImGuiRender()
    {
        if (!_isOpen) return;

        ImGui.Begin("My Panel", ref _isOpen);
        // Panel content
        ImGui.End();
    }
}
```

### Extending the Scripting System

1. Modify `Engine/Scripting/ScriptEngine.cs`
2. Update script base class if needed
3. Test hot reload functionality
4. Update script template generation
5. Document new APIs for game developers

### Adding Third-Party Dependencies

1. Add NuGet package to relevant .csproj file
2. Verify cross-platform compatibility
3. Update README.md dependencies section
4. Document integration in relevant module docs
5. Consider licensing implications

---

## Additional Resources

### Key Files to Reference

- **Project Structure**: `README.md`
- **Build Configuration**: `GameEngine.sln`, `.csproj` files
- **OpenGL Workflows**: `docs/opengl-rendering/`
- **System Documentation**: `docs/modules/`

### Useful Commands

```bash
# Build entire solution
dotnet build

# Run editor
cd Editor && dotnet run

# Run sandbox
cd Sandbox && dotnet run

# Clean build artifacts
dotnet clean

# Restore NuGet packages
dotnet restore
```

### Getting Help

When uncertain about implementation details:
- Check existing similar implementations in codebase
- Review module documentation in `docs/`
- Examine sample projects in `games scripts/`
- Use appropriate specialized agent for domain-specific questions

---

## Contributing Philosophy

### Core Values

1. **Performance Matters**: This is a game engine - frame time is critical
2. **Developer Experience**: APIs should be intuitive and well-documented
3. **Cross-Platform**: Write once, run anywhere
4. **Maintainability**: Code should be readable and well-structured
5. **Pragmatism**: Ship working features over perfect abstractions

### Quality Standards

- Code must build without warnings
- Public APIs must be documented
- Breaking changes require migration guide
- Performance-critical code should be profiled
- Editor features should enhance workflow, not hinder it

### When in Doubt

- Prefer existing patterns over new abstractions
- Ask which agent is appropriate for the task
- Reference similar implementations in codebase
- Document architectural decisions in code comments
- Test changes with sample projects before committing

---

**Remember**: This is a game engine built for game developers. Every decision should consider the end user experience - both for developers using the engine and players experiencing the games built with it.
