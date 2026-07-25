# Technology Stack

## Overview
This document describes the technology choices and rationale for GameEngine — a .NET 10 ECS engine with OpenGL rendering, ImGui editor, and standalone runtime.

## Languages

### C# (.NET 10 / `net10.0`)
- **Usage**: ~100% of engine and tools code
- **Rationale**: Strong tooling, Roslyn scripting/hot-reload, cross-platform desktop apps
- **Key Features Used**: Nullable reference types, implicit usings, primary constructors

## Frameworks

### Engine / Runtime
- Custom **ECS** (`ECS/`) — entity, components, priority-ordered systems
- **DryIoc 5.4.3** — DI for Engine and Editor containers
- **Silk.NET 2.23.0** — OpenGL, windowing, input, OpenAL
- **Box2D.NetStandard 2.4.7-alpha** — 2D physics
- **Roslyn 5.0.0** (`Microsoft.CodeAnalysis.CSharp` + Scripting) — gameplay script compile/hot-reload
- **Serilog** — structured logging
- **CSharpFunctionalExtensions**, **ZLinq** — functional helpers / zero-allocation LINQ patterns

### Editor UI
- **Silk.NET.OpenGL.Extensions.ImGui** (`Ui.ImGui/`) — immediate-mode editor UI (not a web frontend)

### Testing
- **xUnit 2.9.3**, **Shouldly**, **NSubstitute**, **Bogus**, **coverlet**
- Graphics tests tagged `GraphicsIntegration` (CI under xvfb + Mesa on Linux)

## Database
Not applicable — scenes/prefabs/assets are file-based JSON and binary assets, not a DB.

## Build Tools & Package Management
- **.NET SDK 10.0.0** pinned via `global.json`
- **MSBuild** / `dotnet` CLI; NuGet package references in `.csproj`
- Custom targets for OpenAL native copy and GameScriptSdk staging

## Infrastructure

### Containerization
None required for core workflow.

### CI/CD
- **GitHub Actions** (`.github/workflows/dotnet.yml`) — restore, build, test; graphics tests with xvfb

### Hosting
Desktop distribution — Editor/Runtime publish for Windows and macOS (CI also runs on Ubuntu).

## Development Tools

### Linting & Formatting
- No root `.editorconfig` / `Directory.Build.props` detected
- JetBrains settings: `GameEngine.sln.DotSettings`

### Type Checking
- C# compiler with nullable enabled across projects

## Key Dependencies
| Area | Package |
|------|---------|
| Graphics | Silk.NET.OpenGL, Assimp, StbImageSharp, Pfim |
| Audio | Silk.NET.OpenAL, OpenAL.Soft, NVorbis |
| Physics | Box2D.NetStandard |
| DI | DryIoc |
| Scripting | Roslyn CSharp + Scripting |
| Logging | Serilog (+ sinks/enrichers) |

## Version Management
- SDK version: `global.json`
- Library versions: per-project `PackageReference` in `.csproj`
- Target: OpenGL now; DirectX later behind `IRendererAPI`-style abstraction

## Migration Path
Not a legacy stack. Planned evolution is feature depth (animation, FBX, 3D workflow) and optional second graphics backend (DirectX), not a framework rewrite.

---
*Last Updated*: 2026-07-23
*Auto-detected*: languages, packages, CI, test stack from solution/csproj analysis
*User-provided*: OpenGL primary, DirectX later; 2D/3D animation + FBX goals
