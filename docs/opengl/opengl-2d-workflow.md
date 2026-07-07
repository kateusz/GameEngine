# OpenGL 2D Rendering Workflow

**File**: `Engine/Renderer/Graphics2D.cs` — implements `IGraphics2D`

## Overview

The OpenGL 2D rendering system transforms game entities and sprites into pixels on screen through an efficient batched rendering pipeline.

### Purpose
- Efficiently render 2D sprites, quads, and primitive shapes using OpenGL
- Minimize draw calls through intelligent batching of similar geometry
- Support multiple textures within a single batch using texture slots
- Provide a high-level abstraction over low-level OpenGL operations

### Key Concepts

**Batch Rendering**: Instead of issuing one draw call per sprite, the system accumulates multiple quads into large vertex buffers and submits them in batches. This dramatically reduces CPU-GPU communication overhead.

**Texture Atlasing**: Up to 16 different textures can be bound simultaneously (configurable via `RenderingConstants.MaxTextureSlots`). The shader uses a texture index to sample from the correct texture slot, allowing diverse sprites to be rendered in a single draw call.

**Dynamic Vertex Buffers**: Vertex data is built on the CPU each frame and uploaded to GPU memory before drawing. This supports fully dynamic scenes where every sprite can move, change color, or swap textures.

**Immediate Mode API**: The rendering interface uses a "begin scene, draw primitives, end scene" pattern that feels immediate while actually performing deferred batching internally.

## Architecture Flow

### Initialization Phase

When the graphics system starts up, it prepares all the resources needed for rendering:

1. **Vertex Array Objects (VAO)** define how vertex data is laid out
2. **Vertex Buffers** are allocated on the GPU with capacity for maximum batch sizes
3. **Index Buffers** are pre-populated with triangle indices for quad tessellation
4. **Shaders** are compiled from source files and linked into programs
5. **Default Textures** (white 1x1 pixel) are created for solid color rendering
6. **Texture Slots** array is initialized to track which textures are currently bound

### Frame Rendering Cycle

Each frame follows a strict sequence: **BeginScene → Draw Calls → EndScene**

**BeginScene**:
- Accepts camera parameters (view and projection matrices)
- Uploads camera matrices to shader uniforms
- Clears all batch state (vertex counters, texture slots)

**Draw Calls**:
- Application code calls `DrawQuad`, `DrawLine`, or `DrawRect` on `IGraphics2D`
- Each draw call adds vertices to the current batch
- If batch limits are reached, a flush occurs automatically
- New textures are assigned to available slots; duplicates reuse existing slots

**EndScene**:
- Triggers final flush to draw any remaining geometry
- All accumulated data is submitted to the GPU

### Batching Logic

The system maintains two separate batches:

**Quad Batch** (for filled rectangles and sprites):
- Accumulates quad vertices with position, color, texture coordinates, texture index, tiling factor, and entity ID
- Each quad requires 4 vertices and 6 indices (two triangles)
- Maximum capacity: 10,000 quads (40,000 vertices) by default

**Line Batch** (for debug wireframes and primitive shapes):
- Accumulates line vertices with position, color, and entity ID
- Each line requires 2 vertices
- Uses GL_LINES primitive topology

When a batch is full, the system automatically flushes to GPU, resets counters, and continues accumulating.

### Data Flow

```mermaid
sequenceDiagram
    participant App as Application/Scene
    participant Graphics as Graphics2D
    participant Batch as Batch State
    participant GPU as GPU/OpenGL

    App->>Graphics: BeginScene(camera)
    Graphics->>GPU: Upload camera matrices to shaders
    Graphics->>Batch: Clear vertex buffers and counters

    loop For each entity
        App->>Graphics: DrawQuad(transform, texture, color)
        Graphics->>Batch: Add 4 vertices to buffer
        Graphics->>Batch: Check texture slot availability

        alt Batch Full
            Graphics->>GPU: Upload vertex data
            Graphics->>GPU: Bind all textures
            Graphics->>GPU: DrawIndexed()
            Graphics->>Batch: Clear buffers and reset
        end
    end

    App->>Graphics: EndScene()
    Graphics->>GPU: Upload remaining vertex data
    Graphics->>GPU: Bind all textures
    Graphics->>GPU: DrawIndexed()
```

### Shader Execution Pipeline

```mermaid
flowchart TD
    A[Vertex Data in CPU Memory] --> B[Upload to GPU Buffer]
    B --> C[Vertex Shader]
    C --> D{Transform Vertices}
    D --> E[Apply View-Projection Matrix]
    E --> F[Pass to Fragment Shader]
    F --> G{Sample Texture}
    G --> H[Use Texture Index to Select Slot]
    H --> I[Multiply Texture Color by Tint]
    I --> J[Apply Tiling Factor]
    J --> K[Output Final Color]
    K --> L[Write to Framebuffer]
```

**Vertex Shader**: Transforms vertex positions from world space to clip space using camera matrices, passes through vertex attributes to fragment shader.

**Fragment Shader**: Samples the appropriate texture using the texture index, applies tiling and tint color, outputs final pixel color and entity ID.

### Texture Management

The system maintains an array of texture slots (typically 16):

- **Slot 0**: Reserved for the default white texture (solid color quads)
- **Slots 1-15**: Dynamically assigned to textures as encountered

**Texture Binding Logic**:
1. Check if texture is already bound using O(1) dictionary lookup
2. If found, reuse the existing slot index
3. If not found and slots available, bind to next slot and cache the mapping
4. If all slots full, flush current batch, clear cache, and start fresh

## Lifecycle & Timing

### Initialization
- Called once at application startup after OpenGL context creation
- Creates all buffers, shaders, and resources
- Sets OpenGL rendering state (blending, depth testing)

### Per-Frame Execution

```mermaid
sequenceDiagram
    participant Engine as Engine Loop
    participant Scene as Scene System
    participant Graphics as Graphics2D
    participant GPU as GPU

    Engine->>Scene: Update(deltaTime)
    Note over Scene: Physics, scripts, transforms updated

    Scene->>Graphics: BeginScene(camera)

    loop For each visible entity
        Scene->>Graphics: DrawQuad(...)
    end

    Scene->>Graphics: EndScene()
    Graphics->>GPU: All batches flushed

    Engine->>GPU: SwapBuffers()
    Note over GPU: Frame displayed
```

### Shutdown
- `Dispose()` releases all OpenGL resources
- Disposes: shaders, vertex arrays, vertex buffers
- Texture slot references are cleared (textures owned by TextureFactory)

## Advanced Features

### Multi-Texture Batching

Supports rendering up to 16 different textures in a single draw call:
- Fragment shader declares an array of sampler2D uniforms
- Each vertex carries a texture index indicating which sampler to use
- Eliminates texture rebinding between different sprites
- When more than 16 unique textures needed, automatic flushing occurs

### Entity Identification

Each vertex includes an entity ID attribute for editor picking:
- Fragment shader outputs entity ID to a secondary color attachment
- Editor reads this attachment on mouse click to determine selected entity
- Used for object picking, debug visualization, and gizmo rendering

### Dynamic Batching Strategy

**Batch Size Limits** (from `RenderingConstants` / `Renderer2DData`):

| Constant | Value | Purpose |
|----------|-------|---------|
| DefaultMaxQuads | 10,000 | Quads per batch |
| MaxVertices | 40,000 | DefaultMaxQuads × 4 |
| MaxIndices | 60,000 | DefaultMaxQuads × 6 |
| MaxTextureSlots | 16 | Sampler units in quad shader |
| DefaultLineWidth | 1.0f | Line width passed to `IRendererAPI.SetLineWidth` |
| MaxFramebufferSize | 8,192 | Max framebuffer dimension (px) |

Automatic flushing is transparent to application code.

### Solid Color Optimization

Quads without textures use the white texture in slot 0:
- Fragment shader samples white texture and multiplies by tint color
- Unified rendering path for textured and untextured geometry
- Allows mixing textured and colored quads in the same batch

## OpenGL State Configuration

**Blending**: Enabled at `IRendererAPI.Init()` — `SRC_ALPHA`, `ONE_MINUS_SRC_ALPHA`

**Depth testing**: Enabled globally; **disabled only while drawing the quad batch** in `Flush()`, then re-enabled before line drawing

**Shader binding**: `textureShader` for quads/sprites, `lineShader` for wireframes (`assets/shaders/OpenGL/`)

**Vertex array objects**: Separate VAOs for quad and line vertex layouts

## Performance Characteristics

### Draw Call Reduction
- Without batching: 1 draw call per sprite
- With batching: 1 draw call per batch (10,000 sprites might need only 1-10 calls)
- Typical improvement: 100-1000x reduction in draw calls

### Memory Bandwidth
- Each frame uploads vertex data and minimal uniform updates
- Only active vertices are uploaded (not full buffer capacity)

### Bottleneck Analysis

**Typical Bottlenecks**:
1. Vertex upload bandwidth
2. Texture binding (mitigated by multi-texture batching)
3. Draw call overhead (mitigated by batching)

**Optimization Strategies**:
- Increase batch sizes to reduce flush frequency
- Use texture atlases to reduce unique texture count
- Sort draw calls by texture to maximize batching efficiency

## Coordinate Systems

Geometry flows through multiple coordinate spaces:

1. **Local Space**: Model-relative coordinates
2. **World Space**: Scene position after transformation
3. **View Space**: Camera-relative coordinates
4. **Clip Space**: After view-projection matrix (`u_ViewProjection`)
5. **Screen Space**: Final pixel coordinates after GPU rasterization

## Complete Frame Rendering Flow

```mermaid
flowchart TD
    Start([Frame Start]) --> Update[Update Game Logic]
    Update --> BeginScene[BeginScene: Upload Camera & Reset Batches]

    BeginScene --> Loop{More Entities?}

    Loop -->|Yes| DrawCall[DrawQuad / DrawLine / DrawRect]
    DrawCall --> CheckBatch{Batch Full?}

    CheckBatch -->|No| AddVerts[Add Vertices to Batch]
    AddVerts --> CheckTex{Texture Slots Full?}

    CheckTex -->|No| BindTex[Bind/Reuse Texture Slot]
    BindTex --> Loop

    CheckTex -->|Yes| Flush1[Flush Current Batch]
    Flush1 --> BindTex

    CheckBatch -->|Yes| Flush2[Flush Current Batch]
    Flush2 --> AddVerts

    Loop -->|No| EndScene[EndScene: Final Flush]

    EndScene --> FlushQuads[Upload Quad Vertices]
    FlushQuads --> BindTextures[Bind All Textures]
    BindTextures --> DrawIndexed[DrawIndexed Call]

    DrawIndexed --> FlushLines[Upload Line Vertices]
    FlushLines --> DrawLines[DrawLines Call]

    DrawLines --> Swap[Swap Buffers]
    Swap --> End([Frame End])
```

## Texture Slot Management

```mermaid
sequenceDiagram
    participant App as Application
    participant Graphics as Graphics2D
    participant Cache as TextureSlotCache
    participant Slots as Texture Slots

    Note over Slots: Slot 0: White Texture (default)
    Note over Slots: Slots 1-15: Available

    App->>Graphics: DrawQuad(texture=Grass)
    Graphics->>Cache: TryGetValue(Grass)
    Cache-->>Graphics: Not found
    Graphics->>Slots: Assign Grass to Slot 1
    Graphics->>Cache: Add(Grass, 1)

    App->>Graphics: DrawQuad(texture=Tree)
    Graphics->>Cache: TryGetValue(Tree)
    Cache-->>Graphics: Not found
    Graphics->>Slots: Assign Tree to Slot 2
    Graphics->>Cache: Add(Tree, 2)

    App->>Graphics: DrawQuad(texture=Grass)
    Graphics->>Cache: TryGetValue(Grass)
    Cache-->>Graphics: Found: index = 1

    Note over App,Slots: ... more draws until slots full ...

    App->>Graphics: DrawQuad(texture=Texture17)
    Graphics->>Cache: TryGetValue(Texture17)
    Cache-->>Graphics: Not found
    Graphics->>Slots: All slots full (16/16)
    Graphics->>Graphics: Flush current batch
    Graphics->>Cache: Clear()
    Graphics->>Slots: Reset (keep Slot 0)
    Graphics->>Slots: Assign Texture17 to Slot 1
```

## Best Practices

### Sorting for Optimal Batching

Sort draw calls by:
1. Render pass (opaque before transparent)
2. Shader (group by shader program)
3. Texture (maximize slot reuse)
4. Depth (front-to-back for opaque, back-to-front for transparent)

### Texture Atlas Usage

Combine small textures into larger atlases:
- Reduces unique texture count
- Improves batching efficiency
- Use SubTexture2D to define regions within the atlas

### Debug Rendering

The line rendering system supports debug visualization:
- Physics collision shapes
- Bounding boxes (`DrawRect` methods)
- Path visualization
- Gizmos and selection highlights

## Integration with Scene System

```mermaid
sequenceDiagram
    participant Scene as SceneRenderPipeline
    participant ECS as Context
    participant Graphics as Graphics2D

    Scene->>Graphics: BeginScene(camera)
    Scene->>ECS: View<SpriteRendererComponent, TransformComponent>()

    loop For each entity in view
        ECS->>Scene: entity + components
        Scene->>Graphics: DrawQuad(transform, texture, coords, tiling, color, entityId)
    end

    Scene->>Graphics: EndScene()
```

`SceneRenderSystem` (priority 150) drives this path. `PhysicsDebugRenderSystem` (priority 151) issues a **separate** `BeginScene` / `DrawRect` / `EndScene` pass for collider outlines when debug drawing is enabled.

### Component-Based Rendering

Entities with rendering components are automatically rendered by `SceneRenderPipeline`:

| Component | Role |
|-----------|------|
| `TransformComponent` | World position, rotation, scale via `GetTransform()` |
| `SpriteRendererComponent` | `TexturePath`, `Color`, `TilingFactor` |
| `SubTextureRendererComponent` | Atlas `TexturePath`, cell `Coords`, or explicit `TexCoords` |
| `CameraComponent` | Marks primary camera; resolved by `PrimaryCameraSystem` (145) |

### Editor vs Runtime Rendering

**Editor Mode**: Uses editor camera (orthographic, user-controlled), may include debug overlays

**Runtime Mode**: Uses scene's primary camera, queries scene for camera entity

Both modes use the same Graphics2D methods for consistent rendering.

## Summary

The OpenGL 2D rendering workflow is a batching system that balances ease of use with high performance:

- **Batching is transparent**: Application code draws one quad at a time; batching happens automatically
- **Texture management is automatic**: The system tracks texture slots via dictionary for O(1) lookup
- **Two separate batches**: Quads and lines are rendered independently with different shaders
- **Camera-driven**: All rendering is relative to the active camera's view-projection matrix
- **Deferred execution**: Draw calls accumulate data; actual GPU work happens during flush
- **Resource ownership**: `Graphics2D` owns VAOs/VBOs; shaders come from `ShaderFactory`; textures from `TextureFactory`

This architecture supports dynamic scenes with thousands of sprites while maintaining high frame rates.
