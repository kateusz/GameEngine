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
            var ambientLight = entity.GetComponent<AmbientLightComponent>();

            var color = ambientLight.Color;
            ImGui.ColorEdit3("Color", ref color);
            if (color != ambientLight.Color)
                ambientLight.Color = color;

            ambientLight.Type = LightType.Ambient;
        });
    }
}

