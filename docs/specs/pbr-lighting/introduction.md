# PBR Direct Lighting — Conceptual Introduction

## What Problem Does This Solve?

Cubes and models are shaded with Blinn-Phong: a tint, a specular map, and a shininess exponent. A glTF material does not work that way. It describes a surface with albedo, metalness, roughness, and ambient occlusion. Under Blinn-Phong those maps are either ignored or read as the wrong kind of highlight, so a metal and a plastic can only be faked.

This feature replaces that surface response with the direct-lighting model from LearnOpenGL's PBR Lighting chapter, including textured metallic-roughness and occlusion. Image-based lighting is a later chapter and is not part of this feature. Shadows, the sun, and point lights stay where they are. Only the way a surface turns light into a color changes.

## What the Feature Will Achieve

- Every cube and every imported mesh is shaded with Cook-Torrance. Blinn-Phong is gone.
- Albedo comes from the base-color texture when one exists, tinted by the file's base color and by the entity color.
- Metalness, roughness, and occlusion come from glTF textures when those textures exist. The metallic-roughness map uses the glTF packing: roughness in G, metalness in B. Occlusion uses R.
- The entity can override the numeric factors. Textures still multiply the factor. A cube has no file, so its sliders are the material.
- A model leaves the file in charge until override is turned on. One override applies to every submesh of that entity.
- The sun and up to eight point lights use the same surface function. The sun is still the only shadowed light. Point lights still fade with the existing range curve.
- A flat ambient term remains, multiplied by albedo and occlusion. It is the fill until image-based lighting exists.
- The color target is still 8-bit, so the shader compresses HDR brightness with Reinhard and then encodes gamma before FXAA.

## Benefits and Outcomes

| Outcome | Why it matters |
|---------|----------------|
| glTF materials read as authored | Metal, roughness, and occlusion maps land in the slots the file uses |
| One surface model | Cubes and meshes cannot drift back to Phong on one of the two shaders |
| Tunable props | A cube can be gold or chalk without a mesh file |
| File look preserved until edited | Override off keeps each submesh's own factors |
| Existing lights keep their jobs | Ambient, one sun, eight lamps, and the sun shadow do not get a new selection scheme |
| Highlights survive the 8-bit target | Reinhard keeps a bright specular lobe from clipping to white before FXAA |

## Terminology

**Albedo** — The base color of the surface in linear light. Metals tint their reflection with it. Dielectrics tint their diffuse with it.

**Metallic** — A mix from dielectric (0) to metal (1). It chooses the reflectance color and removes the diffuse term as it rises.

**Roughness** — How tight the specular lobe is. Low roughness is a sharp highlight. High roughness is a wide, dim one.

**Ambient occlusion** — A darkening of the ambient fill in creases. It does not shadow the sun or the lamps.

**Factor** — The scalar that a texture multiplies. Metalness is the map's B channel times the factor, or the factor alone when the map is missing. Roughness uses G the same way. Occlusion uses R the same way.

**Override** — An entity switch. Off, the mesh factors are used and the occlusion factor is 1. On, the entity's three sliders replace those factors for every submesh that entity draws. Textures still multiply.

**Cook-Torrance** — The direct specular and diffuse response used here: GGX distribution, Smith geometry with the direct-light Schlick masking, and Schlick Fresnel. Diffuse is energy-conserving: what Fresnel reflects is not also diffused, and metals have no diffuse.

**F0** — Reflectance at a head-on view. Dielectrics use 0.04. Metals use the albedo.

**Radiance** — The light color arriving at the surface before the surface response. The sun's radiance is its color. A lamp's radiance is color times intensity times the existing range fade.

**Reinhard** — The compression `color / (color + 1)` applied after lighting, while the value is still linear. Gamma 2.2 is applied after that. Alpha is not compressed.

## Patterns and Principles

### The file owns the maps, the entity owns the override

Imported textures and imported factors live on the submesh. The entity stores three sliders and a switch. The scene file does not copy the mesh factors. Turning override on is how an author replaces them.

### One formula, two shaders

Cubes and models evaluate the same Cook-Torrance function for the sun and for every point light. The engine loads each shader as its own file, so the function is duplicated. The two copies must stay in agreement. The cube gathers albedo from its optional texture. The model gathers albedo, normal, metallic-roughness, and occlusion from the submesh. After that, the light math is the same.

### Factors are chosen before the draw

The render pipeline decides the three numbers for a draw: cube sliders, mesh factors, or entity sliders. The graphics layer uploads those numbers and binds whatever maps the mesh has. It does not inspect the entity. A missing map does not change the numbers. The shader multiplies by 1 for that channel.

### Lights stay on the old delivery path

Ambient, the sun, the eight point lights, and the directional shadow are resolved and uploaded as they are today. Point-light fade stays the square of the remaining fraction of the range. This feature does not switch lamps to inverse-square falloff.

### Linear light, then a display encode

Albedo is stored as sRGB and sampled as linear. Metallic-roughness, occlusion, and normals are stored as linear. Lighting runs in linear space. Reinhard and gamma run at the end of the fragment, because the scene color is an 8-bit buffer and FXAA runs on that buffer afterward.

### A missing map is an ordinary surface

No metallic-roughness map means a uniform metal and roughness. No occlusion map means the occlusion factor alone. No albedo map means white. No normal map means the vertex normal. Import logs a missing file once. The frame does not log it again.

## Architecture Philosophy

Direct PBR is a change of material and of the fragment's surface function. It is not a new pass, not a new light type, and not a deferred pipeline.

1. **Entity** — color tint, three sliders, override switch
2. **Submesh** — imported textures and imported factors
3. **Import** — reads glTF albedo, normal, metallic-roughness, occlusion, and factors; drops specular and shininess
4. **Pipeline** — picks the three factors for the draw
5. **Graphics** — binds maps and uploads the factors on the color draw; the shadow draw returns before any of that
6. **Shaders** — Cook-Torrance, ambient fill, Reinhard, gamma

The roughness floor used to keep GGX stable (0.045) is part of the shader, applied after the texture multiply. Authors may still set roughness to 0. The stored value is not rewritten.

## Relationship to Existing Rendering

Directional shadow still scales only the sun term. Point lights stay unshadowed. A fragment closer than a tiny epsilon to a lamp still skips the surface function and adds radiance times albedo, so a zero-length light vector is never normalized. Entity id still goes to the integer attachment and is not tone-mapped.

Scenes that already have a model renderer gain the new fields at their defaults: override off, metal 0, roughness 0.5, occlusion 1. Override off means those defaults are ignored for models. The picture still changes, because the surface function is no longer Blinn-Phong.

## Out of Scope

- Image-based lighting, environment maps, and a prefiltered sky
- A floating-point scene buffer, bloom, or auto exposure
- Cascaded shadow maps and point-light shadows
- Inverse-square point lights
- Clear coat, anisotropy, and transmission
- Sorting transparent meshes
- A second lighting model kept alive for old scenes
- Per-submesh sliders on a single entity (unpack the model onto children, then override each child)
- Reading occlusion from the R channel of the metallic-roughness texture unless the file's occlusion slot points at that image
