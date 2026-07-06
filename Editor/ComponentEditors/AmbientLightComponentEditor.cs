using ECS;
using Editor.ComponentEditors.Core;
using Editor.UI.Elements;
using ImGuiNET;
using SceneComponents.Lighting;

namespace Editor.ComponentEditors;

public class AmbientLightComponentEditor(UIPropertyRenderer propertyRenderer)
    : ComponentEditor<AmbientLightComponent>
{
    protected override string DisplayName => "Ambient Light";

    protected override void DrawContent(AmbientLightComponent component, Entity entity)
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
    }
}