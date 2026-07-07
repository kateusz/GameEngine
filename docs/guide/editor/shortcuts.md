# Keyboard Shortcuts

Editor keyboard shortcuts are registered at startup and dispatched through a centralized manager. Use this page as a user reference and as a guide for adding new shortcuts in code.

---

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

### File Operations

| Shortcut | Action |
|----------|--------|
| Ctrl+N | New scene |
| Ctrl+S | Save scene |

### Edit Operations

| Shortcut | Action |
|----------|--------|
| Ctrl+D | Duplicate selected entity |

### Navigation

| Shortcut | Action |
|----------|--------|
| Ctrl+R | Reset camera position |

---

## How editor input is routed

**File**: `Editor/Input/EditorInputHandler.cs` — entry point from `EditorLayer.HandleInputEvent`.

```
InputEvent (from engine layer stack)
        │
        ▼
EditorInputHandler.Handle
        │
        ├─ KeyPressed (not repeat, ImGui not capturing keyboard)
        │     └─ ShortcutManager.HandleKeyPress → action, marks event handled
        │
        ├─ SceneState.Edit
        │     └─ IEditorViewport.HandleWindowInput (viewport tools, camera)
        │
        └─ SceneState.Play
              ├─ KeyboardInputState.Apply (polling for IGameSystem)
              └─ ScriptEngine.ProcessEvent (ScriptableEntity callbacks)
```

| Mode | Shortcuts | Viewport input | Game script input |
|------|-----------|----------------|-------------------|
| **Edit** | Yes | Yes | No |
| **Play** | Yes (when ImGui does not capture keyboard) | No | Yes |

Shortcuts are skipped when `ImGui.GetIO().WantCaptureKeyboard` is true (typing in a text field, search box, etc.).

For game-script input callbacks and polling APIs, see [Input Handling](../scripting/input.md).

---

## Architecture

| Type | File | Role |
|------|------|------|
| `EditorInputHandler` | `Editor/Input/EditorInputHandler.cs` | Routes `InputEvent` to shortcuts, viewport, or play-mode scripts |
| `ShortcutManager` | `Editor/Input/ShortcutManager.cs` | Registers shortcuts, detects conflicts, executes matching actions |
| `KeyboardShortcut` | `Editor/Input/KeyboardShortcut.cs` | Key + `KeyModifiers` + `Action` + description + category |
| `KeyModifiers` | `Editor/Input/KeyModifiers.cs` | `None`, `CtrlOnly`, `ShiftOnly`, `AltOnly`, `CtrlShift`, `CtrlAlt` |
| `EditorShortcutRegistrar` | `Editor/Input/EditorShortcutRegistrar.cs` | Registers all built-in editor shortcuts at startup |
| `KeyboardShortcutsPanel` | `Editor/Input/KeyboardShortcutsPanel.cs` | ImGui panel listing shortcuts by category |

`ShortcutManager` is a singleton registered in `Editor/DI/EditorIoCContainer.cs`. `EditorShortcutRegistrar.RegisterAll` is called from `Editor/Features/Shell/EditorLifecycle.cs` during editor attach.

---

## Adding a shortcut

Register new shortcuts in `EditorShortcutRegistrar.RegisterAll` (or inject `ShortcutManager` from your own registrar if you add a feature module):

```csharp
shortcutManager.RegisterShortcut(new KeyboardShortcut(
    KeyCodes.G, KeyModifiers.CtrlOnly,
    () => myFeature.DoSomething(),
    "Do something", "My Category"));
```

| Parameter | Purpose |
|-----------|---------|
| `key` | Primary `KeyCodes` value (`Input` namespace) |
| `modifiers` | Required modifier flags (`KeyModifiers.CtrlOnly`, etc.) |
| `action` | `Action` invoked when the combo matches |
| `description` | Shown in the Keyboard Shortcuts panel |
| `category` | Panel grouping (e.g. `"File"`, `"Tools"`) |

`RegisterShortcut` returns `false` and logs a warning if the same key+modifier combo is already registered (unless `allowDuplicates: true`).

Display strings (e.g. `Ctrl+S`) come from `KeyboardShortcut.GetDisplayString()`.

---

## Related

- [Scene Editor](scene-editor.md) — viewport tools bound by Shift+Q/W/R/E
- [Input Handling](../scripting/input.md) — runtime game input in Play mode
