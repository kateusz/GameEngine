# Frustum Culling — Conceptual Introduction

## What Problem Does This Solve?

Every opaque cube and model is submitted to the GPU twice whenever a directional light is on: once into the shadow map, once into the color image. Nothing asks whether that object meets the volume being drawn. A crate behind the camera still costs a color draw. A crate far to the side of the shadow box still costs a shadow draw.

Depth testing hides the pixels. It does not cancel the draw. The CPU still walks the entity, and the GPU still receives the triangles.

## What the Feature Will Achieve

- Each submesh, and each cube, is skipped when its box lies completely outside the volume of the current pass.
- The color pass uses the camera's view volume. The directional shadow pass uses the light's box, the same box the shadow fit already builds. An object outside the camera can still cast a shadow if it sits inside that light box.
- An object that crosses the boundary is drawn. The test is allowed to draw a few extra objects. It is not allowed to drop one that is even partly inside.
- Cubes, loaded models, and the fallback cube used when a model fails to load all take the same test. Sprites and subtextures are unchanged.
- If the bounds or the volume cannot be trusted, the object is drawn. A bad matrix never produces a hole in the picture.
- The once-per-second draw log reports how many instances each pass rejected.

## Benefits and Outcomes

| Outcome | Why it matters |
|---------|----------------|
| Fewer color draws | Objects outside the camera no longer fill a batch |
| Fewer shadow draws | Objects outside the light box no longer fill the depth pass |
| Shadows from off-screen casters | The shadow pass does not reuse the camera volume, so a caster just outside the frame still darkens the ground |
| No popping | A partial overlap is always drawn |
| Safe failure | Missing bounds or a broken matrix keeps today's behavior for that object |
| Visible effect | The existing log shows the reject count, so a scene that should cull can be checked without a GPU debugger |

## Terminology

**View volume** — The pyramid of space the camera can see, from the near plane to the far plane, bounded by the left, right, top, and bottom edges of the frame.

**Light box** — The orthographic box the directional shadow fit already wraps around the camera volume, shortened to the shadow distance. It is not the camera pyramid. It is the volume the shadow map actually covers.

**Pass matrix** — The clip matrix of the pass being drawn. For color, that is the camera view-projection. For the directional shadow, that is the light view-projection.

**Local box** — The axis-aligned min and max of a mesh's positions in the mesh's own space, before the entity's transform. Computed once, while the positions still exist on the CPU.

**Oriented box** — Those eight corners after the entity's world matrix. Rotation and negative scale move the corners. The test uses the corners, not a new axis-aligned box in world space. A long mesh turned 45 degrees therefore stays thin.

**Clip plane** — One face of the pass volume, recovered from the pass matrix. Six of them: left, right, bottom, top, near, far. The inside of the volume is the non-negative side of every plane.

**Conservative rejection** — The only reject is "every corner is outside one plane," with a hair of slack so rounding cannot pop a corner that sits on the face. Overlap, a plane that cuts through the box, and a box that surrounds a corner of the volume without placing a corner inside it all count as visible.

## Principles

**Same matrix as the draw.** The walk already draws every submesh with the entity world matrix and ignores the model's node transforms. The test uses that same matrix. Applying a node transform only in the test would hide geometry the GPU still draws, or the reverse.

**One test, two volumes.** Color and shadow do not share a visibility result. Each pass builds planes from its own matrix and runs the same corner test.

**Bounds live with the mesh.** Positions are discarded after upload. The local box has to be stored then, or it cannot be recovered later. The unit cube uses the same half-extent the cube mesh is built from, so the drawn cube and the tested cube cannot drift apart.

**Failure draws.** An empty mesh, a non-finite corner, or a matrix that does not yield six planes means "do not reject." The picture stays correct. The optimization simply does not apply.

**Before the batch.** A rejected instance never enters the instance list. Culling a batch after it is built would still upload the hidden transforms.

## Design Approach

The work is a CPU filter in front of the existing opaque walk. Shaders, instancing, and the shadow fit stay as they are. The filter does not know about sprites, point-light shadows, or the model scene graph. It does not draw debug boxes, and it has no inspector toggle. The shadow distance limit stays where it is: an object inside the camera but past that distance is drawn in color and omitted from the shadow pass, because it was already outside the shadow map.
