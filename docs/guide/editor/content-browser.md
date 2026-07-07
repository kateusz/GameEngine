# Content Browser

Browse and manage your project's assets.

## Overview

The Content Browser panel displays the files inside your project's `assets` directory. The current path is shown at the top of the panel so you always know where you are in the hierarchy.

To navigate into a folder, double-click it. When you are inside a subdirectory, a back arrow button (`<-`) appears at the top of the panel — click it to move up to the parent directory. You cannot navigate above the root `assets` directory.

Files are displayed in a grid using an icon appropriate for each asset type. The grid column count adjusts automatically to fit the available panel width.

## Supported Asset Types

| Extension | Type | Display |
|-----------|------|---------|
| `.png`, `.jpg` | Texture | Thumbnail (actual image preview) |
| `.wav`, `.ogg` | Audio Clip | File icon |
| `.scene` | Scene | File icon |
| `.prefab` | Prefab | Special prefab icon |

> **Not supported yet:** `.obj`, `.fbx` (no 3D mesh import). These may appear as generic file icons if present in `assets/`.

Any file type not listed above also displays a generic file icon.

## Drag and Drop

Assets can be dragged from the Content Browser directly onto component fields in the Properties panel. When a drag begins, a small preview tooltip shows the file name and type.

All drop targets accept only files with matching extensions — dropping an incompatible file type onto a target has no effect.

| Drag source | Drop target | Result |
|-------------|-------------|--------|
| `.png` / `.jpg` texture | SpriteRendererComponent texture field | Assigns the texture |
| `.wav` / `.ogg` audio file | AudioSourceComponent audio clip field | Assigns the audio clip |
| `.prefab` prefab file | Scene Hierarchy panel (onto existing entity) | Applies prefab data to that entity |

The Content Browser passes the asset's path relative to the `assets` directory as the drag-and-drop payload. Drop targets resolve the full path by combining this relative path with the project's assets root.

## Creating Assets

### Context Menu (Directory Tree)

Right-click any folder in the left-side directory tree to open a context menu with three options:

- **Add Script** — creates a new `ScriptableEntity` script in `assets/scripts/`
- **Add Component** — creates a new `IGameComponent` class in `assets/scripts/`
- **Add System** — creates a new `IGameSystem` class in `assets/scripts/`

These options are enabled only when you right-click the `scripts` folder or one of its subfolders. On other folders (textures, scenes, etc.) the menu items appear grayed out. A name prompt opens when you choose an action; the new file is compiled immediately but is not attached to any entity.

**Scripts (Properties panel)**

Scripts can also be created from the NativeScriptComponent in the Properties panel. With an entity selected, expand the Script section and click **Create New Script**. Enter a valid C# identifier as the script name and confirm. The engine generates a script template and saves it to `assets/scripts/`. The script is immediately compiled and attached to the entity.

You can also click **Add Existing Script** to attach a previously created script to the selected entity.

**Scenes**

Use **Ctrl+N** to create a new scene (a name prompt appears). Use **Ctrl+S** to save the current scene. Both actions are also available in the **File** menu in the menu bar.

## Thumbnails and Icons

- **Texture files** (`.png`, `.jpg`): The actual image is loaded and rendered as a thumbnail. Thumbnails are cached after the first load so repeated rendering does not reload from disk.
- **Prefab files** (`.prefab`): Display a dedicated prefab icon to distinguish them from plain data files.
- **Directories**: Display a folder icon.
- **All other files**: Display a generic file icon.

## Next Steps

- [Component Inspector](component-inspector.md) — view and edit component properties, including drag-and-drop targets
- [Roadmap](../roadmap.md) — planned tilemap and 3D model import features
