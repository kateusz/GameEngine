# 3D Import — Lights & Cameras (scene setup)

## Problem

**File → Import 3D Model…** cooks meshes (and skinned companions) and spawns a parent + mesh children, but Assimp cameras and lights in the source file are ignored. Authors must hand-place `DirectionalLightComponent` and `CameraComponent` after every import, even when the DCC scene already authored them.

## Goal

Map **what the engine already supports** onto import spawn so a typical Blender/glTF/FBX scene comes in with usable directional light(s) and camera(s). Not full lighting parity with the file.

## Non-goals

- Point / spot / Assimp ambient lights (log + skip)
- New light component types or multi-light / clustered shading
- Persisting lights/cameras inside `.mesh` or a sidecar
- Changing `.mesh` VERSION or runtime `ModelFactory`
- Deriving `DirectionalLightComponent.Direction` from entity transform (existing component stores world-space direction; parent rotation does not rotate it — accepted v1 limit)
- Import UI checkbox (always on when spawning into the active scene)
- Empties, non-camera/non-light nodes beyond existing mesh parts
- Re-spawn of lights/cameras without re-import

## Decisions (from brainstorm)

| Topic | Choice |
|--------|--------|
| Purpose | Scene-setup from DCC onto existing ECS components |
| Multiplicity | Spawn all mappable directionals + cameras; engine still uses first directional / primary camera |
| Non-directional lights | Skip point/spot/ambient with log |
| Primary camera | `Primary=true` on first imported camera only if the active scene has no primary yet; never clear existing primary |
| Hierarchy | Children of the import parent (same as meshes) |
| Architecture | Extract DTO at cook (same Assimp pass); spawn at import; nothing written to disk for extras |

## Architecture

```text
Assimp cook (existing pass)
  → meshes → .mesh (+ optional .skel / .anim3d)
  → directional lights + cameras → in-memory DTOs only
SpawnHierarchy
  → parent
  → children: mesh parts | DirectionalLight | Camera
```

- Assimp stays cook-only; runtime `.mesh` load unchanged.
- Extract lives on the cook path (`AssimpModelImporter` and/or thin helper; results surfaced through `MeshCreator` split/skinned result).
- Spawn lives in `Import3DModelBatch.SpawnHierarchy` (editor).

## Data mapping

### Directional lights (`aiLightSource_DIRECTIONAL` only)

- Entity: child of import parent.
- `TransformComponent`: local-to-root via existing `ImportSpawnTransform` (same unit/cm rules as mesh parts).
- `DirectionalLightComponent.Direction`: Assimp light direction expressed in import-root space (pipeline treats it as world-space; with identity parent at spawn this matches authored intent).
- `DirectionalLightComponent.Color`: Assimp color × intensity (non-negative RGB).
- Entity name: Assimp light name, fallback `DirectionalLight`.
- Iterate Assimp `mLights` in index order; spawn every directional that resolves to a node.

### Cameras (all `aiCamera`)

- Entity: child of import parent + `TransformComponent` from camera node (same local-to-root path).
- `CameraComponent`: `ProjectionType = Perspective`. Assimp `mHorizontalFOV` is horizontal radians; convert to the engine’s vertical `PerspectiveFOV` using the camera’s aspect (`PerspectiveFOV = 2 * atan(tan(hFov/2) / aspect)`), with `AspectRatio` set from Assimp when available (else component default). Near/Far from file.
- Invalid FOV or Near/Far ≤ 0 → component defaults.
- **Order:** iterate Assimp `mCameras` / `mLights` in file index order (deterministic). “First imported camera” means the first successfully spawned camera in that order.
- `Primary`: that first camera gets `true` only if active scene has no `CameraComponent` with `Primary == true`; all other imported cameras `false`. Existing primaries untouched.
- Entity name: Assimp camera name, fallback `Camera`.

### Ambient

- Do not create `AmbientLightComponent` from the file. Scene ambient stays as-authored by the user.

## API / data flow

### Cook

- Introduce small DTOs, e.g. `ImportedDirectionalLight` (Name, Translation/Rotation/Scale or matrix, Direction, Color) and `ImportedCamera` (Name, transform, PerspectiveFOV, Near, Far).
- Extend `MeshCreator` split/skinned success payloads (and editor `SourceImport`) with light/camera lists (empty lists are valid).
- `.mesh` writer/reader: **no change**.

### Spawn

- After mesh children, spawn light and camera children under the same parent.
- If there are no mesh parts but lights/cameras exist: still create parent + extras (scene-setup-only sources should not no-op).
- If everything is empty: keep current “No parts” behavior.
- Import summary note: e.g. `+ N directional light(s), M camera(s)` and skipped non-directional count when useful.

### UI

- No new Import popup controls in v1.

## Error handling

| Case | Behavior |
|------|----------|
| Point / spot / ambient Assimp light | Log + skip; mesh cook continues |
| Light/camera with missing node | Log + skip |
| Exception while extracting extras | Log; mesh cook/spawn without extras |
| Bad camera projection params | Fallback to `CameraComponent` defaults |
| Scene already has primary camera | Imported cameras all `Primary=false` |

## Testing

Minimal, follow existing `Import3DModelSceneSpawnTests` style:

1. Spawn directional + camera as children with expected components and transforms.
2. Primary policy: empty scene → first camera primary; scene with existing primary → imported cameras false.
3. Skip: fixture with point/spot → no directional entities from those sources (and/or skipped count).
4. Extract unit: small glTF/FBX (or crafted Assimp-facing fixture) with one directional + one camera → DTO fields sensible.

No shader / GraphicsTests changes.

## Success criteria

- Importing a source that contains a directional light and a camera produces corresponding child entities under the import parent.
- Play/viewport shading can use the imported directional when it is the first in the scene (existing “first light” rule).
- Imported camera becomes primary only when the scene had none.
- Point/spot/ambient in the file do not fail the import and do not invent unsupported components.
- Re-import still overwrites cooked meshes as today; lights/cameras are re-spawned only via a new import spawn into the scene (no asset-side persistence).

## Out of scope follow-ups (not this work)

- Transform-driven directional direction / light gizmos that rotate with parent
- Point/spot components + multi-light forward path
- Optional import checkbox; Content Browser “spawn lights from mesh”
- Writing lights/cameras into cooked assets
