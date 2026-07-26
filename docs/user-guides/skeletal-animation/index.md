---
title: Skeletal Animation
audience: Content authors using the GameEngine Editor
last_updated: 2026-07-26
screenshots: none — automated capture was not available for this guide
---

# Skeletal Animation

Bring animated characters into your game by importing skinned 3D models (FBX, glTF, or GLB). The Editor cooks them into engine-ready files and sets up playback controls so you can preview animations in Play mode and ship them in published builds.

## Who is this for?

Use this workflow when you have a **rigged character** or other **skinned mesh** — a model with bones and at least one animation clip. If your model is a plain static prop with no bones, the normal 3D import path still works; you do not need the skeletal playback component.

## What you get after import

When the Editor detects bones in your source file, it writes **three cooked files** next to each other under `assets/models/`:

| File | What it holds |
|------|----------------|
| `<name>.mesh` | Geometry, materials, and bone weights (mesh **version 2**) |
| `<name>.skel` | Skeleton hierarchy and bind pose |
| `<name>.anim3d` | One or more animation clips |

**Static models** (no bones) still produce a `.mesh` file only. Every `.mesh` file is now **version 2**. Older version 1 meshes from previous engine builds **no longer load** — you must re-import. See [Troubleshooting](./troubleshooting.md).

Textures are copied into `assets/models/textures/` as with any 3D import.

## Import a skinned model

### Before you start

- Open a **project** in the Editor (import is disabled without one).
- Your project should have an `assets/models/` folder (created automatically on first import).
- Supported source formats: **`.fbx`**, **`.glb`**, **`.gltf`**.

### Steps

1. Choose **File → Import 3D Model…**
2. Pick a single file, or a folder containing supported models.
   - On **Windows**, you pick a folder; the Editor imports every supported file in that folder (not subfolders).
   - On **macOS**, you can enter a file or folder path in the dialog.
3. If a `.mesh` with the same name already exists, confirm **overwrite** when prompted.
4. When import finishes, read the summary dialog. It reports how many sources succeeded, how many mesh parts were spawned, and any failures.

You do **not** choose “skinned” vs “static” — the Editor decides automatically. If **any** mesh in the file has bones, the skinned cook runs and creates the `.skel` and `.anim3d` companions.

### What appears in the scene

Import creates a **parent entity** (named after your file) with:

- **Transform** — move, rotate, or scale the whole character from here.
- **Skeletal Playback** — paths to the skeleton and animation clip, plus playback settings (see below).

Under that parent, one **child entity** is created per mesh part. Each child has:

- **Transform** — local position/rotation/scale from the source file.
- **Model Renderer** — points at the shared `.mesh` file and draws its assigned submesh slice.

> **Important:** Keep **Skeletal Playback on the parent**. Children only render mesh pieces; animation is driven from the parent. If you move playback to a child, that child’s mesh may animate but siblings will stay in bind pose.

## Skeletal Playback inspector

Select the **parent** entity and find the **Skeletal Playback** section in the Properties panel.

| Field | What it does |
|-------|----------------|
| **Skeleton** | Path to the `.skel` file (set automatically on import). Drag a `.skel` from the Content Browser to change it. |
| **Clip** | Path to the `.anim3d` file (set automatically on import). Drag a `.anim3d` to change it. |
| **Clip Name** | Which clip inside the `.anim3d` to play. Leave empty to use the **first** clip. If the file has several animations (e.g. Walk, Run, Idle), type the exact name here. |
| **Playing** | When **on** and you are in **Play mode**, the animation advances. When **off**, the character stays in **bind pose** (rest position). |
| **Loop** | When **on**, the clip repeats from the start after it ends. |
| **Speed** | Playback rate. `1` = normal speed; `2` = double speed; `0.5` = half speed. |
| **Time** | Current position in the clip (seconds). You can scrub to a specific moment; during Play mode, time advances automatically while **Playing** is on. |

### Edit mode vs Play mode

- **Edit mode** (normal scene editing): the viewport shows the character in **bind pose**, even if **Playing** is checked. The animation system only runs during Play mode (same idea as audio — you hear clips when the game is running, not while laying out the scene).
- **Play mode** (press Play in the Editor): with **Playing** enabled, the clip plays according to **Speed**, **Loop**, and **Time**.

To preview an animation, enter Play mode and turn **Playing** on.

## Working with multiple clips

A single `.anim3d` file can hold **multiple** clips from the source file. Version 1 plays **one clip at a time**:

1. Keep the same **Clip** path.
2. Change **Clip Name** to switch animations (or leave it empty for the first clip).
3. There is no blending between clips in v1 — switch clips by changing **Clip Name** or using separate playback setups.

## Publishing your game

Before you publish, the Editor checks that asset paths on components point to real files.

For any entity with **Skeletal Playback**:

- **Skeleton** and **Clip** must be set to non-empty paths.
- Both files must exist under your project’s `assets/` folder.

If a companion file is missing, **publish fails** with a clear list of broken paths. Fix the paths or re-import the model before publishing.

Static models without skeletal playback do not require `.skel` or `.anim3d` files.

## Tips

- **Re-import after engine updates.** Mesh format version 2 is required. Old `.mesh` files must be re-cooked — see [Troubleshooting](./troubleshooting.md).
- **Keep companions together.** The `.mesh`, `.skel`, and `.anim3d` for one character share the same base name (e.g. `hero.mesh`, `hero.skel`, `hero.anim3d`). Do not rename one without updating component paths.
- **Bone limit.** Models with more than **100 bones** fail at import with an error. Simplify the rig in your DCC tool if you hit this limit.
- **Raw FBX/glTF on Model Renderer.** The **Model Renderer** only accepts cooked `.mesh` paths. Pointing it at a `.fbx` or `.glb` shows a placeholder cube until you import through **File → Import 3D Model…**.

## Related documentation

- [Troubleshooting](./troubleshooting.md) — re-cook, missing files, bind pose issues
- [3D model loading (overview)](../../specs/3d-model-loading/introduction.md) — broader import and mesh format context
- [Re-cook checklist](../../specs/3d-model-loading/re-cook-checklist.md) — technical checklist for upgrading old projects
