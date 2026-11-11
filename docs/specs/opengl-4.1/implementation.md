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

[StructLayout(LayoutKind.Sequential)]
public struct FrameUniforms
{
    public Matrix4x4 ViewProjection;
    public Matrix4x4 LightViewProjection;
    public Matrix4x4 FarLightViewProjection;
    public Vector4 AmbientColorStrength;
    public Vector4 LightDirection;
    public Vector4 LightColor;
    public Vector4 ViewPosition;
    public FrameVec4x8 PointPositions;
    public FrameVec4x8 PointColors;
    public FrameVec4x8 PointIntensityRange;
    public int PointLightCount;
    public int ShadowsEnabled;
    public int CascadeCount;
    public int ShadowPcf;
    public float CascadeSplit;
    public float ShadowMapSize;
    public int PointPcf;
    public float PointMapSize;
    public int PointShadowMask;
    public int Ibl;
    public int Pad0;
    public int Pad1;

    public const int ByteSize = 688;
}
```

**Why:** `InlineArray` is already on .NET 10. `Pad0`/`Pad1` round 680 to 688 (vec4). Add `FrameUniformsTests` that asserts `Unsafe.SizeOf<FrameUniforms>() == FrameUniforms.ByteSize`.

When packing from `Graphics3D`, transpose each `Matrix4x4` the same way `SetMat4` already does (`UniformMatrix4` with `transpose: true`): write columns as rows into the struct (or copy 16 floats in that order). `AmbientColorStrength` is color.xyz + strength in w. `PointIntensityRange[i]` is intensity, range, 0, 0. Bit `i` of `PointShadowMask` is today’s `u_PointShadowsEnabled[i]`.

## 5. IUniformBuffer

`Engine/Renderer/Buffers/IUniformBuffer.cs` and factory:

```csharp
namespace Engine.Renderer.Buffers;

public interface IUniformBuffer : IDisposable
{
    void SetData(ReadOnlySpan<byte> data);
    void BindTo(uint bindingPoint);
}

public interface IUniformBufferFactory
{
    IUniformBuffer Create(int byteSize);
}
```

`Engine/Platform/OpenGL/Buffers/OpenGLUniformBuffer.cs`: `GenBuffer`, `BindBuffer(UniformBuffer)`, `BufferData` with `DynamicDraw` at create. `SetData` is `BindBuffer` + `BufferSubData`. `BindTo` is `BindBufferBase(UniformBuffer, bindingPoint, id)`. `GLDebug.CheckError` after each call. Cap the size with the same 256 MB constant vertex buffers use.

`UniformBufferFactory` in `Engine/Platform/OpenGL/`, register in `RegisterFactories` next to the vertex-buffer factory.

## 6. Graphics3D

Constructor: add `IUniformBufferFactory uniformBuffers`. Field `_frame` created in `Init` with `FrameUniforms.ByteSize`.

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

Every unit test that does `new Graphics3D(...)` passes a substitute factory whose `Create` returns a substitute `IUniformBuffer`. Change `Graphics3DFrameUniformTests.BeginScene_UploadsViewAndLights_*` to expect `_frame.SetData` / `BindTo(0)` and `DidNotReceive` the old `SetMat4("u_ViewProjection", …)` on cube/model.

## 7. ProgramUniform and Frame binding

`OpenGLShader.SetInt` (same pattern for the other setters):

```csharp
SilkNetContext.GL.ProgramUniform1(_handle, location, data);
```

No `UseProgram` in setters. After a successful link, including the binary path:

```csharp
var block = SilkNetContext.GL.GetUniformBlockIndex(_handle, "Frame");
if (block != uint.MaxValue)
    SilkNetContext.GL.UniformBlockBinding(_handle, block, 0);
```

(`GL_INVALID_INDEX` is `0xFFFFFFFFu`.)

## 8. Program binaries

In `ShaderFactory.Create(string, string)`, still lock on the path pair. On miss, call a new `OpenGLShader.TryLoad` that:

1. Expands sources (shared with `LoadShader`).
2. `var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(vertSrc + "\n" + fragSrc)));`
3. Path = `Path.Combine(AppContext.BaseDirectory, "cache", "shader-binaries", hash + ".bin")`.
4. `GetInteger(NumProgramBinaryFormats, out var n)`. If `n > 0` and the file exists, `CreateProgram`, `ProgramBinary(handle, format, blob)`, `LinkProgram`. Format is the first 4 bytes (little-endian `int` of `GLEnum`). On link failure, `DeleteProgram` and fall through.
5. Else compile as today. Then `GetProgram(handle, ProgramBinaryLength, out len)` and `GetProgramBinary`; write format + bytes. Swallow IO exceptions.

`OpenGLShader` needs a constructor or static factory that takes already-expanded strings so the hash matches the compiled text.

## 9. GLSL

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

## 10. DryIoc, editor, Runtime

No new system priority. `RegisterFactories` adds the uniform-buffer factory. Editor and Runtime pick it up through `Graphics3D`’s constructor. Serialization unchanged.

## 11. Tests worth adding

- `FrameUniformsTests.SizeOf_Is688`
- `Graphics3DFrameUniformTests` rewritten for one UBO write
- `EngineShaderPathsTests` (or a sibling): every file under `assets/shaders/OpenGL` starts with `#version 410 core`; cube.frag and modelShader.frag do not contain `GL_ARB_gpu_shader5`
- `GraphicsContextInitTests`: `GetInteger(MajorVersion) == 4` and `MinorVersion >= 1` on the headless window (skip on the existing macOS graphics skip)
- Existing `ShaderFactoryCacheTests` still compile every listed pair

## 12. Doc nits in the same PR

Replace “OpenGL 3.3+” with “OpenGL 4.1+” in `docs/architecture/README.md`, `docs/guide/index.md`, and root `README.md`. Leave historical spec GLSL samples as 330.