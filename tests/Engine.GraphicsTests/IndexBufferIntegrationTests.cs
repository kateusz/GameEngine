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
    public void Create_WithIndices_ReturnsNonZeroBufferId()
    {
        using var buffer = fixture.IndexBufferFactory.Create([0u, 1u, 2u], 3);
        var glBuffer = (OpenGLIndexBuffer)buffer;

        glBuffer.RendererId.ShouldNotBe(0u);
        GlBufferQueries.IsBufferAlive(glBuffer.RendererId).ShouldBeTrue();
    }

    [GraphicsFact]
    public void Create_UploadsStaticDrawUsage()
    {
        using var buffer = fixture.IndexBufferFactory.Create([0u, 1u, 2u], 3);
        var glBuffer = (OpenGLIndexBuffer)buffer;

        GlBufferQueries.GetIndexBufferUsage(glBuffer.RendererId).ShouldBe(BufferUsageARB.StaticDraw);
    }

    [GraphicsFact]
    public void Create_BufferSizeMatchesIndexCountTimesUintSize()
    {
        const int count = 3;
        using var buffer = fixture.IndexBufferFactory.Create([0u, 1u, 2u], count);
        var glBuffer = (OpenGLIndexBuffer)buffer;

        GlBufferQueries.GetIndexBufferSize(glBuffer.RendererId).ShouldBe(count * sizeof(uint));
    }

    [GraphicsFact]
    public void Create_CountPropertyMatchesProvidedCount()
    {
        using var buffer = fixture.IndexBufferFactory.Create([0u, 1u, 2u, 3u], 4);

        buffer.Count.ShouldBe(4);
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
