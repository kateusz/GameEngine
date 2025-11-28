# Claude Code Repository Guidelines

Essential guidelines for Claude Code agents working with this C#/.NET 10.0 game engine. Detailed workflows are handled by specialized skills.

## Table of Contents

- [Project Overview](#project-overview)
- [Architecture](#architecture)
- [Development Guidelines](#development-guidelines)
- [Code Standards](#code-standards)
- [Testing & Quality Assurance](#testing--quality-assurance)
- [Key Resources](#key-resources)

---

## Project Overview

**Tech Stack**: C# .NET 10.0 | OpenGL 3.3+ (Silk.NET) | ImGui.NET | Box2D | OpenAL | DryIoc DI

**Architecture**: ECS-based game engine with visual editor, hot-reloadable C# scripting, cross-platform support (Windows/macOS/Linux)

## Architecture

### Solution Structure

```
GameEngine/
├── Engine/              # Core runtime (Animation, Audio, Renderer, Scene, Scripting)
│   └── Scene/
│       ├── Components/  # 18 ECS components (Transform, Sprite, Physics, etc.)
│       └── Systems/     # Priority-based ECS systems
├── ECS/                 # Pure ECS (ISystem interface, SystemManager)
├── Editor/              # Visual editor with ImGui
│   ├── ComponentEditors/  # Component property editors + Core infrastructure
│   ├── Features/          # Project, Scene, Settings (cohesive feature modules)
│   ├── Panels/            # Console, Properties, ContentBrowser, etc.
│   └── UI/
│       ├── Constants/     # EditorUIConstants
│       ├── Drawers/       # ButtonDrawer, ModalDrawer, TableDrawer, etc.
│       ├── Elements/      # ComponentSelector, drag-drop targets, etc.
│       └── FieldEditors/  # Generic field editors (int, float, Vector3, etc.)
├── Runtime/             # Standalone game player
└── tests/               # Unit tests (ECS.Tests, Engine.Tests)
```

### Key Patterns

**ECS**: Entity (GUID) + Component (data-only) + System (logic with priority ordering)

**Dependency Injection**: Constructor injection via DryIoc, NO static singletons (Program.cs has 50+ registrations)

**Factories**: TextureFactory, ShaderFactory, RendererApiFactory, etc. (caching + DI)

**Rendering**: IRendererAPI abstracts OpenGL, batched 2D (10K quads), texture/shader caching

**Editor UI**: Feature-based organization + reusable Drawers/Elements/FieldEditors

## Development Guidelines

### Code Organization

**Features/** = Cohesive multi-component features (Project, Scene, Settings)
**Panels/** = Standalone utility panels (Console, Stats, ContentBrowser)
**ComponentEditors/** = ECS component property editors (registered in ComponentEditorRegistry)
**UI/** = Reusable infrastructure (Constants, Drawers, Elements, FieldEditors)

### Core Principles

1. **Dependency Injection** - Constructor injection via DryIoc, NO static singletons
2. **Use constants** - EditorUIConstants, RenderingConstants (never magic numbers)
3. **Performance matters** - Minimize allocations in hot paths, use caching, profile changes
4. **Cross-platform** - All OpenGL via IRendererAPI, use Path.Combine(), platform abstractions
5. **Component = data only** - Logic goes in Systems (priority-based execution)
6. **Factory pattern** - Use TextureFactory, ShaderFactory, etc. for resource creation

## Code Standards

### C# Style

- **Modern C#**: Records, nullable types, pattern matching, required properties
- **Naming**: PascalCase (classes/methods/properties), _camelCase (private fields), camelCase (locals)
- **Components**: Data-only classes (matrix calculations OK, but no game logic)
- **IDisposable**: Implement for all OpenGL resources (textures, buffers, shaders, etc.)
- **Performance**: Use Span<T> for stack allocs, object pooling, avoid boxing in hot paths

### Constants (NO magic numbers!)

**EditorUIConstants**: Button sizes, layout ratios (PropertyLabelRatio = 0.33f), spacing, colors (Error/Warning/Success), axis colors (X=red, Y=green, Z=blue)

**RenderingConstants**: MaxQuads (10K), MaxTextureSlots (16), QuadVertexCount (4), MaxFramebufferSize (8K)

**Rule**: Only EditorUIConstants and RenderingConstants may be static classes. Everything else uses DI!

### Editor UI

**Drawers**: ButtonDrawer, ModalDrawer, TableDrawer, TreeDrawer, TextDrawer, LayoutDrawer, DragDropDrawer
**Elements**: ComponentSelector, EntityContextMenu, TextureDropTarget, AudioDropTarget, etc.
**FieldEditors**: IFieldEditor for script inspector (non-generic, boxing-based for reflection)

Example:
```csharp
// Always use Drawers/Elements instead of raw ImGui
if (ButtonDrawer.DrawButton("Save", ButtonDrawer.ButtonType.Primary)) { ... }
ModalDrawer.RenderConfirmationModal("Delete?", ref _show, "Sure?", () => Delete());
TextureDropTarget.Draw("Icon", path, newPath => component.IconPath = newPath);
```

### Resource Management

**CRITICAL**: ALL OpenGL resources MUST be properly disposed. Memory leaks and GPU resource exhaustion are unacceptable.

**IDisposable Pattern** (MANDATORY):
- **Textures**: Texture2D, RenderTexture, Framebuffers
- **Buffers**: VertexBuffer, IndexBuffer, VertexArray
- **Shaders**: Shader, ShaderProgram
- **Audio**: AudioSource, AudioBuffer
- Always use `using` statements or explicit `Dispose()` calls
- Never rely on finalizers alone

**Ownership Rules**:
- **Factories own cached resources**: TextureFactory, ShaderFactory dispose their caches
- **Components DON'T own resources**: Sprite2DComponent references texture path, not Texture2D instance
- **Systems create/dispose as needed**: Renderer2DSystem creates batches, disposes on shutdown
- **Scene cleanup**: All scene resources disposed on scene unload

**Common Pitfalls**:
```csharp
// BAD - Resource leak
var texture = new Texture2D(path);
someComponent.Texture = texture; // Who disposes this?

// GOOD - Factory manages lifetime
var texturePath = "assets/player.png";
someComponent.TexturePath = texturePath; // Renderer2DSystem uses TextureFactory

// GOOD - Explicit disposal
using var tempTexture = new Texture2D("temp.png");
ProcessTexture(tempTexture);
// Automatically disposed
```

**Verification**: Use `resource-management-audit` skill to review resource lifecycles and identify leaks.

## Testing & Quality Assurance

### When to Write Tests

**Unit Tests** (`tests/ECS.Tests`, `tests/Engine.Tests`):
- **Core ECS logic**: Entity management, component operations, system execution order
- **Math utilities**: Vector operations, matrix calculations, collision detection
- **Serialization**: Component JSON serialization/deserialization
- **Resource factories**: Cache behavior, texture loading, shader compilation
- **NOT needed**: Editor UI, ImGui interactions, OpenGL rendering

**Manual Testing Required**:
- OpenGL rendering (visual verification)
- ImGui editor panels (UI/UX)
- Input handling (keyboard, mouse)
- Cross-platform behavior (Windows, macOS, Linux)

### Running Tests

```bash
dotnet test                          # Run all tests
dotnet test --filter "FullyQualifiedName~ECS"  # ECS tests only
dotnet test --logger "console;verbosity=detailed"  # Verbose output
```

### Performance Expectations

**Editor Performance**:
- **Target**: 60 FPS with 1000+ entities (simple components)
- **Hot paths**: OnUpdate() methods, rendering loops, entity queries
- **Allocation budget**: <1MB/frame in steady state
- **Batch efficiency**: >80% filled quads in renderer batches

**Benchmarking**:
```bash
cd Benchmark
dotnet run -c Release  # Run performance benchmarks
```

**When to benchmark**:
- Changes to ECS core (entity iteration, component access)
- Renderer modifications (batching, draw calls, state changes)
- Memory allocation patterns (use `dotnet-counters` or profilers)

### Cross-Platform Verification

**Before major changes**:
1. Test on target platforms (Windows, macOS, Linux)
2. Verify OpenGL calls through IRendererAPI (no platform-specific code in Engine/)
3. Check file path handling (use `Path.Combine()`, not hardcoded separators)
4. Test asset loading (textures, audio, shaders) on all platforms

**Common Issues**:
- OpenGL context differences (macOS requires Core Profile)
- Audio backend variations (OpenAL implementation differences)
- File system case sensitivity (macOS/Linux vs Windows)
- Path separators (`/` vs `\`)

### Quality Gates

**Before committing**:
- [ ] All unit tests pass (`dotnet test`)
- [ ] No build warnings (`dotnet build`)
- [ ] Editor launches without errors
- [ ] No resource leaks (check with `resource-management-audit` skill)

**For performance-critical changes**:
- [ ] Benchmark results show no regression
- [ ] Frame time stays under 16.67ms (60 FPS)
- [ ] Memory allocations in hot paths minimized

**Philosophy**: Performance-critical game engine with clean architecture. Use DI, follow existing patterns, leverage skills for detailed workflows, profile changes, maintain cross-platform compatibility.
