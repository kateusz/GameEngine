using ECS;
using Editor.Features.Scene;
using Engine.Scene;
using SceneComponents.Rendering;

namespace Editor.Features.History.Commands;

/// <summary>
/// Clears <see cref="ModelRendererComponent.ModelPath"/> and removes unpacked import children.
/// </summary>
public sealed class ClearImportedModelCommand(
    IScene scene,
    Entity root,
    ModelRendererComponent component) : IUndoCommand
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
        component.ModelPath = null;
        component.MeshIndex = null;
        component.SuppressDraw = false;
        return true;
    }

    public void Undo()
    {
        component.ModelPath = _oldModelPath;
        component.MeshIndex = _oldMeshIndex;
        component.SuppressDraw = _oldSuppressDraw;

        if (_oldChildren is null)
            return;

        foreach (var snapshot in _oldChildren)
            snapshot.Restore(scene);
    }
}
