using Engine.GraphicsTests.ImageRegression;
using Engine.Platform.SilkNet;
using Engine.Renderer.Buffers.FrameBuffer;
using Engine.Renderer.Pipeline;
using Shouldly;
using Silk.NET.OpenGL;

namespace Engine.GraphicsTests;

[Trait("Category", "GraphicsIntegration")]
[Collection("GraphicsIntegration")]
public class FxaaPassTests(HeadlessGraphicsContextFixture fixture)
    : IClassFixture<HeadlessGraphicsContextFixture>
{
    [GraphicsFact]
    public void Apply_BlursHighContrastVerticalEdge()
    {
        const int width = FramebufferTestSpecs.Width;
        const int height = FramebufferTestSpecs.Height;
        var half = width / 2;

        using var source = fixture.FrameBufferFactory.Create(new FrameBufferSpecification((uint)width, (uint)height)
        {
            AttachmentsSpec = new FrameBufferAttachmentSpecification([
                new FrameBufferTextureSpecification(FrameBufferTextureFormat.RGBA8),
            ])
        });
        using var dest = fixture.FrameBufferFactory.Create(new FrameBufferSpecification((uint)width, (uint)height)
        {
            AttachmentsSpec = new FrameBufferAttachmentSpecification([
                new FrameBufferTextureSpecification(FrameBufferTextureFormat.RGBA8)
                {
                    Filter = FrameBufferTextureFilter.Linear,
                    Wrap = FrameBufferTextureWrap.ClampToEdge
                }
            ])
        });

        PaintJaggedVerticalEdge(source, width, height, half);
        Luma601(GlFramebufferCapture.ReadColorRgba8(source), width, width / 4, height / 2).ShouldBeGreaterThan(0.9f);

        using var fxaa = new FxaaPass(fixture.RendererApi, fixture.ShaderFactory, fixture.VertexArrayFactory);
        fxaa.Init();
        fxaa.Available.ShouldBeTrue();

        fxaa.Apply(source.GetColorAttachmentRendererId(), (uint)width, (uint)height, dest);

        fixture.RendererApi.GetError().ShouldBe(0);

        var pixels = GlFramebufferCapture.ReadColorRgba8(dest);
        var mid = height / 2;
        Luma601(pixels, width, width / 4, mid).ShouldBeGreaterThan(0.9f);
        var edgeLuma = Luma601(pixels, width, half, mid);
        edgeLuma.ShouldBeGreaterThan(0.05f);
        edgeLuma.ShouldBeLessThan(0.95f);
    }

    private static void PaintJaggedVerticalEdge(IFrameBuffer source, int width, int height, int half)
    {
        var gl = SilkNetContext.GL;
        source.Bind();
        gl.Enable(EnableCap.ScissorTest);
        for (var y = 0; y < height; y++)
        {
            var split = half + (y & 1);
            gl.Scissor(0, y, (uint)split, 1);
            gl.ClearColor(1f, 1f, 1f, 1f);
            gl.Clear(ClearBufferMask.ColorBufferBit);
            gl.Scissor(split, y, (uint)(width - split), 1);
            gl.ClearColor(0f, 0f, 0f, 1f);
            gl.Clear(ClearBufferMask.ColorBufferBit);
        }
        gl.Disable(EnableCap.ScissorTest);
        source.Unbind();
    }

    private static float Luma601(byte[] pixels, int width, int x, int y)
    {
        var i = (y * width + x) * 4;
        return (0.299f * pixels[i] + 0.587f * pixels[i + 1] + 0.114f * pixels[i + 2]) / 255f;
    }
}
