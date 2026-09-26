# Point Lighting — Implementation

Step-by-step code for the design in `introduction.md` and `developer-guide.md`. Paste the C# into the existing types. The GLSL is the shader half of the same change. Do not add a shared shader include.

The cap is **8**. Attenuation is `(1 - distance / range)²`. A fragment closer than **0.0001** to a light gets full diffuse and no specular.

## 1. Component

Create `SceneComponents/Lighting/PointLightComponent.cs`.

Color is a `Vector4` so it matches the other lights. Only RGB is read. There is no position: the world translation of `TransformComponent` is the light position.

```csharp
using System.Numerics;
using ECS;

namespace SceneComponents.Lighting;

public class PointLightComponent : IComponent
{
    public Vector4 Color { get; set; } = Vector4.One;
    public float Intensity { get; set; } = 1f;
    public float Range { get; set; } = 10f;

    public IComponent Clone() => new PointLightComponent
    {
        Color = Color,
        Intensity = Intensity,
        Range = Range
    };
}
```

**Why:** Authoring data only. Defaults match a visible white lamp of range 10 the first time someone adds the component.

## 2. Serializer, inspector, menu

In `ComponentSerializerRegistry.RegisterBuiltIn`, next to the directional registration:

```csharp
Register<PointLightComponent>();
```

In `EditorIoCContainer`, next to the directional editor:

```csharp
container.RegisterMany<PointLightComponentEditor>(Reuse.Singleton);
```

Create `Editor/ComponentEditors/Lighting/PointLightComponentEditor.cs`:

```csharp
using ECS;
using Editor.ComponentEditors.Core;
using Editor.Features.History;
using Editor.UI.Elements;
using SceneComponents.Lighting;

namespace Editor.ComponentEditors.Lighting;

public class PointLightComponentEditor(UIPropertyRenderer propertyRenderer, IEditorHistory history)
    : ComponentEditor<PointLightComponent>(history)
{
    protected override string DisplayName => "Point Light";

    protected override void DrawContent(PointLightComponent component, Entity entity)
    {
        propertyRenderer.DrawPropertyField("Color", component.Color,
            newValue => component.Color = (System.Numerics.Vector4)newValue);
        propertyRenderer.DrawPropertyField("Intensity", component.Intensity,
            newValue => component.Intensity = (float)newValue);
        propertyRenderer.DrawPropertyField("Range", component.Range,
            newValue => component.Range = (float)newValue);
    }
}
```

In `ComponentSelector`, next to the directional menu item:

```csharp
DrawComponentMenuItem<PointLightComponent>("Point Light", entity, history);
```

**Why:** Same three hooks ambient and directional already use. No new editor framework.

## 3. Shared light record and attenuation

`IGraphics3D` is public, so the record it accepts is public. Put it in `Engine/Renderer/PointLightData.cs`:

```csharp
using System.Numerics;

namespace Engine.Renderer;

public readonly record struct PointLightData(Vector3 Position, Vector3 Color, float Intensity, float Range);
```

Extend `LightingMath` with the cap and the curve the shader must copy. The CPU helper is for tests. The GPU does not call it.

```csharp
public const int MaxPointLights = 8;
public const float PointLightDistanceEpsilon = 0.0001f;

public static float PointAttenuation(float distance, float range)
{
    if (range <= 0f || distance >= range)
        return 0f;
    var remaining = 1f - distance / range;
    return remaining * remaining;
}
```

**Why:** One named curve. `distance == 0` returns 1. `distance == range` returns 0. Half range returns 0.25.

## 4. Resolve

`SceneRenderPipeline` is already a static class with reused static state. Add a buffer so the 3D pass does not allocate a list every frame.

```csharp
// ponytail: one buffer reused across frames. Ceiling: two scenes rendering at once would clobber it.
// Upgrade path: own the buffer on the render system.
private static readonly PointLightData[] PointLightBuffer = new PointLightData[LightingMath.MaxPointLights];
```

```csharp
internal static int ResolvePointLights(Context context, Span<PointLightData> destination)
{
    var limit = Math.Min(destination.Length, LightingMath.MaxPointLights);
    var count = 0;
    foreach (var (_, light, transform) in context.View<PointLightComponent, TransformComponent>())
    {
        if (light.Range <= 0f)
            continue;
        if (count == limit)
            break;

        destination[count++] = new PointLightData(
            transform.GetWorldTransform().Translation,
            new Vector3(light.Color.X, light.Color.Y, light.Color.Z),
            MathF.Max(0f, light.Intensity),
            light.Range);
    }

    return count;
}
```

`View<PointLightComponent, TransformComponent>()` only yields entities that have both, so a missing transform never takes a slot. Non-positive range is skipped the same way. Negative intensity stays in the slot as zero. Entity order is view order. The ninth valid light hits `count == limit` and stops.

`GetWorldTransform().Translation` is the cache `TransformHierarchySystem` writes (priority 115) before `SceneRenderSystem` (priority 150). Do not read `TransformComponent.Translation` — that is local and ignores parents.

In `Render3D`, after the directional setter and before `BeginScene`:

```csharp
var pointCount = ResolvePointLights(context, PointLightBuffer);
graphics3D.SetPointLights(PointLightBuffer.AsSpan(0, pointCount));
```

**Why:** The graphics object copies the span, so the static buffer can be overwritten next frame.

## 5. Graphics upload

On `IGraphics3D`:

```csharp
void SetPointLights(ReadOnlySpan<PointLightData> lights);
```

On `Graphics3D`, store the list and upload it from `UploadFrame`. Also send the camera position to **both** shaders. The cube needs it for specular. Change `BeginScene` to:

```csharp
UploadFrame(_cubeShader);
UploadFrame(_modelShader);
```

and drop the `includeViewPosition` flag. `UploadFrame` always sets `u_ViewPosition`.

```csharp
private readonly PointLightData[] _pointLights = new PointLightData[LightingMath.MaxPointLights];
private int _pointLightCount;

private static readonly string[] PointPositionUniforms = Names("u_PointLightPositions");
private static readonly string[] PointColorUniforms = Names("u_PointLightColors");
private static readonly string[] PointIntensityUniforms = Names("u_PointLightIntensities");
private static readonly string[] PointRangeUniforms = Names("u_PointLightRanges");

private static string[] Names(string uniform)
{
    var names = new string[LightingMath.MaxPointLights];
    for (var i = 0; i < names.Length; i++)
        names[i] = $"{uniform}[{i}]";
    return names;
}

public void SetPointLights(ReadOnlySpan<PointLightData> lights)
{
    _pointLightCount = Math.Min(lights.Length, LightingMath.MaxPointLights);
    lights[.._pointLightCount].CopyTo(_pointLights);
    Array.Clear(_pointLights, _pointLightCount, LightingMath.MaxPointLights - _pointLightCount);
}
```

Inside `UploadFrame`, after the directional uniforms:

```csharp
shader.SetFloat3("u_ViewPosition", _viewPosition);
shader.SetInt("u_PointLightCount", _pointLightCount);
for (var i = 0; i < LightingMath.MaxPointLights; i++)
{
    shader.SetFloat3(PointPositionUniforms[i], _pointLights[i].Position);
    shader.SetFloat3(PointColorUniforms[i], _pointLights[i].Color);
    shader.SetFloat(PointIntensityUniforms[i], _pointLights[i].Intensity);
    shader.SetFloat(PointRangeUniforms[i], _pointLights[i].Range);
}
```

`SetPointLights` zeros the tail, so a frame with fewer lights cannot leave last frame's lamp in an unused slot. The shader still stops at `u_PointLightCount`.

Indexed names such as `u_PointLightPositions[0]` match what `GetUniformLocation` expects. The name arrays exist so the hot path does not build those strings every frame.

The existing frame-uniform test asserts that the cube shader does **not** receive `u_ViewPosition`. Delete that assertion. Both shaders receive it.

## 6. Shaders

### Cube vertex shader

Add `out vec3 v_FragPos;` and, next to the existing world position:

```glsl
v_FragPos = worldPos.xyz;
```

### Both fragment shaders

Paste this into `cube.frag` and `modelShader.frag`. Keep the copies identical.

```glsl
const int c_MaxPointLights = 8;
const float c_PointEpsilon = 0.0001;

uniform int u_PointLightCount;
uniform vec3 u_PointLightPositions[c_MaxPointLights];
uniform vec3 u_PointLightColors[c_MaxPointLights];
uniform float u_PointLightIntensities[c_MaxPointLights];
uniform float u_PointLightRanges[c_MaxPointLights];

vec3 PointLights(vec3 N, vec3 fragPos, vec3 V, vec3 albedo, vec3 specColor, float shininess)
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
        float attenuation = remaining * remaining;
        vec3 L = toLight / dist;
        float ndotl = max(dot(N, L), 0.0);
        vec3 diffuse = ndotl * radiance * albedo;

        vec3 H = normalize(L + V);
        float spec = pow(max(dot(N, H), 0.0), shininess);
        vec3 specular = spec * radiance * specColor;
        sum += (diffuse + specular) * attenuation;
    }
    return sum;
}
```

The loop bound is the constant 8 so the array index stays in range. `break` honors the uploaded count.

`radiance * albedo` with no specular is the "sitting on the light" case: attenuation is 1 and `N·L` is treated as 1, because there is no direction to normalize.

### Cube fragment shader

`cube.frag` must declare `in vec3 v_FragPos;` and `uniform vec3 u_ViewPosition;`. Replace the end of `main` so the existing directional term (`diffuse`) stays multiplied by albedo, and the point term is added after that. The point function already includes albedo. Specular color is 0.5 and sharpness is 32.

```glsl
vec3 ambient = u_AmbientStrength * u_AmbientColor;

vec3 N = normalize(v_Normal);
vec3 L = normalize(-u_LightDirection);
float ndotl = max(dot(N, L), 0.0);
vec3 diffuse = ndotl * u_LightColor;

vec3 V = normalize(u_ViewPosition - v_FragPos);
vec3 points = PointLights(N, v_FragPos, V, albedo, vec3(0.5), 32.0);
o_Color = vec4((ambient + diffuse) * albedo + points, baseColor.a);
o_EntityID = u_EntityID;
```

### Model fragment shader

After the existing directional specular:

```glsl
vec3 points = PointLights(norm, v_FragPos, V, diffuseColor, specularColor, u_Shininess);
o_Color = vec4(ambient + diffuse + specular + points, u_Color.a);
```

`diffuseColor` is already the albedo (map or white, times `u_Color.rgb`). `specularColor` is the map or 0.5. `u_Shininess` is the mesh value. Do not multiply `points` by `diffuseColor` again.

Do not edit `textureShader` or `lineShader`.

## 7. Tests

Add cases to `SceneRenderPipelineLightingTests`. Each test builds a `Context`, registers entities, and calls `ResolvePointLights` with a stack array of length 8. For every light that should resolve, call `SetWorldTransform` with the world matrix. Setting `Translation` alone leaves the world cache at identity.

```csharp
private static int Resolve(Context context, out PointLightData[] lights)
{
    lights = new PointLightData[LightingMath.MaxPointLights];
    return SceneRenderPipeline.ResolvePointLights(context, lights);
}

private static PointLightComponent Lamp(float range = 10f, float intensity = 1f, Vector4? color = null) =>
    new()
    {
        Range = range,
        Intensity = intensity,
        Color = color ?? Vector4.One
    };
```

Empty context: count is 0.

One light, `SetWorldTransform(Matrix4x4.CreateTranslation(1, 2, 3))`: count is 1, position is `(1, 2, 3)`, intensity is 1, range is 10.

Child case: local `Translation` is `(5, 0, 0)`, but `SetWorldTransform` is a translation of `(9, 0, 0)`. Resolved position is `(9, 0, 0)`.

Two entities, the first has only `PointLightComponent`: count is 1 and the position belongs to the second entity.

Range `0` then a later range `4`: count is 1, range is 4.

Intensity `-2`: count is 1, intensity is 0.

Nine valid lights with positions `(i, 0, 0)` for `i` in `0..8`: count is 8, the last stored position is `(7, 0, 0)`.

Attenuation, in `LightingMathTests`:

```csharp
LightingMath.PointAttenuation(0f, 10f).ShouldBe(1f);
LightingMath.PointAttenuation(10f, 10f).ShouldBe(0f);
LightingMath.PointAttenuation(5f, 10f).ShouldBe(0.25f);
```

Extend `Graphics3DFrameUniformTests` so that after `SetPointLights` with one record and `BeginScene`:

- both shaders received `u_PointLightCount` of 1
- both shaders received that position on `u_PointLightPositions[0]`
- `DrawCube` does not set `u_PointLightCount` again
- the cube shader **does** receive `u_ViewPosition`

No GPU screenshot test.

## 8. Order of work

1. Component, serializer, editor, menu. The scene can store a light before it renders.
2. `PointLightData`, attenuation, resolver, resolver tests.
3. Graphics setter and the frame-uniform test.
4. Cube vertex output, then both fragment shaders.
5. Call the setter from `Render3D`.

Stop if the attenuation test or the nine-light test fails. Those two lock the curve and the cap. The shader copy is wrong if its expression is not `remaining * remaining` with the same 0.0001 cutoff.
