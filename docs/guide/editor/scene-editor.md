# Scene Editor

Docked ImGui layout: **Scene Hierarchy** and **Properties** beside the **viewport**, with a thin **bottom tab bar** (Console, Content Browser) under the viewport column.

## Menus

| Menu | Common actions |
|------|----------------|
| **Project** | New / Open / Close; recent projects; **Settings** (`game.config.json`); **Export…** (publish) |
| **Scene** | New / Open / Save / Close; **Settings** (scene background color) |
| **View** | Command palette, reset camera, toggle **rulers**, toggle **Show Debug** (Stats panel) |
| **Editor** | **Settings** — follow viewport selection in hierarchy, collider debug, FPS counter, FXAA, autosave interval |
| **Help** | Keyboard shortcuts dialog |

**Command palette** (`Ctrl+Shift+P`) — run menu commands and **jump to entity** by name.

---

## Scene Hierarchy Panel

Parent/child **entity tree** with:

- **Search** (pinned at top) — filter by name; ancestors of matches stay expanded.
- **Component-type filter** — show only entities that have selected component types.
- **Select** — click; **Shift** range; **Ctrl** toggle multi-select.
- **Drag-and-drop** — reparent one or many selected entities; drop on empty space to move to root.
- **+** menu / context menus — create empty entity, duplicate (`Ctrl+D`), delete (`Del`).
- **Scroll to selected** — after a **viewport pick** when **Editor → Settings → Follow viewport selection in Scene Hierarchy** is on (default), or when the command palette jumps to an entity.
- **Visible** — per-entity flag on `TransformComponent` (cascades to `EffectiveVisible`).

Clicking an entity in the hierarchy **frames** the edit camera on that entity’s world position (viewport pick selects without reframing).

---

## Viewport

### Scene dimension

The viewport toolbar includes **2D** / **3D** toggles for the active scene’s `Dimension`. **3D** scenes can show a ground grid when the grid toggle is on.

### Navigation

| Action | Input |
|--------|-------|
| Look | Right mouse drag |
| Fly | Right mouse held + WASD |
| Fly up / down | Right mouse held + E / Q |
| Fly speed | Right mouse held + scroll wheel |
| Pan | Middle mouse drag |
| Orbit | Alt + left mouse drag |
| Zoom (drag) | Alt + right mouse drag |
| Pan (Alt) | Alt + middle mouse drag |
| Slide | Left + right mouse drag |
| Zoom (wheel) | Scroll wheel |
| Reset camera | `Ctrl+R` or **View → Reset Camera** |
| Select entity | Left-click (Select mode or while using Move/Scale/Rotate) |

### Gizmo tools

| Tool | Shortcut | Notes |
|------|----------|--------|
| Select | `Shift+Q` | Pick without moving |
| Move | `Shift+W` | ImGuizmo translate |
| Scale | `Shift+R` | ImGuizmo scale |
| Rotate | *(toolbar only)* | ImGuizmo rotate (Z ring in 2D workflow) |
| Ruler | `Shift+E` | Measure; `Escape` clears |

**Grid** — 2D overlay and (in 3D scenes) 3D grid: toggle on the **viewport toolbar**, not the View menu.

**Rulers** — top/left edges; toggle **View → Show Rulers**.

**Edit-only overlays** — selection outline (hidden in Play mode), optional visibility-zone wireframes, optional collider bounds (**Editor → Settings**).

### Drag and drop

Drop **textures**, **audio**, **prefabs**, and **3D models** into the viewport. Models spawn an imported entity hierarchy (meshes and supported lights). See [Content Browser](content-browser.md).

---

## Multi-select and Properties

With multiple entities selected, **Properties** edits fields in bulk where values match (mixed values show blank controls). Component remove buttons are hidden. Details: [Component Inspector](component-inspector.md#overview).

---

## Play / Stop

Toolbar **Play / Stop / Restart**:

- **Play** — recompiles scripts, then runs physics and `IGameSystem` / components using the **Primary** `CameraComponent`. Requires `assets/scripts/`.
- **Stop** — reloads the last **saved** `.scene` from disk (runtime edits discarded unless you saved).
- **Restart** — stop and play again; needs a saved scene path.

Play snapshots unsaved in-memory state for the session; **Stop** still reloads the on-disk file. **Ctrl+S** before Play if Stop should return to what you see.

---

## Scene operations

| Action | Shortcut |
|--------|----------|
| New scene | `Ctrl+N` |
| Save scene | `Ctrl+S` |
| Open scene | **Scene → Open…** or drag `.scene` onto viewport |

`Ctrl+N` does not prompt to save the current scene.

---

## Bottom panel

Tabs under the viewport: **Console** and **Content Browser**. Click a tab to expand or collapse the panel (Godot-style). Console: script `Console.WriteLine`, engine logs, filters, search, auto-scroll.

---

## Stats (debug)

**View → Show Debug** opens the **Stats** window: 2D or 3D renderer metrics (draw calls, vertices, culling counts in 3D), editor camera info, and optional FPS/frame history when **Show FPS Counter** is enabled in **Editor → Settings**.

---

## Publish

**Project → Export…** builds a standalone **Runtime** executable for the chosen RID (defaults to host: Windows/macOS x64 or ARM64). Output includes `game.config.json` and packaged assets. See [Scenes and Prefabs](../concepts/scenes-and-prefabs.md) for player behavior.

---

## Next steps

- [Component Inspector](component-inspector.md)
- [Content Browser](content-browser.md)
- [Keyboard Shortcuts](shortcuts.md)
- [Cameras and Rendering](../concepts/cameras-and-rendering.md)
