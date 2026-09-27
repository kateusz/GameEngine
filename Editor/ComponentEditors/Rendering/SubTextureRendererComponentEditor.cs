using ECS;
using Editor.ComponentEditors.Core;
using Editor.Features.History;
using Editor.UI.Drawers;
using Editor.UI.Elements;
using ImGuiNET;
using SceneComponents.Rendering;

namespace Editor.ComponentEditors.Rendering;

public class SubTextureRendererComponentEditor(
    UIPropertyRenderer propertyRenderer,
    TextureDropTarget textureDropTarget,
    IEditorHistory history) : ComponentEditor<SubTextureRendererComponent>(history)
{
    protected override string DisplayName => "Sub Texture Renderer";

    protected override void DrawContent(SubTextureRendererComponent component, Entity entity)
    {
        textureDropTarget.Draw("Texture",
            relativePath => MultiField.WriteEach(entity, (Entity e, string path) =>
            {
                e.GetComponent<SubTextureRendererComponent>().TexturePath = path;
            }, relativePath),
            MultiField.UniformPath(entity, e => e.GetComponent<SubTextureRendererComponent>().TexturePath));

        propertyRenderer.DrawPropertyField("Coords", entity,
            e => e.GetComponent<SubTextureRendererComponent>().Coords,
            (e, v) => e.GetComponent<SubTextureRendererComponent>().Coords = v,
            UIPropertyRenderer.SameVector2);

        LayoutDrawer.DrawSeparatorWithSpacing();
        ImGui.Text("Atlas Settings");

        propertyRenderer.DrawPropertyField("Cell Size", entity,
            e => e.GetComponent<SubTextureRendererComponent>().CellSize,
            (e, v) => e.GetComponent<SubTextureRendererComponent>().CellSize = v,
            UIPropertyRenderer.SameVector2);
        propertyRenderer.DrawPropertyField("Sprite Size", entity,
            e => e.GetComponent<SubTextureRendererComponent>().SpriteSize,
            (e, v) => e.GetComponent<SubTextureRendererComponent>().SpriteSize = v,
            UIPropertyRenderer.SameVector2);
    }
}
