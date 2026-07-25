using ECS;
using Editor.Features.History;

namespace Editor.ComponentEditors.Core;

public abstract class ComponentEditor<TComponent>(IEditorHistory history) : IComponentEditor
    where TComponent : IComponent
{
    protected abstract string DisplayName { get; }
    protected abstract void DrawContent(TComponent component, Entity entity);

    public void DrawComponent(Entity entity)
    {
        ComponentEditorRegistry.DrawComponent<TComponent>(DisplayName, entity, history, () =>
        {
            var component = entity.GetComponent<TComponent>();
            DrawContent(component, entity);
        });
    }
}
