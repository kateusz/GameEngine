using Engine.Scene;

namespace Editor.Features.History.Commands;

/// <summary>
/// Snapshots a subtree, destroys it, and restores with remapped IDs on Undo.
/// Selection is intentionally not touched (ADR-004).
/// Note (M2): Undo remaps entity IDs; older stack entries that hold pre-delete Entity
/// refs or IDs may silently no-op if undone after a delete-restore.
/// </summary>
public sealed class DestroyEntitySubtreeCommand(IScene scene, int rootEntityId) : IUndoCommand
{
    // updated after Undo so Redo targets the restored root, not the pre-delete id
    private int _rootId = rootEntityId;
    private EntitySubtreeSnapshot? _snapshot;

    public bool Execute()
    {
        if (!scene.Context.Contains(_rootId))
            return false;

        var root = scene.Context.GetById(_rootId);
        _snapshot ??= EntitySubtreeSnapshot.Capture(scene, root);
        scene.DestroyEntity(root);
        return true;
    }

    public void Undo()
    {
        if (_snapshot is null)
            return;

        _rootId = _snapshot.Restore(scene);
    }
}
