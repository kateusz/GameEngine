using ECS;
using Editor.ComponentEditors.Core;
using Editor.UI.Elements;
using Engine.Scene.Components.Lights;
using ImGuiNET;

namespace Editor.ComponentEditors;

public class PointLightComponentEditor : IComponentEditor
{
    public void DrawComponent(Entity entity)
    {
        ComponentEditorRegistry.DrawComponent<PointLightComponent>("Point Light", entity, () =>
        {
            if (!entity.TryGetComponent<PointLightComponent>(out var pointLight))
                return;

            var newColor = pointLight.Color;
            ImGui.ColorEdit3("Color", ref newColor);
            if (newColor != pointLight.Color)
                pointLight.Color = newColor;

            var intensity = pointLight.Intensity;
            if (ImGui.DragFloat("Intensity", ref intensity, 0.1f, 0.0f, 100.0f))
                pointLight.Intensity = intensity;

            pointLight.Type = LightType.Point;
        });
    }
}

