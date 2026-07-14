using System.Numerics;
using Engine.GraphicsTests.ImageRegression;
using Engine.Renderer.Buffers.FrameBuffer;

namespace Engine.GraphicsTests;

[Trait("Category", "GraphicsIntegration")]
[Collection("GraphicsIntegration")]
public class FramebufferClearRegressionTests(HeadlessGraphicsContextFixture fixture)
    : IClassFixture<HeadlessGraphicsContextFixture>
{
    [GraphicsFact]
    public void ClearColor_MatchesBaseline()
    {
        var spec = new FrameBufferSpecification(FramebufferTestSpecs.Width, FramebufferTestSpecs.Height)
        {
            AttachmentsSpec = new FrameBufferAttachmentSpecification([
                new FrameBufferTextureSpecification(FrameBufferTextureFormat.RGBA8),
                new FrameBufferTextureSpecification(FrameBufferTextureFormat.Depth),
            ])
        };

        using var framebuffer = fixture.FrameBufferFactory.Create(spec);

        framebuffer.Bind();
        fixture.RendererApi.SetClearColor(new Vector4(0.12f, 0.34f, 0.56f, 1f));
        fixture.RendererApi.Clear();
        var pixels = GlFramebufferCapture.ReadBoundColorRgba8(FramebufferTestSpecs.Width, FramebufferTestSpecs.Height);
        framebuffer.Unbind();

        ImageRegressionAssert.MatchesBaseline(
            "clear-color",
            pixels,
            FramebufferTestSpecs.Width,
            FramebufferTestSpecs.Height,
            exact: true);
    }
}
