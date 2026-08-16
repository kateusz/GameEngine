# Skeletal Animation (v1) — Introduction

## Problem

Imported 3D characters can already draw in bind pose: a cooked `.mesh`, PBR materials, and a transform. They cannot move their limbs. Artists author that motion as a **skeleton** plus **clips** in DCC tools and ship it in FBX / glTF / GLB. The engine currently throws that data away at cook time.

Without skinning, every walk, idle, or attack is either a static statue or a hand-animated transform hack. The engine already decided Assimp is cook-only and runtime loads `.mesh` only — so animation cannot mean “parse the FBX again in Play mode.” It has to live in the same bake.

## What this feature delivers

- **One cooked file** — a skinned import writes geometry, materials, bone weights, the skeleton, and the clips into a single `.mesh`. Static models stay as they are.
- **GPU skinning** — each vertex is influenced by up to four bones; a bone palette deforms the mesh on the GPU. Lighting and PBR stay the existing forward path.
- **Author-owned playback** — a playback component on the character parent points at that `.mesh` and one clip. Import does not add the component.
- **Parent owns the pose** — child mesh parts that draw the same `.mesh` share the parent’s palette. The character moves as one rig.
- **Edit and Play** — Playing, Time, Speed, and Loop drive the viewport in Edit mode and keep running in Play mode. Playing off is bind pose.

## What this feature explicitly does not do (v1)

- **Clip blending, state machines, or blend trees** — one clip at a time.
- **Retargeting** — a clip from a second Mixamo file is not applied onto another mesh. That pair is a second cook.
- **IK, ragdoll, or procedural bones**
- **Morph targets / blend shapes**
- **CPU vertex skinning** as the draw path
- **Runtime Assimp** — no FBX/glTF on the hot path
- **Auto-wiring playback on import** — cook and spawn only; the author adds the component
- **Independently movable skinned parts** — child local transforms are identity; the skeleton deforms the mesh

## Key terminology

**Skin.** The visible mesh. Vertices sit in a rest pose until bones move them.

**Bone / joint.** A named transform in a parent–child hierarchy. Moving a parent moves its descendants.

**Skeleton.** The full bone hierarchy plus each bone’s **inverse bind** matrix (the transform that takes a vertex from mesh space into that bone’s rest space).

**Bind pose.** The rest pose as authored. What you see when nothing is playing. Playing at the clip’s first key must match this — if it does not, the mesh explodes or shears.

**Influence / weight.** How much a bone moves a vertex. v1 keeps at most four influences per vertex; they sum to one. A vertex with no weights stays in bind.

**Keyframe.** A bone’s translation, rotation, or scale at a time. Rotation is a quaternion so interpolation does not gimbal-lock.

**Clip.** One animation: duration plus per-bone key tracks. A file may contain several clips; v1 plays one (named, or the first if the name is empty).

**Bone palette.** The array of final skinning matrices for the current pose, one per bone, uploaded to the shader. Unused slots are identity. Cap is 100 bones.

**Skeleton root space.** All skinned vertices are cooked into the skeleton’s root. The parent entity’s world transform places the character; children do not add a second node transform on top of the bones.

**Cook.** Editor-time Assimp import → write `.mesh`. Runtime never sees Assimp scene types.

**Playback component.** Scene data on the parent: path to the cooked `.mesh`, clip name, Playing, Loop, Speed, Time. The palette is a transient pose, not saved in the scene.

## Patterns and principles

**Bake once, pose many.** Authors pay Assimp at import. Play mode and published builds sample cooked keys and upload a palette.

**One file, two consumers.** The same `.mesh` feeds drawing (`ModelRenderer` on children) and posing (playback on the parent). No companion skeleton or clip files in v1.

**Path on the component, resources in the factory.** ECS stores the `.mesh` path. GPU meshes and clip tables live in the existing path-keyed cache.

**Parent pose, child draw.** One rig, many submeshes. A child uses the ancestor palette only when it draws the same `.mesh` the playback points at. Missing playback, or a different file, means bind pose and the child’s own transform (static draw, unchanged).

**Identity palette is bind pose.** Playing off, unknown clip, or missing skeleton must not invent a different rest pose.

**Fail soft.** Bad paths and unknown clip names log and show bind pose (or the cube, if the renderer’s mesh is missing). Cook rejects rigs over the bone cap so Play never hits a shader overflow.

**Same controls in Edit and Play.** Preview is not a second pipeline. The animation system runs in both; only the scene clock differs in the usual Edit vs Play way.

**YAGNI against the article’s runtime graph.** LearnOpenGL’s Model / Animation / Animator trio is a teaching structure that keeps Assimp alive. This engine already has a cook boundary and row-vector math. Take the article’s *ideas* (skin, weights, palette, lerp/slerp) and drop its *lifetime*.

## Architecture philosophy

**Extend the `.mesh` bake, don’t add a second asset type.** Static files keep loading. Skinned files are a newer container version with an optional skinning payload. Readers that only draw geometry can skip clips; playback asks for them.

**GPU deforms, CPU samples.** The CPU interpolates keys and walks the hierarchy. The vertex shader applies the palette. The fragment shader does not know about bones.

**Conventions are part of the product.** Assimp is column-vector; the engine is row-vector. Transpose once at cook. Evaluation and the shader must multiply in the same order. The bind / first-key invariant is how you know you got it right.

**Lazy senior default.** One Mixamo or glTF file in, one `.mesh` out, one component to play the first clip. No blend graphs, no retargeter, no third file format. Add those when a game actually needs a clip library on a shared mesh.
