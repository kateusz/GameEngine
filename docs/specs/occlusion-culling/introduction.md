# Occlusion Culling — Conceptual Introduction

## What Problem Does This Solve?

Frustum culling drops opaque cubes and meshes that miss the camera volume. It still submits everything inside that volume. A crate fully hidden behind a wall costs a color draw, and the forward shader still runs on pixels the wall already covered.

The depth test hides those pixels. It does not cancel the draw. Visibility zones only apply when an author marked a room and the camera is inside it. They do not notice that one mesh stands in front of another.

## What the Feature Will Achieve

- On the color pass, a loose cube or submesh is skipped when its box is completely behind surfaces already found in front of it. A merged group is skipped the same way, once, on the group box.
- The test may draw an object that was actually hidden. It must not drop an object that is still visible, including one that only peeks past an edge, one that touches the occluder, and one that crosses the near plane.
- Every opaque mesh can hide other meshes. Nothing new is stored in the scene.
- Alpha-cutout surfaces never hide anything. Their triangles do not describe the holes.
- Directional cascades and point-shadow faces keep today's filters: frustum, caster distance, and a lamp inside its caster. They do not run this test.
- Sprites stay a depth-off batch before the 3D sequence. Pass order stays shadows, then one color pass.
- The editor viewport and the published player share the result, because both call the same scene draw. A skipped mesh is absent from the color image, so it is absent from picking and the selection outline as well.
- The color-pass reject count is reported beside the frustum count.

## Benefits and Outcomes

| Outcome | Why it matters |
|---------|----------------|
| Fewer color draws | A wall or building can remove the meshes behind it before they are batched |
| No holes | A partial footprint, a touching box, or a bad depth draws |
| No scene authoring | Existing transforms, bounds, and triangles are enough |
| Shadows unchanged | Light views do not pay for a second raster |
| Same picture in the player | The filter sits in the shared scene draw, not in an editor pass |
| Visible effect | The stats line shows how many color draws this test removed |

## Terminology

**Occlusion grid** — A small CPU image of depth for the color camera only. Each cell is either uncovered or holds one depth. The grid is discarded as a decision tool at the end of the color pass. It is not a GPU texture.

**Occluder** — An opaque, non-cutout cube or mesh, already inside the camera frustum, whose on-screen box covers at least 2% of the view, and whose triangles are still available on the CPU. Merged groups qualify from the group box and contribute each solid member's triangles.

**Occludee** — Any loose cube, loose submesh, merged group, or single merged member that the color pass would otherwise draw. Small meshes and cutout meshes are occludees even though they are not occluders.

**Full-pixel coverage** — A grid cell is written only when the entire cell sits inside a triangle. A triangle that only crosses a cell leaves that cell uncovered.

**Farthest vertex depth** — The largest post-divide depth among a triangle's three vertices. That is the depth written for a cell the triangle fully covers. Across several triangles, the cell keeps the nearer of those written depths.

**Nearest corner depth** — The smallest post-divide depth among the eight corners of a box that is entirely in front of the near plane.

**Uncovered cell** — A cell no accepted triangle fully covered. A box that overlaps one cannot be skipped.

## Principles

**Same frame as the draw.** The grid is built from this frame's transforms and this frame's camera. A mesh that moved, and a camera that turned, are visible in the same picture that is about to be drawn.

**The box is the occludee, the triangles are the occluder.** Hiding a mesh because its box overlaps an occluder's box would punch holes: the box is larger than the silhouette. Hiding it only when the whole box sits behind real triangles does not.

**Failure draws.** A missing box, a non-finite depth, a triangle that meets the near plane, or a camera matrix that cannot size the grid means the color pass draws that object. The picture stays whole.

**Merged meshes stay merged.** Members are not removed from a combined mesh because the camera moved. The group is submitted or skipped as one draw. Orbiting the camera does not rebuild it.

**One raster per frame.** Shadow views are different cameras. Repeating the grid for each cascade and each cubemap face is a second job, so those passes do not do it.

## Design Approach

The color invocation of the opaque walk collects what hierarchy, visibility zones, and the frustum already kept. It rasterizes the occluders into the grid, front to back, and then skips draws whose boxes are fully behind that grid. The rasterizer is a CPU type beside the frustum test. The renderer abstraction gains no method, and OpenGL is not asked for a query or a depth readback.
