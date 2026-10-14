# SSAO — Implementation

C# for the design in `introduction.md` and `developer-guide.md`. No new `ISystem`. `SceneRenderSystem` stays at priority 150 and remains the runtime caller. OpenGL calls stay in `Platform/`.

Constants: kernel **64**, bias **0.025**, noise **4×4**, blur footprint **4×4**, SSAO unit **16** when the device has 17 fragment samplers and **15** otherwise, radius default **0.5**, strength default **1**, flag default **false**, random seed **1**.

## 1. Kernel

`LightingMath` already owns lighting constants. The kernel is a pure function so the unit test can call it without GL. Seed 1 keeps the image stable between runs.

```csharp
public const int SsaoKernelSize = 64;
public const float SsaoBias = 0.025f;

public static Vector3[] CreateSsaoKernel()
{
    var random = new Random(1);
    var kernel = new Vector3[SsaoKernelSize];
    for (var i = 0; i < SsaoKernelSize; i++)
    {
        var sample = Vector3.Normalize(new Vector3(
            (float)random.NextDouble() * 2f - 1f,
            (float)random.NextDouble() * 2f - 1f,
            (float)random.NextDouble()));
        sample *= (float)random.NextDouble();
        var t = i / (float)SsaoKernelSize;
        var scale = 0.1f + t * t * 0.9f;
        kernel[i] = sample * scale;
    }
    return kernel;
}
```

`CreateSsaoNoise()` uses `Random(2)`, separate from the kernel, and returns 16 `Vector4` values. xy is in −1…1, z and w are 0. Pack each channel with `(byte)Math.Round((channel * 0.5f + 0.5f) * 255f)` and upload through `ITextureFactory.CreateFromRgba`. That path is already RGBA8, repeat, and linear.

**Why:** The scale clusters samples at the surface. A fixed seed is the reproducible version of the chapter's random kernel.

## 2. Camera, view, serialization

```csharp
public bool Ssao { get; set; }
public float SsaoRadius { get; set; } = 0.5f;
public float SsaoStrength { get; set; } = 1f;
```

Copy all three in `Clone()`. `ComponentSerializerRegistry` already registers `CameraComponent`, so there is no new serializer. Missing JSON keys keep the property initializers.

`SceneView` gains optional fields with defaults, so existing `new SceneView(viewProjection)` call sites stay valid:

```csharp
Matrix4x4 View = default,
Matrix4x4 Projection = default,
uint TargetWidth = 0,
uint TargetHeight = 0,
bool Ssao = false,
float SsaoRadius = 0.5f,
float SsaoStrength = 1f
```

`CameraViews.TryFrom` writes `View` and `Projection` from the matrices it already multiplies into `ViewProjection`. `TryGetPrimaryView` then copies the three knobs off that same component.

```csharp
internal static bool TryGetPrimaryCamera(Context context, out CameraComponent camera)
{
    foreach (var (_, component) in context.View<CameraComponent>())
    {
        if (!component.Primary)
            continue;
        camera = component;
        return true;
    }
    camera = null!;
    return false;
}
```

`TryGetPrimaryView` starts from that helper so edit mode and play cannot pick different primaries.

`CameraComponentEditor` draws "SSAO", "SSAO Radius", and "SSAO Strength" for every camera, perspective and orthographic. Only the primary camera is read when the frame runs.

## 3. Target size

```csharp
void SetSceneTargetSize(uint width, uint height);
uint SceneTargetWidth { get; }
uint SceneTargetHeight { get; }
```

`EditorViewport.RenderSceneToFramebuffer` calls `SetSceneTargetSize` with the framebuffer spec before the edit/play switch. `GameLayer.OnUpdate` calls it with the width and height it passes to `DrawScene`, before `OnUpdateRuntime`.

`SceneRenderSystem` copies those two fields onto the view after `TryGetPrimaryView`. Edit mode writes them on the `SceneView` it builds itself, and copies `Ssao`, `SsaoRadius`, and `SsaoStrength` from `TryGetPrimaryCamera`. No primary means the flag stays false.

## 4. Unit limit

No new framebuffer format. The geometry color is the existing `RGBA16F`. Its depth is the existing `Depth` attachment, not `DepthComponent`. The two occlusion targets are the existing `RGBA8`. The factor is the red channel.

```csharp
int MaxFragmentTextureImageUnits { get; }
```

The OpenGL renderer reads `GL_MAX_TEXTURE_IMAGE_UNITS` once during `Init` and stores it.

## 5. Pass

`SsaoPass` is a singleton next to `TonemapPass`. It owns the geometry framebuffer, the two `RGBA8` framebuffers, the noise texture, the kernel, and the triangle from `fxaa.vert`.

```csharp
public bool TryOcclude(
    uint width,
    uint height,
    Matrix4x4 projection,
    float radius,
    Action drawOpaque,
    out uint occlusionTexture)
```

`Available` is false after a failed init. Init creates `ShaderId.Ssao` and `ShaderId.SsaoBlur`, refuses when `MaxFragmentTextureImageUnits < 16`, uploads the kernel with `SetFloat3($"u_Samples[{i}]", ...)`, and uploads the noise. Any exception logs once and leaves `Available` false.

`TryOcclude` returns false, without logging, when the size is 0. It returns false and logs once when `Matrix4x4.Invert` fails or when `Ensure` throws. On success:

1. Bind the geometry target, set the clear color to transparent black, enable the depth test and depth writes, and clear. The default clear depth is 1, and the pass does not change it. Then `drawOpaque()` runs inside `try/finally`, and the target is unbound.
2. Bind the raw occlusion target. Bind normal to unit 0, depth to unit 1, noise to unit 2. Set `u_InverseProjection`, `u_Projection`, `u_Radius`, `u_Bias`, and `u_NoiseScale = (width / 4, height / 4)`. The noise vector is `texture(u_Noise, uv).rgb * 2.0 - 1.0`. Draw 3 vertices. Unbind in `finally`.
3. Bind the blur target, sample the raw map on unit 0, draw 3 vertices, unbind in `finally`.
4. Set `occlusionTexture` to the blur color attachment.

Targets are recreated when the size changes. A failed recreate disposes the new attempt, leaves the previous targets unused for that frame, and returns false.

**Why:** The pipeline still decides which meshes move. The pass owns every framebuffer the kernel reads, so a sprite depth cannot get in.

## 6. Normal draw on Graphics3D

`BeginNormalPass(Matrix4x4 view, Matrix4x4 viewProjection)` sets a flag, the view, and the view-projection. `EndNormalPass` clears the flag. The flag must not be set during a shadow pass.

`DrawCube` and `DrawMeshInstances` grow a branch beside `_shadowPass`. The cube branch binds `ShaderId.ViewNormal`. The mesh branch binds `ShaderId.ViewNormalModel`, reuses the instanced attributes, and calls the same `ApplySurface` alpha-test path as the depth shader. Both set `u_View` and `u_ViewProjection`. Neither evaluates lights.

```glsl
vec3 worldNormal = normalize(a_Normal * mat3(normalMat));
vec3 viewNormal = normalize(worldNormal * mat3(u_View));
```

`viewNormal.frag` writes `vec4(viewNormal, 0.0)` and discards on the same alpha test as `depth.frag`.

`SetSsao(uint textureId, float strength)` stores the bind for `UploadFrame`. The lighting shaders set `u_SsaoStrength` there. When `MaxFragmentTextureImageUnits` is at least 17, SSAO binds on unit 16 and the BRDF LUT stays on 15. At 16 units SSAO binds on 15 and the LUT sampler is not compiled in.

```csharp
private const int BrdfLutSlot = 15;
private const int SsaoSlotWithLut = 16;
private const int SsaoSlotWithoutLut = 15;
```

At init:

```csharp
_cubeShader.SetInt("u_Ssao", SsaoSlot);
_modelShader.SetInt("u_Ssao", SsaoSlot);
```

In both fragment shaders, after the existing ambient line:

```glsl
float ssao = texture(u_Ssao, gl_FragCoord.xy / vec2(textureSize(u_Ssao, 0))).r;
ambient *= mix(1.0, ssao, u_SsaoStrength);
```

The blur shader reads and writes that same red channel. Green, blue, and alpha are unused.

## 7. Pipeline

`RenderScene` takes an optional `SsaoPass? ssao = null` so current tests and the benchmark keep compiling. Null is the skipped path.

After point shadows and before `BeginScene`:

```csharp
var strength = float.IsFinite(view.SsaoStrength) ? Math.Clamp(view.SsaoStrength, 0f, 1f) : 0f;
var run = ssao is { Available: true }
    && view.Ssao
    && view.TargetWidth > 0 && view.TargetHeight > 0
    && float.IsFinite(view.SsaoRadius) && view.SsaoRadius > 0f
    && strength > 0f
    && ssao.TryOcclude(view.TargetWidth, view.TargetHeight, view.Projection, view.SsaoRadius,
        () =>
        {
            graphics3D.BeginNormalPass(view.View, view.ViewProjection);
            try
            {
                DrawOpaque3D(context, graphics3D, textureFactory, modelFactory, view.ViewProjection);
            }
            finally
            {
                graphics3D.EndNormalPass();
            }
        },
        out var occlusion);

if (run)
    graphics3D.SetSsao(occlusion, strength);
else
    graphics3D.SetSsao(textureFactory.GetWhiteTexture().GetRendererId(), 0f);
```

`DrawOpaque3D` for the normal pass uses the camera `ViewProjection`, not the light matrix. Visibility, zones, and instancing stay as they are for the color pass. The caster-distance cut is a shadow rule and stays off here.

## 8. Registration and call sites

```csharp
container.Register<SsaoPass>(Reuse.Singleton);
```

`SceneSystemsFactory` takes `SsaoPass` and passes it into `SceneRenderSystem`. `EditorViewport` takes it and passes it to `RenderScene`. `PipelineBenchmarkRun` can keep the default null.

Shader paths:

| ShaderId | Vertex | Fragment |
|----------|--------|----------|
| `ViewNormal` | `viewNormal.vert` | `viewNormal.frag` |
| `ViewNormalModel` | `viewNormalModel.vert` | `viewNormal.frag` |
| `Ssao` | `fxaa.vert` | `ssao.frag` |
| `SsaoBlur` | `fxaa.vert` | `ssaoBlur.frag` |

`ssao.frag` is the chapter's loop with the reconstruction and the left-multiply from the developer guide. `ssaoBlur.frag` is the 4×4 average. Both write a single float.

## 9. Tests

**`CameraComponentTests`.** Defaults are off, radius 0.5, strength 1. `Clone()` copies a non-default triple.

**Scene serializer**, same shape as `SceneSerializerSkyboxTests`. A camera saved with the triple loads it back. A scene file that omits the three keys loads the defaults.

**`SsaoKernelTests`.** 64 samples, each `Z >= 0`. The scale `0.1 + (i/64)² * 0.9` is non-decreasing in `i`.

**`SsaoPassTests`**, NSubstitute, same style as `TonemapPassTests`.

- Shader create throws, or the unit limit is 15: `Available` is false and `DrawArrays` is never called.
- Size 0 does not draw.
- A live pass at 32×24 creates one geometry target (`RGBA16F` plus `Depth`) and two `RGBA8` targets at that size, and draws the triangle twice.
- The second call does not create the shaders again.
- `Ensure` throwing on resize does not draw.

**`SceneRenderPipeline` tests.** Flag off, strength 0, or a non-positive radius: `BeginNormalPass` is not called. Flag on, 32×24, radius 0.5, strength 1, and a pass that returns true: `BeginNormalPass` runs before `BeginScene`, and `SetSsao` receives that texture and the strength. A null pass binds the white texture with strength 0.

**`SsaoTests` in `Engine.GraphicsTests`.** Two unit cubes share the face at x = 1. Ambient only, no sky. Project the shared-edge midpoint and a point on the outer face with the test's view-projection, and read those texels. The crease texel with the flag on is at most 95% of its luminance with the flag off. Repeat with ambient strength 0 and a non-black sun: the outer-face luminance with the flag on stays between 98% and 102% of the flag-off value.
