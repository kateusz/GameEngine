using ECS;
using Editor.ComponentEditors.Core;
using Editor.Features.History;
using Editor.UI.Elements;
using ImGuiNET;
using SceneComponents.Lighting;

namespace Editor.ComponentEditors;

public class DirectionalLightComponentEditor(UIPropertyRenderer propertyRenderer, IEditorHistory history) : ComponentEditor<DirectionalLightComponent>(history)
{
    protected override string DisplayName => "Directional Light";

    protected override void DrawContent(DirectionalLightComponent component, Entity entity)
    {
        var direction = component.Direction;
        var color = component.Color;
        VectorPanel.DrawVec3Control("Direction", ref direction);

        if (direction != component.Direction)
            component.Direction = direction;

        UIPropertyRenderer.DrawPropertyRow("Color", () =>
        {
            if (ImGui.ColorEdit3("##Color", ref color,
                    ImGuiColorEditFlags.Float | ImGuiColorEditFlags.DisplayRGB | ImGuiColorEditFlags.InputRGB |
                    ImGuiColorEditFlags.NoOptions))
                component.Color = color;
        });

        propertyRenderer.DrawPropertyField("Strength", component.Strength,
            newValue => component.Strength = (float)newValue);
    }
}