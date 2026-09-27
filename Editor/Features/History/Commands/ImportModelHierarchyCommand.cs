using ECS;
using Editor.Features.Scene;
using Engine.Renderer.Models;
using Engine.Scene;
using SceneComponents;
using SceneComponents.Rendering;

namespace Editor.Features.History.Commands;

public sealed class ImportModelHierarchyCommand(
    IScene scene,
    Entity root,
    ModelRendererComponent component,
    Model model,
    string relativeModelPath) : IUndoCommand
{
    private List<EntitySubtreeSnapshot>? _oldChildren;
    private string? _oldModelPath;
    private int? _oldMeshIndex;
    private bool _oldSuppressDraw;

    public bool Execute()
    {
        if (_oldChildren == null)
        {
            _oldModelPath = component.ModelPath;
            _oldMeshIndex = component.MeshIndex;
            _oldSuppressDraw = component.SuppressDraw;
            _oldChildren = EntitySubtreeSnapshot.CaptureChildren(scene, root);
        }

        var visibilityZones = CaptureVisibilityZones(scene, root);

        ModelHierarchySpawner.DestroyChildren(scene, root);
        component.ModelPath = relativeModelPath;
        component.MeshIndex = null;

        var graph = model.SceneGraph;
        component.SuppressDraw = graph is not null && graph.ShouldUnpack;
        if (graph is null)
            return true;

        if (!graph.ShouldUnpack)
        {
            if (graph.FirstMeshIndex is int meshIndex &&
                graph.TryGetMeshWorldTransform(meshIndex, out var meshWorld) &&
                root.TryGetComponent<TransformComponent>(out var transform))
            {
                var combined = meshWorld * transform.GetTransform();
                ModelHierarchySpawner.ApplyLocalTransform(root, combined);
            }

            ModelHierarchySpawner.SpawnPackedLights(scene, root, graph);
            return true;
        }

        ModelHierarchySpawner.SpawnChildren(
            scene, root, graph, relativeModelPath, component.Color, model.Submeshes);
        ApplyVisibilityZones(scene, root, visibilityZones);
        return true;
    }

    public void Undo()
    {
        ModelHierarchySpawner.DestroyChildren(scene, root);
        component.ModelPath = _oldModelPath;
        component.MeshIndex = _oldMeshIndex;
        component.SuppressDraw = _oldSuppressDraw;

        if (_oldChildren is null)
            return;

        foreach (var snapshot in _oldChildren)
            snapshot.Restore(scene);
    }

    private static Dictionary<(string Name, int? MeshIndex), int> CaptureVisibilityZones(IScene scene, Entity root)
    {
        var saved = new Dictionary<(string, int?), int>();
        foreach (var entity in scene.CollectSubtree(root))
        {
            if (!entity.TryGetComponent<ModelRendererComponent>(out var renderer))
                continue;

            if (renderer.VisibilityZoneEntityId < 0)
                continue;

            saved[(entity.Name, renderer.MeshIndex)] = renderer.VisibilityZoneEntityId;
        }

        return saved;
    }

    private static void ApplyVisibilityZones(
        IScene scene,
        Entity root,
        IReadOnlyDictionary<(string Name, int? MeshIndex), int> saved)
    {
        if (saved.Count == 0)
            return;

        foreach (var entity in scene.CollectSubtree(root))
        {
            if (!entity.TryGetComponent<ModelRendererComponent>(out var renderer))
                continue;

            if (!saved.TryGetValue((entity.Name, renderer.MeshIndex), out var zoneId))
                continue;

            renderer.VisibilityZoneEntityId = zoneId;
        }
    }
}
