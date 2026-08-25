using System.Numerics;
using Engine.Renderer.Meshes;

namespace Engine.Renderer.Models;

internal static class MeshMaterialMerger
{
    internal readonly record struct ModelMaterialInfo(
        string? DiffusePath,
        string? SpecularPath,
        string? NormalPath,
        float Shininess);

    public static List<Mesh> Merge(
        IReadOnlyList<Mesh> submeshes,
        ModelSceneNode? sceneGraph,
        IReadOnlyList<ModelMaterialInfo> materialInfos)
    {
        if (submeshes.Count == 0)
            return [];

        var instances = CollectMeshInstances(sceneGraph, submeshes.Count);
        var groups = new Dictionary<ModelMaterialInfo, Mesh>();

        foreach (var (meshIndex, world) in instances)
        {
            var source = submeshes[meshIndex];
            var key = materialInfos[meshIndex];

            if (!groups.TryGetValue(key, out var dest))
            {
                dest = new Mesh("Merged");
                dest.Shininess = key.Shininess;
                dest.DiffuseTexture = source.DiffuseTexture;
                dest.SpecularTexture = source.SpecularTexture;
                dest.NormalTexture = source.NormalTexture;
                groups[key] = dest;
            }

            AppendBakedMesh(dest, source, world);
        }

        return [.. groups.Values];
    }

    private static List<(int MeshIndex, Matrix4x4 World)> CollectMeshInstances(
        ModelSceneNode? sceneGraph,
        int meshCount)
    {
        var instances = new List<(int, Matrix4x4)>();
        if (sceneGraph == null)
        {
            for (var i = 0; i < meshCount; i++)
                instances.Add((i, Matrix4x4.Identity));
            return instances;
        }

        CollectNodeInstances(sceneGraph, Matrix4x4.Identity, isRoot: true, instances);
        return instances;
    }

    private static void CollectNodeInstances(
        ModelSceneNode node,
        Matrix4x4 parentWorld,
        bool isRoot,
        List<(int MeshIndex, Matrix4x4 World)> instances)
    {
        var world = isRoot ? Matrix4x4.Identity : node.LocalTransform * parentWorld;

        foreach (var meshIndex in node.MeshIndices)
            instances.Add((meshIndex, world));

        foreach (var child in node.Children)
            CollectNodeInstances(child, world, isRoot: false, instances);
    }

    private static void AppendBakedMesh(Mesh dest, Mesh source, Matrix4x4 world)
    {
        var baseVertex = dest.Vertices.Count;
        foreach (var v in source.Vertices)
        {
            dest.Vertices.Add(new Mesh.Vertex
            {
                Position = Vector3.Transform(v.Position, world),
                Normal = TransformDirection(v.Normal, world),
                TexCoord = v.TexCoord,
                Tangent = TransformDirection(v.Tangent, world),
                Bitangent = TransformDirection(v.Bitangent, world),
                EntityId = v.EntityId
            });
        }

        foreach (var index in source.Indices)
            dest.Indices.Add(index + (uint)baseVertex);
    }

    private static Vector3 TransformDirection(Vector3 direction, Matrix4x4 world)
    {
        if (direction.LengthSquared() < 1e-12f)
            return direction;

        return Vector3.Normalize(Vector3.TransformNormal(direction, world));
    }
}
