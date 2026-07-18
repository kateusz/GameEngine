# Skeletal Animation (FBX Embedded Clips) — Introduction

## Problem

3D models already load and draw as static meshes. Artist files often also contain a skeleton and animation clips, but the engine ignored them: import flattened hierarchy, vertices had no bone weights, and shaders had no skinning. Characters could only appear in a bind pose.

## What this feature delivers

- **Embedded skeletal clips** — one FBX/glTF carries mesh, skeleton, and clips together.
- **Single-clip playback** — play one named clip at a time with play / pause / stop, loop, and speed.
- **GPU skinning** — up to four bone influences per vertex; bone matrices uploaded each draw.
- **Root motion** — optional application of the root bone’s translation delta to the entity transform.
- **Script-first control** — `AnimatorComponent` API for game scripts; minimal inspector.
- **Fail soft** — missing clips, static models with an animator, or over-limit skeletons degrade with logs.

## What this feature explicitly does not do (v1)

- Blending / crossfades / state machines
- Separate animation files / retargeting
- Morph targets
- Per-bone ECS hierarchy
- Editor bake / native animation format
- Full animation timeline UI

## Key terminology

**Skeleton.** Hierarchy of bones with parent indices and inverse-bind matrices.

**Skin matrices.** Per-frame `inverseBind * globalPose` arrays consumed by the vertex shader.

**Animation clip.** Named keyframe tracks (T/R/S) with a duration, stored on the cached model.

**Animator.** Per-entity playback state: clip, time, playing, loop, speed, apply root motion.

**Root bone.** First hierarchy root in the imported skeleton; source of root-motion translation.

**GPU skinning.** Vertex shader blends up to four bone matrices by weight.

## Patterns and principles

**Asset vs instance.** Model factory caches immutable skeleton/clips; playback lives on the entity.

**Conditional import.** Static files keep `PreTransformVertices`; skinned files drop it and extract bones.

**Pose on CPU, skin on GPU.** Sampling and parenting on CPU; vertex deformation in the lighting shader.

**Root motion opt-in.** Deformation can run without moving the entity.

**Lazy senior defaults.** Four influences, 32-bone budget, no blends, no bake, no bone entities.
