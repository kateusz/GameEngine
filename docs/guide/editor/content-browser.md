# Content Browser

## Overview

The Content Browser lives in the **bottom panel** tab bar under the viewport ([Scene Editor](scene-editor.md#bottom-panel)). Folder tree on the left, asset grid on the right.

To navigate into a folder from the grid, double-click it. When you are inside a subdirectory, a back arrow button (`<-`) appears at the top of the panel — click it to move up to the parent directory. You cannot navigate above the root `assets` directory.

## Supported Asset Types

| Extension | Type | Display |
|-----------|------|---------|
| `.png`, `.jpg` | Texture | Thumbnail (actual image preview) |
| `.glb`, `.gltf`, `.fbx` | 3D model | File icon |
| `.wav`, `.ogg` | Audio Clip | File icon |
| `.scene` | Scene | File icon |
| `.prefab` | Prefab | File icon (same as other data files) |

Any file type not listed above also displays a generic file icon.

## Drag and Drop

Assets can be dragged from the Content Browser directly onto component fields in the Properties panel. When a drag begins, a small preview tooltip shows the file name and type.

All drop targets accept only files with matching extensions — dropping an incompatible file type onto a target has no effect.

| Drag source | Drop target | Result |
|-------------|-------------|--------|
| `.png` / `.jpg` texture | Viewport or Sprite/SubTexture texture field | Creates or assigns sprite rendering |
| `.glb` / `.gltf` / `.fbx` | Viewport | Spawns imported **entity hierarchy** (meshes + point/directional lights where supported) |
| `.glb` / `.gltf` / `.fbx` | ModelRendererComponent model field | Imports hierarchy on that entity (undoable) or bulk-assigns path in multi-select |
| `.wav` / `.ogg` audio file | Viewport or AudioSource clip field | Assigns audio |
| `.prefab` prefab file | Scene Hierarchy (onto existing entity) | Applies prefab v2 subtree to that entity |
| `.scene` scene file | Viewport | Opens the scene |

The Content Browser passes the asset's path relative to the `assets` directory as the drag-and-drop payload. Drop targets resolve the full path by combining this relative path with the project's assets root.

## Creating Assets

### Context Menu (Directory Tree)

Right-click any folder in the left-side directory tree to open a context menu:

- **Show in Explorer** (Windows only) — opens the folder in File Explorer
- **Add Component** — creates a new `IGameComponent` class in `assets/scripts/`
- **Add System** — creates a new `IGameSystem` class in `assets/scripts/`

The Add Component / Add System options are enabled only when you right-click the `scripts` folder or one of its subfolders. On other folders (textures, scenes, etc.) those menu items appear grayed out. A name prompt opens when you choose an action; names must match `^[a-zA-Z][a-zA-Z0-9_]*$` (letters, digits, underscore; must start with a letter). The new file is compiled immediately.

### Context Menu (Content Grid)

Right-click any file or folder in the content grid (Windows only):

- **Show in Explorer** — reveals the item in File Explorer
- **Edit** — opens the item with the default associated application

**Scenes**

Use **Ctrl+N** to create a new scene (a name prompt appears). Use **Ctrl+S** to save the current scene. Both actions are also available in the **Scene...** menu in the menu bar.

## Thumbnails and Icons

- **Texture files** (`.png`, `.jpg`): The actual image is loaded and rendered as a thumbnail. Thumbnails are cached after the first load so repeated rendering does not reload from disk.
- **Known folders** (`scenes`, `scripts`, `textures`, `prefabs`, etc.): Display folder-specific icons in the tree.
- **Directories**: Display a folder icon.
- **All other files** (including `.prefab` and `.scene`): Display a generic file icon.

## Next Steps

- [Component Inspector](component-inspector.md) — view and edit component properties, including drag-and-drop targets
- [Scene Editor](scene-editor.md) — hierarchy and viewport
