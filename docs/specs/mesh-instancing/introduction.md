# Mesh Instancing — Conceptual Introduction

## What Problem Does This Solve?

Each imported submesh is sent to the GPU on its own. The driver cost of that call is paid again for every copy, in the color image and in the shadow map. A forest of the same tree pays it thousands of times. The triangles are cheap next to the calls.

Depth testing does not merge those calls. Two crates that share a mesh still become two submissions.

## What the Feature Will Achieve

- Copies of one imported mesh are drawn together. One submission carries every visible copy of that mesh.
- A copy may differ in placement, picking id, tint, and metallic, roughness, and ambient occlusion. Those differences stay inside the submission. They do not split it.
- The color image and the shadow map each build their own groups. A crate the camera cannot see can still cast a shadow. A crate outside the light box can still appear in color.
- One copy of a mesh uses the same submission path as a thousand copies.
- Cubes, sprites, and subtextures keep today's submissions.
- A mesh that cannot be drawn is left out. The other meshes in the pass are still drawn.
- A transform that is not a finite number is still submitted. One bad copy does not drop the group.
- The once-per-second log shows, for a repeated mesh, how many copies were submitted and how many GPU calls that group became.

## Benefits and Outcomes

| Outcome | Why it matters |
|---------|----------------|
| Fewer submissions | A thousand trees become one call per pass, not a thousand |
| Varying tint and material factors | Autumn trees and differently worn rocks still share a call |
| Picking still names the object | Each copy carries the entity id the editor already reads |
| Shadow and color stay independent | Each pass groups only the copies it actually draws |
| One code path | A unique prop and a forest use the same submission |
| Visible effect | The log shows copies against calls, so a scene that should merge can be checked without a GPU debugger |

## Terminology

**Submission** — One request that the GPU draw a mesh. Today every copy is its own request.

**Copy** — One entity's placement of a mesh: where it sits, which entity it is, its tint, and its material factors.

**Group** — Every copy of one mesh gathered during a single pass. The mesh's textures belong to the mesh, so they are the same for the whole group.

**Instance buffer** — A GPU buffer rewritten each pass. Each entry is one copy. The mesh geometry stays where it was uploaded. The buffer only adds what changes per copy. Shadow and color keep separate buffers. A rewrite leaves the previous bytes with any draw still reading them, and the CPU writes a new store.

**Divisor** — A rate on an attribute. Geometry attributes advance per vertex. Instance attributes advance per copy, so every vertex of a copy sees that copy's transform and tint.

**Color record** — The entry used when shading. It holds the world transform, the normal transform, the entity id, the tint, and the three material factors.

**Shadow record** — The entry used for the depth map. It holds only the world transform.

**Pass** — One walk of the opaque objects. The shadow pass and the color pass are separate walks with separate groups.

## Principles

**Group by the mesh object.** The model factory already returns the same mesh for the same file. That object is the group. Textures live on it. Tint and material factors live on the copy.

**Gather, then submit.** The walk only appends. The GPU sees the group after the walk finishes. An object rejected by frustum culling is never appended, so a hidden copy is not uploaded.

**Color and shadow do not share a list.** The shadow entry is smaller, and the two passes do not keep the same set of copies.

**Geometry stays on the GPU.** The vertex buffer of the mesh is uploaded once. The CPU rewrites the instance buffer every frame, including copies that did not move. The visible set changes with culling, so a buffer kept across frames would draw last frame's survivors.

**Opaque copies only.** A group shares the mesh's textures, and the pass depth-tests. A per-entity texture is a different group. A blended surface is not gathered here: blending needs back-to-front order, and grouping would destroy it. Sprites stay on the 2D path.

**Opaque order does not matter.** Both passes test depth. Groups may be submitted in any order.

**Fit the attribute budget.** The implementation has sixteen vertex-attribute slots, and the color record uses all of them. The record is closed: a new per-copy field replaces one that is already there. An extra slot is dropped on a GPU that only has sixteen. The shadow record uses only the transform slots.

## Design Approach

The work is a gather step in the existing opaque walk, plus one instanced submission per group at the end of the pass. Cubes stay immediate. Sprites and subtextures stay on the 2D path. There is no authored multi-mesh asset and no merging of vertex buffers on the CPU. A group that does not fit in the vertex-buffer size cap is split into consecutive submissions. No copy is dropped.
