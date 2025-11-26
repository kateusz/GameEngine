---
name: shader-optimization
description: Optimize GLSL shaders in OpenGL 3.3+ rendering pipelines for maximum GPU performance. Analyzes fragment shader costs, vertex shader efficiency, dynamic branching, texture sampling patterns, uniform buffer usage, and batch rendering compatibility. Use when shaders are slow, contain expensive operations, or need refactoring for efficiency.
---

# Shader Optimization

## Overview
Analyzes and optimizes GLSL shader code for maximum GPU efficiency in the OpenGL 3.3+ rendering pipeline. This skill focuses on reducing pixel cost, eliminating GPU divergence, optimizing memory access, and improving batch rendering compatibility.

## When to Use
Invoke this skill when encountering:
- Fragment shaders with high pixel cost or overdraw
- Shaders using expensive operations (pow, sin, cos, sqrt) per-pixel
- Dynamic branching causing GPU divergence
- Texture lookup patterns needing optimization
- Batch rendering efficiency concerns
- Shader compilation errors or warnings
- Uniform buffer update performance issues
- Compatibility issues across graphics hardware

## Optimization Analysis Areas

### 1. Fragment Shader Cost Reduction
**Goal**: Minimize per-pixel computation cost

- **Identify expensive operations**: `pow()`, `sin()`, `cos()`, `sqrt()`, `normalize()`, `dot()`, `cross()`
- **Move to vertex shader**: Calculations that vary per-vertex (lighting directions, texture coordinates)
- **Use cheaper approximations**: Lookup tables for trigonometric functions, fast inverse square root
- **Eliminate redundant calculations**: Hoist invariant expressions outside loops
- **Reduce texture-dependent calculations**: Minimize operations that depend on texture lookups

**Example Optimization**:
```glsl
// BEFORE (expensive per-pixel)
void main() {
    vec3 normal = normalize(vNormal);
    vec3 lightDir = normalize(uLightPos - vFragPos);
    float diff = max(dot(normal, lightDir), 0.0);
    fragColor = vec4(diff * uColor.rgb, 1.0);
}

// AFTER (normal and lightDir computed in vertex shader)
// Vertex Shader
vNormalNorm = normalize(normal);
vLightDir = normalize(uLightPos - position.xyz);

// Fragment Shader
void main() {
    float diff = max(dot(vNormalNorm, vLightDir), 0.0);
    fragColor = vec4(diff * uColor.rgb, 1.0);
}
```

### 2. Dynamic Branching Analysis
**Goal**: Eliminate GPU divergence and improve warp efficiency

- **Flag problematic branches**: Texture-dependent branches, per-pixel conditionals
- **Suggest branch elimination**: Use `step()`, `mix()`, `smoothstep()` instead of `if/else`
- **Recommend predication**: Multiply results by condition flags
- **Early-exit patterns**: Discard fragments as early as possible
- **Uniform branching**: Prefer uniform-based branches (entire draw call uses same path)

**Example Optimization**:
```glsl
// BEFORE (dynamic branching - causes divergence)
if (texColor.a < 0.1)
    discard;
fragColor = texColor * uTint;

// AFTER (predication - no branching)
float alpha = step(0.1, texColor.a);
fragColor = vec4(texColor.rgb * uTint.rgb, texColor.a * uTint.a * alpha);
```

### 3. Texture Sampling Optimization
**Goal**: Reduce texture fetch latency and improve cache coherency

- **Dependent texture reads**: Identify texture coordinates computed from previous texture reads
- **Texture fetch count**: Minimize total texture samples per fragment
- **Mipmapping**: Ensure proper mipmap usage for minification
- **Texture atlasing**: Recommend atlas usage for batch rendering (see `Graphics2D` batching)
- **Sampler states**: Verify appropriate filtering modes (nearest, linear, trilinear)
- **Texture format**: Suggest optimal formats (RGB vs RGBA, compressed formats)

**Key Pattern in Engine**:
```glsl
// Engine uses texture atlasing for batching (see Graphics2D.cs)
// Supports up to 16 texture slots (RenderingConstants.MaxTextureSlots)
uniform sampler2D uTextures[16];
varying float vTexIndex;

vec4 texColor = texture(uTextures[int(vTexIndex)], vTexCoord);
```

### 4. Uniform Buffer Optimization
**Goal**: Reduce CPU-GPU synchronization overhead

- **Identify per-frame uniforms**: Group into uniform buffer objects (UBOs)
- **Batch-compatible uniforms**: Check compatibility with batched rendering
- **Uniform update frequency**: Flag uniforms updated unnecessarily
- **Immutable uniforms**: Identify constants that could be hardcoded or moved to textures
- **Uniform buffer layout**: std140 layout for compatibility

**Engine Pattern**:
```glsl
// Per-frame uniforms (updated once per frame)
uniform mat4 uViewProjection;

// Per-object uniforms (batched via vertex attributes)
varying vec4 vColor;
varying float vTexIndex;
```

### 5. Vertex Shader Efficiency
**Goal**: Minimize vertex processing cost

- **Move calculations from fragment shader**: Leverage GPU interpolation
- **Reduce attribute count**: Pack data efficiently (combine attributes)
- **Transform feedback**: Check if geometry shader alternative is better
- **Instancing compatibility**: Verify shader works with instanced rendering

### 6. Batch Rendering Compatibility
**Goal**: Ensure shaders work with engine's batched rendering system

- **Verify texture indexing**: Must support dynamic texture array indexing
- **Per-vertex attributes**: Use varying instead of uniforms for per-object data
- **Batch size limits**: Check against `RenderingConstants.MaxQuads` (10,000)
- **State changes**: Minimize shader program switches

**Engine Batching Requirements**:
- Support up to 16 textures (`RenderingConstants.MaxTextureSlots`)
- Per-quad data via vertex attributes (color, texture index)
- Single draw call for 10,000 quads
- See: `Engine/Renderer/Graphics2D.cs`

### 7. Cross-Platform Compatibility
**Goal**: Ensure shaders work across OpenGL 3.3+ implementations

- **GLSL version**: Verify `#version 330 core` compatibility
- **Extension usage**: Flag non-standard extensions
- **Precision qualifiers**: Check for mobile compatibility (highp, mediump, lowp)
- **Built-in variables**: Use standard gl_Position, gl_FragCoord, etc.
- **Platform-specific issues**: Test on Windows, macOS, Linux via `Platform/SilkNet/`

## Profiling Recommendations

### GPU Performance Measurement
1. **Frame time analysis**: Measure before/after with engine's profiler
2. **Overdraw visualization**: Enable depth pre-pass to identify overdraw hotspots
3. **GPU profiler**: Use platform-specific tools (RenderDoc, Nsight, Instruments)
4. **Batch count tracking**: Monitor draw call reduction via `Graphics2D` stats

### Validation Process
1. Run benchmark in `Benchmark` project
2. Compare frame times with reference scenes
3. Verify visual correctness across platforms
4. Check for shader compilation warnings
5. Test with max batch size (10,000 quads)

## Output Format
Provide findings as:

**Issue**: [Shader inefficiency description]
**Impact**: [GPU cost - ms/frame, pixel cost, draw call count]
**Location**: [Shader file path and line number]
**Optimization**: [Code change with before/after examples]
**Compatibility**: [Platform considerations]
**Priority**: [Critical/High/Medium/Low]

### Example Output
```text
**Issue**: Expensive per-pixel normalization in fragment shader
**Impact**: ~0.5ms/frame at 1080p (30% of fragment shader cost)
**Location**: Engine/Renderer/Shaders/Sprite.frag:12
**Optimization**:
// BEFORE
vec3 normal = normalize(vNormal);

// AFTER (normalize in vertex shader, interpolate)
// Vertex: vNormal = normalize(aNormal);
// Fragment: vec3 normal = vNormal; // Already normalized

**Compatibility**: Works across all OpenGL 3.3+ platforms
**Priority**: High
```

## Reference Documentation
- **Rendering Pipeline**: `docs/modules/rendering-pipeline.md`
- **OpenGL Workflows**: `docs/opengl-rendering/`
- **Renderer Architecture**: `CLAUDE.md` - Renderer Architecture section
- **Constants**: `Engine/Renderer/RenderingConstants.cs`
- **Batching Implementation**: `Engine/Renderer/Graphics2D.cs`

## Integration with Agents
This skill complements the **game-engine-expert** agent. Use this skill for shader analysis, then delegate to game-engine-expert for low-level OpenGL implementation details.

## Tool Restrictions
None - this skill may read shader files, analyze rendering code, and suggest optimizations with full access to engine source.
