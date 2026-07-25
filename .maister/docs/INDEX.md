# Documentation Index

**IMPORTANT**: Read this file at the beginning of any development task to understand available documentation and standards.

## Quick Reference

### Project Documentation
Project-level documentation covering vision, goals, architecture, and technology choices.

### Technical Standards
Coding standards, conventions, and best practices organized by domain.

---

## Project Documentation

Located in `.maister/docs/project/`

### Vision (`project/vision.md`)
C# ECS game engine with ImGui editor and standalone runtime for 2D/3D games on OpenGL (DirectX later). Active development toward public 2D alpha; near-term goals are 2D animation, FBX import with animation, and 3D scene workflow.

### Roadmap (`project/roadmap.md`)
Pre-alpha roadmap aligned with `docs/guide/roadmap.md`: high priority on 2D animation, FBX+animation import, 3D scene workflow, and M1 leftovers; medium priority M2–M5 (undo, runtime UI, hierarchy, public alpha packaging); notes technical debt and post-alpha ideas (DirectX, particles).

### Tech Stack (`project/tech-stack.md`)
.NET 10 / C#, custom ECS, DryIoc, Silk.NET OpenGL/OpenAL, Box2D, Roslyn scripting, Serilog, ImGui editor UI; xUnit/Shouldly/NSubstitute testing; GitHub Actions CI; file-based assets (no DB); OpenGL primary with DirectX path via renderer abstraction.

### Architecture (`project/architecture.md`)
Multi-project solution: ECS library, Engine runtime, SceneComponents, Scripting/GameScriptSdk, ImGui Editor, lean Runtime player, samples and tests. Platform behind `IRendererAPI` / `IPhysicsWorld2D`; scene JSON → ECS systems by priority; Roslyn hot-reload; editor publish pipeline. Detail in `docs/architecture/README.md`.

---

## Technical Standards

### Global Standards

Located in `.maister/docs/standards/global/`

These standards apply across the entire codebase, regardless of domain.

#### Error Handling (`standards/global/error-handling.md`)
Clear user messages, fail-fast validation, typed exceptions, centralized boundary handling, graceful degradation, retry with backoff, resource cleanup.

#### Validation (`standards/global/validation.md`)
Server-side validation always, client-side for feedback only, early checks, field-specific errors, allowlists over blocklists, type/format checks, input sanitization, business-rule validation, consistent enforcement.

#### Conventions (`standards/global/conventions.md`)
Predictable structure, up-to-date docs, clean version control, environment variables for secrets, minimal dependencies, consistent reviews, testing before merge, feature flags, changelog updates, build what's needed.

#### Coding Style (`standards/global/coding-style.md`)
Naming consistency, automatic formatting, descriptive names, focused functions, uniform indentation, no dead code, no unnecessary backward compatibility, DRY.

#### Commenting (`standards/global/commenting.md`)
Let code speak, comment sparingly for non-obvious logic, no changelog-style change comments.

#### Minimal Implementation (`standards/global/minimal-implementation.md`)
Build only what's needed, clear purpose per method, delete exploration artifacts, no future stubs, no speculative abstractions, review before commit, unused code is debt.

#### Dependency Injection (`standards/global/dependency-injection.md`)
No static singletons, constructor injection (prefer primary constructors), DryIoc with explicit singleton lifetime, interface decision guide (when to abstract), no circular DI dependencies.

#### C# Project (`standards/global/csharp-project.md`)
Nullable reference types enabled, implicit usings, target `net10.0`, file-scoped namespaces, PascalCase files and root namespaces, interface files prefixed with `I`, SonarQube Cloud quality gate.

---

### Engine Standards

Located in `.maister/docs/standards/engine/`

These standards apply to engine core, platform backends, and GPU/resource lifetime.

#### Platform Abstraction (`standards/engine/platform-abstraction.md`)
Platform boundary via `IRendererAPI` / physics interfaces (no direct OpenGL in engine core), GL error checks after platform GL calls, automated engine review criteria for abstraction compliance.

#### Resources (`standards/engine/resources.md`)
Never call OpenGL in finalizers, disposal guards required, factory owns cached resources, serialize paths not loaded resources, Serilog logging stack, unsafe code only for interop projects.

---

### Editor Standards

Located in `.maister/docs/standards/editor/`

These standards apply to ImGui editor panels, drawers, and component/field editors.

#### UI Infrastructure (`standards/editor/ui-infrastructure.md`)
Always use Editor UI infrastructure (Drawers/Elements), `EditorUIConstants` and no magic numbers, specialized drop targets for assets, semantic colors for actions, panels as singletons with constructor injection.

#### Component Editors (`standards/editor/component-editors.md`)
Component editors via `IComponentEditor` / `ComponentEditorRegistry`, `IFieldEditor` only for script inspector fields.

---

### ECS Standards

Located in `.maister/docs/standards/ecs/`

These standards apply to components, systems, scripting tiers, and game assemblies.

#### Components (`standards/ecs/components.md`)
Components are data-only, physics pairing requirement for related physics components.

#### Systems (`standards/ecs/systems.md`)
Systems own logic and priorities, unsubscribe in `OnDetach`.

#### Scripting and Games (`standards/ecs/scripting-and-games.md`)
Scripting tiers separation, game assembly logic stays in `assets/scripts`, no ImGui in published games.

---

### Testing Standards

Located in `.maister/docs/standards/testing/`

These standards apply to all testing code (unit, integration, E2E).

#### Test Writing (`standards/testing/test-writing.md`)
Test behavior not implementation, clear names, mock external dependencies, fast unit tests, risk-based testing, balance coverage and velocity, critical-path focus, appropriate depth for edge cases.

#### Conventions (`standards/testing/conventions.md`)
xUnit / Shouldly / NSubstitute stack, test class file naming, CI-required test suites, graphics integration and headless CI constraints.

---

### Frontend Standards

*Not initialized for this project. If you need frontend standards, you can:*
- *Add them manually using the docs-manager skill*
- *Run `/standards-discover --scope=frontend` to auto-discover*

---

### Backend Standards

*Not initialized for this project. If you need backend standards, you can:*
- *Add them manually using the docs-manager skill*
- *Run `/standards-discover --scope=backend` to auto-discover*

---

## How to Use This Documentation

1. **Start Here**: Always read this INDEX.md first to understand what documentation exists
2. **Project Context**: Read relevant project documentation before starting work
3. **Standards**: This index only points to the standards — open and follow the specific standard files relevant to your task; don't rely on the index alone
4. **Keep Updated**: Update documentation when making significant changes
5. **Customize**: Adapt all documentation to your project's specific needs

## Updating Documentation

- Project documentation should be updated when goals, tech stack, or architecture changes
- Technical standards should be updated when team conventions evolve
- Always update INDEX.md when adding, removing, or significantly changing documentation

---

## Documentation Priority

When making development decisions, follow this priority order:

1. **Project documentation** in `.maister/docs/` (highest priority)
2. **Code patterns** visible in the codebase
3. **User's direct instructions**
4. **General best practices** (lowest priority)

**The documentation in `.maister/docs/` represents team decisions and should be followed unless the user explicitly overrides them.**

---

**Last Generated**: 2026-07-25
**Maintained by**: Documentation Manager skill
