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

            UIPropertyRenderer.DrawPropertyField("Sub texture coords", component.Coords,
                newValue => component.Coords = (System.Numerics.Vector2)newValue);

            TextureDropTarget.Draw("Texture", texture => component.Texture = texture);
        });
    }
}