# Development Roadmap

Aligned with user goals and `docs/guide/roadmap.md` (2D public alpha milestones). Raycast / overlap world queries already exist in code (e.g. ArenaShooter); treat remaining M1 items (sort layers, circle collider, publish smoke) as still open unless verified done.

## Current State
- **Version**: Pre-alpha / internal prototyping (M0 baseline)
- **Key Features**: ECS, OpenGL 2D/3D rendering, Box2D physics, Roslyn hot-reload scripting, ImGui editor, publish → Runtime, sample games
- **Recent focus**: Platform abstraction, scripting SDK, sample gameplay (Snake, FlappyBird, ArenaShooter)

## Planned Enhancements (Next 3-6 Months)

### High Priority
- [ ] **2D animation workflow** — sprite/clip authoring and playback end-to-end in editor + runtime
- [ ] **FBX import with animation** — load skeletal/mesh + animation clips into the asset pipeline
- [ ] **3D scene workflow** — compose a world scene with animated content (not just static meshes)
- [ ] **M1 leftovers** — sort layers, circle collider, publish smoke test (if still incomplete)
- [ ] **M2 Editor undo/redo** — transform / delete / component ops reversible

### Medium Priority
- [ ] **M3 Runtime UI MVP** — canvas, label, button, screen-space layout for menus/HUD
- [ ] **M4 Entity hierarchy** — parent/child transforms, hierarchy panel, serialized trees
- [ ] **M5 Public alpha packaging** — docs freshness, alpha template project, known-issues list

### Technical Debt
- [ ] Reduce single-implementation abstraction churn where it adds cost without a second backend yet
- [ ] Shared MSBuild/style props to cut per-project duplication
- [ ] Keep readiness docs in sync with shipped features (queries already landed)

## Future Considerations
- **Feature Ideas**: DirectX renderer backend (after OpenGL path is solid); particles, gamepad, navmesh (post-alpha)
- **Scalability**: Batching, asset GUID database, texture hot-reload

---
**Effort Scale** (from guide roadmap): `S`: 2-3 days | `M`: 1 week | `L`: 2+ weeks
