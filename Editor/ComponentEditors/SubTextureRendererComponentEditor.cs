using ECS;
using Editor.ComponentEditors.Core;
using Editor.UI.Elements;
using Engine.Renderer.Textures;
using ImGuiNET;
using SceneComponents.Rendering;

namespace Editor.ComponentEditors;

public class SubTextureRendererComponentEditor(
    ITextureFactory textureFactory,
    UIPropertyRenderer propertyRenderer)
    : IComponentEditor
{
    public void DrawComponent(Entity entity)
    {
        ComponentEditorRegistry.DrawComponent<SubTextureRendererComponent>("Sub Texture Renderer", entity, () =>
        {
            var component = entity.GetComponent<SubTextureRendererComponent>();

            TextureDropTarget.Draw("Texture", relativePath =>
            {
                component.TexturePath = relativePath;
            }, textureFactory);
            propertyRenderer.DrawPropertyField("Sub texture coords", component.Coords,
                newValue => component.Coords = (System.Numerics.Vector2)newValue);

            ImGui.Separator();
            ImGui.Text("Atlas Settings");

            propertyRenderer.DrawPropertyField("Cell Size", component.CellSize,
                newValue => component.CellSize = (System.Numerics.Vector2)newValue);
            propertyRenderer.DrawPropertyField("Sprite Size", component.SpriteSize,
                newValue => component.SpriteSize = (System.Numerics.Vector2)newValue);

            ImGui.EndDisabled();
        });
    }
}