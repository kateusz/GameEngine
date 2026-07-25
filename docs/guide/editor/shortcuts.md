# Keyboard Shortcuts
## Viewing shortcuts in the editor

Open **Help → Keyboard Shortcuts** to browse all registered bindings grouped by category. The panel supports filtering by key combo or description.

**File**: `Editor/Input/KeyboardShortcutsPanel.cs`

---

## Quick reference

### Viewport Tools

| Shortcut | Action |
|----------|--------|
| Shift+Q | Select tool |
| Shift+W | Move tool |
| Shift+R | Scale tool |
| Shift+E | Ruler tool |
| Escape | Clear ruler measurement (when Ruler tool is active) |

> **Rotate tool** is available on the viewport toolbar but has no keyboard shortcut.

### File Operations

| Shortcut | Action |
|----------|--------|
| Ctrl+N | New scene |
| Ctrl+S | Save scene |

### Edit Operations

| Shortcut | Action |
|----------|--------|
| Ctrl+Z | Undo |
| Ctrl+Y | Redo |
| Ctrl+D | Duplicate selected entity |

Undo covers gizmo transforms, hierarchy delete, and component add/remove. Property inspector edits, create-entity, and duplicate are not undoable yet. After undoing a delete, entity IDs are remapped — further undoing older edits that targeted the pre-delete entity may no-op.

### Navigation

| Shortcut | Action |
|----------|--------|
| Ctrl+R | Reset camera position |

---

## Related

- [Scene Editor](scene-editor.md) — viewport tools (Shift+Q/W/R/E; Rotate is toolbar-only)
- [Input Handling](../scripting/input.md) — runtime game input in Play mode
