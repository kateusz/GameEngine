using System.Numerics;
using ECS;
using Engine.Renderer.Models;
using Engine.Scene;
using Math;
using SceneComponents;
using SceneComponents.Rendering;

namespace Editor.Features.Scene;

public static class ModelHierarchySpawner
{
    public static void SpawnChildren(
        IScene scene,
        Entity root,
        ModelSceneNode graphRoot,
        string modelPath,
        Vector4 color)
    {
        if (graphRoot.MeshIndices.Count == 1)
            SetRenderer(root, modelPath, color, graphRoot.MeshIndices[0]);
        else
            SpawnMeshChildren(scene, root, graphRoot.Name, graphRoot.MeshIndices, modelPath, color);

        foreach (var child in graphRoot.Children)
            SpawnNode(scene, root, child, modelPath, color);
    }

    public static void DestroyChildren(IScene scene, Entity root)
    {
        foreach (var child in scene.GetChildren(root).ToList())
            scene.DestroyEntity(child);
    }

    private static void SpawnNode(
        IScene scene,
        Entity parent,
        ModelSceneNode node,
        string modelPath,
        Vector4 color)
    {
        if (node.MeshIndices.Count == 0 && node.Children.Count == 0)
            return;

        if (node.MeshIndices.Count <= 1 && node.Children.Count == 0)
        {
            var leaf = CreateEntity(scene, parent, node.Name, node.LocalTransform);
            if (node.MeshIndices.Count == 1)
                SetRenderer(leaf, modelPath, color, node.MeshIndices[0]);
            return;
        }

        var host = CreateEntity(scene, parent, node.Name, node.LocalTransform);
        if (node.MeshIndices.Count == 1)
            SetRenderer(host, modelPath, color, node.MeshIndices[0]);
        else if (node.MeshIndices.Count > 1)
            SpawnMeshChildren(scene, host, node.Name, node.MeshIndices, modelPath, color);

        foreach (var child in node.Children)
            SpawnNode(scene, host, child, modelPath, color);
    }

    private static void SpawnMeshChildren(
        IScene scene,
        Entity parent,
        string nodeName,
        IReadOnlyList<int> meshIndices,
        string modelPath,
        Vector4 color)
    {
        for (var i = 0; i < meshIndices.Count; i++)
        {
            var meshEntity = CreateEntity(scene, parent, $"{nodeName}_mesh{i}", Matrix4x4.Identity);
            SetRenderer(meshEntity, modelPath, color, meshIndices[i]);
        }
    }

    public static void ApplyLocalTransform(Entity entity, Matrix4x4 localTransform)
    {
        EnsureTransform(entity);
        if (!entity.TryGetComponent<TransformComponent>(out var transform))
            return;

        if (localTransform == Matrix4x4.Identity)
            return;

        if (!MathHelpers.DecomposeTransform(localTransform, out var translation, out var rotation, out var scale))
            return;

        transform.Translation = translation;
        transform.Rotation = rotation;
        transform.Scale = scale;
    }

    private static Entity CreateEntity(IScene scene, Entity parent, string name, Matrix4x4 localTransform)
    {
        var entity = scene.CreateEntity(name);
        EnsureTransform(entity);
        ApplyLocalTransform(entity, localTransform);
        scene.SetParent(entity, parent);
        return entity;
    }

    private static void SetRenderer(Entity entity, string modelPath, Vector4 color, int meshIndex)
    {
        EnsureTransform(entity);

        if (entity.TryGetComponent<ModelRendererComponent>(out var renderer))
        {
            renderer.ModelPath = modelPath;
            renderer.MeshIndex = meshIndex;
            renderer.Color = color;
            return;
        }

        entity.AddComponent(new ModelRendererComponent(color)
        {
            ModelPath = modelPath,
            MeshIndex = meshIndex
        });
    }

    private static void EnsureTransform(Entity entity)
    {
        if (!entity.HasComponent<TransformComponent>())
            entity.AddComponent<TransformComponent>();
    }
}
