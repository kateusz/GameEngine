using ECS;

namespace Editor.Features.Selection;

public interface IEditorSelection
{
    Entity? SelectedEntity { get; }
    IReadOnlyList<Entity> SelectedEntities { get; }
    event Action<Entity?, SelectionSource> SelectionChanged;
    void Select(Entity? entity, SelectionSource source);
    void Toggle(Entity entity);
    void SelectRange(List<Entity> visibleInOrder, Entity clicked);
}
