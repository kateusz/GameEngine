using System.Numerics;
using ECS;
using Engine.Renderer.Meshes;
using Engine.Renderer.Models;
using Engine.Scene;
using Math;
using SceneComponents;
using SceneComponents.Lighting;
using SceneComponents.Rendering;
using Serilog;

namespace Editor.Features.Scene;

public static class ModelHierarchySpawner
{
    private static readonly ILogger Logger = Log.ForContext(typeof(ModelHierarchySpawner));

    public static void SpawnChildren(
        IScene scene,
        Entity root,
        ModelSceneNode graphRoot,
        string modelPath,
        Vector4 color,
        IReadOnlyList<Mesh>? submeshes = null)
    {
        if (graphRoot.MeshIndices.Count == 1)
            SetRenderer(root, modelPath, color, graphRoot.MeshIndices[0], submeshes);
        else
            SpawnMeshChildren(scene, root, graphRoot.Name, graphRoot.MeshIndices, modelPath, color, submeshes);

        foreach (var child in graphRoot.Children)
            SpawnNode(scene, root, child, modelPath, color, submeshes);

        if (graphRoot.Light is not null)
        {
            var lamp = CreateEntity(scene, root, graphRoot.Name, Matrix4x4.Identity);
            AddLight(lamp, graphRoot.Light);
        }
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
        Vector4 color,
        IReadOnlyList<Mesh>? submeshes)
    {
        if (node.MeshIndices.Count == 0 && node.Children.Count == 0 && node.Light is null)
            return;

        if (node.MeshIndices.Count <= 1 && node.Children.Count == 0)
        {
            var leaf = CreateEntity(scene, parent, node.Name, node.LocalTransform);
            if (node.MeshIndices.Count == 1)
                SetRenderer(leaf, modelPath, color, node.MeshIndices[0], submeshes);
            AddLight(leaf, node.Light);
            return;
        }

        var host = CreateEntity(scene, parent, node.Name, node.LocalTransform);
        if (node.MeshIndices.Count == 1)
            SetRenderer(host, modelPath, color, node.MeshIndices[0], submeshes);
        else if (node.MeshIndices.Count > 1)
            SpawnMeshChildren(scene, host, node.Name, node.MeshIndices, modelPath, color, submeshes);
        AddLight(host, node.Light);

        foreach (var child in node.Children)
            SpawnNode(scene, host, child, modelPath, color, submeshes);
    }

    public static void SpawnPackedLights(IScene scene, Entity root, ModelSceneNode graphRoot)
    {
        var meshWorld = Matrix4x4.Identity;
        if (graphRoot.FirstMeshIndex is int meshIndex &&
            graphRoot.TryGetMeshWorldTransform(meshIndex, out var found))
            meshWorld = found;

        if (!Matrix4x4.Invert(meshWorld, out var inverseMesh))
        {
            Logger.Debug("Skipped packed lights because the mesh transform could not be inverted");
            return;
        }

        SpawnPackedWalk(scene, root, graphRoot, Matrix4x4.Identity, inverseMesh);
    }

    private static void SpawnPackedWalk(
        IScene scene,
        Entity root,
        ModelSceneNode node,
        Matrix4x4 parentWorld,
        Matrix4x4 inverseMesh)
    {
        var world = node.LocalTransform * parentWorld;
        if (node.Light is { } light)
        {
            var lamp = CreateEntity(scene, root, node.Name, world * inverseMesh);
            if (light is ImportedDirectionalLight sun)
            {
                light = sun with
                {
                    Direction = ModelLightConversion.BakeDirection(
                        Vector3.TransformNormal(sun.Direction, inverseMesh))
                };
            }

            AddLight(lamp, light);
        }

        foreach (var child in node.Children)
            SpawnPackedWalk(scene, root, child, world, inverseMesh);
    }

    private static void AddLight(Entity entity, ImportedLight? light)
    {
        switch (light)
        {
            case ImportedPointLight point:
                entity.AddComponent(new PointLightComponent
                {
                    Color = point.Color,
                    Intensity = point.Intensity,
                    Range = point.Range
                });
                break;
            case ImportedDirectionalLight sun:
                entity.AddComponent(new DirectionalLightComponent
                {
                    Color = sun.Color,
                    Direction = sun.Direction
                });
                break;
        }
    }

    private static void SpawnMeshChildren(
        IScene scene,
        Entity parent,
        string nodeName,
        IReadOnlyList<int> meshIndices,
        string modelPath,
        Vector4 color,
        IReadOnlyList<Mesh>? submeshes)
    {
        for (var i = 0; i < meshIndices.Count; i++)
        {
            var meshEntity = CreateEntity(scene, parent, $"{nodeName}_mesh{i}", Matrix4x4.Identity);
            SetRenderer(meshEntity, modelPath, color, meshIndices[i], submeshes);
        }
    }

    public static void ApplyLocalTransform(Entity entity, Matrix4x4 localTransform)
    {
        EnsureTransform(entity);
        if (!entity.TryGetComponent<TransformComponent>(out var transform))
            return;

        localTransform = NormalizeNodeMatrix(localTransform);
        if (localTransform == Matrix4x4.Identity)
            return;

        if (!MathHelpers.DecomposeTransform(localTransform, out var translation, out var rotation, out var scale))
            return;

        transform.Translation = translation;
        transform.Rotation = rotation;
        transform.Scale = scale;
    }

    /// <summary>
    /// Cooked runtime meshes from importer v1 stored Assimp matrices transposed, so translation
    /// landed in column 4 and decompose read it as zero.
    /// </summary>
    internal static Matrix4x4 NormalizeNodeMatrix(Matrix4x4 matrix)
    {
        var row = new Vector3(matrix.M41, matrix.M42, matrix.M43);
        var column = new Vector3(matrix.M14, matrix.M24, matrix.M34);
        if (row.LengthSquared() <= 1e-8f && column.LengthSquared() > 1e-8f)
            return Matrix4x4.Transpose(matrix);

        return matrix;
    }

    private static Entity CreateEntity(IScene scene, Entity parent, string name, Matrix4x4 localTransform)
    {
        var entity = scene.CreateEntity(name);
        EnsureTransform(entity);
        ApplyLocalTransform(entity, localTransform);
        scene.SetParent(entity, parent);
        return entity;
    }

    private static void SetRenderer(
        Entity entity,
        string modelPath,
        Vector4 color,
        int meshIndex,
        IReadOnlyList<Mesh>? submeshes)
    {
        EnsureTransform(entity);

        ModelRendererComponent renderer;
        if (entity.TryGetComponent<ModelRendererComponent>(out var existing))
        {
            renderer = existing;
            renderer.ModelPath = modelPath;
            renderer.MeshIndex = meshIndex;
            renderer.Color = color;
        }
        else
        {
            renderer = new ModelRendererComponent(color)
            {
                ModelPath = modelPath,
                MeshIndex = meshIndex
            };
            entity.AddComponent(renderer);
        }

        if (submeshes != null && (uint)meshIndex < (uint)submeshes.Count)
        {
            var mesh = submeshes[meshIndex];
            renderer.Metallic = mesh.MetallicFactor;
            renderer.Roughness = mesh.RoughnessFactor;
            renderer.FactorsSeeded = true;
        }

        var pivot = MeshCenter(submeshes, meshIndex);
        renderer.Pivot = pivot;
        if (pivot == Vector3.Zero || !entity.TryGetComponent<TransformComponent>(out var transform))
            return;

        ApplyLocalTransform(entity, Matrix4x4.CreateTranslation(pivot) * transform.GetTransform());
    }

    private static Vector3 MeshCenter(IReadOnlyList<Mesh>? submeshes, int meshIndex)
    {
        if (submeshes == null || (uint)meshIndex >= (uint)submeshes.Count)
            return Vector3.Zero;

        if (submeshes[meshIndex].Bounds is not { } bounds)
            return Vector3.Zero;

        var center = (bounds.Min + bounds.Max) * 0.5f;
        return center.LengthSquared() <= 1e-8f ? Vector3.Zero : center;
    }

    private static void EnsureTransform(Entity entity)
    {
        if (!entity.HasComponent<TransformComponent>())
            entity.AddComponent<TransformComponent>();
    }
}
