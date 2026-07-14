using Engine.Platform.OpenGL.Buffers;
using Engine.Renderer;
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
    public void SetMeshData_UploadsStaticDrawUsage()
    {
        using var buffer = fixture.VertexBufferFactory.Create((uint)Mesh.Vertex.GetSize());
        var vertices = new List<Mesh.Vertex> { default };

        buffer.SetMeshData(vertices, vertices.Count * Mesh.Vertex.GetSize());

        var glBuffer = (OpenGLVertexBuffer)buffer;
        GlBufferQueries.GetBufferUsage(glBuffer.RendererId).ShouldBe(BufferUsageARB.StaticDraw);
    }

    [GraphicsFact]
    public void SetMeshData_MatchesUploadedByteSize()
    {
        using var buffer = fixture.VertexBufferFactory.Create((uint)Mesh.Vertex.GetSize());
        var vertices = new List<Mesh.Vertex> { default };
        var byteSize = vertices.Count * Mesh.Vertex.GetSize();

        buffer.SetMeshData(vertices, byteSize);

        var glBuffer = (OpenGLVertexBuffer)buffer;
        GlBufferQueries.GetBufferSize(glBuffer.RendererId).ShouldBe(byteSize);
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
