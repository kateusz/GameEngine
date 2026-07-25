namespace Editor.Features.History;

public interface IUndoCommand
{
    /// <summary>Applies the forward edit. Returns false if nothing changed (caller must not push).</summary>
    bool Execute();

    void Undo();
}
