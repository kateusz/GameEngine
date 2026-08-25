using System.Numerics;

namespace Engine.Renderer.Models;

/// <summary>
/// Hierarchy node from an imported model file (Assimp scene graph).
/// </summary>
public sealed class ModelSceneNode
{
    public ModelSceneNode(
        string name,
        IReadOnlyList<int> meshIndices,
        IReadOnlyList<ModelSceneNode> children,
        Matrix4x4? localTransform = null)
    {
        Name = name;
        MeshIndices = meshIndices;
        Children = children;
        LocalTransform = localTransform ?? Matrix4x4.Identity;
    }

    public string Name { get; }
    public IReadOnlyList<int> MeshIndices { get; }
    public IReadOnlyList<ModelSceneNode> Children { get; }
    public Matrix4x4 LocalTransform { get; }

    /// <summary>Accumulated transform from the graph root to the node that owns <paramref name="meshIndex"/>.</summary>
    public bool TryGetMeshWorldTransform(int meshIndex, out Matrix4x4 worldFromRoot)
    {
        return TryGetMeshWorldTransform(meshIndex, Matrix4x4.Identity, out worldFromRoot);
    }

    private bool TryGetMeshWorldTransform(int meshIndex, Matrix4x4 parentWorld, out Matrix4x4 worldFromRoot)
    {
        var world = LocalTransform * parentWorld;
        if (MeshIndices.Contains(meshIndex))
        {
            worldFromRoot = world;
            return true;
        }

        foreach (var child in Children)
        {
            if (child.TryGetMeshWorldTransform(meshIndex, world, out worldFromRoot))
                return true;
        }

        worldFromRoot = default;
        return false;
    }

    public int TotalMeshCount => MeshIndices.Count + Children.Sum(c => c.TotalMeshCount);

    public int? FirstMeshIndex
    {
        get
        {
            if (MeshIndices.Count > 0)
                return MeshIndices[0];

            foreach (var child in Children)
            {
                if (child.FirstMeshIndex is int index)
                    return index;
            }

            return null;
        }
    }

    public bool ShouldUnpack => TotalMeshCount > 1;
}
