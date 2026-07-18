# Mouse Aiming — Introduction

## Problem

2D games often aim toward the cursor: twin-stick shooters, top-down action, click-to-target tools. The engine already delivers mouse **events** (move, button, scroll) to scripts and can poll the **keyboard** from game systems. It cannot do the two things aiming needs:

1. **Poll mouse state** from an `IGameSystem` the way `IKeyboardInput` works (position held, button held this frame).
2. **Convert that cursor into world space** under the primary game camera — especially in **editor Play**, where the game draws into an ImGui viewport that is not the full window.

Without those, games either fake aim with arrow keys (as Arena Shooter does today) or invent one-off math that breaks as soon as the camera or viewport changes.

## What this feature delivers

- **Pollable mouse input** for systems and any other DI consumer: last cursor position plus button held / pressed-this-frame, fed from the existing SilkNet mouse event stream.
- **Pointer surface** — an explicit rectangle describing “where the game view lives” in the same coordinate space as mouse positions. Editor Play publishes the viewport; Runtime publishes the full client window.
- **Screen-to-world for 2D** — a public query that maps a screen (window-space) point through the pointer surface and the primary camera’s view-projection onto the Z=0 play plane.
- **Arena Shooter wired to the new path** — mouse aims, hold left mouse button to auto-fire; WASD still moves; arrow-key aim/fire removed.

## What this feature explicitly does not do (v1)

- **Change script mouse event coordinates** — `OnMouseMoved` remains window-space; scripts that need world aim call the new query (or read what a system wrote).
- **Cursor capture / relative mouse mode** — absolute cursor → world only.
- **Gamepad dual-stick aim** — keyboard/mouse only.
- **Input rebinding UI** — fixed LMB for Arena fire in this pass.
- **Editor edit-mode picking via this API** — viewport tools keep their existing converter; this feature serves Play and Runtime gameplay.
- **Full multi-button gameplay framework** — LMB is the Arena contract; other buttons may be exposed on the poll API if cheap, but are not required for success.

## Key terminology

**Pollable input.** State a system can read each frame (`IsDown` / `WasPressed` / current position) instead of only reacting to discrete events.

**Window space (logical pixels).** Cursor coordinates as reported by the platform/windowing layer (SilkNet). Matches what mouse-move events already carry.

**Pointer surface.** The axis-aligned rectangle, in window space, that corresponds to the rendered game view. Conversion treats the cursor relative to this rectangle, not relative to the whole OS window, when they differ (editor Play).

**Screen-to-world (2D).** Mapping a window-space point → normalized device coordinates over the pointer surface → unproject with the primary camera → intersection with the Z=0 plane → world X/Y.

**Primary camera.** The scene camera marked primary; the same camera the runtime/play renderer uses for the game view.

**Content scale.** Ratio of physical framebuffer pixels to logical window pixels (e.g. Retina). Pointer surface and mouse stay in logical space so they match events; scale is an implementation detail inside conversion/picking where framebuffers need physical sizes.

**Fail soft.** When conversion cannot run (no camera, empty surface, bad matrix), the query returns “no point” and gameplay keeps a safe previous aim rather than throwing.

## Patterns and principles

**Mirror keyboard, don’t invent a second input style.** Mouse polling lives beside `IKeyboardInput`: apply events in Play/Runtime handlers, clear edge state at end of frame.

**Separate “where is the cursor” from “what world point is that.”** Raw mouse state never assumes a camera. World aim is a second step that needs surface + camera. That keeps unit tests and Runtime/Editor hosts honest about their responsibilities.

**One conversion path for Play and Runtime.** Only the surface rectangle changes (viewport vs full window). Games always call the same screen-to-world query.

**Lift, don’t duplicate.** The editor already knows how to unproject through a view-projection matrix for gizmos. Gameplay needs that idea in Engine as a public service, not a copy pasted into each game.

**Systems stay first-class for Arena.** Arena Shooter keeps Pattern A (one `IGameSystem` owns input and combat). New services are injectables, same as keyboard and physics queries.

## Architecture philosophy

**Three narrow services, one consumer story.** Mouse state, pointer surface, and camera queries each have one job. Arena (and future games) compose them: read mouse → convert → aim → shoot while button held.

**Hosts own the surface.** The Editor viewport and the Runtime window know their geometry; Engine stores and exposes the latest rect. Games never talk to ImGui or window internals.

**Editor Play is a first-class target.** Shipping Runtime-only conversion would leave the sample untestable in the place developers actually iterate. Surface publishing in Play is part of the feature, not a follow-up.

**Lazy defaults.** Hold-to-fire for Arena matches today’s feel; outside-surface cursor does not steal aim or fire; null world point keeps last facing. Expand later only when a second game needs more.
