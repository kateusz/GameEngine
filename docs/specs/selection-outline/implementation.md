# Selection Outline — Implementation

Step-by-step code for the design in `introduction.md` and `developer-guide.md`. Paste the C# into the existing types. The GLSL is the shader half of the same change.

The edge color is `(1, 0.55, 0, 1)`. The entity-id attachment is index **1**. Empty pixels stay **−1**. An id that is not greater than **0** does not draw.

## 1. Color attachment by index

Change the getter on `IFrameBuffer`, the abstract `FrameBuffer`, and `OpenGLFrameBuffer`. The default keeps every current call.

```csharp
uint GetColorAttachmentRendererId(int index = 0);
```

```csharp
public override uint GetColorAttachmentRendererId(int index = 0)
{
    if (_colorAttachments == null || index < 0 || index >= _colorAttachments.Length)
    {
        Debug.WriteLine("Warning: Attempted to get color attachment from framebuffer with no color attachments");
        return 0;
    }
    return _colorAttachments[index];
}
```

## 2. Shader id and sources

Add `SelectionOutline` to `ShaderId`, and this case in `EngineShaderPaths`:

`EngineShaderPaths` maps `ShaderId.SelectionOutline` to `fxaa.vert` and `selectionOutline.frag`. Add `Resolve_SelectionOutline_ReusesFxaaVertexShader` to `EngineShaderPathsTests`. New fragment shader files under `assets/shaders/OpenGL` are already copied to the output.

`assets/shaders/OpenGL/selectionOutline.frag`:

```glsl
#version 330 core

in vec2 v_TexCoord;
layout(location = 0) out vec4 o_Color;

uniform sampler2D u_Color;
uniform isampler2D u_EntityIds;
uniform int u_Id;

bool Selected(ivec2 p)
{
    ivec2 size = textureSize(u_EntityIds, 0);
    if (p.x < 0 || p.y < 0 || p.x >= size.x || p.y >= size.y)
        return false;
    int id = texelFetch(u_EntityIds, p, 0).r;
    return id > 0 && id == u_Id;
}

void main()
{
    vec4 color = texture(u_Color, v_TexCoord);
    ivec2 p = ivec2(gl_FragCoord.xy);
    if (Selected(p))
    {
        o_Color = color;
        return;
    }
    if (Selected(p + ivec2(1, 0))
        || Selected(p + ivec2(-1, 0))
        || Selected(p + ivec2(0, 1))
        || Selected(p + ivec2(0, -1)))
    {
        o_Color = vec4(1.0, 0.55, 0.0, 1.0);
        return;
    }
    o_Color = color;
}
```

## 3. The pass

Add `Engine/Renderer/Pipeline/SelectionOutlinePass.cs`. The shader factory owns the shader, so `Dispose` drops the reference and does not dispose it. The triangle and the outline buffer belong to the pass.

```csharp
using Engine.Renderer.Buffers.FrameBuffer;
using Engine.Renderer.Buffers.VertexArray;
using Engine.Renderer.Shaders;
using Serilog;

namespace Engine.Renderer.Pipeline;

public sealed class SelectionOutlinePass(
    IRendererAPI rendererApi,
    IShaderFactory shaderFactory,
    IVertexArrayFactory vertexArrayFactory,
    IFrameBufferFactory frameBuffers) : IDisposable
{
    private static readonly ILogger Logger = Log.ForContext<SelectionOutlinePass>();

    private IShader? _shader;
    private IVertexArray? _triangle;
    private IFrameBuffer? _output;
    private bool _initAttempted;
    private bool _disposed;

    public bool Available { get; private set; }

    public IFrameBuffer Resolve(IFrameBuffer colorSource, IFrameBuffer scene, int entityId)
    {
        var spec = colorSource.GetSpecification();
        if (entityId <= 0 || spec.Width == 0 || spec.Height == 0 || !EnsureInitialized() || _output == null)
            return colorSource;

        var entityTexture = scene.GetColorAttachmentRendererId(1);
        if (entityTexture == 0 || !Fit(spec.Width, spec.Height))
            return colorSource;

        Draw(colorSource.GetColorAttachmentRendererId(), entityTexture, spec.Width, spec.Height, entityId);
        return _output;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _triangle?.Dispose();
        _triangle = null;
        _output?.Dispose();
        _output = null;
        _shader = null;
        Available = false;
        _disposed = true;
    }

    private bool EnsureInitialized()
    {
        if (_initAttempted)
            return Available && _shader != null && _triangle != null && _output != null;

        _initAttempted = true;
        try
        {
            _shader = shaderFactory.Create(ShaderId.SelectionOutline);
            _triangle = vertexArrayFactory.Create();
            _output = frameBuffers.Create(ColorTarget(1, 1));
            _shader.Bind();
            _shader.SetInt("u_Color", 0);
            _shader.SetInt("u_EntityIds", 1);
            _shader.Unbind();
            Available = true;
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Selection outline disabled: failed to create shader or buffer");
            Available = false;
            _triangle?.Dispose();
            _triangle = null;
            _output?.Dispose();
            _output = null;
            _shader = null;
        }

        return Available && _shader != null && _triangle != null && _output != null;
    }

    private bool Fit(uint width, uint height)
    {
        try
        {
            var spec = _output!.GetSpecification();
            if (spec.Width != width || spec.Height != height)
                _output.Resize(width, height);
            return true;
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Selection outline disabled: framebuffer resize failed");
            Available = false;
            return false;
        }
    }

    private void Draw(uint colorTextureId, uint entityTextureId, uint width, uint height, int entityId)
    {
        _output!.Bind();
        rendererApi.SetViewport(0, 0, width, height);
        rendererApi.SetDepthTest(false);
        rendererApi.SetBlend(false);
        rendererApi.SetFaceCulling(false);
        try
        {
            _shader!.Bind();
            rendererApi.BindTexture2D(colorTextureId, 0);
            rendererApi.BindTexture2D(entityTextureId, 1);
            _shader.SetInt("u_Id", entityId);
            rendererApi.DrawArrays(_triangle!, 3);
            _shader.Unbind();
        }
        finally
        {
            rendererApi.SetDepthTest(true);
            rendererApi.SetBlend(true);
            rendererApi.SetFaceCulling(true);
            _output.Unbind();
        }
    }

    private static FrameBufferSpecification ColorTarget(uint width, uint height) =>
        new(width, height)
        {
            AttachmentsSpec = new FrameBufferAttachmentSpecification([
                new FrameBufferTextureSpecification(FrameBufferTextureFormat.RGBA8)
                {
                    Filter = FrameBufferTextureFilter.Linear,
                    Wrap = FrameBufferTextureWrap.ClampToEdge
                }
            ])
        };
}
```

`Resize` on a zero size returns without throwing. `Resolve` already rejects a zero size before `Fit`, so a hidden viewport does not disable the pass.

## 4. Viewport and editor container

Add `SelectionOutlinePass selectionOutlinePass` to the `EditorViewport` constructor. In `EditorIoCContainer.Register`, next to the viewport registration:

```csharp
container.Register<SelectionOutlinePass>(Reuse.Singleton);
```

Do not add that line to `EngineIoCContainer`.

In `LayoutAndRender`, replace the display choice with:

```csharp
var display = editorPreferences.Fxaa ? fxaaPass.Resolve(_frameBuffer) : _frameBuffer;
if (selection.SelectedEntity is { } selected)
    display = selectionOutlinePass.Resolve(display, _frameBuffer, selected.Id);
var texturePointer = ImGuiNativeTexture.FromColorAttachment(display);
```

`Resolve` returns `display` unchanged when it skips, so the ImGui image line stays as it is. There is no `SceneState` check. Play renders into `_frameBuffer` before this runs, and attachment 1 is cleared to −1 at the start of `RenderSceneToFramebuffer`.

## 5. Graphics test

Add `tests/Engine.GraphicsTests/SelectionOutlinePassTests.cs`. `FramebufferTestSpecs.ColorAndEntityId` is the scene target. `0.55 * 255` lands on byte **140**.

```csharp
using Engine.GraphicsTests.ImageRegression;
using Engine.Platform.SilkNet;
using Engine.Renderer.Pipeline;
using Shouldly;
using Silk.NET.OpenGL;

namespace Engine.GraphicsTests;

[Trait("Category", "GraphicsIntegration")]
[Collection("GraphicsIntegration")]
public class SelectionOutlinePassTests(HeadlessGraphicsContextFixture fixture)
    : IClassFixture<HeadlessGraphicsContextFixture>
{
    [GraphicsFact]
    public void Resolve_NonPositiveId_ReturnsSource()
    {
        using var scene = fixture.FrameBufferFactory.Create(FramebufferTestSpecs.ColorAndEntityId());
        using var pass = new SelectionOutlinePass(
            fixture.RendererApi, fixture.ShaderFactory, fixture.VertexArrayFactory, fixture.FrameBufferFactory);
        pass.Resolve(scene, scene, 0).ShouldBeSameAs(scene);
        fixture.RendererApi.GetError().ShouldBe(0);
    }

    [GraphicsFact]
    public void Resolve_PaintsOnePixelOutsideTheIdRect()
    {
        const int width = FramebufferTestSpecs.Width;
        const int id = 42;
        using var scene = fixture.FrameBufferFactory.Create(FramebufferTestSpecs.ColorAndEntityId());
        Paint(scene, id);

        using var pass = new SelectionOutlinePass(
            fixture.RendererApi, fixture.ShaderFactory, fixture.VertexArrayFactory, fixture.FrameBufferFactory);

        var outlined = pass.Resolve(scene, scene, id);

        outlined.ShouldNotBeSameAs(scene);
        fixture.RendererApi.GetError().ShouldBe(0);

        var pixels = GlFramebufferCapture.ReadColorRgba8(outlined);
        Byte(pixels, width, 24, 24, 0).ShouldBe((byte)255);
        Byte(pixels, width, 15, 24, 0).ShouldBe((byte)255);
        Byte(pixels, width, 15, 24, 1).ShouldBe((byte)140);
        Byte(pixels, width, 15, 24, 2).ShouldBe((byte)0);
        Byte(pixels, width, 14, 24, 0).ShouldBe((byte)255);
        Byte(pixels, width, 14, 24, 1).ShouldBe((byte)255);
        Byte(pixels, width, 14, 24, 2).ShouldBe((byte)255);
    }

    private static void Paint(Engine.Renderer.Buffers.FrameBuffer.IFrameBuffer scene, int id)
    {
        var gl = SilkNetContext.GL;
        scene.Bind();
        gl.ClearColor(1f, 1f, 1f, 1f);
        gl.Clear(ClearBufferMask.ColorBufferBit);
        scene.ClearAttachment(1, -1);
        gl.Enable(EnableCap.ScissorTest);
        gl.Scissor(16, 16, 16, 16);
        scene.ClearAttachment(1, id);
        gl.Disable(EnableCap.ScissorTest);
        scene.Unbind();
    }

    private static byte Byte(byte[] pixels, int width, int x, int y, int channel) =>
        pixels[(y * width + x) * 4 + channel];
}
```

The scissor covers x and y from 16 through 31. Pixel `(24, 24)` is inside and stays white. Pixel `(15, 24)` is the left neighbor and is orange. Pixel `(14, 24)` is two steps away and stays white.
