## C# Project Conventions

### Nullable Reference Types
All C# projects must enable nullable reference types (`Nullable=enable`) so nullability warnings are part of the default compile surface.

**Sources:** code-patterns, config (confidence 92%)

```csharp
<Nullable>enable</Nullable> in every new .csproj PropertyGroup
<Nullable>enable</Nullable>
```

### Implicit Usings
Projects enable SDK implicit usings (`ImplicitUsings=enable`) rather than maintaining manual global using lists for BCL namespaces.

**Sources:** config (confidence 85%)

```csharp
<ImplicitUsings>enable</ImplicitUsings> in project PropertyGroup
```

### Target Framework net10.0
Solution projects target .NET 10 (`net10.0`). The Editor is the exception on Windows, where it uses `net10.0-windows` for WinForms folder-picker support; non-Windows Editor builds stay on `net10.0`.

**Sources:** config (confidence 85%)

```csharp
<TargetFramework>net10.0</TargetFramework>
```

### File-Scoped Namespaces
All production C# files use file-scoped namespace declarations (`namespace X;`) rather than block-scoped namespaces.

**Sources:** code-patterns (confidence 88%)

```csharp
namespace Engine.Scene.Systems;
namespace Editor.ComponentEditors;
```

### PascalCase Files and Root Namespaces
C# source files use PascalCase names matching their primary type (e.g. AudioSystem.cs, IRendererAPI.cs). No snake_case or kebab-case filenames.

**Sources:** code-patterns (confidence 88%)

```csharp
TransformComponent.cs / public class TransformComponent
ISystem.cs / public interface ISystem
```

### Interface File Prefix I
Files whose primary type is an interface are named with an I prefix matching the interface (ISystem, IComponentEditor). Exception: IComponent lives in Component.cs.

**Sources:** code-patterns (confidence 85%)

```csharp
public interface ISystem { ... }
// file: IRendererAPI.cs
```

### SonarQube Cloud Quality Gate
PRs are gated by SonarQube Cloud: Reliability Rating on New Code must be ≥ A; Duplication on New Code must be ≤ 3%; open Security Hotspots can fail the gate.

**Sources:** ci-config (confidence 88%)
