# Evaluation Criteria

Apply all eight groups. Reference mature engine patterns where useful; state trade-offs.

## 1. Responsibilities and Cohesion

- God classes, misplaced responsibilities, unnecessary orchestration
- Low-cohesion modules mixing unrelated concerns
- Responsibilities that belong in another subsystem

## 2. Dependencies and Coupling

- Tight, hidden, circular, and temporal coupling
- Cross-boundary coupling: engine ↔ editor, runtime ↔ tooling, scene ↔ rendering/physics/ECS/scripting/assets
- Dependency direction: lower-level modules must not depend on higher-level systems
- Engine/editor separation: runtime independent of editor; editor isolated from runtime
- Runtime vs authoring: inspector metadata, serialization metadata, editor helpers, debug tooling, scene editing kept out of runtime
- Rate **coupling** 1–10 (see [rating scales](SKILL.md#rating-scales))

## 3. Abstractions and Public API

- Abstraction leaks, implementation leaks, wrong boundaries
- Missing abstractions vs unnecessary abstractions (YAGNI)
- Public API as a game developer would use it: intuitive, expressive, minimal, hard to misuse, internally consistent

## 4. ECS and Data Model

- OO assumptions fighting ECS (entity/component ownership, hidden mutable state)
- Systems doing work that belongs elsewhere
- Direct component manipulation bypassing intended flow

## 5. Scale and Performance Architecture

Ignore micro-optimizations. Evaluate architectural performance:

- Ownership, allocation patterns, cache friendliness
- Unnecessary indirection, synchronization, lifetime management
- Scale assumptions: hundreds of scenes, tens of thousands of entities, thousands of prefabs, multiple engine projects
- Future bottlenecks at target scale

## 6. Extensibility and Future-Proofing

Difficulty of adding: prefabs, additive scenes, scene streaming, serialization, save/load, undo/redo, hot reload, multiplayer, plugins, scripting, editor extensions.

Identify decisions that become technical debt under continued development.

## 7. Serialization and Testability

- Scene/prefab serialization, save/load, versioning, backward compatibility
- Unit-test friction: tight coupling, hard-to-mock deps, global state, poor separation

## 8. SOLID and Design Patterns

Evaluate SOLID with concrete compliance/violation examples.

Patterns: Factory, Strategy, Observer, Composite, Visitor, Command, Repository, Singleton, Service Locator, Dependency Injection — note correct use, missing use, over-engineering, and anti-patterns (especially Singleton and Service Locator).

Maintainability signals: duplicated logic, implicit contracts, hidden assumptions, fragile architecture, unnecessary complexity.
