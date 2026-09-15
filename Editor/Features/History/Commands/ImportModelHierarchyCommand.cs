using ECS;
using Engine.Renderer.Models;
using Engine.Scene;
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

        ModelHierarchySpawner.DestroyChildren(scene, root);
        ModelHierarchySpawner.Instantiate(scene, root, component, model, relativeModelPath);
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
}
