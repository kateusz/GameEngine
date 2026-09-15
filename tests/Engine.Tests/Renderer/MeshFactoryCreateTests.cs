using System.Numerics;
using Engine.Renderer.Buffers;
using Engine.Renderer.Buffers.VertexArray;
using Engine.Renderer.Meshes;
using NSubstitute;
using Shouldly;

namespace Engine.Tests.Renderer;

[Trait("Category", "Unit")]
public class MeshFactoryCreateTests
{
    [Fact]
    public void Create_UploadsVerticesAndDropsCpuCopy()
    {
        var ibo = Substitute.For<IIndexBuffer>();
        ibo.Count.Returns(3);
        var vao = Substitute.For<IVertexArray>();
        vao.IndexBuffer.Returns(ibo);

        var vaoFactory = Substitute.For<IVertexArrayFactory>();
        vaoFactory.Create().Returns(vao);
        var vboFactory = Substitute.For<IVertexBufferFactory>();
        vboFactory.Create(Arg.Any<List<Mesh.Vertex>>()).Returns(Substitute.For<IVertexBuffer>());
        var iboFactory = Substitute.For<IIndexBufferFactory>();
        iboFactory.Create(Arg.Any<uint[]>(), Arg.Any<int>()).Returns(ibo);

        var factory = new MeshFactory(vaoFactory, vboFactory, iboFactory);
        var mesh = factory.Create("tri",
            [new Mesh.Vertex { Position = Vector3.UnitX }],
            [0u, 0u, 0u]);

        mesh.Name.ShouldBe("tri");
        mesh.Vertices.ShouldBeEmpty();
        mesh.GetIndexCount().ShouldBe(3);
        mesh.BoundsMin.ShouldBe(Vector3.UnitX);
        mesh.BoundsMax.ShouldBe(Vector3.UnitX);
    }
}
