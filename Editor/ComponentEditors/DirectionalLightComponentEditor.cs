using ECS;
using Editor.ComponentEditors.Core;
using Editor.UI.Elements;
using ImGuiNET;
using SceneComponents.Lighting;

namespace Editor.ComponentEditors;

public class DirectionalLightComponentEditor : IComponentEditor
{
    public void DrawComponent(Entity entity)
    {
        ComponentEditorRegistry.DrawComponent<DirectionalLightComponent>("Directional Light", entity, () =>
        {
            var dlc = entity.GetComponent<DirectionalLightComponent>();
            var direction = dlc.Direction;
            var color = dlc.Color;
            VectorPanel.DrawVec3Control("Direction", ref direction);

            if (direction != dlc.Direction)
                dlc.Direction = direction;

            UIPropertyRenderer.DrawPropertyRow("Color", () =>
            {
                if (ImGui.ColorEdit3("##Color", ref color,
                        ImGuiColorEditFlags.Float | ImGuiColorEditFlags.DisplayRGB | ImGuiColorEditFlags.InputRGB |
                        ImGuiColorEditFlags.NoOptions))
                    dlc.Color = color;
            });
        });
    }
}