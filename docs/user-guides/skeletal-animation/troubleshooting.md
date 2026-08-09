---
title: Skeletal Animation — Troubleshooting
audience: Content authors using the GameEngine Editor
last_updated: 2026-07-26
screenshots: none — automated capture was not available for this guide
---

# Skeletal Animation — Troubleshooting

Common problems when importing, previewing, or publishing skinned models.

## Mesh version 2 — you must re-import old models

The engine only loads **`.mesh` version 2** files. Older version 1 meshes from previous builds **will not load**.

### Symptoms

- A model that used to work now shows a **unit cube** in the viewport.
- The console mentions an **unsupported mesh version** and tells you to **re-import**.
- Import or load errors refer to **VERSION 1** vs **VERSION 2**.

### What to do

1. Find the **original source file** (FBX, glTF, or GLB) — or export it again from your 3D tool.
2. Use **File → Import 3D Model…** and import into `assets/models/`.
3. When prompted, **overwrite** the old `.mesh` file (or delete the stale `.mesh` first).
4. Open scenes that used the old mesh and confirm **Model Renderer → Model Path** still points at the new `models/<name>.mesh`.
5. For skinned characters, confirm `.skel` and `.anim3d` were written beside the mesh.

There is **no automatic converter** — re-import is the only fix.

### Huge mesh / `VERTEX_COUNT exceeds max`

If the console shows **`VERTEX_COUNT … exceeds max`** (or the viewport draws a unit cube after a skinned import), the cooked `.mesh` has too many vertices — often because an older import did not weld duplicate corners.

**Fix:** delete the stale `models/<name>.mesh`, `.skel`, and `.anim3d`, then **re-import** the source FBX/glTF. A healthy character is usually thousands of vertices, not millions.

### Transform scale shows 100,100,100 after FBX import

FBX files (Mixamo, Blender, etc.) often store **centimeters**. Import converts oversized cooks to **meters** (×0.01 on mesh, skeleton, and clips) and spawns with **Scale 1,1,1**.

If the model still looks huge with scale 1:1:1, **re-import** after rebuilding the Editor — old `.mesh` files keep centimeter vertex data until re-cooked.

---


Work through every `.mesh` in your `assets/models/` folder. A step-by-step checklist lives in the [re-cook checklist](../../specs/3d-model-loading/re-cook-checklist.md).

---

## Missing `.skel` or `.anim3d` companions

Skinned characters need **three** files with the same base name:

```text
assets/models/character.mesh
assets/models/character.skel
assets/models/character.anim3d
```

### Symptoms

- **Publish fails** with missing path errors for Skeleton or Clip.
- Animation does not play even in Play mode (playback has nothing to read).
- You moved or renamed one file but not the others.

### What to do

1. In the Content Browser, check that all three files exist for that character.
2. If any are missing, **re-import** the source model. Skinned import creates all three together.
3. On the parent entity, open **Skeletal Playback** and verify:
   - **Skeleton** → `models/<name>.skel`
   - **Clip** → `models/<name>.anim3d`
4. Paths are **project-relative** (e.g. `models/hero.skel`), not absolute disk paths.

Static props only need `.mesh` — no companions required.

---

### Animation plays but character explodes / spikes

**Cause (fixed in engine):** Assimp FBX often leaves **inverse-bind matrices in centimeters** while mesh vertices are already in **meters**. Playing animation then multiplies incompatible units.

**What to do:** **Re-import** the character (File → Import 3D Model) so `.skel` inverse binds are harmonized with mesh units. Old `.skel` files keep the bad offsets until re-cooked.

---


**Bind pose** is the model’s rest position — arms and legs in the default rig layout, as exported from your 3D app.

### Expected bind pose behavior

The character **should** show bind pose when:

- You are in **edit mode** (not playing the scene), **even if Playing is checked**.
- **Playing** is **off** in Play mode.
- **Skeletal Playback** is missing from the parent (only mesh children remain).

This is normal. Animation only evaluates while the scene is **running in Play mode** with **Playing** turned on.

### Checklist if animation still does not play in Play mode

| Check | Fix |
|-------|-----|
| **Playing** is off | Turn **Playing** on. |
| Still in edit mode | Press **Play** to enter Play mode. |
| **Skeleton** or **Clip** path is empty | Re-import, or drag the correct `.skel` / `.anim3d` onto the fields. |
| Wrong **Clip Name** | Leave empty for the first clip, or match the name exactly (case-sensitive). |
| **Speed** is `0` | Set **Speed** to a positive value (default is `1`). |
| Playback on a child, not the parent | Move or duplicate **Skeletal Playback** onto the **parent** entity that owns the mesh children. |
| Source had no animation | Re-export from your DCC tool with at least one clip, then re-import. |
| Import failed silently | Read the import summary dialog; fix bone-count or file errors and try again. |

---

## Model shows a cube instead of the character

| Cause | Fix |
|-------|-----|
| **Model Path** points at `.fbx`, `.glb`, or `.gltf` | Import via **File → Import 3D Model…** and set **Model Path** to the cooked `.mesh`. |
| **Model Path** is empty | Assign `models/<name>.mesh` on each child’s **Model Renderer**. |
| Stale or corrupt `.mesh` | Re-import from source. |
| Version 1 `.mesh` | Re-import (see [Mesh version 2](#mesh-version-2--you-must-re-import-old-models)). |

---

## Import failed or partial import

### “More than 100 bones”

The engine supports up to **100 bones** per skeleton. Reduce bone count in Blender, Maya, or your export preset, then re-import.

### Duplicate destination name

Two source files in one batch would both write `same-name.mesh`. Rename one source file or import them separately.

### Overwrite not confirmed

If `hero.mesh` already exists, the import stops until you confirm overwrite. Run import again and accept the overwrite prompt.

### No active scene

Meshes are still written to disk, but **no entities are spawned**. Open a scene and import again, or manually add entities and assign paths from the Content Browser.

---

## Publish validation errors

Publish scans components for asset paths (`Model Path`, `Skeleton`, `Clip`, etc.) and checks that files exist.

**Skinned setup:** both **Skeleton** and **Clip** must be set and the files must be present under `assets/`.

**Fix:** re-import the character, or correct paths in the inspector, then publish again.

---

## Quick reference

| I want to… | Do this |
|------------|---------|
| Fix old meshes after an engine update | Re-import every model from FBX/glTF/GLB |
| Get skeleton + clip files | Import a rigged model; companions are created automatically |
| Preview animation | Play mode + **Playing** on |
| Hold a pose while editing the scene | Stay in edit mode, or turn **Playing** off |
| Switch walk → run | Change **Clip Name** (same **Clip** file) |
| Ship a character in a build | Ensure `.mesh`, `.skel`, and `.anim3d` exist and paths are set before publish |

For the full technical re-cook procedure, see the [re-cook checklist](../../specs/3d-model-loading/re-cook-checklist.md).
