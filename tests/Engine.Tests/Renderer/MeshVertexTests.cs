using System.Numerics;
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
}
