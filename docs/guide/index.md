# Game Engine Developer Guide

This is a C# game engine built on an Entity Component System (ECS) architecture, offering production-ready **2D** rendering via OpenGL, **3D** mesh rendering with Assimp import and PBR metal/rough shading (cube fallback when no model is set), a visual editor powered by ImGui, and hot-reloadable C# scripting so you can iterate without restarting the application. It is designed to be cross-platform (Windows, macOS) and covers core game development needs: physics simulation, spatial audio, and sprite atlasing.

## Features

- **Entity Component System** — data-oriented architecture with priority-based systems and a clean component model
- **2D rendering** — OpenGL 3.3+ batched sprite pipeline with framebuffers and a flexible camera system
- **3D rendering** — FBX/glTF/GLB via Assimp, PBR materials, ambient + directional lights; unit-cube fallback when `ModelPath` is empty
- **Physics** — rigid-body simulation and collision detection via Box2D
- **C# scripting with hot reload** — write game logic in C#; changes are compiled and reloaded at runtime without restarting the editor
- **Audio support** — spatial audio via OpenAL (WAV and Ogg Vorbis)
- **Sprite atlasing** — `SubTextureRendererComponent` for sprite sheets with manual frame selection via grid coordinates
- **Visual editor** — ImGui editor with flat entity list, properties panel, content browser, and console
- **Publishing** — build standalone Windows and macOS executables from the editor

## Prerequisites

Before building, ensure you have the following installed:

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- A GPU with OpenGL 3.3 or newer support (most discrete and integrated GPUs from the last decade qualify)

## Quick Start

Clone the repository, build the solution, then launch the editor:

```bash
git clone https://github.com/kateusz/GameEngine.git
cd GameEngine
dotnet build
cd Editor && dotnet run
```

The editor window will open. From there you can create a new project, add entities to a scene, attach components, and run the game directly inside the editor viewport.

Sample games live under `games/` (Snake, Tic Tac Toe, Flappy Bird). Open one via **Open Project** and select its `project/` folder.

## Where to Go Next

### Editor
- [Project Setup](editor/project-setup.md) — create a project and orient in the UI
- [Scene Editor](editor/scene-editor.md) — hierarchy, viewport, and tools
- [Component Inspector](editor/component-inspector.md) — editing components in Properties
- [Content Browser](editor/content-browser.md) — assets and drag-and-drop workflow
- [Shortcuts](editor/shortcuts.md) — keyboard shortcuts reference

### Scripting
- [Getting Started](scripting/getting-started.md) — first scripts and hot reload
- [Scripting Tiers](scripting/scripting-tiers.md) — components, per-entity scripts, and game systems
- [Input](scripting/input.md) — keyboard, mouse, and event flow
- [Physics](scripting/physics.md) — collisions and queries from scripts
- [API Reference](scripting/api-reference.md) — `ScriptableEntity` methods

### Concepts
- [ECS Overview](concepts/ecs-overview.md) — entities, components, and systems
- [Scenes and Prefabs](concepts/scenes-and-prefabs.md) — scene lifecycle and reuse
- [Cameras and Rendering](concepts/cameras-and-rendering.md) — cameras and the 2D draw path
- [3D Rendering](concepts/3d-rendering.md) — models, lights, and materials

### Architecture
- [Architecture overview](../architecture/README.md) — solution structure and system docs
- [Rendering Pipeline](../architecture/rendering-pipeline.md) — 2D batching, 3D PBR, framebuffers
- [Game Loop](../architecture/game-loop.md) — application lifecycle and frame tick
- [Scripting Lifecycle](../architecture/scripting-lifecycle.md) — Roslyn compile, assembly reload, editor vs runtime

### Planning
- [Roadmap](roadmap.md) — milestones and planned work
