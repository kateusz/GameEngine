using System.Numerics;
using Engine.Renderer.Buffers;
using Engine.Renderer.Buffers.VertexArray;
using Engine.Renderer.Meshes;
using Engine.Renderer.Pipeline;
using Engine.Scene;
using NSubstitute;
using Shouldly;

namespace Engine.Tests.Scene;

[Trait("Category", "Unit")]
public class MeshLodTests
{
    [Fact]
    public void Select_DistanceZero_ReturnsFullList()
    {
        var mesh = MeshWithErrors();

        MeshLod.Select(mesh, Matrix4x4.Identity, Vector3.Zero, View(), 1f).ShouldBe(0);
    }

    [Fact]
    public void Select_LimitBetweenErrors_ReturnsTheLevelThatStillFits()
    {
        var mesh = MeshWithErrors();
        var between = MeshLod.Select(mesh, Matrix4x4.Identity, new Vector3(0f, 0f, 2f), View(), 1f);
        var tighter = MeshLod.Select(mesh, Matrix4x4.Identity, new Vector3(0f, 0f, 1.4f), View(), 1f);

        between.ShouldBe(1);
        tighter.ShouldBe(0);
    }

    [Fact]
    public void Select_HigherBias_KeepsAFinerLevel()
    {
        var mesh = MeshWithErrors();
        var camera = new Vector3(0f, 0f, 1.6f);

        var biased = MeshLod.Select(mesh, Matrix4x4.Identity, camera, View(), 2f);
        var normal = MeshLod.Select(mesh, Matrix4x4.Identity, camera, View(), 1f);

        biased.ShouldBeLessThan(normal);
    }

    [Fact]
    public void Select_Orthographic_IgnoresCameraDistance()
    {
        var mesh = MeshWithErrors();
        var ortho = View(ortho: true);

        MeshLod.Select(mesh, Matrix4x4.Identity, Vector3.Zero, ortho, 1f)
            .ShouldBe(MeshLod.Select(mesh, Matrix4x4.Identity, new Vector3(0f, 0f, 40f), ortho, 1f));
        MeshLod.Select(mesh, Matrix4x4.Identity, Vector3.Zero, ortho, 1f).ShouldBe(1);
    }

    [Fact]
    public void Select_UnusableViewOrScale_ReturnsFullList()
    {
        var mesh = MeshWithErrors();
        var broken = Matrix4x4.Identity;
        broken.M11 = float.NaN;

        MeshLod.Select(mesh, Matrix4x4.Identity, new Vector3(0f, 0f, 2f), View(height: 0f), 1f).ShouldBe(0);
        MeshLod.Select(new Mesh("plain"), Matrix4x4.Identity, Vector3.Zero, View(), 1f).ShouldBe(0);
        MeshLod.Select(mesh, broken, new Vector3(0f, 0f, 2f), View(), 1f).ShouldBe(0);
    }

    private static SceneView View(float height = 100f, bool ortho = false) =>
        new(Matrix4x4.Identity, ViewportHeight: height, FrustumHeightAtUnit: 2f, Orthographic: ortho);

    private static Mesh MeshWithErrors()
    {
        var mesh = new Mesh("lod");
        mesh.Vertices.Add(new Mesh.Vertex(new Vector3(-1f, -1f, -1f), Vector3.UnitY, Vector2.Zero, Vector3.UnitX, Vector3.UnitZ));
        mesh.Vertices.Add(new Mesh.Vertex(new Vector3(1f, 1f, 1f), Vector3.UnitY, Vector2.Zero, Vector3.UnitX, Vector3.UnitZ));
        mesh.Indices.AddRange([0u, 1u, 0u]);
        mesh.LodIndices.Add([0u, 1u, 0u]);
        mesh.LodIndices.Add([0u, 1u, 0u]);
        mesh.LodErrors.Add(0.01f);
        mesh.LodErrors.Add(0.05f);

        var vertexArrays = Substitute.For<IVertexArrayFactory>();
        vertexArrays.Create().Returns(_ =>
        {
            var vertexArray = Substitute.For<IVertexArray>();
            IIndexBuffer indexBuffer = null!;
            vertexArray.IndexBuffer.Returns(_ => indexBuffer);
            vertexArray.When(array => array.SetIndexBuffer(Arg.Any<IIndexBuffer>()))
                .Do(call => indexBuffer = call.Arg<IIndexBuffer>());
            return vertexArray;
        });
        var vertexBuffers = Substitute.For<IVertexBufferFactory>();
        vertexBuffers.Create(Arg.Any<List<Mesh.Vertex>>()).Returns(_ => Substitute.For<IVertexBuffer>());
        var indexBuffers = Substitute.For<IIndexBufferFactory>();
        indexBuffers.Create(Arg.Any<uint[]>(), Arg.Any<int>()).Returns(call =>
        {
            var indexBuffer = Substitute.For<IIndexBuffer>();
            indexBuffer.Count.Returns(call.Arg<int>());
            return indexBuffer;
        });
        mesh.Initialize(vertexArrays, vertexBuffers, indexBuffers);
        return mesh;
    }
}
