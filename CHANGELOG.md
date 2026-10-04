# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [0.10.0] - 2026-10-03

### Added

#### Engine & rendering

- **3D PBR** — metallic/roughness shading for imported meshes; GLB alpha channel support for cutouts and transparency.
- **Post-processing** — optional FXAA pass (toggle in editor).
- **Lighting** — point lights with Cook-Torrance BRDF; directional light intensity; import of lights from GLB/GLTF/FBX (point, directional, spot where supported).
- **Shadows** — directional shadow maps; omnidirectional point-light shadow cubemaps with caching; configurable shadow-caster distance for directional lights.
- **Culling & visibility** — camera frustum culling for 3D draws; **visibility zones** (volume-based show/hide assignments on model renderers).
- **Performance** — GPU mesh instancing for repeated geometry; **runtime mesh format** (cooked sibling files) with concurrent source import; background loading for scenes, models, and textures.
- **Editor viewport selection** — entity selection outline in edit mode (including multiple selected entities).

#### Editor

- **ImGuizmo integration** — move, scale, and rotate gizmos in the 3D viewport.
- **3D model workflow** — drag-and-drop models into the viewport; spawns entity hierarchy from imported meshes.
- **Multi-entity editing** — Shift/Ctrl multi-select in the hierarchy; properties panel shows only components shared by the selection; bulk edit for tag components.
- **Scene hierarchy** — pinned search field; filter by component type; scroll-to-selected; multi-entity reparenting; `Del` to delete; quick-add entity menu.
- **Selection sync** — optional viewport pick → hierarchy selection; frame camera on hierarchy selection (including child entities).
- **Godot-style bottom panel** — always-visible tab bar (Console, Content Browser) under the viewport column.
- **Command palette** — searchable command list with entity jump (`Ctrl+Shift+P`).
- **Project settings** — default startup scene, window title, resolution, fullscreen, and target frame rate (separate from publish/build options).
- **Scene settings dialog** — scene-level options moved to **Scene → Settings**.
- **Properties search** — filter component fields by name across the selection.
- **Content Browser** — Windows context menu (Show in Explorer, Edit); tree expand/collapse fixes; UI refactor.
- **Undo/redo** — reversible transforms, component add/remove, entity delete, and model import hierarchy.
- **Play mode** — safer stop/reload path; selection outlines disabled while playing.
- **Viewport** — optional grid disable; visibility-zone wireframe debug in edit mode; improved 3D picking.
- **Console** — auto-scroll to latest log; layout and search improvements.

#### Runtime & platform

- **Player** — `game.config.json` drives window and graphics settings in the standalone player and published builds.
- **Publishing** — simplified export (self-contained single-file defaults); host RID selection for OpenAL packaging.
- **Scripting** — leaner runtime graph (Roslyn not required in the player); simplified `IGameSystem` / hot-reload path.
- **Benchmarks** — opening screen with four categories (2D, 3D, lighting, shadows). A category times only its own scenes; results stay for the session and compare against a baseline. 3D measures geometry with shadows and point lights off; lighting and shadows reuse one cube grid so the extra cost is the lights or the shadow passes.

### Changed

- **ECS & scenes** — simplified scene management, system registration, and primary-camera resolution.
- **Lighting pipeline** — centralized light resolver and shared lighting math.
- **Asset management** — smaller surface area; GPU resource lifetime fixes.
- **Model materials** — per-instance material override removed; materials come from imported assets and component fields.
- **Editor layout** — dockspace and panel structure updated for the bottom panel host; 2D/3D stats panels respect project type.
- **Directional shadows** — a view can turn the sun's depth pass off (`DirectionalShadows`, default on) so a lit frame is not also a shadow measurement.
- **NuGet** — dependency updates across the solution.

### Fixed

- Point-light attenuation and point-light shadow rendering.
- GLB/FBX loading (textures, colors, UE-exported FBX).
- Dark fringes on cutout textures: sRGB decode copies edge color into transparent texels before filtering.
- Entity selection outline when multiple entities share the same name.
- Ctrl+click multi-select clearing immediately in the hierarchy.
- Editor crash when stopping play mode (intermittent).
- Content Browser directory tree expand/collapse.
- Various UX and rendering regressions from the 3D expansion.

### Documentation

- Architecture guides for the **scene rendering pipeline**, **lighting**, and **shadows**.
- Module and developer-guide updates; README refresh.
- Feature specs under `docs/specs/` (multi-entity edit, runtime mesh, visibility zones, selection outline, benchmark categories, and related topics).
