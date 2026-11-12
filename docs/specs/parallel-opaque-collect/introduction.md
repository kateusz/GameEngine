# Parallel Opaque Collect — Conceptual Introduction

## What Problem Does This Solve?

On IslandDash, a typical frame spends most of its time on the CPU inside the opaque 3D walk: walking entities, testing visibility, classifying merge candidates, and queueing mesh batches. Measured Color Collect is on the order of nine milliseconds; the directional shadow walk adds a similar cost. Mesh and merge flush together are about one millisecond. The GPU is not drowning in triangles — the scene is modest — yet the render thread stays busy for roughly a full frame budget before present. Average process CPU across all cores looks low because only one thread owns that walk.

OpenGL submission must stay on one thread. That does not force the walk that *builds* the draw lists to stay on one thread.

## What the Feature Will Achieve

- Split each opaque pass into a CPU **collect** phase and a render-thread **flush** phase.
- Run collect across workers when the candidate count clears a threshold: cull, LOD, merge classify, and queue into per-worker buffers.
- Keep every graphics call, buffer upload, and first-time model or texture resolve on the render thread.
- Apply the same split to the color pass and to shadow passes that already share the opaque walk.
- Leave 2D batching, pass order (sprites, lights, directional shadow, point shadows, color), and the 2D/3D depth policy unchanged.
- Treat parallel collect as an optimization with a safe serial fallback when the candidate set is small or a worker fails.
- Succeed when Color Collect wall time drops on IslandDash and average / median frame time improves toward a stable sixty frames per second. Long-tail hitch spikes from merge rebuild or first load are out of scope.

## Benefits and Outcomes

| Outcome | Why it matters |
|---------|----------------|
| Lower Color Collect wall time | The dominant CPU slice of the color pass shrinks without changing shaders |
| Lower shadow collect wall time | The same walk pattern serves directional and point shadow sites |
| Higher average / median FPS | Less serial CPU before flush and present |
| Unchanged picture contracts | Cull rules, instancing keys, and merge classify stay the same; only who fills the queues changes |
| Single GL thread preserved | Matches the engine’s render-thread GPU ownership rule |
| No scene format change | Nothing new to serialize; no gameplay component |

## Terminology

**Collect** — The CPU work that walks opaque candidates, applies visibility and frustum or caster tests, picks LOD, classifies merge membership, and appends to mesh-batch or merge queues. No draw submission.

**Flush** — The render-thread step that turns those queues into `IGraphics3D` draws (instanced mesh batches and merge groups), including any GPU resource creation.

**Candidate list** — A render-thread snapshot of opaque entities for one pass: enough transform and renderer data for workers to cull and queue without touching the ECS context again.

**Thread-local collect buffers** — Per-worker mesh-batch maps and merge member lists. Workers never write the shared static queues used today.

**Deferred resolve** — A factory or texture miss discovered on a worker. The worker does not load or upload; the render thread resolves before flush, using today’s cube fallback and one-shot warnings.

**Parallel threshold** — Minimum candidate count before workers run. Below it, collect stays single-threaded to avoid scheduling cost on tiny scenes.

**Join** — Render-thread merge of thread-local buffers into one queue set (or an equivalent ordered flush), replay of immediate cube draws queued by workers, then deferred resolve, then flush.

**Immediate cube list** — Non-merge unit cubes that the serial walk would draw mid-loop. Workers record them; the render thread submits them after join so no GL runs on a worker.

## Principles

**GL stays single-threaded.** Workers build CPU lists only. The platform OpenGL context is never current on a worker.

**Read-only after materialize.** The ECS view is read once on the render thread into the candidate list. Workers index that list. Gameplay systems are not expected to mutate the scene during this pass.

**Same filter, same keys.** Frustum, zones, caster distance, lamp-inside skip, batch keys, and merge classify match the serial path so serial-vs-parallel tests can compare counts.

**Degrade, do not crash.** Worker failure or an empty parallel path falls back to serial collect for that pass.

**One feature, one walk.** No separate job framework, no frame pipelining, no overlapping color collect with shadow collect in this design. Those remain later options after this path is measured.
