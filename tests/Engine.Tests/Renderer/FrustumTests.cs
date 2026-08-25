using System.Numerics;
using Engine.Renderer;
using Engine.Renderer.Buffers;
using Engine.Renderer.Buffers.VertexArray;
using Engine.Renderer.Meshes;
using NSubstitute;
using Shouldly;

namespace Engine.Tests.Renderer;

[Trait("Category", "Unit")]
public class FrustumTests
{
    private static Frustum Perspective() =>
        Frustum.FromViewProjection(Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 4f, 800f / 600f, 0.1f, 100f));

    private static Frustum Orthographic() =>
        Frustum.FromViewProjection(Matrix4x4.CreateOrthographicOffCenter(-10f, 10f, -10f, 10f, -10f, 10f));

    private static Aabb Point(Vector3 p) => new(p, p);

    [Fact]
    public void Perspective_PointInFront_IsInside_PointBehindOrPastFar_IsOutside()
    {
        var frustum = Perspective();

        frustum.Intersects(Point(new Vector3(0, 0, -5))).ShouldBeTrue();
        frustum.Intersects(Point(new Vector3(0, 0, 5))).ShouldBeFalse();
        frustum.Intersects(Point(new Vector3(0, 0, -200))).ShouldBeFalse();
    }

    [Fact]
    public void Orthographic_PointInView_IsInside_PointOutside_IsOutside()
    {
        var frustum = Orthographic();

        frustum.Intersects(Point(Vector3.Zero)).ShouldBeTrue();
        frustum.Intersects(Point(new Vector3(0, 0, 50))).ShouldBeFalse();
        frustum.Intersects(Point(new Vector3(100, 0, 0))).ShouldBeFalse();
    }

    [Fact]
    public void Aabb_FullyInside_IsVisible_FullyLeft_IsCulled_Straddling_IsVisible()
    {
        var frustum = Perspective();
        var inside = new Aabb(new Vector3(-0.5f, -0.5f, -5.5f), new Vector3(0.5f, 0.5f, -4.5f));
        var left = new Aabb(new Vector3(-100f, -0.5f, -5.5f), new Vector3(-80f, 0.5f, -4.5f));
        var straddling = new Aabb(new Vector3(-80f, -0.5f, -5.5f), new Vector3(0.5f, 0.5f, -4.5f));

        frustum.Intersects(inside).ShouldBeTrue();
        frustum.Intersects(left).ShouldBeFalse();
        frustum.Intersects(straddling).ShouldBeTrue();
    }

    [Fact]
    public void WorldTransform_OffAxisCulls_LargeScaleBringsBackIntoView()
    {
        var frustum = Perspective();
        var local = new Aabb(new Vector3(-0.5f), new Vector3(0.5f));

        var culled = local.Transform(Matrix4x4.CreateTranslation(20f, 0f, -5f));
        var scaled = local.Transform(Matrix4x4.CreateScale(80f) * Matrix4x4.CreateTranslation(20f, 0f, -5f));

        frustum.Intersects(culled).ShouldBeFalse();
        frustum.Intersects(scaled).ShouldBeTrue();
    }

    [Fact]
    public void MeshInitialize_StoresAabbFromVertexPositions()
    {
        using var mesh = CreateInitializedMesh(
            [new Vector3(1, 2, 3), new Vector3(-4, 5, -6), new Vector3(0, 0, 0)]);

        mesh.LocalAabb.ShouldNotBeNull();
        mesh.LocalAabb.Value.Min.ShouldBe(new Vector3(-4, 0, -6));
        mesh.LocalAabb.Value.Max.ShouldBe(new Vector3(1, 5, 3));
    }

    private static Mesh CreateInitializedMesh(Vector3[] positions)
    {
        var mesh = new Mesh("aabb");
        foreach (var position in positions)
            mesh.Vertices.Add(new Mesh.Vertex { Position = position });
        mesh.Indices.AddRange([0u, 1u, 2u]);

        var vao = Substitute.For<IVertexArray>();
        var vbo = Substitute.For<IVertexBuffer>();
        var ibo = Substitute.For<IIndexBuffer>();
        ibo.Count.Returns(mesh.Indices.Count);
        vao.IndexBuffer.Returns(ibo);

        var vaoFactory = Substitute.For<IVertexArrayFactory>();
        vaoFactory.Create().Returns(vao);
        var vboFactory = Substitute.For<IVertexBufferFactory>();
        vboFactory.Create(Arg.Any<List<Mesh.Vertex>>()).Returns(vbo);
        var iboFactory = Substitute.For<IIndexBufferFactory>();
        iboFactory.Create(Arg.Any<uint[]>(), Arg.Any<int>()).Returns(ibo);

        mesh.Initialize(vaoFactory, vboFactory, iboFactory);
        return mesh;
    }
}
