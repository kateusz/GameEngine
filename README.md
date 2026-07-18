# Game Engine

A modern, component-based game engine built with C# and .NET 10, featuring a comprehensive editor, hot-reloadable scripting system, and 2D game development support.

<img width="2536" height="1322" alt="image" src="https://github.com/user-attachments/assets/6587c9de-8d17-4457-baf3-e65ae7b97dbc" />


## ✨ Features

### 🎮 Core Engine
- **Entity Component System (ECS)** - Flexible, data-driven architecture with priority-based system execution
- **2D Rendering** - OpenGL-based rendering pipeline via Silk.NET with batching (10,000 quads/batch)
- **Physics Integration** - Platform-abstracted 2D physics (Box2D backend) with contact queue and debug visualization
- **Hot-Reloadable Scripting** - C# scripting with real-time compilation via Roslyn
- **Dependency Injection** - DryIoc IoC with staged engine registration (`RegisterCore` / `RegisterWindowing`) and editor overlay ([docs](docs/architecture/dependency-injection.md))
- **Cross-Platform** - Windows, macOS support

### 🛠️ Editor
- **Visual Scene Editor** - Drag-and-drop scene composition with flat entity list
- **Asset Browser** - Integrated asset management
- **Live Console** - Real-time logging and debugging with Serilog integration
- **Component Inspector** - Visual component editing with 17 editor panels
- **Project Management** - Create and manage game projects
- **Game Publishing** - Build standalone executables via **Publish → Build & Publish** (`IGamePublisher` / `Editor/Publisher/`); Debug builds also copy source `.cs` files to `assets/scripts/`
- **Keyboard Shortcuts** - Centralized shortcut registry with in-editor reference panel ([docs](docs/guide/editor/shortcuts.md))

### 🎨 Rendering
- **2D Sprite Rendering** - Batched quad rendering with texture atlasing (10,000 quads per batch)
- **3D Model Rendering** - Assimp FBX/glTF/GLB import with PBR metal/rough materials; unit-cube fallback when no model path is set
- **Shader System** - OpenGL shader management with caching
- **Camera System** - Orthographic and perspective cameras with optimized matrix calculation
- **Framebuffer Support** - Multi-attachment render-to-texture (RGBA16F color, entity ID, depth) for editor viewports
- **GPU Entity Picking** - Efficient entity selection in editor

### 🔧 Scripting System
- **Hot Reload** - Editor recompiles `assets/scripts/` to versioned `GameAssembly_{guid}.dll` and reloads via collectible `AssemblyLoadContext`
- **Roslyn Compilation** - `GameAssemblyCompiler` emits game DLLs with optional portable PDBs for debugging
- **Three Tiers** - `IGameComponent` (data), `ScriptableEntity` (per-entity glue), `IGameSystem` (batch logic via `[Register]`)
- **Event Dispatch** - `IScriptEngine.ProcessEvent` forwards input events to `ScriptableEntity` instances via `NativeScriptIteration`

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
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
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
│   │   ├── Systems/    # ECS system implementations
│   │   └── Serializer/ # JSON scene/prefab serialization (ComponentSerializerRegistry)
│   ├── Scripting/      # Roslyn-based script engine
│   └── UI/             # UI system integration
├── ECS/                # Pure ECS framework (Entity, Context, ISystem, SystemManager)
│   └── Systems/        # ISystem, IGameSystem, SystemManager
├── SceneComponents/    # Built-in ECS component definitions (14 components)
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
├── games/              # Demo games (Snake, TicTacToe, FlappyBird)
└── docs/               # Technical documentation
    ├── modules/        # 17 module documentation files
    ├── opengl-rendering/ # OpenGL workflow guides
    └── specifications/ # Feature specifications and designs
```

### Key Systems

- **Graphics2D** (`IGraphics2D`) - Batched 2D rendering with automatic state management and shader/texture caching ([docs](docs/architecture/rendering-pipeline.md))
- **Graphics3D** (`IGraphics3D`) - Per-entity 3D draws: PBR meshes via Assimp/`ModelFactory`, cube fallback, ambient/directional ECS lights ([docs](docs/opengl/opengl-3d-workflow.md))
- **ScriptEngine** - `GameAssemblyCompiler` (Roslyn emit) + `IScriptEngine` (collectible load, type index, instance factory) ([docs](docs/architecture/scripting-lifecycle.md))
- **Scene System** - Hierarchical entity management with JSON serialization ([docs](docs/modules/scene-management.md))
- **ECS Systems** - Priority-based system execution with dependency injection ([docs](docs/architecture/ecs-architecture.md))
- **Event System** - Event-driven input handling with layer-based propagation ([docs](docs/guide/scripting/input.md))
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

## 🎯 Demo Games

Sample projects under [`games/`](games/) show how to build small 2D games with components, systems, and publishable scenes. Open a demo in the editor via **Open Project** and select the game’s `project/` folder.

| Game | Project | Startup scene | What it shows |
|------|---------|---------------|---------------|
| **Snake** | [`games/Snake/`](games/Snake/) | [`snake.scene`](games/Snake/project/assets/scenes/snake.scene) | Grid arcade: `IGameSystem` + keyboard input, tick loop, sprite sync, one-shot audio |
| **Tic Tac Toe** | [`games/TicTacToe/`](games/TicTacToe/) | [`main.scene`](games/TicTacToe/project/assets/scenes/main.scene) | Turn-based board: script mailboxes + rules system, win/draw banners |
| **Flappy Bird** | [`games/FlappyBird/`](games/FlappyBird/) | [`flappybird.scene`](games/FlappyBird/project/assets/scenes/flappybird.scene) | Side-scroller: flap physics, scrolling pipes/ground, score digits, audio cues |

**Snake highlights:** [`SnakeGameComponent`](games/Snake/project/assets/scripts/SnakeGameComponent.cs) (state), [`SnakeSystem`](games/Snake/project/assets/scripts/SnakeSystem.cs) (input, step, visuals, audio), [`game.config.json`](games/Snake/project/game.config.json).

**Tic Tac Toe highlights:** [`BoardComponent`](games/TicTacToe/project/assets/scripts/BoardComponent.cs) + [`GameControllerScript`](games/TicTacToe/project/assets/scripts/GameControllerScript.cs) (input → mailboxes), [`TicTacToeSystem`](games/TicTacToe/project/assets/scripts/TicTacToeSystem.cs) (rules + sprites), [`game.config.json`](games/TicTacToe/project/game.config.json).

**Flappy Bird highlights:** [`FlappyBirdGameComponent`](games/FlappyBird/project/assets/scripts/FlappyBirdGameComponent.cs) (phase, bird, pipes, tunables), [`FlappyBirdSystem`](games/FlappyBird/project/assets/scripts/FlappyBirdSystem.cs) (flap, scroll, collision, score UI, audio), [`game.config.json`](games/FlappyBird/project/game.config.json).

Scripting tier patterns used by these demos: [Scripting tiers](docs/guide/scripting/scripting-tiers.md).

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
- **.NET 10 SDK** - Runtime and development tools
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
- **Priority-Based ECS** - Systems execute in configurable priority order (PhysicsSimulationSystem: 100)
- **Dependency Injection** - DryIoc with 80+ registrations; scene systems created via `ISceneSystemsFactory`, not as DI singletons
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
- [Input Handling](docs/guide/scripting/input.md) - Keyboard, mouse, and layer-based event propagation
- [Scene Management](docs/modules/scene-management.md) - Hierarchical entity management
- [Camera System](docs/modules/camera-system.md) - Camera system with optimizations
- [ECS Architecture](docs/architecture/ecs-architecture.md) - Entity Component System design
- [Game Loop](docs/architecture/game-loop.md) - Application lifecycle, layer stack, and frame tick

**Rendering:**
- [Rendering Pipeline](docs/architecture/rendering-pipeline.md) - OpenGL rendering pipeline overview
- [OpenGL 2D Workflow](docs/opengl/opengl-2d-workflow.md) - Batched 2D rendering with multi-texture support
- [OpenGL 3D Workflow](docs/opengl/opengl-3d-workflow.md) - Model import, PBR shading, and ECS lights
- [Frame Buffers](docs/opengl/frame-buffers.md) - Render-to-texture capabilities

**Tools & Publishing:**
- [Editor Tools](docs/modules/editor.md) - Visual editor features and workflow
- [Resource Management](docs/modules/resource-management.md) - Asset loading and management
- **Game Publishing** — editor menu **Publish → Build & Publish** (`PublishSettingsUI`). Requires a loaded project and a saved scene. Builds `Runtime/Runtime.csproj` with `dotnet publish`, copies project `assets/`, compiles scripts to `GameAssembly.dll`, and writes `game.config.json`. Default output: `{project}/Builds/`. Targets: `win-x64`, `win-x86`, `win-arm64`, `osx-x64`, `osx-arm64`. Options: Release/Debug, self-contained .NET runtime, single-file executable.
  - **Debug script copy** — when Configuration is `Debug`, `CopyScripts` copies project scripts into `assets/scripts/` in the build output; Release asset copy skips `assets/scripts/` from the project tree (`includeScripts: false` in `CopyAssets`).
  - **Post-build validation** — `ValidatePublishedBuild` requires the platform executable (`Runtime.exe` / `Runtime`), `game.config.json`, `GameAssembly.dll`, and the startup scene. Logs a warning (non-fatal) if the executable is under 100 KB.
  - Source: `Editor/Publisher/`, `Runtime/Program.cs`

### OpenGL Rendering Workflows
Detailed guides on the OpenGL rendering implementation:

- [OpenGL 2D Rendering Workflow](docs/opengl/opengl-2d-workflow.md) - Batched 2D rendering with multi-texture support
- [OpenGL 3D Rendering Workflow](docs/opengl/opengl-3d-workflow.md) - Assimp models, PBR materials, ambient and directional lights

### Technical Specifications
Design documents for major features:

- [ECS Architecture](docs/architecture/ecs-architecture.md) - Priority-based ECS design
- [Entity Search Filter](docs/specifications/entity-search-filter.md) - Scene hierarchy filtering
- [Ogg Audio Format Support](docs/specifications/ogg-audio-format-support.md) - Ogg Vorbis integration
- [Physics Benchmark Design](docs/specifications/physics-benchmark-design.md) - Performance testing
- [Readiness analysis](docs/readiness-analysis-2026-07.md) - Known gaps and alpha priorities

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

Graphics integration tests (`Engine.GraphicsTests`) require a working OpenGL stack. They skip automatically on machines without one. To run unit tests only:

```bash
dotnet test --filter "Category!=GraphicsIntegration"
```

Image regression baselines are raw RGBA blobs (`tests/Engine.GraphicsTests/Golden/*.rgba`, 64×64×4 bytes). After an intentional visual change, regenerate with:

```bash
UPDATE_GOLDENS=1 dotnet test tests/Engine.GraphicsTests --filter "Regression"
```

See `docs/specs/graphics-image-regression-tests/` for scene design, tolerances, and CI artifact details.

## 🏗️ Architectural Highlights

### Entity Component System (14 Built-in Components)
- **Core**: IdComponent, TagComponent, TransformComponent
- **Rendering**: SpriteRendererComponent, SubTextureRendererComponent, ModelRendererComponent, CameraComponent
- **Lighting**: AmbientLightComponent, DirectionalLightComponent
- **Physics**: RigidBody2DComponent, BoxCollider2DComponent
- **Scripting**: NativeScriptComponent
- **Audio**: AudioSourceComponent, AudioListenerComponent

### Design Patterns
- **Dependency Injection** - `EngineIoCContainer` + `EditorIoCContainer`; game assemblies extend the container via `[Register]` ([docs](docs/architecture/dependency-injection.md))
- **Factory Pattern** - Resource creation via factories (TextureFactory, ShaderFactory, AudioClipFactory, etc.)
- **Interface-Driven Design** - IGraphics2D, IRendererAPI, ISystem, etc.
- **Constants Classes** - EditorUIConstants and RenderingConstants prevent magic numbers
- **Event-Driven Architecture** - Layer-based event propagation

---
