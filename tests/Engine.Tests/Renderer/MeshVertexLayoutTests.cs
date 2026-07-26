using Engine.Renderer;
using Shouldly;

namespace Engine.Tests.Renderer;

[Trait("Category", "Unit")]
public class MeshVertexLayoutTests
{
    [Fact]
    public void CreateVertexLayout_Stride_MatchesPackedVertexSize()
    {
        // Entity ID is a per-draw uniform in 3D shaders; Mesh.Vertex has no EntityId field.
        // If layout stride exceeds packed size, GL reads garbage positions (vertex explosion).
        Mesh.CreateVertexLayout().Stride.ShouldBe(Mesh.Vertex.GetSize());
    }
}
