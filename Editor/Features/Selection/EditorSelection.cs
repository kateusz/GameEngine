using ECS;

namespace Editor.Features.Selection;

public sealed class EditorSelection : IEditorSelection
{
    public Entity? SelectedEntity { get; private set; }
    public event Action<Entity?, SelectionSource>? SelectionChanged;

    public void Select(Entity? entity, SelectionSource source)
    {
        if (SelectedEntity?.Id == entity?.Id)
            return;

        SelectedEntity = entity;
        SelectionChanged?.Invoke(entity, source);
    }
}
