# Point Lighting — Conceptual Introduction

## What Problem Does This Solve?

Directional light treats every surface as if the source were infinitely far away. A torch, a lamp, or a muzzle flash cannot be placed in the scene: moving an entity does not move the light, and brightness does not fall off with distance.

Point lighting adds a light that lives at a position in the world and fades to nothing at a finite range. It does not replace ambient or directional light. Those terms stay, and the point contribution is added on top.

## What the Feature Will Achieve

- An author places a point light by adding a component to an entity that already has a transform. Moving or parenting that entity moves the light.
- Each light has a color, an intensity, and a single range. At the light, the contribution is full. At the range, and beyond it, the contribution is zero.
- Up to eight point lights affect 3D cubes and 3D models in the same frame. Their contributions add together.
- Cubes and models both receive diffuse and specular from those lights. Ambient and directional shading stay as they are.
- When a scene has no point lights, rendering is unchanged: the point term adds nothing.
- Color, intensity, and range are editable in the editor and persist with the scene.

## Benefits and Outcomes

| Outcome | Why it matters |
|---------|----------------|
| Local lights | Lamps and projectiles can sit in the world instead of acting as a second sun |
| Predictable authoring | Three fields describe a light; there is no separate constant, linear, and quadratic falloff to tune |
| Bounded cost | The frame never uploads more than eight lights, so shader work stays fixed |
| Same receivers | A cube and a mesh respond to the same lamps, including highlights |
| Safe empty scenes | Scenes that only use ambient and directional keep their current look |

## Terminology

**Point light** — A light source at one position. Illumination depends on the direction from the surface point to that position, and on how far apart they are.

**Range** — The distance, in world units, at which the light's contribution becomes zero. Inside the range the light fades. Outside it, that light adds nothing.

**Intensity** — A scalar multiplier on the light color. One is the default brightness. Zero turns the contribution off without removing the light from the frame's set of eight.

**Attenuation** — The fade from full brightness at the light to zero at the range. It follows the square of the remaining fraction of the range, so brightness gathers near the lamp instead of cutting off as a hard disk.

**Diffuse** — The orientation-dependent term. A surface facing the light is brighter than a surface facing away.

**Specular** — The highlight term. It depends on the angle between the light, the surface, and the camera. Models take highlight sharpness from the mesh material. Cubes use the same default sharpness as a mesh that has no material.

**Albedo** — The base color of the surface. Diffuse is tinted by it. Specular uses the surface's specular color, or a neutral default when the surface has no specular map.

**Frame light set** — The list of point lights chosen for one 3D pass. It holds at most eight lights. The shader loops only over that count.

**First-in-order selection** — Lights are considered in entity order. The first eight that are valid fill the set. Later lights are ignored until a slot opens. There is no search for the lights closest to the camera.

## Patterns and Principles

### The entity is the light

The component stores color, intensity, and range. It does not store a position. The position is the world translation of the transform on the same entity. Parenting works because the world transform already includes the parent.

### Pull at the start of the 3D pass

The render pipeline reads the scene once per 3D pass, builds the frame light set, and hands it to the graphics layer. There is no lighting system that pushes updates through the frame. Resolution happens at the point of use, next to ambient and directional resolution.

### Scene logic stays off the GPU

The pipeline decides which lights exist and what their values are. The graphics layer stores that list and uploads it with the other per-frame data. Scene code does not set shader uniforms. The graphics layer does not search entities.

### One formula, two shaders

Cubes and models evaluate the same point-light formula: diffuse plus specular, faded by range. The engine loads each shader as its own file pair, so the formula is duplicated in the two fragment shaders. They must stay in agreement with each other and with the attenuation checked on the CPU.

### Additive combination

The pixel result is ambient, plus directional, plus every point light in the frame set. Point lights do not replace the sun or the fill. They add local illumination.

### Invalid lights do not take a slot

A light without a transform is not in the world, so it is skipped. A light whose range is zero or negative cannot fade correctly, so it is skipped. The next valid light can take that place in the eight. A negative intensity is clamped to zero, but the light still occupies a slot, so turning a lamp down does not suddenly promote another entity.

## Architecture Philosophy

Point lighting is a thin extension of the existing frame-uniform path:

1. **Component** — authorable color, intensity, and range
2. **Pipeline** — selects up to eight valid lights and reads world positions
3. **Graphics interface** — holds the list and uploads it once per 3D pass
4. **Shaders** — add the point contribution on cubes and models
5. **Editor** — exposes the three fields and a way to add the component

The component does not know about shaders. The shaders do not know about entities. The cap of eight is shared knowledge between the pipeline and both shaders: the pipeline never emits more, and the shaders never read more.

This feature does not add shadow maps, a uniform buffer, or a nearest-light search. Eight lights in entity order are enough to prove local lighting. A later design can change selection or delivery without changing the component.

## Relationship to Existing Lights

Ambient remains a scene-wide floor. Directional remains a single distant light, still resolved as the first matching component, still independent of transform. Point lights are a third term. A scene can use any combination. With no point lights, the third term is zero and the picture matches today's lighting.

## Out of Scope

- Shadows from point lights
- More than eight point lights in one frame
- Choosing lights by distance to the camera or to a player
- A separate position field on the component
- Constant, linear, and quadratic coefficients in the inspector
- Lighting for 2D sprites or lines
- A light-radius gizmo
- Per-cube highlight sharpness as an authored field
- Physically based materials or energy conservation

These limits keep the first version on the same path as ambient and directional.
