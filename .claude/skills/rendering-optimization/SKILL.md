---
name: rendering-optimization
description: Optimize rendering performance including batch efficiency, draw call reduction, texture atlas usage, state change minimization, overdraw reduction, and GPU pipeline efficiency. Analyzes Graphics2D and Graphics3D rendering paths, shader state management, and OpenGL API usage patterns. Use for frame rate optimization and rendering bottleneck analysis.
---

# Rendering Optimization

## Overview
This skill analyzes and optimizes the OpenGL 3.3+ rendering pipeline, focusing on batch efficiency, draw call reduction, state change minimization, and GPU utilization. It covers both 2D (Graphics2D) and 3D (Graphics3D) rendering paths.

## When to Use
Invoke this skill when:
- Frame rate is below target (60 FPS)
- Draw call count is excessive
- Batch efficiency is poor
- Texture switches are frequent
- State changes cause performance drops
- GPU utilization is low (CPU bottleneck)
- Overdraw is suspected
- Rendering profiling shows bottlenecks

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

## Optimization Analysis Areas

### 1. Batch Efficiency Analysis

**Goal**: Maximize quads per batch, minimize draw calls

**Key Metrics**:
- Quads per batch (target: close to 10,000)
- Batches per frame (fewer is better)
- Batch flush reasons (track why batches break)

**Common Issues**:
```csharp
// ❌ BAD - Batch breaks on every sprite (different textures)
foreach (var entity in entities)
{
    var sprite = entity.GetComponent<SpriteRendererComponent>();
    Graphics2D.DrawQuad(position, size, LoadTexture(sprite.TexturePath));
    // Each different texture may break the batch!
}

// ✅ GOOD - Use texture atlas to keep same texture
var atlas = TextureAtlas.Load("sprites_atlas.png");
foreach (var entity in entities)
{
    var sprite = entity.GetComponent<SpriteRendererComponent>();
    var region = atlas.GetRegion(sprite.SpriteName);
    Graphics2D.DrawQuad(position, size, atlas.Texture, region);
    // All sprites use same atlas texture - batches efficiently!
}
```

**Batch Break Reasons**:
1. **Texture limit exceeded**: More than 16 unique textures
2. **Quad limit exceeded**: More than 10,000 quads
3. **State change**: Shader, blend mode, or render target change
4. **Manual flush**: Explicit batch submit

**Optimization Strategies**:
- Combine textures into atlases
- Sort draw calls by texture
- Reuse textures across frames (caching)
- Reduce unique texture count per frame

### 2. Draw Call Reduction

**Goal**: Minimize glDraw* calls per frame

**Current Engine Patterns**:
```csharp
// Graphics2D batching (automatic)
public class Graphics2D
{
    private int _quadCount = 0;
    private const int MaxQuads = 10000;

    public void DrawQuad(/* params */)
    {
        if (_quadCount >= MaxQuads)
        {
            Flush(); // Draw call here
            Reset();
        }

        // Add quad to batch
        _quadCount++;
    }

    public void Flush()
    {
        // Single draw call for all quads in batch
        GL.DrawElements(PrimitiveType.Triangles, _quadCount * 6, ...);
    }
}
```

**Target Metrics**:
- 2D Scene: < 10 draw calls per frame (depends on sprite count)
- 3D Scene: < 50 draw calls per frame (depends on model count)
- UI: < 5 draw calls per frame

**Optimization Strategies**:
- Enable batching for all sprite rendering
- Use instanced rendering for repeated objects
- Combine small meshes into single mesh
- Sort by material/texture to reduce state changes

### 3. State Change Minimization

**Goal**: Reduce OpenGL state changes (expensive)

**State Categories**:
1. **Shader program**: Most expensive
2. **Texture binding**: Moderate cost
3. **Blend mode**: Low cost
4. **Depth testing**: Low cost
5. **Culling mode**: Low cost

**Current State Caching**:
```csharp
public class RenderCommand
{
    private static BlendMode _currentBlendMode = BlendMode.None;
    private static uint _boundShader = 0;

    public static void SetBlendMode(BlendMode mode)
    {
        if (_currentBlendMode == mode)
            return; // Skip redundant state change

        // Apply state change
        // ...
        _currentBlendMode = mode;
    }
}
```

**Optimization Strategies**:
- Cache current OpenGL state
- Skip redundant state changes
- Group draw calls by state (shader, texture, blend mode)
- Use state batching (change multiple states once)

**Example Optimization**:
```csharp
// ❌ BAD - Frequent shader switches
foreach (var entity in entities)
{
    if (entity.HasWireframe)
        UseShader(wireframeShader);
    else
        UseShader(defaultShader);

    DrawEntity(entity);
}

// ✅ GOOD - Batch by shader
// Draw all default shader entities first
UseShader(defaultShader);
foreach (var entity in entities.Where(e => !e.HasWireframe))
{
    DrawEntity(entity);
}

// Then draw all wireframe entities
UseShader(wireframeShader);
foreach (var entity in entities.Where(e => e.HasWireframe))
{
    DrawEntity(entity);
}
```

### 4. Texture Management

**Goal**: Minimize texture switches, maximize atlas usage

**Current Texture System**:
- **TextureFactory**: Caches loaded textures
- **Graphics2D**: Supports 16 texture slots
- **Texture Atlas**: Not fully utilized (opportunity!)

**Optimization Strategies**:

**A. Texture Atlasing**:
```csharp
// Create atlas from individual sprites
var atlas = new TextureAtlas(2048, 2048);
atlas.AddTexture("player.png");
atlas.AddTexture("enemy.png");
atlas.AddTexture("bullet.png");
atlas.Build();

// All sprites now use one texture
Graphics2D.DrawQuad(pos, size, atlas.Texture, atlas.GetRegion("player.png"));
```

**B. Texture Reuse**:
```csharp
// ✅ GOOD - Factory caches textures
public class TextureFactory
{
    private readonly Dictionary<string, Texture> _cache = new();

    public Texture Load(string path)
    {
        if (_cache.TryGetValue(path, out var texture))
            return texture; // Reuse cached texture

        texture = LoadFromDisk(path);
        _cache[path] = texture;
        return texture;
    }
}
```

**C. Texture Slot Tracking**:
```csharp
// Graphics2D tracks texture slots to avoid redundant binds
private int GetTextureIndex(Texture texture)
{
    for (int i = 0; i < _textureSlotIndex; i++)
    {
        if (_textureSlots[i] == texture.RendererID)
            return i; // Texture already bound
    }

    // Add new texture
    int index = _textureSlotIndex++;
    _textureSlots[index] = texture.RendererID;
    return index;
}
```

### 5. Overdraw Reduction

**Goal**: Minimize pixels drawn multiple times

**Overdraw Sources**:
1. Overlapping opaque sprites (no depth testing)
2. Transparent objects drawn in wrong order
3. Occluded geometry not culled
4. UI elements stacked unnecessarily

**Optimization Strategies**:

**A. Depth Pre-Pass** (for 3D):
```csharp
// First pass: Write depth only (cheap)
UseShader(depthOnlyShader);
GL.ColorMask(false, false, false, false);
DrawOpaqueGeometry();

// Second pass: Full shading (expensive pixels only drawn once)
GL.ColorMask(true, true, true, true);
GL.DepthFunc(DepthFunction.Equal); // Only draw if depth matches
UseShader(fullShader);
DrawOpaqueGeometry();
GL.DepthFunc(DepthFunction.Less); // Restore default
```

**B. Back-to-Front Sorting** (for 2D transparency):
```csharp
// Sort transparent sprites by depth
var sprites = entities
    .Where(e => e.HasComponent<SpriteRendererComponent>())
    .OrderByDescending(e => e.GetComponent<TransformComponent>().Translation.Z);

foreach (var entity in sprites)
{
    DrawSprite(entity);
}
```

**C. Frustum Culling**:
```csharp
public class CameraSystem
{
    public bool IsInView(Entity entity, Camera camera)
    {
        var transform = entity.GetComponent<TransformComponent>();
        var bounds = CalculateBounds(entity);

        return camera.Frustum.Intersects(bounds);
    }
}

// Only draw visible entities
foreach (var entity in entities)
{
    if (CameraSystem.IsInView(entity, camera))
    {
        DrawEntity(entity);
    }
}
```

### 6. GPU Pipeline Efficiency

**Goal**: Keep GPU busy, avoid CPU/GPU synchronization stalls

**Common Issues**:

**A. CPU-GPU Sync Points** (avoid):
```csharp
// ❌ BAD - glReadPixels causes pipeline stall
var pixels = GL.ReadPixels(/* ... */); // CPU waits for GPU!

// ✅ BETTER - Use pixel buffer objects (PBO) for async reads
var pbo = GL.GenBuffer();
GL.BindBuffer(BufferTarget.PixelPackBuffer, pbo);
GL.ReadPixels(/* ... */); // Async - doesn't wait
// ... do other work ...
// Read PBO data later (may still block, but minimized)
```

**B. Buffer Orphaning** (for dynamic buffers):
```csharp
// ✅ GOOD - Orphan old buffer to avoid stall
GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
GL.BufferData(BufferTarget.ArrayBuffer, size, IntPtr.Zero, BufferUsageHint.DynamicDraw);
GL.BufferSubData(BufferTarget.ArrayBuffer, 0, size, data);
```

**C. Minimize Uniform Updates**:
```csharp
// ❌ BAD - Update uniforms every draw call
foreach (var entity in entities)
{
    shader.SetMatrix4("uModel", entity.Transform);
    DrawEntity(entity);
}

// ✅ GOOD - Use vertex attributes or UBOs
// Graphics2D approach: per-quad data via vertex attributes (no per-draw uniforms)
```

### 7. Render Target Management

**Goal**: Minimize framebuffer switches

**Optimization Strategies**:
- Batch all rendering to same target together
- Use multi-render targets (MRT) when possible
- Avoid frequent switches between screen and FBO

```csharp
// ✅ GOOD - Batch by render target
// Render to shadow map
BindFramebuffer(shadowFBO);
DrawAllShadowCasters();

// Render to main scene
BindFramebuffer(mainFBO);
DrawAllSceneGeometry();

// Render to screen
BindFramebuffer(0);
DrawFinalComposite();
```

## Performance Profiling

### Key Metrics to Track

1. **Frame Time**: Target < 16.67ms (60 FPS)
2. **Draw Calls**: As few as possible
3. **Triangles/Quads**: How many rendered
4. **Texture Switches**: Minimize
5. **Shader Switches**: Minimize (most expensive)
6. **GPU Utilization**: Should be high (not CPU bottlenecked)

### Engine Stats Panel

```csharp
// Use StatsPanel to monitor rendering metrics
public class StatsPanel
{
    public void OnImGuiRender()
    {
        ImGui.Text($"FPS: {fps}");
        ImGui.Text($"Frame Time: {frameTime:F2} ms");
        ImGui.Text($"Draw Calls: {drawCalls}");
        ImGui.Text($"Quads: {quadCount}");
        ImGui.Text($"Batches: {batchCount}");
    }
}
```

### External Tools

- **RenderDoc**: Frame capture and GPU profiling
- **Nsight Graphics**: NVIDIA GPU profiler
- **Xcode Instruments**: macOS GPU profiling
- **dotnet-trace**: .NET performance profiling

## Output Format

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
// Create texture atlas
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

## Reference Documentation
- **Rendering Pipeline**: `docs/modules/rendering-pipeline.md`
- **OpenGL Workflows**: `docs/opengl-rendering/`
- **Graphics2D**: `Engine/Renderer/Graphics2D.cs`
- **Graphics3D**: `Engine/Renderer/Graphics3D.cs`
- **Constants**: `Engine/Renderer/RenderingConstants.cs`

## Integration with Agents
This skill complements the **game-engine-expert** agent. Use this skill for high-level rendering analysis, then delegate to game-engine-expert for OpenGL implementation details.

## Tool Restrictions
None - this skill may read code, analyze performance, and suggest optimizations with full codebase access.
