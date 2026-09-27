# Runtime Mesh — Conceptual Introduction

## What Problem Does This Solve?

A scene remembers a model by its source file: `.glb`, `.gltf`, or `.fbx`. The first time that path is drawn, the engine parses the source with Assimp and only then uploads vertex and index buffers. The parse runs on the same thread that draws. The next launch pays it again.

There is no cooked model file. Meshoptimizer is not in the repository. Levels of detail are not generated. Animation and skinning are not part of this engine's model path.

## What the Feature Will Achieve

- The scene keeps storing the source path.
- A sibling file next to that source holds everything the current model needs: submeshes, material paths and factors, the axis-aligned box, the node graph, and imported lights.
- The editor rebuilds the sibling when the source bytes, the importer revision, the Assimp post-process flags, or the file version disagree with the stamp in the sibling.
- The runtime reads the sibling. It does not parse the source. A missing or rejected sibling is a failed model load, which already draws a unit cube.
- Several nodes may cite one submesh. The geometry is stored once. Node transforms stay on the nodes.
- A file that fails the checks never allocates its buffers and never uploads to the GPU.
- An older or newer version is not migrated in place. The editor cooks again from the source. The runtime rejects the file.

The sibling is a reproducible copy of today's import. It is not a claim about frame rate. File size, read time, decode time, upload time, and draw time are separate measurements.

## Benefits and Outcomes

| Outcome | Why it matters |
|---------|----------------|
| Launch skips Assimp | The draw thread reads a versioned file instead of parsing the source |
| Source stays the editor's input | Artists still drop `.glb`, `.gltf`, and `.fbx`. Scenes do not change path fields |
| One geometry, many nodes | A mesh used twice in the file is one buffer, with two transforms |
| Failed files stay off the GPU | Truncation, bad counts, and unknown versions stop before upload |
| Rebuild is a new cook | The editor replaces the sibling from the source. Old binaries are not patched |
| Same picture as today's import | Positions, seams, tangents, bitangents, materials, the graph, and lights survive the round trip |

## Terminology

**Source** — The artist file the scene path already points at.

**Sibling** — The cooked file beside the source. Its name is the source file name plus `.mesh`, so `crate.glb` and `crate.fbx` do not share one sibling.

**Stamp** — The source length, the SHA-256 of the source bytes, the importer revision, and the post-process flags, stored in the sibling header. The editor uses the stamp to decide whether to cook again. The runtime does not.

**Cook** — Read the source once, write a sibling, and write any embedded images beside the source. The editor does this. The runtime does not.

**CPU model** — Submeshes, material descriptions, the node graph, and lights in memory, with no GPU objects yet.

**Submesh** — One mesh from the source. It keeps its own vertices, indices, material, and box. It becomes one GPU mesh, as it does today.

**Layout** — The vertex fields stored in the sibling: position, normal, texture coordinate, tangent, and bitangent, all 32-bit floats. The per-vertex entity id is a draw-time value and is not stored.

**Shared mesh** — Two nodes list the same submesh index. The vertex and index bytes exist once.

**Embedded image** — A texture packed inside a `.glb`. The cook writes it beside the source, in a folder named with the source file name plus `.tex`, and the sibling stores a path relative to the source directory.

## Principles

**The source is the editor's authority. The sibling is the runtime's authority.** The runtime never opens the source to repair a bad sibling. The editor never draws from a sibling whose stamp does not match.

**Bytes are explicit.** The file is little-endian. Counts, offsets, and vertex fields are written field by field. A C# struct is not dumped into the file, because padding and field order would then depend on the runtime.

**Trust begins after the checks.** Counts are tested against the file length before the large arrays exist. Index values are tested against the vertex count. Unknown versions and unknown flags are rejected whole.

**Version 1 is a copy, not an optimization.** Vertex-cache reordering, simplification, 16-bit indices, quantization, and disk compression are absent. The header can name a layout and an index type so a later version can add them. A version-1 reader refuses anything other than the one layout and 32-bit indices.

**Replace the file, do not edit it.** The cook writes a temporary file in the same directory and then replaces the sibling. A failed cook leaves the previous sibling untouched and unused until a later cook succeeds.

**Transforms stay on the nodes.** Baking a node matrix into vertices would freeze one placement into the shared geometry and would break a second node that uses the same mesh.

## Design Approach

Import produces a CPU model. The cook writes that model to the sibling. The reader rebuilds the CPU model and checks it. Only then does the existing mesh upload create vertex and index buffers.

The editor's model load hashes the source, compares the stamp, and cooks when they differ. The runtime's model load resolves the sibling from the source path and reads it. Both paths then upload through the same mesh initialization the engine already uses, on the thread that already calls it. That thread owns the OpenGL context today. Version 1 does not add a background upload queue.

Materials stay paths and factors on each submesh. The scene graph and lights stay on the model so hierarchy unpack still works without Assimp. Instancing still groups by the mesh object created at load. The sibling does not store per-entity transforms.

OpenGL stays on the current draw: 32-bit indices, the whole index buffer, one vertex array per submesh. Shaders stay on `#version 330 core`. The model shader already rebuilds the bitangent in the pixel tangent frame and takes the entity id from a uniform or from the instance attribute. The sibling still stores the bitangent so the round trip matches the imported vertex. The loader fills the entity id with the same empty value the importer uses today when it builds the GPU vertex.
