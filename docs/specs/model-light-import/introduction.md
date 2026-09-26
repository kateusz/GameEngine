# Model Light Import — Conceptual Introduction

## What Problem Does This Solve?

A model file can carry the lamps that were placed with the meshes. Dropping that file into the editor creates mesh entities and drops the lamps. The points and the sun exist only in the file, not in the scene, so the room renders with whatever lights the scene already had.

The engine already knows how to light a scene from point lights and one directional light. This feature does not add a new kind of light. It turns the lamps in the file into those existing scene lights at the moment the model hierarchy is unpacked.

## What the Feature Will Achieve

- Point lights and directional lights in a `.glb`, `.gltf`, or `.fbx` become entities when the editor unpacks the model.
- A point light sits on a transform, so parenting and moving the model move the lamp. Its color, intensity, and range are the fields the point light already has.
- A directional light stores a direction on the component, in the space of the model root at import. Rotating the entity afterward does not rotate that vector. That is how directional lights already work.
- File brightness is converted once, at import, into those fields. The result is a lamp in the same brightness band as a hand-placed light, with a finite range of a few meters up to the mid-teens. A finite range stored in the file is kept. A missing range is derived from brightness.
- A directional color that arrives brighter than white is pulled down until its brightest channel is 1. A color that is already at most 1 is kept.
- Spot, area, and ambient lights in the file are ignored. The frame still uses the first directional light and the first eight point lights in the scene. Import does not change that selection.
- A model with a single mesh still draws that mesh on the root. Its lamps become children of the root. A lamp on the file's root node is also a child, so undoing the hierarchy import removes it with the other children.
- Unpacking again destroys those children and creates them again, the same way mesh children are replaced. Edits made to an imported lamp in the inspector do not survive a second unpack.

## Benefits and Outcomes

| Outcome | Why it matters |
|---------|----------------|
| Lamps arrive with the model | A file that was lit in a DCC tool places those lights in the scene instead of only the meshes |
| Same authoring after import | Color, intensity, range, and direction are the existing components, so the inspector and the scene file already know how to edit and save them |
| One conversion | glTF candela, directional lux, and FBX intensity all pass through the same Assimp color, then one formula |
| Predictable brightness | A fire does not white-out the frame, and a candle does not import as a number the shader treats as zero |
| Packed models still draw | One mesh stays on the root. Lights are extra children, not a reason to hide the mesh |
| Undo matches meshes | Imported lamps are children, so the existing hierarchy undo removes them |

## Terminology

**Imported light** — A point or directional lamp read from the model file and stored on the scene graph as engine fields. The spawner copies those fields onto a component. The graph does not keep candela or lux.

**File brightness** — The strongest channel of the diffuse color Assimp reports. For glTF, Assimp has already multiplied the file color by the file intensity, so this channel is the brightness the formula sees. It is not watts.

**Derived range** — The range used when the file does not provide a finite cutoff. It grows with the square root of file brightness and is clamped so a small lamp dies within a couple of meters and a very bright one dies by 16 meters.

**File range** — A finite cutoff distance stored on the node, in meters. When it is present and positive, it replaces the derived range. The placeholder attenuation Assimp writes for every glTF point light is not a file range.

**Baked direction** — The directional vector written onto the component at import, expressed in the model root's space. The component does not read the entity rotation on later frames.

**Packed model** — A file whose scene graph has a single mesh. The root entity keeps that mesh and keeps drawing it. Lamps are still spawned.

**Unpacked model** — A file with more than one mesh. The hierarchy spawner creates an entity per node, as it does today. A lamp is added to the entity of its node.

## Patterns and Principles

### Convert once, then forget the file

The importer reads Assimp lights in the same import as the meshes, converts them, and attaches the result to the scene-graph node. After that, the model asset and the scene entities do not know which format the file was. Reloading the scene does not read the file again for lights. A new export shows up when the hierarchy is unpacked again.

### The entity is the light

Point position is the world translation of the transform, including parents. The component holds color, intensity, and range. Directional direction is a vector on the component. Import uses those rules. It does not invent a second way to place a lamp.

### One record for every format

glTF, GLB, and FBX all arrive as Assimp lights. The formula reads that record. It does not branch on the file extension. An FBX whose intensity was not multiplied into the color looks dim and short-ranged. The inspector is the correction. That ceiling is accepted.

### Selection stays where it is

Import can create dozens of point lights and more than one directional light. The frame still takes the first directional light and the first eight valid point lights in view order. Lights already in the scene are part of that walk. Import does not sort by brightness and does not raise the cap.

### Reimport replaces children

Mesh children are destroyed and recreated when the hierarchy is unpacked again. Light children follow the same command. A lamp placed on the model root entity would survive that undo and redo path. Lamps are therefore children, including a lamp that belonged to the file's root node.

## Architecture Philosophy

The change sits on the existing import path:

1. **Importer** — reads point and directional lights, converts brightness, attaches engine fields to the scene-graph node with the same name.
2. **Scene graph** — carries those fields beside the mesh indices and the local transform. It does not reference a component.
3. **Hierarchy spawner** — creates or reuses the node entity and adds the existing light component. Packed models get a separate light walk so the single mesh still draws.
4. **Frame** — unchanged. Ambient, the first directional light, and eight point lights are resolved as they are today.
