# Pareto Readiness Analysis — GameEngine

**Analysis date:** 2026-08-16  
**Target use case:** Ship a small **2D** game and reach a **public alpha** (editor-driven content creation, standalone publish, external testers).

**Stack (verified):** .NET 10, Silk.NET, OpenGL 3.3+, ECS, Box2D, OpenAL, Roslyn scripting, DryIoc, ImGui editor, Assimp (3D import).

**Evidence base:** ~924 C# source files (non-test), 22 projects, **~812 automated tests** (86 `ECS.Tests` + 580 `Engine.Tests` + 109 `Editor.Tests` + ~37 `Engine.GraphicsTests`; **769 passing**, **6 failing** in `Engine.Tests` at analysis time), sample games (`Snake`, `FlappyBird`, `ArenaShooter`, `Arena3D`), cross-checked against [readiness analysis (2026-07)](readiness-analysis-2026-07.md) and [roadmap](guide/roadmap.md).

**Overall verdict:** ~**82–86% ready for 2D alpha** (up from ~68–72% in July). Major July blockers — **physics queries**, **entity hierarchy**, and **undo/redo** — are now implemented. The remaining ~15–18% clusters around **runtime UI**, **sprite draw-order control**, and **script field serialization**; 3D capability expanded significantly but remains orthogonal to 2D alpha.

---

## Delta from July 2026

| Gap (July rank) | July status | August status |
|-----------------|-------------|---------------|
| Physics raycasting & queries | 0% | **Done** — `IPhysicsQueries.Raycast` + `OverlapCircle` on `IPhysicsWorld2D`; exposed via `ScriptableEntity`; 22 Box2D query tests |
| Entity hierarchy | 0% | **Done** — `ParentComponent`, `Scene.SetParent`, world transforms, cascade destroy, prefab v2 subtrees, hierarchy panel tree, 15 hierarchy tests |
| Editor undo/redo | 0% | **Done** — `EditorHistory` command stack (depth 100); transform, add/remove component, destroy subtree; Ctrl+Z/Y; 33 editor history tests |
| Collider shapes beyond box | 25% | **Partial** — `CircleCollider2DComponent`, `EdgeCollider2DComponent` + editors/gizmos; no polygon collider |
| Publish smoke tests | Missing | **Done** — `GamePublisherSmokeTests` (Snake publish), `PublishedAssetValidatorTests`, exe size guard |
| Sprite sorting layers | 0% | **Still missing** — no `SortingOrder` on `SpriteRendererComponent` |
| Runtime UI | 0% | **Still missing** — spec docs exist; `Ui.ImGui` remains editor-only |
| 2D particles | 0% | **Still missing** |

**New since July (beyond alpha scope):** Assimp 3D model import, PBR materials, skeletal animation, IBL/skybox, shadow mapping, HDR bloom/tonemap/FXAA post-processing, wireframe viewport mode, `Arena3D` sample, ~37 GPU integration tests, 109 editor tests.

---

## 1. Major Subsystems — What Exists Today

| Subsystem | Implemented | Key evidence |
|-----------|-------------|--------------|
| **ECS core** | Entity/context, typed components, priority `SystemManager`, `[Register]` game systems | `ECS/Entity.cs`, `ECS/Systems/SystemManager.cs` |
| **2D rendering** | Batched quads (10K/batch), sprites, sub-texture atlasing, lines, framebuffers, GPU entity picking | `Engine/Renderer/Pipeline/Graphics2D.cs`, `SceneRenderPipeline.cs` |
| **3D rendering** | PBR meshes, skeletal animation, directional/point/ambient/sky lights, shadows, IBL, HDR post-process, wireframe | `Graphics3D.cs`, `SkeletalAnimationSystem.cs`, `PostProcessOrchestrator.cs` |
| **3D import** | Assimp FBX/OBJ → mesh assets, texture relocation, skinned meshes, editor batch import | `Editor/Features/Import/AssimpModelImporter.cs` |
| **Editor UI** | Dockspace, Hierarchy (tree), Properties, Content Browser, Console, Viewport (Select/Move/Scale/Rotate/Ruler), prefab drag-drop, publish UI, shortcuts panel | `Editor/Features/`, `Editor/Panels/` |
| **Scene management** | JSON save/load, Edit/Play modes, scene snapshot on play | `SceneSerializer.cs`, `SceneManager.cs` |
| **Entity hierarchy** | Parent/child, local/world transforms, cascade destroy, duplicate subtree, serialized `ParentComponent`, prefab v2 | `Engine/Scene/Scene.cs`, `SceneComponents/ParentComponent.cs` |
| **Prefabs** | Save, apply, instantiate, drag-drop, v1 backward compat + v2 subtree | `PrefabSerializer.cs`, `PrefabManager.cs` |
| **Physics 2D** | Box2D, rigid bodies, box/circle/edge colliders, triggers, contact queue, debug draw, fixed timestep | `PhysicsSimulationSystem.cs`, collider components |
| **Physics queries** | Raycast, overlap circle, script + test coverage | `IPhysicsQueries.cs`, `Box2DPhysicsWorld2D.cs` |
| **Scripting** | Roslyn compile, collectible ALC hot-reload, `ScriptableEntity`, `IGameSystem`, input/collision callbacks, hierarchy + physics query APIs | `ScriptEngine.cs`, `ScriptableEntity.cs` |
| **Audio** | OpenAL, WAV/OGG, spatial 3D, sources/listeners, PlayOneShot, basic EFX (reverb/echo/low-pass) | `OpenALAudioEngine.cs`, `AudioSystem.cs` |
| **Input** | Keyboard + mouse, event queue, layer propagation, screen/world pointer conversion | `SilkNetInputSystem.cs`, `Input/KeyCodes.cs` |
| **Serialization** | JSON scenes/prefabs, `ComponentSerializerRegistry`, vector converters, post-process settings | `Engine/Scene/Serializer/` |
| **Asset loading** | Factory + cache: textures (PNG/JPG/HDR), shaders, audio, meshes; path-string references | `TextureFactory`, `ShaderFactory`, `ModelFactory` |
| **Build/export** | `GamePublisher` → `dotnet publish` Runtime, script compile, asset copy, `game.config.json`, win/osx RIDs, publish validation | `Editor/Publisher/`, `GamePublisherSmokeTests.cs` |
| **Editor undo/redo** | Command stack: transform, add/remove component, destroy entity subtree | `Editor/Features/History/EditorHistory.cs` |
| **Runtime player** | Standalone `Runtime` project loads config + scene | `Runtime/Program.cs` |
| **DI / platform** | DryIoc, Silk.NET windowing, `IRendererAPI` abstraction | `EngineIoCContainer.cs`, `OpenGLRendererApi.cs` |
| **Testing** | ~812 tests across ECS, engine logic, editor workflows, GPU integration; 6 current failures | `tests/Engine.Tests/`, `tests/Editor.Tests/`, `tests/Engine.GraphicsTests/` |
| **Sample games** | Snake, FlappyBird (2D arcade), ArenaShooter (2D twin-stick + raycast), Arena3D (3D PBR showcase) | `games/` |

**Confirmed absent:** runtime UI, 2D sprite sort layers, particles, tilemaps, polygon collider, gamepad, multi-entity selection, asset GUID database, script field serialization, asset hot-reload (textures/audio), editor property-change undo, crash reporting.

**Doc vs code corrections:**

- README now accurately claims .NET 10, physics queries, and does **not** falsely claim hierarchy — hierarchy **is implemented** (July doc noted README claimed it when code lacked it).
- [roadmap](guide/roadmap.md) still references July baseline (~70%); M1 (raycast, circle collider, publish smoke) and M2 (undo/redo) and M4 (hierarchy) are **largely complete**; M3 (runtime UI) remains open.
- `LightingSystem.cs` is still a `NotImplementedException` stub — real lighting runs through render pipeline components, not this system.

---

## 2. Per-Subsystem Ratings

Completeness = feature coverage for small 2D alpha. Stability = bug/regression risk from code structure and test coverage. Blocking = impact on shipping alpha.

| Subsystem | Completeness | Stability | Blocking |
|-----------|-------------|-----------|----------|
| ECS core | 92% | High | Low |
| 2D rendering | 82% | Med-High | Low |
| 3D rendering | 58% | Med | Low (not alpha-critical for 2D) |
| 3D import pipeline | 55% | Med | Low (not alpha-critical for 2D) |
| Editor shell | 80% | Med-High | Low |
| Scene/prefabs | 82% | Med-High | Low |
| Entity hierarchy | 85% | Med-High | Low |
| Physics 2D (sim) | 78% | Med-High | Low |
| Physics queries | 88% | Med-High | Low |
| Collider shapes | 65% | Med-High | Med |
| Scripting + hot-reload | 85% | Med-High | Low |
| Audio | 70% | Med | Low |
| Input | 55% | High | Med |
| Runtime UI | 0% | N/A | **Critical** |
| Serialization | 78% | Med-High | Med |
| Asset pipeline | 45% | Med | Med |
| Build/publish | 85% | Med-High | Low |
| Editor undo/redo | 70% | Med-High | Low |
| Testing | 68% | Med | Med |
| Particles/VFX | 0% | N/A | Med |
| Sprite sort layers | 0% | N/A | **High** |

---

## 3. The Vital 20% — Ranked by Alpha Impact

These ~6 gaps now account for most remaining schedule risk (down from ~8 in July):

| Rank | Gap | Impact |
|------|-----|--------|
| **1** | **Runtime UI system** (Canvas, Button, Label, layout, text) | Blocks every menu, HUD, settings screen, game-over overlay — still the #1 alpha blocker |
| **2** | **Sprite sorting layers / render order** | Z-fighting and draw-order bugs in multi-layer 2D scenes; 3D has transparent sort, 2D sprites do not |
| **3** | **Script field serialization** | Inspector values on `ScriptableEntity` fields don't round-trip; forces code-only config |
| **4** | **2D particle system** | Polish gap; manual sprite animation remains 10× slower for juice |
| **5** | **Polygon collider** | Circle + edge + box cover most cases; slopes/complex platforms still need multi-fixture hacks |
| **6** | **Test suite health** | 6 failing `Engine.Tests` (5× ArenaShooter missing HUD entities in test setup, 1× 3D wireframe VP cache) — CI red erodes alpha confidence |

**Retired from vital list (implemented since July):** physics queries, entity hierarchy, undo/redo (core ops), circle collider, publish smoke tests.

---

## 4. Why These Have Outsized Impact

### 1. Runtime UI (largest — unchanged blocker)

`Ui.ImGui` is editor-only. Snake, FlappyBird, and ArenaShooter all work around this by rendering colored quads/sprites as "UI" (`SnakeSystem.SyncBanners`, `ArenaSystem.SyncHearts`). ArenaShooter proves raycast gameplay works but **doubles down on the quad-HUD pattern**. That does not scale to buttons, text input, or responsive layouts. Every alpha game with a main menu is still blocked unless you hand-roll quad UI per project.

### 2. Sprite sorting layers

`SpriteRendererComponent` has `Color`, `TexturePath`, `TilingFactor` — no sort key. Draw order follows entity iteration in `Graphics2D` batching. `TransparentDrawSort` exists for 3D transparency only. Alpha bug reports will cluster here as scenes grow beyond demo size.

### 3. Script field serialization

No `[SerializeField]`-equivalent or field serializer for script components. Game logic constants and tuning values must live in code or separate game components — friction for designers and alpha testers who expect inspector persistence.

### 4. Particles

No `Particle` references in codebase. Not a hard ship blocker for minimal alpha, but it is the main gap between "works" and "feels finished."

### 5. Polygon collider

`CircleCollider2D` and `EdgeCollider2D` landed since July. Remaining gap: arbitrary polygon shapes for one-way platforms and complex terrain without multi-box jitter.

### 6. Failing tests

At analysis time: 5 `ArenaShooterSystemTests` fail because `SyncHearts` calls `Context.GetByName` for HUD entities not created in test harness; 1 `Graphics3DWireframeTests` fails on view-projection uniform caching. Small fixes, but a red CI undermines "ready for external testers."

---

## 5. Prioritized Action List (Top 8)

| # | Action | Effort | Readiness gain |
|---|--------|--------|----------------|
| **1** | **Minimal runtime UI MVP** — `UICanvasComponent`, `UILabel`/`UIImage`/`UIButton`, rect anchors, screen-space render pass, click raycast; bitmap font or BMFont | **L** (3–4 wk) | +12% — unblocks menus/HUD for alpha |
| **2** | **Sprite sort key** — `SortingOrder` on `SpriteRendererComponent` + `SubTextureRendererComponent`, stable sort in `Graphics2D` batch | **S** (2–3 d) | +4% — fixes visible z-order bugs |
| **3** | **Script field serialization** — reflect public fields on `ScriptableEntity` subclasses, serialize in scene/prefab JSON, draw in `ScriptComponentEditor` | **M** (1–2 wk) | +5% — designer-friendly tuning |
| **4** | **Fix 6 failing Engine.Tests** — seed ArenaShooter HUD entities in test harness; align wireframe VP cache test with renderer | **S** (1–2 d) | +2% — green CI for alpha drops |
| **5** | **Basic 2D particles** — emitter component, pooled quads, color/size over lifetime (no sub-emitters) | **M** (2 wk) | +4% polish |
| **6** | **`PolygonCollider2D`** — component, Box2D `PolygonShape`, editor gizmo, serializer | **M** (1 wk) | +3% — complex terrain without box hacks |
| **7** | **Undo: property edits** — extend command stack to component field changes in inspector (beyond transform) | **M** (1 wk) | +3% editor UX |
| **8** | **Sample: menu + gameplay template** — `games/MenuTemplate` with Play/Quit buttons (blocked on #1, but scaffold scene structure now) | **S** (2–3 d after UI) | +2% onboarding |

**Suggested sequencing for alpha:** 4 → 2 (quick wins) → 1 → 3 → 7 → 5 → 6 → 8.

---

## 6. Summary Table

| Subsystem | Completeness % | Blocking Severity | Est. Effort | Priority Rank |
|-----------|---------------|-------------------|-------------|---------------|
| Runtime UI | 0% | **Critical** | L | **1** |
| Sprite sorting layers | 0% | **High** | S | **2** |
| Script field serialization | 0% | **Med** | M | **3** |
| Test suite health | 95% pass | **Med** | S | **4** |
| 2D particles | 0% | Med | M | **5** |
| Collider shapes (polygon) | 65% | Med | M | **6** |
| Editor undo (property edits) | 70% | Low | M | **7** |
| Alpha onboarding template | 40% | Med | S | **8** |
| Entity hierarchy | 85% | Low | — | ✅ Done |
| Physics queries | 88% | Low | — | ✅ Done |
| Editor undo/redo (core) | 70% | Low | — | ✅ Done |
| Build/publish pipeline | 85% | Low | — | ✅ Done |
| 2D rendering | 82% | Low | — | — |
| Scripting + hot-reload | 85% | Low | — | — |
| ECS core | 92% | Low | — | — |
| Physics simulation | 78% | Low | — | — |
| Scene serialization | 78% | Low | — | — |
| Editor panels/viewport | 80% | Low | — | — |
| Audio | 70% | Low | — | — |
| Input (keyboard/mouse) | 55% | Low | — | — |
| Asset pipeline (path-based) | 45% | Low | — | — |
| 3D rendering / import | 55% | Low (2D alpha) | — | Defer |
| Gamepad / input rebinding | 0% | Low | M | Defer |
| Asset GUID database | 0% | Low | L | Defer |

---

## 7. Hidden 20% — Small Gaps, Large DX Pain

| Hidden gap | Why it hurts disproportionately |
|------------|----------------------------------|
| **No asset hot-reload** | Script hot-reload works; textures/audio/meshes require restart — slows art iteration |
| **Path-string asset refs** | Renaming/moving files breaks scenes silently; no GUID `.meta` files |
| **No crash reporting** | Serilog to file/console only; alpha crashes are invisible remotely |
| **Single-entity selection** | `EditorSelection` tracks one entity; bulk edits painful in scenes beyond demo size |
| **No snap-to-grid** | Viewport has grid visual but no snap; level layout slower than competitors |
| **No multi-entity selection** | Cannot group-move or bulk-delete without scripting |
| **Quad-HUD workaround entrenched** | Three sample games prove the pattern works — also prove UI gap is being papered over, not solved |
| **`LightingSystem` stub** | `NotImplementedException` in `LightingSystem.cs` — not registered, but a footgun if wired |
| **6 red unit tests** | Small harness gaps, but external contributors see failing CI first |
| **Roadmap drift** | [roadmap](guide/roadmap.md) milestones M1/M2/M4 done but doc still lists them as future — erodes trust |

---

## Alpha Readiness Snapshot

```
Ship-ready today          Remaining blockers       Alpha polish
─────────────────         ──────────────────       ────────────
ECS + systems             Runtime UI               Particles
2D rendering              Sprite sort layers       Polygon collider
Hot-reload scripts        Script field ser.        Property undo
Physics + queries
Entity hierarchy
Undo/redo (core)
Publish + smoke tests
Editor core
3D (bonus, not alpha)
```

**Bottom line:** The July triad — **physics queries**, **entity hierarchy**, **undo/redo** — is largely delivered. For a **public alpha** where strangers build content in the editor and download standalone builds, the new critical path is **runtime UI** first, then **sprite sort order** and **script field serialization**. Fix the **6 failing tests** before any alpha tag. The engine can prototype and publish small 2D games today (four sample games prove it); the gap is no longer mechanics — it is **presentation and designer workflow**.

---

## Related docs

- [Roadmap](guide/roadmap.md) — phased plan (needs refresh against this analysis)
- [Readiness analysis (2026-07)](readiness-analysis-2026-07.md) — prior baseline
- [Architecture overview](architecture/README.md)
