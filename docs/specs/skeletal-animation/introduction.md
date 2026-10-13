# Skeletal Animation — Conceptual Introduction

## What Problem Does This Solve?

A mesh is stored in the pose it was exported in, and every frame draws those vertices with the entity's world matrix. A character file can contain a skeleton and named clips, but the draw never reads them. Joints, weights, and clips are dropped at import.

Playing a clip has to deform that mesh. Moving the entity only slides the whole model.

## What the Feature Will Achieve

- A glTF, GLB, or FBX file with a skeleton plays a named clip on the mesh entity. The clip moves the joints, and the vertex shader deforms the mesh.
- The scene saves the clip name and whether the clip loops. Loading the scene starts that clip at the beginning. Playback speed is one second of clip per second of frame time.
- An empty clip name, an unknown name, or a clip with no duration shows the rest pose. The unknown name stays in the scene, so a later cook that adds the clip starts playing it.
- Looping wraps. Playing once holds the last keys.
- Switching to a different clip name starts that clip from its first keys. Setting the same name again leaves the clock where it is.
- A file with no skin draws exactly as it does today, including instancing and hierarchy unpack.
- A file with a skin stays on one entity. Every submesh uses the joint palette and the entity's placement.
- The same palette is used for the color image, the emissive add, both shadow maps, and the view-normal pass. The 2D pass and the depth-test rule are unchanged.
- The editor previews the clip while editing. Play mode and the standalone player run the same pose update from the system list.

## Benefits and Outcomes

| Outcome | Why it matters |
|---------|----------------|
| Clips from the art file | The names exported with the model are the names the entity can play |
| Deformation in the existing draw | The mesh stays one submission on the forward path the engine already has |
| Shadows match the body | A limb that moves also moves in the shadow and in the ambient-occlusion normals |
| Small scene file | The skeleton and the keys stay in the cooked model. The entity stores a name and a loop flag |
| Static props unchanged | A crate with no skin never pays for a joint palette |
| One importer | glTF, GLB, and FBX all arrive through Assimp and the cooked sibling |

## Terminology

**Skin** — The skeleton plus the weights that tie vertices to joints. A model has one skin. A second skin in the file is ignored, and those pieces draw as unskinned geometry.

**Joint** — One bone in that skeleton. It has a parent (or none), a rest pose, and an inverse-bind matrix. Joints are not entities.

**Inverse bind** — The matrix that takes a vertex from the mesh's stored pose into the joint's local space at rest. Combined with the joint's current world matrix, it poses the vertex.

**Rest pose** — The skeleton as stored on the asset, before a clip is applied. The mesh vertices are already in that pose.

**Palette** — One matrix per joint for the current pose, ready for the vertex shader. At most 128 joints.

**Clip** — Named keys that move joints over time. Translation and scale blend linearly. Rotation uses a spherical blend. A stepped key holds its value. A cubic-spline key is read as a linear blend between its endpoints, and its tangents are ignored.

**Influence** — One joint index and one weight on a vertex. A vertex has at most four. A vertex with none stays on the position stored in the mesh.

**Playback** — The entity's choice of clip and loop flag, plus the clock and the palette, which exist only while the scene is running.

## How a Clip Becomes a Pose

Import reads the skin and the clips through Assimp. The editor writes them into the cooked sibling beside the source file. The player reads that sibling and does not open the source. A sibling from before this feature is stale. The editor cooks a new one. The player treats a stale or broken sibling the way it already treats a model it cannot read: the entity draws the stand-in cube.

Each joint's rest pose is translation, rotation, and scale. A joint that cannot be represented that way, a skeleton with more than 128 joints, a joint whose parent link cycles or points past the list, or a non-finite inverse bind causes import to drop the skin. The mesh is still written, without weights, and draws in the pose stored on the vertices.

While the scene runs, the clock moves by the frame delta. The pose is built by turning each joint's keys into a local transform, then walking parents from the root of the file. The palette is that pose multiplied by the inverse bind. A correct rest pose leaves the stored vertices where they are. The walk result is kept on the entity and reused by every 3D pass that moves the mesh.

The draw places every submesh of a skinned file with the entity's world matrix after the palette. It does not also apply a mesh-node transform, and it does not shift the mesh by the static-mesh pivot. Those draws are one at a time. They do not join the batch that merges identical static meshes.

The box used to reject off-screen meshes is the box of the stored vertices. A pose that reaches outside that box can disappear at the edge of the view.

This feature does not crossfade clips, move the entity from the clip, blend shapes, or solve inverse kinematics. Those are separate work.

## Patterns and Principles

The skeleton is part of the model asset. The scene remembers which clip to play. Sampling is a system that runs before the frame is drawn. The draw coordinator only binds the palette that sampling already wrote.

Edit mode does not run systems, so the editor viewport calls that same sampling before it draws. Play mode and the player do not call it a second time. Their system list is the only caller.

OpenGL stays behind the renderer. The palette crosses to the GPU as one uniform buffer, updated on the thread that already owns the context. The vertex shader does the deformation.
