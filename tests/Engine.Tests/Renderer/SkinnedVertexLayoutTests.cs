using System.Runtime.InteropServices;
using Engine.Renderer;
using Engine.Renderer.Animation;
using Shouldly;

namespace Engine.Tests.Renderer;

public class SkinnedVertexLayoutTests
{
    [Fact]
    public void Vertex_GetSize_MatchesMarshalSizeOf()
    {
        // Crash class: CPU stride != GL BufferLayout stride → garbage bone ids → GPU fault.
        Marshal.SizeOf<Mesh.Vertex>().ShouldBe(Mesh.Vertex.GetSize());
        Mesh.Vertex.GetSize().ShouldBe(92);
    }

    [Fact]
    public void MaxBones_LeavesHeadroomBelowGl33MinimumVertexUniforms()
    {
        // GL 3.3 guarantees only 1024 vertex uniform components. Each mat4 = 16.
        // lightingShader also needs u_ViewProjection/u_Model/u_NormalMatrix (+ scalars).
        var boneComponents = Skeleton.MaxBones * 16;
        boneComponents.ShouldBeLessThan(1024);
        (1024 - boneComponents).ShouldBeGreaterThanOrEqualTo(64);
    }
}
