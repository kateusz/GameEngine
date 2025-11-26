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
- **Audio**: OpenAL via Silk.NET (with Ogg Vorbis support via NVorbis)
- **Asset Loading**: StbImageSharp, Silk.NET.Assimp
- **Logging**: Serilog (multi-sink with async support)
- **Dependency Injection**: DryIoc IoC container
- **Utilities**: CSharpFunctionalExtensions, ZLinq

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
│   ├── Animation/      # 2D sprite animation system with events
│   ├── Audio/          # OpenAL audio system (Ogg, WAV support)
│   ├── Core/           # Application framework, layer system
│   ├── Events/         # Event system (input, window)
│   ├── ImGuiNet/       # ImGui integration layer
│   ├── Math/           # Vector, matrix, transforms
│   ├── Platform/       # Platform-specific abstractions (SilkNet)
│   ├── Renderer/       # OpenGL rendering pipeline
│   ├── Scene/          # Scene management, serialization
│   │   ├── Components/ # All ECS component definitions (18 components)
│   │   ├── Systems/    # ECS system implementations
│   │   └── Serializer/ # JSON scene/prefab serialization
│   ├── Scripting/      # Roslyn-based script engine
│   └── UI/             # UI system integration
│
├── ECS/                # Pure ECS implementation
│   └── System/         # ISystem interface, SystemManager
│
├── Editor/             # Visual editor application
│   ├── ComponentEditors/  # Component property editors
│   │   └── Core/       # Component editor infrastructure (registry, interfaces)
│   ├── Features/       # Feature-based organization
│   │   ├── Project/    # Project management (creation, loading, recent projects)
│   │   ├── Scene/      # Scene management (hierarchy, context, settings)
│   │   └── Settings/   # Editor preferences and settings UI
│   ├── Input/          # Keyboard shortcuts and input handling
│   ├── Logging/        # Console panel integration with Serilog
│   ├── Managers/       # Legacy managers (being phased into Features)
│   ├── Panels/         # Core UI panels (Console, Properties, ContentBrowser, etc.)
│   ├── Publisher/      # Game build and publishing tools
│   ├── Systems/        # Editor-specific ECS systems (EditorCameraSystem)
│   ├── UI/             # Reusable UI components and styling
│   │   ├── Constants/  # EditorUIConstants for consistent styling
│   │   ├── Drawers/    # Reusable UI drawing utilities (buttons, modals, tables)
│   │   ├── Elements/   # Complex UI elements (drag-drop, selectors, menus)
│   │   └── FieldEditors/ # Generic field editors for primitive types
│   ├── Utilities/      # Helper classes (rulers, manipulators, converters)
│   ├── Windows/        # Specialized windows (AnimationTimeline)
│   └── Resources/      # Editor-specific assets (icons, fonts)
│
├── Runtime/            # Standalone game runtime
├── Sandbox/            # Testing and experimentation
├── Benchmark/          # Performance benchmarking tools
├── tests/              # Unit test projects (ECS.Tests, Engine.Tests)
├── games scripts/      # Sample game projects
└── docs/               # Technical documentation
    ├── modules/        # 17 module documentation files
    ├── opengl-rendering/ # OpenGL workflow guides
    └── specifications/ # Feature specifications and designs
```

### Key Architectural Patterns

#### Entity Component System (ECS)
- **Entity**: Lightweight container with GUID identifier
- **Component**: Data-only structs/classes (18 component types available)
- **System**: Logic processors implementing `ISystem` interface
- **SystemManager**: Orchestrates system execution with priority-based ordering
- **Scene**: Container for entities with hierarchical relationships
- **SceneSystemRegistry**: Centralized system registration and configuration

**Priority-Based System Execution:**
Systems execute in priority order (lower numbers = earlier execution):
- ScriptUpdateSystem (Priority 100)
- AnimationSystem (Priority 198)
- TileMapRenderSystem (Priority 200)
- SpriteRenderingSystem, ModelRenderingSystem, etc.

#### Dependency Injection with DryIoc
- **All systems, panels, and managers** use constructor injection
- **No static singletons** - all singletons registered in IoC container
- Program.cs configures 50+ service registrations
- Enables testability and clean separation of concerns

#### Factory Pattern
Extensive use of factories for resource creation:
- **TextureFactory**: Texture loading with caching
- **ShaderFactory**: Shader compilation with caching
- **RendererApiFactory**: Platform-specific renderer creation
- **SceneFactory**: Scene instantiation with DI
- **AudioClipFactory**: Audio loading with format detection

#### Layer System
- Event propagation through ordered layers
- Each layer can consume or pass events to next layer
- Editor uses ImGuiLayer for UI, ViewportLayer for scene rendering

#### Renderer Architecture
**Interface-Driven Design:**
- **IGraphics2D** / **Graphics2D**: Batched 2D rendering (10,000 quads per batch)
- **IGraphics3D** / **Graphics3D**: 3D immediate-mode rendering
- **IRendererAPI** / **SilkNetRendererApi**: Platform abstraction over OpenGL
- **RenderingConstants**: Centralized rendering configuration (MaxQuads, MaxTextureSlots, etc.)

**Key Features:**
- Batched rendering with texture atlasing
- Shader and texture caching for performance
- Multi-pass rendering with framebuffers
- GPU-based entity picking for editor
- Debug visualization (physics, bounds)

### Major Engine Systems

#### 2D Animation System
Complete sprite animation system with comprehensive tooling:
- **AnimationComponent**: Playback control (play, pause, speed, looping)
- **AnimationAsset**: JSON-based animation definitions with clips and frames
- **AnimationSystem**: Automatic ECS-based updates (Priority 198)
- **AnimationController**: Scripting API for runtime control
- **Frame Events**: Trigger callbacks at specific animation frames
- **AnimationTimelineWindow**: Visual timeline editor in Editor
- **Documentation**: Full guides in `docs/modules/animation-event-system.md`

#### TileMap System
Multi-layer tilemap rendering for 2D level design:
- **TileMapComponent**: Container for multiple tile layers
- **TileMapLayer**: Individual layer with tile data
- **TileSet**: Configuration for tile textures and properties
- **TileMapRenderSystem**: Automatic batched rendering (Priority 200)
- **TileMapPanel**: Visual editor with layer management
- **Serialization**: Custom JSON converters for efficient storage
- **Documentation**: Quick-start and usage guides in `docs/modules/tilemap-*.md`

#### Audio System
3D spatial audio with multiple format support:
- **AudioSourceComponent**: 3D positioned audio emitter
- **AudioListenerComponent**: Audio receiver (typically on camera)
- **AudioSystem**: Updates 3D audio positioning each frame
- **Format Support**: WAV, Ogg Vorbis (via NVorbis)
- **AudioClipFactory**: Automatic format detection and loading

### Available Components

The engine provides 18 built-in component types:

**Core Components:**
- **IDComponent**: GUID-based entity identification
- **TagComponent**: Entity naming and tagging
- **TransformComponent**: Position, rotation, scale with matrix calculation

**Rendering Components:**
- **SpriteRendererComponent**: 2D sprite with color tint
- **SubTextureRendererComponent**: Sprite atlas/texture region rendering
- **MeshComponent**: 3D mesh data
- **ModelRendererComponent**: 3D model with material
- **CameraComponent**: Camera configuration (orthographic/perspective)

**Physics Components (Box2D):**
- **RigidBody2DComponent**: 2D physics body (dynamic, kinematic, static)
- **BoxCollider2DComponent**: 2D box collision shape

**Scripting:**
- **NativeScriptComponent**: Hot-reloadable C# script attachment

**Audio:**
- **AudioSourceComponent**: 3D audio source
- **AudioListenerComponent**: Audio receiver

**Advanced Systems:**
- **AnimationComponent**: 2D sprite animation with events
- **TileMapComponent**: Multi-layer tilemap
- **TileMapLayer**: Individual tilemap layer
- **TileSet**: Tileset configuration
- **TileComponent**: Individual tile data

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

### Editor Organization: Features vs. Panels

The Editor uses a hybrid organization pattern:

**Use `Features/{FeatureName}/` when:**
- Implementing a cohesive feature with multiple related components
- The feature has its own manager, panel(s), popups, and settings
- Examples: Project management (creation, loading, recent projects), Scene management (hierarchy, context), Settings

**Use `Panels/` when:**
- Creating standalone utility panels without complex feature context
- The panel is relatively self-contained
- Examples: Console, RendererStats, PerformanceMonitor, ContentBrowser

**Use `ComponentEditors/` when:**
- Creating property editors for ECS components
- The editor will be registered in ComponentEditorRegistry
- All individual component editors (Transform, Sprite, Camera, etc.)

**Use `UI/` when:**
- Creating reusable UI utilities (Drawers)
- Creating reusable UI components (Elements)
- Creating generic field editors (FieldEditors)

### When Making Changes

1. **Understand Context First**
   - Read relevant module documentation in `docs/modules/`
   - Review existing implementations before proposing changes
   - Check for similar patterns elsewhere in codebase
   - Note: Recent focus has been on performance optimization and proper resource management

2. **Choose the Right Agent**
   - Engine runtime code → `game-engine-expert`
   - Editor tools and UI → `game-editor-architect`
   - Ask if uncertain which agent is appropriate

3. **Maintain Architectural Consistency**
   - Follow existing patterns for similar features
   - Use dependency injection - never create static singletons
   - Use factory pattern for resource creation
   - Respect component/system separation in ECS
   - Keep rendering code in Renderer namespace
   - Keep editor-specific code in Editor project
   - Use constants classes (EditorUIConstants, RenderingConstants) instead of magic numbers

4. **Consider Performance** (High Priority)
   - Minimize allocations in hot paths (OnUpdate, render loops)
   - Use caching where appropriate (see ShaderFactory, TextureFactory examples)
   - Prefer static reflection caching over repeated reflection calls
   - Use object pooling for frequently created/destroyed objects
   - Profile before and after significant changes
   - Document performance characteristics in code comments
   - Implement proper IDisposable patterns for unmanaged resources

5. **Ensure Cross-Platform Compatibility**
   - Test on multiple platforms when possible
   - Use platform abstractions from `Platform/SilkNet/` namespace
   - All OpenGL code must go through IRendererAPI interface
   - Avoid platform-specific file paths or APIs
   - Use `Path.Combine()` for path construction

### Code Organization Principles

#### Engine Project
- **Animation**: 2D sprite animation system with frame events and timeline editor
- **Audio**: Sound loading (Ogg, WAV), playback, 3D spatial positioning
- **Core**: Application lifecycle, layer management, time stepping
- **Events**: Event definitions and dispatching
- **Platform**: SilkNet implementations for all platform-specific code
- **Renderer**: All OpenGL code via IRendererAPI abstraction
- **Scene**: Entity management, ECS systems, scene graphs, JSON serialization
- **Scripting**: Roslyn-based script compilation, hot reload, hybrid debugging

#### Editor Project
The Editor follows a feature-based organization pattern combined with traditional layering:

**Feature-Based Organization** (`Features/`):
- **Project**: Project creation, loading, management, and recent projects window
- **Scene**: Scene hierarchy panel, scene manager, scene context, and scene settings
- **Settings**: Editor preferences and settings UI

**Component System** (`ComponentEditors/`):
- Individual component editors (Transform, Sprite, Camera, Animation, etc.)
- **Core**: Component editor infrastructure (IComponentEditor, ComponentEditorRegistry)
- Registered via DI for extensibility

**UI Infrastructure** (`UI/`):
- **Constants**: EditorUIConstants for consistent styling across all panels
- **Drawers**: Reusable drawing utilities (ButtonDrawer, ModalDrawer, TableDrawer, TreeDrawer)
- **Elements**: Complex UI components (drag-drop targets, component selector, entity context menu)
- **FieldEditors**: Generic field editors for primitive types (int, float, Vector2, etc.)

**Core Panels** (`Panels/`):
- Console, Properties, ContentBrowser, TileMapPanel, VectorPanel
- PerformanceMonitor, RendererStats, EditorToolbar
- Each panel implements interface-based design for testability

**Specialized Systems**:
- **Input**: Keyboard shortcuts manager and shortcut panel
- **Logging**: Console panel sink for Serilog integration
- **Publisher**: Game build and export functionality
- **Systems**: Editor-specific ECS systems (EditorCameraSystem)
- **Utilities**: Helper classes (ObjectManipulator, RulerTool, ViewportRuler)
- **Windows**: Specialized windows (AnimationTimelineWindow)

#### ECS Project
- Pure ECS implementation with ISystem interface
- SystemManager for priority-based execution
- No engine-specific dependencies
- Fully unit tested

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

// For reference types prefer records with init-only properties and mark properties as required when appropriate
public record SpriteRendererComponent
{
    public required string TexturePath { get; init; }
    public Vector4 Color { get; set; }
}

For value types, use record structs when appropriate

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

### Rendering Constants

Similar to EditorUIConstants, the rendering system uses `RenderingConstants` to centralize all rendering-related magic numbers:

```csharp
using Engine.Renderer;

// Always use RenderingConstants for rendering configuration
public class MyRenderSystem
{
    public void Initialize()
    {
        // Use constants instead of magic numbers
        int maxQuads = RenderingConstants.MaxQuads;          // 10,000
        int maxTextures = RenderingConstants.MaxTextureSlots; // 16
        int verticesPerQuad = RenderingConstants.QuadVertexCount; // 4
        int indicesPerQuad = RenderingConstants.QuadIndexCount;   // 6
    }
}
```

**Available Constants:**
- **Batch Sizes**: `MaxQuads` (10,000), `QuadVertexCount` (4), `QuadIndexCount` (6)
- **Textures**: `MaxTextureSlots` (16)
- **Framebuffers**: `MaxFramebufferSize` (8,192)
- **Line Rendering**: `MaxLines`, `MaxLineVertices`, `MaxLineIndices`

**Architectural Rule:**
Never create singleton-style static classes! Use IoC container for singleton registration. The only exceptions are pure constant classes like `EditorUIConstants` and `RenderingConstants`.

### Editor UI Infrastructure

The Editor provides a comprehensive UI infrastructure to promote code reuse and consistency across all panels:

#### UI Drawers (`Editor/UI/Drawers/`)
Reusable drawing utilities that encapsulate common ImGui patterns. These are static utility classes for drawing UI elements:

- **ButtonDrawer**: Consistent button rendering with types (Primary, Secondary, Danger, Success)
- **ModalDrawer**: Modal dialog/popup rendering with backdrop and centering
- **TableDrawer**: Table rendering with headers and row formatting
- **TreeDrawer**: Tree node rendering for hierarchical data
- **TextDrawer**: Text rendering with color coding based on MessageType
- **LayoutDrawer**: Layout utilities for spacing, separators, and alignment
- **DragDropDrawer**: Drag-and-drop visualization and handling

```csharp
// Example: Using ButtonDrawer for consistent styling
if (ButtonDrawer.DrawButton("Save", ButtonDrawer.ButtonType.Primary))
{
    SaveProject();
}

// Example: Using ModalDrawer for confirmation dialogs
private bool _showDeleteConfirm;

ModalDrawer.RenderConfirmationModal(
    title: "Delete Confirmation",
    showModal: ref _showDeleteConfirm,
    message: "Are you sure you want to delete this?",
    onOk: () => DeleteItem());

// Example: Using TableDrawer for data display
TableDrawer.BeginTable("MyTable", new[] { "Name", "Type", "Value" });
foreach (var item in items)
{
    TableDrawer.DrawRow(item.Name, item.Type, item.Value.ToString());
}
TableDrawer.EndTable();
```

#### UI Elements (`Editor/UI/Elements/`)
Complex, reusable UI components with internal state and logic:

- **ComponentSelector**: Popup for selecting and adding components to entities
- **EntityContextMenu**: Right-click context menu for entity operations
- **Drag-Drop Targets**: Specialized drop targets for textures, meshes, audio, models, prefabs
- **PrefabManager**: Prefab creation and instantiation UI
- **UIPropertyRenderer**: Generic property rendering for reflection-based editors

```csharp
// Example: Using ComponentSelector
private readonly ComponentSelector _componentSelector = new();

if (ImGui.Button("Add Component"))
{
    _componentSelector.Show(selectedEntity);
}

_componentSelector.Draw(); // Call each frame

// Example: Using drag-drop targets
TextureDropTarget.Draw("Texture", currentTexturePath, (newTexturePath) =>
{
    spriteRenderer.TexturePath = newTexturePath;
});

AudioDropTarget.Draw("Audio Clip", currentAudioPath, (newAudioPath) =>
{
    audioSource.AudioClipPath = newAudioPath;
});
```

#### Field Editors (`Editor/UI/FieldEditors/`)
Generic field editors for primitive types registered in FieldEditorRegistry:

- **BoolFieldEditor**: Checkbox for boolean values
- **IntFieldEditor**: Integer input with drag support
- **FloatFieldEditor**: Float input with drag support
- **DoubleFieldEditor**: Double precision input
- **StringFieldEditor**: Text input for strings
- **Vector2FieldEditor**: 2D vector input with X/Y labels
- **Vector3FieldEditor**: 3D vector input with X/Y/Z labels
- **Vector4FieldEditor**: 4D vector input with X/Y/Z/W labels

```csharp
// Field editors are registered in DI and used automatically by reflection-based editors
// In Program.cs:
container.Register<IFieldEditor<int>, IntFieldEditor>(Reuse.Singleton);
container.Register<IFieldEditor<float>, FloatFieldEditor>(Reuse.Singleton);
container.Register<IFieldEditor<Vector3>, Vector3FieldEditor>(Reuse.Singleton);
// ... etc

// Usage in component editors:
public class MyComponentEditor : IComponentEditor<MyComponent>
{
    private readonly IFieldEditor<float> _floatEditor;
    private readonly IFieldEditor<Vector3> _vectorEditor;

    public MyComponentEditor(
        IFieldEditor<float> floatEditor,
        IFieldEditor<Vector3> vectorEditor)
    {
        _floatEditor = floatEditor;
        _vectorEditor = vectorEditor;
    }

    public void DrawEditor(MyComponent component)
    {
        _floatEditor.DrawField("Speed", ref component.Speed);
        _vectorEditor.DrawField("Position", ref component.Position);
    }
}
```

#### Best Practices for Editor UI

1. **Always use Drawers for common patterns** - Don't reimplement buttons, modals, or tables
2. **Leverage Elements for complex interactions** - Use ComponentSelector, drag-drop targets, etc.
3. **Follow EditorUIConstants** - Never hardcode sizes, colors, or spacing
4. **Dependency Inject Field Editors** - Use FieldEditorRegistry for type-safe field editing
5. **Keep panels focused** - Delegate complex UI logic to Elements or Drawers
6. **Maintain consistency** - If you need a new pattern, create a Drawer instead of duplicating code

---

## Testing & Quality Assurance

### Testing Strategy

1. **Unit Tests**
   - **ECS.Tests**: Entity, component, and system tests
   - **Engine.Tests**: 30+ test files covering Animation, Audio, Components, Serialization, etc.
   - Located in `tests/` directory
   - Run with `dotnet test`
   - Tests use xUnit framework

2. **Integration Tests**
   - Scene loading/saving with complex hierarchies
   - Script compilation and hot reload validation
   - Asset loading pipeline (textures, audio, models)
   - Animation system with frame events
   - TileMap serialization and rendering

3. **Performance Benchmarks**
   - Render performance (draw calls, batch efficiency)
   - ECS iteration performance
   - Memory allocation tracking
   - Physics simulation benchmarks
   - Use Benchmark project with BenchmarkDotNet
   - Check `docs/specifications/physics-benchmark-design.md`

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
2. Implement serialization support (add custom converter if needed)
3. Create component editor in `Editor/ComponentEditors/`
4. Register editor in `ComponentEditorRegistry` (via DI in Program.cs)
5. Update documentation in `docs/modules/ecs-gameobject.md`

Example:
```csharp
// Engine/Scene/Components/MyComponent.cs
public class MyComponent
{
    public float Value { get; set; }
    public bool IsEnabled { get; set; } = true;
}

// Editor/ComponentEditors/MyComponentEditor.cs
public class MyComponentEditor : IComponentEditor<MyComponent>
{
    public void DrawEditor(MyComponent component)
    {
        ImGui.DragFloat("Value", ref component.Value);
        ImGui.Checkbox("Enabled", ref component.IsEnabled);
    }
}

// Program.cs (in ConfigureServices)
container.Register<IComponentEditor<MyComponent>, MyComponentEditor>(Reuse.Singleton);
```

### Creating a Component Editor

Component editors are located in `Editor/ComponentEditors/` and use the UI infrastructure for consistency:

1. Create editor class implementing `IComponentEditor<TComponent>`
2. Inject required field editors via constructor
3. Use field editors for primitive types (int, float, Vector3, etc.)
4. Use UI Elements for complex interactions (drag-drop targets)
5. Use EditorUIConstants for spacing and layout
6. Register in Program.cs DI container

Example:
```csharp
// Editor/ComponentEditors/MyComponentEditor.cs
using Editor.UI.FieldEditors;
using Editor.UI.Elements;
using Editor.UI.Constants;

public class MyComponentEditor : IComponentEditor<MyComponent>
{
    private readonly IFieldEditor<float> _floatEditor;
    private readonly IFieldEditor<Vector3> _vectorEditor;

    public MyComponentEditor(
        IFieldEditor<float> floatEditor,
        IFieldEditor<Vector3> vectorEditor)
    {
        _floatEditor = floatEditor;
        _vectorEditor = vectorEditor;
    }

    public void DrawEditor(MyComponent component)
    {
        // Use field editors for primitive types
        _floatEditor.DrawField("Speed", ref component.Speed);
        _vectorEditor.DrawField("Offset", ref component.Offset);

        // Use drag-drop targets for asset references
        TextureDropTarget.Draw("Icon", component.IconPath, (newPath) =>
        {
            component.IconPath = newPath;
        });

        // Manual ImGui for custom UI, with EditorUIConstants
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (ImGui.TreeNode("Advanced Settings"))
        {
            ImGui.Checkbox("Enabled", ref component.IsEnabled);
            ImGui.TreePop();
        }
    }
}

// Program.cs (in ConfigureServices)
container.Register<IComponentEditor<MyComponent>, MyComponentEditor>(Reuse.Singleton);
```

**Best Practices:**
- Always inject field editors rather than creating them inline
- Use drag-drop targets for all asset references (textures, audio, meshes)
- Follow EditorUIConstants for spacing and layout consistency
- Keep editor logic simple - complex operations should be in systems or managers
- Test editor with sample component data to ensure proper behavior

### Adding a New Renderer Feature

1. Implement rendering logic in `Engine/Renderer/`
2. Create shader files if needed
3. Add render command abstraction
4. Update `Renderer2D` or `Renderer3D` class
5. Test with Sandbox project
6. Document in `docs/opengl-rendering/`

### Creating a New Editor Panel

**Determine Panel Location:**
- Feature-specific panels → `Editor/Features/{FeatureName}/`
- Core UI panels → `Editor/Panels/`
- Specialized windows → `Editor/Windows/`

**Implementation Steps:**
1. Create panel interface (e.g., `IMyNewPanel`)
2. Create panel implementation class
3. Use constructor injection for dependencies
4. Implement `OnImGuiRender()` method using:
   - **EditorUIConstants** for styling consistency
   - **UI Drawers** for common patterns (buttons, modals, tables)
   - **UI Elements** for complex interactions (drag-drop, selectors)
5. Register in Program.cs IoC container
6. Inject into EditorLayer via constructor
7. Add menu item for showing/hiding panel
8. Document in `docs/modules/editor.md`

Example:
```csharp
// Features/MyFeature/IMyFeaturePanel.cs (or Panels/IMyNewPanel.cs)
public interface IMyFeaturePanel
{
    void OnImGuiRender();
}

// Features/MyFeature/MyFeaturePanel.cs
public class MyFeaturePanel : IMyFeaturePanel
{
    private readonly ISceneManager _sceneManager;
    private readonly ModalDrawer _modalDrawer;
    private bool _isOpen = true;
    private bool _showConfirmDialog;

    public MyFeaturePanel(ISceneManager sceneManager)
    {
        _sceneManager = sceneManager;
        _modalDrawer = new ModalDrawer();
    }

    public void OnImGuiRender()
    {
        if (!_isOpen) return;

        ImGui.Begin("My Feature Panel", ref _isOpen);

        // Use ButtonDrawer for consistent button styling
        if (ButtonDrawer.DrawButton("Primary Action", ButtonDrawer.ButtonType.Primary))
        {
            _showConfirmDialog = true;
        }

        // Use ModalDrawer for confirmation dialogs
        _modalDrawer.Draw("Confirm Action", ref _showConfirmDialog, () =>
        {
            TextDrawer.DrawText("Are you sure?", MessageType.Warning);

            if (ButtonDrawer.DrawButton("Confirm", ButtonDrawer.ButtonType.Danger))
            {
                PerformAction();
                _showConfirmDialog = false;
            }

            ImGui.SameLine();

            if (ButtonDrawer.DrawButton("Cancel", ButtonDrawer.ButtonType.Secondary))
            {
                _showConfirmDialog = false;
            }
        });

        ImGui.End();
    }

    private void PerformAction()
    {
        var scene = _sceneManager.GetActiveScene();
        // Panel logic using injected dependencies
    }
}

// Program.cs
container.Register<IMyFeaturePanel, MyFeaturePanel>(Reuse.Singleton);
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
- **System Documentation**: `docs/modules/` (17 module docs)
  - Animation: `animation-event-system.md`, `animation-system-usage-guide.md`
  - Audio: `audio/quick-start.md`, `audio/README.md`
  - TileMap: `tilemap-quick-start.md`, `tilemap-usage-guide.md`, `tilemap-tileset-configs.md`
  - Core Systems: `ecs-gameobject.md`, `rendering-pipeline.md`, `scene-management.md`
- **Specifications**: `docs/specifications/` (design documents for major features)
- **Pareto Analysis**: `docs/pareto-analysis-missing-features.md` (known gaps and priorities)

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

## Recent Improvements & Focus Areas

The project has recently undergone significant architectural improvements and optimizations:

### Performance Optimizations (2024-2025)
- **Static Reflection Caching**: ScriptableEntity now uses static caching for reflection operations
- **Shader Caching**: ShaderFactory caches compiled shaders for reuse
- **Texture Dictionary Cache**: Graphics2D uses O(1) dictionary lookup instead of linear search
- **Optimized Matrix Calculation**: OrthographicCamera matrix calculation improved
- **Removed Lazy Initialization**: Mesh class simplified for better performance and safety

### Resource Management
- **IDisposable Patterns**: Proper disposal implemented throughout (Model, Mesh, buffers)
- **Resource Cleanup**: Physics body cleanup moved to PhysicsSimulationSystem
- **Script Lifecycle**: Consolidated into ScriptEngine for better management

### Architectural Refactoring
- **ECS System Architecture**: Migrated to proper ISystem pattern with priority-based execution
- **Dependency Injection**: Full DryIoc integration eliminates static singletons
- **Error Handling**: Unified GL error checking and validation across rendering system
- **Factory Pattern**: Consistent factory-based resource creation
- **Editor UI Refactoring**: Feature-based organization with reusable UI infrastructure
  - ComponentEditors moved to top-level directory with Core infrastructure
  - Features directory for cohesive feature modules (Project, Scene, Settings)
  - UI directory with Drawers, Elements, and FieldEditors for code reuse
  - Consistent styling with EditorUIConstants throughout all panels

### New Features
- **Complete Animation System**: 2D sprite animation with events and timeline editor
- **TileMap System**: Multi-layer tilemap support with visual editor
- **Audio Format Support**: Added Ogg Vorbis support via NVorbis
- **Editor Enhancements**: Shortcuts manager, performance monitor, animation timeline

### Known Gaps (From Pareto Analysis)
- ImGuizmo transform gizmos have stability issues
- Build/export system incomplete
- Undo/redo system missing
- Prefab system partial implementation
- Material system not yet implemented

---

## Contributing Philosophy

### Core Values

1. **Performance Matters**: This is a game engine - frame time is critical (recent focus area)
2. **Developer Experience**: APIs should be intuitive and well-documented
3. **Cross-Platform**: Write once, run anywhere
4. **Maintainability**: Code should be readable and well-structured
5. **Pragmatism**: Ship working features over perfect abstractions
6. **Clean Architecture**: Dependency injection and interface-driven design throughout

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
