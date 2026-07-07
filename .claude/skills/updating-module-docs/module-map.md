# Module Map

Resolve the user's module argument (case-insensitive, hyphens ok) to these entries. Use aliases in parentheses.

| Module | Aliases | Source scope | Exclude | Primary doc | Secondary docs | Doc strategy | README sections | README grep |
|--------|---------|--------------|---------|-------------|----------------|--------------|-----------------|-------------|
| `ecs` | entity-component-system | `ECS/`, `SceneComponents/` | — | `docs/architecture/ecs-architecture.md` | `docs/guide/concepts/ecs-overview.md` | — | Features → Core Engine; Architecture → Project Structure (`ECS/`); Architectural Highlights → ECS components | `Entity Component System`, `├── ECS/`, `Built-in Components` |
| `rendering` | renderer, graphics, renderer2d, renderer3d | `Engine/Renderer/`, `Engine/Scene/Systems/SceneRenderSystem.cs`, `Engine/Scene/Systems/PhysicsDebugRenderSystem.cs`, `Engine/Scene/Systems/PrimaryCameraSystem.cs`, `Engine/Scene/SceneRenderPipeline.cs` | — | `docs/architecture/rendering-pipeline.md` | `docs/opengl/opengl-2d-workflow.md`, `docs/opengl/opengl-3d-workflow.md`, `docs/guide/concepts/cameras-and-rendering.md` | — | Features → Rendering; Key Systems → Renderer2D/3D; Documentation → Rendering; OpenGL Rendering Workflows | `### 🎨 Rendering`, `Renderer2D/3D`, `**Rendering:**`, `### OpenGL Rendering` |
| `scene` | scenes, prefabs | `Engine/Scene/` | `Engine/Scene/Systems/`, `Engine/Scene/Serializer/` | `docs/guide/concepts/scenes-and-prefabs.md` | `docs/architecture/serialization.md` (scene JSON sections only) | — | Features → Core Engine; Key Systems → Scene System; Architecture → `Engine/Scene/` tree | `Scene System`, `├── Scene/` |
| `scripting` | scripts, roslyn, hot-reload | `Engine/Scripting/` | — | `docs/architecture/scripting-lifecycle.md` | `docs/guide/scripting/getting-started.md`, `docs/guide/scripting/api-reference.md`, `docs/guide/scripting/scripting-tiers.md` | — | Features → Scripting System; Key Systems → ScriptEngine | `### 🔧 Scripting`, `ScriptEngine` |
| `physics` | box2d | `Engine/Physics/`, `Engine/Platform/Box2D/`, `Engine/Scene/Systems/Physics*.cs` | — | `docs/architecture/physics-system.md` | `docs/guide/scripting/physics.md` | — | Features → Core Engine (physics bullet); Key Systems (if listed) | `Physics Integration`, `Box2D` |
| `audio` | openal, sound | `Engine/Audio/`, `Engine/Platform/OpenAL/`, `Engine/Scene/Systems/AudioSystem.cs` | — | `docs/architecture/audio-system.md` | — | — | Features → Audio System; Key Systems → Audio System | `### 🎧 Audio`, `Audio System` |
| `serialization` | serializer, json, prefabs | `Engine/Scene/Serializer/` | — | `docs/architecture/serialization.md` | `docs/guide/concepts/scenes-and-prefabs.md` (serialization sections only) | — | Architecture → `Serializer/` line | `Serializer/` |
| `dependency-injection` | di, dryioc, ioc | `Engine/Core/DI/`, `Editor/DI/` | — | `docs/architecture/dependency-injection.md` | — | — | Features → Core Engine (DI bullet); Architectural Highlights → Design Patterns | `Dependency Injection`, `DryIoc` |
| `game-loop` | application, core, lifecycle | `Engine/Core/Application.cs`, `Engine/Core/IApplication.cs`, `Engine/Core/IFrameCompositor.cs`, `Runtime/` | — | `docs/architecture/game-loop.md` | `docs/guide/index.md` (architecture pointer only) | — | Features → Core Engine | `Game Loop`, `Application` |
| `events` | input, keyboard, mouse | `Engine/Events/`, `Engine/Core/Input/`, `Engine/Platform/SilkNet/Input/` | — | — | `docs/guide/scripting/input.md` | Update guide only; no new architecture file unless user asks | Features → Scripting (event bullet); Key Systems → Event System | `Event System`, `Event-driven` |
| `editor` | panels, inspector, viewport | `Editor/` | — | — | `docs/guide/editor/*.md` | Update matching guide files under `docs/guide/editor/`; no new file unless user asks | Features → Editor; Documentation → Tools & Publishing | `### 🛠️ Editor`, `Editor Tools` |
| `cameras` | camera | `Engine/Scene/Cameras/`, `Engine/Scene/SceneCamera.cs`, `Engine/Scene/Systems/PrimaryCameraSystem.cs` | — | `docs/architecture/rendering-pipeline.md` (camera sections only) | `docs/guide/concepts/cameras-and-rendering.md` | — | Features → Rendering (camera bullet); Key Systems → Camera System | `Camera System`, `CameraComponent` |
| `framebuffers` | framebuffer, render-to-texture | `Engine/Renderer/Buffers/FrameBuffer/` | — | `docs/opengl/frame-buffers.md` | `docs/architecture/rendering-pipeline.md` (framebuffer sections only) | — | Features → Rendering (framebuffer bullet) | `Framebuffer Support` |
| `platform` | opengl, silknet, window | `Engine/Platform/`, `Engine/Core/Window/` | — | — | `docs/opengl/*.md` (as relevant) | Update relevant OpenGL guide sections only | Architecture → `Platform/` line | `├── Platform/` |
| `publishing` | publisher, build, deploy | `Editor/Publisher/`, `Runtime/` | — | — | — | README link + Features only; create `docs/guide/` page only if user asks | Documentation → Tools & Publishing | `Publishing`, `Game Publishing` |

## Unmapped module

If the argument matches no row:

1. Try substring match on module name and aliases.
2. If still ambiguous, ask the user to pick from the table — do **not** scan the whole codebase to guess.

## Stale README links

`README.md` still references `docs/modules/` and `docs/opengl-rendering/` in places. When updating any module, fix **only** that module's links to point at current paths (`docs/architecture/`, `docs/guide/`, `docs/opengl/`).

## Overlap quick reference

See [SKILL.md — Overlap policy](SKILL.md#overlap-policy). Prefer the invoked module's row; do not edit another module's primary doc unless the user names both.
