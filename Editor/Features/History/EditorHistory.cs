using Engine.Scene;

namespace Editor.Features.History;

public sealed class EditorHistory(ISceneContext sceneContext) : IEditorHistory
{
    private const int MaxDepth = 100;

    private readonly LinkedList<IUndoCommand> _undo = new();
    private readonly Stack<IUndoCommand> _redo = new();

    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;

    public void Execute(IUndoCommand command)
    {
        if (!IsEditMode())
            return;

        ArgumentNullException.ThrowIfNull(command);

        if (!command.Execute())
            return;

        PushUndo(command);
        _redo.Clear();
    }

    public void Undo()
    {
        if (!IsEditMode() || _undo.Count == 0)
            return;

        var command = _undo.Last!.Value;
        _undo.RemoveLast();
        try
        {
            command.Undo();
            _redo.Push(command);
        }
        catch
        {
            _undo.AddLast(command);
            throw;
        }
    }

    public void Redo()
    {
        if (!IsEditMode() || _redo.Count == 0)
            return;

        var command = _redo.Pop();
        try
        {
            if (!command.Execute())
            {
                _redo.Push(command);
                return;
            }

            PushUndo(command);
        }
        catch
        {
            _redo.Push(command);
            throw;
        }
    }

    public void Clear()
    {
        _undo.Clear();
        _redo.Clear();
    }

    private bool IsEditMode() => sceneContext.State == SceneState.Edit;

    private void PushUndo(IUndoCommand command)
    {
        _undo.AddLast(command);
        if (_undo.Count > MaxDepth)
            _undo.RemoveFirst();
    }
}
