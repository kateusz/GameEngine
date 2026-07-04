using ECS;
using Engine.Scene;

namespace Editor.Features.Selection;

public sealed class EditorSelection(ISceneContext sceneContext) : IEditorSelection
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
        if (_selectedEntity?.Id == entity?.Id)
            return;

        _selectedEntity = entity;
        SelectionChanged?.Invoke(entity, source);
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
