# Game Engine

A modern, component-based game engine built with C# and .NET 10, featuring a visual editor, hot-reloadable C# scripting, and 2D/3D game development support.

<img width="2536" height="1322" alt="image" src="https://github.com/user-attachments/assets/6587c9de-8d17-4457-baf3-e65ae7b97dbc" />

## Features

### Core Engine
- **Entity Component System (ECS)** — data-driven architecture with ordered system execution
- **2D & 3D Rendering** — OpenGL pipeline with batched sprites, model import, and PBR materials
- **Physics** — 2D rigid-body simulation with collision detection, world queries (raycast/overlap), and debug visualization
- **Hot-Reloadable Scripting** — write game logic in C# and reload without restarting the editor
- **Audio** — OpenAL spatial audio (WAV/Ogg), per-entity sources with optional EFX (reverb, echo, low-pass)
- **Cross-Platform** — Windows and macOS

### Editor
- **Visual Scene Editor** — compose scenes with a hierarchy, viewport, and properties panel
- **Asset Browser** — browse and manage project assets
- **Live Console** — real-time logging while you work
- **Project Management** — create and open game projects
- **Game Publishing** — build standalone executables for Windows and macOS
- **Keyboard Shortcuts** — configurable shortcuts with an in-editor reference ([docs](docs/guide/editor/shortcuts.md))

## Getting Started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- OpenGL 3.3+ compatible graphics card

### Build & Run

```bash
git clone https://github.com/kateusz/GameEngine.git
cd GameEngine
dotnet build
cd Editor && dotnet run
```

### Quick Start

1. Launch the editor and create a new project
2. Create a scene (`Ctrl+N`), add entities, and attach components
3. Add scripts from the entity context menu — edits hot-reload in the editor

For a fuller walkthrough, see the [Developer Guide](docs/guide/index.md).

## Project Layout

```
├── Engine/          # Core runtime (rendering, physics, audio, scripting, scenes)
├── ECS/             # Entity Component System framework
├── Editor/          # Visual editor
├── Runtime/         # Standalone game player
├── games/           # Sample games
├── tests/           # Automated tests
└── docs/            # Guides and architecture docs
```

## Demo Games

Open a demo in the editor via **Open Project** and select the game's `project/` folder.

### Flappy Bird
Side-scroller — physics, scrolling pipes, scoring. [`games/FlappyBird/project/`](games/FlappyBird/project/)

![Flappy Bird](docs/images/demo-games/flappybird.png)

### Snake
Grid arcade — movement, tick loop, sprites, audio. [`games/Snake/project/`](games/Snake/project/)

![Snake](docs/images/demo-games/snake.png)

## Documentation

- [Developer Guide](docs/guide/index.md) — setup, editor, scripting, concepts
- [Architecture](docs/architecture/README.md) — how the engine is structured
- [Roadmap](docs/guide/roadmap.md) — planned work

## Testing

```bash
dotnet test
```

Graphics integration tests skip automatically when OpenGL is unavailable. To run unit tests only:

```bash
dotnet test --filter "Category!=GraphicsIntegration"
```

## Dependencies

Silk.NET, ImGui, Box2D, OpenAL, Roslyn, DryIoc, Serilog
