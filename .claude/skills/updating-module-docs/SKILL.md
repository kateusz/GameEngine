---
name: updating-module-docs
description: Updates README.md and docs/ for a single named engine module by reading only that module's source paths. Use when the user invokes /update-module-docs or /updating-module-docs, passes a module name to update documentation, or asks to refresh docs for one subsystem (rendering, scene, scripting, physics, audio, ecs, editor, etc.). Never scans the whole codebase.
disable-model-invocation: true
---

# Updating Module Documentation

Scoped documentation sync: one module in, targeted README + `docs/` updates out.

**Required argument**: module name (e.g. `rendering`, `scene`, `scripting`). If missing, ask before doing anything else.

| Section | Contents |
|---------|----------|
| [Hard limits](#hard-limits) | Scoping rules |
| [Workflow](#workflow) | Steps 1–8 |
| [Overlap policy](#overlap-policy) | Shared modules |
| [Output](#output-to-user) | Report template |
| [Examples](#examples) | Invocation patterns |
| [Resources](#additional-resources) | Map + co-authoring |

## Hard limits

- **Do not** read or grep outside the module's source scope from [module-map.md](module-map.md).
- **Do not** update docs or README sections for other modules.
- **Do not** invent APIs — every claim must trace to a file in the scoped source paths.
- **Do not** create new markdown files unless the map lists no primary doc and the user explicitly wants one.

## Workflow

```
Task Progress:
- [ ] Step 1: Resolve module name
- [ ] Step 2: Read scoped source
- [ ] Step 3: Read existing docs for module
- [ ] Step 4: Diff code vs docs
- [ ] Step 5: Update docs/
- [ ] Step 6: Update README.md (module sections only)
- [ ] Step 7: Update doc indexes if links changed
- [ ] Step 8: Verify
```

### Step 1: Resolve module name

1. Normalize: lowercase, trim, replace spaces with hyphens.
2. Look up in [module-map.md](module-map.md) (module column or aliases).
3. On no match: substring-match once; if still unclear, `AskQuestion` with map rows — stop.

### Step 2: Read scoped source

Read files only under the module's **Source scope** paths. Respect **Exclude** paths in the map.

| Priority | What to read |
|----------|--------------|
| 1 | Public interfaces (`I*.cs`) and factories |
| 2 | Core implementations wired in DI (`*Factory.cs`, `*System.cs`) |
| 3 | Components (if ECS-related module) |
| 4 | Constants and priority enums |

Use targeted search **within scoped paths only** (e.g. `Grep` with `path` set to `Engine/Renderer/`). Never repo-wide `Grep` for discovery.

### Step 3: Read existing docs

Read the module's **Primary doc** and **Secondary docs** from the map. Also read `docs/architecture/README.md` and `docs/guide/index.md` only to check whether this module's links need updating.

For modules with **no primary doc**, read secondary docs only and follow the map's **Doc strategy** column.

### Step 4: Diff code vs docs

Build a short internal checklist (do not dump to user unless asked):

- File paths cited in docs still exist?
- Class/interface names match?
- System priorities match `SystemPriorities.cs`?
- Component count and names accurate?
- README feature bullets still true?
- Broken links (`docs/modules/`, `docs/opengl-rendering/` → current paths)?

### Step 5: Update docs/

Edit existing docs in place. Match the style of sibling architecture docs:

- Title + one-line summary
- `---` section breaks
- **File**: `path/to/File.cs` for key types
- Tables for APIs, constants, priorities
- Mermaid diagrams only when relationships changed (keep existing diagrams if still accurate)

**Per doc type:**

| Doc location | Focus |
|--------------|-------|
| `docs/architecture/*.md` | Design, data flow, key types, file paths, system priorities |
| `docs/guide/**/*.md` | How-to, prerequisites, examples — minimal internal file paths |
| `docs/opengl/*.md` | GPU resources, batch limits, shader/buffer details |

Remove or rewrite sections that describe deleted code. Mark genuinely uncertain gaps with `<!-- TODO: verify in source -->` only when source is ambiguous after reading scoped paths.

### Step 6: Update README.md (module sections only)

Touch **only** sections listed in the map's **README sections** column. Use the **README grep** patterns from the map to locate them.

Leave all other README sections unchanged. Sync .NET version and component counts only if you read them in scoped source and they are wrong **in a section you are already editing**.

### Step 7: Update doc indexes

If primary/secondary doc paths changed:

- `docs/architecture/README.md` — architecture table row for this module
- `docs/guide/index.md` — "Where to Go Next" link if user-facing

### Step 8: Verify

Before reporting done:

1. **Re-read** every file you edited — confirm edits saved and sections outside scope were not touched.
2. **Path check**: every `**File**:` path and inline code path must exist under the module's source scope (or documented platform impl path for rendering).
3. **Link check**: every relative doc link in edited files resolves to an existing file.
4. **README scope**: `git diff README.md` — only lines matching this module's README grep patterns should change.
5. **Source trace**: each new factual claim in docs maps to a file read in Step 2.

If verification fails, fix before reporting. Do not skip this step.

## Overlap policy

When modules share code, update **only the invoking module's docs** unless the user names multiple modules:

| Overlap | Owner module | Other module — do not edit |
|---------|--------------|----------------------------|
| Cameras in rendering pipeline | `cameras` | `rendering` — leave camera sections unless user asked for `rendering` and only fix cross-refs |
| Scene JSON in scenes guide | `serialization` | `scene` — touch serialization.md only when `serialization` is the argument |
| `SceneRenderPipeline` | `rendering` | `scene` |
| Editor viewport / framebuffers | `framebuffers` or `editor` | Whichever module was invoked |

When user invokes the broader module (e.g. `rendering`), update integrated docs end-to-end including camera/batch sections. When user invokes the narrow module (e.g. `cameras`), touch only that module's primary/secondary docs and README rows.

## Output to user

After Step 8 passes, report briefly:

1. **Module**: resolved name
2. **Files updated**: list paths
3. **Key changes**: 3–5 bullets (what was stale and what changed)
4. **Verification**: one line (paths checked, links OK, README scope OK)
5. **Not documented**: anything in source with no natural doc home (optional, short)

## Examples

**User**: `/update-module-docs rendering`

1. Map → `Engine/Renderer/`, `SceneRenderPipeline`, render systems
2. Read `rendering-pipeline.md`, `opengl-2d-workflow.md`, `opengl-3d-workflow.md`
3. Update batch limits, system list, `IRendererAPI` methods from source
4. Fix README Key Systems + Rendering feature bullets + doc links only
5. Verify all cited paths exist; confirm README diff is rendering-only

**User**: `update docs for di`

1. Resolve alias → `dependency-injection`
2. Read `Engine/Core/DI/`, `Editor/DI/` only
3. Update `docs/architecture/dependency-injection.md` + README DI bullet

Full before/after scenarios: [examples.md](examples.md)

## Additional resources

- Module → path mapping: [module-map.md](module-map.md)
- Co-authoring new docs from scratch: `doc-coauthoring` skill (use only when map has no primary doc and user wants a new file)
