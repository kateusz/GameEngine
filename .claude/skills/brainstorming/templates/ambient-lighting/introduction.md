# Ambient Lighting — Conceptual Introduction

## What Problem Does This Solve?

3D objects lit only by directional light have faces that fall into complete darkness when they do not face the light source. In a game engine prototype, this makes blockout geometry hard to read and gives scenes an unnatural, high-contrast look.

Ambient lighting provides a **scene-wide base illumination** — a minimum light level applied uniformly to all surfaces regardless of orientation. It does not replace directional or point lights; it fills shadowed areas so geometry remains visible while directional light still conveys form and depth.

## What the Feature Will Achieve

After ambient lighting is integrated into the rendering pipeline:

- Designers can add an **ambient light source** to any scene as a standard ECS component on an entity.
- The 3D renderer reads ambient settings once per frame and applies them to all shaded draws in that pass.
- Shadowed cube faces remain visible at a tunable baseline brightness instead of going fully black.
- Ambient color and strength are editable in the editor and persist through scene serialization.
- When no ambient light entity exists, the renderer falls back to safe defaults so scenes never render with undefined lighting.

## Benefits and Outcomes

| Outcome | Why it matters |
|---------|----------------|
| Readable blockout scenes | Artists and designers can evaluate layout without fighting pure-black shadows |
| Data-driven lighting | Light mood changes without shader or code edits |
| ECS consistency | Lighting follows the same component + system pattern as cameras, sprites, and physics |
| Platform abstraction preserved | Light values flow through the graphics interface; no OpenGL calls in scene logic |
| Graceful degradation | Missing ambient entity does not break rendering |

## Terminology

**Ambient light** — Illumination that reaches a surface equally from all directions. In this engine's prototype shader, it is a flat addend: base brightness multiplied by a color tint, independent of surface normal.

**Ambient color** — An RGB tint applied to the ambient contribution. White means neutral fill; colored ambient shifts the overall mood of shadowed regions.

**Ambient strength** — A scalar multiplier controlling how bright the ambient fill is. Low values keep shadows dark but visible; high values flatten contrast toward a uniformly lit look.

**Directional light** — A separate light type that varies with surface orientation (diffuse term). Ambient and directional combine additively before being multiplied by the object's albedo color.

**Albedo** — The base color of a rendered object (from its renderer component). Final pixel color is `(ambient + diffuse) × albedo`.

**Scene render pipeline** — The per-frame orchestration that resolves camera, gathers light settings from ECS, and issues draw calls through the 3D graphics layer.

**First-match resolution** — When multiple ambient light components exist, the pipeline uses the first one found in iteration order. Additional instances are ignored (documented limitation for the prototype).

## Patterns and Principles

### ECS as the configuration surface

Lighting parameters belong on components, not in global statics or hardcoded shader constants. This keeps scenes self-contained, serializable, and editable. The render pipeline **reads** component data; it does not own lighting state.

### Pull model at render time

The pipeline queries the scene context each frame for ambient light components. There is no separate "lighting system" that pushes state — resolution happens at the point of use, immediately before the 3D draw pass. This minimizes synchronization and keeps the data flow easy to trace.

### Separation of scene logic and graphics API

Scene code resolves *what* the ambient values are. The graphics layer accepts those values and uploads them to the active shader. Scene code never touches GPU uniforms directly; the graphics layer never queries ECS entities.

### Additive lighting model (prototype scope)

The prototype uses a simple `(ambient + diffuse) × albedo` model. There is no specular highlight, no shadow mapping, and no physically based energy conservation. This is intentional: ambient lighting is the minimum viable fill term, not a full PBR lighting rig.

### Sensible defaults over hard failures

If no ambient light entity exists, the pipeline supplies white color at low strength (0.1). Scenes without explicit lighting setup still render legibly rather than producing black geometry or shader errors.

## Architecture Philosophy

Ambient lighting is a **thin vertical slice** through existing layers:

1. **Component** — holds authorable data (color, strength)
2. **Render pipeline** — resolves component data into runtime values
3. **Graphics interface** — transports values to the GPU path
4. **Shader** — consumes uniforms and contributes to final color
5. **Editor** — exposes component fields for live tuning

Each layer has one job. The component does not know about shaders. The shader does not know about entities. The editor does not know about OpenGL.

This feature deliberately does **not** introduce light managers, light registries, or multi-light blending. Those are future concerns once the prototype proves the data path. The design optimizes for traceability and minimal surface area over generality.

## Relationship to Directional Lighting

Ambient and directional lighting are independent components resolved separately. Directional light provides orientation-dependent shading; ambient provides the floor. A well-lit prototype scene typically has both: ambient for base visibility, directional for readable surface orientation.

If directional light is absent or has zero color, ambient alone still produces a flat but visible result — useful for debugging and for scenes that intentionally avoid strong shadows.

## Out of Scope (Prototype)

- Multiple ambient lights blended or averaged
- Ambient occlusion or environment maps
- Per-object ambient overrides
- 2D sprite lighting (ambient applies to the 3D cube pass only)
- HDR exposure or tone mapping of light values

These boundaries keep the first implementation focused and reviewable.
