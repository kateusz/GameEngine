# Mesh LOD — Conceptual Introduction

## What Problem Does This Solve?

Instancing folds copies of one mesh into one draw. Static merging folds different idle meshes that share a material into one draw. Both still submit every triangle of the imported mesh. A tree that covers a few pixels on screen pays the same vertex work as the tree in front of the camera. Frustum culling and visibility zones remove objects. They do not thin the ones that remain.

## What the Feature Will Achieve

- Every imported submesh can carry coarser triangle lists, generated when the editor writes `{model}.mesh`.
- The lists point at the original vertices. Positions, UVs, normals, and tangents are not rewritten.
- Each frame the opaque walk picks the coarsest list whose geometric error still covers at most about one pixel of viewport height, scaled by the renderer’s bias.
- Copies that pick the same list still instance together.
- A submesh that has a coarser list stays on the instance path, including on the frame it picks the full list. Static merging is unchanged for cubes and for meshes that gained no coarser list.
- Color, directional shadows, and point-shadow faces use the level chosen from the game camera.
- The published player selects a level. It does not run the simplifier.
- A simplify pass that cannot cut triangles, or a file that has no levels, draws the full mesh.

## Benefits and Outcomes

| Outcome | Why it matters |
|---------|----------------|
| Fewer triangles far away | A forest and a unique prop both drop vertex work as they shrink on screen |
| One vertex buffer | Extra levels add index data, not a second copy of the attributes |
| Same draw for a shared level | Copies that agree still go out as one instanced submission |
| Shadows match the silhouette | The shadow map uses the same list as the color pass |
| Old siblings keep working | A version-1 `.mesh` is the full mesh and still loads |
| Player stays a reader | Simplification runs in the editor when `{model}.mesh` is written |

## Terminology

**Level** — One index list for a submesh, plus the geometric error of that list in mesh units. Level 0 is the imported index list and its error is zero.

**Error** — How far, in mesh units, the simplified surface may deviate from the original. Stored already scaled from the simplifier’s relative result. Each coarser level’s error is at least one and a half times the previous level’s error.

**Bias** — A positive finite multiplier on a `ModelRendererComponent`. One is the default. Two keeps the detailed list until the error would cover about half a pixel. A non-finite value, or a value at or below zero, is stored and used as one.

**Pixel threshold** — One pixel of the viewport’s height. It is an engine constant. It is not saved in the scene or in the mesh file.

**Simplifier** — The editor-side pass that builds coarser index lists while `{model}.mesh` is written from the source model, on the CPU, with Evergine.Bindings.MeshOptimizer.

## Principles

**Generate once, select every frame.** The simplifier runs while the sibling `.mesh` is written. The frame only chooses an index list that is already on the GPU.

**Share the vertices.** Simplification deletes triangles. Surviving vertices keep the attributes they were imported with.

**Measure the camera, not the light.** Distance is from the game camera to the nearest point of the submesh’s world box. A camera inside that box is distance zero, so the full list is used. Shadow passes reuse that choice. The light matrix still decides which casters are in the shadow frustum.

**One pixel, adjusted by field of view and resolution.** The allowed world error grows with distance and with how tall the frustum is at one unit, and shrinks with viewport height. An orthographic camera uses a distance of one, because zoom is already the frustum height (`2 * OrthographicSize`).

**Merging stays still.** A combined mesh is baked from full-detail vertices and must not rebuild when the camera moves. Any submesh that owns a coarser list never joins a merge group.

**Fail open.** A missing native library, a simplify step that cannot cut triangles, a version-1 file, or a broken level block still draws the full mesh. The rest of the model still loads.

## Design Approach

The simplifier calls `meshopt_simplifyWithAttributes` on each submesh, halving the triangle count each step, with the submesh border locked. It stops at four triangles, when a step cannot remove at least a quarter of the indices, or when the accumulated error exceeds the mesh size. Each kept list is optimized for the vertex cache and stored in `.mesh` version 2 beside the original indices. At draw time a pure function picks the level, the instance batch key includes that level, and the draw binds that index buffer after the vertex array. Pass order and the 2D/3D depth policy stay as they are. There is no new shader and no new system.

The editor host needs the native library shipped by Evergine.Bindings.MeshOptimizer 2026.10.1.20: Windows x64 and ARM64, Linux x64 and ARM64, macOS ARM64. An Intel Mac hits the missing-library path and writes a mesh with no coarser levels.
