# Static Mesh Merging — Conceptual Introduction

## What Problem Does This Solve?

Instancing already folds many copies of **one** mesh into a single submission. Unique scenery does not share a mesh: walls, rocks, and furniture that only share a material still pay one driver call each. Depth testing hides fragments; it does not hide the calls.

Those pieces often sit still. Paying instancing setup for a one-off mesh, or a draw per rock, is the wrong tool. Concatenating their triangles into one buffer, with the world transform already in the vertices, is the right one.

## What the Feature Will Achieve

- Idle opaque 3D pieces that share maps, tint, and material factors become one indexed draw.
- Sources stay in the scene. They are not deleted, hidden, or replaced by a baked asset.
- Picking and the selection outline still name the original entity. Vertex ids are the source ids.
- A piece that moved this frame, has no previous transform, or carries a moving 2D body leaves the group and uses today's instance path.
- The first frame an entity is seen is treated as motion. Merge starts on the next still frame.
- Camera motion does not rebuild the buffer. Frustum tests the group's box, not the sources, after the group exists.
- Color, directional shadow, and point-shadow faces share the world-space buffer. Each pass still culls that box against its own volume.
- A group of one stays on the instance path. Merge is for two or more members.
- Editor and Runtime use the same walk. Nothing is saved besides the scene that already exists.
- Stats count groups, baked vertices, and rebuilds so a still scene can be checked without a GPU debugger.

## Benefits and Outcomes

| Outcome | Why it matters |
|---------|----------------|
| Fewer calls on mixed static clutter | Different meshes, one material, one draw |
| Sources remain entities | Hierarchy, scripts, and gizmos keep working |
| Picking stays correct | The editor still highlights the rock you clicked |
| Idle heuristic | Authors do not tag static; a moving crate drops out by itself |
| Shared GPU mesh across passes | One upload, several tests against different frusta |
| Visible effect | Rebuild count stays zero while the camera orbits a still room |

## Terminology

**Idle** — This frame's world matrix equals the matrix stored from the previous frame for that entity.

**Member** — One entity's cube or submesh that passed visibility and zone filters, is idle, and is not a dynamic or kinematic 2D body.

**Material key** — The maps, tint, metallic, roughness, ambient occlusion, emissive, cutout, and double-sided flag that must match for members to share a draw.

**Group** — Every idle member with one material key. Geometry may come from different files.

**Combined mesh** — One indexed mesh whose vertices are already in world space. Drawn with an identity transform.

**Dirty** — The group's members, worlds, source meshes, or key changed since the combined mesh was uploaded.

**History** — Last world matrix per entity, kept next to the pipeline, not in the scene file.

## Principles

**Instancing stays for copies.** Merge is not a replacement. Identical moving trees still instance. Merge is for different still geometry that happens to look like one material.

**Do not frustum sources before baking.** A combined mesh built from only on-screen pieces would rebuild whenever the camera moved. Bake the idle set that survived show/hide and zones. Cull the group's box in the pass that is drawing.

**World space on the CPU.** The vertex layout does not grow a color channel. Matching tint lives in the key. Positions and tangent-space vectors are transformed once at rebuild.

**No scene flag.** Participation is derived. Load and save are unchanged.

**Fail open.** A group that cannot upload draws its members on the instance path that frame. Other groups still draw.

**Opaque only.** Sprites and subtextures stay on the 2D batch. Blended surfaces are not concatenated.

## Design Approach

The opaque walk already queues submeshes into instance batches. After visibility and zones it splits: motion and ineligible bodies go to that queue; idle members go to a material group. On dirty, the CPU concatenates world-space vertices and uploads a combined mesh. Each pass draws that mesh if its box intersects the pass volume. Cubes may join a group when the key matches. There is no sidecar, no editor bake button, and no new component.
