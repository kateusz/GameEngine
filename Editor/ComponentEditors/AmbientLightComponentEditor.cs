using ECS;
using Editor.ComponentEditors.Core;
using Engine.Scene.Components.Lights;
using ImGuiNET;

namespace Editor.ComponentEditors;

public class AmbientLightComponentEditor : IComponentEditor
{
    public void DrawComponent(Entity entity)
    {
        ComponentEditorRegistry.DrawComponent<AmbientLightComponent>("Ambient Light", entity, () =>
        {
            if (!entity.TryGetComponent<AmbientLightComponent>(out var ambientLight))
                return;

            var color = ambientLight.Color;
            ImGui.ColorEdit3("Color", ref color);
            if (color != ambientLight.Color)
                ambientLight.Color = color;

            var strength = ambientLight.Strength;
            if (ImGui.DragFloat("Strength", ref strength, 0.01f, 0.0f, 1.0f))
                ambientLight.Strength = strength;

            ambientLight.Type = LightType.Ambient;
        });
    }
}

