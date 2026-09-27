using System.Numerics;
using Engine.Renderer.Meshes;
using Engine.Renderer.Models;
using Engine.Renderer.Models.RuntimeMesh;
using Shouldly;

namespace Engine.Tests.Renderer;

[Trait("Category", "Unit")]
public class RuntimeMeshTests
{
    [Fact]
    public void Serialize_IsDeterministic_ForSameCpuModel()
    {
        var model = SampleModel();
        var stamp = new RuntimeMeshStamp(1, new byte[32], RuntimeMeshFormat.ImporterVersion, RuntimeMeshFormat.AssimpPostProcessFlags);

        var first = RuntimeMeshWriter.Serialize(model, stamp);
        var second = RuntimeMeshWriter.Serialize(model, stamp);

        first.ShouldBe(second);
    }

    [Fact]
    public void RoundTrip_PreservesGeometryAndGraph()
    {
        var model = SampleModel();
        var stamp = new RuntimeMeshStamp(42, Enumerable.Range(0, 32).Select(i => (byte)i).ToArray(),
            RuntimeMeshFormat.ImporterVersion, RuntimeMeshFormat.AssimpPostProcessFlags);
        var bytes = RuntimeMeshWriter.Serialize(model, stamp);

        RuntimeMeshReader.TryRead(bytes, out var read).ShouldBeTrue();
        read.ShouldNotBeNull();
        read!.Submeshes.Count.ShouldBe(1);
        read.Submeshes[0].Vertices.Count.ShouldBe(3);
        read.Submeshes[0].Indices.ShouldBe([0u, 1u, 2u]);
        read.Nodes.Count.ShouldBe(2);
        read.Nodes[0].ChildIndices.ShouldBe([1]);
        read.Nodes[1].MeshIndices.ShouldBe([0]);
    }

    [Fact]
    public void RoundTrip_KeepsDistinctVertices_WhenOnlyUvDiffers()
    {
        var model = SampleModel();
        var adjusted = model.Submeshes[0].Vertices[1];
        adjusted.TexCoord = new Vector2(1f, 0f);
        model.Submeshes[0].Vertices[1] = adjusted;
        var stamp = new RuntimeMeshStamp(1, new byte[32], RuntimeMeshFormat.ImporterVersion, RuntimeMeshFormat.AssimpPostProcessFlags);
        var bytes = RuntimeMeshWriter.Serialize(model, stamp);

        RuntimeMeshReader.TryRead(bytes, out var read).ShouldBeTrue();
        read!.Submeshes[0].Vertices[0].TexCoord.ShouldBe(Vector2.Zero);
        read.Submeshes[0].Vertices[1].TexCoord.ShouldBe(new Vector2(1f, 0f));
    }

    [Fact]
    public void TryRead_RejectsTruncatedFile()
    {
        var model = SampleModel();
        var bytes = RuntimeMeshWriter.Serialize(model, new RuntimeMeshStamp(1, new byte[32],
            RuntimeMeshFormat.ImporterVersion, RuntimeMeshFormat.AssimpPostProcessFlags));
        RuntimeMeshReader.TryRead(bytes.AsSpan(0, 64), out _).ShouldBeFalse();
    }

    [Fact]
    public void TryRead_RejectsBadIndex()
    {
        var model = SampleModel();
        model.Submeshes[0].Indices[2] = 99;
        var bytes = RuntimeMeshWriter.Serialize(model, new RuntimeMeshStamp(1, new byte[32],
            RuntimeMeshFormat.ImporterVersion, RuntimeMeshFormat.AssimpPostProcessFlags));
        RuntimeMeshReader.TryRead(bytes, out _).ShouldBeFalse();
    }

    [Fact]
    public void TryRead_Accepts_MaxUshortIndex()
    {
        var vertices = new List<Mesh.Vertex>(65536);
        for (var i = 0; i < 65536; i++)
            vertices.Add(new Mesh.Vertex { Position = new Vector3(i, 0f, 0f) });

        var submesh = new SourceSubmesh
        {
            Name = "big",
            BoundsMin = new Vector3(0f, 0f, 0f),
            BoundsMax = new Vector3(65535f, 0f, 0f)
        };
        submesh.Vertices.AddRange(vertices);
        submesh.Indices.AddRange([0u, 1u, 65535u]);

        var model = new SourceModel(
            [submesh],
            [new SourceNode { Name = "root" }],
            []);
        var bytes = RuntimeMeshWriter.Serialize(model, new RuntimeMeshStamp(1, new byte[32],
            RuntimeMeshFormat.ImporterVersion, RuntimeMeshFormat.AssimpPostProcessFlags));

        RuntimeMeshReader.TryRead(bytes, out _).ShouldBeTrue();
    }

    private static SourceModel SampleModel()
    {
        var submesh = new SourceSubmesh
        {
            Name = "tri",
            BoundsMin = Vector3.Zero,
            BoundsMax = new Vector3(1f, 1f, 0f),
            Metallic = 0.25f,
            Roughness = 0.5f,
            BaseColorFactor = Vector3.One
        };
        submesh.Vertices.Add(new Mesh.Vertex { Position = Vector3.Zero });
        submesh.Vertices.Add(new Mesh.Vertex { Position = Vector3.UnitX });
        submesh.Vertices.Add(new Mesh.Vertex { Position = Vector3.UnitY });
        submesh.Indices.AddRange([0u, 1u, 2u]);

        return new SourceModel(
            [submesh],
            [
                new SourceNode { Name = "root", ChildIndices = { 1 } },
                new SourceNode { Name = "mesh", MeshIndices = { 0 } }
            ],
            []);
    }
}
