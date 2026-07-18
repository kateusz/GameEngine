# Pareto Readiness Analysis — GameEngine

**Analysis date:** 2026-07-07  
**Target use case:** Ship a small **2D** game and reach a **public alpha** (editor-driven content creation, standalone publish, external testers).

**Stack (verified):** .NET 10, Silk.NET, OpenGL 3.3+, ECS, Box2D, OpenAL, Roslyn scripting, DryIoc, ImGui editor.

**Evidence base:** ~313 C# source files, 19 projects, **520 passing unit tests** (434 `Engine.Tests` + 86 `ECS.Tests`), sample games (`Snake`, `FlappyBird`), cross-checked against [pareto-analysis-missing-features.md](pareto-analysis-missing-features.md).

**Overall verdict:** ~**68–72% ready for 2D alpha**. Strong ECS, 2D rendering, scripting, editor shell, and publish pipeline. The remaining ~30% clusters around **runtime UI**, **physics queries**, **entity hierarchy**, and **editor workflow polish** — these dominate alpha risk, not 3D mesh import or navmesh.

---

## 1. Major Subsystems — What Exists Today

| Subsystem | Implemented | Key evidence |
|-----------|-------------|--------------|
| **ECS core** | Entity/context, typed components, priority `SystemManager`, `[Register]` game systems | `ECS/Entity.cs`, `ECS/Systems/SystemManager.cs` |
| **2D rendering** | Batched quads (10K/batch), sprites, sub-texture atlasing, lines, framebuffers, GPU entity picking | `Engine/Renderer/Graphics2D.cs`, `SceneRenderPipeline.cs` |
| **3D rendering** | Lit unit cubes only; ambient + directional lights | `ModelRendererComponent`, `Graphics3D.cs` |
| **Editor UI** | Dockspace, Hierarchy, Properties, Content Browser, Console, Viewport (Select/Move/Scale/Rotate/Ruler), prefab drag-drop, publish UI | `Editor/Features/`, `Editor/Panels/` |
| **Scene management** | JSON save/load, Edit/Play modes, scene snapshot on play | `SceneSerializer.cs`, `SceneManager.cs` |
| **Prefabs** | Save, apply, instantiate, drag-drop | `PrefabSerializer.cs`, `PrefabManager.cs` |
| **Physics 2D** | Box2D, rigid bodies, box colliders, triggers, contact queue, debug draw, fixed timestep | `PhysicsSimulationSystem.cs`, `BoxCollider2DComponent` |
| **Scripting** | Roslyn compile, collectible ALC hot-reload, `ScriptableEntity`, `IGameSystem`, input callbacks, collision callbacks | `ScriptEngine.cs`, `ScriptableEntity.cs` |
| **Audio** | OpenAL, WAV/OGG, spatial 3D, sources/listeners, PlayOneShot, basic EFX (reverb/echo/low-pass) | `OpenALAudioEngine.cs`, `AudioSystem.cs` |
| **Input** | Keyboard + mouse, event queue, layer propagation | `SilkNetInputSystem.cs`, `Input/KeyCodes.cs` |
| **Serialization** | JSON scenes/prefabs, `ComponentSerializerRegistry`, vector converters | `Engine/Scene/Serializer/` |
| **Asset loading** | Factory + cache: textures (PNG/JPG), shaders, audio; path-string references | `TextureFactory`, `ShaderFactory`, `AudioClipFactory` |
| **Build/export** | `GamePublisher` → `dotnet publish` Runtime, script compile, asset copy, `game.config.json`, win/osx RIDs | `Editor/Publisher/` |
| **Runtime player** | Standalone `Runtime` project loads config + scene | `Runtime/Program.cs` |
| **DI / platform** | DryIoc, Silk.NET windowing, `IRendererAPI` abstraction | `EngineIoCContainer.cs`, `OpenGLRendererApi.cs` |
| **Testing** | 520 unit tests on ECS + engine logic; benchmarks | `tests/Engine.Tests/`, `tests/ECS.Tests/` |
| **Sample games** | Snake (grid + audio + `IGameSystem`), FlappyBird | `games/Snake/`, `games/FlappyBird/` |

**Confirmed absent:** runtime UI, physics raycasting/queries, entity parent/child hierarchy, particles, tilemaps, extra collider shapes, gamepad, undo/redo, asset GUID database, 3D mesh import, editor automated tests, crash reporting.

**Doc vs code corrections:**

- README claims "hierarchical entity management" — entities are **flat** (`ECS/Entity.cs` has no Parent/Children).
- [pareto-analysis-missing-features.md](pareto-analysis-missing-features.md) lists collision callbacks and rotation tool as missing — both **exist** in code.

---

## 2. Per-Subsystem Ratings

Completeness = feature coverage for small 2D alpha. Stability = bug/regression risk from code structure and test coverage. Blocking = impact on shipping alpha.

| Subsystem | Completeness | Stability | Blocking |
|-----------|-------------|-----------|----------|
| ECS core | 90% | High | Low |
| 2D rendering | 82% | Med-High | Low |
| 3D rendering | 15% | Med | Low (not alpha-critical for 2D) |
| Editor shell | 75% | Med | Med |
| Scene/prefabs | 70% | Med-High | Med |
| Physics 2D (sim) | 72% | Med-High | Med |
| Physics queries | 0% | N/A | **High** |
| Scripting + hot-reload | 85% | Med-High | Low |
| Audio | 70% | Med | Low |
| Input | 55% | High | Med |
| Runtime UI | 0% | N/A | **Critical** |
| Serialization | 75% | Med-High | Med |
| Asset pipeline | 45% | Med | Med |
| Build/publish | 72% | Med | Med |
| Testing | 55% | Med | Med |
| Particles/VFX | 0% | N/A | Med |
| Entity hierarchy | 0% | N/A | **High** |

---

## 3. The Vital 20% — Ranked by Alpha Impact

These ~8 gaps account for most remaining schedule risk and blocker surface area:

| Rank | Gap | Impact |
|------|-----|--------|
| **1** | **Runtime UI system** (Canvas, Button, Label, layout, text) | Blocks every menu, HUD, settings screen, game-over overlay |
| **2** | **Physics raycasting & shape queries** | Blocks shooting, ground checks, mouse-pick gameplay, AI line-of-sight |
| **3** | **Entity hierarchy (parent/child)** | Blocks weapons-on-character, vehicles, grouped level props, UI nesting |
| **4** | **Editor undo/redo** | Blocks safe iteration for external alpha testers and developers |
| **5** | **Additional collider shapes** (circle, edge, polygon) | Forces box hacks; poor feel for top-down/platformer alpha |
| **6** | **2D particle system** | Polish gap; manual sprite animation is 10× slower for juice |
| **7** | **Sprite sorting layers / render order** | Z-fighting and draw-order bugs in multi-layer 2D scenes |
| **8** | **Publish pipeline test coverage + asset validation** | Silent broken builds for alpha distribution |

---

## 4. Why These Have Outsized Impact

### 1. Runtime UI (largest)

`Ui.ImGui` is editor-only. Snake/FlappyBird work around this by rendering colored quads/sprites as "UI" (`SnakeSystem.SyncBanners`). That pattern does not scale to buttons, text input, or responsive layouts. **Every alpha game with a main menu is blocked** unless you hand-roll quad UI per project.

### 2. Physics raycasting

`IPhysicsWorld2D` exposes only `Step`, `CreateBody`, `DestroyBody`, `SetContactListener` — no query API. Box2D supports `World.RayCast()` but it is not wrapped. Contact events exist for collisions; **queries are a separate capability** used constantly for ground checks and click-to-shoot.

### 3. Entity hierarchy

Flat entity list + manual transform math. Snake sidesteps this (grid cells are independent entities). Any game with attached parts (turret + barrel, player + weapon sprite) requires per-frame manual sync — error-prone and blocks prefab reuse patterns users expect from Unity/Godot.

### 4. Undo/redo

Zero matches for undo/redo in `.cs` files. For alpha, accidental deletes, bad drags, and property typos are **unrecoverable**. This disproportionately hurts external testers who explore aggressively.

### 5. Collider shapes

Only `BoxCollider2DComponent`. Circular enemies, slopes, one-way platforms need multi-box approximations — jittery collisions and more fixtures (Box2D perf + tuning pain).

### 6. Particles

No `Particle` references in codebase. Not a hard ship blocker for minimal alpha, but it is the main gap between "works" and "feels finished."

### 7. Sorting layers

No `SortingLayer` / render-order field. Draw order follows entity iteration — fragile as scenes grow. Alpha bug reports will cluster here.

### 8. Publish validation gaps

`GamePublisher` is real and multi-step (`ValidateProject` → build → compile scripts → copy assets → deploy), but **no automated tests** cover it. README notes a non-fatal warning for executables under 100 KB — alpha builds can ship broken without CI catching it.

---

## 5. Prioritized Action List (Top 8)

| # | Action | Effort | Readiness gain |
|---|--------|--------|----------------|
| **1** | **Minimal runtime UI MVP** — `UICanvasComponent`, `UILabel`/`UIImage`/`UIButton`, rect anchors, screen-space render pass, click raycast; bitmap font or BMFont | **L** (3–4 wk) | +15% — unblocks menus/HUD for alpha |
| **2** | **Physics queries** — wrap Box2D `RayCast`, `RaycastHit2D`, expose on `IPhysicsWorld2D` + `ScriptableEntity`; add circle overlap | **S** (1 wk) | +8% — unlocks standard 2D mechanics |
| **3** | **Entity hierarchy** — parent/child on `Entity`, local/world transforms, tree in `SceneHierarchyPanel`, serialize relationships | **L** (2–3 wk) | +10% — composite objects, cleaner prefabs |
| **4** | **Undo/redo command stack** — transform, add/remove component, delete entity (start with 3 command types) | **M** (1–2 wk) | +5% editor UX; cuts alpha support load |
| **5** | **`CircleCollider2D`** — component, Box2D `CircleShape`, editor gizmo, serializer | **S** (3–5 d) | +4% — removes worst physics workaround |
| **6** | **Sprite sort key** — `SortingOrder` on render components, stable sort in `Graphics2D` batch | **S** (2–3 d) | +3% — fixes visible z-order bugs |
| **7** | **Publish smoke tests** — one integration test: publish Snake project, assert exe + `game.config.json` + `GameAssembly.dll` + startup scene | **S** (2–3 d) | +3% — confidence for alpha drops |
| **8** | **Basic 2D particles** — emitter component, pooled quads, color/size over lifetime (no sub-emitters) | **M** (2 wk) | +5% polish |

**Suggested sequencing for alpha:** 2 → 6 → 7 (quick wins) → 1 → 4 → 3 → 5 → 8.

---

## 6. Summary Table

| Subsystem | Completeness % | Blocking Severity | Est. Effort | Priority Rank |
|-----------|---------------|-------------------|-------------|---------------|
| Runtime UI | 0% | **High** | L | **1** |
| Physics queries | 0% | **High** | S | **2** |
| Entity hierarchy | 0% | **High** | L | **3** |
| Editor undo/redo | 0% | **Med** | M | **4** |
| Collider shapes (beyond box) | 25% | **Med** | S | **5** |
| 2D particles | 0% | Med | M | **6** |
| Sprite sorting layers | 0% | Med | S | **7** |
| Build/publish pipeline | 72% | Med | S | **8** |
| 2D rendering | 82% | Low | — | — |
| Scripting + hot-reload | 85% | Low | — | — |
| ECS core | 90% | Low | — | — |
| Physics simulation | 72% | Low | — | — |
| Scene serialization | 75% | Low | — | — |
| Editor panels/viewport | 75% | Low | — | — |
| Audio | 70% | Low | — | — |
| Input (keyboard/mouse) | 55% | Low | — | — |
| Asset pipeline (path-based) | 45% | Low | — | — |
| 3D rendering / mesh import | 15% | Low (2D alpha) | XL | Defer |
| Unit test coverage (engine) | 55% | Med | M | **9** |
| Gamepad / input rebinding | 0% | Low | M | Defer |

---

## 7. Hidden 20% — Small Gaps, Large DX Pain

These are easy to underestimate but will dominate alpha feedback and iteration speed:

| Hidden gap | Why it hurts disproportionately |
|------------|-----------------------------------|
| **No undo/redo** | One misclick = lost work; alpha testers will hit this immediately |
| **No asset hot-reload** | Script hot-reload works (`GameScriptWorkspace`); textures/audio require restart — slows art iteration |
| **Path-string asset refs** | Renaming/moving files breaks scenes silently; no GUID `.meta` files |
| **No crash reporting** | Serilog to file/console only; alpha crashes are invisible remotely |
| **No script field serialization** | Inspector values don't round-trip on `ScriptableEntity` fields |
| **No multi-entity selection** | Bulk edits painful in scenes beyond demo size |
| **No snap-to-grid** | Level layout slower than competitors; viewport has grid visual but no snap |
| **README/doc drift** | Claims .NET 9, hierarchy, "17 panels" — erodes trust for open-alpha contributors |
| **Zero editor/GPU tests** | Regressions in viewport picking, play mode, publish won't be caught by current 520 tests |
| **`LightingSystem` stub** | `NotImplementedException` in `LightingSystem.cs` — not registered, but a footgun if wired |
| **Sample games as UI workaround** | Snake's banner quads prove the engine works but also prove UI gap is being papered over |

---

## Alpha Readiness Snapshot

```
Ship-ready today          Alpha blockers           Alpha polish
─────────────────         ──────────────           ────────────
ECS + systems             Runtime UI               Undo/redo
2D rendering              Physics queries          Particles
Hot-reload scripts        Entity hierarchy         Sort layers
Publish pipeline
Editor core
```

**Bottom line:** You can prototype and publish small 2D games today (Snake/FlappyBird prove it). For a **public alpha** where strangers build content in the editor and download standalone builds, invest first in **runtime UI**, **physics queries**, and **hierarchy** — that triad is ~80% of functional risk. Pair with **undo/redo** and **publish smoke tests** for the hidden 20% that determines whether alpha feels professional or fragile.

---

## Related docs

- [Roadmap](guide/roadmap.md) — phased implementation plan derived from this analysis
- [Pareto missing features (feature-level detail)](pareto-analysis-missing-features.md)
- [Architecture overview](architecture/README.md)
