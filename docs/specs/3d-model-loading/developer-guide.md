# 3D Model Loading (bake-on-import / `.mesh`) — Developer Guide

Implementation guide for cook-on-import and `.mesh`-only runtime load. See `introduction.md` for conceptual background.

## Glossary (implementation subset)

| Term | Meaning in code |
|------|-----------------|
| Model path | String on `ModelRendererComponent`; empty = cube; must be `.mesh` when set |
| Model | Cached path key + ordered submeshes |
| Submesh | Mesh geometry + `MeshMaterial` (PBR) |
| MeshCreator | Assimp → texture relocate → `MeshWriter` → `assets/models/<stem>.mesh` |
| Model factory | Path → cached model; `MeshReader` on miss; rejects non-`.mesh` |
| Cube fallback | Empty, failed, or rejected path → unit-cube draw |
| Tint | Component color multiplied with albedo |

## Pipeline (current)

1. **Author** — **File → Import 3D Model…** (macOS file|folder path; Windows folder pick + non-recursive enumerate of `.fbx` / `.glb` / `.gltf`)
2. **Create** — `MeshCreator.Create(source, projectAssetsRoot, stem)` → nested `.mesh` + relocated textures
3. **Author assign** — set `ModelPath` to `models/<stem>.mesh` (inspector MeshDropTarget / Content Browser)
4. **Runtime** — `ModelFactory.Create` reads `.mesh` only; draw submeshes or cube fallback

Animation/skinning is **out of scope for v1** (roadmap follow-on).

---

## Create path (`MeshCreator`)

**Files**: `Engine/Renderer/MeshCreator.cs`, `AssimpModelImporter.cs`, `TextureRelocator.cs`, `MeshWriter.cs`

```
open source with Assimp (cook-time only)
post-process: triangulate, normals/tangents as needed, PreTransformVertices (no FlipUVs)
map materials → MeshMaterial (PBR + legacy heuristics)
TextureRelocator → copy maps under assets/models/textures/, rewrite paths relative
MeshWriter → assets/models/<stem>.mesh
```

Ignore bones, animations, cameras, lights, and empties.

**Why:** Assimp stays behind one cook boundary; Runtime never opens raw interchange.

---

## Runtime path (`ModelFactory`)

**Files**: `Engine/Renderer/ModelFactory.cs`, `MeshReader.cs`

- `Create(path)` → cache hit, or open `.mesh` via `MeshReader`, resolve textures with `PathBuilder.Resolve`, upload GPU, cache
- Non-`.mesh` extension → warn + return null (cube fallback upstream) — **no Assimp fallback**
- Missing/corrupt file or unknown magic/VERSION → fail soft (null); do not cache hard failures as success
- Register in engine DI; dispose/clear aligned with other renderer factories

**Why:** Call sites stay path-based; hot path is binary I/O only.

---

## Graphics3D and scene render path

Per entity with model renderer + transform:

```
if ModelPath empty:
  DrawCube(tint)
else:
  model = ModelFactory.Create(resolved ModelPath)
  if model missing:
    log (throttled) + DrawCube(tint)
  else:
    for each submesh:
      bind PBR maps + tint / overrides
      draw mesh
```

---

## Component and editor

On `ModelRendererComponent`:

- `ModelPath` (string) — project-relative `.mesh`
- `Color` tint; optional metallic/roughness overrides

Editor:

- **File → Import 3D Model…** — bake sources into `assets/models/`
- Inspector MeshDropTarget — **`.mesh` only**
- Content Browser Type: Model — same allowlist as MeshDropTarget
- Required project dir: `assets/models`

### Legacy raw ModelPath (hard cutover)

Existing scenes with raw `.fbx` / `.gltf` / `.glb` (etc.) on `ModelPath` show the **unit cube** until authors **re-import** and update the path to the cooked nested `.mesh`. Document this break; do not dual-load.

---

## Testing focus

**Automated**

- `MeshWriter` ↔ `MeshReader` round-trip (geometry + materials)
- `ModelFactory` accepts `.mesh`, rejects raw extensions
- Cook/re-home on tiny glTF fixtures; flat `models/<stem>.mesh` layout
- Serialization: `ModelPath` + `Color` round-trip

**Manual smoke**

- Import → Content Browser shows `.mesh` → drag to Model field → lit mesh
- Empty / raw / bad path → cube + log
- Sample `Editor/assets/scenes/3d.scene` → `models/stachu-light.mesh` (cooked sample may be large ~94 MB under `Editor/assets/models/`)

---

## Architecture

```mermaid
flowchart TB
  subgraph editor [Editor]
    IMP[File → Import 3D Model]
    MRC[ModelRendererComponent<br/>ModelPath + Color]
  end

  subgraph cook [Cook-time]
    MB[MeshCreator]
    AI[AssimpModelImporter]
    TR[TextureRelocator]
    MW[MeshWriter]
  end

  subgraph runtime [Runtime]
    MF[ModelFactory cache]
    MR[MeshReader]
    TF[TextureFactory]
    G3D[Graphics3D]
  end

  IMP --> MB
  MB --> AI
  AI --> TR
  TR --> MW
  MW -->|models/stem.mesh| Disk[(assets)]
  MRC --> MF
  MF -->|miss .mesh| MR
  MR --> TF
  MF -->|Model| G3D
  MF -->|reject raw / fail| G3D
```

```mermaid
sequenceDiagram
  participant Pipeline as Scene render pipeline
  participant Factory as Model factory
  participant Reader as MeshReader
  participant Tex as Texture factory
  participant G3D as Graphics3D

  Pipeline->>Pipeline: read ModelPath + Color + transform
  alt path empty or load failed or non-.mesh
    Pipeline->>G3D: DrawCube(tint)
  else .mesh path set
    Pipeline->>Factory: Create(path)
    alt cache miss
      Factory->>Reader: Read stream
      Reader->>Tex: resolve re-homed maps
      Factory->>Factory: cache
    end
    Factory-->>Pipeline: Model
    loop each submesh
      Pipeline->>G3D: bind material + tint, draw mesh
    end
  end
```

---

## Error-handling checklist

| Case | Behavior |
|------|----------|
| Missing / corrupt `.mesh` | No success cache; throttled log; cube fallback |
| Non-`.mesh` ModelPath (legacy raw) | Reject + log; cube until re-import |
| Zero meshes in file | Treat as load failure |
| Missing texture map | Skip that map; draw with remaining material + tint |
| Unsupported chunks at cook (anim, lights, …) | Ignore |
| Single submesh GPU init failure | Drop that submesh; keep others; log |

---

## Out of scope reminders

Do not add in this feature: animation/skins (roadmap), child-entity hierarchy from nodes, dual-load Assimp in `ModelFactory`, mesh-index selection API, or treating Assimp as a public runtime service.
