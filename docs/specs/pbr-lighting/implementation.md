# PBR Direct Lighting — Implementation

Step-by-step code for the design in `introduction.md` and `developer-guide.md`. Paste the C# into the existing types. The GLSL is the shader half of the same change. Do not add a shared shader include. Paste the Cook-Torrance functions into both fragment shaders.

Metallic-roughness packing is **G = roughness, B = metallic**. Occlusion is **R**. The roughness floor is **0.045**, applied after the map multiply. Dielectric F0 is **0.04**. The specular denominator epsilon is **0.0001**. The lamp epsilon stays **0.0001**. Shadow sampler unit stays **3**. Occlusion uses unit **4**.

## 1. Component

In `SceneComponents/Rendering/ModelRendererComponent.cs`, add the four fields and clamp in the setters. `Color` stays the albedo tint.

```csharp
private float _metallic;
private float _roughness = 0.5f;
private float _ao = 1f;

public float Metallic
{
    get => _metallic;
    set => _metallic = Sanitize(value, 0f);
}

public float Roughness
{
    get => _roughness;
    set => _roughness = Sanitize(value, 0.5f);
}

public float Ao
{
    get => _ao;
    set => _ao = Sanitize(value, 1f);
}

public bool OverrideMaterial { get; set; }

private static float Sanitize(float value, float fallback) =>
    float.IsFinite(value) ? Math.Clamp(value, 0f, 1f) : fallback;
```

Extend `Clone` with the new fields:

```csharp
public IComponent Clone() => new ModelRendererComponent
{
    Color = Color,
    TexturePath = TexturePath,
    TilingFactor = TilingFactor,
    ModelPath = ModelPath,
    MeshIndex = MeshIndex,
    SuppressDraw = SuppressDraw,
    Metallic = Metallic,
    Roughness = Roughness,
    Ao = Ao,
    OverrideMaterial = OverrideMaterial
};
```

**Why:** The JSON serializer already walks public properties, so old scenes load the defaults and override stays off. Clamping here covers the inspector and a hand-edited scene. The shader clamps again.

## 2. Inspector

In `Editor/ComponentEditors/Rendering/ModelRendererComponentEditor.cs`, after the tiling field:

```csharp
propertyRenderer.DrawPropertyField("Metallic", component.Metallic,
    newValue => component.Metallic = (float)newValue);
propertyRenderer.DrawPropertyField("Roughness", component.Roughness,
    newValue => component.Roughness = (float)newValue);
propertyRenderer.DrawPropertyField("AO", component.Ao,
    newValue => component.Ao = (float)newValue);
if (!string.IsNullOrWhiteSpace(component.ModelPath))
{
    propertyRenderer.DrawPropertyField("Override Material", component.OverrideMaterial,
        newValue => component.OverrideMaterial = (bool)newValue);
}
```

**Why:** A cube has no file factors. Showing the switch there would suggest a second source.

## 3. Mesh

In `Engine/Renderer/Meshes/Mesh.cs`, delete `SpecularTexture`, `Shininess`, and `HasSpecularMap`. Add:

```csharp
public Texture2D? MetallicRoughnessTexture { get; set; }
public Texture2D? OcclusionTexture { get; set; }
public float MetallicFactor { get; set; }
public float RoughnessFactor { get; set; } = 0.5f;
public Vector3 BaseColorFactor { get; set; } = Vector3.One;

public bool HasMetallicRoughnessMap => MetallicRoughnessTexture != null;
public bool HasOcclusionMap => OcclusionTexture != null;
```

Keep `DiffuseTexture`, `NormalTexture`, and their `Has*` properties.

**Why:** Slot 1 stops meaning specular. Call sites that bound a black specular map have to move with this, or they upload Phong data into the roughness channel.

## 4. Factor pick

Add this to `SceneRenderPipeline` next to the other resolve helpers. `Engine.Tests` already sees internals.

```csharp
internal readonly record struct PbrFactors(float Metallic, float Roughness, float Ao);

internal static PbrFactors ResolvePbr(
    bool cube,
    ModelRendererComponent renderer,
    float meshMetallic,
    float meshRoughness)
{
    var entityMetallic = Finite01(renderer.Metallic);
    var entityRoughness = Finite01(renderer.Roughness);
    var entityAo = Finite01(renderer.Ao);
    if (cube || renderer.OverrideMaterial)
        return new PbrFactors(entityMetallic, entityRoughness, entityAo);

    return new PbrFactors(Finite01(meshMetallic), Finite01(meshRoughness), 1f);
}

private static float Finite01(float value) =>
    float.IsFinite(value) ? Math.Clamp(value, 0f, 1f) : 0f;
```

**Why:** Occlusion factor 1 with override off is the rule that lets an occlusion map darken creases without the unused slider zeroing it. `Finite01` maps a bad mesh number to 0, which is the metallic missing-default, not the roughness one. Roughness's missing-default of 0.5 is applied at import, before this function runs. A non-finite roughness that somehow sits on the mesh becomes 0 here and the shader then lifts it to 0.045.

## 5. Draw signatures

In `IGraphics3D`:

```csharp
void DrawCube(Matrix4x4 transform, Vector4 color, int entityId = -1, Texture2D? texture = null,
    float tilingFactor = 1.0f, float metallic = 0f, float roughness = 0.5f, float ao = 1f);

void DrawMesh(Matrix4x4 transform, Mesh mesh, Vector4 tint, int entityId = -1,
    float metallic = 0f, float roughness = 0.5f, float ao = 1f);
```

The defaults keep existing call sites compiling. The pipeline always passes `ResolvePbr` explicitly.

In `SceneRenderPipeline.DrawOpaque3D`, every cube and mesh draw goes through the triple. Failed import is a cube.

```csharp
if (string.IsNullOrWhiteSpace(modelRenderer.ModelPath))
{
    var factors = ResolvePbr(cube: true, modelRenderer, 0f, 0.5f);
    if (!string.IsNullOrWhiteSpace(modelRenderer.TexturePath))
        DrawCubeWithTexture(graphics3D, textureFactory, modelRenderer, transform, entity, factors);
    else
        graphics3D.DrawCube(transform, modelRenderer.Color, entity.Id, metallic: factors.Metallic,
            roughness: factors.Roughness, ao: factors.Ao);
    continue;
}

// ... model == null ...
var fallback = ResolvePbr(cube: true, modelRenderer, 0f, 0.5f);
graphics3D.DrawCube(transform, tint, entity.Id, metallic: fallback.Metallic,
    roughness: fallback.Roughness, ao: fallback.Ao);

// one submesh or each submesh:
var pbr = ResolvePbr(cube: false, modelRenderer, submesh.MetallicFactor, submesh.RoughnessFactor);
graphics3D.DrawMesh(transform, submesh, tint, entity.Id, pbr.Metallic, pbr.Roughness, pbr.Ao);
```

Thread `PbrFactors` through `DrawCubeWithTexture` and into `DrawCube` the same way. The shadow pass is unchanged: it still calls these draws, and `Graphics3D` still returns at `_shadowPass` before any material bind.

Update `RecordingGraphics3D` in `SceneRenderPipelineShadowTests` so the methods accept the three floats. Record them:

```csharp
public List<(float Metallic, float Roughness, float Ao)> CubeFactors { get; } = [];
public List<(float Metallic, float Roughness, float Ao)> MeshFactors { get; } = [];

public void DrawCube(Matrix4x4 transform, Vector4 color, int entityId = -1, Texture2D? texture = null,
    float tilingFactor = 1.0f, float metallic = 0f, float roughness = 0.5f, float ao = 1f)
{
    CubeDraws++;
    CubeFactors.Add((metallic, roughness, ao));
    Order.Add("cube");
}

public void DrawMesh(Matrix4x4 transform, Mesh mesh, Vector4 tint, int entityId = -1,
    float metallic = 0f, float roughness = 0.5f, float ao = 1f)
{
    MeshDraws++;
    MeshFactors.Add((metallic, roughness, ao));
    Order.Add("mesh");
}
```

**Why:** Optional parameters on the interface do not update the test double by themselves. The shadow test class is the other implementer.

## 6. Graphics upload

In `Graphics3D.Init`, replace the specular sampler with metallic-roughness and bind occlusion at unit 4. Leave `u_ShadowMap` at `ShadowMapSlot` (3).

```csharp
_modelShader.Bind();
_modelShader.SetInt("u_DiffuseMap", 0);
_modelShader.SetInt("u_MetallicRoughnessMap", 1);
_modelShader.SetInt("u_NormalMap", 2);
_modelShader.SetInt("u_ShadowMap", ShadowMapSlot);
_modelShader.SetInt("u_OcclusionMap", 4);
_modelShader.Unbind();
```

`DrawCube` still returns immediately when `_shadowPass` is set. After the existing texture bind, add:

```csharp
_cubeShader.SetFloat("u_Metallic", metallic);
_cubeShader.SetFloat("u_Roughness", roughness);
_cubeShader.SetFloat("u_Ao", ao);
```

`DrawMesh` drops shininess and the specular bind. After the shadow early-return:

```csharp
_modelShader.SetFloat("u_Metallic", metallic);
_modelShader.SetFloat("u_Roughness", roughness);
_modelShader.SetFloat("u_Ao", ao);
_modelShader.SetFloat3("u_BaseColor", mesh.BaseColorFactor);
_modelShader.SetInt("u_HasDiffuseMap", mesh.HasDiffuseMap ? 1 : 0);
_modelShader.SetInt("u_HasMetallicRoughnessMap", mesh.HasMetallicRoughnessMap ? 1 : 0);
_modelShader.SetInt("u_HasNormalMap", mesh.HasNormalMap ? 1 : 0);
_modelShader.SetInt("u_HasOcclusionMap", mesh.HasOcclusionMap ? 1 : 0);

(mesh.DiffuseTexture ?? textureFactory.GetWhiteTexture()).Bind(0);
(mesh.MetallicRoughnessTexture ?? textureFactory.GetWhiteTexture()).Bind(1);
(mesh.NormalTexture ?? textureFactory.GetFlatNormalTexture()).Bind(2);
(mesh.OcclusionTexture ?? textureFactory.GetWhiteTexture()).Bind(4);
```

**Why:** The has-map flags are what stop a white stand-in from being read as metal 1. The stand-in only keeps the sampler valid.

## 7. Import

In `AssimpModelImporter`, add two helpers next to `IsUnrealCollisionMesh`:

```csharp
internal static string? ChooseMetallicRoughnessPath(string? packed, string? metalness, string? roughness)
{
    if (!string.IsNullOrEmpty(packed))
        return packed;
    if (!string.IsNullOrEmpty(metalness))
        return metalness;
    if (!string.IsNullOrEmpty(roughness))
        return roughness;
    return null;
}

internal static float ImportedFactor(bool found, float value, float missing)
{
    if (!found || !float.IsFinite(value))
        return missing;
    return Math.Clamp(value, 0f, 1f);
}
```

Change `MaterialInfo` to:

```csharp
private readonly record struct MaterialInfo(
    string? DiffusePath,
    string? NormalPath,
    string? MetallicRoughnessPath,
    string? OcclusionPath,
    float MetallicFactor,
    float RoughnessFactor,
    Vector3 BaseColorFactor);
```

Inside `ExtractMaterialInfo`, keep the albedo and normal lookups. Delete the specular lookup and the shininess read. Then:

```csharp
var packed = ResolveTexturePath(scene, aiMaterial, TextureType.GltfMetallicRoughness, directory);
var metalness = ResolveTexturePath(scene, aiMaterial, TextureType.Metalness, directory);
var roughness = ResolveTexturePath(scene, aiMaterial, TextureType.DiffuseRoughness, directory);
if (string.IsNullOrEmpty(packed)
    && !string.IsNullOrEmpty(metalness)
    && !string.IsNullOrEmpty(roughness)
    && !string.Equals(metalness, roughness, StringComparison.OrdinalIgnoreCase))
{
    Logger.Warning(
        "Metallic and roughness textures differ; using metallic {Path}",
        metalness);
}
var metallicRoughnessPath = ChooseMetallicRoughnessPath(packed, metalness, roughness);
var occlusionPath = ResolveTexturePath(scene, aiMaterial, TextureType.AmbientOcclusion, directory);

var metallicFactor = ReadFactor(aiMaterial, Assimp.MatkeyMetallicFactor, missing: 0f);
var roughnessFactor = ReadFactor(aiMaterial, Assimp.MatkeyRoughnessFactor, missing: 0.5f);
var baseColor = ReadBaseColor(aiMaterial);
```

`ReadFactor` uses the same float read the importer already uses for shininess. Check the return. Silk.NET.Assimp 2.23 names the keys `MatkeyMetallicFactor` and `MatkeyRoughnessFactor`.

```csharp
private unsafe float ReadFactor(Material* material, string key, float missing)
{
    var value = 0f;
    var found = _assimp.GetMaterialFloatArray(material, key, 0, 0, ref value, (uint*)null) == Return.Success;
    return ImportedFactor(found, value, missing);
}

private unsafe Vector3 ReadBaseColor(Material* material)
{
    var color = new Vector4(1f, 1f, 1f, 1f);
    if (_assimp.GetMaterialColor(material, Assimp.MatkeyBaseColor, 0, 0, ref color) != Return.Success)
        return Vector3.One;

    return new Vector3(
        Math.Clamp(color.X, 0f, 1f),
        Math.Clamp(color.Y, 0f, 1f),
        Math.Clamp(color.Z, 0f, 1f));
}
```

If `GetMaterialColor` only exists as the pointer overload, pass a pointer to `color` and still branch on `Return.Success`. Do not fall back to the diffuse color. A missing base color is white, and the entity `Color` still tints.

Where the mesh is filled today:

```csharp
mesh.MetallicFactor = material.MetallicFactor;
mesh.RoughnessFactor = material.RoughnessFactor;
mesh.BaseColorFactor = material.BaseColorFactor;
```

And when textures load, after the scene pointer is released:

```csharp
mesh.DiffuseTexture = LoadTexture(material.DiffusePath, sRgb: true);
mesh.NormalTexture = LoadTexture(material.NormalPath);
mesh.MetallicRoughnessTexture = LoadTexture(material.MetallicRoughnessPath);
mesh.OcclusionTexture = LoadTexture(material.OcclusionPath);
```

`LoadTexture` already defaults `sRgb` to false. Leave metallic-roughness and occlusion on that default.

**Why:** A failed float read must not look like a stored 0. `ImportedFactor` is the branch the test hits without parsing a glTF. The warning fires only when two different images would be silently dropped.

## 8. Shaders

Delete Phong, `u_Shininess`, and `u_SpecularMap` / `u_HasSpecularMap` from both fragment shaders. Keep `DirectionalShadow` and the point-light uniform block as they are.

Paste this into `cube.frag` and `modelShader.frag`:

```glsl
const float PI = 3.14159265359;
const float c_MinRoughness = 0.045;
const float c_DielectricF0 = 0.04;
const float c_SpecularEpsilon = 0.0001;

vec3 FresnelSchlick(float cosTheta, vec3 F0)
{
    return F0 + (1.0 - F0) * pow(clamp(1.0 - cosTheta, 0.0, 1.0), 5.0);
}

float DistributionGGX(vec3 N, vec3 H, float roughness)
{
    float a = roughness * roughness;
    float a2 = a * a;
    float NdotH = max(dot(N, H), 0.0);
    float denom = (NdotH * NdotH) * (a2 - 1.0) + 1.0;
    return a2 / (PI * denom * denom);
}

float GeometrySchlickGGX(float Ndot, float roughness)
{
    float r = roughness + 1.0;
    float k = (r * r) / 8.0;
    return Ndot / (Ndot * (1.0 - k) + k);
}

float GeometrySmith(vec3 N, vec3 V, vec3 L, float roughness)
{
    return GeometrySchlickGGX(max(dot(N, V), 0.0), roughness)
         * GeometrySchlickGGX(max(dot(N, L), 0.0), roughness);
}

vec3 CookTorrance(vec3 N, vec3 V, vec3 L, vec3 radiance, vec3 albedo, float metallic, float roughness)
{
    float NdotL = max(dot(N, L), 0.0);
    vec3 H = normalize(V + L);
    vec3 F0 = mix(vec3(c_DielectricF0), albedo, metallic);
    float D = DistributionGGX(N, H, roughness);
    float G = GeometrySmith(N, V, L, roughness);
    vec3 F = FresnelSchlick(max(dot(H, V), 0.0), F0);
    vec3 specular = (D * G * F) / (4.0 * max(dot(N, V), 0.0) * NdotL + c_SpecularEpsilon);
    vec3 kD = (vec3(1.0) - F) * (1.0 - metallic);
    return (kD * albedo / PI + specular) * radiance * NdotL;
}

vec3 PointLights(vec3 N, vec3 V, vec3 fragPos, vec3 albedo, float metallic, float roughness)
{
    vec3 sum = vec3(0.0);
    for (int i = 0; i < c_MaxPointLights; i++)
    {
        if (i >= u_PointLightCount)
            break;

        vec3 toLight = u_PointLightPositions[i] - fragPos;
        float dist = length(toLight);
        if (dist >= u_PointLightRanges[i])
            continue;

        vec3 radiance = u_PointLightColors[i] * u_PointLightIntensities[i];
        if (dist < c_PointEpsilon)
        {
            sum += radiance * albedo;
            continue;
        }

        float remaining = 1.0 - dist / u_PointLightRanges[i];
        radiance *= remaining * remaining;
        sum += CookTorrance(N, V, toLight / dist, radiance, albedo, metallic, roughness);
    }
    return sum;
}

vec3 Encode(vec3 color)
{
    color = color / (color + vec3(1.0));
    return pow(color, vec3(1.0 / 2.2));
}
```

`k = (roughness + 1)² / 8` is the direct-light Smith term. Do not switch it to `alpha² / 2`.

Cube material and output, replacing the Phong main. The cube has no base-color factor and no MR or AO map:

```glsl
uniform float u_Metallic;
uniform float u_Roughness;
uniform float u_Ao;

void main()
{
    vec3 albedo = (u_UseTexture == 1 ? texture(u_Texture, v_TexCoord).rgb : vec3(1.0)) * u_Color.rgb;
    float metallic = clamp(u_Metallic, 0.0, 1.0);
    float roughness = max(clamp(u_Roughness, 0.0, 1.0), c_MinRoughness);
    float ao = clamp(u_Ao, 0.0, 1.0);

    vec3 N = normalize(v_Normal);
    vec3 V = normalize(u_ViewPosition - v_FragPos);
    vec3 L = normalize(-u_LightDirection);
    float shadow = DirectionalShadow(v_FragPos);
    vec3 sun = CookTorrance(N, V, L, u_LightColor, albedo, metallic, roughness) * shadow;
    vec3 lamps = PointLights(N, V, v_FragPos, albedo, metallic, roughness);
    vec3 ambient = u_AmbientStrength * u_AmbientColor * albedo * ao;
    o_Color = vec4(Encode(ambient + sun + lamps), u_Color.a);
    o_EntityID = u_EntityID;
}
```

Model material uses the maps. Keep the existing normal-map block and use that `norm` as N.

```glsl
uniform float u_Metallic;
uniform float u_Roughness;
uniform float u_Ao;
uniform vec3 u_BaseColor;
uniform int u_HasDiffuseMap;
uniform int u_HasMetallicRoughnessMap;
uniform int u_HasNormalMap;
uniform int u_HasOcclusionMap;
uniform sampler2D u_DiffuseMap;
uniform sampler2D u_MetallicRoughnessMap;
uniform sampler2D u_NormalMap;
uniform sampler2D u_OcclusionMap;

void main()
{
    vec3 norm = u_HasNormalMap != 0
        ? normalize(v_TBN * (texture(u_NormalMap, v_TexCoord).rgb * 2.0 - 1.0))
        : normalize(v_Normal);

    vec3 albedo = (u_HasDiffuseMap != 0 ? texture(u_DiffuseMap, v_TexCoord).rgb : vec3(1.0))
        * u_BaseColor * u_Color.rgb;
    vec3 mr = u_HasMetallicRoughnessMap != 0
        ? texture(u_MetallicRoughnessMap, v_TexCoord).rgb
        : vec3(1.0);
    float metallic = clamp(mr.b * u_Metallic, 0.0, 1.0);
    float roughness = max(clamp(mr.g * u_Roughness, 0.0, 1.0), c_MinRoughness);
    float aoSample = u_HasOcclusionMap != 0 ? texture(u_OcclusionMap, v_TexCoord).r : 1.0;
    float ao = clamp(aoSample * u_Ao, 0.0, 1.0);

    vec3 V = normalize(u_ViewPosition - v_FragPos);
    vec3 L = normalize(-u_LightDirection);
    float shadow = DirectionalShadow(v_FragPos);
    vec3 sun = CookTorrance(norm, V, L, u_LightColor, albedo, metallic, roughness) * shadow;
    vec3 lamps = PointLights(norm, V, v_FragPos, albedo, metallic, roughness);
    vec3 ambient = u_AmbientStrength * u_AmbientColor * albedo * ao;
    o_Color = vec4(Encode(ambient + sun + lamps), u_Color.a);
    o_EntityID = u_EntityID;
}
```

When the MR flag is off, `mr` is 1 so the multiply is a no-op. When the flag is on, G and B scale the uploaded factors. Do not multiply the cube result by albedo a second time.

**Why:** Reinhard plus gamma match the lighting chapter and match an RGBA8 target that FXAA reads next. Alpha stays the entity color's alpha, so a textured cube does not pick up texture alpha.

## 9. Docs

In `docs/guide/concepts/cameras-and-rendering.md`, replace the "supported today" sentence with: triangle meshes, albedo, normal, metallic-roughness, and occlusion maps, Cook-Torrance direct lighting. Not supported: image-based lighting, skinning, animation clips, transparent mesh sort.

In `docs/architecture/rendering-pipeline.md`, replace the sentences that call the 3D path "not a PBR pipeline" and "Lighting is Blinn-Phong" with direct Cook-Torrance, one directional shadow, and no image-based lighting.

## 10. Tests

`tests/Engine.Tests/Scene/SceneRenderPipelineLightingTests.cs`:

```csharp
[Fact]
public void ResolvePbr_Cube_UsesEntityTripleEvenWhenOverrideIsOff()
{
    var renderer = new ModelRendererComponent
    {
        Metallic = 1f,
        Roughness = 0.2f,
        Ao = 0.4f,
        OverrideMaterial = false
    };

    SceneRenderPipeline.ResolvePbr(cube: true, renderer, meshMetallic: 0f, meshRoughness: 1f)
        .ShouldBe(new SceneRenderPipeline.PbrFactors(1f, 0.2f, 0.4f));
}

[Fact]
public void ResolvePbr_ModelOverrideOff_UsesMeshFactorsAndFullOcclusion()
{
    var renderer = new ModelRendererComponent { Metallic = 1f, Roughness = 0f, Ao = 0f };

    SceneRenderPipeline.ResolvePbr(cube: false, renderer, meshMetallic: 0.25f, meshRoughness: 0.75f)
        .ShouldBe(new SceneRenderPipeline.PbrFactors(0.25f, 0.75f, 1f));
}

[Fact]
public void ResolvePbr_ModelOverrideOn_UsesEntityTriple()
{
    var renderer = new ModelRendererComponent
    {
        Metallic = 1f,
        Roughness = 0.2f,
        Ao = 0.4f,
        OverrideMaterial = true
    };

    SceneRenderPipeline.ResolvePbr(cube: false, renderer, meshMetallic: 0f, meshRoughness: 1f)
        .ShouldBe(new SceneRenderPipeline.PbrFactors(1f, 0.2f, 0.4f));
}

[Fact]
public void ResolvePbr_OutOfRange_Clamps()
{
    var renderer = new ModelRendererComponent { Metallic = 2f, Roughness = -1f, Ao = 3f };

    SceneRenderPipeline.ResolvePbr(cube: true, renderer, 0f, 0.5f)
        .ShouldBe(new SceneRenderPipeline.PbrFactors(1f, 0f, 1f));
}
```

The setters already clamp, so `Metallic = 2f` becomes 1 before `ResolvePbr`. That is the clamp the scene file hits. Also call `Finite01` indirectly with a mesh factor above 1:

```csharp
SceneRenderPipeline.ResolvePbr(
        cube: false,
        new ModelRendererComponent(),
        meshMetallic: 2f,
        meshRoughness: float.NaN)
    .ShouldBe(new SceneRenderPipeline.PbrFactors(1f, 0f, 1f));
```

`tests/Engine.Tests/Renderer/PbrImportTests.cs`:

```csharp
[Fact]
public void ChooseMetallicRoughnessPath_PackedWins()
{
    AssimpModelImporter.ChooseMetallicRoughnessPath("packed.png", "metal.png", "rough.png")
        .ShouldBe("packed.png");
}

[Fact]
public void ChooseMetallicRoughnessPath_MetalnessWinsWhenPathsDiffer()
{
    AssimpModelImporter.ChooseMetallicRoughnessPath(null, "metal.png", "rough.png")
        .ShouldBe("metal.png");
}

[Fact]
public void ChooseMetallicRoughnessPath_SameSeparatePaths_ReturnsThatPath()
{
    AssimpModelImporter.ChooseMetallicRoughnessPath(null, "orm.png", "orm.png")
        .ShouldBe("orm.png");
}

[Fact]
public void ImportedFactor_MissingOrNonFinite_UsesDefault()
{
    AssimpModelImporter.ImportedFactor(false, 1f, 0f).ShouldBe(0f);
    AssimpModelImporter.ImportedFactor(true, float.NaN, 0.5f).ShouldBe(0.5f);
}

[Fact]
public void ImportedFactor_PresentValue_Clamps()
{
    AssimpModelImporter.ImportedFactor(true, 1f, 0f).ShouldBe(1f);
    AssimpModelImporter.ImportedFactor(true, 2f, 0f).ShouldBe(1f);
}
```

Pipeline recording goes in `SceneRenderPipelineShadowTests`, which already has `ViewProjection` and `RecordingGraphics3D`. No directional light, so only the color draw runs:

```csharp
[Fact]
public void RenderScene_Cube_PassesEntityFactors()
{
    var context = new Context();
    var entity = new Entity(1, "gold");
    entity.AddComponent(new TransformComponent());
    entity.AddComponent(new ModelRendererComponent { Metallic = 1f, Roughness = 0.2f, Ao = 0.8f });
    context.Register(entity);

    var graphics = new RecordingGraphics3D();
    SceneRenderPipeline.RenderScene(
        context,
        Substitute.For<IGraphics2D>(),
        graphics,
        Substitute.For<ITextureFactory>(),
        Substitute.For<IModelFactory>(),
        new SceneView(ViewProjection(new Vector3(0f, 2f, 5f))));

    graphics.CubeFactors.ShouldBe([(1f, 0.2f, 0.8f)]);
}

[Fact]
public void RenderScene_MeshOverrideOff_PassesSubmeshFactors()
{
    var mesh = new Mesh("part") { MetallicFactor = 0.3f, RoughnessFactor = 0.6f };
    var model = new Model("prop.glb", [mesh]);
    var models = Substitute.For<IModelFactory>();
    models.Create(Arg.Any<string>()).Returns(model);

    var context = new Context();
    var entity = new Entity(1, "prop");
    entity.AddComponent(new TransformComponent());
    entity.AddComponent(new ModelRendererComponent
    {
        ModelPath = "prop.glb",
        Metallic = 1f,
        Roughness = 0f,
        Ao = 0f,
        OverrideMaterial = false
    });
    context.Register(entity);

    var graphics = new RecordingGraphics3D();
    SceneRenderPipeline.RenderScene(
        context,
        Substitute.For<IGraphics2D>(),
        graphics,
        Substitute.For<ITextureFactory>(),
        models,
        new SceneView(ViewProjection(new Vector3(0f, 2f, 5f))));

    graphics.MeshFactors.ShouldBe([(0.3f, 0.6f, 1f)]);
}
```

Add the override-on case the same way and expect `(1, 0, 0)` from the entity, not the submesh. Dispose the model at the end of the test. An uninitialized mesh disposes without a vertex array.

Do not add a test that reimplements GGX on the CPU.

**Why:** The factor pick is the branch that silently flattens a multi-material model. The import helpers are the branch that turns a missing key into a full metal. The shader stays one copy per file, checked by reading it, not by a second formula in C#.
