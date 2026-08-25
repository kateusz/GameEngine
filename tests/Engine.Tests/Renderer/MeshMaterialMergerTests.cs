using System.Numerics;
using Engine.Renderer.Meshes;
using Engine.Renderer.Models;
using Shouldly;

namespace Engine.Tests.Renderer;

[Trait("Category", "Unit")]
public class MeshMaterialMergerTests
{
    private static MeshMaterialMerger.ModelMaterialInfo Material(string diffuse, float shininess = 32f) =>
        new(diffuse, null, null, shininess);

    [Fact]
    public void Merge_SameMaterial_CombinesMeshesAndRemapsIndices()
    {
        var meshA = CreateMesh("A",
            [new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(0, 1, 0)],
            [0u, 1u, 2u]);
        var meshB = CreateMesh("B",
            [new Vector3(2, 0, 0), new Vector3(3, 0, 0), new Vector3(2, 1, 0)],
            [0u, 1u, 2u]);
        var material = Material("textures/a.png");

        var merged = MeshMaterialMerger.Merge(
            [meshA, meshB],
            sceneGraph: null,
            [material, material]);

        merged.Count.ShouldBe(1);
        merged[0].Vertices.Count.ShouldBe(6);
        merged[0].Indices.ShouldBe([0u, 1u, 2u, 3u, 4u, 5u]);
    }

    [Fact]
    public void Merge_DifferentDiffusePaths_ProducesSeparateMeshes()
    {
        var meshA = CreateMesh("A",
            [new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(0, 1, 0)],
            [0u, 1u, 2u]);
        var meshB = CreateMesh("B",
            [new Vector3(2, 0, 0), new Vector3(3, 0, 0), new Vector3(2, 1, 0)],
            [0u, 1u, 2u]);

        var merged = MeshMaterialMerger.Merge(
            [meshA, meshB],
            sceneGraph: null,
            [Material("textures/a.png"), Material("textures/b.png")]);

        merged.Count.ShouldBe(2);
    }

    [Fact]
    public void Merge_ChildNodeTranslation_BakesIntoVertexPosition()
    {
        var mesh = CreateMesh("Leaf",
            [new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(0, 1, 0)],
            [0u, 1u, 2u]);
        var graph = new ModelSceneNode(
            "Root",
            [],
            [new ModelSceneNode("Child", [0], [], Matrix4x4.CreateTranslation(10, 0, 0))]);

        var merged = MeshMaterialMerger.Merge(
            [mesh],
            graph,
            [Material("textures/a.png")]);

        merged.Count.ShouldBe(1);
        merged[0].Vertices[0].Position.ShouldBe(new Vector3(10, 0, 0));
    }

    private static Mesh CreateMesh(string name, IReadOnlyList<Vector3> positions, IReadOnlyList<uint> indices)
    {
        var mesh = new Mesh(name);
        foreach (var position in positions)
        {
            mesh.Vertices.Add(new Mesh.Vertex
            {
                Position = position,
                Normal = Vector3.UnitY
            });
        }

        foreach (var index in indices)
            mesh.Indices.Add(index);

        return mesh;
    }
}
