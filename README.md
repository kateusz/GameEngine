# Game Engine

A modern, component-based game engine built with C# and .NET 9, featuring a comprehensive editor, hot-reloadable scripting system, and support for both 2D and 3D game development.

## ✨ Features

### 🎮 Core Engine
- **Entity Component System (ECS)** - Flexible, data-driven architecture
- **2D & 3D Rendering** - OpenGL-based rendering pipeline via Silk.NET
- **Physics Integration** - Box2D physics simulation
- **Hot-Reloadable Scripting** - C# scripting with real-time compilation
- **Cross-Platform** - Windows, macOS, and Linux support

### 🛠️ Editor
- **Visual Scene Editor** - Drag-and-drop scene composition
- **Asset Browser** - Integrated asset management
- **Live Console** - Real-time logging and debugging
- **Component Inspector** - Visual component editing
- **Project Management** - Create and manage game projects

### 🎨 Rendering
- **2D Sprite Rendering** - Batched quad rendering with texture atlasing
- **3D Model Support** - OBJ model loading via Assimp
- **Shader System** - OpenGL shader management
- **Camera System** - Orthographic and perspective cameras
- **Framebuffer Support** - Render-to-texture capabilities

### 🔧 Scripting System
- **Hot Reload** - Modify scripts without restarting
- **Visual Debugging** - Debug symbols and breakpoint support
- **Component Integration** - Direct access to ECS components
- **Event System** - Event-driven architecture for input and game events

### 🎧 Audio System
- **OpenAL Integration** - 3D positional audio support
- **Sound Management** - Load and play audio files
- **Multiple Sources** - Support for simultaneous audio playback

### 📊 Performance & Benchmarking
- **Automated Benchmarking** - Headless performance testing suite
- **Regression Detection** - Automatic performance comparison
- **CI/CD Integration** - GitHub Actions workflow for continuous benchmarking
- **Detailed Metrics** - FPS, frame time, percentiles, and custom metrics

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
├── Engine/           # Core engine systems
│   ├── Audio/        # Audio system (OpenAL integration)
│   ├── Core/         # Application framework & layer system
│   ├── Events/       # Event system (keyboard, mouse, window)
│   ├── ImGuiNet/     # ImGui integration
│   ├── Math/         # Mathematics utilities
│   ├── Platform/     # Platform-specific code
│   ├── Renderer/     # Rendering pipeline (2D/3D)
│   ├── Scene/        # Scene management & ECS
│   ├── Scripting/    # Script engine (Roslyn-based)
│   └── UI/           # UI system
├── Editor/           # Visual editor application
│   ├── Managers/     # Editor managers
│   ├── Panels/       # Editor UI panels
│   ├── Popups/       # Editor dialogs
│   ├── Publisher/    # Game publishing tools
│   └── Resources/    # Editor resources
├── ECS/              # Entity Component System
├── Benchmark/        # Performance benchmarking tools
├── Runtime/          # Runtime environment
└── Sandbox/          # Testing & experimentation
```

### Key Systems

- **Renderer2D/3D** - Batched rendering with automatic state management ([docs](docs/opengl-rendering/opengl-2d-workflow.md))
- **ScriptEngine** - Roslyn-based C# compilation with debugging support
- **Scene System** - Hierarchical entity management with serialization ([docs](docs/modules/scene-management.md))
- **Event System** - Event-driven input handling with layer-based propagation ([docs](docs/modules/input-system-architecture.md))
- **Camera System** - Flexible camera system with orthographic and perspective projections ([docs](docs/modules/camera-system.md))
- **Audio System** - OpenAL-based audio playback
- **Asset Pipeline** - Texture and model loading with resource management ([docs](docs/modules/resource-management.md))

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

### 3D Model Rendering

```csharp
// Add mesh component
var meshComponent = new MeshComponent();
var mesh = MeshFactory.Create("assets/models/character.obj");
meshComponent.SetMesh(mesh);
entity.AddComponent(meshComponent);

// Add model renderer
var renderer = new ModelRendererComponent
{
    Color = new Vector4(1, 1, 1, 1),
    CastShadows = true
};
entity.AddComponent(renderer);
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

## 📊 Performance Benchmarking

The engine includes a comprehensive automated benchmarking system for tracking and comparing performance:

```bash
# Run benchmarks locally
./run-benchmark.sh --tests all

# Compare with baseline
./run-benchmark.sh --compare-with-baseline

# Save new baseline
./run-benchmark.sh --save-baseline
```

**Features:**
- **Headless Mode** - Run benchmarks without GUI for CI/CD
- **Regression Detection** - Automatic performance comparison with configurable thresholds
- **Multiple Test Scenarios** - Renderer stress tests, texture switching, draw call optimization
- **GitHub Actions Integration** - Automatic benchmarking on commits and PRs
- **Statistical Analysis** - FPS, frame time, percentiles, and custom metrics

See [BENCHMARK_SETUP.md](BENCHMARK_SETUP.md) for complete setup guide and [Benchmark/README.md](Benchmark/README.md) for detailed documentation.

## 📦 Dependencies

### Core Dependencies
- **Silk.NET** - OpenGL bindings and windowing
- **ImGui.NET** - Editor user interface
- **Box2D.NetStandard** - 2D physics simulation
- **StbImageSharp** - Image loading
- **Silk.NET.Assimp** - 3D model loading
- **NLog** - Logging framework

### Development Dependencies
- **.NET 9 SDK** - Runtime and development tools
- **System.Numerics** - Vector and matrix mathematics

## 📚 Documentation

### Module Documentation
Comprehensive documentation for each major system in the engine:

- [Input System Architecture](docs/modules/input-system-architecture.md) - Event-driven input handling with layer-based propagation
- [Scene Management](docs/modules/scene-management.md) - Hierarchical entity management with serialization
- [Camera System](docs/modules/camera-system.md) - Flexible camera system with orthographic and perspective projections
- [Frame Buffers](docs/modules/frame-buffers.md) - Render-to-texture capabilities
- [Rendering Pipeline](docs/modules/rendering-pipeline.md) - OpenGL rendering pipeline overview
- [Resource Management](docs/modules/resource-management.md) - Asset loading and management
- [ECS & GameObject Architecture](docs/modules/ecs-gameobject.md) - Entity Component System design
- [Editor Tools](docs/modules/editor.md) - Visual editor features and workflow
- [Game Loop](docs/modules/game-loop.md) - Core engine execution cycle
- [Performance Benchmarking](Benchmark/README.md) - Automated benchmarking and regression detection

### OpenGL Rendering Workflows
Detailed guides on the OpenGL rendering implementation:

- [OpenGL 2D Rendering Workflow](docs/opengl-rendering/opengl-2d-workflow.md) - Batched 2D rendering with multi-texture support
- [OpenGL 3D Rendering Workflow](docs/opengl-rendering/opengl-3d-workflow.md) - 3D model rendering and pipeline

---
