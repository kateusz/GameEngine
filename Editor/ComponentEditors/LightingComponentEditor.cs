using System.Numerics;
using ECS;
using Editor.ComponentEditors.Core;
using Editor.UI.Elements;
using Engine.Scene.Components;
using ImGuiNET;

namespace Editor.ComponentEditors;

public class LightingComponentEditor : IComponentEditor
{
    private static readonly string[] LightTypeNames = ["Point", "Directional"];

    public void DrawComponent(Entity entity)
    {
        ComponentEditorRegistry.DrawComponent<LightingComponent>("Lighting", entity, () =>
        {
            var lc = entity.GetComponent<LightingComponent>();

            var typeIndex = (int)lc.Type;
            if (ImGui.Combo("Type", ref typeIndex, LightTypeNames, LightTypeNames.Length))
                lc.Type = (LightType)typeIndex;

            var newPosition = lc.Position;
            VectorPanel.DrawVec3Control("Position", ref newPosition);
            if (newPosition != lc.Position)
                lc.Position = newPosition;

            var newDirection = lc.Direction;
            VectorPanel.DrawVec3Control("Direction", ref newDirection);
            if (newDirection != lc.Direction)
                lc.Direction = newDirection;

            var newColor = lc.Color;
            ImGui.ColorEdit3("Color", ref newColor);
            if (newColor != lc.Color)
                lc.Color = newColor;
        });
    }
}