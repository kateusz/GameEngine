# Game Engine Developer Guide

This is a C# game engine built on an Entity Component System (ECS) architecture, offering 2D and 3D rendering via OpenGL, a visual editor powered by ImGui, and hot-reloadable C# scripting so you can iterate without restarting the application. It is designed to be cross-platform (Windows, macOS) and covers the full range of common game development needs: physics simulation, spatial audio, and sprite animation.

## Features

- **Entity Component System** — data-oriented architecture with priority-based systems and a clean component model
- **2D and 3D rendering** — OpenGL 3.3+ renderer with sprite batching, framebuffers, and a flexible camera system
- **Physics** — rigid-body simulation and collision detection via Box2D
- **C# scripting with hot reload** — write game logic in C#; changes are compiled and reloaded at runtime without restarting the editor
- **Audio with 3D spatial support** — positional audio via OpenAL, including attenuation and listener tracking
- **Sprite animation** — frame-based animation system integrated with the ECS component pipeline
- **Visual editor** — full-featured ImGui editor with scene hierarchy, properties panel, content browser, and console

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
- **Need to understand the architecture?** Read the [ECS Overview](concepts/ecs-overview.md) for a deep dive into entities, components, and systems.

For a look at what is planned, see the [Roadmap](roadmap.md).
