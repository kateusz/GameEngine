# Image-Based Lighting — Implementation

Step-by-step code for the design in `introduction.md` and `developer-guide.md`. Paste the C# into the existing types. The GLSL is the shader half of the same change. Do not add a shared shader include.

There is no new component, no new system, and no new DryIoc registration. `Skybox` is already serialized. `IGraphics3D` is already a singleton. `Graphics3D` stays one type. The convolution draws sit next to the current six-face capture.

Environment face size stays `LightingMath.SkyCaptureFaceSize` (512). Capture near stays 0.1, far stays `SkyCaptureFar` (10).

## 1. Sizes

`LightingMath` gains the sizes the capture resizes to. The sample count and the hemisphere step live in the shaders.

```csharp
public const int IrradianceFaceSize = 32;
public const int PrefilterFaceSize = 128;
public const int PrefilterMipCount = 5;
public const int BrdfLutSize = 512;
```

**Why:** C# only needs the values that size a texture or a viewport. Duplicating the shader's 1024 here would be a second source for a number the CPU never reads.

## 2. Radiance decode

`TextureFileDecoder` gains a float load beside `Decode`. It takes the same lock and sets the same vertical flip.

```csharp
internal readonly record struct DecodedHdr(float[] Rgb, int Width, int Height);

public static DecodedHdr? DecodeHdr(string path)
{
    if (!File.Exists(path) || !path.EndsWith(".hdr", StringComparison.OrdinalIgnoreCase))
        return null;

    lock (DecodeLock)
    {
        StbImage.stbi_set_flip_vertically_on_load(StbiFlipVerticallyEnabled);
        using var stream = File.OpenRead(path);
        var image = ImageResultFloat.FromStream(stream, ColorComponents.RedGreenBlue);
        if (image.Width <= 0 || image.Height <= 0 || image.Data is not { Length: > 0 })
            return null;
        foreach (var sample in image.Data)
        {
            if (!float.IsFinite(sample))
                return null;
        }

        return new DecodedHdr(image.Data, image.Width, image.Height);
    }
}
```

`OpenGLSkyCapture.TryCreate` calls `DecodeHdr` instead of `Decode`. A null result logs once and returns false. A width that is not twice the height logs once and continues. The equirect upload expands to RGBA with alpha 1, clamps every channel to `(float)Half.MaxValue`, and uses `InternalFormat.Rgba16f`, `PixelFormat.Rgba`, and `PixelType.Float`.

**Why:** `ImageResult.FromStream` returns bytes. The radiance load is the float API already in StbImageSharp 2.30.16. Non-finite samples would survive Reinhard as garbage. A finite sample above 65504 becomes `+Inf` in half float, hence the clamp at upload. The lock keeps the flip flag stable against the byte decoder.

## 3. Capture object

`ISkyCapture` attaches every face through one `Begin`. It also exposes the two blurred-map ids the graphics layer keeps.

```csharp
public interface ISkyCapture : IDisposable
{
    bool GenerateEnvironmentMips();
    bool Begin(uint cubemap, int face, int mip, int size);
    uint IrradianceId { get; }
    uint PrefilterId { get; }
}
```

`TryCreateSkyCapture` still returns the environment id through its existing `out` parameter. All three cubemaps are `RGBA16F`, clamp on S, T, and R. OpenGL 3.3 does not require `RGB16F` to be color-renderable. Blending is off for the capture draws and the lookup draw.

- Environment: 512, base level only at creation. `GenerateEnvironmentMips` runs after the six faces, sets minification to trilinear, and calls `GenerateMipmap`.
- Irradiance: 32, linear filter, no mips.
- Prefilter: base level 128 on each face, then `GenerateMipmap`, minification trilinear. A prefilter face is `Begin(prefilter, face, mip, 128 >> mip)`.

`Begin` attaches `GL_TEXTURE_CUBE_MAP_POSITIVE_X + face` of `cubemap` at `mip`, resizes the depth renderbuffer to `size`, sets the viewport, and clears. It returns false when the framebuffer is incomplete. The draw loop binds the source on unit 0 before the faces: the equirect as a 2D texture for the environment pass, the environment cubemap for the two blurs. The other target on that unit is unbound first.

`Dispose` deletes the equirect, the framebuffer, and the depth buffer, and restores the framebuffer and the viewport when a pass saved them. It leaves the three cubemap ids. `SetSkybox` deletes those ids when a draw fails.

**Why:** One attach covers every face. The environment id already outlives the capture object, and the two blurred maps follow that rule. The caller already deletes a rejected sky cubemap today.

## 4. Lookup

`IRendererAPI.TryCreateBrdfLut(out uint textureId)` builds the lookup while the BRDF program is bound. It logs once and returns false when the texture or the framebuffer cannot be created. The texture is `RG16F`, 512², clamp to edge, linear. The framebuffer has a color attachment and no depth buffer. The method saves the viewport and the bound framebuffer, draws, deletes the framebuffer and the quad, and restores both. The texture id stays with the caller.

The quad is six clip-space vertices, `DrawArrays`:

| Clip | UV |
|------|----|
| (−1, −1) | (0, 0) |
| (1, −1) | (1, 0) |
| (1, 1) | (1, 1) |
| (−1, −1) | (0, 0) |
| (1, 1) | (1, 1) |
| (−1, 1) | (0, 1) |

**Why:** The cube mesh locates texture coordinates at attribute 2 and uses them per face. This quad is attribute 0 position and attribute 1 uv, and it exists only inside this call.

## 5. Shader ids

```csharp
Irradiance,
Prefilter,
BrdfLut
```

`EngineShaderPaths` maps `Irradiance` and `Prefilter` to `skybox.vert` plus `irradiance.frag` and `prefilter.frag`. `BrdfLut` maps to `brdfLut.vert` and `brdfLut.frag`.

`Graphics3D.Init` creates the three programs. The irradiance and prefilter programs set `environmentMap` to unit 0. On the cube and model programs it sets, once:

```csharp
shader.SetInt("u_Irradiance", 13);
shader.SetInt("u_Prefilter", 14);
shader.SetInt("u_BrdfLut", 15);
```

**Why:** The unit numbers are not frame state. The shadow samplers are already fixed the same way.

## 6. Shaders

### Sky

`skybox.frag` reads lod 0 and encodes. Entity id stays −1.

```glsl
void main()
{
    vec3 envColor = textureLod(u_Skybox, v_Direction, 0.0).rgb;
    envColor = envColor / (envColor + vec3(1.0));
    envColor = pow(envColor, vec3(1.0 / 2.2));
    o_Color = vec4(envColor, 1.0);
    o_EntityID = -1;
}
```

**Why:** The environment is linear `RGB16F` and the scene target is `RGBA8`. Lod 0 keeps the backdrop on the base face after mipmaps exist.

### Irradiance

`irradiance.frag`. One color output. `environmentMap` is the environment cubemap.

```glsl
#version 330 core

layout(location = 0) out vec4 o_Color;
in vec3 v_Direction;

uniform samplerCube environmentMap;

const float PI = 3.14159265359;

void main()
{
    vec3 normal = normalize(v_Direction);
    vec3 up = abs(normal.z) < 0.999 ? vec3(0.0, 0.0, 1.0) : vec3(1.0, 0.0, 0.0);
    vec3 right = normalize(cross(up, normal));
    up = normalize(cross(normal, right));

    vec3 irradiance = vec3(0.0);
    float nrSamples = 0.0;
    const float sampleDelta = 0.025;
    const float c_SourceLod = 4.0;
    for (float phi = 0.0; phi < 2.0 * PI; phi += sampleDelta)
    {
        for (float theta = 0.0; theta < 0.5 * PI; theta += sampleDelta)
        {
            vec3 tangent = vec3(sin(theta) * cos(phi), sin(theta) * sin(phi), cos(theta));
            vec3 sampleVec = tangent.x * right + tangent.y * up + tangent.z * normal;
            irradiance += textureLod(environmentMap, sampleVec, c_SourceLod).rgb * cos(theta) * sin(theta);
            nrSamples++;
        }
    }

    irradiance = PI * irradiance * (1.0 / nrSamples);
    o_Color = vec4(irradiance, 1.0);
}
```

**Why:** An explicit lod is the diffuse-chapter sample after the specular chapter has added mips. Lod 4 texels (about 2.8°) are wider than the 0.025-radian step, so a small sun is averaged instead of hit or missed per texel. The `+Z` up matches `ImportanceSampleGGX` so the `+Y` face does not cross a zero tangent. `π` times the average is the value the color shader multiplies by albedo.

### Prefilter

`prefilter.frag`. `DistributionGGX` is the function already in `cube.frag`. `brdfLut.frag` uses the same Hammersley pair and the same `ImportanceSampleGGX`. Paste both copies. There is no shared include.

```glsl
float RadicalInverse_VdC(uint bits)
{
    bits = (bits << 16u) | (bits >> 16u);
    bits = ((bits & 0x55555555u) << 1u) | ((bits & 0xAAAAAAAAu) >> 1u);
    bits = ((bits & 0x33333333u) << 2u) | ((bits & 0xCCCCCCCCu) >> 2u);
    bits = ((bits & 0x0F0F0F0Fu) << 4u) | ((bits & 0xF0F0F0F0u) >> 4u);
    bits = ((bits & 0x00FF00FFu) << 8u) | ((bits & 0xFF00FF00u) >> 8u);
    return float(bits) * 2.3283064365386963e-10;
}

vec2 Hammersley(uint i, uint N)
{
    return vec2(float(i) / float(N), RadicalInverse_VdC(i));
}

vec3 ImportanceSampleGGX(vec2 Xi, vec3 N, float roughness)
{
    float a = roughness * roughness;
    float phi = 2.0 * PI * Xi.x;
    float cosTheta = sqrt((1.0 - Xi.y) / (1.0 + (a * a - 1.0) * Xi.y));
    float sinTheta = sqrt(1.0 - cosTheta * cosTheta);
    vec3 H = vec3(cos(phi) * sinTheta, sin(phi) * sinTheta, cosTheta);
    vec3 up = abs(N.z) < 0.999 ? vec3(0.0, 0.0, 1.0) : vec3(1.0, 0.0, 0.0);
    vec3 tangent = normalize(cross(up, N));
    vec3 bitangent = cross(N, tangent);
    return normalize(tangent * H.x + bitangent * H.y + N * H.z);
}
```

```glsl
const float PI = 3.14159265359;
const float c_SourceResolution = 512.0;
const uint c_SampleCount = 1024u;

uniform samplerCube environmentMap;
uniform float roughness;

void main()
{
    vec3 N = normalize(v_Direction);
    vec3 R = N;
    vec3 V = R;
    vec3 prefiltered = vec3(0.0);
    float totalWeight = 0.0;

    for (uint i = 0u; i < c_SampleCount; ++i)
    {
        vec2 Xi = Hammersley(i, c_SampleCount);
        vec3 H = ImportanceSampleGGX(Xi, N, roughness);
        vec3 L = normalize(2.0 * dot(V, H) * H - V);
        float nDotL = max(dot(N, L), 0.0);
        if (nDotL > 0.0)
        {
            float nDotH = max(dot(N, H), 0.0);
            float hDotV = max(dot(H, V), 0.0001);
            float D = DistributionGGX(N, H, roughness);
            float pdf = (D * nDotH / (4.0 * hDotV)) + 0.0001;
            float saTexel = 4.0 * PI / (6.0 * c_SourceResolution * c_SourceResolution);
            float saSample = 1.0 / (float(c_SampleCount) * pdf + 0.0001);
            float mip = roughness == 0.0 ? 0.0 : 0.5 * log2(saSample / saTexel);
            prefiltered += textureLod(environmentMap, L, mip).rgb * nDotL;
            totalWeight += nDotL;
        }
    }

    o_Color = vec4(totalWeight > 0.0 ? prefiltered / totalWeight : vec3(0.0), 1.0);
}
```

**Why:** The mip from the PDF is the chapter's fix for bright dots. `c_SourceResolution` is `SkyCaptureFaceSize`. The `H·V` floor avoids the division the chapter writes as a raw dot. A zero total weight stores black instead of a non-finite texel.

### Lookup

`brdfLut.vert` writes `gl_Position = vec4(a_Position.xy, 0.0, 1.0)` and passes `a_TexCoord`. `brdfLut.frag` uses the same Hammersley and `ImportanceSampleGGX` as the prefilter. Its geometry term is the indirect one, not the direct one:

```glsl
float GeometrySchlickGGX(float nDot, float roughness)
{
    float a = roughness;
    float k = (a * a) / 2.0;
    return nDot / (nDot * (1.0 - k) + k);
}
```

`GeometrySmith` is the product of that function on `N·V` and `N·L`. `IntegrateBRDF` is the 1024-sample loop:

```glsl
vec2 IntegrateBRDF(float nDotV, float roughness)
{
    vec3 V = vec3(sqrt(1.0 - nDotV * nDotV), 0.0, nDotV);
    float A = 0.0;
    float B = 0.0;
    vec3 N = vec3(0.0, 0.0, 1.0);
    const uint c_SampleCount = 1024u;
    for (uint i = 0u; i < c_SampleCount; ++i)
    {
        vec2 Xi = Hammersley(i, c_SampleCount);
        vec3 H = ImportanceSampleGGX(Xi, N, roughness);
        vec3 L = normalize(2.0 * dot(V, H) * H - V);
        float nDotL = max(L.z, 0.0);
        float nDotH = max(H.z, 0.0);
        float vDotH = max(dot(V, H), 0.0);
        if (nDotL > 0.0)
        {
            float G = GeometrySmith(N, V, L, roughness);
            float vis = (G * vDotH) / (nDotH * nDotV);
            float fc = pow(1.0 - vDotH, 5.0);
            A += (1.0 - fc) * vis;
            B += fc * vis;
        }
    }
    return vec2(A, B) / float(c_SampleCount);
}
```

The fragment writes:

```glsl
void main()
{
    vec2 integrated = IntegrateBRDF(v_TexCoord.x, v_TexCoord.y);
    o_Color = vec4(integrated, 0.0, 1.0);
}
```

**Why:** `k = roughness² / 2` is the split-sum geometry. The color shaders keep `(roughness + 1)² / 8` for the sun and the lamps. The attachment stores red and green; the extra components are dropped.

### Color

Add the samplers and the flag next to the ambient uniforms in `cube.frag` and `modelShader.frag`.

```glsl
uniform int u_Ibl;
uniform samplerCube u_Irradiance;
uniform samplerCube u_Prefilter;
uniform sampler2D u_BrdfLut;
```

Paste into both:

```glsl
vec3 FresnelSchlickRoughness(float cosTheta, vec3 F0, float roughness)
{
    return F0 + (max(vec3(1.0 - roughness), F0) - F0) * pow(clamp(1.0 - cosTheta, 0.0, 1.0), 5.0);
}

vec3 ImageBasedLight(vec3 N, vec3 V, vec3 albedo, float metallic, float roughness, float ao)
{
    vec3 F0 = mix(vec3(c_DielectricF0), albedo, metallic);
    float nDotV = max(dot(N, V), 0.0);
    vec3 F = FresnelSchlickRoughness(nDotV, F0, roughness);
    vec3 kD = (1.0 - F) * (1.0 - metallic);
    vec3 diffuse = texture(u_Irradiance, N).rgb * albedo;
    vec3 R = reflect(-V, N);
    vec3 prefiltered = textureLod(u_Prefilter, R, roughness * 4.0).rgb;
    vec2 brdf = texture(u_BrdfLut, vec2(nDotV, roughness)).rg;
    return (kD * diffuse + prefiltered * (F * brdf.x + brdf.y)) * ao;
}
```

Replace the ambient line in both `main` functions:

```glsl
vec3 ambient = u_Ibl != 0
    ? ImageBasedLight(norm, V, albedo, metallic, roughness, ao)
    : u_AmbientStrength * u_AmbientColor * albedo * ao;
o_Color = vec4(Encode(ambient + sun + lamps), u_Color.a);
```

`ao` is the value each shader already computed. Direct `FresnelSchlick` and `GeometrySmith` stay.

**Why:** The irradiance cubemap has no mips, so a plain sample is the diffuse-chapter fetch. The prefilter lod stops at 4 because five mips were written. `Encode` already wraps the sum.

## 7. Graphics3D

```csharp
private const int IrradianceSlot = 13;
private const int PrefilterSlot = 14;
private const int BrdfLutSlot = 15;

private uint _irradiance;
private uint _prefilter;
private uint _brdfLut;
```

Init binds the BRDF program and calls `TryCreateBrdfLut`. Failure leaves `_brdfLut` at 0.

```csharp
_brdfShader.Bind();
rendererApi.TryCreateBrdfLut(out _brdfLut);
_brdfShader.Unbind();
```

`SetSkybox` keeps the early-outs. A new path draws every pass before it keeps the ids. A failed draw deletes the three new cubemap ids. `Dispose` does not.

```csharp
public void SetSkybox(string? path)
{
    if (string.IsNullOrWhiteSpace(path))
    {
        ClearSky();
        return;
    }

    if (string.Equals(path, _skySource, StringComparison.Ordinal))
        return;

    var full = Path.IsPathRooted(path) ? Path.GetFullPath(path) : PathBuilder.Resolve(path);
    if (!rendererApi.TryCreateSkyCapture(full, out var environment, out var capture))
    {
        _skySource = path;
        return;
    }

    using (capture)
    {
        Span<Matrix4x4> faces = stackalloc Matrix4x4[LightingMath.PointShadowFaceCount];
        if (!LightingMath.TryBuildPointShadowFaces(Vector3.Zero, LightingMath.SkyCaptureFar, faces)
            || !DrawEnvironment(capture, environment, faces)
            || !capture.GenerateEnvironmentMips()
            || !DrawIrradiance(capture, environment, faces)
            || !DrawPrefilter(capture, faces))
        {
            rendererApi.DeleteTexture(environment);
            rendererApi.DeleteTexture(capture.IrradianceId);
            rendererApi.DeleteTexture(capture.PrefilterId);
            _skySource = path;
            return;
        }

        ClearSky();
        _skyCubemap = environment;
        _irradiance = capture.IrradianceId;
        _prefilter = capture.PrefilterId;
        _skySource = path;
    }
}
```

`DrawEnvironment` binds the equirect on unit 0 and loops `Begin(environment, face, 0, SkyCaptureFaceSize)`. `DrawIrradiance` binds the irradiance program and the environment cubemap, sets scale 1, and loops `Begin(capture.IrradianceId, face, 0, IrradianceFaceSize)`. `DrawPrefilter` binds the prefilter program and the environment cubemap, and for each mip sets `u_Roughness` to `mip / (PrefilterMipCount - 1)` before `Begin(capture.PrefilterId, face, mip, PrefilterFaceSize >> mip)`. Any `Begin` failure returns false. Scale stays 1. Culling stays off for these draws and is restored afterwards, as the sky capture already does.

`ClearSky` unbinds units 13 and 14, deletes the three cubemaps, and clears the source string. It does not delete `_brdfLut`. `Dispose` calls `ClearSky` and then deletes the lookup.

`UploadFrame` writes the flag and the binds for whichever color shader it was given. `DrawCube` and `DrawMesh` do not.

```csharp
var ibl = _irradiance != 0 && _prefilter != 0 && _brdfLut != 0;
shader.SetInt("u_Ibl", ibl ? 1 : 0);
if (ibl)
{
    rendererApi.BindTextureCube(_irradiance, IrradianceSlot);
    rendererApi.BindTextureCube(_prefilter, PrefilterSlot);
    rendererApi.BindTexture2D(_brdfLut, BrdfLutSlot);
}
```

`DrawSkybox` and `SceneRenderPipeline` stay on the same calls. The viewport and `GameLayer` stay on the same `SetSkybox` calls.

**Why:** `BeginScene` already uploads both color programs before the first mesh. That is the shadow bind's home, so it is the indirect bind's home. `ClearSky` before assigning the new ids drops the previous trio only after the new one is complete.

## 8. Editor and publish

The browse call in `SceneSettingsPopup` uses `AssetKind.BuildOsFilter("HDR environment", [".hdr"])`. `AssetKind.Texture` stays png and jpg.

In `PublishedAssetValidator.CollectMissingPaths`, after a `Skybox` string has been found on disk:

```csharp
else if (prop.Name == "Skybox"
         && !path.EndsWith(".hdr", StringComparison.OrdinalIgnoreCase))
{
    missing.Add(
        $"{path} (from {RelativeToAssets(assetsRoot, sourceFile)}, Skybox must be a .hdr file)");
}
```

**Why:** A png under `assets/` exists, so the missing-file check would let it through. The extension check is the publish gate for the format. The content browser keys off `AssetKind.Texture` and must not start decoding radiance files as RGBA8.

## 9. Tests

`ResolveAmbient` and the scene serializer stay as they are.

`PublishedAssetValidatorTests`:

- An existing `assets/sky/day.hdr` referenced by `Skybox` succeeds.
- An existing `assets/sky/day.png` referenced by `Skybox` fails and the message says `.hdr`.
- The empty-string case still succeeds. The missing-file case still fails.

`ShaderFactoryCacheTests` gains a compile fact for `irradiance.frag`, `prefilter.frag`, and `brdfLut.frag`, same shape as the point-depth fact.

`Graphics3DFrameUniformTests.BeginScene_UploadsViewAndLights_DrawCubeDoesNotRepeatThem` also expects `u_Ibl` set to 0, and expects no bind on slots 13, 14, or 15. Every `Init` fake in that file returns an `IShader` for `ShaderId.Irradiance`, `Prefilter`, and `BrdfLut`.

A new `Graphics3D` fact with a fake `IRendererAPI`: the same path does not call `TryCreateSkyCapture` twice; a failed create leaves the previous environment id in place and a second frame with that failed string does not call `TryCreateSkyCapture` again.

`Engine.GraphicsTests`: one headless draw. Camera on `+Z` looking at the origin, unit cube, metallic 1, roughness 0, ao 1, directional color black, no point lights. The fixture is a 2:1 `.hdr` whose captured `+Z` face is the constant 4 and whose other faces are 0. The camera sits on `+Z` looking at the origin, so the visible face reflects along `+Z` and samples that face. Read the cube pixel with `GlFramebufferCapture`. Its max channel is at least 20 above the max channel of the same cube after `SetSkybox` with an empty path.

**Why:** The publish cases lock the extension. The pixel case is the metal-in-shadow result: a black sun plus a bright face must beat the flat fill of strength 0.1. The fake-renderer case locks the "remember a failed path" rule without a GPU.
