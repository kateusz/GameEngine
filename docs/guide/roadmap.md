# GameEngine Roadmap

Phased plan to reach **public 2D alpha**, derived from [readiness analysis (2026-07)](../readiness-analysis-2026-07.md).

**Current baseline:** ~70% foundation — ECS, 2D rendering, scripting, editor, publish pipeline.  
**Alpha target:** External developers can build a small 2D game in the editor, publish a standalone build, and ship with menus/HUD without hand-rolled quad hacks.

### Shipped since roadmap draft (Aug 2026)

| Area | Status |
|------|--------|
| Physics raycast + `OverlapCircle` | Done — `ScriptableEntity` + `IPhysicsQueries` |
| `CircleCollider2DComponent` + editor | Done |
| `EdgeCollider2DComponent` + editor | Done |
| Partial undo/redo (`Ctrl+Z`) | Done — transforms, delete, component add/remove ([Shortcuts](editor/shortcuts.md)) |
| Parent/child hierarchy | Done — tree panel, `ParentComponent`, drag-reparent |
| 3D import + PBR + shadows + IBL + skeletal animation | Done — see [3D Rendering](concepts/3d-rendering.md), [Architecture](../architecture/README.md) |

Still open from early phases: `SortingOrder`, publish smoke test in CI, full undo coverage, Runtime UI MVP (M3).

---

## Milestones

| Milestone | Goal | Exit criteria |
|-----------|------|---------------|
| **M0 — Today** | Internal prototyping | Snake + FlappyBird run in editor and publish |
| **M1 — Alpha foundations** | Remove top mechanics blockers | Raycasting, sort layers, circle collider, publish smoke test |
| **M2 — Editor safety** | Safe content iteration | Undo/redo for transform, delete, component add/remove |
| **M3 — Runtime UI MVP** | Menus and HUD without quad hacks | Canvas + Label + Button + screen-space layout |
| **M4 — Scene composition** | Composite game objects | Parent/child hierarchy, tree panel, serialized relationships |
| **M5 — Public alpha** | External testers | Docs updated, sample "menu + gameplay" template, known-issues list |
| **M6 — Polish** | Juice and feel | Basic particles, edge collider, script field serialization |

Navmesh, gamepad, and asset GUID database are **post-alpha** unless scope changes. (3D mesh import + PBR are already in; see [3D Rendering](concepts/3d-rendering.md) and [Architecture](../architecture/README.md).)

---

## Phase 0 — Quick wins (Week 1)

**Goal:** Unlock standard 2D mechanics and CI confidence with minimal diff.

| Task | Effort | Owner hint | Done when | Status |
|------|--------|------------|-----------|--------|
| Physics raycast API on `IPhysicsWorld2D` | S | Engine | `Raycast(origin, dir, distance)` returns `RaycastHit2D?`; exposed to `ScriptableEntity` | **Done** |
| Circle overlap / point test (minimal) | S | Engine | `OverlapCircle` or `TestPoint` for ground checks | **Done** (`OverlapCircle`) |
| `SortingOrder` on sprite render components | S | Engine | Stable draw order in `Graphics2D`; editor field on inspector | Open |
| `CircleCollider2DComponent` + gizmo | S | Engine + Editor | Box2D `CircleShape`; serializes; shows in viewport | **Done** |
| Publish smoke test (Snake project) | S | Tests | CI asserts exe, `game.config.json`, `GameAssembly.dll`, startup scene exist | Open |

**Milestone:** M1

---

## Phase 1 — Editor safety net (Weeks 2–3)

**Goal:** Alpha testers can experiment without fear of unrecoverable mistakes.

| Task | Effort | Owner hint | Done when | Status |
|------|--------|------------|-----------|--------|
| Command pattern foundation | M | Editor | `IUndoCommand` + stack with depth limit | **Partial** |
| Undo: entity transform changes | M | Editor | Move/scale/rotate via viewport tools reversible | **Done** |
| Undo: delete entity | S | Editor | Ctrl+Z restores entity + components | **Done** |
| Undo: add/remove component | S | Editor | Component ops reversible | **Done** |
| Keyboard shortcuts: Ctrl+Z / Ctrl+Y | S | Editor | Wired in shortcut registry | **Done** |
| Undo: property inspector edits | S | Editor | Inspector field changes reversible | Open |
| Undo: create entity / duplicate | S | Editor | Create and duplicate reversible | Open |

**Milestone:** M2

---

## Phase 2 — Runtime UI MVP (Weeks 4–7)

**Goal:** Ship menus, HUD, and dialogs without per-game quad rendering.

| Task | Effort | Owner hint | Done when |
|------|--------|------------|-----------|
| Text rendering (BMFont or bitmap atlas) | M | Engine | Load font asset; generate quads for string |
| `UICanvasComponent` (screen-space) | M | SceneComponents | Render mode, reference resolution, sort order |
| `UIRectTransform` (anchors, pivot) | M | SceneComponents | Position/size relative to canvas |
| `UILabel`, `UIImage`, `UIButton` | M | SceneComponents | Color, texture, text, enabled state |
| UI render pass | M | Engine | Separate batch after world; respects canvas order |
| UI input (screen ray → element) | M | Engine | Click/hover on buttons; blocks game input when over UI |
| Component editors for UI | S | Editor | Inspector fields for rect + visuals |
| Sample: main menu scene | S | games/ | Template project with Play / Quit buttons |

**Milestone:** M3

**Defer to post-MVP UI:** scroll views, sliders, text input, world-space canvas, 9-slice, theming system.

---

## Phase 3 — Entity hierarchy (Weeks 8–10)

**Goal:** Composite objects and prefab trees work like users expect.

| Task | Effort | Owner hint | Done when | Status |
|------|--------|------------|-----------|--------|
| Parent/child on `Entity` | L | ECS | `SetParent`, `GetChildren`, cycle detection | **Done** |
| Local vs world transform | L | SceneComponents | `TransformComponent` propagates on parent change | **Done** |
| Hierarchy panel tree view | M | Editor | Indent + expand; drag-drop reparent | **Done** |
| Serialize parent refs in scenes/prefabs | M | Engine | Round-trip parent-child in JSON | **Done** |
| Prefab + hierarchy | M | Engine | Instantiate preserves tree | **Partial** (apply-to-entity drag; spawn-from-prefab not wired) |

**Milestone:** M4

---

## Phase 4 — Public alpha release (Week 11)

**Goal:** External developers can onboard without reading the whole codebase.

| Task | Effort | Owner hint | Done when |
|------|--------|------------|-----------|
| Fix README/doc drift (.NET 10, flat hierarchy) | S | Docs | README matches code |
| "Alpha template" project (menu + level + publish) | M | games/ | Clone → edit → publish in <30 min |
| Known issues / limitations page | S | Docs | Links from README |
| Alpha test checklist | S | Docs | Play mode, save, publish, script hot-reload |
| Optional: basic crash log path in Runtime | S | Runtime | Unhandled exception writes to `crash.log` |

**Milestone:** M5

---

## Phase 5 — Polish (Weeks 12–14, post-alpha or parallel)

**Goal:** Visual juice and remaining physics shapes.

| Task | Effort | Owner hint | Done when |
|------|--------|------------|-----------|
| Basic 2D particle emitter | M | Engine | Pooled quads; color/size over lifetime |
| `EdgeCollider2DComponent` | S | Engine | One-way platforms, slopes | **Done** |
| Script field serialization | M | Engine + Editor | Public fields on `ScriptableEntity` persist in scene |
| Asset hot-reload (textures) | M | Editor | File watcher reloads changed PNGs in play mode |

**Milestone:** M6

---

## Post-alpha backlog (prioritized)

| Item | Effort | Notes |
|------|--------|-------|
| Asset GUID + `.meta` files | L | Stops silent broken refs on rename |
| Gamepad support | M | Silk.NET gamepad API |
| Physics layers / collision matrix | M | Depends on raycast layer masks |
| `PolygonCollider2D` | M | Vertex editing in viewport |
| Multi-entity selection | M | Bulk transform + delete |
| Snap-to-grid | S | Viewport already draws grid |
| 2D lighting | L | Deferred or forward+ pass |
| Audio mixer groups | M | OpenAL EFX routing exists partially |
| Tilemap system | L | Genre-specific |
| Visual scripting | XL | C# scripting sufficient for alpha |

---

## Timeline overview

```
Week:  1    2    3    4    5    6    7    8    9   10   11   12+
       ├────┤    ├─────────┤    ├──────────────────┤    ├────┤
       P0        P1              P2 (Runtime UI)         P3   P4
       M1        M2              M3                       M4   M5
```

**Critical path:** P0 → P2 (UI) → P3 (hierarchy) → P4 (alpha release).  
P1 (undo) can run in parallel with late P0 / early P2 if staffed.

---

## Success metrics for public alpha

| Metric | Target |
|--------|--------|
| New user: project → playable scene | < 1 hour with template |
| Publish standalone build | Works on win-x64 without manual steps |
| Script hot-reload | Edit script → see change in play mode |
| Menu-driven game | Possible without custom quad UI code |
| Test suite | 523 unit tests green; publish smoke test in CI |
| Known crash on publish | 0 in Snake/FlappyBird template publish |

---

## References

- [Readiness analysis (2026-07)](../readiness-analysis-2026-07.md)
- [Architecture overview](../architecture/README.md)
- [Game publishing](../architecture/README.md#tools--publishing) (via README)
