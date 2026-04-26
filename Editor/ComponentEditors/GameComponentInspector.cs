using System.Numerics;
using ECS;
using Editor.UI.Constants;
using Editor.UI.Drawers;
using Editor.UI.Elements;
using Editor.UI.FieldEditors;
using Engine.Scene;
using ImGuiNET;

namespace Editor.ComponentEditors;

public class GameComponentInspector
{
    public void Draw(Entity entity)
    {
        var gameComponents = entity.GetAllComponents()
            .Where(component => component is IGameComponent)
            .OrderBy(component => component.GetType().Name)
            .ToList();

        foreach (var component in gameComponents)
        {
            DrawComponent(entity, component);
        }
    }

    private static void DrawComponent(Entity entity, IComponent component)
    {
        var componentType = component.GetType();
        var treeNodeFlags = ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.Framed
                                                           | ImGuiTreeNodeFlags.SpanAvailWidth
                                                           | ImGuiTreeNodeFlags.AllowOverlap
                                                           | ImGuiTreeNodeFlags.FramePadding;

        var contentRegionAvailable = ImGui.GetContentRegionAvail();
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(EditorUIConstants.StandardPadding, EditorUIConstants.StandardPadding));
        var lineHeight = ImGui.GetFont().FontSize + ImGui.GetStyle().FramePadding.Y * 2.0f;
        ImGui.Separator();

        var treeNodeId = $"{componentType.FullName}_{entity.Id}";
        var open = ImGui.TreeNodeEx(treeNodeId, treeNodeFlags, componentType.Name);
        ImGui.PopStyleVar();

        ImGui.SameLine(contentRegionAvailable.X - lineHeight * 0.5f);
        var removed = ButtonDrawer.DrawButton("-", lineHeight, lineHeight, () => entity.RemoveComponent(componentType));

        if (!open)
            return;

        if (removed || !entity.TryGetComponent(componentType, out _))
        {
            ImGui.TreePop();
            return;
        }

        DrawComponentFields(component, treeNodeId);
        ImGui.TreePop();
    }

    private static void DrawComponentFields(IComponent component, string componentId)
    {
        var fields = ExposedMemberAccessor.GetExposedMembers(component).ToList();
        if (!fields.Any())
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

        ImGui.TextDisabled($"Unsupported type: {type.Name}");
        return false;
    }
}
