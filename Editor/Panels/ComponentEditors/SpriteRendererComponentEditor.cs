using ECS;
using Editor.Panels.Elements;
using Engine.Scene.Components;
using ImGuiNET;

namespace Editor.Panels.ComponentEditors;

public class SpriteRendererComponentEditor : IComponentEditor
{
    public void DrawComponent(Entity e)
    {
        ComponentEditorRegistry.DrawComponent<SpriteRendererComponent>("Sprite Renderer", e, entity =>
        {
            var component = entity.GetComponent<SpriteRendererComponent>();

            var newColor = component.Color;
            UIPropertyRenderer.DrawPropertyRow("Color", () => ImGui.ColorEdit4("##Color", ref newColor));
            if (component.Color != newColor)
                component.Color = newColor;

            TextureDropTarget.Draw("Texture", texture => component.Texture = texture);

            float tillingFactor = component.TilingFactor;
            UIPropertyRenderer.DrawPropertyRow("Tiling Factor",
                () => ImGui.DragFloat("##TilingFactor", ref tillingFactor, 0.1f, 0.0f, 100.0f));
            if (component.TilingFactor != tillingFactor)
                component.TilingFactor = tillingFactor;
        });
    }
}