using Engine.Platform.OpenGL;
using Engine.Platform.OpenGL.Buffers;
using Shouldly;

namespace Engine.GraphicsTests;

[Trait("Category", "GraphicsIntegration")]
[Collection("GraphicsIntegration")]
public class VertexArrayIntegrationTests(HeadlessGraphicsContextFixture fixture)
    : IClassFixture<HeadlessGraphicsContextFixture>
{
    [GraphicsFact]
    public void Create_ReturnsNonZeroVaoId()
    {
        using var vao = fixture.VertexArrayFactory.Create();
        var glVao = (OpenGLVertexArray)vao;

        glVao.RendererId.ShouldNotBe(0u);
    }

    [GraphicsFact]
    public void AddVertexBuffer_FloatLayout_BindsCorrectStrideAndOffsets()
    {
        using var vao = fixture.VertexArrayFactory.Create();
        using var vbo = fixture.VertexBufferFactory.Create(1024);
        vbo.SetLayout(TestBufferLayouts.FloatOnly);
        vao.AddVertexBuffer(vbo);
        vao.Bind();

        GlBufferQueries.GetAttribEnabled(0).ShouldBe(1);
        GlBufferQueries.GetAttribSize(0).ShouldBe(3);
        GlBufferQueries.GetAttribStride(0).ShouldBe(TestBufferLayouts.FloatOnlyStride);
        GlBufferQueries.GetAttribOffset(0).ShouldBe(0);

        GlBufferQueries.GetAttribEnabled(1).ShouldBe(1);
        GlBufferQueries.GetAttribSize(1).ShouldBe(4);
        GlBufferQueries.GetAttribStride(1).ShouldBe(TestBufferLayouts.FloatOnlyStride);
        GlBufferQueries.GetAttribOffset(1).ShouldBe(12);
    }

    [GraphicsFact]
    public void AddVertexBuffer_MixedLayout_EnablesIntAttributeViaIPointer()
    {
        using var vao = fixture.VertexArrayFactory.Create();
        using var vbo = fixture.VertexBufferFactory.Create(1024);
        vbo.SetLayout(TestBufferLayouts.MixedFloatInt);
        vao.AddVertexBuffer(vbo);
        vao.Bind();

        GlBufferQueries.GetAttribEnabled(0).ShouldBe(1);
        GlBufferQueries.GetAttribSize(0).ShouldBe(3);
        GlBufferQueries.GetAttribStride(0).ShouldBe(TestBufferLayouts.MixedFloatIntStride);
        GlBufferQueries.GetAttribOffset(0).ShouldBe(0);

        GlBufferQueries.GetAttribEnabled(1).ShouldBe(1);
        GlBufferQueries.GetAttribSize(1).ShouldBe(1);
        GlBufferQueries.GetAttribStride(1).ShouldBe(TestBufferLayouts.MixedFloatIntStride);
        GlBufferQueries.GetAttribOffset(1).ShouldBe(12);
    }

    [GraphicsFact]
    public void AddVertexBuffer_WithoutLayout_ThrowsInvalidOperationException()
    {
        using var vao = fixture.VertexArrayFactory.Create();
        using var vbo = fixture.VertexBufferFactory.Create(1024);

        Should.Throw<InvalidOperationException>(() => vao.AddVertexBuffer(vbo));
    }

    [GraphicsFact]
    public void SetIndexBuffer_BindsElementArrayBuffer()
    {
        using var vao = fixture.VertexArrayFactory.Create();
        using var ibo = fixture.IndexBufferFactory.Create([0u, 1u, 2u], 3);
        var indexId = ((OpenGLIndexBuffer)ibo).RendererId;

        vao.SetIndexBuffer(ibo);
        vao.Bind();

        GlBufferQueries.GetElementArrayBufferBinding().ShouldBe(indexId);
    }

    [GraphicsFact]
    public void Dispose_DeletesVaoAndChildBuffers_IsBufferAndIsVertexArrayReturnFalse()
    {
        var vao = fixture.VertexArrayFactory.Create();
        var glVao = (OpenGLVertexArray)vao;

        using var vbo = fixture.VertexBufferFactory.Create(1024);
        vbo.SetLayout(TestBufferLayouts.FloatOnly);
        vao.AddVertexBuffer(vbo);
        var vboId = ((OpenGLVertexBuffer)vbo).RendererId;

        var ibo = fixture.IndexBufferFactory.Create([0u, 1u, 2u], 3);
        var iboId = ((OpenGLIndexBuffer)ibo).RendererId;
        vao.SetIndexBuffer(ibo);

        vao.Dispose();

        glVao.RendererId.ShouldBe(0u);
        GlBufferQueries.IsBufferAlive(vboId).ShouldBeFalse();
        GlBufferQueries.IsBufferAlive(iboId).ShouldBeFalse();
    }
}
