# Runtime UI — Introduction

## Problem

Shipped games and templates need menus, HUD text, and clickable buttons. Today the engine has no runtime UI stack. Sample games fake overlays by swapping sprite textures on world entities (pre-rendered “banner” images). That works for one or two screens and collapses as soon as you need real text, hover states, or a menu that is not also a gameplay sprite.

The editor’s ImGui tooling solves a different problem: tooling UI for developers. It is not available in published builds and must not become a runtime dependency. Games need a **first-class, scene-authored, screen-space UI** that draws through the engine’s own 2D path and talks to the same scripting model as everything else.

## What this feature delivers

- **Screen-space canvas** — a scene marker for UI drawn after the world, in window pixels.
- **Absolute pixel layout** — each widget has an explicit screen rectangle (position and size). No anchors or reference-resolution scaling in this version.
- **Flat widgets** — labels, images, and buttons as sibling entities under a canvas idea; no nested UI trees (entity hierarchy is a later milestone).
- **Bitmap text** — BMFont (or equivalent pre-rendered atlas) so labels become textured glyph quads.
- **Interactive buttons** — hover / pressed / disabled visuals, click detection, and a script hook on the same entity (`ScriptableEntity`).
- **Input consumption** — pointer interaction over UI does not also fire as gameplay input.
- **Editor authoring** — inspectors for the new components so menus are built in the scene, not only in code.
- **Sample menu path** — a small Play / Quit style scene that replaces banner hacks for the alpha story.

## What this feature explicitly does not do (v1)

- **Anchors, pivots, and reference resolution** — no automatic scale-to-window layout.
- **Nested parent/child UI** — no panel trees; wait for scene hierarchy or a later UI parenting pass.
- **Scroll views, sliders, text input** — deferred.
- **World-space canvas** — UI is screen overlay only.
- **Nine-slice / advanced image slicing** — deferred.
- **Theming system** — per-widget colors and textures only.
- **Reuse of editor ImGui at runtime** — never.

## Key terminology

**Runtime UI.** Screen-space interface drawn and interacted with in play mode and published builds, authored as scene entities with UI components.

**Canvas.** The root concept for screen-space UI in a scene. Marks that UI should be collected, hit-tested, and drawn in a dedicated pass after the world.

**UIRect.** The widget’s box in screen pixels: position and size. For v1 this *is* the layout result — nothing computes it further.

**Screen space.** Coordinates in window pixels. Convention for v1: origin at the **top-left**, Y increases downward (UI space), distinct from world Y-up.

**Label.** A text widget: string content, font asset, color, and horizontal alignment within its rect.

**Image.** A non-interactive (by itself) textured or solid-color quad filling a rect.

**Button.** An interactive widget with visual states and click dispatch to a script on the same entity. Owns its own visual states in v1 (separate from Image).

**BMFont.** A pre-rendered bitmap font: a texture atlas plus a metrics file describing glyph UVs, advances, and line metrics. Runtime turns strings into quads; it does not rasterize TrueType at runtime.

**UI render pass.** A second draw pass after world sprites/meshes, using an orthographic screen projection and the existing 2D batcher.

**Hit-test.** Mapping a pointer position into screen pixels and finding the front-most interactable button whose rect contains that point.

**Input consumption.** When the pointer is over UI, the click/press is treated as handled by UI so gameplay systems should not also react.

**Click hook.** A method on the button entity’s scriptable behaviour invoked once when a press and release complete on that same button.

## Patterns and principles

**ECS data, systems for behaviour.** Widgets are components. Layout (trivial in v1), input, and rendering are systems. No parallel immediate-mode UI API for menus.

**Same scene, different pass.** UI entities live beside gameplay entities for authoring and serialization, but world cameras ignore them for drawing; the UI pass owns screen presentation.

**Reuse the 2D batcher.** Images, button quads, and glyphs are all textured/colored quads. Fonts are an asset + quad emitter, not a second renderer.

**Scripts stay on entities.** Buttons do not invent a new event bus for v1. Click goes to the script already attached to that entity — the pattern games already know.

**Fail soft.** Missing fonts or textures log and skip drawing; they must not tear down the frame loop. Missing scripts on a clicked button log and ignore.

**YAGNI boundaries.** Absolute pixels and a flat canvas unlock menus and HUD for alpha. Scaling, nesting, and richer controls are deliberate follow-ups with clear demand signals.

## Architecture philosophy

**Extend rendering and ECS; do not bolt on a UI framework.** The smallest useful stack is: components for canvas/rect/visuals/button, a font loader, hit-testing, a post-world draw pass, and thin editor inspectors.

**One canvas association rule for v1.** With a single active canvas assumed, widgets do not need a parent link field. Multi-canvas targeting can add an explicit canvas id later without rewriting the conceptual model.

**Editor ImGui is contrast, not a dependency.** Inspectors use ImGui because the editor already does. Published games never load that stack for game UI.

**Ship the M3 unlock, defer the suite.** Canvas + Label + Image + Button + BMFont + click/hover + sample menu is the exit criterion. Anchors, hierarchy-backed nesting, scroll, and text fields wait until alpha games prove which pain is next.
