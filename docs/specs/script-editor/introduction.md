# In-Engine Script Editor — Conceptual Introduction

## What Problem Does This Solve?

Scripts attached to entities are C# source files on disk. Today the inspector **Edit** button opens that file in whatever the operating system considers the default editor. The engine does not see those edits until a later compile (typically **Play**). There is no in-editor buffer, no Save versus Discard, and no way to keep the viewport and inspector usable while looking at the script.

Authors working inside the editor need to change a script, try the scene, and come back to the same buffer — without leaving the editor and without a blocking dialog that owns the whole UI.

## What the Feature Will Achieve

After this feature lands:

- **Edit** on an attached script opens a **dockable Script Editor** panel (a tab the user can leave and return to), not the OS editor.
- The panel shows the script with **line numbers** and **C# keyword coloring**.
- **Save** writes the file and **recompiles** the game scripts. Compile failures stay visible in the panel as the workspace error strings.
- **Discard** restores the last saved file into the buffer without closing the panel.
- Unsaved work is obvious (`*` in the title) and cannot be thrown away by closing the panel, switching to another script, or pressing **Play** without a confirmation.

## Terminology

**Attached script** — A `NativeScriptComponent` on an entity whose `ScriptTypeName` names a `ScriptableEntity` class. The matching source file is `assets/scripts/{ScriptTypeName}.cs`.

**Script Editor panel** — A closable, dockable editor window. Hidden until **Edit**. Not a modal; not a permanent View-menu pane in the first version.

**Buffer** — The text currently in the in-engine editor widget. It may differ from the file on disk.

**Last-saved snapshot** — A copy of the file text as of the last load or Save (Save writes the file even when compile later fails). Used to decide whether the buffer is dirty.

**Dirty** — Buffer text does not match the last-saved snapshot. Shown as `*` in the window title.

**Game assembly** — The compiled DLL produced from all project scripts. Save and Play both rebuild/load this assembly through the existing script workspace.

**Keyword highlighting** — Coloring of C# keywords, comments, and similar tokens. Not a full language service (no IntelliSense, no type checking in the widget).

## Patterns and Principles

### Panel over modal

A modal would force the user to finish or cancel before touching the scene. The requirement is the opposite: glance at the script, tweak an entity, come back. A dockable panel is the same pattern as Console or Keyboard Shortcuts: one ImGui window, show/hide, layout remembered by docking.

### One file, one buffer

The first version edits **one script at a time**. Switching to another attached script replaces the buffer (after a dirty confirmation if needed). Multiple document tabs inside the panel are out of scope.

### Workspace owns disk and compile; the panel owns the buffer

The script workspace already writes `.cs` files and compiles the game assembly. The panel does not invent a second compile pipeline. It loads text, lets the user edit, and on Save hands content back to that workspace. Error strings that come back are displayed; they are not re-parsed into a new diagnostics engine.

### Write-then-compile is the Save contract

Save always persists the file first, then compiles — the same order as **Create New Script**. If compile fails, the file on disk already matches the buffer (no longer dirty). The error strings stay so the author can fix the file. This also means Discard after a failed Save does nothing useful: there is no hidden “previous good file” in the panel.

### Confirm before losing the buffer or playing stale disk

Because the panel is non-modal, **Play** can run while the buffer is dirty. Play compiles **from disk**, not from the widget. If the panel is dirty, Play is blocked behind **Save and Play** / **Cancel**. Closing the panel or switching scripts uses Save / Discard & continue / Cancel.

### Borrow a widget; do not build an IDE

Syntax highlighting and line numbers come from an existing ImGui text-editor widget (C# port). The engine adds a small C# keyword/comment definition and a toolbar. No language server, no project-wide search, no debug integration.

## Architecture Philosophy

The Script Editor is a **thin editor-only shell** around two facts that already exist: scripts are files under `assets/scripts/`, and the workspace can write and compile them.

1. **Inspector** — decides *which* script to open (the attached type name).
2. **Panel** — holds the buffer, dirty state, and Save / Discard / confirmations.
3. **Workspace** — writes the file and reloads the game assembly.
4. **Play path** — asks the panel whether the buffer is dirty before compiling from disk.

The Engine runtime does not know the panel exists. Published games do not include it.

Failed compile still unloads the game assembly until the next successful compile. That is existing workspace behavior, not a new editor policy. The panel surfaces the errors; it does not try to keep a stale assembly loaded.

## Out of Scope

- Opening game-component or game-system `.cs` files from the Content Browser
- Multiple scripts open at once / inner tab strip
- OS “Open in external editor” as a secondary action
- File-system watching for edits made outside the engine
- Full C# language service (completion, rename, go-to-definition)
- Changing the workspace so a failed compile keeps the previous assembly loaded
- “Play without saving” while the buffer is dirty (Discard first, then Play)

These limits keep the first version a replacement for **Edit**, not a second IDE.
