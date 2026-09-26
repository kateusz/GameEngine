# Point Shadows — Conceptual Introduction

## What Problem Does This Solve?

A point light shines in every direction, and today nothing blocks it. A crate between a lamp and a floor still leaves the floor fully lit by that lamp. The directional light already casts a shadow. Point lights only add color, intensity, and a falloff out to their range.

This feature makes every point light that already contributes to the frame cast its own shadow, out to the range it already has.

## What the Feature Will Achieve

- Each point light in the frame, up to the existing cap of eight, casts a shadow around itself.
- The shadow reaches as far as that light's range and no farther. Inside the range, a nearer opaque surface blocks a farther one.
- Only that light's diffuse and specular terms are darkened. Ambient light and the directional light keep their current shadows and fill.
- Cubes and models both cast and receive, including the fallback cube drawn when a model fails to load. Sprites, lines, and physics debug shapes do neither.
- A light with no usable shadow map still lights the scene. The frame is not blacked out.
- Nothing new appears in the inspector. Face size, the near plane, and the self-shadow bias are fixed constants.

## Benefits and Outcomes

| Outcome | Why it matters |
|---------|----------------|
| Occlusion around a lamp | A point light stops shining through crates, walls, and characters |
| Same receivers | The geometry that already receives point light is the geometry that casts and receives the shadow |
| Unchanged fill | Ambient and the directional term stay independent, so a shadowed side of a crate is not crushed to black |
| Every contributing lamp | A scene with several point lights gets a shadow from each of them, up to eight |
| Safe frames | A map that cannot be built leaves that lamp unshadowed and the rest of the frame intact |
| No new authoring | Range, color, and intensity stay the controls the light already has |

## Terminology

**Point light** — A light with a position, a color, an intensity, and a range. It illuminates in every direction and fades to nothing at the range. The frame already keeps at most eight of them.

**Shadow cubemap** — Six square depth images arranged as the faces of a cube around the lamp. Each face is what the lamp sees through a 90° perspective. Together they cover every direction. This is not a color image.

**Face camera** — One of those six views. Its eye is the lamp. Its far plane is the lamp's range. Its near plane is a small fixed distance, so the lamp does not try to rasterize geometry inside that distance.

**Stored distance** — The value written into a face: how far the surface is from the lamp, divided by the range. Zero is the lamp. One is the far plane. An empty texel stays at one, which means "nothing blocked the light."

**Shadow factor** — A number from 0 to 1 for one lamp at one fragment. 0 means that lamp is fully blocked. 1 means it arrives unblocked. Values in between are the softened edge. The factor multiplies only that lamp's diffuse and specular contribution.

**Percentage-closer filtering** — Several depth comparisons around the direction from the lamp to the fragment, averaged. Each comparison is still a yes or no. The average is what softens the edge. The samples stay in the plane perpendicular to that direction, about two texels wide at the fragment's distance.

**Depth bias** — A small fixed world distance subtracted, after dividing by the range, before the comparison. It stops a surface from shadowing itself because the stored distance and the fragment's distance are never exactly equal.

**Caster and receiver** — The same opaque cubes and models. A caster is drawn into the six faces. A receiver reads the cubemap in the color pass.

## Patterns and Principles

### Six drawings around the lamp, not a new light

The point-light component stays position, color, intensity, and range. The shadow is six extra drawings of geometry that already exists, from cameras built at that position. The range the light already uses for falloff is the far plane of those cameras.

### The pipeline decides, the graphics layer draws

The render pipeline walks the point lights and the cubes and models. The graphics layer owns the cubemaps, the depth shader that stores distance, and the upload into the color shaders. Scene code does not bind textures. The graphics layer does not search entities.

### One walk, many passes

Cubes, textured cubes, models, failed-model fallbacks, and suppressed draws are decided in one walk. The directional depth pass, each cubemap face, and the color pass all call it. A fallback cube in the picture is a fallback cube in every face.

### Distance, not window depth

The directional map stores window depth, because an orthographic sun has a single depth axis. A point light does not. Each face stores distance from the lamp over range, and the color shader compares that same ratio. Mixing the two comparisons would darken the wrong pixels.

### One comparison, two shaders

Cubes and models use the same cubemap test. The engine loads each shader as its own file, so the test is copied into both fragment shaders. They have to stay in agreement.

### Fail open

If a cubemap cannot be created, that lamp's factor stays 1 and the frame continues. A shadow that cannot be computed does not black out the scene, and it does not turn off the directional shadow.

### The cube is the range, not the view

The directional map follows the camera. A point shadow does not. It covers every direction out to the lamp's range, including directions the camera cannot see, because those casters still block light that does reach the view. Geometry past the range is clipped and casts nothing.

## Architecture Philosophy

Point shadows sit on the frame path that point lights already use, after the directional shadow and before the color pass:

1. **Resolve** — the same list of up to eight point lights, skipping a non-positive range
2. **Faces** — six perspective cameras per lamp, far plane equal to that lamp's range
3. **Depth passes** — the shared walk draws into one face at a time, storing distance over range, with front faces culled
4. **Color pass** — the existing drawing, each lamp sampling its own cubemap and scaling only its own term

The directional shadow keeps its own map, its own fit, and its own factor. Ambient stays a scene-wide floor.

## Relationship to Existing Lights

Ambient is never multiplied by a point-shadow factor. The directional light still casts only through its own map. A point light past the cap of eight still neither lights nor casts. A point light whose range is too small to place a near and a far plane still lights, with factor 1.

With no point lights, no cubemap is drawn and the picture matches the directional-shadow frame.

## Out of Scope

- A geometry shader that emits all six faces in one draw
- Two-hemisphere maps in place of a cubemap
- More than eight shadowed point lights
- A shadow on sprites, lines, or physics debug drawing
- A shadow toggle, resolution, bias, or near-plane field on the component
- A kernel wider than the nine-sample average
- Changing the directional map, its fit, or its filtering
- Skipping lamps that sit outside the camera view
- Transparent casters and colored shadows

These limits keep the feature to one cubemap per contributing point light, six ordinary draws per cubemap, and no new authoring.
