using ECS;
using Editor.ComponentEditors.Core;
using Editor.Features.History;
using Editor.UI.Elements;
using Engine.Renderer.Textures;
using ImGuiNET;
using SceneComponents.Rendering;

namespace Editor.ComponentEditors.Rendering;

public class SubTextureRendererComponentEditor(
    ITextureFactory textureFactory,
    UIPropertyRenderer propertyRenderer, IEditorHistory history) : ComponentEditor<SubTextureRendererComponent>(history)
{
    protected override string DisplayName => "Sub Texture Renderer";

    protected override void DrawContent(SubTextureRendererComponent component, Entity entity)
    {
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
    }
}