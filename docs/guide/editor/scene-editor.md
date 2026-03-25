# Scene Editor

The core editor workflow - panels, viewport, and tools you interact with most.

---

## Scene Hierarchy Panel

The Scene Hierarchy panel lists every entity present in the current scene. Entities are displayed by name and can be selected, created, duplicated, or removed from here.

- **Select an entity** by clicking its name. The entity becomes the active selection and its components appear in the Properties panel.
- **Right-click** anywhere in the panel to open the context menu:
  - **Create Entity** - adds a new empty entity to the scene.
  - **Duplicate** (`Ctrl+D`) - creates an exact copy of the selected entity.
  - **Delete** - permanently removes the selected entity from the scene.

---

## Viewport

The viewport is the main visual canvas where you see and interact with your scene.

### Navigation

| Action | Input |
|--------|-------|
| Pan | Middle mouse button drag |
| Zoom | Scroll wheel |
| Select entity | Left-click on entity |

### Gizmo Tools

Gizmo tools control how you interact with selected entities directly in the viewport. Switch between them using keyboard shortcuts.

| Tool | Shortcut | Behavior |
|------|----------|----------|
| Select | `Shift+Q` | Click entities to select them without moving them. |
| Move | `Shift+W` | Drag the directional arrows to translate the entity's position. |
| Scale | `Shift+R` | Drag the handles to resize the entity. |
| Ruler | `Shift+E` | Click and drag to measure distances in the viewport. Press `Escape` to clear the measurement. |

> **Note:** There is no rotation gizmo. To rotate an entity, set the rotation value directly in the Properties panel.

---

## Play / Stop Controls

The toolbar at the top of the editor provides controls for entering and exiting runtime mode.

- **Play** - Starts the simulation. Physics begins, scripts execute, and the game camera takes over the viewport. The scene runs as it would in a built game.
- **Stop** - Ends the simulation and returns to edit mode. The scene is reloaded from the last saved file on disk, reverting any changes that occurred during play.
- **Restart** - Stops the current simulation and immediately starts it again from the saved state.

**Important:** Save your scene (`Ctrl+S`) before pressing Play. Stopping the simulation discards all runtime changes and reverts to the saved state. Unsaved edits made before pressing Play will be lost when you stop.

---

## Scene Operations

| Action | Shortcut |
|--------|----------|
| New scene | `Ctrl+N` |
| Save scene | `Ctrl+S` |

Creating a new scene with `Ctrl+N` will prompt you to save any unsaved changes to the current scene before proceeding.

---

## Other Panels

### Console

The Console panel displays output from `Console.WriteLine()` calls inside your scripts as well as internal engine log messages. It is the primary tool for debugging script behavior and tracking runtime events. Messages are color-coded by severity: info, warning, and error.

### Performance Monitor

The Performance Monitor panel tracks frame time and frames per second (FPS) in real time. Use it to identify performance bottlenecks and measure the impact of changes during play mode.

### Renderer Stats

The Renderer Stats panel reports rendering workload metrics including draw call counts and vertex counts per frame. These numbers are useful when optimizing scene complexity and batching behavior.

---

## Viewport Grid and Rulers

The viewport renders a background grid that spans the world coordinate space. The grid provides a consistent visual reference for positioning and aligning entities.

Rulers run along the top and left edges of the viewport and display coordinate positions corresponding to the current camera view. As you pan and zoom, the rulers update to reflect the visible coordinate range.

---

## Next Steps

- [Component Inspector](component-inspector.md) - view and edit the components attached to a selected entity.
- [Keyboard Shortcuts](shortcuts.md) - a complete reference of all editor keyboard shortcuts.
