using Engine.Renderer.Models;
using Shouldly;

namespace Engine.Tests.Renderer;

[Trait("Category", "Unit")]
public class AssimpTriangleFaceTests
{
    [Fact]
    public void AddTriangleFace_SkipsPointsAndLines_SoFollowingTrianglesStayAligned()
    {
        var indices = new List<uint>();

        AssimpModelImporter.AddTriangleFace(indices, [0u, 1u, 3u]);
        AssimpModelImporter.AddTriangleFace(indices, [1u, 2u]);
        AssimpModelImporter.AddTriangleFace(indices, [7u]);
        AssimpModelImporter.AddTriangleFace(indices, [1u, 2u, 3u]);

        indices.ShouldBe([0u, 1u, 3u, 1u, 2u, 3u]);
        (indices.Count % 3).ShouldBe(0);
    }
}
