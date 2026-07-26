# Viewport Wireframe Display — Introduction

## Problem

When authoring 3D scenes, solid shaded view alone makes it hard to judge mesh topology, overlapping geometry, and silhouette shape. Other engines expose a viewport display mode that draws meshes as edges instead of filled surfaces. This engine already has an editor viewport and a 3D mesh path, but no way to switch that view between normal shading and wireframe.

## What this feature delivers

- **Two viewport display modes** — Display Normal (today’s shaded 3D) and Display Wireframe (flat unlit mesh edges).
- **Editor-only** — Available in the edit viewport and play-in-editor view. Published and standalone runtime always render Normal.
- **3D meshes and cubes only** — Sprites, grid, gizmos, and collider debug overlays stay in their current solid/line styles.
- **Toolbar toggle** — A control beside the existing 2D/3D scene toggles flips between Normal and Wireframe.
- **Flat unlit edges** — Wireframe uses a single fixed edge color with no lighting or textures, so topology reads clearly.
- **Session-scoped** — Mode always starts as Normal when the editor launches; it is not saved in preferences or per scene.

## What this feature explicitly does not do (v1)

- Keyboard shortcuts for the mode
- Persisted or per-scene display settings
- Wireframe for 2D sprites or UI
- Runtime / script API to change display mode
- Solid-plus-edge overlay (hybrid) view
- User-configurable wireframe color
- Unique-edge / silhouette-only extraction (every triangle edge is shown)

## Key terminology

**Viewport display mode.** Editor-only view setting that chooses how the viewport presents 3D geometry: Normal or Wireframe. It is not part of the scene asset and does not change what is saved.

**Display Normal.** Filled, shaded 3D drawing (PBR meshes and lit cubes) as the viewport already does.

**Display Wireframe.** The same 3D mesh and cube geometry drawn as triangle edges in a single flat color, without materials or lights.

**Polygon line mode.** Graphics API rasterization option that strokes triangle edges instead of filling them. Used as the GPU mechanism behind Wireframe; Fill is restored for Normal and for any pass that must stay solid.

**Apply / restore.** Per-frame editor contract: enter Wireframe for the 3D scene pass, then restore Fill so overlays and the next frame cannot inherit line mode accidentally.

## Patterns and principles

**View setting, not content.** Wireframe changes how the editor looks at the scene, not the scene itself. Runtime players never see the toggle.

**Narrow blast radius.** Only mesh and cube draws take the wireframe path. Everything else keeps current behavior so the feature stays small and predictable.

**Platform boundary.** Polygon fill vs line stays behind the renderer API. Engine and editor code never talk to OpenGL directly.

**Fail toward Normal.** If wireframe setup fails, or mode is unknown, the viewport behaves as Normal rather than leaving the GPU in a sticky line state.

**YAGNI for v1.** Toolbar + session state + flat color is enough. Shortcuts, persistence, and fancy edge filtering wait until someone actually needs them.

## Architecture philosophy

**Editor owns the switch; graphics owns the draw.** The toolbar holds the mode. The viewport applies it around the existing scene render. The 3D graphics layer chooses shaded vs unlit-line drawing. The scene render pipeline stays unaware of display modes and keeps calling the same draw entry points.

**Reuse geometry, change rasterization and shading.** Wireframe does not build a second mesh. It draws the same indexed triangles with line polygon mode and an unlit color path.

**Lazy senior default.** Two modes, one toolbar button, one fixed edge color, restore Fill every frame. Ship the useful viewport tool; grow preferences and shortcuts only if the workflow demands them.
