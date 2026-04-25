using ECS;
using Editor.ComponentEditors.Core;
using Editor.UI.Elements;
using Engine.Scene.Components.Lights;
using ImGuiNET;

namespace Editor.ComponentEditors;

public class DirectionalLightComponentEditor : IComponentEditor
{
    public void DrawComponent(Entity entity)
    {
        ComponentEditorRegistry.DrawComponent<DirectionalLightComponent>("Directional Light", entity, () =>
        {
            if (!entity.TryGetComponent<DirectionalLightComponent>(out var directionalLight))
                return;

            var direction = directionalLight.Direction;
            VectorPanel.DrawVec3Control("Direction", ref direction);
            if (direction != directionalLight.Direction)
            {
                if (direction.LengthSquared() < 0.0001f)
                    direction = new System.Numerics.Vector3(0f, -1f, 0f);
                directionalLight.Direction = direction;
            }

            var color = directionalLight.Color;
            ImGui.ColorEdit3("Color", ref color,
                ImGuiColorEditFlags.Float | ImGuiColorEditFlags.DisplayRGB | ImGuiColorEditFlags.InputRGB |
                ImGuiColorEditFlags.NoOptions);
            if (color != directionalLight.Color)
                directionalLight.Color = color;

            var strength = directionalLight.Strength;
            if (ImGui.DragFloat("Strength", ref strength, 0.01f, 0.0f, 100.0f))
                directionalLight.Strength = strength;

            directionalLight.Type = LightType.Directional;
        });
    }
}

