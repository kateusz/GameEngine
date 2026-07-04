using ECS;
using Editor.ComponentEditors.Core;
using Editor.UI.Drawers;
using Editor.UI.Elements;
using Editor.UI.FieldEditors;
using Engine.Scene;

namespace Editor.ComponentEditors;

public class GameComponentInspector : IComponentEditor
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

    private static void DrawComponentFields(IComponent component, string componentId)
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
                if (!TryDrawFieldEditor(inputLabel, fieldType, fieldValue, out var newValue))
                    return;

                ExposedMemberAccessor.SetMemberValue(component, fieldName, newValue);
            });
        }
    }

    private static bool TryDrawFieldEditor(string label, Type type, object value, out object newValue)
    {
        newValue = value;

        var editor = FieldEditorRegistry.GetEditor(type);
        if (editor != null)
            return editor.Draw(label, value, out newValue);

        ImGuiNET.ImGui.TextDisabled($"Unsupported type: {type.Name}");
        return false;
    }
}
