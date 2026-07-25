namespace Editor.Features.History;

public interface IEditorHistory
{
    bool CanUndo { get; }
    bool CanRedo { get; }
    void Execute(IUndoCommand command);
    void Undo();
    void Redo();
    void Clear();
}
