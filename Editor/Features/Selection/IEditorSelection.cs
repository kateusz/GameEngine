using ECS;

namespace Editor.Features.Selection;

public interface IEditorSelection
{
    Entity? SelectedEntity { get; }
    IReadOnlyList<Entity> SelectedEntities { get; }
    event Action<Entity?, SelectionSource> SelectionChanged;
    bool IsSelected(int entityId);
    void Select(Entity? entity, SelectionSource source);
    void Toggle(Entity entity);
    void SelectRange(List<Entity> visibleInOrder, Entity clicked);
}
