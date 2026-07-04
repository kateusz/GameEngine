using ECS;
using Editor.ComponentEditors.Core;
using Editor.UI.Drawers;
using Editor.UI.Elements;
using Engine.Scene;

namespace Editor.ComponentEditors;

public class GameComponentInspector(UIPropertyRenderer propertyRenderer) : IComponentEditor
{
    public void DrawComponent(Entity entity)
    {
        foreach (var component in entity.GetAllComponents()
                     .Where(c => c is IGameComponent)
                     .OrderBy(c => c.GetType().Name))
        {
            var componentType = component.GetType();
            var treeNodeId = $"{componentType.FullName}_{entity.Id}";
            ComponentEditorRegistry.DrawComponent(componentType.Name, entity, componentType,
                () => DrawComponentFields(component, treeNodeId));
        }
    }

    private void DrawComponentFields(IComponent component, string componentId)
    {
        var fields = ExposedMemberAccessor.GetExposedMembers(component).ToList();
        if (fields.Count == 0)
        {
            TextDrawer.DrawErrorText("No public fields/properties found!");
            return;
        }

        foreach (var (fieldName, fieldType, fieldValue) in fields)
        {
            UIPropertyRenderer.DrawPropertyRow(fieldName, () =>
            {
                var inputLabel = $"{fieldName}##{componentId}_{fieldName}";
                if (!propertyRenderer.TryDrawFieldEditor(inputLabel, fieldType, fieldValue, out var newValue))
                    return;

                if (!EqualityComparer<object>.Default.Equals(fieldValue, newValue))
                    ExposedMemberAccessor.SetMemberValue(component, fieldName, newValue);
            });
        }
    }
}
