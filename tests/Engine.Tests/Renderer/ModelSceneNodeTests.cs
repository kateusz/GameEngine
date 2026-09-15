using System.Numerics;
using Engine.Renderer.Models;
using Shouldly;

namespace Engine.Tests.Renderer;

public class ModelSceneNodeTests
{
    private static ModelSceneNode Node(
        string name,
        IReadOnlyList<int> meshIndices,
        params ModelSceneNode[] children) =>
        new(name, meshIndices, children);

    private static ModelSceneNode Node(
        string name,
        IReadOnlyList<int> meshIndices,
        Matrix4x4 localTransform,
        params ModelSceneNode[] children) =>
        new(name, meshIndices, children, localTransform);

    [Fact]
    public void ShouldUnpack_SingleMesh_ReturnsFalse()
    {
        Node("root", [0]).ShouldUnpack.ShouldBeFalse();
    }

    [Fact]
    public void ShouldUnpack_TwoMeshes_ReturnsTrue()
    {
        var room = Node("Room", [], Node("Chair", [0]), Node("Table", [1]));
        room.ShouldUnpack.ShouldBeTrue();
        room.TotalMeshCount.ShouldBe(2);
    }

    [Fact]
    public void TryGetMeshWorldTransform_DeepChild_ReturnsAccumulatedTransform()
    {
        var graph = Node("Root", [],
            localTransform: Matrix4x4.CreateTranslation(1, 0, 0),
            Node("Chair", [0], Matrix4x4.CreateTranslation(2, 0, 0)));

        graph.TryGetMeshWorldTransform(0, out var world).ShouldBeTrue();
        new Vector3(world.M41, world.M42, world.M43).ShouldBe(new Vector3(3, 0, 0));
    }

    [Fact]
    public void PackedSubmeshWorld_MultipliesNodeWorldByEntityWorld()
    {
        var graph = Node("Root", [],
            localTransform: Matrix4x4.CreateTranslation(1, 0, 0),
            Node("Chair", [0], Matrix4x4.CreateTranslation(2, 0, 0)));
        var entity = Matrix4x4.CreateTranslation(10, 0, 0);

        var packed = ModelSceneNode.PackedSubmeshWorld(graph, 0, entity);
        new Vector3(packed.M41, packed.M42, packed.M43).ShouldBe(new Vector3(13, 0, 0));
    }

    [Fact]
    public void PackedSubmeshWorld_NullGraph_ReturnsEntityWorld()
    {
        var entity = Matrix4x4.CreateTranslation(4, 5, 6);
        ModelSceneNode.PackedSubmeshWorld(null, 0, entity).ShouldBe(entity);
    }
}
