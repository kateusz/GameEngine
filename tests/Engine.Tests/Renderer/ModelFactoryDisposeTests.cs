using Engine.Renderer.Buffers;
using Engine.Renderer.Buffers.VertexArray;
using Engine.Renderer.Meshes;
using Engine.Renderer.Models;
using NSubstitute;
using Shouldly;

namespace Engine.Tests.Renderer;

[Trait("Category", "Unit")]
public class ModelFactoryDisposeTests
{
    [Fact]
    public void Clear_DisposesCachedModelMeshes_AndAllowsCreateAgain()
    {
        var path = Path.GetTempFileName();
        try
        {
            var vao = Substitute.For<IVertexArray>();
            var factory = CreateFactory(path, vao);

            factory.Create(path).ShouldNotBeNull();
            factory.Clear();
            vao.Received(1).Dispose();

            factory.Create(path).ShouldNotBeNull();
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Dispose_DisposesCachedModels_AndRejectsFurtherCreate()
    {
        var path = Path.GetTempFileName();
        try
        {
            var vao = Substitute.For<IVertexArray>();
            var factory = CreateFactory(path, vao);

            factory.Create(path).ShouldNotBeNull();
            factory.Dispose();

            vao.Received(1).Dispose();
            Should.Throw<ObjectDisposedException>(() => factory.Create(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static ModelFactory CreateFactory(string path, IVertexArray vao)
    {
        var vbo = Substitute.For<IVertexBuffer>();
        var ibo = Substitute.For<IIndexBuffer>();
        ibo.Count.Returns(3);
        vao.IndexBuffer.Returns(ibo);

        var vaoFactory = Substitute.For<IVertexArrayFactory>();
        vaoFactory.Create().Returns(vao);
        var vboFactory = Substitute.For<IVertexBufferFactory>();
        vboFactory.Create(Arg.Any<List<Mesh.Vertex>>()).Returns(vbo);
        var iboFactory = Substitute.For<IIndexBufferFactory>();
        iboFactory.Create(Arg.Any<uint[]>(), Arg.Any<int>()).Returns(ibo);

        return new ModelFactory(
            _ => ([NewMesh()], null),
            vaoFactory,
            vboFactory,
            iboFactory);
    }

    private static Mesh NewMesh()
    {
        var mesh = new Mesh("m");
        mesh.Vertices.Add(default);
        mesh.Vertices.Add(default);
        mesh.Vertices.Add(default);
        mesh.Indices.AddRange([0u, 1u, 2u]);
        return mesh;
    }
}
