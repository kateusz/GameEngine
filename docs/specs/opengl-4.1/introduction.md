# OpenGL 4.1 — Conceptual Introduction

## What Problem Does This Solve?

The engine asks for OpenGL 3.3 and writes every shader as GLSL 330. Lighting, shadows, and IBL already need behavior 330 does not give: a fragment loop indexing a sampler array. That only links because two color shaders enable `GL_ARB_gpu_shader5`. Mesa requires the extension; Apple’s last OpenGL is 4.1, which is also the last version this project will ever target.

Per color frame, the 3D layer walks the cube program and the model program and sets the same view, lights, and shadow knobs twice, as dozens of individual uniforms. Startup compiles those programs from text every process, even when the sources have not changed.

## What the Feature Will Achieve

- Editor, Runtime, and graphics tests create a **4.1 core, forward-compatible** context. Below 4.1, startup fails with a clear error. There is no 3.3 fallback and no second shader tree.
- Every GLSL file is `#version 410 core`. Dynamic sampler indexing is core. The gpu_shader5 lines go away.
- Cube and model color programs share one **std140 frame block** (view, lights, numeric shadow knobs, IBL flag). `BeginScene` writes that buffer once. Material values, instance flags, and texture units stay per draw.
- Setting a uniform does not require the program to be bound first (4.1 program uniforms). Draws still bind the program they use.
- Cubemap face filtering stays the global seamless enable already done at renderer init. This work does not add a second path.
- 2D then 3D, depth on only for 3D, and “no OpenGL outside the platform layer” stay as they are. Scene files do not change.

## Terminology

**4.1 core context** — The window’s OpenGL API version. Forward-compatible so macOS will create it. Not a compatibility profile.

**Frame block** — One uniform buffer object at binding 0. Cube and model vertex and fragment stages both read it. Samplers cannot live in it.

**Program uniform** — A uniform write that names the program in the call. The current program object does not have to be the one being written.

**Expanded source** — Shader text after `#include` files are spliced in.

## How It Works

The window options request OpenGL 4.1 core. After the context exists, renderer init reads the major and minor version. Too old throws the existing renderer initialization error with the version that was found.

Shader load still goes through the shader factory. It expands includes and compiles from GLSL. Linked cube and model programs bind their `Frame` block to point 0. Other programs have no such block.

Each color `BeginScene` packs the current camera, lights, and shadow numbers into one CPU struct that matches std140, uploads it, and binds the buffer to point 0. It still binds shadow maps and IBL textures to the same units as today. A cube or mesh draw only writes model, material, and entity values.

2D sprites, lines, depth-only shadows, sky, IBL bake, tonemap, bloom, FXAA, and selection outline keep ordinary uniforms and do not read the frame block. The emissive add pass reuses `modelShader.vert`, which includes `frame.glsl`, so it reads the same binding-0 block as the lit model pass instead of taking `u_ViewProjection` from C#.

If a 4.1 context cannot be created, the process does not open a window on a silent 3.3 path. The user sees the initialization error.

## Out of Scope

- Tessellation, geometry-shader cubemap shadows, viewport arrays, separate shader objects, and indirect draw.
- Compute shaders, shader storage buffers, immutable texture storage, explicit `layout(binding = …)` (4.2), persistent maps, and direct state access (4.5). macOS cannot grow past 4.1.
- A frame block for 2D, post, depth, or bake programs.
- Changing PCF, light counts, pass order, or the depth policy.
- A second renderer backend.
