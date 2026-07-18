# Content Browser

Browse and manage your project's assets.

## Overview

The Content Browser panel displays the files inside your project's `assets` directory. A **directory tree** on the left shows folders; single-click a folder to navigate. The current path is shown at the top of the panel so you always know where you are in the hierarchy.

To navigate into a folder from the grid, double-click it. When you are inside a subdirectory, a back arrow button (`<-`) appears at the top of the panel — click it to move up to the parent directory. You cannot navigate above the root `assets` directory.

Files are displayed in a grid using an icon appropriate for each asset type. The grid column count adjusts automatically to fit the available panel width.

## Supported Asset Types

| Extension | Type | Display |
|-----------|------|---------|
| `.png`, `.jpg` | Texture | Thumbnail (actual image preview) |
| `.fbx`, `.gltf`, `.glb` | Model | File icon (labeled Type: Model) |
| `.wav`, `.ogg` | Audio Clip | File icon |
| `.scene` | Scene | File icon |
| `.prefab` | Prefab | File icon (same as other data files) |

Any file type not listed above also displays a generic file icon.

## Drag and Drop

Assets can be dragged from the Content Browser directly onto component fields in the Properties panel. When a drag begins, a small preview tooltip shows the file name and type.

All drop targets accept only files with matching extensions — dropping an incompatible file type onto a target has no effect.

| Drag source | Drop target | Result |
|-------------|-------------|--------|
| `.png` / `.jpg` texture | SpriteRendererComponent texture field | Assigns the texture |
| `.fbx` / `.gltf` / `.glb` model | ModelRendererComponent model field | Assigns `ModelPath` |
| `.wav` / `.ogg` audio file | AudioSourceComponent audio clip field | Assigns the audio clip |
| `.prefab` prefab file | Scene Hierarchy panel (onto existing entity) | Applies prefab data to that entity |
| `.scene` scene file | Viewport | Opens the scene |

The Content Browser passes the asset's path relative to the `assets` directory as the drag-and-drop payload. Drop targets resolve the full path by combining this relative path with the project's assets root.

## Creating Assets

### Context Menu (Directory Tree)

Right-click any folder in the left-side directory tree to open a context menu with three options:

- **Add Script** — creates a new `ScriptableEntity` script in `assets/scripts/`
- **Add Component** — creates a new `IGameComponent` class in `assets/scripts/`
- **Add System** — creates a new `IGameSystem` class in `assets/scripts/`

These options are enabled only when you right-click the `scripts` folder or one of its subfolders. On other folders (textures, scenes, etc.) the menu items appear grayed out. A name prompt opens when you choose an action; script names must match `^[a-zA-Z][a-zA-Z0-9_]*$` (letters, digits, underscore; must start with a letter). The new file is compiled immediately but is not attached to any entity.

**Scripts (Properties panel)**

Scripts can also be created from the NativeScriptComponent in the Properties panel. With an entity selected, expand the **Script** section (or use the **Add Script** placeholder) and click **Create New Script**. Enter a valid C# identifier as the script name and confirm. The engine generates a script template and saves it to `assets/scripts/`. The script is immediately compiled and attached to the entity.

You can also click **Add Existing Script** to attach a previously created script to the selected entity.

**Scenes**

Use **Ctrl+N** to create a new scene (a name prompt appears). Use **Ctrl+S** to save the current scene. Both actions are also available in the **Scene...** menu in the menu bar.

## Thumbnails and Icons

- **Texture files** (`.png`, `.jpg`): The actual image is loaded and rendered as a thumbnail. Thumbnails are cached after the first load so repeated rendering does not reload from disk.
- **Known folders** (`scenes`, `scripts`, `textures`, `prefabs`, etc.): Display folder-specific icons in the tree.
- **Directories**: Display a folder icon.
- **All other files** (including `.prefab` and `.scene`): Display a generic file icon.

## Next Steps

- [Component Inspector](component-inspector.md) — view and edit component properties, including drag-and-drop targets
- [3D Rendering](../concepts/3d-rendering.md) — placing models and lights
- [Roadmap](../roadmap.md) — planned features
