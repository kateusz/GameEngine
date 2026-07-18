# ImGuizmo 3D Transform — Introduction

## Problem

Editor transform tools today draw and hit-test a custom 2D gizmo on the ImGui draw list. That works for simple planar moves, but it is not a real 3D manipulator: axes do not follow perspective correctly, rotation and scale feel unlike Unreal/Unity, and local vs world space is awkward to express with screen-space arrows.

Authors editing 3D scenes need the familiar translate / rotate / scale gizmo that sits in the viewport, uses the editor camera’s view and projection, and updates the selected entity’s transform while the drag is active.

## What this feature delivers

- **Full 3D T/R/S manipulator** in the edit-mode viewport for a single selected entity, driven by [ImGuizmo](https://github.com/cedricguillemet/imguizmo).
- **Local and world space** switching for the same operations.
- **Same tool modes as today** — Move / Rotate / Scale on the toolbar map to ImGuizmo operations; no new editor modes in v1.
- **Input ownership** — while the gizmo is hovered or dragged, viewport camera orbit/pan and entity picking do not steal the gesture.
- **Windows-only native bridge** — ImGuizmo runs in a small DLL; ImGui.NET and Silk.NET stay as they are.

## What this feature explicitly does not do (v1)

- Snap to grid or angle increments.
- Multi-select or shared pivot.
- View cube / orientation widget.
- Non-Windows platforms (missing DLL → gizmos off, editor still runs).
- Replacing camera icon/frustum gizmos (those stay on the scene framebuffer path).
- Migrating off ImGui.NET to another binding stack.

## Key terminology

**ImGuizmo.** A Dear ImGui companion that draws and interacts with 3D transform gizmos given view and projection matrices and a model matrix.

**ImGui context.** The native Dear ImGui state owned by `cimgui` / ImGui.NET. ImGuizmo must use the exact same context pointer as the running UI, or draws and input will not match.

**Native bridge DLL.** A Windows x64 library that compiles ImGuizmo and exposes a tiny C API for P/Invoke. It does not own OpenGL, ECS, or editor state.

**Operation.** Which manipulator is active: translate, rotate, or scale — chosen by the current editor tool mode.

**Mode.** Coordinate space for the operation: local (entity axes) or world (global axes).

**Model matrix.** A 4×4 transform built from the entity’s translation, rotation, and scale. ImGuizmo edits this matrix; the editor decomposes it back onto the transform component.

**Viewport rect.** The screen rectangle of the scene image inside the ImGui viewport window. ImGuizmo must be told this rect so hit-testing and drawing align with the framebuffer.

**IsOver / IsUsing.** ImGuizmo signals that the mouse is over a handle or that a drag is in progress. The editor uses these to gate camera and picking input.

## Patterns and principles

**Share the context, don’t fork ImGui.** The bridge receives the current ImGui context from managed code. It does not create its own ImGui or replace Silk’s controller.

**Pin the ABI.** The bridge is built against the same Dear ImGui generation that ImGui.NET 1.90.8 ships. Version mismatch is treated as a hard integration bug, not something to paper over at runtime with guesses.

**Thin tools, fat math at the boundary.** Move / Rotate / Scale tools stay thin: pick operation, build/decompose matrix, set rect, call Manipulate. Hit-testing and drawing live inside ImGuizmo.

**Fail soft on native load.** If the DLL is missing or fails to load, log and disable 3D gizmos. The rest of the editor must keep working.

**Editor-only concern.** Packaged games and play mode do not load or call the bridge.

## Architecture philosophy

Treat ImGuizmo as an **editor input/visualization plugin** behind a narrow native boundary. The engine’s transform component remains plain data. The viewport remains the place that knows camera matrices and window bounds. The bridge knows only ImGuizmo and an ImGui context pointer.

Success for v1: with one entity selected, an author can translate, rotate, and scale in local or world space with a perspective-correct 3D gizmo, without fighting the editor camera, on Windows, without changing the ImGui stack.
