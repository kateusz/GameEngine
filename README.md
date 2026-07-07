# Game Engine

A modern, component-based game engine built with C# and .NET 9, featuring a comprehensive editor, hot-reloadable scripting system, and 2D game development support.

## ✨ Features

### 🎮 Core Engine
- **Entity Component System (ECS)** - Flexible, data-driven architecture with priority-based system execution
- **2D Rendering** - OpenGL-based rendering pipeline via Silk.NET with batching (10,000 quads/batch)
- **Physics Integration** - Box2D physics simulation with debug visualization
- **Hot-Reloadable Scripting** - C# scripting with real-time compilation via Roslyn
- **Dependency Injection** - DryIoc IoC container for clean architecture
- **Cross-Platform** - Windows, macOS support

### 🛠️ Editor
- **Visual Scene Editor** - Drag-and-drop scene composition with hierarchical entities
- **Asset Browser** - Integrated asset management
- **Live Console** - Real-time logging and debugging with Serilog integration
- **Component Inspector** - Visual component editing with 17 editor panels
- **Project Management** - Create and manage game projects
- **Keyboard Shortcuts** - Configurable shortcuts for improved workflow

### 🎨 Rendering
- **2D Sprite Rendering** - Batched quad rendering with texture atlasing (10,000 quads per batch)
- **Shader System** - OpenGL shader management with caching
- **Camera System** - Orthographic cameras with optimized matrix calculation
- **Framebuffer Support** - Render-to-texture capabilities for editor viewports
- **GPU Entity Picking** - Efficient entity selection in editor

### 🔧 Scripting System
- **Hot Reload** - Modify scripts without restarting
- **Visual Debugging** - Debug symbols and breakpoint support
- **Component Integration** - Direct access to ECS components
- **Event System** - Event-driven architecture for input and game events

### 🎧 Audio System
- **OpenAL Integration** - 3D spatial audio with listener orientation
- **Multiple Formats** - WAV and Ogg Vorbis via `AudioLoaderRegistry` (NVorbis)
- **Clip Caching** - Weak-reference cache in `OpenALAudioEngine`
- **Audio Effects** - Reverb, echo, and low-pass via OpenAL EFX (with no-op fallback)
- **ECS Integration** - `AudioSourceComponent` and `AudioListenerComponent`; runtime sources managed by `AudioSystem`

## 📸 Screenshots

*Screenshots will be added here*

## 🚀 Getting Started

### Prerequisites
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- OpenGL 3.3+ compatible graphics card

### Building from Source

1. **Clone the repository**
   ```bash
   git clone https://github.com/yourusername/game-engine.git
   cd game-engine
   ```

2. **Restore dependencies**
   ```bash
   dotnet restore
   ```

3. **Build the solution**
   ```bash
   dotnet build
   ```

4. **Run the editor**
   ```bash
   cd Editor
   dotnet run
   ```

### Quick Start

1. **Create a New Project**
    - Launch the editor
    - Click "New Project" and enter a project name
    - The editor will create the project structure automatically

2. **Create Your First Scene**
    - Use `Ctrl+N` to create a new scene
    - Add entities using the Scene Hierarchy panel
    - Attach components via the Properties panel

3. **Add Scripts**
    - Right-click an entity and select "Add Component" → "Script"
    - Choose "Create New Script" to generate a template
    - Edit scripts in the built-in editor with hot reload support

## 🏗️ Architecture

### Project Structure
```
├── Engine/              # Core engine runtime
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
├── ECS/                # Pure ECS implementation
│   └── System/         # ISystem interface, SystemManager
├── Editor/             # Visual editor application
│   ├── Input/          # Editor input handling
│   ├── Logging/        # Console panel integration
│   ├── Managers/       # ProjectManager, SceneManager (DI-based)
│   ├── Panels/         # UI panels (17 panels total)
│   ├── Popups/         # Dialogs and modal windows
│   ├── Publisher/      # Build and publishing tools
│   ├── Systems/        # EditorCameraSystem
│   ├── UI/             # EditorUIConstants for styling
│   ├── Utilities/      # Helper classes
│   ├── Windows/        # RecentProjectsWindow
│   └── Resources/      # Editor-specific assets
├── Runtime/            # Standalone game runtime
├── Sandbox/            # Testing and experimentation
├── Benchmark/          # Performance benchmarking tools with BenchmarkDotNet
├── tests/              # Unit test projects
│   ├── ECS.Tests/      # ECS unit tests
│   └── Engine.Tests/   # Engine unit tests (30+ test files)
├── games scripts/      # Sample game projects
└── docs/               # Technical documentation
    ├── modules/        # 17 module documentation files
    ├── opengl-rendering/ # OpenGL workflow guides
    └── specifications/ # Feature specifications and designs
```

### Key Systems

- **Renderer2D** - Batched rendering with automatic state management and shader/texture caching ([docs](docs/architecture/rendering-pipeline.md))
- **ScriptEngine** - Roslyn-based C# compilation with hot-reload and debugging support
- **Scene System** - Hierarchical entity management with JSON serialization ([docs](docs/modules/scene-management.md))
- **ECS Systems** - Priority-based system execution with dependency injection ([docs](docs/specifications/ecs-systems-integration.md))
- **Event System** - Event-driven input handling with layer-based propagation ([docs](docs/modules/input-system-architecture.md))
- **Camera System** - Orthographic camera with optimized matrix calculation ([docs](docs/architecture/rendering-pipeline.md#camera-system))
- **Audio System** - OpenAL-based spatial audio with WAV/Ogg loading and EFX ([docs](docs/architecture/audio-system.md))
- **Asset Pipeline** - Factory-based texture loading with resource management ([docs](docs/modules/resource-management.md))

## 💻 Usage Examples

### Creating a Simple 2D Game Object

```csharp
// Create entity
var player = scene.CreateEntity("Player");

// Add transform component
var transform = new TransformComponent
{
    Translation = new Vector3(0, 0, 0),
    Scale = new Vector3(1, 1, 1)
};
player.AddComponent(transform);

// Add sprite renderer
var sprite = new SpriteRendererComponent
{
    Color = new Vector4(1, 0, 0, 1), // Red color
    Texture = TextureFactory.Create("player.png")
};
player.AddComponent(sprite);
```

### Writing a Movement Script

```csharp
public class PlayerController : ScriptableEntity
{
    public float speed = 5.0f;
    private Vector3 velocity = Vector3.Zero;

    public override void OnUpdate(TimeSpan ts)
    {
        // Apply velocity to position
        float deltaTime = (float)ts.TotalSeconds;
        var position = GetPosition();
        SetPosition(position + velocity * deltaTime);

        // Apply damping
        velocity *= 0.9f;
    }

    public override void OnKeyPressed(KeyCodes key)
    {
        // Handle input through event system
        if (key == KeyCodes.A)
            velocity.X = -speed;
        if (key == KeyCodes.D)
            velocity.X = speed;
        if (key == KeyCodes.W)
            velocity.Y = speed;
        if (key == KeyCodes.S)
            velocity.Y = -speed;
    }

    public override void OnKeyReleased(KeyCodes key)
    {
        // Stop movement when key is released
        if (key == KeyCodes.A || key == KeyCodes.D)
            velocity.X = 0;
        if (key == KeyCodes.W || key == KeyCodes.S)
            velocity.Y = 0;
    }
}
```

## 🎯 Sample Projects

TBD

## 🔧 Configuration

### Editor Settings
- Customize editor theme and layout
- Configure asset directories
- Set up input mappings

### Rendering Settings
- Adjust viewport resolution
- Configure rendering pipeline
- Shader hot-reloading options

## 📦 Dependencies

### Core Dependencies
- **Silk.NET (2.22.0)** - OpenGL bindings, windowing, and OpenAL audio
- **ImGui.NET** - Editor user interface with ImGui integration
- **Box2D.NetStandard (2.4.7-alpha)** - 2D physics simulation
- **StbImageSharp (2.30.15)** - Image loading
- **Serilog (4.2.0)** - Logging framework with multi-sink async support
- **DryIoc (5.4.3)** - Dependency injection IoC container
- **NVorbis (0.10.5)** - Ogg Vorbis audio format support
- **Microsoft.CodeAnalysis.CSharp (4.14.0)** - Roslyn compiler for scripting
- **CSharpFunctionalExtensions (3.6.0)** - Functional programming utilities
- **ZLinq (1.5.2)** - High-performance LINQ extensions

### Development Dependencies
- **.NET 9 SDK** - Runtime and development tools
- **xUnit** - Unit testing framework (ECS.Tests, Engine.Tests)
- **BenchmarkDotNet** - Performance benchmarking tools

## 🚀 Recent Improvements

The engine has undergone significant architectural improvements and optimizations:

### Performance Optimizations
- **Static Reflection Caching** - ScriptableEntity uses static caching for reflection operations
- **Shader & Texture Caching** - ShaderFactory and TextureFactory implement smart caching
- **Optimized Matrix Math** - Improved OrthographicCamera matrix calculations
- **Dictionary-based Lookups** - Graphics2D uses O(1) texture lookups instead of linear search

### Architectural Enhancements
- **Priority-Based ECS** - Systems execute in configurable priority order (ScriptUpdateSystem: 100)
- **Dependency Injection** - Full DryIoc integration with 50+ service registrations
- **Factory Pattern** - Consistent factory-based resource creation throughout
- **IDisposable Patterns** - Proper resource cleanup for all unmanaged resources
- **Unified Error Handling** - Consistent GL error checking across rendering system

### New Major Features
- **Ogg Vorbis Support** - Extended audio format support via NVorbis
- **Editor Enhancements** - Shortcuts manager, 17 specialized panels, constants-driven UI

## 📚 Documentation

### Module Documentation
Comprehensive documentation for each major system in the engine (17 modules):

**Audio:**
- [Audio System](docs/architecture/audio-system.md) - OpenAL engine, spatial audio, loaders, and effects

**Core Systems:**
- [Input System Architecture](docs/modules/input-system-architecture.md) - Event-driven input handling
- [Scene Management](docs/modules/scene-management.md) - Hierarchical entity management
- [Camera System](docs/modules/camera-system.md) - Camera system with optimizations
- [ECS & GameObject Architecture](docs/modules/ecs-gameobject.md) - Entity Component System design
- [Game Loop](docs/modules/game-loop.md) - Core engine execution cycle

**Rendering:**
- [Rendering Pipeline](docs/architecture/rendering-pipeline.md) - OpenGL rendering pipeline overview
- [OpenGL 2D Workflow](docs/opengl/opengl-2d-workflow.md) - Batched 2D rendering with multi-texture support
- [Frame Buffers](docs/opengl/frame-buffers.md) - Render-to-texture capabilities

**Tools & Publishing:**
- [Editor Tools](docs/modules/editor.md) - Visual editor features and workflow
- [Resource Management](docs/modules/resource-management.md) - Asset loading and management
- [Game Publishing Workflow](docs/modules/game-publishing-workflow.md) - Build and export tools

### OpenGL Rendering Workflows
Detailed guides on the OpenGL rendering implementation:

- [OpenGL 2D Rendering Workflow](docs/opengl/opengl-2d-workflow.md) - Batched 2D rendering with multi-texture support

### Technical Specifications
Design documents for major features:

- [ECS Systems Integration](docs/specifications/ecs-systems-integration.md) - Priority-based ECS design
- [Entity Search Filter](docs/specifications/entity-search-filter.md) - Scene hierarchy filtering
- [Ogg Audio Format Support](docs/specifications/ogg-audio-format-support.md) - Ogg Vorbis integration
- [Physics Benchmark Design](docs/specifications/physics-benchmark-design.md) - Performance testing
- [Pareto Analysis](docs/pareto-analysis-missing-features.md) - Known gaps and priorities

## 🧪 Testing

The engine includes comprehensive testing infrastructure:

- **Unit Tests** - ECS.Tests and Engine.Tests projects with 30+ test files
- **Integration Tests** - Scene serialization and rendering
- **Performance Benchmarks** - BenchmarkDotNet-based performance testing
- **Test Coverage** - Audio, Components, Serialization, and more

Run tests with:
```bash
dotnet test
```

## 🏗️ Architectural Highlights

### Entity Component System (18 Built-in Components)
- **Core**: IDComponent, TagComponent, TransformComponent
- **Rendering**: SpriteRendererComponent, SubTextureRendererComponent, CameraComponent
- **Physics**: RigidBody2DComponent, BoxCollider2DComponent
- **Scripting**: NativeScriptComponent
- **Audio**: AudioSourceComponent, AudioListenerComponent

### Design Patterns
- **Dependency Injection** - DryIoc IoC container throughout
- **Factory Pattern** - Resource creation via factories (TextureFactory, ShaderFactory, AudioClipFactory, etc.)
- **Interface-Driven Design** - IGraphics2D, IRendererAPI, ISystem, etc.
- **Constants Classes** - EditorUIConstants and RenderingConstants prevent magic numbers
- **Event-Driven Architecture** - Layer-based event propagation

---
