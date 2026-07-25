# System Architecture

## Overview
GameEngine is a multi-project .NET solution: a pure ECS library, an Engine runtime (rendering, physics, audio, scripting, scenes), an ImGui Editor app, and a lean Runtime player. Games are assemblies + assets loaded by Editor or Runtime.

Detailed C4 diagrams live in `docs/architecture/README.md`; this file is the Maister summary.

## Architecture Pattern
**Pattern**: Library + apps (game-engine) / umbrella solution with ECS data-driven runtime

Editor and Runtime both compose Engine + ECS. Platform details (OpenGL, Box2D) sit behind interfaces (`IRendererAPI`, `IPhysicsWorld2D`) so a future DirectX backend can plug in without rewriting gameplay.

## System Structure

### ECS
- **Location**: `ECS/`
- **Purpose**: Entity registry, components, `ISystem` / priority execution — no engine deps
- **Key Files**: `Entity.cs`, `Context.cs`, `Systems/ISystem.cs`, `SystemManager`

### Engine
- **Location**: `Engine/`
- **Purpose**: Application loop, DI, rendering, physics, audio, scene serialization, scripting host
- **Key Files**: `Core/Application.cs`, `Core/DI/EngineIoCContainer.cs`, `Renderer/IRendererAPI.cs`, `Scene/`, `Scripting/`

### SceneComponents
- **Location**: `SceneComponents/`
- **Purpose**: Shared component types (physics, rendering, lighting, audio, camera)

### Scripting / GameScriptSdk
- **Location**: `Scripting/`, `GameScriptSdk/`
- **Purpose**: `ScriptableEntity` API and staged SDK for game projects / hot-reload

### Editor
- **Location**: `Editor/`
- **Purpose**: ImGui tooling — viewport, hierarchy, inspectors, asset browser, publisher
- **Key Files**: `DI/EditorIoCContainer.cs`, `Panels/`, `ComponentEditors/`, `Publisher/`

### Runtime
- **Location**: `Runtime/`
- **Purpose**: Standalone player for published games (no editor overhead)

### Samples & Tests
- **Location**: `games/`, `tests/`
- **Purpose**: Snake / FlappyBird / ArenaShooter demos; xUnit unit + graphics integration tests

## Data Flow
1. Editor or Runtime boots DryIoc container and Application layer stack.
2. Scene JSON loads entities/components into ECS `Context`.
3. Systems run each frame (input → physics → scripts → render) by priority.
4. Scripts compile via Roslyn into collectible load contexts for hot-reload in Editor.
5. Publisher packs Runtime + game assembly + assets for distribution.

## External Integrations
- **Silk.NET** — windowing, input, OpenGL, OpenAL
- **Box2D** — 2D physics
- **Assimp / image libs** — mesh and texture import
- **ImGui** — editor UI
- **GPU / OS** — OpenGL 3.3+ drivers; Windows & macOS primary targets

## Database Schema
N/A — file-based scenes, prefabs, and assets.

## Configuration
- Project: `game.config.json` under game folders
- Engine/Editor: DI registration + Serilog setup in `Program.cs` entry points
- Maister: `.maister/config.yml`

## Deployment Architecture
- Dev: `dotnet run` Editor
- Ship: Editor publish pipeline → Runtime executable + assets (Windows/macOS)
- CI: GitHub Actions build/test on Ubuntu (graphics via xvfb)

---
*Based on codebase analysis performed 2026-07-23*
