using System.Numerics;
using System.Runtime.CompilerServices;
using Engine.Renderer.Meshes;
using Shouldly;

namespace Engine.Tests.Renderer;

[Trait("Category", "Unit")]
public class MeshVertexTests
{
    [Fact]
    public void LayoutStride_MatchesUnsafeSizeOf_WithoutEntityId()
    {
        Unsafe.SizeOf<Mesh.Vertex>().ShouldBe(Mesh.Vertex.Layout.Stride);
        Mesh.Vertex.Layout.Stride.ShouldBe(56);
        Mesh.Vertex.Layout.Elements.Count.ShouldBe(5);
        Mesh.Vertex.Layout.Elements[0].Offset.ShouldBe(0);
        Mesh.Vertex.Layout.Elements[1].Offset.ShouldBe(12);
        Mesh.Vertex.Layout.Elements[2].Offset.ShouldBe(24);
        Mesh.Vertex.Layout.Elements[3].Offset.ShouldBe(32);
        Mesh.Vertex.Layout.Elements[4].Offset.ShouldBe(44);
    }

    [Fact]
    public void ObjectInitializer_LeavesUnspecifiedFieldsDefault()
    {
        var vertex = new Mesh.Vertex { Position = Vector3.UnitX };

        vertex.Position.ShouldBe(Vector3.UnitX);
        vertex.Normal.ShouldBe(Vector3.Zero);
    }
}
