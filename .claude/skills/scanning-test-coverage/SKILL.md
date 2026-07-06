---
name: scanning-test-coverage
description: Scans a given module (source directory) for missing unit tests. Discovers source types, checks for corresponding test files, and reports coverage gaps. Optionally generates test stubs following project conventions. Use when adding new code, ensuring test coverage, onboarding to the existing test suite, or invoking /scanning-test-coverage.
---

# Scanning Test Coverage

## Contents

- [Overview](#overview)
- [When to Use](#when-to-use)
- [Workflow](#workflow)
- [Automated scan script](#automated-scan-script)
- [Example](#example-scanning-enginescenesystems)
- [Test conventions](conventions.md)
- [Common mistakes](common-mistakes.md)

## Overview

Scans a source module directory and identifies which source types lack corresponding unit test files. Conventions: [conventions.md](conventions.md).

## When to Use

Invoke this skill when:

- Adding a new class/module that needs unit tests
- Doing a coverage audit before a release
- Onboarding to an existing module and wanting to understand test gaps
- Generating test stubs for newly created source types
- Reviewing PRs for test completeness

## Workflow

### Step 1: Identify the Module and Target Test Project

Determine the source directory and corresponding test project:

| Source Project | Source Dir | Test Project | Test Dir |
|---------------|-----------|-------------|----------|
| `ECS` | `ECS/` | `tests/ECS.Tests/` | `tests/ECS.Tests/` |
| `Engine` | `Engine/` | `tests/Engine.Tests/` | `tests/Engine.Tests/` |
| `Engine.Scene.Systems` | `Engine/Scene/Systems/` | `tests/Engine.Tests/` | `tests/Engine.Tests/` |
| `Engine.Renderer` | `Engine/Renderer/` | `tests/Engine.Tests/` | `tests/Engine.Tests/Renderer/` |

**Rule**: If the source sub-namespace has a matching subdirectory under `tests/{Project}.Tests/`, tests go there. Otherwise, they go in the root of the test project.

**Unmapped paths** (not in the table above):

1. `Editor/` or `Runtime/` → skip with note: manual or integration verification, not covered by this skill.
2. Otherwise, find the owning `.csproj` under the repo root (`ECS`, `Engine`, etc.).
3. Test project = `tests/{ProjectName}.Tests/`.
4. Mirror subdirectory structure from the project root (same rule as mapped paths).

If no `.csproj` matches or the path is outside known projects, stop and ask the user to confirm the target test project.

### Step 2: Discover Source Types

Scan the given source directory for public types that should have tests. Include:

- Public classes and records (components, systems, services)
- Public structs used as data types
- Static utility/service classes

Exclude (no test needed):

- Interfaces (tested via implementations)
- Enum types (tested implicitly if used)
- Private/internal types
- Pure data-only records without logic (e.g., `VelocityComponent(Vector2 Velocity)`)
- Editor-only UI code (manual verification, not unit-testable)
- Types marked with `[SkipUnitTests]` attribute (defined in `Engine.Core`)

**Source of truth** (cross-platform, requires `rg` on PATH):

```bash
rg "public (?:\w+ )*(class|record|struct) " <source-dir>/ -g '*.cs'
```

If `rg` returns no matches, report zero discoverable types and stop. If `rg` is not installed, fall back to the IDE grep tool with the same pattern.

### Step 3: Resolve Expected Test File Path

**Default (1:1 mapping)** — for each source type `{ClassName}` in namespace `{SourceNamespace}`:

1. Expected test filename: `{ClassName}Tests.cs`
2. Expected test namespace: `{SourceNamespace}.Tests` (e.g., `Engine.Scene.Components` → `Engine.Tests.Components`)
3. Expected test project: `tests/{SourceProject}.Tests/`
4. Subdirectory: mirror path under the source project root. For `Engine/Scene/{Module}/`, drop the `Scene/` segment (e.g., `Engine/Scene/Systems/` → `tests/Engine.Tests/Systems/`; `Engine/Renderer/` → `tests/Engine.Tests/Renderer/`).

**Exception: feature-area grouping** — use when the repo already groups related types in one test file:

- Serialization suites (`SerializationTests.cs`, `ComponentSerializerRegistryTests.cs`)
- Small extension/helper types tested alongside a parent type (e.g., `ShaderDataTypeExtensions` in `ShaderDataTypeExtensionsTests.cs` under `tests/Engine.Tests/Renderer/`)
- Multiple types in one source area sharing one behavioral concern

For grouped types, the expected file name may differ from `{ClassName}Tests.cs`. Do not flag as missing until Step 4 grouped detection runs.

**Example mapping**:

- `Engine/Scene/Components/TransformComponent.cs` → `tests/Engine.Tests/Components/TransformComponentTests.cs` (1:1)
- `ECS/Context.cs` → `tests/ECS.Tests/ContextTests.cs` (1:1)
- `Engine/Renderer/ShaderDataTypeExtensions.cs` → `tests/Engine.Tests/Renderer/ShaderDataTypeExtensionsTests.cs` (feature-area grouping; not `ShaderTests.cs`)

### Step 4: Check for Existing Test File

1. **1:1 file anywhere in test project** — search for `{ClassName}Tests.cs` under the test project (mirrored path is preferred for new tests, but existing tests may live at the project root):

```bash
rg --files tests/Engine.Tests -g 'TransformComponentTests.cs'
```

2. **Grouped coverage** — if no 1:1 file exists, search for references to the type in other test files (exclude `*Tests.cs` files named after the type):

```bash
rg -l '\bClassName\b' tests/Engine.Tests -g '*.cs'
```

If a match is found in a differently named test file, mark as **grouped** (not missing). Prefer files with `#region ClassName` or test methods that exercise the type.

3. If neither 1:1 nor grouped match exists, flag as **missing**. Use the mirrored path from Step 3 as the expected location for new stubs.

### Step 5: Report Coverage Gaps

Produce a structured report:

```
# Test Coverage Report: {Module}

## Missing Tests ({count})
| Source File | Source Type | Expected Test File |
|------------|------------|-------------------|
| Engine/Scene/Systems/MySystem.cs | MySystem | tests/Engine.Tests/Systems/MySystemTests.cs |

## Covered ({count})
| Source File | Source Type | Test File |
|------------|------------|----------|
| ECS/Context.cs | Context | tests/ECS.Tests/ContextTests.cs |

## Grouped ({count})
| Source File | Source Type | Test File |
|------------|------------|----------|
| Engine/Renderer/ShaderDataTypeExtensions.cs | ShaderDataTypeExtensions | tests/Engine.Tests/Renderer/ShaderDataTypeExtensionsTests.cs |

## Notes
- {count} types excluded (interfaces, enums, data-only records)
- {count} types grouped into existing test files
```

### Step 6: Generate Test Stubs (Optional)

If asked, generate stub test files for **missing** types only. Follow [conventions.md](conventions.md#stub-template).

Run `dotnet build` in the test project directory. If the build fails, fix namespace/path mismatches before finishing.

## Automated scan script

For repeatable scans, run from the repo root. Both scripts emit the same `covered|`, `grouped|`, `missing|`, and `summary|` lines for Step 5.

**Unix / Git Bash**:

```bash
bash .claude/skills/scanning-test-coverage/scan.sh <source-dir> <test-dir> [source-project-root]
```

**Windows (PowerShell)**:

```powershell
powershell -File .claude/skills/scanning-test-coverage/scan.ps1 -SourceDir Engine/Scene/Systems -TestDir tests/Engine.Tests -SourceRoot Engine
```

Example output line: `missing|Engine/Scene/Systems/Foo.cs|Foo|tests/Engine.Tests/Systems/FooTests.cs`

Map script output into the Step 5 report. Scripts exit non-zero if `rg` is missing or directories are invalid.

## Example: Scanning Engine/Scene/Systems

**Discover types**:

```bash
rg "public (?:\w+ )*(class|record|struct) " Engine/Scene/Systems/ -g '*.cs'
```

**Source types found** (public only; most systems are `internal`):

| Type | File |
|------|------|
| `LightingSystem` | `Engine/Scene/Systems/LightingSystem.cs` |
| `PhysicsContactQueue` | `Engine/Scene/Systems/PhysicsContactQueue.cs` |

**Check coverage** (script or manual glob):

```powershell
powershell -File .claude/skills/scanning-test-coverage/scan.ps1 -SourceDir Engine/Scene/Systems -TestDir tests/Engine.Tests -SourceRoot Engine
```

**Report output** (from scan script):

```
# Test Coverage Report: Engine/Scene/Systems

## Missing Tests (2)
| Source File | Source Type | Expected Test File |
|------------|------------|-------------------|
| Engine/Scene/Systems/LightingSystem.cs | LightingSystem | tests/Engine.Tests/Systems/LightingSystemTests.cs |
| Engine/Scene/Systems/SystemPriorities.cs | SystemPriorities | tests/Engine.Tests/Systems/SystemPrioritiesTests.cs |

## Covered (1)
| Source File | Source Type | Test File |
|------------|------------|----------|
| Engine/Scene/Systems/PhysicsContactQueue.cs | PhysicsContactQueue | tests/Engine.Tests/PhysicsContactQueueTests.cs |

## Grouped (1)
| Source File | Source Type | Test File |
|------------|------------|----------|
| Engine/Scene/Systems/PhysicsRuntimeBodyStore.cs | PhysicsRuntimeBodyStore | tests/Engine.Tests/SceneTests.cs |
```
