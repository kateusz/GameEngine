# Advanced GPU Optimization Patterns

Advanced techniques for GPU pipeline efficiency and CPU-GPU synchronization.

## CPU-GPU Synchronization

### Problem: Pipeline Stalls
CPU and GPU run asynchronously. Certain operations force synchronization, stalling the pipeline and killing performance.

**Stall-Causing Operations**:
- `glReadPixels()` - CPU waits for GPU to finish rendering
- `glGetTexImage()` - Downloads texture data (blocks)
- `glMapBuffer()` with read access - Waits for GPU writes
- Query objects (`glGetQueryObjectiv`) - Blocks if result not ready

---

### Pattern: Asynchronous Pixel Reads with PBOs

**Problem**: `glReadPixels()` causes immediate CPU-GPU sync stall.

**Solution**: Use Pixel Buffer Objects (PBOs) for asynchronous reads.

```csharp
// ❌ BAD - Synchronous read (stalls pipeline)
byte[] pixels = new byte[width * height * 4];
GL.ReadPixels(0, 0, width, height, PixelFormat.Rgba, PixelType.UnsignedByte, pixels);
// CPU blocked here waiting for GPU!

// ✅ GOOD - Asynchronous read with PBO
uint pbo = GL.GenBuffer();
GL.BindBuffer(BufferTarget.PixelPackBuffer, pbo);
GL.BufferData(BufferTarget.PixelPackBuffer, width * height * 4, IntPtr.Zero, BufferUsageHint.StreamRead);

// Start async read (doesn't block)
GL.ReadPixels(0, 0, width, height, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);

// ... do other work while GPU reads ...

// Later: Map buffer to get data (may still block, but minimized)
IntPtr ptr = GL.MapBuffer(BufferTarget.PixelPackBuffer, BufferAccess.ReadOnly);
// Copy data
GL.UnmapBuffer(BufferTarget.PixelPackBuffer);
```

**Double-Buffered PBO** (eliminates stalls completely):
```csharp
// Use two PBOs - alternate between them
uint[] pbos = new uint[2];
int currentIndex = 0;

void ReadPixelsAsync()
{
    int nextIndex = 1 - currentIndex;

    // Read into next PBO (async)
    GL.BindBuffer(BufferTarget.PixelPackBuffer, pbos[nextIndex]);
    GL.ReadPixels(/* ... */);

    // Get data from current PBO (previous frame's read - should be ready)
    GL.BindBuffer(BufferTarget.PixelPackBuffer, pbos[currentIndex]);
    IntPtr ptr = GL.MapBuffer(/* ... */); // No stall - data ready
    // Use data
    GL.UnmapBuffer(/* ... */);

    currentIndex = nextIndex; // Swap
}
```

---

### Pattern: Buffer Orphaning

**Problem**: Updating dynamic buffers (VBOs) can stall if GPU is still using them.

**Solution**: "Orphan" the old buffer to avoid synchronization.

```csharp
// ❌ BAD - May stall if GPU still reading buffer
GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
GL.BufferSubData(BufferTarget.ArrayBuffer, 0, size, newData);
// GPU may still be using old data - driver must wait or copy!

// ✅ GOOD - Orphan old buffer (avoid sync)
GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);

// Pass null data - tells driver to allocate new buffer, orphan old one
GL.BufferData(BufferTarget.ArrayBuffer, size, IntPtr.Zero, BufferUsageHint.DynamicDraw);

// Now upload new data to fresh buffer (no stall)
GL.BufferSubData(BufferTarget.ArrayBuffer, 0, size, newData);
```

**How it works**:
- `BufferData(null)` tells OpenGL: "I don't need old data, give me new buffer"
- Driver allocates new buffer memory
- Old buffer memory is freed when GPU finishes (async)
- No CPU-GPU sync required

**Engine Usage**: Graphics2D uses this for dynamic quad batches.

---

### Pattern: Unsynchronized Mapping

**Problem**: `glMapBuffer()` may block waiting for GPU writes to complete.

**Solution**: Use `GL_MAP_UNSYNCHRONIZED_BIT` if you know data isn't in use.

```csharp
// ❌ Potentially blocking
IntPtr ptr = GL.MapBuffer(BufferTarget.ArrayBuffer, BufferAccess.WriteOnly);

// ✅ Non-blocking (if you guarantee GPU isn't using this region)
IntPtr ptr = GL.MapBufferRange(
    BufferTarget.ArrayBuffer,
    offset,
    length,
    MapBufferAccessMask.MapWriteBit | MapBufferAccessMask.MapUnsynchronizedBit
);
```

**⚠️ Warning**: Only use `MapUnsynchronizedBit` if you're certain the GPU isn't accessing the buffer region. Use with buffer orphaning or ring buffers.

---

## Advanced Uniform Management

### Problem: Per-Draw Uniform Updates
Updating uniforms for every draw call is CPU-expensive and breaks batching.

---

### Pattern: Uniform Buffer Objects (UBOs)

**Solution**: Batch uniform data in UBOs, update once per frame.

```csharp
// ❌ BAD - Update uniform per draw call (slow)
foreach (var entity in entities)
{
    shader.SetMatrix4("uModel", entity.Transform);
    shader.SetVector3("uColor", entity.Color);
    DrawEntity(entity);
    // 2 uniform updates × 1000 entities = 2000 GL calls!
}

// ✅ GOOD - Use UBO for shared data
// Create UBO
uint ubo = GL.GenBuffer();
GL.BindBuffer(BufferTarget.UniformBuffer, ubo);
GL.BufferData(BufferTarget.UniformBuffer, sizeof(CameraData), IntPtr.Zero, BufferUsageHint.DynamicDraw);
GL.BindBufferBase(BufferTarget.UniformBuffer, 0, ubo);

// Upload once per frame
CameraData data = new() { ViewProj = camera.ViewProjection, CameraPos = camera.Position };
GL.BufferSubData(BufferTarget.UniformBuffer, 0, sizeof(CameraData), ref data);

// All shaders access via binding point 0 (no per-draw updates)
```

**Shader Side**:
```glsl
// Binding point 0
layout(std140, binding = 0) uniform Camera
{
    mat4 uViewProj;
    vec3 uCameraPos;
};
```

---

### Pattern: Vertex Attributes Instead of Uniforms

**Best for**: Per-instance data (used by Graphics2D).

```csharp
// ❌ BAD - Uniform per quad
for (int i = 0; i < 1000; i++)
{
    shader.SetVector3("uColor", colors[i]);
    DrawQuad(i);
    // 1000 uniform updates + 1000 draw calls
}

// ✅ GOOD - Color as vertex attribute
// Upload all colors as vertex data
struct QuadVertex
{
    Vector3 Position;
    Vector4 Color; // Per-vertex attribute
    Vector2 TexCoord;
}

// Single draw call for all quads
GL.DrawElements(PrimitiveType.Triangles, quadCount * 6, ...);
```

**Graphics2D uses this**: Color, texture index, etc. are vertex attributes, not uniforms.

---

## Depth Pre-Pass (Z-Prepass)

**Problem**: Overdraw on opaque geometry - fragment shader runs multiple times per pixel.

**Solution**: Two-pass rendering: depth-only first pass, then full shading.

```csharp
// ✅ First Pass: Depth Only (cheap)
GL.UseProgram(depthOnlyShader); // Simple shader (no lighting/textures)
GL.ColorMask(false, false, false, false); // Don't write color
GL.DepthMask(true); // Write depth

DrawAllOpaqueGeometry(); // Establishes depth buffer

// ✅ Second Pass: Full Shading (expensive, but only visible pixels)
GL.ColorMask(true, true, true, true); // Write color
GL.DepthMask(false); // Don't write depth (already have it)
GL.DepthFunc(DepthFunction.Equal); // Only draw if depth matches (early-Z test)

GL.UseProgram(fullShadingShader); // Complex shader (lighting, textures, etc.)
DrawAllOpaqueGeometry(); // Expensive shader only runs on visible pixels!

// Restore state
GL.DepthFunc(DepthFunction.Less);
GL.DepthMask(true);
```

**When to use**:
- Complex fragment shaders (lighting, multiple textures)
- High-poly 3D scenes with depth complexity
- **Don't use for**: Simple 2D games (overhead not worth it)

---

## Occlusion Culling with Query Objects

**Problem**: Drawing geometry behind walls wastes GPU cycles.

**Solution**: Test if bounding box is visible before drawing full geometry.

```csharp
// Generate query object
uint query = GL.GenQuery();

// Render bounding box (no color/depth writes)
GL.ColorMask(false, false, false, false);
GL.DepthMask(false);
GL.BeginQuery(QueryTarget.AnySamplesPassed, query);

DrawBoundingBox(entity); // Simple box geometry

GL.EndQuery(QueryTarget.AnySamplesPassed);
GL.ColorMask(true, true, true, true);
GL.DepthMask(true);

// Check result (may block - use previous frame's result in production)
GL.GetQueryObject(query, GetQueryObjectParam.QueryResult, out int visible);

if (visible > 0)
{
    DrawFullGeometry(entity); // Only draw if visible
}
```

**Asynchronous Approach** (no stall):
```csharp
// Use previous frame's query result (1 frame latency is acceptable)
if (prevFrameQueryResults[entity] > 0)
{
    DrawFullGeometry(entity);
}

// Start query for next frame
GL.BeginQuery(/* ... */);
DrawBoundingBox(entity);
GL.EndQuery(/* ... */);
```

---

## Multi-Draw Indirect

**Problem**: Thousands of draw calls for similar geometry (e.g., instanced objects with different properties).

**Solution**: `glMultiDrawElementsIndirect` - single call for multiple draws.

```csharp
// ❌ BAD - 1000 draw calls
foreach (var mesh in meshes)
{
    GL.BindVertexArray(mesh.VAO);
    GL.DrawElements(PrimitiveType.Triangles, mesh.IndexCount, ...);
}

// ✅ GOOD - 1 draw call for all meshes
// Build indirect command buffer
struct DrawCommand
{
    uint IndexCount;
    uint InstanceCount;
    uint FirstIndex;
    uint BaseVertex;
    uint BaseInstance;
}

DrawCommand[] commands = BuildCommandBuffer(meshes);

// Upload to GPU
GL.BindBuffer(BufferTarget.DrawIndirectBuffer, indirectBuffer);
GL.BufferData(BufferTarget.DrawIndirectBuffer, commands.Length * sizeof(DrawCommand), commands, BufferUsageHint.DynamicDraw);

// Single draw call for all
GL.MultiDrawElementsIndirect(PrimitiveType.Triangles, DrawElementsType.UnsignedInt, IntPtr.Zero, commands.Length, 0);
```

**Requirements**: OpenGL 4.3+ or `ARB_multi_draw_indirect` extension.

---

## Summary

| Technique | Use Case | Complexity | Impact |
|-----------|----------|------------|--------|
| Async PBO Reads | Screenshot, picking | Medium | Eliminates stalls |
| Buffer Orphaning | Dynamic VBOs | Low | Avoids sync points |
| UBOs | Shared uniform data | Medium | Reduces GL calls |
| Vertex Attributes | Per-instance data | Low | Best batching |
| Depth Pre-Pass | Complex shaders + overdraw | High | 2-3× speedup |
| Occlusion Culling | Large 3D scenes | High | Variable (scene-dependent) |
| Multi-Draw Indirect | Many similar meshes | High | 10-100× fewer draw calls |

**Recommendation**: Start with low-complexity techniques (buffer orphaning, vertex attributes). Add advanced patterns (depth pre-pass, multi-draw) only when profiling identifies specific bottlenecks.
