# Skybox — Implementation

Step-by-step code for the design in `introduction.md` and `developer-guide.md`. Paste the C# into the existing types. The GLSL is the shader half of the same change.

Face size is **512**. Capture near is **0.1**, far is **10**. Equirect scales are **0.1591** and **0.3183**. The sky mesh scale is **4**. There is no new system and no new DryIoc registration. `IGraphics3D` is already a singleton.

`Graphics3D` and `SceneRenderPipeline` gain a method each. Do not split those files.

## 1. Scene field and JSON

`IScene` and `Scene` gain the string. Default is empty.

```csharp
public string Skybox { get; set; } = "";
```

`SceneSerializer` writes the key only when the string is non-empty, and always assigns it on load so a second load onto the same scene can clear a previous sky.

```csharp
private const string SkyboxKey = "Skybox";

// SerializeToString, next to BackgroundColor
if (!string.IsNullOrWhiteSpace(scene.Skybox))
    jsonObj[SkyboxKey] = scene.Skybox;

// Deserialize, next to BackgroundColor
scene.Skybox = "";
if (jsonObj.TryGetPropertyValue(SkyboxKey, out var skyboxNode) && skyboxNode != null)
{
    var text = skyboxNode.Deserialize<string>(_options);
    if (!string.IsNullOrWhiteSpace(text))
        scene.Skybox = text;
}
```

**Why:** Old scene files have no key. Omitting the key when the field is empty keeps a re-save of those files from growing a blank entry. Assigning on every load stops a stale path surviving a file that no longer has one.

## 2. Capture matrices and equirect UV

`LightingMath` already builds the six capture views. Call `TryBuildPointShadowFaces(Vector3.Zero, SkyCaptureFar, faces)`. Far 10 is the range argument, so the shadow helper stays as it is. Near stays 0.1.

```csharp
public const int SkyCaptureFaceSize = 512;
public const float SkyCaptureFar = 10f;
public const float EquirectU = 0.1591f;
public const float EquirectV = 0.3183f;

public static Vector2 EquirectUv(Vector3 direction)
{
    var d = Vector3.Normalize(direction);
    var uv = new Vector2(MathF.Atan2(d.Z, d.X), MathF.Asin(d.Y));
    return uv * new Vector2(EquirectU, EquirectV) + new Vector2(0.5f, 0.5f);
}
```

**Why:** The face axes, the 90° projection, and the near plane already exist for point shadows. `EquirectUv` is the oracle for the fragment shader. `Atan2(z, x)` is GLSL `atan(z, x)`. `Atan2(0, 0)` is 0, which is the pole U of 0.5.

## 3. Shaders

Add `ShaderId.EquirectToCube` and `ShaderId.Skybox`. `Skybox` maps to the `skybox` pair. `EquirectToCube` uses `skybox.vert` and `equirectToCube.frag`, the same split `SelectionOutline` already uses with `fxaa.vert`.

Shared vertex, `skybox.vert`. Capture sets `u_Scale` to 1. The sky sets it to 4.

```glsl
#version 330 core

layout(location = 0) in vec3 a_Position;

out vec3 v_Direction;

uniform mat4 u_ViewProjection;
uniform float u_Scale;

void main()
{
    v_Direction = a_Position;
    gl_Position = vec4(a_Position * u_Scale, 1.0) * u_ViewProjection;
}
```

Capture fragment, `equirectToCube.frag`. One color output. The capture target has no entity-id attachment. Scale 1 keeps the unit cube between capture near 0.1 and far 10.

```glsl
#version 330 core

layout(location = 0) out vec4 o_Color;

in vec3 v_Direction;

uniform sampler2D u_Equirect;

const vec2 c_InvAtan = vec2(0.1591, 0.3183);

void main()
{
    vec3 d = normalize(v_Direction);
    float x = d.x == 0.0 ? 0.0 : d.x;
    float z = d.z == 0.0 ? 0.0 : d.z;
    vec2 uv = vec2(atan(z, x), asin(d.y)) * c_InvAtan + 0.5;
    o_Color = vec4(texture(u_Equirect, uv).rgb, 1.0);
}
```

Sky fragment, `skybox.frag`. The sample is the output. Entity id matches the picking clear.

```glsl
#version 330 core

layout(location = 0) out vec4 o_Color;
layout(location = 1) out int  o_EntityID;

in vec3 v_Direction;

uniform samplerCube u_Skybox;

void main()
{
    o_Color = texture(u_Skybox, v_Direction);
    o_EntityID = -1;
}
```

**Why:** One vertex file feeds both programs, and both read attribute 0, so they bind the existing cube mesh. The sky shader writes attachment 1 because the editor framebuffer is an MRT. The capture shader does not, because its framebuffer is a single color face.

## 4. Platform capture target

OpenGL stays in `Engine/Platform/OpenGL/OpenGLSkyCapture.cs`, reached through `IRendererAPI`. The six draws stay in `Graphics3D`, because that is where the unit cube mesh already lives. The cubemap is a texture id. `BindTextureCube` already binds one.

```csharp
namespace Engine.Renderer.Textures;

public interface ISkyCapture : IDisposable
{
    bool BeginFace(int face);
}
```

```csharp
bool TryCreateSkyCapture(string absolutePath, out uint cubemapId, out ISkyCapture capture);
void DeleteTexture(uint textureId);
```

`TryCreateSkyCapture`:

1. `TextureFileDecoder.Decode(absolutePath, sRgb: false)`. On failure, log once for that path and return false.
2. If `width != height * 2`, log once for that path and continue.
3. Allocate a 512 cubemap, `RGBA8`, linear, clamp on S, T, and R. No mipmaps. Return its id.
4. Upload the decoded image as a 2D `RGBA8` texture, clamp, linear, no mipmaps. `ISkyCapture` keeps that texture until `Dispose`.
5. Allocate a private 512 framebuffer with a depth renderbuffer. Do not touch the scene framebuffer or a point-shadow cubemap.

`BeginFace` saves the draw framebuffer and the viewport on face 0, binds the equirectangular texture on unit 0, binds the capture framebuffer, attaches `TEXTURE_CUBE_MAP_POSITIVE_X + face`, sets the viewport to 512, and clears color and depth. It returns false when the face cannot be attached. `Dispose` deletes the 2D texture, unbinds unit 0, and restores the framebuffer and the viewport. It does not delete the cubemap.

**Why:** The scene target and the point-shadow depth cubemaps stay out of this pass. Save and restore is what makes a capture safe both before the editor binds its framebuffer and inside the runtime FXAA callback, which has already bound one. The cube draw stays next to the mesh. The caller deletes the id when the capture fails, and keeps it when the six faces succeed.

## 5. Graphics3D

`IGraphics3D` gains two members. `Graphics3D` creates both shaders in `Init` and sets `u_Equirect` and `u_Skybox` to unit 0.

```csharp
void SetSkybox(string? path);
void DrawSkybox(Matrix4x4 skyViewProjection);
```

```csharp
// ponytail: scale 4 puts a face at 2 units. PerspectiveNear >= 2 clips the sky.
// Upgrade path: scale from that camera's near and far.
private const float SkyScale = 4f;
private uint _skyCubemap;
private string _skySource = "";

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
    if (!rendererApi.TryCreateSkyCapture(full, out var cubemapId, out var capture))
    {
        _skySource = path;
        return;
    }

    using (capture)
    {
        Span<Matrix4x4> faces = stackalloc Matrix4x4[LightingMath.PointShadowFaceCount];
        if (!LightingMath.TryBuildPointShadowFaces(Vector3.Zero, LightingMath.SkyCaptureFar, faces)
            || !DrawCapture(capture, faces))
        {
            rendererApi.DeleteTexture(cubemapId);
            _skySource = path;
            return;
        }

        ClearSky();
        _skyCubemap = cubemapId;
        _skySource = path;
    }
}

private bool DrawCapture(ISkyCapture capture, ReadOnlySpan<Matrix4x4> faces)
{
    rendererApi.SetDepthTest(true);
    rendererApi.SetDepthWrite(true);
    rendererApi.SetFaceCulling(false);
    try
    {
        _equirectShader.Bind();
        _equirectShader.SetInt("u_Equirect", 0);
        _equirectShader.SetFloat("u_Scale", 1f);
        _cubeMesh.Bind();
        for (var i = 0; i < faces.Length; i++)
        {
            if (!capture.BeginFace(i))
                return false;

            _equirectShader.SetMat4("u_ViewProjection", faces[i]);
            rendererApi.DrawIndexed(_cubeMesh.GetVertexArray(), (uint)_cubeMesh.GetIndexCount());
        }

        return true;
    }
    finally
    {
        _equirectShader.Unbind();
        rendererApi.SetFaceCulling(true);
        rendererApi.SetDepthWrite(true);
    }
}

public void DrawSkybox(Matrix4x4 skyViewProjection)
{
    if (_skyCubemap == 0)
        return;

    rendererApi.SetDepthTest(true);
    rendererApi.SetDepthWrite(false);
    rendererApi.SetFaceCulling(false);
    try
    {
        _skyShader.Bind();
        _skyShader.SetMat4("u_ViewProjection", skyViewProjection);
        _skyShader.SetFloat("u_Scale", SkyScale);
        rendererApi.BindTextureCube(_skyCubemap, 0);
        _skyShader.SetInt("u_Skybox", 0);
        _cubeMesh.Bind();
        rendererApi.DrawIndexed(_cubeMesh.GetVertexArray(), (uint)_cubeMesh.GetIndexCount());
    }
    finally
    {
        rendererApi.BindTextureCube(0, 0);
        _skyShader.Unbind();
        rendererApi.SetFaceCulling(true);
        rendererApi.SetDepthWrite(true);
    }
}
```

`Dispose` calls `ClearSky`. `ClearSky` deletes `_skyCubemap` when it is not 0 and sets `_skySource` to empty.

The two hand-written `IGraphics3D` fakes (`MeshDrawRecordingGraphics3D` and the shadow-test recorder) gain `SetSkybox` and `DrawSkybox`. NSubstitute fakes pick the methods up on their own.

**Why:** The path compare happens before `Resolve`, so a frame that repeats the scene string does not allocate a path and does not capture. A failed capture remembers that string and leaves the previous cubemap in place, so the next frame does not resolve again and does not drop a sky that was already showing. Binding cubemap 0 in `finally` clears unit 0 before sprites bind a `sampler2D`.

## 6. Sky view and the draw call

`SceneView` gains a matrix at the end, defaulting to the zero matrix, so existing positional construction still compiles.

```csharp
public readonly record struct SceneView(
    Matrix4x4 ViewProjection,
    Vector3 ViewPosition = default,
    bool PointShadows = true,
    float DirectionalShadowCasterMaxDistance = LightingMath.ShadowDistance,
    bool DirectionalShadows = true,
    Matrix4x4 SkyViewProjection = default);
```

`CameraViews.TryFrom`:

```csharp
var projection = camera.GetProjectionMatrix();
var skyView = viewMatrix;
skyView.M41 = 0f;
skyView.M42 = 0f;
skyView.M43 = 0f;
view = new SceneView(
    viewMatrix * projection,
    new Vector3(transform.M41, transform.M42, transform.M43),
    SkyViewProjection: skyView * projection);
```

Edit mode in `EditorViewport` builds its `SceneView` by hand. Do the same zeroing on `_editorCamera.GetViewMatrix()` and pass `SkyViewProjection`.

`SceneRenderPipeline.RenderScene`, before `RenderSpritesAndSubTextures`:

```csharp
graphics3D.DrawSkybox(view.SkyViewProjection);
```

**Why:** Play mode and the player already go through `CameraViews`. Edit mode does not. Both have to strip translation or the edit camera flies out of the sky. `DrawSkybox` returns immediately when no cubemap is stored.

## 7. Editor, runtime, publish

`EditorViewport.RenderSceneToFramebuffer`, before `_frameBuffer.Bind()`:

```csharp
graphics3D.SetSkybox(sceneContext.ActiveScene?.Skybox);
```

Scene settings, under the color edit:

```csharp
var skybox = scene.Skybox;
if (ImGui.InputText("Skybox", ref skybox, 260))
    scene.Skybox = string.IsNullOrWhiteSpace(skybox)
        ? ""
        : PathBuilder.ToAssetRelativePath(skybox.Trim());
```

`GameLayer` takes `IGraphics3D` in its constructor. DryIoc already registers that service. Inside the FXAA callback, before `SetClearColor`:

```csharp
graphics3D.SetSkybox(scene.Skybox);
```

`PublishedAssetValidator.PathPropertyNames` gains `"Skybox"`.

**Why:** Edit and play share the viewport call. The player has no settings popup, so the game layer is the second owner. The validator already walks scene JSON for path strings. The assets directory copy already includes the image.

## 8. Tests

**Scene JSON.** In `tests/Engine.Tests/Scene/`, next to the dimension serializer tests. A file with `"Skybox":"sky/day.png"` loads that string. A file without the key loads `""`. Serializing a scene with a path writes the key. Serializing an empty path omits it. Loading that output onto a scene that already had a path leaves `""`.

**Equirect UV.** `EquirectUv` against literals spelled in the test, not against `EquirectU` / `EquirectV` from production code:

| Direction | U | V |
|-----------|---|---|
| +X | 0.5 | 0.5 |
| −X | 0.5 + atan2(0, −1) × 0.1591 | 0.5 |
| +Y | 0.5 | 0.5 + (π/2) × 0.3183 |
| −Y | 0.5 | 0.5 − (π/2) × 0.3183 |
| +Z | 0.5 + (π/2) × 0.1591 | 0.5 |
| −Z | 0.5 − (π/2) × 0.1591 | 0.5 |

Tolerance `1e-4`. +Y and −Y use U 0.5 because `Atan2(0, 0)` is 0.

**Pipeline order.** `RenderScene` calls `DrawSkybox` before 2D `BeginScene`. With no cubemap stored, that call returns without binding or drawing.

**Publish.** `ValidateAssetReferences` fails when a scene contains a non-empty `Skybox` whose file is absent. An empty `Skybox` does not add a missing path.

**Why:** The UV test fails without a GPU if the sphere mapping drifts from the shader. The pipeline test fails if the sky moves behind the sprites. The serializer test fails if an old scene grows a sky or a saved sky disappears. The publish test fails if a player build can ship a scene that points at nothing.
