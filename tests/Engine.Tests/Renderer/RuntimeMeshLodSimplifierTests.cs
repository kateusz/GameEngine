using System.Numerics;
using Engine.Renderer.Meshes;
using Engine.Renderer.Models.RuntimeMesh;
using Shouldly;

namespace Engine.Tests.Renderer;

[Trait("Category", "Unit")]
public class RuntimeMeshLodSimplifierTests
{
    [Fact]
    public void Simplify_Grid_StoresShorterInRangeLevelsWithGrowingError()
    {
        var submesh = Grid(8);

        RuntimeMeshLodSimplifier.Simplify(submesh);

        submesh.LodIndices.Count.ShouldBeGreaterThan(1);
        submesh.LodErrors.Count.ShouldBe(submesh.LodIndices.Count);
        var previous = submesh.Indices.Count;
        for (var level = 0; level < submesh.LodIndices.Count; level++)
        {
            var indices = submesh.LodIndices[level];
            indices.Length.ShouldBeLessThan(previous);
            (indices.Length % 3).ShouldBe(0);
            indices.ShouldAllBe(index => index < (uint)submesh.Vertices.Count);
            previous = indices.Length;
            if (level > 0)
                submesh.LodErrors[level].ShouldBeGreaterThanOrEqualTo(submesh.LodErrors[level - 1] * 1.5f);
        }

        submesh.LodErrors[0].ShouldBeGreaterThan(0f);
    }

    [Fact]
    public void Simplify_StepThatCannotCutAQuarter_StoresNoLevel()
    {
        var submesh = new SourceSubmesh
        {
            Name = "soup",
            BoundsMin = Vector3.Zero,
            BoundsMax = new Vector3(18f, 1f, 0f)
        };
        for (var i = 0; i < 6; i++)
        {
            var x = i * 3f;
            submesh.Vertices.Add(new Mesh.Vertex { Position = new Vector3(x, 0f, 0f), Normal = Vector3.UnitZ });
            submesh.Vertices.Add(new Mesh.Vertex { Position = new Vector3(x + 1f, 0f, 0f), Normal = Vector3.UnitZ });
            submesh.Vertices.Add(new Mesh.Vertex { Position = new Vector3(x, 1f, 0f), Normal = Vector3.UnitZ });
            var start = (uint)(i * 3);
            submesh.Indices.AddRange([start, start + 1, start + 2]);
        }

        RuntimeMeshLodSimplifier.Simplify(submesh);

        submesh.LodIndices.ShouldBeEmpty();
    }

    private static SourceSubmesh Grid(int cells)
    {
        var edge = cells + 1;
        var submesh = new SourceSubmesh
        {
            Name = "grid",
            BoundsMin = Vector3.Zero,
            BoundsMax = new Vector3(cells, 0f, cells)
        };
        for (var z = 0; z < edge; z++)
        for (var x = 0; x < edge; x++)
        {
            submesh.Vertices.Add(new Mesh.Vertex
            {
                Position = new Vector3(x, 0f, z),
                Normal = Vector3.UnitY,
                TexCoord = new Vector2((float)x / cells, (float)z / cells)
            });
        }

        for (var z = 0; z < cells; z++)
        for (var x = 0; x < cells; x++)
        {
            var i = (uint)(z * edge + x);
            var row = (uint)edge;
            submesh.Indices.AddRange([i, i + 1, i + row, i + 1, i + row + 1, i + row]);
        }

        return submesh;
    }
}
