# Directional Shadows — Implementation

Step-by-step code for the design in `introduction.md` and `developer-guide.md`. Paste the C# into the existing types. The GLSL is the shader half of the same change. Do not add a shared shader include.

The map is **1024**. The bias is **0.002** in window depth. The depth texture is bound on **unit 3**. A disabled shadow contributes a factor of **1**.

## 1. Fit

Extend `LightingMath` with the map size and the fit. The orthographic matrix is written by hand so it covers corners behind the eye at the world origin. `CreateOrthographicOffCenter` only sees in front of the eye and will clip those corners.

`System.Numerics` perspective stores near as clip Z **0** and far as clip Z **1**. This process never calls `glClipControl`, so the depth image stores window depth `ndcZ * 0.5 + 0.5`. The fit's Z mapping uses the same 0-at-near, 1-at-far clip range. The shaders convert to window depth.

```csharp
public const int ShadowMapResolution = 1024;
private const float ShadowExtentEpsilon = 1e-4f;
private const float ShadowUpParallel = 0.99f;

public static bool TryFitDirectionalShadow(
    Matrix4x4 cameraViewProjection,
    Vector3 lightDirection,
    out Matrix4x4 lightViewProjection)
{
    lightViewProjection = Matrix4x4.Identity;
    if (!Matrix4x4.Invert(cameraViewProjection, out var inverseViewProjection))
        return false;

    Span<Vector3> corners = stackalloc Vector3[8];
    var corner = 0;
    for (var z = 0f; z <= 1f; z += 1f)
    for (var y = -1f; y <= 1f; y += 2f)
    for (var x = -1f; x <= 1f; x += 2f)
    {
        if (!TryUnproject(inverseViewProjection, x, y, z, out corners[corner]))
            return false;
        corner++;
    }

    var direction = NormalizeDirection(lightDirection);
    var up = MathF.Abs(Vector3.Dot(direction, Vector3.UnitY)) > ShadowUpParallel
        ? Vector3.UnitZ
        : Vector3.UnitY;
    var lightView = Matrix4x4.CreateLookAt(Vector3.Zero, direction, up);

    var min = new Vector3(float.PositiveInfinity);
    var max = new Vector3(float.NegativeInfinity);
    foreach (var world in corners)
    {
        var lightSpace = Vector3.Transform(world, lightView);
        min = Vector3.Min(min, lightSpace);
        max = Vector3.Max(max, lightSpace);
    }

    if (!HasShadowArea(min.X, max.X, min.Y, max.Y))
        return false;

    SnapAxis(ref min.X, ref max.X);
    SnapAxis(ref min.Y, ref max.Y);

    lightViewProjection = lightView * BuildLightOrtho(min, max);
    return true;
}

internal static bool HasShadowArea(float minX, float maxX, float minY, float maxY) =>
    maxX - minX > ShadowExtentEpsilon && maxY - minY > ShadowExtentEpsilon;

private static void SnapAxis(ref float min, ref float max)
{
    var size = max - min;
    var texel = size / ShadowMapResolution;
    var center = MathF.Floor(((min + max) * 0.5f) / texel) * texel;
    var half = size * 0.5f;
    min = center - half;
    max = center + half;
}

private static Matrix4x4 BuildLightOrtho(Vector3 min, Vector3 max)
{
    var ortho = Matrix4x4.Identity;
    ortho.M11 = 2f / (max.X - min.X);
    ortho.M22 = 2f / (max.Y - min.Y);
    ortho.M33 = 1f / (min.Z - max.Z);
    ortho.M41 = -1f - min.X * ortho.M11;
    ortho.M42 = -1f - min.Y * ortho.M22;
    ortho.M43 = -max.Z * ortho.M33;
    return ortho;
}

private static bool TryUnproject(Matrix4x4 inverseViewProjection, float x, float y, float z, out Vector3 world)
{
    var clip = Vector4.Transform(new Vector4(x, y, z, 1f), inverseViewProjection);
    if (MathF.Abs(clip.W) < ShadowExtentEpsilon)
    {
        world = default;
        return false;
    }

    world = new Vector3(clip.X, clip.Y, clip.Z) / clip.W;
    return true;
}
```

**Why:** `M33` and `M43` map the larger light-space Z (closer to the light, even when that point is behind the origin eye) to clip Z 0, and the smaller Z to clip Z 1. Snap runs on X and Y only. `HasShadowArea` is internal so the zero-area case has a direct test; a real camera frustum does not collapse.

## 2. Depth shader

`Engine/Engine.Shaders.props` already copies every file under `assets/shaders/OpenGL`. Add the pair there. Also add the same two `Content` entries to `tests/Engine.GraphicsTests/Engine.GraphicsTests.csproj` next to the cube shaders, or a GPU run from that project cannot load them.

`Engine/assets/shaders/OpenGL/depth.vert`:

```glsl
#version 330 core

layout(location = 0) in vec3 a_Position;

uniform mat4 u_ViewProjection;
uniform mat4 u_Model;

void main()
{
    vec4 worldPos = vec4(a_Position, 1.0) * u_Model;
    gl_Position = worldPos * u_ViewProjection;
}
```

`Engine/assets/shaders/OpenGL/depth.frag`:

```glsl
#version 330 core

void main()
{
}
```

Register the id and the file name.

```csharp
public enum ShaderId
{
    Texture,
    Line,
    Cube,
    Model,
    Depth
}
```

```csharp
ShaderId.Depth => "depth",
```

Add one `[InlineData(ShaderId.Depth, "depth")]` to `EngineShaderPathsTests`.

**Why:** Cube and model both put position at location 0. An empty fragment shader still writes depth from `gl_Position`.

## 3. Graphics interface

On `IGraphics3D`:

```csharp
void BeginShadowPass(Matrix4x4 lightViewProjection);
void EndShadowPass();
void SetDirectionalShadow(Matrix4x4 lightViewProjection, bool enabled);
```

`SetDirectionalShadow` only stores data for the next color scene. It does not draw.

## 4. Depth image and passes

Add `IFrameBufferFactory` as the fifth constructor argument of `Graphics3D`. DryIoc already registers that factory. Update `Graphics3DFrameUniformTests` and `Graphics3DDisposeTests` to pass `Substitute.For<IFrameBufferFactory>()` and to return a shader for `ShaderId.Depth`.

```csharp
private const int ShadowMapSlot = 3;

private IShader _depthShader = null!;
private IFrameBuffer? _shadowMap;
private bool _shadowPass;
private Matrix4x4 _lightViewProjection = Matrix4x4.Identity;
private bool _shadowsEnabled;
```

In `Init`, after the model shader:

```csharp
_depthShader = shaderFactory.Create(ShaderId.Depth);
_cubeShader.Bind();
_cubeShader.SetInt("u_ShadowMap", ShadowMapSlot);
_cubeShader.Unbind();
_modelShader.Bind();
_modelShader.SetInt("u_ShadowMap", ShadowMapSlot);
_modelShader.Unbind();
```

The model shader's existing sampler binds stay. Set the shadow sampler on it in that same bind, or bind it twice. Either way unit 3 is the depth image.

```csharp
public void SetDirectionalShadow(Matrix4x4 lightViewProjection, bool enabled)
{
    _lightViewProjection = lightViewProjection;
    _shadowsEnabled = enabled;
}

public void BeginShadowPass(Matrix4x4 lightViewProjection)
{
    _shadowPass = true;
    _lightViewProjection = lightViewProjection;
    var map = ShadowMap();
    map.Bind();
    rendererApi.SetDepthTest(true);
    rendererApi.SetDepthWrite(true);
    rendererApi.Clear();
    _depthShader.Bind();
    _depthShader.SetMat4(ViewProjectionUniform, lightViewProjection);
}

public void EndShadowPass()
{
    _depthShader.Unbind();
    _shadowMap?.Unbind();
    _shadowPass = false;
}

private IFrameBuffer ShadowMap()
{
    if (_shadowMap != null)
        return _shadowMap;

    var size = (uint)LightingMath.ShadowMapResolution;
    var spec = new FrameBufferSpecification(size, size)
    {
        AttachmentsSpec = new FrameBufferAttachmentSpecification([
            new FrameBufferTextureSpecification(FrameBufferTextureFormat.DepthComponent)
            {
                Filter = FrameBufferTextureFilter.Nearest,
                Wrap = FrameBufferTextureWrap.ClampToBorder
            }
        ])
    };
    _shadowMap = frameBuffers.Create(spec);
    return _shadowMap;
}
```

`Bind` on the framebuffer already sets the viewport to 1024 and saves the previous viewport and framebuffer. `Unbind` restores both. Do not call `SetViewport` here.

`Clear` clears color and depth. This target has no color attachment, so the color bit does nothing. The default clear depth is 1, which matches the far border.

In `Dispose`, dispose `_shadowMap` before dropping the shader references. The factory does not own this buffer.

At the start of `DrawCube` and `DrawMesh`:

```csharp
if (_shadowPass)
{
    DrawShadow(meshOrCube, transform);
    return;
}
```

`DrawCube` passes `_cubeMesh`. `DrawMesh` passes the mesh argument. Neither binds a texture in this branch.

```csharp
private void DrawShadow(Mesh mesh, Matrix4x4 transform)
{
    rendererApi.SetDepthTest(true);
    _depthShader.SetMat4("u_Model", transform);
    mesh.Bind();
    rendererApi.DrawIndexed(mesh.GetVertexArray(), (uint)mesh.GetIndexCount());
    _stats.DrawCalls++;
}
```

The depth shader stays bound from `BeginShadowPass` until `EndShadowPass`.

In `UploadFrame`, after the point-light loop:

```csharp
shader.SetMat4("u_LightViewProjection", _lightViewProjection);
shader.SetInt("u_ShadowsEnabled", _shadowsEnabled ? 1 : 0);
if (_shadowsEnabled && _shadowMap != null)
    rendererApi.BindTexture2D(_shadowMap.GetDepthAttachmentRendererId(), ShadowMapSlot);
```

**Why:** Unit 3 survives the color draws, which rebind units 0 through 2. The flag is what the shader trusts. Binding is skipped when the pass never created a map.

## 5. One walk, two calls

In `SceneRenderPipeline`, keep a latch for the warning:

```csharp
private static bool _shadowFitWarned;
```

Move the existing `foreach` over `ModelRendererComponent` into:

```csharp
private static void DrawOpaque3D(
    Context context,
    IGraphics3D graphics3D,
    ITextureFactory textureFactory,
    IModelFactory? modelFactory)
```

Leave the body of that loop unchanged, including `DrawCubeWithTexture` and the fallback cube.

`Render3D` becomes:

```csharp
var (ambientColor, ambientStrength) = ResolveAmbient(context);
graphics3D.SetAmbientLight(ambientColor, ambientStrength);

var (lightDirection, lightColor) = ResolveDirectional(context);
graphics3D.SetDirectionalLight(lightDirection, lightColor);

var pointCount = ResolvePointLights(context, PointLightBuffer);
graphics3D.SetPointLights(PointLightBuffer.AsSpan(0, pointCount));

graphics3D.SetDirectionalShadow(Matrix4x4.Identity, false);
if (lightColor != Vector3.Zero &&
    LightingMath.TryFitDirectionalShadow(view.ViewProjection, lightDirection, out var lightViewProjection))
{
    graphics3D.BeginShadowPass(lightViewProjection);
    DrawOpaque3D(context, graphics3D, textureFactory, modelFactory);
    graphics3D.EndShadowPass();
    graphics3D.SetDirectionalShadow(lightViewProjection, true);
}
else if (lightColor != Vector3.Zero && !_shadowFitWarned)
{
    _shadowFitWarned = true;
    Logger.Warning("Directional shadow fit failed; drawing the frame without directional shadows");
}

graphics3D.BeginScene(view);
DrawOpaque3D(context, graphics3D, textureFactory, modelFactory);
graphics3D.EndScene();
```

**Why:** The flag is cleared at the start of every 3D pass, then set only after a successful depth pass. A black directional color does not warn. A failed fit warns once.

## 6. Fragment shaders

Add the same uniforms and function to `cube.frag` and `modelShader.frag`. Do not add them to the 2D or line shaders.

```glsl
uniform mat4 u_LightViewProjection;
uniform sampler2D u_ShadowMap;
uniform int u_ShadowsEnabled;

const float c_ShadowBias = 0.002;

float DirectionalShadow(vec3 fragPos)
{
    if (u_ShadowsEnabled == 0)
        return 1.0;

    vec4 clipPos = vec4(fragPos, 1.0) * u_LightViewProjection;
    vec3 ndc = clipPos.xyz / clipPos.w;
    vec2 uv = ndc.xy * 0.5 + 0.5;
    float current = ndc.z * 0.5 + 0.5;
    float closest = texture(u_ShadowMap, uv).r;
    return current - c_ShadowBias > closest ? 0.0 : 1.0;
}
```

Cube, replacing the final color assignment:

```glsl
float shadow = DirectionalShadow(v_FragPos);
o_Color = vec4((ambient + diffuse * shadow) * albedo + points, baseColor.a);
```

Model, replacing the final color assignment:

```glsl
float shadow = DirectionalShadow(v_FragPos);
o_Color = vec4(ambient + (diffuse + specular) * shadow + points, u_Color.a);
```

**Why:** Ambient stays outside the factor. On the cube, diffuse is scaled before the albedo multiply that it already shares with ambient. On the model, diffuse and specular are the whole directional term. `current - bias` pushes the surface toward the light so it does not shadow itself. Border depth 1 loses to that test, so texels outside the map stay lit.

If a surface stripes itself, raise `c_ShadowBias` in both files together. If the shadow detaches from the contact point, lower it. Grazing angles can do either. That is the ceiling of a constant bias.

## 7. Tests

Fit tests next to `LightingMathTests`:

```csharp
[Fact]
public void TryFitDirectionalShadow_Perspective_ContainsFrustumCorners()
{
    var viewProjection = ViewProjection(new Vector3(0f, 2f, 5f));

    LightingMath.TryFitDirectionalShadow(viewProjection, new Vector3(0f, -1f, 0f), out var light)
        .ShouldBeTrue();

    foreach (var z in new[] { 0f, 1f })
    foreach (var y in new[] { -1f, 1f })
    foreach (var x in new[] { -1f, 1f })
    {
        Matrix4x4.Invert(viewProjection, out var inverse);
        var world = Vector4.Transform(new Vector4(x, y, z, 1f), inverse);
        world /= world.W;
        var clip = Vector4.Transform(world, light);
        clip /= clip.W;
        clip.X.ShouldBeInRange(-1.01f, 1.01f);
        clip.Y.ShouldBeInRange(-1.01f, 1.01f);
        clip.Z.ShouldBeInRange(-0.01f, 1.01f);
    }
}

[Fact]
public void TryFitDirectionalShadow_SubTexelTranslation_KeepsMatrix()
{
    var first = ViewProjection(new Vector3(0f, 2f, 5f));
    var second = ViewProjection(new Vector3(0.0001f, 2f, 5f));

    LightingMath.TryFitDirectionalShadow(first, new Vector3(0f, -1f, 0f), out var a).ShouldBeTrue();
    LightingMath.TryFitDirectionalShadow(second, new Vector3(0f, -1f, 0f), out var b).ShouldBeTrue();

    a.M11.ShouldBe(b.M11, 1e-4f);
    a.M22.ShouldBe(b.M22, 1e-4f);
    a.M41.ShouldBe(b.M41, 1e-4f);
    a.M42.ShouldBe(b.M42, 1e-4f);
}

[Fact]
public void TryFitDirectionalShadow_StraightDown_Succeeds()
{
    LightingMath.TryFitDirectionalShadow(
        ViewProjection(new Vector3(0f, 2f, 5f)),
        new Vector3(0f, -1f, 0f),
        out _).ShouldBeTrue();
}

[Fact]
public void TryFitDirectionalShadow_ZeroMatrix_ReturnsFalse()
{
    LightingMath.TryFitDirectionalShadow(default, new Vector3(0f, -1f, 0f), out var light)
        .ShouldBeFalse();
    light.ShouldBe(Matrix4x4.Identity);
}

[Fact]
public void HasShadowArea_ZeroExtent_IsFalse()
{
    LightingMath.HasShadowArea(0f, 0f, -1f, 1f).ShouldBeFalse();
    LightingMath.HasShadowArea(-1f, 1f, 0f, 1f).ShouldBeTrue();
}

private static Matrix4x4 ViewProjection(Vector3 eye)
{
    var forward = Vector3.Normalize(new Vector3(0f, -0.3f, -1f));
    var view = Matrix4x4.CreateLookAt(eye, eye + forward, Vector3.UnitY);
    var projection = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 3f, 16f / 9f, 0.1f, 100f);
    return view * projection;
}
```

Pipeline tests call `RenderScene` with substitutes. Use a perspective `SceneView`, an entity with `TransformComponent` and `ModelRendererComponent`, and `IGraphics2D` plus `IGraphics3D` substitutes. `IModelFactory.Create` returns null when the path should fail.

- No directional component: `BeginShadowPass` is not called. `BeginScene` is called. `SetDirectionalShadow` is called with `false`.
- Directional color white and a cube with an empty model path: `BeginShadowPass` once, `DrawCube` twice, `EndShadowPass` once, then `SetDirectionalShadow` with `true` and the matrix `TryFitDirectionalShadow` produced for that same view.
- `ModelPath` set and `Create` returning null: `DrawCube` twice, `DrawMesh` never.

Frame-uniform test, after `SetDirectionalShadow(light, true)` and `BeginScene`:

- Both shaders receive `u_LightViewProjection` and `u_ShadowsEnabled` of 1.
- `DrawCube` does not set `u_LightViewProjection` again.

A separate test calls `BeginShadowPass` with a substitute framebuffer whose `Bind` and `Unbind` are observable, and a depth shader distinct from the cube shader. The draw uses the depth shader's `SetMat4("u_Model", ...)` and does not call `SetMat4` on the cube shader.

**Why:** The fit is pure. The pipeline test locks the double walk. The uniform test locks "once per color scene." None of them need a window.
