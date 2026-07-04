using ECS;

namespace Editor.Features.Selection;

public interface IEditorSelection
{
    Entity? SelectedEntity { get; }
    event Action<Entity?, SelectionSource> SelectionChanged;
    void Select(Entity? entity, SelectionSource source);
}
