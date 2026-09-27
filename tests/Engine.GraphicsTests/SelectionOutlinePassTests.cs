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
    public void Resolve_MultipleIds_OutlinesEach()
    {
        const int width = FramebufferTestSpecs.Width;
        using var scene = fixture.FrameBufferFactory.Create(FramebufferTestSpecs.ColorAndEntityId());
        PaintEntityIdRect(scene, 10, 16, 16, 8, 8, clear: true);
        PaintEntityIdRect(scene, 20, 48, 16, 8, 8, clear: false);

        using var pass = new SelectionOutlinePass(
            fixture.RendererApi, fixture.ShaderFactory, fixture.VertexArrayFactory, fixture.FrameBufferFactory);

        var outlined = pass.Resolve(scene, scene, [10, 20]);

        outlined.ShouldNotBeSameAs(scene);
        fixture.RendererApi.GetError().ShouldBe(0);

        var pixels = GlFramebufferCapture.ReadColorRgba8(outlined);
        var y = FramebufferTestSpecs.Height - 1 - 24;
        Byte(pixels, width, 15, y, 1).ShouldBe((byte)140);
        Byte(pixels, width, 47, y, 1).ShouldBe((byte)140);
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
        var y = FramebufferTestSpecs.Height - 1 - 24; // capture is top-down; scissor rect uses GL y = 24
        Byte(pixels, width, 24, y, 0).ShouldBe((byte)255);
        Byte(pixels, width, 15, y, 0).ShouldBe((byte)255);
        Byte(pixels, width, 15, y, 1).ShouldBe((byte)140);
        Byte(pixels, width, 15, y, 2).ShouldBe((byte)0);
        Byte(pixels, width, 14, y, 0).ShouldBe((byte)255);
        Byte(pixels, width, 14, y, 1).ShouldBe((byte)255);
        Byte(pixels, width, 14, y, 2).ShouldBe((byte)255);
    }

    private static void Paint(Engine.Renderer.Buffers.FrameBuffer.IFrameBuffer scene, int id) =>
        PaintEntityIdRect(scene, id, 16, 16, 16, 16, clear: true);

    private static void PaintEntityIdRect(
        Engine.Renderer.Buffers.FrameBuffer.IFrameBuffer scene,
        int id,
        int x,
        int y,
        int w,
        int h,
        bool clear)
    {
        var gl = SilkNetContext.GL;
        scene.Bind();
        if (clear)
        {
            gl.ClearColor(1f, 1f, 1f, 1f);
            gl.Clear(ClearBufferMask.ColorBufferBit);
            scene.ClearAttachment(1, -1);
        }

        gl.Enable(EnableCap.ScissorTest);
        gl.Scissor(x, y, w, h);
        scene.ClearAttachment(1, id);
        gl.Disable(EnableCap.ScissorTest);
        scene.Unbind();
    }

    private static byte Byte(byte[] pixels, int width, int x, int y, int channel) =>
        pixels[(y * width + x) * 4 + channel];
}
