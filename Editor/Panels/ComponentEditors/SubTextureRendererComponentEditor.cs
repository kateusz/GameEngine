using ECS;
using Editor.Panels.Elements;
using Engine.Scene.Components;
using ImGuiNET;

namespace Editor.Panels.ComponentEditors;

public class SubTextureRendererComponentEditor : IComponentEditor
{
    public void DrawComponent(Entity e)
    {
        ComponentEditorRegistry.DrawComponent<SubTextureRendererComponent>("Sub Texture Renderer", e, entity =>
        {
            var component = entity.GetComponent<SubTextureRendererComponent>();
            
            var newCoords = component.Coords;
            UIPropertyRenderer.DrawPropertyRow("Sub texture coords", () => ImGui.DragFloat2("##SubTexCoords", ref newCoords));
            if (newCoords != component.Coords)
                component.Coords = newCoords;

            TextureDropTarget.Draw("Texture", texture => component.Texture = texture);
        });
    }
}