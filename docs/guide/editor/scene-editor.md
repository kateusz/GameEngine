# Scene Editor

The core editor workflow - panels, viewport, and tools you interact with most.

---

## Scene Hierarchy Panel

The Scene Hierarchy panel lists every entity present in the current scene. Entities are displayed by name and can be selected, created, duplicated, or removed from here.

- **Select an entity** by clicking its name. The entity becomes the active selection and its components appear in the Properties panel.
- **Search** using the filter box at the top of the panel to narrow the list.
- **Right-click** on empty space in the panel to open the context menu:
  - **Create Empty Entity** — adds a new empty entity to the scene.
  - **Create 3D Entity** — adds an entity with a perspective camera, ambient/directional lights, and a lit cube.
- **Right-click** a selected entity to **Delete Entity**.
- **Duplicate** the selected entity with `Ctrl+D`.

---

## Viewport

The viewport is the main visual canvas where you see and interact with your scene.

### Navigation

| Action | Input |
|--------|-------|
| Pan | Alt + middle mouse drag |
| Orbit | Alt + left mouse drag |
| Zoom (drag) | Alt + right mouse drag |
| Zoom (wheel) | Scroll wheel |
| Select entity | Left-click on entity (Select mode, or while using Move/Scale/Rotate tools) |

### Gizmo Tools

Gizmo tools control how you interact with selected entities directly in the viewport. Switch between them using keyboard shortcuts.

| Tool | Shortcut | Behavior |
|------|----------|----------|
| Select | `Shift+Q` | Click entities to select them without moving them. |
| Move | `Shift+W` | Drag the directional arrows to translate the entity's position. |
| Scale | `Shift+R` | Drag the handles to resize the entity. |
| Rotate | *(toolbar only)* | Drag the Z-axis ring to rotate the entity. |
| Ruler | `Shift+E` | Click and drag to measure distances in the viewport. Press `Escape` to clear the measurement. |

The toolbar also provides **2D Grid** and **3D Grid** toggles. Grid and ruler visibility can be changed from the **View** menu.

---

## Play / Stop Controls

The toolbar at the top of the editor provides controls for entering and exiting runtime mode.

- **Play** - Starts the simulation. Physics begins, scripts execute, and the game camera takes over the viewport. Requires an open project with an `assets/scripts/` directory. Scripts are recompiled before play starts.
- **Stop** - Ends the simulation and returns to edit mode. The scene is reloaded from the last **saved** file on disk, reverting any changes that occurred during play.
- **Restart** - Stops the current simulation and immediately starts it again. Requires the scene to have been saved at least once.

**Important:** Play snapshots the current editor state (including unsaved edits) into a temporary file. Stop reloads the saved scene path on disk — not the pre-play in-memory state. Save your scene (`Ctrl+S`) before pressing Play if you want Stop to return to that version.

---

## Scene Operations

| Action | Shortcut |
|--------|----------|
| New scene | `Ctrl+N` |
| Save scene | `Ctrl+S` |

`Ctrl+N` opens a name/settings popup for the new scene. It does not prompt to save the current scene first — save manually if needed.

---

## Other Panels

### Console

The Console panel displays output from `Console.WriteLine()` calls inside your scripts as well as internal engine log messages. It is the primary tool for debugging script behavior and tracking runtime events. Messages are color-coded by severity: info, warning, and error.

### Stats

The **Stats** panel (open via **View → Show Stats**) reports rendering workload metrics including draw call counts and vertex counts per frame. When **Show FPS Counter** is enabled in Editor Settings, frame time and FPS are also shown in this panel.

---

## Viewport Grid and Rulers

The viewport renders a background grid that spans the world coordinate space. The grid provides a consistent visual reference for positioning and aligning entities.

Rulers run along the top and left edges of the viewport and display coordinate positions corresponding to the current camera view. As you pan and zoom, the rulers update to reflect the visible coordinate range.

---

## Next Steps

- [Component Inspector](component-inspector.md) - view and edit the components attached to a selected entity.
- [Keyboard Shortcuts](shortcuts.md) - a complete reference of all editor keyboard shortcuts.
