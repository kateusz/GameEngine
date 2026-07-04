# Scripting Engine — Architecture Review

**Scope:** `Engine/Scripting/`, `Scripting/`, `ScriptableEntity` lifecycle, `NativeScriptComponent`, `ScriptUpdateSystem`, `SceneContactListener` script dispatch, game assembly compilation/loading, and editor/runtime integration.

**Date:** July 2026

---

## Issue 1 — Dual Model Is Valid; Implementation and Contracts Are Lopsided

### Severity

**Medium** (revised from Critical)

### Location

`ScriptableEntity` / `NativeScriptComponent` vs `IGameComponent` / `IGameSystem` / `[Register]`; `ScriptEngine` vs `GameComponentFactory` / `TicTacToeSystem`

### Problem

The engine exposes three tiers that mirror Unity's mature split:

| Unity | This engine | Role |
|-------|-------------|------|
| `MonoBehaviour` | `ScriptableEntity` + `NativeScriptComponent` | Per-entity behavior, lifecycle, glue |
| Custom `Component` | `IGameComponent` + `[SerializableComponent]` | Serializable data on entities |
| `ISystem` / `SystemBase` | `IGameSystem` + `[Register]` | Batch logic over queries |

TicTacToe demonstrates intentional composition: `GameControllerScript` handles input on one entity, `BoardComponent` / `CellComponent` hold state, `TicTacToeSystem` runs rules and visuals for the whole board. **Script on entity + system over set is a valid model.**

The architectural gap is not "two paradigms exist." It is that **both tiers are half-integrated**, not two deliberate layers of one product:

1. **Asymmetric runtime services** — Input and physics dispatch only reach `ScriptableEntity` (`ScriptEngine.ProcessEvent`, `SceneContactListener` → `ScriptRuntimeStore`). `IGameSystem` has DI and `IContext` queries but no first-class input or contact API. TicTacToe works around this by using a script as an input shim that writes into `BoardComponent.PendingCellIndex`.

2. **`ScriptableEntity` lacks MonoBehaviour parity** — `NativeScriptComponent` persists only `ScriptTypeName`. Runtime instances live in static `ScriptRuntimeStore`, not on the entity. Field serialization and inspector editing for script fields were removed while docs still describe them.

3. **No authoring contract** — Editor presents `ScriptComponentEditor` and `GameComponentEditor` as peers without guidance on when to use each. Authors cannot tell whether to put logic in a script, a game component, or a system.

4. **Two integration spines** — `ScriptableEntity` flows through `ScriptEngine` lifecycle and event fan-out. `IGameSystem` flows through `GameAssemblyContainerRegistration` and `SceneManager.RegisterGameSystems()`. They share a `GameAssembly` but not event or lifecycle infrastructure.

### Why it matters

Unity succeeds with scripts + components + systems because each tier has clear responsibilities and comparable access to engine services. Here, authors are pushed toward hybrid hacks (script forwards input into component fields) not because the model is wrong, but because the system tier is incomplete.

Choosing one paradigm and deleting the other would discard a useful per-entity glue layer that Unity keeps permanently.

### Long-term consequences

Without a documented contract and symmetric services:

- Every game reinvents input routing into component fields.
- Contributors debate "which path is canonical" instead of "which tier fits this concern."
- Editor investment stays split with no clear finish line.
- Physics and input middleware cannot ship as reusable systems.

### Recommended redesign

**Keep both paths.** Do not merge into a single abstraction or delete `ScriptableEntity`.

1. **Publish the authoring contract** (in getting-started / architecture docs):
   - `IGameComponent` — serializable state on entities.
   - `ScriptableEntity` — per-entity glue: input wiring, one-off behavior, bridging to components.
   - `IGameSystem` — batch logic over `IContext` views.

2. **Give `IGameSystem` first-class engine access** — Inject `IInputSystem` (or equivalent) into systems that need input. Expose physics contacts to systems via `IContext` or a small handler interface consumed by systems — not only via `ScriptableEntity` virtuals.

3. **Complete `ScriptableEntity` tier parity** — Per-scene runtime store (see Issue 2). Either restore field serialization for inspector editing, or explicitly document: "put data in `IGameComponent`; scripts are glue only."

4. **Use TicTacToe as the reference layout** — Script for entity-local input, components for state, system for rules — but evolve it so input can live in the system when appropriate.

### Ponytail challenge

`yagni:` unified `IScriptingFacade` or migration framework — cut; two tiers are intentional. `yagni:` per-entity event subscription registry — cut; inject `IInputSystem` into systems instead. `delete:` "pick one paradigm" recommendation from original review. **Issue survives as Medium with lean redesign.**

### Expected benefits

Unity-aligned mental model for authors. Systems no longer require a `ScriptableEntity` shim for input. Clear editor guidance without removing per-entity scripting. Both paths remain compile-time unified in `GameAssembly`.

---

## Issue 2 — `ScriptRuntimeStore` Is Global and Scene-Unsafe

### Severity

**High**

### Location

`Engine/Scripting/ScriptRuntimeStore.cs`, consumers in `ScriptEngine`, `SceneContactListener`

### Problem

Runtime script instances live in a **static** `Dictionary<int, ScriptableEntity>` keyed by entity ID. Physics uses the opposite pattern — `PhysicsRuntimeBodyStore` is **per-scene**, created in `SystemManagerFactory`.

`DestroyEntity` does not remove entries. `SceneTests` explicitly allows **ID reuse** after destroy. `OnRuntimeStop` is the only cleanup path.

### Why it matters

Stale script instances can attach to wrong entities. Memory leaks when entities are destroyed at runtime. Multi-scene / additive loading cannot isolate script state.

### Long-term consequences

Subtle bugs in games that spawn/destroy entities. Scene streaming and prefab instantiation become unsafe.

### Recommended redesign

Replace the static store with a **per-scene** `ScriptRuntimeStore` (instance class), created alongside `PhysicsRuntimeBodyStore` in `SystemManagerFactory`, injected into `ScriptUpdateSystem` and `SceneContactListener`. Remove entries on entity destroy and on `OnShutdown`.

### Ponytail challenge

Mirrors existing `PhysicsRuntimeBodyStore` — no new abstraction. **Proposal lean. Keep.**

### Expected benefits

Correct lifetime semantics, testable isolation, consistent with physics stores.

---

## Issue 3 — `ScriptEngine` Is a God Object; `IScriptEngine` Mixes Authoring and Runtime

### Severity

**High**

### Location

`Engine/Scripting/ScriptEngine.cs`, `Engine/Scripting/IScriptEngine.cs`

### Problem

`ScriptEngine` owns compilation orchestration, hot-reload, per-frame ECS iteration, input broadcast, editor file CRUD, debug symbols, and `IsEditorProcess()` path heuristics. `IScriptEngine` exposes all of this to both Editor and Runtime.

### Why it matters

Violates SRP and editor/runtime separation. Runtime depends on the fattest possible interface.

### Long-term consequences

Hard to test compilation without scene context. Editor UX changes risk breaking runtime play.

### Recommended redesign

1. **Keep** `GameAssemblyCompiler` for compile.
2. **Move** editor-only methods to `Editor.Features.Scripting` using `GameAssemblyCompiler` directly.
3. **Leave** on a slim runtime interface: load, lifecycle delegation, `ProcessEvent`, type registry, `CreateScriptInstance`.
4. **Delete** `IsEditorProcess()` — callers pass output DLL path.

### Ponytail challenge

`yagni:` three-interface split (`IScriptRuntime`, `IGameScriptWorkspace`, `ICompilationService`). Cut to two concrete classes, no new hierarchy. **Issue survives, lean fix.**

### Expected benefits

Smaller runtime surface. Editor changes stop touching runtime contract.

---

## Issue 4 — ECS Integration Is Inverted: Engine Iterates, System Is a Pass-Through

### Severity

**Medium**

### Location

`ScriptUpdateSystem`, `ScriptEngine.OnUpdate`

### Problem

`ScriptUpdateSystem` only forwards to `scriptEngine.OnUpdate()`. The engine queries `_sceneContext.ActiveScene.Entities` itself. Every other system receives `IContext` and queries its own archetype.

### Why it matters

`ScriptEngine` is a singleton reaching into whichever scene is active. Scripting bypasses normal system patterns.

### Recommended redesign

Move entity iteration into `ScriptUpdateSystem` using `IContext`. `ScriptEngine` becomes factory + type registry only.

### Ponytail challenge

`shrink:` ~40 lines move to `ScriptUpdateSystem`. No `IScriptInstanceManager`. **Issue survives.**

### Expected benefits

Consistent ECS patterns, scene-scoped execution.

---

## Issue 5 — Editor/Runtime Coupling in Compilation References

### Severity

**Medium**

### Location

`Engine/Scripting/ScriptCompilationReferences.cs`, `ScriptEngine.IsEditorProcess()`

### Problem

Compilation metadata includes **Editor** assemblies. Reference resolution probes hardcoded `net10.0` / `Debug` paths. `GameScriptReferences.props` exists but isn't the single source of truth.

### Recommended redesign

`delete:` Editor from compile references. Drive reference list from `GameScriptSdk` manifest. Pass paths into `GameAssemblyCompiler.TryCompile` from caller.

### Ponytail challenge

`delete:` Editor branch + AppDomain scan. **Issue survives.**

### Expected benefits

Deterministic compilation, no editor leakage into game assemblies.

---

## Issue 6 — Hot Reload Is Incomplete and Leaks Assemblies

### Severity

**Medium**

### Location

`ScriptEngine.CheckForScriptChanges`, `TryLoadCompiledAssembly`, editor `GetNextEditorBuildPath`

### Problem

Automatic reload does not tear down old instances. New files may be missed. Editor generates `GameAssembly_{Guid}.dll` without unload. `CompileScript` ignores its parameters.

### Recommended redesign

On recompile: destroy all instances in scene store, reload, re-init. Stable DLL path in editor or collectible `AssemblyLoadContext`. Do not add incremental compilation yet.

### Ponytail challenge

`yagni:` incremental Roslyn, `FileSystemWatcher`. **Issue survives.**

### Expected benefits

Trustworthy hot reload, bounded memory.

---

## Issue 7 — Physics and Input Only Serve `ScriptableEntity` (Subsystem of Issue 1)

### Severity

**Medium**

### Location

`SceneContactListener`, `ScriptEngine.ProcessEvent`

### Problem

Collision/trigger and input only dispatch to `ScriptableEntity`. ECS systems must poll or use component fields as a mailbox.

### Recommended redesign

Addressed by Issue 1 redesign: inject `IInputSystem` into systems; expose contacts to systems via `IContext` or handler — not a second parallel event framework.

### Ponytail challenge

Merged with Issue 1. **No separate fix.**

### Expected benefits

Systems become viable for input-heavy and physics-heavy logic without script shims.

---

## Issue 8 — `GameComponentDiscovery` Regex Duplicates Compiler Output

### Severity

**Low**

### Location

`Engine/Scripting/GameComponentDiscovery.cs`

### Problem

Regex over source files duplicates assembly type scan. False positives possible.

### Recommended redesign

`delete:` regex path. Discover only from loaded `GameAssembly` types implementing `IGameComponent`.

### Ponytail challenge

`delete:` 25-line regex class. **Issue survives.**

### Expected benefits

One source of truth.

---

## Issue 9 — Documentation Describes Architecture That No Longer Exists

### Severity

**Low**

### Location

`docs/architecture/scripting-lifecycle.md`

### Problem

Docs reference `DynamicScripts`, reflection field exposure, `SetSceneContext`, transform helpers — removed from current code.

### Recommended redesign

Update doc to describe dual-tier model, authoring contract (Issue 1), and current serialization limits.

### Ponytail challenge

Doc-only. **Issue survives as Low.**

### Expected benefits

Accurate onboarding.

---

# Final Summary

| Rating | Score | Notes |
|--------|-------|-------|
| **Overall Architecture** | **6/10** | Valid Unity-like tiering undermined by lopsided integration |
| **Scalability** | **4/10** | O(entities × scripts) input broadcast; full recompile; assembly leak |
| **Maintainability** | **5/10** | God object, stale docs, missing author contract |
| **ECS Compatibility** | **7/10** | `IGameComponent` / `IGameSystem` path is sound; script path fights it |
| **Editor/Runtime Separation** | **4/10** | Editor APIs on runtime interface; Editor in compile refs |
| **Runtime Decoupling** | **5/10** | `GameAssemblyCompiler` clean; `ScriptEngine` not |
| **Public API** | **6/10** | Tiering is intuitive once documented; implementation gaps confuse |
| **Extensibility** | **7/10** | `[Register]` + `[SerializableComponent]` extensibility is strong |

**Coupling rating: 7/10**

## Top Five Architectural Improvements

1. **Document and enforce the three-tier contract** (Issue 1) — `IGameComponent` for data, `ScriptableEntity` for per-entity glue, `IGameSystem` for batch logic; give systems input/physics access.

2. **Replace static `ScriptRuntimeStore` with per-scene store** (Issue 2).

3. **Strip editor authoring from `IScriptEngine`** (Issue 3).

4. **Move ECS iteration from `ScriptEngine` into `ScriptUpdateSystem`** (Issue 4).

5. **Fix hot reload teardown + stable assembly loading** (Issue 6).

## What's Already Good

- `GameAssemblyCompiler` — focused Roslyn pipeline
- `[SerializableComponent]` + `RegisterFromAssembly`
- `[Register]` + `GameAssemblyContainerRegistration` for game systems
- `GameScriptSdk` bootstrap
- Per-script exception isolation
- `ScriptableEntity` → `IComponentAccessor` indirection
