---
name: rendering-optimization
description: Optimize rendering performance including batch efficiency, draw call reduction, texture atlas usage, state change minimization, overdraw reduction, and GPU pipeline efficiency. Analyzes Graphics2D and Graphics3D rendering paths, shader state management, and OpenGL API usage patterns. Use when frame rate is below target (60 FPS), profiling reveals excessive draw calls, batch efficiency is poor, or conducting rendering performance audits.
---

# Rendering Optimization

## Table of Contents
- [Overview](#overview)
- [When to Use](#when-to-use)
- [Rendering Architecture Overview](#rendering-architecture-overview)
- [Systematic Rendering Audit](#systematic-rendering-audit)
- [Optimization Analysis Areas](#optimization-analysis-areas)
  - [Batch Efficiency Analysis](#1-batch-efficiency-analysis)
  - [Draw Call Reduction](#2-draw-call-reduction)
  - [State Change Minimization](#3-state-change-minimization)
  - [Texture Management](#4-texture-management)
  - [Overdraw Reduction](#5-overdraw-reduction)
  - [GPU Pipeline Efficiency](#6-gpu-pipeline-efficiency)
  - [Render Target Management](#7-render-target-management)
- [Output Format](#output-format)
- [Reference Documentation](#reference-documentation)

## Overview
This skill analyzes and optimizes the OpenGL 3.3+ rendering pipeline, focusing on batch efficiency, draw call reduction, state change minimization, and GPU utilization. It covers both 2D (Graphics2D) and 3D (Graphics3D) rendering paths.

## When to Use
Invoke this skill when:
- Frame rate is below target (60 FPS)
- Draw call count is excessive (>10 for 2D, >50 for 3D)
- Batch efficiency is poor (<1000 quads/batch)
- GPU utilization is low (CPU bottleneck)
- Implementing new rendering features that impact performance
- Conducting rendering performance audits

## Rendering Architecture Overview

### Graphics2D (Batched 2D Rendering)
- **Batch Size**: 10,000 quads per batch (`RenderingConstants.MaxQuads`)
- **Texture Slots**: 16 simultaneous textures (`RenderingConstants.MaxTextureSlots`)
- **Vertex Format**: Position, Color, TexCoord, TexIndex
- **Draw Call**: Single draw for entire batch
- **Location**: `Engine/Renderer/Graphics2D.cs`

### Graphics3D (Immediate-Mode 3D Rendering)
- **Rendering**: Immediate-mode API for 3D primitives
- **Batching**: Limited (per-model draw calls)
- **Location**: `Engine/Renderer/Graphics3D.cs`

### Renderer API (OpenGL Abstraction)
- **Interface**: `IRendererAPI`
- **Implementation**: `SilkNetRendererApi`
- **Location**: `Engine/Platform/SilkNet/`

---

## Systematic Rendering Audit

Follow this workflow for comprehensive rendering performance analysis:

### 1. Baseline Measurement
**Capture frame with profiling tools**:
- Use RenderDoc (`F12` in-game) or Nsight Graphics
- Note current metrics:
  - Frame time (target: <16.67ms for 60 FPS)
  - Draw calls (target: <10 for 2D, <50 for 3D)
  - Batch efficiency (quads per batch)
  - Texture/shader switches
- Check GPU utilization (low usage = CPU bottleneck)

**Tools**: See [references/profiling-tools.md](references/profiling-tools.md) for detailed tool usage.

### 2. Identify Bottlenecks
**Prioritize issues by severity**:

| Symptom | Root Cause | Priority | Fix Section |
|---------|------------|----------|-------------|
| Frame time >20ms, low GPU usage | CPU bottleneck (draw calls) | Critical | [#2](#2-draw-call-reduction) |
| High draw calls (>50) | Poor batching | Critical | [#1](#1-batch-efficiency-analysis) |
| Frequent texture switches | No texture atlasing | High | [#4](#4-texture-management) |
| Frequent shader switches | Unordered draw calls | High | [#3](#3-state-change-minimization) |
| High GPU usage, low triangle count | Overdraw | Medium | [#5](#5-overdraw-reduction) |
| GPU stalls in profiler | CPU-GPU sync points | Medium | [#6](#6-gpu-pipeline-efficiency) |

### 3. Apply Optimizations
**Implement fixes from relevant sections below**:
- Start with highest-priority bottlenecks
- Apply 1-2 optimizations at a time
- Measure impact after each change

### 4. Validate & Iterate
**Re-capture frame and measure improvement**:
- Compare new metrics to baseline
- Target: 60 FPS (16.67ms/frame), <10 draw calls (2D)
- If target not met, repeat from step 2

---

## Optimization Analysis Areas

### 1. Batch Efficiency Analysis

**Goal**: Maximize quads per batch, minimize draw calls.

**Key Metrics**:
- Quads per batch (target: close to 10,000)
- Batches per frame (fewer is better)
- Batch flush reasons (why batches break)

**Common Issue**: Batch breaks on every sprite due to different textures.

```csharp
// ❌ BAD - Each texture may break the batch
foreach (var entity in entities)
{
    var sprite = entity.GetComponent<SpriteRendererComponent>();
    Graphics2D.DrawQuad(position, size, LoadTexture(sprite.TexturePath));
}

// ✅ GOOD - Use texture atlas (all sprites share one texture)
var atlas = TextureAtlas.Load("sprites_atlas.png");
foreach (var entity in entities)
{
    var sprite = entity.GetComponent<SpriteRendererComponent>();
    var region = atlas.GetRegion(sprite.SpriteName);
    Graphics2D.DrawQuad(position, size, atlas.Texture, region);
    // All sprites batch efficiently - single draw call!
}
```

**Batch Break Reasons**:
1. **Texture limit exceeded**: More than 16 unique textures
2. **Quad limit exceeded**: More than 10,000 quads
3. **State change**: Shader, blend mode, or render target change
4. **Manual flush**: Explicit batch submit

**Optimization Strategies**:
- Combine textures into atlases (see [examples/texture-atlas-example.cs](references/examples/texture-atlas-example.cs))
- Sort draw calls by texture
- Reuse textures via TextureFactory caching
- Reduce unique texture count per frame

---

### 2. Draw Call Reduction

**Goal**: Minimize glDraw* calls per frame.

**Target Metrics**: 2D < 10 draws, 3D < 50 draws, UI < 5 draws per frame.

**Graphics2D Auto-Batching**: Automatically batches up to 10,000 quads into single draw call. Flushes only when batch full or state changes.

**Optimization Strategies**:
- Enable batching for all sprite rendering
- Use instanced rendering for repeated objects
- Combine small meshes into single mesh
- Sort by material/texture to reduce state changes

---

### 3. State Change Minimization

**Goal**: Reduce expensive OpenGL state changes.

**State Cost Hierarchy** (expensive to cheap):
1. **Shader program**: Most expensive (100-1000 cycles)
2. **Texture binding**: Moderate cost (10-50 cycles)
3. **Blend mode**: Low cost (5-10 cycles)
4. **Depth testing**: Low cost (1-5 cycles)

**Current State Caching**: RenderCommand caches current state (blend mode, shader, etc.) to skip redundant OpenGL calls.

**Optimization Strategy**: Sort draw calls by state (shader first, then texture, then blend mode).

```csharp
// ❌ BAD - Frequent shader switches
foreach (var entity in entities)
{
    UseShader(entity.HasWireframe ? wireframeShader : defaultShader);
    DrawEntity(entity);
}

// ✅ GOOD - Batch by shader
UseShader(defaultShader);
foreach (var entity in entities.Where(e => !e.HasWireframe))
    DrawEntity(entity);

UseShader(wireframeShader);
foreach (var entity in entities.Where(e => e.HasWireframe))
    DrawEntity(entity);
```

**Full Example**: See [examples/batch-sorting-example.cs](references/examples/batch-sorting-example.cs) for complete implementation.

---

### 4. Texture Management

**Goal**: Minimize texture switches, maximize atlas usage.

**Current Texture System**:
- **TextureFactory**: Caches loaded textures (automatic reuse)
- **Graphics2D**: Supports 16 texture slots (tracks bound textures)
- **Texture Atlas**: Opportunity for optimization

**Optimization Strategies**:

**A. Texture Atlasing** (Best approach):
```csharp
// Create atlas from individual sprites
var atlas = new TextureAtlas(2048, 2048);
atlas.AddTexture("player.png");
atlas.AddTexture("enemy.png");
atlas.AddTexture("bullet.png");
atlas.Build();

// All sprites now use one texture - perfect batching
Graphics2D.DrawQuad(pos, size, atlas.Texture, atlas.GetRegion("player.png"));
```

**Implementation**: See [examples/texture-atlas-example.cs](references/examples/texture-atlas-example.cs)

**B. Texture Reuse** (Automatic via TextureFactory): TextureFactory caches loaded textures - multiple loads return same instance.

**C. Texture Slot Tracking** (Automatic in Graphics2D): Tracks bound textures to avoid redundant binds (up to 16 slots).

---

### 5. Overdraw Reduction

**Goal**: Minimize pixels drawn multiple times.

**Overdraw Sources**:
1. Overlapping opaque sprites (no depth testing)
2. Transparent objects in wrong order
3. Occluded geometry not culled
4. Stacked UI elements

**Optimization Strategies**:

**A. Back-to-Front Sorting** (for 2D transparency):
```csharp
// Sort transparent sprites by depth (back-to-front)
var sprites = entities
    .Where(e => e.HasComponent<SpriteRendererComponent>())
    .OrderByDescending(e => e.GetComponent<TransformComponent>().Translation.Z);

foreach (var entity in sprites)
    DrawSprite(entity);
```

**B. Frustum Culling** (skip off-screen entities):
```csharp
// Only draw visible entities
foreach (var entity in entities)
{
    if (CameraSystem.IsInView(entity, camera))
        DrawEntity(entity);
}
```

**C. Depth Pre-Pass** (for 3D with complex shaders): Render depth-only pass first (cheap), then full shading pass (expensive shader only on visible pixels). Use `GL.DepthFunc(Equal)` in second pass.

**Advanced**: Depth pre-pass details, occlusion culling → [references/advanced-gpu-patterns.md](references/advanced-gpu-patterns.md)

---

### 6. GPU Pipeline Efficiency

**Goal**: Avoid CPU-GPU synchronization stalls.

**Common Sync Points** (avoid): `glReadPixels()`, `glMapBuffer(read)`, blocking query objects.

**Key Patterns**:
- **Buffer Orphaning**: Call `GL.BufferData(null)` before `BufferSubData` to avoid sync on dynamic VBOs
- **Vertex Attributes over Uniforms**: Graphics2D uses per-vertex attributes (color, texIndex) instead of per-draw uniforms

**Advanced**: PBOs for async reads, UBOs, multi-draw indirect → [references/advanced-gpu-patterns.md](references/advanced-gpu-patterns.md)

---

### 7. Render Target Management

**Goal**: Minimize framebuffer switches.

**Strategy**: Batch all rendering to same target together. Render all shadow casters to shadow FBO, then all scene geometry to main FBO, then composite to screen. Avoid frequent FBO switches.

---

## Output Format

When reporting optimization issues, use this structured format:

**Issue**: [Rendering inefficiency description]
**Impact**: [Performance cost - ms/frame, draw calls, memory]
**Location**: [File path and line number]
**Optimization**: [Specific fix with code example]
**Expected Improvement**: [Estimated performance gain]
**Priority**: [Critical/High/Medium/Low]

### Example Output

```text
**Issue**: Excessive draw calls due to per-sprite texture switching
**Impact**: 500 draw calls/frame (target: <10), frame time 25ms (target: 16.67ms)
**Location**: Engine/Scene/Systems/SpriteRenderingSystem.cs:45

**Optimization**:
// Create texture atlas (see references/examples/texture-atlas-example.cs)
var atlas = TextureAtlas.Create("sprites", 2048, 2048);
atlas.AddSprites(allSpriteTextures);

// Batch all sprites using atlas
foreach (var entity in entities)
{
    var region = atlas.GetRegion(entity.SpriteName);
    Graphics2D.DrawQuad(pos, size, atlas.Texture, region);
}
// Result: 1 draw call for all sprites instead of 500

**Expected Improvement**: Frame time: 25ms → 10ms, Draw calls: 500 → 1
**Priority**: Critical
```

---

## Reference Documentation

### Detailed Guides
- **[Profiling Tools](references/profiling-tools.md)**: RenderDoc, Nsight Graphics, dotnet-trace usage
- **[Advanced GPU Patterns](references/advanced-gpu-patterns.md)**: PBOs, buffer orphaning, UBOs, depth pre-pass, multi-draw indirect

### Code Examples
- **[Texture Atlas Implementation](references/examples/texture-atlas-example.cs)**: Complete atlas system
- **[Batch Sorting](references/examples/batch-sorting-example.cs)**: State-based draw call sorting

### Engine Resources
- **StatsPanel**: `Editor/Panels/StatsPanel.cs` - Real-time performance metrics
- **RenderingConstants**: `Engine/Renderer/RenderingConstants.cs` - MaxQuads, MaxTextureSlots
- **Graphics2D**: `Engine/Renderer/Graphics2D.cs` - Batched 2D renderer implementation
