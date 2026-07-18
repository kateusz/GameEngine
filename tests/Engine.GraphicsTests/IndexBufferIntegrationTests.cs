using Engine.Platform.OpenGL.Buffers;
using Silk.NET.OpenGL;
using Shouldly;

namespace Engine.GraphicsTests;

[Trait("Category", "GraphicsIntegration")]
[Collection("GraphicsIntegration")]
public class IndexBufferIntegrationTests(HeadlessGraphicsContextFixture fixture)
    : IClassFixture<HeadlessGraphicsContextFixture>
{
    [GraphicsFact]
    public void Create_AllocatesValidStaticDrawBuffer()
    {
        const int count = 3;
        using var buffer = fixture.IndexBufferFactory.Create([0u, 1u, 2u], count);
        var glBuffer = (OpenGLIndexBuffer)buffer;

        glBuffer.RendererId.ShouldNotBe(0u);
        GlBufferQueries.IsBufferAlive(glBuffer.RendererId).ShouldBeTrue();
        GlBufferQueries.GetIndexBufferUsage(glBuffer.RendererId).ShouldBe(BufferUsageARB.StaticDraw);
        GlBufferQueries.GetIndexBufferSize(glBuffer.RendererId).ShouldBe(count * sizeof(uint));
        buffer.Count.ShouldBe(count);
    }

    [GraphicsFact]
    public void Dispose_DeletesBuffer_IsBufferReturnsFalse()
    {
        var buffer = fixture.IndexBufferFactory.Create([0u, 1u, 2u], 3);
        var bufferId = ((OpenGLIndexBuffer)buffer).RendererId;

        buffer.Dispose();

        GlBufferQueries.IsBufferAlive(bufferId).ShouldBeFalse();
    }
}
