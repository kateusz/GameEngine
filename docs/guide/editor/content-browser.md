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
| `.obj` | 3D Model | File icon |
| `.anim` | Animation | File icon |
| `.scene` | Scene | File icon |
| `.prefab` | Prefab | Special prefab icon |

Any file type not listed above also displays a generic file icon.

## Drag and Drop

Assets can be dragged from the Content Browser directly onto component fields in the Properties panel. When a drag begins, a small preview tooltip shows the file name and type.

All drop targets accept only files with matching extensions — dropping an incompatible file type onto a target has no effect.

| Drag source | Drop target | Result |
|-------------|-------------|--------|
| `.png` / `.jpg` texture | SpriteRendererComponent texture field | Assigns the texture |
| `.wav` / `.ogg` audio file | AudioSourceComponent audio clip field | Assigns the audio clip |
| `.obj` mesh file | MeshComponent mesh field | Assigns the mesh |
| `.prefab` prefab file | Scene Hierarchy panel | Instantiates the prefab as a new entity |

The Content Browser passes the asset's path relative to the `assets` directory as the drag-and-drop payload. Drop targets resolve the full path by combining this relative path with the project's assets root.

## Creating Assets

**Scripts**

Scripts are created from the NativeScriptComponent in the Properties panel, not directly from the Content Browser. With an entity selected, expand the Script section in the Properties panel and click **Create New Script**. Enter a valid C# identifier as the script name and confirm. The engine generates a script template and saves it to `assets/scripts/`. The script is immediately compiled and attached to the entity.

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
- [Animation Timeline](animation-timeline.md) — create and edit `.anim` animation clips
