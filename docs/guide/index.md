# Game Engine Developer Guide

C# / .NET 10 ECS game engine: **2D** batched sprites, **3D** forward rendering (PBR, Cook-Torrance lights, directional/point shadows), ImGui **editor**, and C# **scripting** compiled into a GameAssembly (reload without restarting the editor). Cross-platform: **Windows** and **macOS**.

## Features

### Runtime

- **Entity Component System** — priority-sorted systems; built-in components in `SceneComponents/` plus game `IGameComponent` / `IGameSystem` types
- **Scenes** — JSON `.scene` files (`TwoD` / `ThreeD` dimension, entities, components); prefabs under `assets/prefabs/`
- **2D rendering** — OpenGL 3.3+ batched sprites, orthographic/perspective cameras
- **3D rendering** — `.glb` / `.gltf` / `.fbx`, runtime `.mesh` siblings, mesh instancing, frustum culling, visibility zones, FXAA in the editor viewport only
- **Physics** — Box2D 2D rigid bodies, colliders, raycast/overlap, contact events for systems
- **Audio** — OpenAL spatial audio (WAV/Ogg), optional EFX per source
- **Scripting** — Roslyn compile in the editor; reload on **Play**, project open, and Content Browser script creation ([Scripting Lifecycle](../architecture/scripting-lifecycle.md)). External IDE saves are **not** watched automatically.

### Editor

- **Hierarchy** — search, component-type filter, multi-select, multi-entity edit, drag-reparent
- **Viewport** — edit camera, ImGuizmo tools, 3D pick, selection outline (edit mode), drag-drop assets and models
- **Panels** — Properties (field search), Godot-style **bottom** tabs (Console, Content Browser), **Stats** debug overlay
- **Command palette** — `Ctrl+Shift+P` (menu actions, jump to entity)
- **Project → Settings** — `game.config.json` (startup scene, title, window, FPS); **Scene → Settings** — scene background color
- **Publish** — **Project → Export…** standalone player for host RID (Windows/macOS x64 or ARM64)

See also the repo [README](../../README.md) and [Changelog](../../CHANGELOG.md).

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- OpenGL 3.3+ GPU

## Quick Start

```bash
git clone https://github.com/kateusz/GameEngine.git
cd GameEngine
dotnet build
cd Editor && dotnet run
```

Create a project, add a scene (`Ctrl+N`), attach components, add scripts, press **Play**.

**Sample games** under `games/` — open the **game folder** (e.g. `games/Snake/`, `games/FlappyBird/`, `games/ArenaShooter/`) via **Project → Open…**. Each has `game.config.json` and `assets/` at the project root.

## Where to Go Next

### Editor

- [Scene Editor](editor/scene-editor.md) — hierarchy, viewport, play mode, layout, menus
- [Component Inspector](editor/component-inspector.md) — built-in components in Properties
- [Content Browser](editor/content-browser.md) — assets and drag-and-drop
- [Shortcuts](editor/shortcuts.md) — keyboard reference

### Scripting

- [Getting Started](scripting/getting-started.md) — first system and compile/reload
- [Scripting Tiers](scripting/scripting-tiers.md) — components vs systems
- [Input](scripting/input.md) — keyboard and mouse in Play mode
- [Physics](scripting/physics.md) — collisions and queries
- [API Reference](scripting/api-reference.md) — `IGameSystem` services

### Concepts

- [ECS Overview](concepts/ecs-overview.md) — entities, components, systems
- [Scenes and Prefabs](concepts/scenes-and-prefabs.md) — lifecycle and prefab v2
- [Cameras and Rendering](concepts/cameras-and-rendering.md) — 2D/3D draw rules, lights, shadows

### Architecture

- [Architecture overview](../architecture/README.md)
- [Scene Rendering Pipeline](../architecture/scene-rendering-pipeline.md)
- [Lighting](../architecture/lighting.md)
- [Shadows](../architecture/shadows.md)
- [Game Loop](../architecture/game-loop.md)
- [Scripting Lifecycle](../architecture/scripting-lifecycle.md)
