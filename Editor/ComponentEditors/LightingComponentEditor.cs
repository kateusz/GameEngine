using System.Numerics;
using ECS;
using Editor.ComponentEditors.Core;
using Editor.UI.Elements;
using Engine.Scene.Components;
using ImGuiNET;

namespace Editor.ComponentEditors;

public class LightingComponentEditor : IComponentEditor
{
    public void DrawComponent(Entity entity)
    {
        ComponentEditorRegistry.DrawComponent<LightingComponent>("Lighting", entity, () =>
        {
            var lc = entity.GetComponent<LightingComponent>();

            var newPosition = lc.Position;
            VectorPanel.DrawVec3Control("Position", ref newPosition);
            if (newPosition != lc.Position)
                lc.Position = newPosition;

            var newColor = lc.Color;
            ImGui.ColorEdit3("Color", ref newColor);
            if (newColor != lc.Color)
                lc.Color = newColor;
        });
    }
}
