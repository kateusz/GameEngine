# Point Shadows — Implementation

Step-by-step code for the design in `introduction.md` and `developer-guide.md`. Paste the C# into the existing types. The GLSL is the shader half of the same change. Do not add a shared shader include.

Face size is **512**. Near plane is **0.1**. World bias is **0.05**. Cubemaps bind on units **4** through **11**. A disabled point shadow contributes a factor of **1**. The directional map stays on unit **3**.

## 1. Face matrices

Extend `LightingMath`. `CreatePerspectiveFieldOfView` throws when the far plane is not greater than the near plane, so the range check runs first.

```csharp
public const int PointShadowFaceResolution = 512;
public const float PointShadowNear = 0.1f;
public const int PointShadowFaceCount = 6;

private static readonly Vector3[] PointShadowDirections =
[
    Vector3.UnitX, -Vector3.UnitX,
    Vector3.UnitY, -Vector3.UnitY,
    Vector3.UnitZ, -Vector3.UnitZ
];

private static readonly Vector3[] PointShadowUps =
[
    -Vector3.UnitY, -Vector3.UnitY,
    Vector3.UnitZ, -Vector3.UnitZ,
    -Vector3.UnitY, -Vector3.UnitY
];

public static bool TryBuildPointShadowFaces(Vector3 position, float range, Span<Matrix4x4> faces)
{
    if (faces.Length < PointShadowFaceCount || range <= PointShadowNear)
        return false;

    var projection = Matrix4x4.CreatePerspectiveFieldOfView(
        MathF.PI / 2f, 1f, PointShadowNear, range);
    for (var i = 0; i < PointShadowFaceCount; i++)
    {
        var view = Matrix4x4.CreateLookAt(position, position + PointShadowDirections[i], PointShadowUps[i]);
        faces[i] = view * projection;
    }

    return true;
}

internal static bool PointShadowFaceContains(Matrix4x4 face, Vector3 world)
{
    var clip = Vector4.Transform(new Vector4(world, 1f), face);
    if (MathF.Abs(clip.W) < ShadowExtentEpsilon)
        return false;

    var ndc = new Vector3(clip.X, clip.Y, clip.Z) / clip.W;
    return ndc.X is >= -1f and <= 1f
        && ndc.Y is >= -1f and <= 1f
        && ndc.Z is >= 0f and <= 1f;
}
```

**Why:** `view * projection` matches the camera matrix and the row-vector multiply in the depth shaders. The up vectors are the cubemap orientation OpenGL sampling expects. `PointShadowFaceContains` uses the same clip divide as the directional fit so the face test does not invent a second convention.

## 2. Complete the cubemap framebuffer

`AttachShadowDepthCubemap` already allocates six faces, nearest filtering, and clamp-to-edge. Replace the layered attach at the end of that method with a single face. Creation runs the completeness check immediately afterwards, and a layered cubemap on a non-layered framebuffer fails it.

```csharp
gl.FramebufferTexture2D(
    FramebufferTarget.Framebuffer,
    FramebufferAttachment.DepthAttachment,
    TextureTarget.TextureCubeMapPositiveX,
    id,
    0);
```

`BindDepthCubemapFace` already rebinds faces 0 through 5 the same way. Leave it.

**Why:** Six draws need one face attached at a time. The layered `FramebufferTexture` call is for a geometry shader this feature does not add.

## 3. Cull front faces

`SetFaceCulling(true)` always selects back faces. Add a separate switch.

`IRendererAPI`:

```csharp
void SetCullFrontFaces(bool cullFront);
```

`OpenGLRendererApi`:

```csharp
public void SetCullFrontFaces(bool cullFront)
{
    SilkNetContext.GL.Enable(EnableCap.CullFace);
    SilkNetContext.GL.CullFace(cullFront ? TriangleFace.Front : TriangleFace.Back);
    OpenGLDebug.CheckError(SilkNetContext.GL, "CullFace");
}
```

**Why:** The point depth pass renders back faces so a thin surface occludes from its far side. The color pass and the directional depth pass keep culling back faces.

## 4. Distance shader

`Engine/Engine.Shaders.props` copies every file under `assets/shaders/OpenGL`. Add the pair there. Also add the same two `Content` entries to `tests/Engine.GraphicsTests/Engine.GraphicsTests.csproj` next to `depth.frag`. The props glob copies them too; the test project lists shaders a second time, and the new pair has to be on that list.

`Engine/assets/shaders/OpenGL/pointDepth.vert`:

```glsl
#version 330 core

layout(location = 0) in vec3 a_Position;

uniform mat4 u_ViewProjection;
uniform mat4 u_Model;

out vec3 v_WorldPos;

void main()
{
    vec4 worldPos = vec4(a_Position, 1.0) * u_Model;
    v_WorldPos = worldPos.xyz;
    gl_Position = worldPos * u_ViewProjection;
}
```

`Engine/assets/shaders/OpenGL/pointDepth.frag`:

```glsl
#version 330 core

in vec3 v_WorldPos;

uniform vec3 u_LightPosition;
uniform float u_LightRange;

void main()
{
    gl_FragDepth = length(v_WorldPos - u_LightPosition) / u_LightRange;
}
```

Register the id and the file name. Leave `ShaderId.Depth` on `depth`.

```csharp
public enum ShaderId
{
    Texture,
    Line,
    Cube,
    Model,
    Depth,
    PointDepth
}
```

```csharp
ShaderId.PointDepth => "pointDepth",
```

**Why:** The directional fragment shader writes nothing, so the image keeps window depth. Point faces must store distance over range. A second program avoids a mode flag that could leave the sun map comparing the wrong value.

## 5. Draw one face

`IGraphics3D` gains:

```csharp
bool BeginPointShadowFace(int lightIndex, int face, Matrix4x4 viewProjection, Vector3 lightPosition, float range);
void EndPointShadowFace();
```

In `Graphics3D`, keep a cubemap slot per light, an enabled flag per light, the distance shader, and which pass is open.

```csharp
private const int PointShadowSlot = 4;

private IShader _pointDepthShader = null!;
private readonly IFrameBuffer?[] _pointShadowMaps = new IFrameBuffer[LightingMath.MaxPointLights];
private readonly bool[] _pointShadowEnabled = new bool[LightingMath.MaxPointLights];
private bool _pointShadowPass;
private IFrameBuffer? _activePointMap;

private static readonly string[] PointShadowEnabledUniforms = Names("u_PointShadowsEnabled");
private static readonly string[] PointShadowMapUniforms = Names("u_PointShadowMaps");
```

`Names` already builds `uniform[i]`. `SetPointLights` already copies the light list. Clear the flags there so a light that is absent this frame cannot stay enabled:

```csharp
Array.Clear(_pointShadowEnabled);
```

`Init` loads `ShaderId.PointDepth` and, while each color shader is bound, sets `u_PointShadowMaps[i]` to `PointShadowSlot + i`.

```csharp
public bool BeginPointShadowFace(
    int lightIndex, int face, Matrix4x4 viewProjection, Vector3 lightPosition, float range)
{
    if ((uint)lightIndex >= LightingMath.MaxPointLights || (uint)face >= LightingMath.PointShadowFaceCount)
        return false;

    IFrameBuffer map;
    try
    {
        map = PointShadowMap(lightIndex);
    }
    catch (Exception)
    {
        _pointShadowEnabled[lightIndex] = false;
        return false;
    }

    _shadowPass = true;
    _pointShadowPass = true;
    _activePointMap = map;
    map.Bind();
    map.BindDepthCubemapFace(face);
    rendererApi.SetDepthTest(true);
    rendererApi.SetDepthWrite(true);
    rendererApi.Clear();
    rendererApi.SetCullFrontFaces(true);

    _pointDepthShader.Bind();
    _pointDepthShader.SetMat4(ViewProjectionUniform, viewProjection);
    _pointDepthShader.SetFloat3("u_LightPosition", lightPosition);
    _pointDepthShader.SetFloat("u_LightRange", range);
    _pointShadowEnabled[lightIndex] = true;
    return true;
}

public void EndPointShadowFace()
{
    _pointDepthShader.Unbind();
    _activePointMap?.Unbind();
    _activePointMap = null;
    rendererApi.SetCullFrontFaces(false);
    _pointShadowPass = false;
    _shadowPass = false;
}

private IFrameBuffer PointShadowMap(int lightIndex)
{
    var existing = _pointShadowMaps[lightIndex];
    if (existing != null)
        return existing;

    var size = (uint)LightingMath.PointShadowFaceResolution;
    var spec = new FrameBufferSpecification(size, size)
    {
        AttachmentsSpec = new FrameBufferAttachmentSpecification([
            new FrameBufferTextureSpecification(FrameBufferTextureFormat.DepthCubemap)
        ])
    };
    var map = frameBuffers.Create(spec);
    _pointShadowMaps[lightIndex] = map;
    return map;
}
```

`DrawShadow` picks the shader from the pass. The directional pass leaves `_pointShadowPass` false.

```csharp
private void DrawShadow(Mesh mesh, Matrix4x4 transform)
{
    var shader = _pointShadowPass ? _pointDepthShader : _depthShader;
    rendererApi.SetDepthTest(true);
    shader.SetMat4("u_Model", transform);
    mesh.Bind();
    rendererApi.DrawIndexed(mesh.GetVertexArray(), (uint)mesh.GetIndexCount());
    _stats.DrawCalls++;
}
```

`UploadFrame` already runs once per color shader per scene. After the point-light uniforms:

```csharp
for (var i = 0; i < LightingMath.MaxPointLights; i++)
{
    shader.SetInt(PointShadowEnabledUniforms[i], _pointShadowEnabled[i] ? 1 : 0);
    if (_pointShadowEnabled[i] && _pointShadowMaps[i] != null)
        rendererApi.BindTextureCube(_pointShadowMaps[i]!.GetDepthAttachmentRendererId(), PointShadowSlot + i);
}
```

`Dispose` disposes each entry in `_pointShadowMaps` and the directional map it already disposes.

**Why:** `DrawCube` and `DrawMesh` already divert into `DrawShadow` when `_shadowPass` is set. The distance shader is the only new branch. Flags are uploaded with the other frame uniforms, not per draw. A failed create returns false without changing the cull mode, so the caller must not call end.

## 6. Walk the lights

In `SceneRenderPipeline.Render3D`, after the directional block and before `BeginScene`. `pointCount` is the value `ResolvePointLights` just returned.

```csharp
private static bool _pointShadowWarned;

for (var i = 0; i < pointCount; i++)
{
    var light = PointLightBuffer[i];
    Span<Matrix4x4> faces = stackalloc Matrix4x4[LightingMath.PointShadowFaceCount];
    if (!LightingMath.TryBuildPointShadowFaces(light.Position, light.Range, faces))
        continue;

    for (var face = 0; face < LightingMath.PointShadowFaceCount; face++)
    {
        if (!graphics3D.BeginPointShadowFace(i, face, faces[face], light.Position, light.Range))
        {
            if (!_pointShadowWarned)
            {
                _pointShadowWarned = true;
                Logger.Warning("Point shadow cubemap failed; drawing that light without a shadow");
            }

            break;
        }

        try
        {
            DrawOpaque3D(context, graphics3D, textureFactory, modelFactory);
        }
        finally
        {
            graphics3D.EndPointShadowFace();
        }
    }
}
```

The recording substitute used by the pipeline tests implements the two new methods. Begin appends the matrix and returns true. End counts completions. No point light means this loop does not run, so the existing directional order assertions stay valid.

**Why:** `try/finally` restores back-face culling when a draw throws. `break` stops the remaining faces of the light that failed and leaves the later lights to try. `SetPointLights` already cleared every flag, and a failed begin clears that light again, including after an earlier face of the same light had set it.

## 7. Sample nine times in both fragment shaders

Add the uniforms next to the other point-light uniforms in `cube.frag` and `modelShader.frag`:

```glsl
uniform int u_PointShadowsEnabled[c_MaxPointLights];
uniform samplerCube u_PointShadowMaps[c_MaxPointLights];

const float c_PointShadowBias = 0.05;

float PointShadow(int i, vec3 fragPos)
{
    if (u_PointShadowsEnabled[i] == 0)
        return 1.0;

    vec3 toFrag = fragPos - u_PointLightPositions[i];
    float dist = length(toFrag);
    float range = u_PointLightRanges[i];
    float current = dist / range;
    float bias = c_PointShadowBias / range;
    vec3 direction = toFrag / dist;
    vec3 helper = abs(direction.y) > 0.99 ? vec3(1.0, 0.0, 0.0) : vec3(0.0, 1.0, 0.0);
    vec3 tangent = normalize(cross(helper, direction));
    vec3 bitangent = cross(direction, tangent);
    float step = dist * 3.14159265 / 512.0;
    float shadow = 0.0;
    for (int x = -1; x <= 1; ++x)
    {
        for (int y = -1; y <= 1; ++y)
        {
            vec3 sampleDir = toFrag + (tangent * float(x) + bitangent * float(y)) * step;
            float closest = texture(u_PointShadowMaps[i], sampleDir).r;
            shadow += current - bias > closest ? 0.0 : 1.0;
        }
    }

    return shadow / 9.0;
}
```

In both copies of `PointLights`, the early-out that adds full radiance when `dist < c_PointEpsilon` stays. The attenuated term becomes:

```glsl
float pointShadow = PointShadow(i, fragPos);
sum += (diffuse + specular) * attenuation * pointShadow;
```

Index `u_PointShadowMaps` only with the loop index `i`. Leave `DirectionalShadow` as it is.

**Why:** `current` and the cubemap are both distance over range. The step is two texels of a 90° face at `dist`: one texel is `dist * (pi / 2) / 512`. A fragment on the lamp still takes the epsilon path and does not divide by a zero distance.

## 8. Tests

In `LightingMathTests`, no GPU:

```csharp
[Fact]
public void TryBuildPointShadowFaces_PointOnPositiveX_IsOnlyInThatFace()
{
    var position = new Vector3(2f, 3f, 4f);
    Span<Matrix4x4> faces = stackalloc Matrix4x4[6];
    LightingMath.TryBuildPointShadowFaces(position, 10f, faces).ShouldBeTrue();

    var onAxis = position + new Vector3(1f, 0f, 0f);
    LightingMath.PointShadowFaceContains(faces[0], onAxis).ShouldBeTrue();
    for (var i = 1; i < 6; i++)
        LightingMath.PointShadowFaceContains(faces[i], onAxis).ShouldBeFalse();

    var pastRange = position + new Vector3(11f, 0f, 0f);
    for (var i = 0; i < 6; i++)
        LightingMath.PointShadowFaceContains(faces[i], pastRange).ShouldBeFalse();
}

[Fact]
public void TryBuildPointShadowFaces_RangeAtNear_ReturnsFalse()
{
    Span<Matrix4x4> faces = stackalloc Matrix4x4[6];
    LightingMath.TryBuildPointShadowFaces(Vector3.Zero, LightingMath.PointShadowNear, faces).ShouldBeFalse();
}
```

In `SceneRenderPipelineShadowTests`, record faces on the substitute:

```csharp
public List<Matrix4x4> PointShadowFaces { get; } = [];
public int EndPointShadowFaces { get; private set; }

public bool BeginPointShadowFace(
    int lightIndex, int face, Matrix4x4 viewProjection, Vector3 lightPosition, float range)
{
    PointShadowFaces.Add(viewProjection);
    Order.Add("begin-point-shadow");
    return true;
}

public void EndPointShadowFace()
{
    EndPointShadowFaces++;
    Order.Add("end-point-shadow");
}
```

One point light at the origin with range 10, one cube, and no directional light: `PointShadowFaces.Count` is 6, `EndPointShadowFaces` is 6, cube draws are 7, and the order is six `begin-point-shadow` / `cube` / `end-point-shadow` groups followed by `begin-scene` / `cube`. A second light makes 12 faces. Range `0.1` makes 0 faces. The existing directional tests construct no point light, so their order lists stay as they are.

In `ShaderFactoryCacheTests`, add a fact that creates `pointDepth.vert` with `pointDepth.frag` the same way the cache test creates `cube.vert` with `cube.frag`, and asserts the program id is not 0. The existing `cube.frag` compile covers `PointShadow`. Do not add a compile fact for `modelShader.frag`.

**Why:** Face containment is a CPU clip test. The pipeline substitute never builds a framebuffer, which is why a failed cubemap is not what those tests assert. Shader compilation is the check that the distance write and the nine-tap function are valid GLSL.
