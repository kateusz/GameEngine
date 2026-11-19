# Rendering refactor: stages 0 and 1

Implemented 2026-11-19 against main `ad6f5b0e8a60ed4d1facbbaa58555ee2eca4d28e` plus the existing Windows test-target change. No stage 2+ class extraction is included.

## Stage 0: reproducible evidence

`RenderingReferenceScenesTests` constructs eight procedural scenarios without importing user assets: HDRI-only, directional shadows, point shadows with alpha cutout, repeated mesh instances, identifiable LOD, static merging, shadow-quality change, and scene change/picking. Each renders three frames at 96×64 with a fixed perspective camera, near/far 0.1/50, a fixed clear color and Reinhard/gamma presentation with bloom and no FXAA. The LOD scene deliberately uses a front-face-only coarse level with zero error so a changed draw path is observable. It is a contract fixture, not an asset-quality test.

Output per frame:

- `.rgba32f`: linear HDR RGBA float32 little-endian, bottom row first. This preserves values above 1.
- `.rgba8` and generated `.png`: tonemapped display RGBA8, top row first.
- `.ids`: signed int32 little-endian entity IDs, bottom row first.
- `.json`: view, quality, pass counters and visible IDs.
- `run.json`: resolution, camera and display policy, GPU vendor/renderer/version and scenario list.

Scene definitions/materials/light positions are executable and versioned in the test source. Generated captures are evidence, never automatically approved or copied over checked-in goldens. Existing `ImageRegressionAssert`/Golden tests remain intact. The capture runner rejects an output directory already containing a run.

```powershell
./tools/rendering/capture-reference.ps1 -OutputDirectory C:/Temp/render-reference-a
./tools/rendering/capture-reference.ps1 -OutputDirectory C:/Temp/render-reference-b
python tools/rendering/compare_reference.py C:/Temp/render-reference-b --compare C:/Temp/render-reference-a --exact
```

`--exact` means byte-identical HDR, display and picking on the same driver/build. Comparison reports maximum absolute error/MSE for image channels and differing pixels for IDs. It is not a cross-vendor tolerance. Establish driver-specific tolerances from actual repeat measurements before approving other platforms. Python uses only its standard library. The test harness can also be directed with `RENDER_BASELINE_DIR`.

Initial existing tests: Engine 749 pass / 1 fail; Graphics 34 pass / 1 fail, no skips. The Windows TFM correction was already in the user's worktree and is preserved. The stale Mesh disposal test assumed only a VAO owned index buffers; Mesh now owns all LOD index buffers. The corrected test asserts both LOD levels are disposed once and the VBO remains VAO-owned. No Mesh implementation was changed. The invalid-shader test now uses a self-contained temporary fragment shader so it reaches invalid vertex compilation rather than failing include lookup. Runtime shader include resolution is unchanged and remains a later audit finding. `global.json` now requests valid SDK feature band 10.0.100, retaining its existing roll-forward policy.

Before runtime fixes, the expanded capture suite passed 36/36, while semantic defects were separately reproduced by the new red contract tests. Early capture repetitions matched only 21/24 frames. Follow-up testing exposed an invalid reference HDRI aspect ratio (4x4 instead of 2:1) and insufficient initialization of preparation state. Capture input version 2 uses 8x4 HDRI, explicit state before Init/SetSkybox, a decoded input check and rejection of nonfinite HDR output. These corrections belong to the reference harness. The early HDRI variation does not independently establish a runtime regression. The corrected full-suite, isolated-test and Release captures match 24/24 exactly.

## Stage 1: corrected contracts

### Framebuffer ownership of ambient state

`OpenGLFrameBuffer.Invalidate` builds a replacement before retiring the old allocation. Success and failure restore separate read/draw framebuffer bindings and the affected 2D/cubemap bindings on the active texture unit. Allocation does not change the active unit or viewport. Resizing an already bound target rebinds its replacement, remaps its currently bound attachment and updates its viewport. A failed resize retains the previous dimensions/resources. Partially created resources are deleted on allocation failure.

`Bind`/`Unbind` now preserve read and draw bindings independently. Binding the same target twice or unbinding it without a binding is rejected, rather than silently overwriting saved state. Nested *different* targets remain supported. `BindScoped()` provides an allocation-free using scope; the editor scene target and runtime HDR capture use it, so exceptions return to the previous target. Other callers using balanced Bind/Unbind retain their API.

### Instancing

Instance attribute layout state belongs to the `OpenGLVertexArray` instance and records its bound instance VBO. It no longer lives in a renderer-wide set of recyclable VAO numbers. A new VAO always receives a layout; switching renderer instance buffers also refreshes it. DrawIndexedInstanced explicitly binds its input VAO.

### Culling

`SetFaceCulling` only enables/disables culling. `SetCullFrontFaces` only chooses front/back. Point depth selects Front, directional depth and color select Back; a double-sided material can disable culling without changing the selected face. EndPointShadowFace restores Back and enables culling. Existing default color/IBL algorithms and shader formulas are unchanged.

## Acceptance evidence

Real OpenGL tests cover allocation and resize while another HDR target is bound, distinct read/draw bindings, active texture/viewport preservation, allocation failure restoration, scope disposal after an exception, bound-target resize, cold sun/point allocations, quality change, new lamp maps, front/back policy across double-sided draws, and instancing/picking through recreated VAOs and competing renderer buffers.

The original backend failed all three initial contract tests: target bindings became zero and instance attributes referenced the other renderer buffer. A later scope/resize test extends coverage. The reference suite now requires scene IDs in the HDR target for all 24 frames and rejects IDs from the previous scene. Tests also verify instancing/merging counters and forced LOD selection, so a blank capture alone cannot pass.

Local observed acceptance: Engine.Tests Debug 754/754, Editor.Tests Debug 134/134, Engine.GraphicsTests Debug 40/40 and Release 40/40, no skips. Engine count includes four concurrently added model-resolution tests, not authored by this change. The parallel SceneRenderPipeline frame-model cache changes were preserved. Existing compiler warnings remain; this is not a warning-free build claim.

Historical before/after captures for this run are outside the repository at `C:/Users/matku/blender/Documents/ChatGPT/GameEngine/rendering-stage0-before`, `rendering-stage0-repeat`, `rendering-stage1-after`, and `rendering-stage1-repeat`.

Geometry changes are intentional corrections, not a cosmetic refactor: missing geometry returns to HDR on cold maps/quality changes; recreated VAOs render the instances again; point shadows retain the chosen culling policy. Do not compare the revised HDRI inputs against the original fixture as if they were equivalent. Do not replace goldens indiscriminately to hide a difference.

## Remaining scope

No claim of macOS runtime verification: GraphicsFact skips there. No interactive editor/device acceptance or performance gain is inferred from a headless fixture. Context-current global teardown, cache retirement, full point-shadow invalidation, shader warning/include policy and the larger responsibility split remain later work. The renderer's process-lifetime instance VBO has not been redesigned in stage 1.
