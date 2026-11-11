# OpenGL 4.1 — Implementation

C# and GLSL for the design in `introduction.md` and `developer-guide.md`. Paste into the existing types. Do not add a new ECS component or system.

## 1. Window API 4.1

`Engine/Core/DI/EngineIoCContainer.cs` `RegisterWindowing`, and `tests/Engine.GraphicsTests/HeadlessWindow.cs`, after `WindowOptions.Default`:

```csharp
windowOptions.API = new GraphicsAPI(
    ContextAPI.OpenGL,
    ContextProfile.Core,
    ContextFlags.ForwardCompatible,
    new APIVersion(4, 1));
```

`using Silk.NET.Core.Contexts;` / `Silk.NET.Windowing` as the project already uses for `WindowOptions`.

## 2. Version gate

`OpenGLRendererApi.Init`, first lines, before Enable(Blend):

```csharp
SilkNetContext.GL.GetInteger(GetPName.MajorVersion, out var major);
SilkNetContext.GL.GetInteger(GetPName.MinorVersion, out var minor);
if (major < 4 || (major == 4 && minor < 1))
{
    throw new RendererInitializationException(
        $"OpenGL 4.1 is required (got {major}.{minor}).");
}
```

Leave the existing `Enable(TextureCubeMapSeamless)` in place.

## 3. Include loop

Replace the single-include splice in `OpenGLShader.LoadShader` with a loop. Stop when the source no longer contains `#include "`. Resolve names relative to the including file’s directory, as today. One include still in flight is enough; do not add a search path.

## 4. FrameUniforms

New file `Engine/Renderer/FrameUniforms.cs`:

```csharp
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Engine.Renderer;

[InlineArray(8)]
public struct FrameVec4x8
{
    private Vector4 _e0;
}

[StructLayout(LayoutKind.Explicit, Size = ByteSize)]
public struct FrameUniforms
{
    [FieldOffset(0)] public Matrix4x4 ViewProjection;
    [FieldOffset(64)] public Matrix4x4 LightViewProjection;
    [FieldOffset(128)] public Matrix4x4 FarLightViewProjection;
    [FieldOffset(192)] public Vector4 AmbientColorStrength;
    [FieldOffset(208)] public Vector4 LightDirection;
    [FieldOffset(224)] public Vector4 LightColor;
    [FieldOffset(240)] public Vector4 ViewPosition;
    [FieldOffset(256)] public FrameVec4x8 PointPositions;
    [FieldOffset(384)] public FrameVec4x8 PointColors;
    [FieldOffset(512)] public FrameVec4x8 PointIntensityRange;
    [FieldOffset(640)] public int PointLightCount;
    [FieldOffset(644)] public int ShadowsEnabled;
    [FieldOffset(648)] public int CascadeCount;
    [FieldOffset(652)] public int ShadowPcf;
    [FieldOffset(656)] public float CascadeSplit;
    [FieldOffset(660)] public float ShadowMapSize;
    [FieldOffset(664)] public int PointPcf;
    [FieldOffset(668)] public float PointMapSize;
    [FieldOffset(672)] public int PointShadowMask;
    [FieldOffset(676)] public int Ibl;

    public const int ByteSize = 688;
}
```

**Why:** `InlineArray` is already on .NET 10. `Size = 688` rounds the last vec4. Add `FrameUniformsTests` that asserts `Unsafe.SizeOf<FrameUniforms>() == FrameUniforms.ByteSize`.

When packing from `Graphics3D`, transpose each `Matrix4x4` the same way `SetMat4` already does (`UniformMatrix4` with `transpose: true`): write columns as rows into the struct (or copy 16 floats in that order). `AmbientColorStrength` is color.xyz + strength in w. `PointIntensityRange[i]` is intensity, range, 0, 0. Bit `i` of `PointShadowMask` is today’s `u_PointShadowsEnabled[i]`.

## 5. IUniformBuffer

`Engine/Renderer/Buffers/IUniformBuffer.cs`:

```csharp
namespace Engine.Renderer.Buffers;

public interface IUniformBuffer : IDisposable
{
    void SetData(ReadOnlySpan<byte> data);
    void BindTo(uint bindingPoint);
}
```

`IRendererAPI.CreateUniformBuffer(int byteSize)` returns `OpenGLUniformBuffer`. That type: `GenBuffer`, `BindBuffer(UniformBuffer)`, `BufferData` with `DynamicDraw` at create. `SetData` is `BindBuffer` + `BufferSubData`. `BindTo` is `BindBufferBase(UniformBuffer, bindingPoint, id)`. `GLDebug.CheckError` after each call. Reject `byteSize <= 0`.

## 6. Graphics3D

Field `_frame` created in `Init` with `rendererApi.CreateUniformBuffer(FrameUniforms.ByteSize)`.

Replace `UploadFrame(IShader)` with:

```csharp
private void UploadFrame()
{
    FrameUniforms frame = default;
    frame.ViewProjection = Transpose(_viewProjection);
    // … remaining fields from the current UploadFrame sources …
    _frame.SetData(MemoryMarshal.AsBytes(new Span<FrameUniforms>(ref frame)));
    _frame.BindTo(0);

    if (_shadowsEnabled && _shadowMap != null)
        rendererApi.BindTexture2D(_shadowMap.GetDepthAttachmentRendererId(), ShadowMapSlot);
    // point cubes and IBL binds unchanged
}
```

`BeginScene` calls `UploadFrame()` once. Remove `_cubeShader` / `_modelShader` bind/unbind around frame uniforms. Keep sampler `SetInt` in `Init`.

Every unit test that does `new Graphics3D(...)` stubs `IRendererAPI.CreateUniformBuffer` to return a concrete `FakeUniformBuffer` (NSubstitute cannot stub `ReadOnlySpan`). Change `Graphics3DFrameUniformTests.BeginScene_UploadsViewAndLights_*` to expect `_frame.SetData` / `BindTo(0)` and `DidNotReceive` the old `SetMat4("u_ViewProjection", …)` on cube/model.

## 7. ProgramUniform and Frame binding

`OpenGLShader.SetInt` (same pattern for the other setters):

```csharp
SilkNetContext.GL.ProgramUniform1(_handle, location, data);
```

No `UseProgram` in setters. After a successful link:

```csharp
var block = SilkNetContext.GL.GetUniformBlockIndex(_handle, "Frame");
if (block != uint.MaxValue)
    SilkNetContext.GL.UniformBlockBinding(_handle, block, 0);
```

(`GL_INVALID_INDEX` is `0xFFFFFFFFu`.)

## 8. GLSL

`Engine/assets/shaders/OpenGL/frame.glsl` — `vec3` then `float` so `u_ViewPosition` and `u_AmbientColor` stay the names `shadow.glsl` already uses. The trailing floats on the light vectors are padding only:

```glsl
layout(std140) uniform Frame {
    mat4  u_ViewProjection;
    mat4  u_LightViewProjection;
    mat4  u_FarLightViewProjection;
    vec3  u_AmbientColor;
    float u_AmbientStrength;
    vec3  u_LightDirection;
    float _pad0;
    vec3  u_LightColor;
    float _pad1;
    vec3  u_ViewPosition;
    float _pad2;
    vec4  u_PointLightPositions[8];
    vec4  u_PointLightColors[8];
    vec4  u_PointLightIntensityRange[8];
    int   u_PointLightCount;
    int   u_ShadowsEnabled;
    int   u_CascadeCount;
    int   u_ShadowPcf;
    float u_CascadeSplit;
    float u_ShadowMapSize;
    int   u_PointPcf;
    float u_PointMapSize;
    int   u_PointShadowMask;
    int   u_Ibl;
};
```

In cube/model fragments, `#include "frame.glsl"` before `#include "shadow.glsl"`. Map only what packed:

- `u_PointLightPositions[i]` / `u_PointLightColors[i]` → `.xyz` of those vec4 slots
- `u_PointLightIntensities[i]` / `u_PointLightRanges[i]` → `u_PointLightIntensityRange[i].x` / `.y`
- `u_PointShadowsEnabled[i]` → `((u_PointShadowMask & (1 << i)) != 0)`

Vertex files include `frame.glsl` and delete their `uniform mat4 u_ViewProjection`. `shadow.glsl` deletes the uniforms that are now in the block; keep `u_ShadowMap`. Point-shadow sampling still uses `u_PointShadowMaps[i]` as a sampler array. C# still writes those vec3+float pairs as `Vector4` (color+strength, direction+0, color+0, view+0).

Every other shader: first line `#version 410 core` only.

`Engine.Shaders.props` already copies the OpenGL folder; `frame.glsl` is included automatically.

## 9. DryIoc, editor, Runtime

No new system priority. Editor and Runtime get the UBO through `IRendererAPI.CreateUniformBuffer`. Serialization unchanged.

## 10. Tests worth adding

- `FrameUniformsTests.SizeOf_Is688`
- `Graphics3DFrameUniformTests` rewritten for one UBO write
- `EngineShaderPathsTests` (or a sibling): every file under `assets/shaders/OpenGL` starts with `#version 410 core`; cube.frag and modelShader.frag do not contain `GL_ARB_gpu_shader5`
- `GraphicsContextInitTests`: `GetInteger(MajorVersion) == 4` and `MinorVersion >= 1` on the headless window (skip on the existing macOS graphics skip)
- Existing `ShaderFactoryCacheTests` still compile every listed pair

## 11. Doc nits in the same PR

Replace “OpenGL 3.3+” with “OpenGL 4.1+” in `docs/architecture/README.md`, `docs/guide/index.md`, and root `README.md`. Leave historical spec GLSL samples as 330.