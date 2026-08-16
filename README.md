# Game Engine

A modern, component-based game engine built with C# and .NET 10, featuring a visual editor, hot-reloadable C# scripting, and 2D/3D game development support.

<img width="2536" height="1322" alt="image" src="https://github.com/user-attachments/assets/6587c9de-8d17-4457-baf3-e65ae7b97dbc" />

## Features

### Core Engine
- **Entity Component System (ECS)** — data-driven architecture with ordered system execution
- **Entity Hierarchy** — parent/child transforms, cascade destroy, prefab subtrees, serialized relationships
- **2D & 3D Rendering** — OpenGL pipeline with batched sprites, PBR meshes, shadows, skeletal animation, IBL, and HDR post-processing
- **3D Model Import** — Assimp FBX/OBJ import in the editor, runtime `.mesh` assets with materials and skinning
- **Physics** — 2D rigid-body simulation with box/circle/edge colliders, raycast & overlap queries, and debug visualization
- **Hot-Reloadable Scripting** — write game logic in C# and reload without restarting the editor
- **Audio** — OpenAL spatial audio (WAV/Ogg), per-entity sources with optional EFX (reverb, echo, low-pass)
- **Cross-Platform** — Windows and macOS

### Editor
- **Visual Scene Editor** — hierarchy tree, viewport tools (select/move/scale/rotate/ruler), and properties panel
- **Undo/Redo** — reversible transform, component, and entity-delete operations (Ctrl+Z / Ctrl+Y)
- **Asset Browser** — browse and manage project assets; drag-drop prefabs and 3D models into scenes
- **Live Console** — real-time logging while you work
- **Project Management** — create and open game projects
- **Game Publishing** — build standalone executables for Windows and macOS, with publish validation
- **Keyboard Shortcuts** — configurable shortcuts with an in-editor reference ([docs](docs/guide/editor/shortcuts.md))

### Status

~**82–86% ready for 2D public alpha** ([readiness analysis](docs/readiness-analysis-2026-08.md)). Core mechanics, hierarchy, physics queries, undo/redo, and publish pipeline are in place. Main remaining gaps: **runtime UI** (menus/HUD), **sprite sort layers**, and **script field serialization**.

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

Open a demo in the editor via **Open Project** and select the game's folder under `games/`.

### Flappy Bird
Side-scroller — physics, scrolling pipes, scoring. [`games/FlappyBird/`](games/FlappyBird/)

![Flappy Bird](docs/images/demo-games/flappybird.png)

### Snake
Grid arcade — movement, tick loop, sprites, audio. [`games/Snake/`](games/Snake/)

![Snake](docs/images/demo-games/snake.png)

### Arena Shooter
Twin-stick arena — WASD move, mouse aim, hold LMB to shoot (hitscan raycast), chasing enemies, health and score HUD. [`games/ArenaShooter/`](games/ArenaShooter/)

![Arena Shooter](docs/images/demo-games/arenashooter.png)

Open `assets/scenes/arena.scene`, then press Play. **R** restarts after game over.

### Arena 3D
3D showcase — imported skeletal mesh, PBR materials, IBL skybox, shadows, HDR post-processing. [`games/Arena3D/`](games/Arena3D/)

Open `assets/scenes/arena3d.scene`, then press Play. WASD to move, mouse to look.

## Documentation

- [Developer Guide](docs/guide/index.md) — setup, editor, scripting, concepts
- [Architecture](docs/architecture/README.md) — how the engine is structured
- [Readiness Analysis (2026-08)](docs/readiness-analysis-2026-08.md) — alpha readiness assessment and priorities
- [Roadmap](docs/guide/roadmap.md) — planned work

## Dependencies

Silk.NET, ImGui, Box2D, BepuPhysics, OpenAL, DryIoc, Serilog, Assimp
