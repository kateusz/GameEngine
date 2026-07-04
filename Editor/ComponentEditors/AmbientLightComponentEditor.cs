using System.Numerics;
using ECS;
using Editor.ComponentEditors.Core;
using Editor.UI.Elements;
using ImGuiNET;
using SceneComponents.Lighting;

namespace Editor.ComponentEditors;

public class AmbientLightComponentEditor(UIPropertyRenderer propertyRenderer) : IComponentEditor
{
    public void DrawComponent(Entity entity)
    {
        ComponentEditorRegistry.DrawComponent<AmbientLightComponent>("Ambient Light", entity, () =>
        {
            var alc = entity.GetComponent<AmbientLightComponent>();
            var color = alc.Color;

            UIPropertyRenderer.DrawPropertyRow("Color", () =>
            {
                if (ImGui.ColorEdit3("##Color", ref color,
                        ImGuiColorEditFlags.Float | ImGuiColorEditFlags.DisplayRGB | ImGuiColorEditFlags.InputRGB |
                        ImGuiColorEditFlags.NoOptions))
                    alc.Color = color;
            });

            propertyRenderer.DrawPropertyField("Strength", alc.Strength,
                newValue => alc.Strength = (float)newValue);
        });
    }
}