using Engine.Renderer.Buffers;
using Engine.Renderer.Buffers.VertexArray;
using Engine.Renderer.Meshes;
using NSubstitute;
using Shouldly;

namespace Engine.Tests.Renderer;

[Trait("Category", "Unit")]
public class MeshCpuDataReleaseTests
{
    [Fact]
    public void Initialize_DropsCpuVerticesAndIndices_ButKeepsIndexCount()
    {
        var vao = Substitute.For<IVertexArray>();
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

        using var mesh = new Mesh("test");
        mesh.Vertices.Add(default);
        mesh.Vertices.Add(default);
        mesh.Vertices.Add(default);
        mesh.Indices.AddRange([0u, 1u, 2u]);

        mesh.Initialize(vaoFactory, vboFactory, iboFactory);

        mesh.Vertices.ShouldBeEmpty();
        mesh.Indices.ShouldBeEmpty();
        mesh.GetIndexCount().ShouldBe(3);
    }

    [Fact]
    public void Initialize_ComputesAabbFromPositions()
    {
        var vao = Substitute.For<IVertexArray>();
        var ibo = Substitute.For<IIndexBuffer>();
        ibo.Count.Returns(3);
        vao.IndexBuffer.Returns(ibo);

        var vaoFactory = Substitute.For<IVertexArrayFactory>();
        vaoFactory.Create().Returns(vao);
        var vboFactory = Substitute.For<IVertexBufferFactory>();
        vboFactory.Create(Arg.Any<List<Mesh.Vertex>>()).Returns(Substitute.For<IVertexBuffer>());
        var iboFactory = Substitute.For<IIndexBufferFactory>();
        iboFactory.Create(Arg.Any<uint[]>(), Arg.Any<int>()).Returns(ibo);

        using var mesh = new Mesh("aabb");
        mesh.Vertices.Add(new Mesh.Vertex { Position = new System.Numerics.Vector3(-1, 2, 0) });
        mesh.Vertices.Add(new Mesh.Vertex { Position = new System.Numerics.Vector3(3, -4, 5) });
        mesh.Vertices.Add(new Mesh.Vertex { Position = new System.Numerics.Vector3(0, 0, -2) });
        mesh.Indices.AddRange([0u, 1u, 2u]);

        mesh.Initialize(vaoFactory, vboFactory, iboFactory);

        mesh.BoundsMin.ShouldBe(new System.Numerics.Vector3(-1, -4, -2));
        mesh.BoundsMax.ShouldBe(new System.Numerics.Vector3(3, 2, 5));
    }

    [Fact]
    public void Dispose_DisposesVertexArray_NotBuffersDirectly()
    {
        var vao = Substitute.For<IVertexArray>();
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

        var mesh = new Mesh("test");
        mesh.Vertices.Add(default);
        mesh.Indices.AddRange([0u, 1u, 2u]);
        mesh.Initialize(vaoFactory, vboFactory, iboFactory);
        mesh.Dispose();

        vao.Received(1).Dispose();
        vbo.DidNotReceive().Dispose();
        ibo.DidNotReceive().Dispose();
    }
}
