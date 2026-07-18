# 3D Rendering

**Formats:** `.fbx`, `.gltf`, `.glb` (Assimp). Empty or bad `ModelPath` → lit 1×1 cube.

**Lights:** first `AmbientLightComponent` + first `DirectionalLightComponent` per frame. No directional light → zero directional contribution (ambient only).

**Not supported:** animation/skins, IBL, shadows, per-submesh entities.

## Setup

1. Scene **3D** (Properties, no selection) or **Create 3D Entity**
2. Primary **Perspective** camera (included in Create 3D Entity)
3. **Ambient Light** + **Directional Light** entities
4. **Model Renderer** → drag model onto **Model** field (or leave empty for cube)

Example: `Editor/assets/scenes/3d.scene`.

## Models

One entity draws all submeshes under its transform. Textures resolve relative to the model file. Parsed once, then cached. Missing maps use defaults — whole import still loads.

Inspector tuning: **Color** tint, optional **Metallic** / **Roughness Override** (0–1, all submeshes). No per-map swap in editor yet.

## PBR

Metal/rough workflow: albedo, metallic-roughness packed map (G=roughness, B=metallic), optional normal. Legacy Phong → heuristic conversion.

## Troubleshooting

| Symptom | Check |
|---------|-------|
| Nothing visible | Primary camera, frustum, scene 3D + perspective |
| Black / flat | Directional `Color` non-zero; raise ambient `Strength` |
| Cube fallback | `ModelPath`, file under `assets/`, console for Assimp errors |
| Wrong scale | Transform **Scale** |
| Bad metals | Overrides on component, or fix maps in DCC |

Details: [Component Inspector](../editor/component-inspector.md#modelrenderercomponent), [Rendering Pipeline](../../architecture/rendering-pipeline.md).
