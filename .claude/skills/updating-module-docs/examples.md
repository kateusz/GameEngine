# Doc Sync Examples

Full before/after scenarios from real dry-runs. Use as quality bar for Steps 4–8.

---

## Example 1: Stale ECS rendering systems (`rendering`)

**Trigger**: `/update-module-docs rendering`

**Scoped source read** (excerpt):

- `Engine/Scene/Systems/SystemPriorities.cs` — `SceneRenderSystem = 150`, `PhysicsDebugRenderSystem = 151`
- `Engine/Scene/Systems/SceneRenderSystem.cs` — delegates to `SceneRenderPipeline.RenderScene`
- `Engine/Scene/SceneRenderPipeline.cs` — iterates `SpriteRendererComponent`, `SubTextureRendererComponent`, `ModelRendererComponent`

**Before** (`docs/architecture/rendering-pipeline.md` mermaid):

```mermaid
subgraph "ECS Rendering Systems"
    PCS["PrimaryCameraSystem (145)"]
    SRS["SpriteRenderingSystem (150)"]
    STRS["SubTextureRenderingSystem (160)"]
    MRS["ModelRenderingSystem (170)"]
    PDRS["PhysicsDebugRenderSystem (180)"]
end
```

**After**:

```mermaid
subgraph "ECS Rendering Systems"
    PCS["PrimaryCameraSystem (145)<br/><i>Resolves active camera</i>"]
    SRS["SceneRenderSystem (150)<br/><i>SceneRenderPipeline: sprites, subtextures, 3D cubes</i>"]
    PDRS["PhysicsDebugRenderSystem (151)<br/><i>Collider debug lines when enabled</i>"]
end
```

**Why**: Per-sprite systems were merged into `SceneRenderPipeline`; priorities in source are 150 and 151, not 160–180.

---

## Example 2: Wrong file paths (`rendering`)

**Scoped source read**:

- `Engine/Renderer/Primitives/QuadVertex.cs` exists
- `Engine/Renderer/QuadVertex.cs` does **not** exist
- `Engine/Scene/Cameras/EditorCamera.cs` exists
- `Engine/Renderer/Cameras/EditorCamera.cs` does **not** exist

**Before**:

```markdown
**Files**: `Engine/Renderer/QuadVertex.cs`, `Engine/Renderer/LineVertex.cs`

**File**: `Engine/Renderer/Cameras/EditorCamera.cs`
```

**After**:

```markdown
**Files**: `Engine/Renderer/Primitives/QuadVertex.cs`, `Engine/Renderer/Primitives/LineVertex.cs`

**File**: `Engine/Scene/Cameras/EditorCamera.cs`
```

**Verification (Step 8)**: Glob each cited path; fail if any missing.

---

## Example 3: Stale README doc links (`rendering`)

**Scoped source**: unchanged — link fix only, no API drift.

**Before** (`README.md` Key Systems):

```markdown
- **Renderer2D/3D** - Batched rendering ... ([docs](docs/opengl-rendering/opengl-2d-workflow.md))
```

**Before** (Documentation → Rendering):

```markdown
- [Rendering Pipeline](docs/modules/rendering-pipeline.md) - OpenGL rendering pipeline overview
```

**After**:

```markdown
- **Renderer2D/3D** - Batched rendering ... ([docs](docs/architecture/rendering-pipeline.md))
```

```markdown
- [Rendering Pipeline](docs/architecture/rendering-pipeline.md) - OpenGL rendering pipeline overview
- [OpenGL 2D Workflow](docs/opengl/opengl-2d-workflow.md) - Batched 2D rendering
```

**Verification (Step 8)**: `git diff README.md` — only lines matching README grep `Renderer2D/3D`, `**Rendering:**`, `### OpenGL Rendering` changed; no edits to Audio or Scripting sections.

---

## Example 4: Missing `IRendererAPI` members (`rendering`)

**Scoped source** (`Engine/Renderer/IRendererAPI.cs`):

```csharp
void BindTexture2D(uint textureId, int slot = 0);
void SetDepthTest(bool enabled);
int GetError();
```

**Before** (API table): only `Init`, `SetClearColor`, `Clear`, `DrawIndexed`, `DrawLines`, `SetLineWidth`.

**After**: add rows for `BindTexture2D`, `SetDepthTest`, `GetError`.

**Why**: Step 4 diff must include interface completeness for architecture docs.
