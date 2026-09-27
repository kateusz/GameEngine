using ECS;
using Editor.ComponentEditors.Core;
using Editor.Features.History;
using Editor.UI.Drawers;
using Editor.UI.Elements;

namespace Editor.ComponentEditors;

public class GameComponentEditor(UIPropertyRenderer propertyRenderer, IEditorHistory history) : IComponentEditor
{
    public void DrawComponent(Entity entity)
    {
        foreach (var component in entity.GetAllComponents()
                     .Where(c => c is IGameComponent)
                     .OrderBy(c => c.GetType().Name))
        {
            var componentType = component.GetType();
            if (MultiField.Targets is { } targets
                && targets.Any(e => !e.TryGetComponent(componentType, out _)))
                continue;

            var treeNodeId = $"{componentType.FullName}_{entity.Id}";
            ComponentEditorRegistry.DrawComponent(componentType.Name, entity, componentType, history,
                () => DrawComponentFields(component, componentId: treeNodeId, entity));
        }
    }

    private void DrawComponentFields(IComponent component, string componentId, Entity entity)
    {
        var componentType = component.GetType();
        var fields = ExposedMemberAccessor.GetExposedMembers(component).ToList();
        if (fields.Count == 0)
        {
            TextDrawer.DrawErrorText("No public fields/properties found!");
            return;
        }

        foreach (var (fieldName, fieldType, _) in fields)
        {
            object ReadMember(Entity e)
            {
                if (!e.TryGetComponent(componentType, out var c))
                    throw new InvalidOperationException();
                foreach (var (name, _, value) in ExposedMemberAccessor.GetExposedMembers(c))
                {
                    if (name == fieldName)
                        return value;
                }

                throw new InvalidOperationException();
            }

            bool Same(object a, object b) => UIPropertyRenderer.ValuesSame(fieldType, a, b);

            UIPropertyRenderer.DrawPropertyRow(fieldName, () =>
            {
                if (!MultiField.TryUniform(entity, ReadMember, Same, out var value))
                {
                    propertyRenderer.DrawBlankControl(
                        $"##{componentId}_{fieldName}",
                        fieldType,
                        entity,
                        ReadMember,
                        (e, v) =>
                        {
                            if (!e.TryGetComponent(componentType, out var c))
                                return;
                            ExposedMemberAccessor.SetMemberValue(c, fieldName, v);
                        });
                    return;
                }

                var inputLabel = $"##{componentId}_{fieldName}";
                if (!propertyRenderer.TryDrawFieldEditor(inputLabel, fieldType, value, out var newValue))
                    return;

                if (!Same(value, newValue))
                {
                    MultiField.WriteEach(entity, (Entity e, object v) =>
                    {
                        if (!e.TryGetComponent(componentType, out var c))
                            return;
                        ExposedMemberAccessor.SetMemberValue(c, fieldName, v);
                    }, newValue);
                }
            });
        }
    }
}
