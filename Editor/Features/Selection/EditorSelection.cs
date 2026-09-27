using ECS;
using Editor.Features.Viewport;
using Engine.Scene;

namespace Editor.Features.Selection;

public sealed class EditorSelection(ISceneContext sceneContext, IEditorCameraFraming cameraFraming) : IEditorSelection
{
    private readonly List<Entity> _selected = [];
    private Entity? _primary;
    private int? _anchorId;

    public event Action<Entity?, SelectionSource>? SelectionChanged;

    public Entity? SelectedEntity
    {
        get
        {
            Prune();
            return _primary;
        }
    }

    public IReadOnlyList<Entity> SelectedEntities
    {
        get
        {
            Prune();
            return _selected;
        }
    }

    public void Select(Entity? entity, SelectionSource source)
    {
        Prune();
        var unchanged = _selected.Count == (entity is null ? 0 : 1)
                        && _primary?.Id == entity?.Id;
        if (unchanged && source != SelectionSource.Hierarchy)
            return;

        _selected.Clear();
        if (entity is not null)
            _selected.Add(entity);
        _primary = entity;
        _anchorId = entity?.Id;

        SelectionChanged?.Invoke(entity, source);
        if (source == SelectionSource.Hierarchy && entity is not null)
            cameraFraming.FocusOnEntity(entity);
    }

    public void Toggle(Entity entity)
    {
        Prune();
        var index = _selected.FindIndex(e => e.Id == entity.Id);
        if (index >= 0)
            _selected.RemoveAt(index);
        else
            _selected.Add(entity);

        _anchorId = entity.Id;
        _primary = _selected.Exists(e => e.Id == entity.Id)
            ? entity
            : _selected.Count > 0 ? _selected[^1] : null;

        SelectionChanged?.Invoke(_primary, SelectionSource.Hierarchy);
    }

    public void SelectRange(List<Entity> visibleInOrder, Entity clicked)
    {
        Prune();
        var anchorIndex = _anchorId is { } id
            ? visibleInOrder.FindIndex(e => e.Id == id)
            : -1;
        var clickIndex = visibleInOrder.FindIndex(e => e.Id == clicked.Id);
        if (anchorIndex < 0 || clickIndex < 0)
        {
            Select(clicked, SelectionSource.Hierarchy);
            return;
        }

        var from = System.Math.Min(anchorIndex, clickIndex);
        var to = System.Math.Max(anchorIndex, clickIndex);
        _selected.Clear();
        for (var i = from; i <= to; i++)
            _selected.Add(visibleInOrder[i]);

        _primary = clicked;
        SelectionChanged?.Invoke(_primary, SelectionSource.Hierarchy);
        cameraFraming.FocusOnEntity(clicked);
    }

    private void Prune()
    {
        _selected.RemoveAll(e => !IsInActiveScene(e.Id));
        if (_primary is not null && _selected.TrueForAll(e => e.Id != _primary.Id))
            _primary = _selected.Count > 0 ? _selected[^1] : null;
        if (_anchorId is { } anchor && !IsInActiveScene(anchor))
            _anchorId = null;
    }

    private bool IsInActiveScene(int id) =>
        sceneContext.ActiveScene?.Entities.Any(e => e.Id == id) == true;
}
