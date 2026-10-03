# Benchmark Categories — Conceptual Introduction

## What Problem Does This Solve?

The benchmark opens straight into one panel that only stresses 2D sprites and 2D physics. Meshes, lights, and shadows have no scene there, so a change in those paths cannot be timed the way a sprite batch can. The panel is also one place: the list of tests, the clock, and the drawing of each scene sit together. Adding a 3D scene to that list mixes the cost of shadows into a number that was supposed to be about geometry.

## What the Feature Will Achieve

- After launch, the window offers four categories and no scene: 2D, 3D, lighting, and shadows.
- Choosing a category shows only that category's tests. Starting one builds its scene and times it. Back destroys the scene and returns to the four categories.
- Results collected during the session stay on screen across that return. A run stopped with Back is not recorded. A run that finishes, or that the user stops, is recorded.
- 2D keeps the five tests it has today, and it keeps drawing them the way it draws them today.
- 3D times geometry with shadows off and with no point lights: many cubes, many copies of one mesh, and many distinct meshes.
- Lighting times a fixed grid of cubes under three light setups, with both kinds of shadow off: the sun, one point light, and eight point lights.
- Shadows times that same grid with the matching shadow on: casters under the sun, one point light that casts, and eight point lights that cast.
- A directional light with a color no longer always draws a shadow. The view can turn that shadow off. Left alone, the view still draws it.

## Benefits and Outcomes

| Outcome | Why it matters |
|---------|----------------|
| A choice before any scene | The run you start is the cost you meant to measure |
| One clock for every category | Frame time, FPS, CPU, and memory stay comparable |
| 2D unchanged | Existing baselines still match the same test names |
| Geometry without shadows | A mesh change is not hidden inside a depth pass |
| Light without shadows | Eight lamps are not eight cubemaps unless the shadow category says so |
| Shadow against the same grid | The difference from the lighting run is the shadow passes |
| A skipped shadow is visible | A frame that could not build a shadow map does not look like a successful shadow measurement |

## Terminology

**Category** — One of four choices on the opening screen: 2D, 3D, lighting, or shadows. A category owns a list of tests. It does not own the clock or the result list.

**Test** — One timed scene inside a category. It has a stable name, a setup, and a duration.

**Harness** — The part that times frames, samples CPU and memory, stores results, compares a baseline, and exports them. It does not build a scene.

**Runner** — The part that knows one category. It lists the tests, builds the scene, updates it, and attaches the facts that are special to that test.

**Opening screen** — The first view, and the view Back returns to. Four categories. No scene.

**Result name** — The string a result is stored and compared under. 2D keeps the names it already uses. The other categories put the category in the name, so eight point lights with shadows are not the same entry as eight point lights without them.

**Steady grid** — The same cubes, in the same places, for every lighting test and every shadow test. The count does not change between those tests. What changes is the light and whether it casts.

**Mesh identity** — The path a model is loaded under. Copies of one path are one mesh. A distinct path is a distinct mesh, even when the bytes are the same template.

**Directional shadow switch** — A choice on the view. On, a colored sun draws its depth pass, as it does today. Off, that pass is skipped and the sun still lights the color pass.

## Patterns and Principles

### The opening screen is not a test

Nothing is drawn for measurement until a test starts. Back is how you leave a category. It is not a fifth category.

### The harness measures, the runner stages

Frame time, FPS, CPU, and memory are one calculation for every test. The runner only supplies the scene and the extra facts (how many objects, which lights, whether a shadow pass ran).

### A category isolates one cost

3D does not cast shadows and does not add point lights, so the number is submission cost. Lighting uses one grid and turns shadows off, so the number is the light. Shadows uses that grid again and turns the matching shadow on, so the number is the extra depth drawing. A test does not turn the sun's shadow and a lamp's shadow on together.

### Same names, same baselines

A 2D result keeps the name it has today. A lighting result and a shadow result never share a name.

### Fail open, then say so

If a shadow map cannot be built, the frame is still drawn and the light still contributes. The result still exists. Its facts say the shadow pass did not run, so a high frame rate is not read as a shadow measurement.

### Eight lamps is the cap

A point-light test is one lamp or eight lamps. Eight is already the most the frame will shade. The count is the test, not a slider.

## Architecture Philosophy

The window has three jobs that stay apart:

1. **Choose** — the opening screen picks a category
2. **Stage** — that category's runner builds one scene and tears it down
3. **Measure** — the harness times the frames and keeps the list

2D staging stays on the paths it already has: direct sprite drawing, or the scene runtime when the test is physics. 3D, lighting, and shadows stage a scene and draw it through the same pipeline the game uses, because that is where meshes, lights, and shadows already live. They do not start the scene runtime. The view they pass in is what turns each shadow on or off.

The directional shadow switch sits on that view, beside the switch that already turns point-light shadows on or off. Its default is on. Callers that do not mention it keep today's sun shadow. The 3D runner and the lighting runner turn it off. The shadow runner leaves it on for the sun test and turns it off for the lamp tests, so a sun shadow is not added to a cubemap measurement.
