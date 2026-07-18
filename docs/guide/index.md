# Game Engine Developer Guide

This is a C# game engine built on an Entity Component System (ECS) architecture, offering production-ready **2D** rendering via OpenGL, **3D** mesh rendering with Assimp import and PBR metal/rough shading (cube fallback when no model is set), a visual editor powered by ImGui, and hot-reloadable C# scripting so you can iterate without restarting the application. It is designed to be cross-platform (Windows, macOS) and covers core game development needs: physics simulation, spatial audio, and sprite atlasing.

## Features

- **Entity Component System** — data-oriented architecture with priority-based systems and a clean component model
- **2D rendering** — OpenGL 3.3+ batched sprite pipeline with framebuffers and a flexible camera system
- **3D rendering** — FBX/glTF/GLB via Assimp, PBR materials, ambient + directional lights; unit-cube fallback when `ModelPath` is empty
- **Physics** — rigid-body simulation and collision detection via Box2D
- **C# scripting with hot reload** — write game logic in C#; changes are compiled and reloaded at runtime without restarting the editor
- **Audio support** — audio via OpenAL
- **Sprite atlasing** — `SubTextureRendererComponent` for sprite sheets with manual frame selection via grid coordinates
- **Visual editor** — ImGui editor with flat entity list, properties panel, content browser, and console

## Prerequisites

Before building, ensure you have the following installed:

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- A GPU with OpenGL 3.3 or newer support (most discrete and integrated GPUs from the last decade qualify)

## Quick Start

Clone the repository, build the solution, then launch the editor:

```bash
git clone <repo-url>
cd GameEngine
dotnet build
cd Editor && dotnet run
```

The editor window will open. From there you can create a new project, add entities to a scene, attach components, and run the game directly inside the editor viewport.

## Where to Go Next

Choose a path based on what you want to do:

- **New to the engine?** Start with the [Editor Setup Guide](editor/project-setup.md) to create your first project and get oriented in the editor UI.
- **Want to write game scripts?** See [Scripting Getting Started](scripting/getting-started.md) for an introduction to the C# scripting API and hot-reload workflow.
- **Building a 3D scene?** See [3D Rendering](concepts/3d-rendering.md) for models, lights, and materials.
- **Need to understand the architecture?** Read the [ECS Overview](concepts/ecs-overview.md) for entities, components, and systems, then [Game Loop](../architecture/game-loop.md) for application lifecycle and the frame tick.

For a look at what is planned, see the [Roadmap](roadmap.md).
