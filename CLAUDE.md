# Claude Code Repository Guidelines

**Tech Stack**: C# .NET 10.0 | OpenGL 3.3+ (Silk.NET) | ImGui.NET | Box2D | OpenAL | DryIoc DI
**Architecture**: ECS-based game engine with visual editor, hot-reloadable C# scripting, cross-platform (Windows/macOS)

## Solution Structure

```
GameEngine/
├── Engine/              # Core runtime (Animation, Audio, Renderer, Scene, Scripting)
│   ├── Core/            # Engine core utilities
│   ├── Events/          # Event system
│   ├── Math/            # Math utilities
│   ├── Platform/        # Platform abstraction
│   ├── Renderer/        # Rendering pipeline
│   └── Scene/
│       ├── Components/  # ECS components (Transform, Sprite, Physics, etc.)
│       └── Systems/     # Priority-based ECS systems
├── ECS/                 # Pure ECS (ISystem interface, SystemManager)
├── Editor/              # Visual editor with ImGui
│   ├── ComponentEditors/  # Component property editors + Core infrastructure
│   ├── DI/                # Editor DI registrations
│   ├── Features/          # Project, Scene, Settings (cohesive feature modules)
│   ├── Input/             # Editor input handling
│   ├── Logging/           # Editor logging
│   ├── Panels/            # Console, Properties, ContentBrowser, etc.
│   ├── Publisher/         # Build/publish pipeline
│   └── UI/
│       ├── Constants/     # EditorUIConstants
│       ├── Drawers/       # ButtonDrawer, ModalDrawer, TableDrawer, etc.
│       ├── Elements/      # ComponentSelector, drag-drop targets, etc.
│       └── FieldEditors/  # Generic field editors (int, float, Vector3, etc.)
├── Runtime/             # Standalone game player
├── Sandbox/             # Experimental/scratch project
├── Benchmark/           # Performance benchmarks
└── tests/               # Unit tests (ECS.Tests, Engine.Tests, ECS.Benchmarks)
```

## Code Standards

- **Primary constructors**: Always use for DI (C# 12+). Never traditional constructors with null checks.
- **Naming**: PascalCase (classes/methods/properties), `_camelCase` (private fields), `camelCase` (locals)
- **Static classes**: Only `EditorUIConstants` and `RenderingConstants` may be static. Everything else uses DI.
- **Components**: Data-only. Matrix calculations OK, no game logic — logic goes in Systems.
- **Comments**: No obvious XML docs or inline comments. Implementations don't repeat interface XML docs.
- **IDisposable**: All OpenGL resources (textures, buffers, shaders, framebuffers) must be disposed.

## Commands

```bash
dotnet build                        # Build — must produce zero warnings
dotnet test                         # Run all unit tests
dotnet test --filter "FullyQualifiedName~ECS"  # ECS tests only
dotnet test --filter "FullyQualifiedName~Engine"  # Engine tests only
cd Benchmark && dotnet run -c Release          # Performance benchmarks
```

## Quality Gates

Before committing:
- [ ] `dotnet test` passes
- [ ] `dotnet build` has zero warnings
- [ ] Editor launches without errors
