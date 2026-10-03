# Game Engine

![3d.png](docs/images/3d.png)

A component-based game engine built with C# and .NET 10, featuring a visual editor, hot-reloadable C# scripting, 2D games, and basic 3D (static meshes).

## Features

### Core Engine
- **Entity Component System (ECS)** — data-driven architecture with ordered system execution
- **Entity Hierarchy** — parent/child transforms, cascade destroy, show/hide (`Visible` / `EffectiveVisible`), prefab subtrees, serialized relationships
- **2D Rendering** — OpenGL pipeline with batched sprites
- **3D Rendering** — static `.glb` / `.gltf` / `.fbx` meshes (PBR textures, mesh instancing), unit cubes, perspective camera, ambient + directional + point lights (Cook-Torrance), directional and point shadows, frustum culling, visibility zones. No skinning or animation yet.
- **Physics** — 2D rigid-body simulation with box/circle/edge colliders, raycast & overlap queries, and debug visualization
- **Hot-Reloadable Scripting** — C# `IGameSystem` / `IGameComponent` under `assets/scripts/`, compiled to a GameAssembly and loaded via `ScriptEngine` without restarting the editor. Systems poll `IKeyboardInput` / `IMouseInput` ([docs](docs/guide/scripting/input.md))
- **Audio** — OpenAL spatial audio (WAV/Ogg), per-entity sources with optional EFX (reverb, echo, low-pass)
- **Cross-Platform** — Windows and macOS

## Editor

The visual editor targets 2D and 3D projects with a docked ImGui layout. See the [scene editor guide](docs/guide/editor/scene-editor.md) for day-to-day workflow.

### Scene & hierarchy

- Parent/child **entity tree** with search (always visible at the top of the panel), **component-type filter**, expand/collapse, and drag-and-drop reparenting
- **Multi-select** (Shift/Ctrl), **multi-entity move** in the hierarchy, and **multi-entity property editing** (shared components only)
- Create entities from a **+** menu; **duplicate** (`Ctrl+D`); **delete** (`Del`); context menus on entities and empty space
- **Scroll to selected** when picking in the viewport (optional) or when selection changes
- Per-entity **Visible** flag (cascading show/hide in the viewport and at runtime)

### Viewport & tools

- **2D/3D edit camera** — fly, orbit, pan, zoom; frame selection; reset camera (`Ctrl+R`)
- **Tools** — Select, Move, Scale, Rotate (ImGuizmo), Ruler (`Shift+Q/W/R/E`); left-click pick in 3D
- **Selection outline** on selected entities (edit mode only; supports multiple selection)
- **2D grid** and rulers; **3D grid**; grid can be fully disabled from the toolbar or **View** menu
- **Visibility zone** wireframe debug for assigned volumes
- Drag-and-drop **textures**, **audio**, **prefabs**, and **3D models** into the scene

### Panels & layout

- **Godot-style bottom panel** — thin tab bar under the viewport (Console, Content Browser); click to expand or collapse
- **Properties** — component inspectors, **field search** across the current selection, asset pickers on paths
- **Console** — script `Console.WriteLine` and engine logs, level filters, search, auto-scroll
- **Stats** — 2D or 3D render metrics depending on project type (**View → Show Stats**)
- **Command palette** — run menu actions and jump to entities (`Ctrl+Shift+P`)

### Project, play, and publish

- **New / open / close** project and scene; recent projects list
- **Project → Settings** — default scene, game title, window size, fullscreen, target FPS
- **Scene → Settings** — per-scene options (e.g. ambient, physics)
- **Play / Stop / Restart** — hot-reload scripts before play; stop reloads the last saved scene from disk
- **Publish** — standalone executable for the host RID (Windows/macOS x64 or ARM64) with validation
- **Undo/redo** — transforms, components, deletes, and model-import hierarchy (`Ctrl+Z` / `Ctrl+Y`)

### Assets & shortcuts

- **Content Browser** — folder tree and asset grid; drag to viewport or inspector; Windows **Show in Explorer** / **Edit** on right-click
- **Configurable keyboard shortcuts** and in-editor reference ([docs](docs/guide/editor/shortcuts.md))

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
├── Engine/          # Core runtime (2D/3D rendering, physics, audio, scripting, scenes)
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

## Documentation

- [Changelog](CHANGELOG.md) — release notes (latest: **0.10.0**)
- [Developer Guide](docs/guide/index.md) — setup, editor, scripting, concepts
- [Cameras and Rendering](docs/guide/concepts/cameras-and-rendering.md) — 2D sprites, 3D models, lights, cameras
- [Architecture](docs/architecture/README.md) — how the engine is structured
- [Scene Rendering Pipeline](docs/architecture/scene-rendering-pipeline.md) — pass ordering, 2D batching, 3D meshes
- [Lighting](docs/architecture/lighting.md) — forward lighting and light resolution
- [Shadows](docs/architecture/shadows.md) — directional and point shadow passes

## Dependencies

Silk.NET (OpenGL, Assimp), ImGui, Box2D, OpenAL, DryIoc, Serilog
