# Shader Optimization Command

Optimize GLSL shaders in OpenGL 3.3+ rendering pipelines for maximum GPU performance.

## Command Syntax

```
@shader-optimization [analyze|optimize|review] <shader-path-or-code>
```

## Arguments

- **action**: One of:
    - `analyze` - Identify performance bottlenecks and optimization opportunities
    - `optimize` - Apply optimizations and provide refactored code
    - `review` - Review shader code for batch rendering compatibility and efficiency

- **shader-path-or-code**: Either:
    - File path: `Engine/Renderer/Shaders/ShaderFactory.cs` (or specific shader identifier)
    - Inline code: Paste GLSL shader code directly

## Usage Examples

### Analyze a shader for bottlenecks
```
@shader-optimization analyze Engine/Renderer/Shaders/ShaderFactory.cs:SpriteShader
```

### Optimize specific shader code
```
@shader-optimization optimize
#version 330 core
in vec3 vNormal;
in vec3 vFragPos;
uniform vec3 uLightPos;
void main() {
    vec3 normal = normalize(vNormal);
    vec3 lightDir = normalize(uLightPos - vFragPos);
    float diff = max(dot(normal, lightDir), 0.0);
    fragColor = vec4(diff, diff, diff, 1.0);
}
```

### Review shader for batch rendering compatibility
```
@shader-optimization review MyCustomShader
```

## What Claude Will Do

1. **Analyze** the shader systematically:
    - Identify bottlenecks (fragment vs vertex shader)
    - Count expensive operations scaled by execution frequency
    - Check batch rendering compatibility
    - Detect dynamic branching issues
    - Review texture sampling patterns

2. **Apply optimization patterns**:
    - Move calculations from fragment to vertex shader
    - Eliminate dynamic branching (replace with step/mix/smoothstep)
    - Minimize texture samples
    - Remove expensive operations (pow, sin, cos, sqrt)
    - Ensure batch rendering compatibility

3. **Provide structured report**:
    - Issue description and performance impact
    - Code location in engine
    - Before/after optimization code
    - Validation steps
    - Platform compatibility notes
    - Priority level

## Key Focus Areas

### 1. Fragment Shader Optimization (Highest Impact)
- Executes ~2M times/frame at 1080p
- Move calculations to vertex shader where possible
- Eliminate expensive operations
- Small improvements = huge impact

### 2. Batch Rendering Compatibility (Critical)
- Must support 10,000 quads in single draw call
- Use texture array indexing (`sampler2D uTextures[16]`)
- Pass per-object data via vertex attributes, not uniforms
- Support 16 texture slots (`RenderingConstants.MaxTextureSlots`)

### 3. Dynamic Branching Elimination (High Impact)
- GPU warps execute in lockstep
- Divergent branches destroy parallelism
- Replace if/else with step(), mix(), smoothstep()
- Uniform-based branches are OK

### 4. Texture Sampling (Medium Impact)
- Minimize samples per fragment
- Pack multiple properties into single texture
- Avoid dependent texture reads
- Use engine's texture array pattern

### 5. Vertex Shader Optimization (Medium Impact)
- Leverage GPU interpolation
- Calculate once per vertex, interpolate for free
- Pack vertex attributes efficiently

## Engine-Specific Constraints

- **Shader Location**: `Engine/Renderer/Shaders/ShaderFactory.cs` (inline GLSL, not separate files)
- **Constants**: From `Engine/Renderer/RenderingConstants.cs`:
    - `MaxTextureSlots = 16`
    - `DefaultMaxQuads = 10000`
    - `QuadVertexCount = 4`, `QuadIndexCount = 6`
- **Batching**: See `Engine/Renderer/Graphics2D.cs` for vertex layout
- **Cross-Platform**: Must work on Windows, macOS, Linux (OpenGL 3.3+)

## Validation Requirements

Before optimization is complete:
- [ ] Visual validation: Reference screenshots match
- [ ] Compilation: No errors on Windows, macOS, Linux
- [ ] Performance: ≥5% frame time improvement
- [ ] Batching: Draw calls unchanged or reduced
- [ ] Stress test: 10,000 quads at 1080p+
- [ ] Texture indexing: All 16 slots work
- [ ] Edge cases: No artifacts with extreme values

## Output Format

Claude provides structured reports with:

**Issue**: Brief description of inefficiency  
**Impact**: Performance cost (ms/frame or percentage)  
**Location**: File path and identifier  
**Optimization**: Before/after code with explanation  
**Validation**: Steps to verify the fix  
**Compatibility**: Platform considerations  
**Priority**: Critical/High/Medium/Low

## Common Optimization Patterns

### Pattern: Move Normalize to Vertex Shader
```glsl
// BEFORE (fragment shader - expensive per-pixel)
vec3 normal = normalize(vNormal);

// AFTER (vertex shader - once per vertex)
vNormalNorm = normalize(aNormal); // Interpolates automatically
```

### Pattern: Eliminate Dynamic Branching
```glsl
// BEFORE (divergent branch)
if (texColor.a < 0.1) discard;

// AFTER (branchless)
float alphaMask = step(0.1, texColor.a);
fragColor.a *= alphaMask;
```

### Pattern: Batch-Compatible Data Passing
```glsl
// BEFORE (breaks batching)
uniform vec4 uColor;

// AFTER (batching-compatible)
in vec4 vColor; // Vertex attribute
```

### Pattern: Texture Channel Packing
```glsl
// BEFORE (4 samples)
float metallic = texture(uMetallicMap, uv).r;
float roughness = texture(uRoughnessMap, uv).r;
float ao = texture(uAOMap, uv).r;

// AFTER (1 sample - pack R=metallic, G=roughness, B=ao)
vec3 props = texture(uPropertiesMap, uv).rgb;
```

## Related References

- **Optimization Patterns**: Detailed code examples for all patterns
- **Profiling Guide**: Measurement and validation procedures
- **Output Template**: Structured reporting format
- **Graphics2D**: Engine batching implementation
- **ShaderFactory**: Shader definitions
- **RenderingConstants**: Engine constants

## Priority Guidelines

**Critical**: Breaks functionality, affects all shaders, crashes  
**High**: >10% frame time impact, common rendering paths, easy fix  
**Medium**: 5-10% impact, specific use cases, moderate effort  
**Low**: <5% impact, rare edge cases, high effort

## Notes

- Analysis is systematic: baseline → hot path → expensive ops → patterns → validation
- Fragment shader optimizations have highest ROI (2M executions/frame)
- Batch rendering compatibility is non-negotiable for engine performance
- Always test on macOS first (strictest GLSL compiler)
- Use `#version 330 core` for cross-platform compatibility