# Project Setup

Get from zero to a running editor with a project open.

## Launching the Editor

Navigate to the `Editor` directory and run the editor using the .NET CLI:

```bash
cd Editor && dotnet run
```

On first launch, the editor displays the start screen. From there you can create a new project, continue without a project, or reopen a recent project.

## Creating a New Project

1. Click **New Project** on the start screen, or use the **File** menu and select **New Project**.
2. Enter a project name. Allowed characters are alphanumeric characters, spaces, dashes, and underscores.
3. Enter the parent directory path where the project folder will be created (text input — not a file browser).
4. Click **Create**.

The engine generates the following directory structure inside the new project folder:

```
ProjectName/
├── assets/
│   ├── scenes/
│   ├── textures/
│   ├── scripts/
│   └── prefabs/
```

## Opening Existing Projects

To open a project that already exists on disk:

1. Open the **File** menu and select **Open Project**.
2. Enter the root directory path of the project (the folder that contains the `assets/` subdirectory).
3. Confirm the selection.

The editor will load the project and display its contents in the Content Browser.

## Recent Projects

The **Recent Projects** panel on the start screen lists previously opened projects for quick access. **Double-click** any entry to reopen that project directly. Use **File → Show Recent Projects** or **File → Recent Projects** from the menu bar when a project is already open.

## Project Directory Structure

| Path | Description |
|---|---|
| `assets/scenes/` | Scene files in the `.scene` format (JSON-based) |
| `assets/textures/` | Image assets (PNG, JPG) |
| `assets/scripts/` | C# game scripts, compiled at runtime |
| `assets/prefabs/` | Reusable entity templates in the `.prefab` format |

All paths within the engine are resolved relative to the project root, so the project folder can be moved or shared without breaking asset references.

## Next Steps

With a project open, you are ready to start building. See [Scene Editor](scene-editor.md) to learn how to create and populate scenes.
