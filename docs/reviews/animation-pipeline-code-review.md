# Code Review — FBX Loading & Skeletal Animation Pipeline

- **Date:** 2026-07-26
- **Scope:** cook path (`AssimpModelImporter`, `MeshCreator`, `CookUnitScale`, `SkinnedMeshSpace`, writers/readers for `.mesh` / `.skel` / `.anim3d`), runtime (`SkeletalPoseMath`, `SkeletalPlaybackUpdater`, `SkeletalAnimationSystem`, `SceneRenderPipeline`, `Graphics3D`, `OpenGLShader`, `OpenGLVertexBuffer`, `lightingShader.vert`), factories/caches, and the test suites covering them.
- **Method:** full read of every file above, cross-checked against the System.Numerics row-vector convention, the Assimp 5.x C ABI, and the GL 3.3 spec; findings verified experimentally where possible (probe tests on the real Mixamo FBX).
- **Note:** the requested `ponytail-review` skill is marked `disable-model-invocation` and can only be run manually as `/ponytail-review`; this review follows an equivalent read-everything-first, no-speculative-fixes process.

---

## 0. Executive summary

The "Current bug" described in the review brief (bind pose fine, PLAY → exploding triangles) was root-caused and fixed during this session, **before** this review. Two independent defects stacked:

1. **RESOLVED — Silk.NET.Assimp 2.23.0 ABI mismatch (was Critical).** The managed `QuatKey` struct carried the assimp-6 layout (`MInterpolation`, 32 bytes/key) while the loader resolved the bundled assimp **5.4.1** binary (24 bytes/key). Reading `MRotationKeys[k]` walked the wrong stride: rotation key 0 was valid, every later key was spliced garbage (timestamps interleaved into quaternion components), plus out-of-bounds heap reads past the native array. Vector keys are 24 bytes in both ABIs, so translations survived — the asymmetry that made this so confusing. **Fix:** `Silk.NET.Assimp` pinned to 2.22.0 (ABI-matched pair) in `Engine.csproj` with an explanatory comment; regression tests added (`SkinnedAnimRotationKeysTests` — CI, no local FBX needed; rotation-track sanity + 6 ms continuity assertions in `MixamoFbxSkinningTests`).
2. **RESOLVED — column-order composition in a row-vector engine (was Critical).** `SkeletalPoseMath` composed `globals = parent × local`, locals as `T×R×S`, rest locals as `invParent × bindGlobal` — all column-vector order, while every matrix in the engine is row-vector (importer transposes, shader does `v*M`). Bind pose and the t0 retarget are *cancellation identities* (`rest·inv(k0)·k0 = rest` holds under any convention), so every bind/t0 diagnostic passed while animated poses distorted proportionally to rotation magnitude and chain depth (fingers stretched up to 96× bind length at the punch apex). **Fix:** all four compositions flipped to row-vector order; regression tests added (`Evaluate_ChildOrbitsRotatedParent`, `Evaluate_RotatingBoneAboutItsOwnJoint_DoesNotMoveTheJoint`, semantic `ComposeLocal` test, 33-frame bone-rigidity sweep).

The pipeline as it stands now **renders correctly on this machine** (629+ unit tests green, rigidity ratio 1.000 across the clip, GPU readback tests green). The open findings below are, in order of importance: two **portability time bombs in the GPU upload path** that will reproduce the exact "exploding triangles" symptom on non-Apple drivers, one **latent asset-dependent evaluator bug**, and a set of robustness/hygiene items. Nothing open affects current macOS rendering.

---

## 1. How the pipeline works (stage by stage)

### 1.1 Cook (import → cooked assets)

`MeshCreator.CreateSkinned(fbx, assetsRoot, stem)`:

1. `AssimpSceneImport.Import` — one shared entry point; sets `FbxConvertToMeters=1`. Skinned flags: `Triangulate | GenerateNormals | CalculateTangentSpace | JoinIdenticalVertices | LimitBoneWeights` — deliberately **no** `PreTransformVertices` (would destroy the node hierarchy) and no `FlipUVs` (textures are stbi-flipped at upload). Correct choice of flags.
2. `AssimpModelImporter.ImportSkinned`:
   - `BuildSkeleton` — bone set = (mesh `aiBone` names) ∪ (animated node names); sorted by node depth, then name → **parents always precede children in index order** (required by the single-pass global composition, satisfied). Parent = nearest ancestor that is itself in the bone set (correctly skips non-bone pivot nodes). Inverse bind = transposed `aiBone.mOffsetMatrix`; fallback = inverse of the node's accumulated bind global (with warning).
   - `WalkNode` — accumulates node transforms in Assimp column space, transposes once at part emission. Meshes extracted per node; skinned extraction attaches ≤4 (bone, weight) pairs per vertex: sorted by weight, trimmed, renormalized.
   - `ExtractAnimations` — per `aiNodeAnim` channel keyed by node name → bone index; key times divided by `mTicksPerSecond` (fallback 25 — assimp's own default); quaternion stored as `(X,Y,Z,W)` from the ABI-matched Silk struct. Channels for unknown nodes can't occur (animated node names are added to the skeleton).
3. `SkinnedMeshSpace.BakePartsToRootSpace` — bakes each part's node transform into its vertices so mesh, inverse binds, and animation all live in the same root space, and the entity's `u_Model` composes on top. Sound design; it makes the palette root-space by construction.
4. `CookUnitScale` — heuristics: mesh extent > 20 → cm→m on everything; anim translation ≫ mesh extent → cm→m on anim only; IB translation ≫ mesh extent → harmonize IB. The row-vector algebra of the IB corrections (`invScale * IB`, `IB * S`) is correct (derivations documented in comments).
5. Writers — `.mesh` (KULA v2: positions/normals/UV/tangents/bitangents + int32 bone ids + float weights), `.skel` (SKEL v1: name, parentIndex, IB — topology validated on read), `.anim3d` (AN3D v1: per-channel T/R/S key tracks). Reader/writer field order verified symmetric; round-trip tests exist for all three.

### 1.2 Runtime evaluation

- `SkeletalAnimationSystem` (priority 135) runs **before** `SceneRenderSystem` (150) in play mode; `EditorViewport` ticks the same `SkeletalPlaybackUpdater` in edit mode, guarded by `SceneState.Edit` — no double-tick.
- `SkeletalPlaybackUpdater` — resolves `.skel`/`.anim3d` through path-keyed caches (evicted on re-import), advances `Time` (loop = wrap incl. negative speed, else clamp), calls `SkeletalPoseMath.Evaluate` into the component's 100-entry `BonePalette`.
- `SkeletalPoseMath.Evaluate` (all row-vector, post-fix):
  - rest locals from inverse binds: `local_i = bindGlobal_i × inv(bindGlobal_parent)`;
  - per channel: sample T (lerp) / R (slerp + normalize) / S (lerp) with clamp-at-ends and duplicate-time guard; compose `S×R×T`; retarget `local = key(t) × inv(key(t0)) × rest` so the first key frame skins exactly as bind and rotation deltas pivot about the joint (bone offsets provably rigid for constant-translation tracks);
  - globals `G_i = L_i × G_parent`; palette `IB_i × G_i`; unused palette slots = identity.

### 1.3 Render

- `SceneRenderPipeline.RenderModels` — per `ModelRendererComponent`: palette resolved from the entity's own `SkeletalPlaybackComponent` or the nearest ancestor's (sibling mesh entities share the rig's palette); identity palette when not playing.
- `Graphics3D.DrawMesh` — uploads the 100-matrix palette (`SetMat4Array`), then re-uploads `u_ViewProjection`/`u_Model`/`u_NormalMatrix` (defensively ordered after the array upload), material uniforms, textures, draws indexed.
- `OpenGLShader` — `SetMat4`/`SetMat4Array` upload with `transpose=true`, making GLSL `v * M` equal to Numerics `v·M` (verified by the GPU readback test `SetMat4Array_TransposeMatchesSetMat4`). `SetMat4Array` uploads each matrix individually at `location + i*4` (see F1).
- `lightingShader.vert` — float bone indices (`+0.5` round, clamp 0..99), 4-weight accumulation, `weightSum < 1e-5` → unskinned fallback; skins position, normal, tangent, bitangent; then `u_Model` → `u_ViewProjection`, all row-vector. Normal matrix = transpose(inverse(model)) applied as `n·M` — correct for the row convention.

---

## 2. Findings

Severity legend: **Critical** = can produce the reported corruption class (now or on other platforms) · **Major** = wrong results or dead renderer under realistic conditions · **Minor** = robustness, performance, hygiene.

### F1 — CRITICAL (portability): hardcoded `location + i*4` uniform stride in `SetMat4Array`

`Engine/Platform/OpenGL/OpenGLShader.cs:139-146` uploads each palette matrix at `baseLocation + i*4`, assuming every `mat4` array element occupies 4 uniform locations. That matches what the current Apple driver assigns (the shader inventory log shows `u_BoneMatrices[0]@3` and the next uniform at `@403`, and the GPU readback test passes locally), **but automatically assigned uniform locations for array elements are not spec-guaranteed to follow any stride**. Mainstream desktop drivers (NVIDIA/AMD/Mesa) conventionally assign +1 per element; there, matrix *i* would land on element *4i* (bones 25..99 on invalid locations → `GL_INVALID_OPERATION`, silently swallowed since `OpenGLDebug.CheckError` runs only after the loop).

- **Why it produces the corruption symptom:** on a +1-stride driver, 75% of palette entries are never written; the shader reads stale/undefined matrices → vertices fly, exactly the "triangles explode across the screen" report — but only on non-macOS machines, which makes it look like a heisenbug.
- **Verify experimentally:** on each target platform run `Engine.GraphicsTests/OpenGLShaderSetMat4ArrayTests` (it reads elements back via per-element `glGetUniformLocation`, so it fails loudly on stride mismatch). Or at shader init, log `loc("u_BoneMatrices[1]") - loc("u_BoneMatrices[0]")`.
- **Fix:** query and cache per-element locations once at link time (`u_BoneMatrices[i]` for all i), or derive the stride from `loc[1]-loc[0]`; re-test the single-call `glUniformMatrix4fv(base, count, …)` path (spec-guaranteed to fill consecutive **elements** regardless of location numbering — the "macOS mishandles count>1" comment deserves re-verification now that the two real bugs are fixed; it may have been misattributed during the original debugging). Long-term: UBO (see F2).

### F2 — MAJOR (portability): 100 `mat4` uniforms exceed the GL 3.3 minimum uniform budget

`u_BoneMatrices[100]` = 1600 float components; GL 3.3 only guarantees `MAX_VERTEX_UNIFORM_COMPONENTS ≥ 1024`. On minimum-spec drivers (older Intel/embedded, llvmpipe in CI) the shader **fails to link** — no corruption, just a dead lighting shader everywhere.

- **Verify:** query `GL_MAX_VERTEX_UNIFORM_COMPONENTS` at startup and log; try llvmpipe/Mesa.
- **Fix:** move the palette to a **UBO** (std140, 100×64 B = 6400 B, comfortably under the guaranteed 16 KB) — this also removes F1 entirely and cuts 100 driver calls per draw to one buffer update per entity. This is the single highest-leverage GPU-path change available.

### F3 — MAJOR (latent, asset-dependent): `Matrix4x4.Decompose` failure ignored in `ApplyChannels`

`Engine/Scene/SkeletalPoseMath.cs:111` discards the `bool` result. If a rest local contains shear or negative scale (mirrored rigs, some exporters), `restT/restR/restS` come out garbage. They are only used as **fallbacks for missing tracks**, so fully-keyed Mixamo clips are unaffected — but a rotation-only channel on such a rig would compose a garbage local every frame.

- **Why corruption:** one bad local propagates down the chain → localized limb explosion that looks exactly like the original bug, but only for specific assets.
- **Verify:** log `Decompose` failures once per skeleton at first evaluate; import a rig with a −1 scale (mirrored) joint and a rotation-only clip.
- **Fix:** when decompose fails, skip the TRS-fallback path and use `restLocal` directly for missing-track channels (compose the delta only from the tracks that exist).

### F4 — MAJOR (defense-in-depth): no validation of animation key data at cook or load

`Anim3dWriter`/`Anim3dReader` accept unsorted / non-finite key times and non-unit quaternions without complaint; `SampleVec3/SampleQuat` silently mis-sample unsorted tracks (returning the last key for all t — a frozen wrong pose). The ABI bug (F0.1) wrote exactly such data to disk and nothing in the pipeline objected; a single cook-time assert would have turned a multi-day investigation into an immediate import error.

- **Verify:** hex-edit one key time in a cooked `.anim3d` out of order; observe that today it loads silently and the pose freezes.
- **Fix:** in `Anim3dWriter.Write` (cook time) throw on non-ascending times, non-finite values, or |quat|−1 > 0.05; mirror the cheap checks in `Anim3dReader` (files can arrive from elsewhere). Matching checks exist in the new regression tests, but the product code should refuse bad data itself.

### F5 — MINOR (robustness): `.mesh` bone indices/weights trusted on load

`MeshReader` validates triangle indices against vertex count, but bone indices only get saved by the shader clamp (0..99) and the CPU fallback `idx >= palette.Length → 0`. Negative or ≥ bone-count indices from a corrupted/hand-made file deform silently instead of failing loudly. Weight range/sum is also unvalidated (cook normalizes, so this only matters for foreign files).
**Fix:** validate `0 ≤ boneIndex < SkeletonReader.MaxBones` and `0 ≤ weight ≤ 1` in `ReadVertex`; optionally warn on `sum > 1+ε`.

### F6 — MINOR (correctness edge): `IsIdentityPalette` samples only the first 32 bones

`Graphics3D.cs:246-255`. A live palette whose first 32 entries are float-exact identity while later ones are not would be misclassified and rendered as bind. Practically improbable (live palettes are never exactly identity), and the pipeline already sends identity when not playing — the value-sniffing is redundant. **Fix:** drop palette content detection; pass an explicit `isIdentity`/`null` from `SceneRenderPipeline` (which already knows the palette source).

### F7 — MINOR (performance): per-frame re-derivation of retarget data

`ApplyChannels` recomputes, per channel per frame: `ChannelBindTime`, two full `SampleChannelLocal` calls (one of them for the constant `key0`), a matrix `Invert`, and a `Decompose` of the rest local. All of it is clip-constant. Negligible at 28 bones / 1 entity; measurable at 100 bones × many entities. **Fix:** cache `inv(key0) × rest` (and the decomposed rest TRS) per (skeleton, clip) pair — e.g. a small cache keyed alongside the anim asset, or precompute at cook into the `.anim3d`.

### F8 — MINOR (performance): 100 individual `glUniformMatrix4` calls per skinned draw, per submesh

Multi-submesh characters re-upload the identical palette per submesh. Superseded by the UBO move (F2); until then, upload once per entity per frame and skip re-upload when the palette pointer + frame haven't changed.

### F9 — MINOR (portability heuristic): `CookUnitScale` misclassifies legitimately large models

Any model whose bind extent exceeds 20 units is downscaled ×0.01 as "cm-authored". A real 25 m building authored in meters cooks to 25 cm. **Fix:** read the FBX `UnitScaleFactor` metadata when available and use the heuristic only as fallback; expose a per-import override in the import popup.

### F10 — MINOR (correctness edge): direction skinning ignores non-uniform animated scale

`SkinDirection` uses the bone matrix directly for normals/tangents — correct for rigid bones (Mixamo scale ≡ 1), skewed lighting under animated non-uniform scale. Also: vertices with zero weights stay bind-frozen by design (fallback), which is correct for fully-weighted meshes but will surprise on partially-weighted ones. Document both; consider a cook-time warning when a skinned mesh contains unweighted vertices or scale keys ≠ 1.

### F11 — MINOR (consistency): bone-count constant duplicated across five sites

`100`/`99` literals in `lightingShader.vert` (×4 host copies), `SkeletonReader.MaxBones`, `SkeletalPlaybackComponent.MaxBones`, `Graphics3D.BoneMatrixCount`, `Anim3dReader.MaxChannelsPerClip`. `LightingShaderSkinnedHostsTests` pins the shader copies to each other, which helps; an `Init`-time assert that the shader's array size matches `SkeletonReader.MaxBones` would close the loop.

### F12 — MINOR (hygiene): debug diagnostics permanently resident in hot paths

`SkinnedDbg` logging (LINQ scan of all vertices on first draw, palette dumps, `EveryNFrames` ticks) plus never-cleared static `HashSet`s (`LoggedSkinnedDrawEntities`, `WarnedTintEntities`, …) — entity-id recycling across scene loads suppresses future diagnostics, and release builds pay the residual cost. **Fix:** gate behind a debug flag/`#if`, clear the sets on scene load.

### F13 — MINOR (cosmetic): first animated frame is `t = dt`, not `t = 0`

`SkeletalPlaybackUpdater` advances time before the first `Evaluate`, so playback visibly starts one frame into the clip. Evaluate first (or advance after evaluate) if exact first-frame fidelity matters.

### F14 — INFO: version couplings that must move together

- `Silk.NET.Assimp` **2.22.0 pin** ↔ `TextureType.Unknown` used for glTF metallic-roughness (assimp 5.x semantics; assimp 6 renames the slot). Both are commented; keep them in sync on any future upgrade — `SkinnedAnimRotationKeysTests` will catch a repeated ABI break.
- `MulRowVectorMatrix(v, m)` is mathematically identical to GLSL `v * m` (its comment claims otherwise); harmless, but the comment should be corrected to avoid future "fixes".

---

## 3. Review-brief checklist — verdicts

| # | Area | Verdict |
|---|------|---------|
| 1 | **FBX import** — positions/normals/tangents/UVs extracted per vertex with bounds logging; index buffers `uint32`, triangulated, validated `< vertexCount` on load; multi-mesh & multi-node handled via `WalkNode` (one part per mesh-bearing node); hierarchy preserved (no PreTransform on skinned path); Assimp column → Numerics row conversion done by a single transpose at each boundary, verified consistent; cm→m handled by import property + `CookUnitScale` | ✅ sound (F9 heuristic caveat) |
| 2 | **Materials & textures** — base color (`BaseColor` → `Diffuse` fallback), MR (`Unknown` → `Metalness` → `DiffuseRoughness` → `Specular`, correct for assimp 5.x), normals (`Normals` → `Height`); embedded GLB textures extracted to a content-addressed cache; texture paths relocated into assets and sandboxed on load; sRGB only for albedo; per-submesh material assignment via `mMaterialIndex` | ✅ sound (F14 version coupling) |
| 3 | **Skeleton** — depth-sorted bones guarantee parent-before-child; parent = nearest ancestor in bone set (pivot-safe); IB from offset matrices with node-bind fallback; topology (range, self-parent, cycles) validated in `SkeletonReader`; animated-but-unweighted joints included; root absorbs static ancestor transforms via IB-derived rest locals | ✅ sound |
| 4 | **Vertex skinning data** — ≤4 influences (assimp `LimitBoneWeights` + cook-side sort/trim/renormalize); zero-influence vertices → explicit unskinned fallback; invalid indices clamped in shader and CPU reference | ✅ sound (F5 reader validation gap) |
| 5 | **Animation import** — clips/duration/ticksPerSecond correct (`/tps`, fallback 25); channels mapped by node name with no silent drops (animated names are bones by construction); quaternion component order correct **only** with the pinned ABI-matched package (F0.1 history; guarded by tests) | ✅ sound under pin |
| 6 | **Animation evaluation** — lerp/slerp(+normalize)/lerp with end-clamping and duplicate-time guards; retarget delta pivots about the joint; parent accumulation and palette assembly correct for row-vector convention **after the F0.2 fix**; verified by orbit/pivot/rigidity tests (bone stretch ratio 1.000 across the real clip) | ✅ fixed & regression-guarded (F3 latent edge) |
| 7 | **GPU upload** — `transpose=true` consistently, `v·M` semantics verified by GPU readback test; exact-count packing; **per-element location stride is driver-specific (F1)** and the uniform budget exceeds GL 3.3 minimums (F2) | ⚠️ works locally, portability risks |
| 8 | **Vertex shader** — skinning math, weight accumulation, <4-weight handling, clamped indexing, model/VP transform order all correct and convention-consistent; direction skinning assumes rigid bones (F10) | ✅ sound |
| 9 | **Rendering states** — animation OFF → identity palette (bind) ✅; ON at t=0 → bind by the t0-retarget contract (tested) ✅; unrelated objects unaffected (separate programs; defensive uniform re-upload after the array; polygon mode restored after wireframe) ✅ | ✅ sound |

---

## 4. Test coverage assessment

**Strong:** format round-trips (mesh/skel/anim3d), skeleton topology validation, cook flag contracts, vertex layout, GPU readback of `SetMat4Array` (count + transpose), shader-copy synchronization across hosts, pose-math semantics (orbit, joint pivot, rigidity sweep, t0-as-bind), ABI regression (3-key rotation track through the real interop), real-FBX gated end-to-end test with key sanity + 6 ms continuity + rigidity assertions.

**Gaps worth closing:**
1. A graphics test asserting `loc(u_BoneMatrices[1]) − loc(u_BoneMatrices[0])` matches whatever stride `SetMat4Array` uses (turns F1 into a red test on any driver, not just readback divergence).
2. Cook-time validation tests for F4/F5 (bad key times / bad bone indices must throw, not load).
3. A mirrored-rig (negative scale) fixture with a rotation-only channel (F3).
4. An animated-scale clip through the glTF fixture (S-track sampling is currently untested end-to-end).

---

## 5. Prioritized actions

1. **F2 + F1 together:** move the bone palette to a std140 UBO (kills the stride assumption, the uniform-budget risk, and 100 calls/draw in one change). Until then, derive the element stride from queried locations instead of hardcoding ×4.
2. **F4:** cook-time + load-time key validation (ascending, finite, unit quats).
3. **F3:** honor the `Decompose` result; fall back to raw rest local for missing tracks.
4. **F5:** bone index/weight range validation in `MeshReader`.
5. F7/F8 perf caching when character count grows; F12 debug-log gating before shipping anything.
6. Keep the `Silk.NET.Assimp` 2.22.0 pin and its tests until a Silk release whose struct ABI matches the native binary it actually loads (re-run `SkinnedAnimRotationKeysTests` on any bump).
