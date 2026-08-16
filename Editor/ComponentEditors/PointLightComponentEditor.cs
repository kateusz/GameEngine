using ECS;
using Editor.ComponentEditors.Core;
using Editor.Features.History;
using Editor.UI.Elements;
using ImGuiNET;
using SceneComponents.Lighting;

namespace Editor.ComponentEditors;

public class PointLightComponentEditor(UIPropertyRenderer propertyRenderer, IEditorHistory history)
    : ComponentEditor<PointLightComponent>(history)
{
    protected override string DisplayName => "Point Light";

    protected override void DrawContent(PointLightComponent component, Entity entity)
    {
        var color = component.Color;
        UIPropertyRenderer.DrawPropertyRow("Color", () =>
        {
            if (ImGui.ColorEdit3("##Color", ref color,
                    ImGuiColorEditFlags.Float | ImGuiColorEditFlags.DisplayRGB | ImGuiColorEditFlags.InputRGB |
                    ImGuiColorEditFlags.NoOptions))
                component.Color = color;
        });

        propertyRenderer.DrawPropertyField("Strength", component.Strength,
            newValue => component.Strength = (float)newValue);
        propertyRenderer.DrawPropertyField("Range", component.Range,
            newValue => component.Range = MathF.Max(0.1f, (float)newValue));
    }
}
