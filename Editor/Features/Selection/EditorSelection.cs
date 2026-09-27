using ECS;
using Editor.Features.Viewport;
using Engine.Scene;

namespace Editor.Features.Selection;

public sealed class EditorSelection(ISceneContext sceneContext, IEditorCameraFraming cameraFraming) : IEditorSelection
{
    private Entity? _selectedEntity;
    public event Action<Entity?, SelectionSource>? SelectionChanged;

    public Entity? SelectedEntity
    {
        get
        {
            if (_selectedEntity is not null && !IsInActiveScene(_selectedEntity))
                _selectedEntity = null;
            return _selectedEntity;
        }
    }

    public void Select(Entity? entity, SelectionSource source)
    {
        var unchanged = _selectedEntity?.Id == entity?.Id;
        if (unchanged && source != SelectionSource.Hierarchy)
            return;

        if (!unchanged)
            _selectedEntity = entity;

        SelectionChanged?.Invoke(entity, source);

        if (source == SelectionSource.Hierarchy && entity is not null)
            cameraFraming.FocusOnEntity(entity);
    }

    private bool IsInActiveScene(Entity entity)
    {
        var scene = sceneContext.ActiveScene;
        if (scene is null)
            return false;

        foreach (var e in scene.Entities)
        {
            if (e.Id == entity.Id)
                return true;
        }

        return false;
    }
}
