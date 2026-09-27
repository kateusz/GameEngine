using ECS;
using Editor.Features.History;
using Editor.UI.Elements;

namespace Editor.ComponentEditors.Core;

public abstract class ComponentEditor<TComponent>(IEditorHistory history) : IComponentEditor
    where TComponent : IComponent
{
    protected abstract string DisplayName { get; }
    protected abstract void DrawContent(TComponent component, Entity entity);

    public void DrawComponent(Entity entity)
    {
        if (MultiField.Targets is { } targets && targets.Any(e => !e.HasComponent<TComponent>()))
            return;

        ComponentEditorRegistry.DrawComponent<TComponent>(DisplayName, entity, history, () =>
        {
            var component = entity.GetComponent<TComponent>();
            DrawContent(component, entity);
        });
    }
}
