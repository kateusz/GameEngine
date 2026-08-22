using Engine.Platform.OpenGL.Buffers;
using Silk.NET.OpenGL;
using Shouldly;

namespace Engine.GraphicsTests;

[Trait("Category", "GraphicsIntegration")]
[Collection("GraphicsIntegration")]
public class VertexBufferIntegrationTests(HeadlessGraphicsContextFixture fixture)
    : IClassFixture<HeadlessGraphicsContextFixture>
{
    private const uint MaxBufferSize = 256u * 1024 * 1024;

    [GraphicsFact]
    public void Create_AllocatesValidDynamicDrawBuffer()
    {
        using var buffer = fixture.VertexBufferFactory.Create(1024);
        var glBuffer = (OpenGLVertexBuffer)buffer;

        glBuffer.RendererId.ShouldNotBe(0u);
        GlBufferQueries.IsBufferAlive(glBuffer.RendererId).ShouldBeTrue();
        GlBufferQueries.GetBufferUsage(glBuffer.RendererId).ShouldBe(BufferUsageARB.DynamicDraw);
    }

    [GraphicsFact]
    public void Create_WithZeroSize_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => fixture.VertexBufferFactory.Create(0));
    }

    [GraphicsFact]
    public void Create_ExceedingMaxSize_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => fixture.VertexBufferFactory.Create(MaxBufferSize + 1));
    }

    [GraphicsFact]
    public void Dispose_DeletesBuffer_IsBufferReturnsFalse()
    {
        var buffer = fixture.VertexBufferFactory.Create(1024);
        var bufferId = ((OpenGLVertexBuffer)buffer).RendererId;

        buffer.Dispose();

        GlBufferQueries.IsBufferAlive(bufferId).ShouldBeFalse();
    }

    [GraphicsFact]
    public void Bind_AfterDispose_ThrowsObjectDisposedException()
    {
        var buffer = fixture.VertexBufferFactory.Create(1024);
        buffer.Dispose();

        Should.Throw<ObjectDisposedException>(() => buffer.Bind());
    }
}
