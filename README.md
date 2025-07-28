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
- **Event System** - Input and collision event handling

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
│   ├── Core/         # Application framework
│   ├── Renderer/     # Rendering pipeline
│   ├── Scene/        # Scene management & ECS
│   ├── Scripting/    # Script engine
│   └── Platform/     # Platform-specific code
├── Editor/           # Visual editor application
├── ECS/              # Entity Component System
```

### Key Systems

- **Renderer2D/3D** - Batched rendering with automatic state management
- **ScriptEngine** - Roslyn-based C# compilation with debugging support
- **Scene System** - Hierarchical entity management with serialization
- **Input System** - Cross-platform input handling
- **Asset Pipeline** - Texture and model loading

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
    
    public override void OnUpdate(TimeSpan ts)
    {
        var transform = GetComponent<TransformComponent>();
        float deltaTime = (float)ts.TotalSeconds;
        
        if (InputState.Instance.Keyboard.IsKeyPressed(KeyCodes.A))
            transform.Translation.X -= speed * deltaTime;
        if (InputState.Instance.Keyboard.IsKeyPressed(KeyCodes.D))
            transform.Translation.X += speed * deltaTime;
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

---
