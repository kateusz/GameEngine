using Engine.GraphicsTests.ImageRegression;
using Engine.Platform.SilkNet;
using Engine.Renderer.Buffers.FrameBuffer;
using Engine.Renderer.Pipeline;
using Shouldly;
using Silk.NET.OpenGL;

namespace Engine.GraphicsTests;

[Trait("Category", "GraphicsIntegration")]
[Collection("GraphicsIntegration")]
public class VignettePassTests(HeadlessGraphicsContextFixture fixture)
    : IClassFixture<HeadlessGraphicsContextFixture>
{
    [GraphicsFact]
    public void Apply_DarkensCornersMoreThanCenter()
    {
        const int width = FramebufferTestSpecs.Width;
        const int height = FramebufferTestSpecs.Height;

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

        PaintFlatWhite(source, width, height);

        using var vignette = new VignettePass(fixture.RendererApi, fixture.ShaderFactory, fixture.VertexArrayFactory);
        vignette.Init();
        vignette.Available.ShouldBeTrue();

        vignette.Apply(source.GetColorAttachmentRendererId(), (uint)width, (uint)height, dest, intensity: 0.8f,
            radius: 0.5f);

        fixture.RendererApi.GetError().ShouldBe(0);

        var pixels = GlFramebufferCapture.ReadColorRgba8(dest);
        Luma601(pixels, width, width / 2, height / 2).ShouldBeGreaterThan(0.9f);
        Luma601(pixels, width, 0, 0).ShouldBeLessThan(0.5f);
    }

    private static void PaintFlatWhite(IFrameBuffer source, int width, int height)
    {
        var gl = SilkNetContext.GL;
        source.Bind();
        gl.ClearColor(1f, 1f, 1f, 1f);
        gl.Clear(ClearBufferMask.ColorBufferBit);
        source.Unbind();
    }

    private static float Luma601(byte[] pixels, int width, int x, int y)
    {
        var i = (y * width + x) * 4;
        return (0.299f * pixels[i] + 0.587f * pixels[i + 1] + 0.114f * pixels[i + 2]) / 255f;
    }
}
