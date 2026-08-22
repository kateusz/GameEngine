using System.Numerics;
using System.Runtime.CompilerServices;
using Engine.Renderer.Meshes;
using Shouldly;

namespace Engine.Tests.Renderer;

[Trait("Category", "Unit")]
public class MeshVertexTests
{
    [Fact]
    public void ObjectInitializer_KeepsEntityIdSentinelMinusOne()
    {
        var vertex = new Mesh.Vertex { Position = Vector3.UnitX };

        vertex.EntityId.ShouldBe(-1);
    }

    [Fact]
    public void PrimaryConstructor_DefaultEntityIdIsMinusOne()
    {
        var vertex = new Mesh.Vertex(Vector3.Zero, Vector3.UnitY, Vector2.Zero, Vector3.UnitX, Vector3.UnitZ);

        vertex.EntityId.ShouldBe(-1);
    }

    [Fact]
    public void LayoutStride_MatchesUnsafeSizeOf()
    {
        Unsafe.SizeOf<Mesh.Vertex>().ShouldBe(Mesh.Vertex.Layout.Stride);
        Mesh.Vertex.Layout.Stride.ShouldBe(60);
        Mesh.Vertex.Layout.Elements[0].Offset.ShouldBe(0);
        Mesh.Vertex.Layout.Elements[1].Offset.ShouldBe(12);
        Mesh.Vertex.Layout.Elements[2].Offset.ShouldBe(24);
        Mesh.Vertex.Layout.Elements[3].Offset.ShouldBe(32);
        Mesh.Vertex.Layout.Elements[4].Offset.ShouldBe(44);
        Mesh.Vertex.Layout.Elements[5].Offset.ShouldBe(56);
    }
}
