using ECS;
using Editor.ComponentEditors.Core;
using Editor.Features.History;
using Editor.UI.Elements;
using ImGuiNET;
using SceneComponents.Rendering;

namespace Editor.ComponentEditors.Rendering;

public class SubTextureRendererComponentEditor(
    UIPropertyRenderer propertyRenderer, IEditorHistory history) : ComponentEditor<SubTextureRendererComponent>(history)
{
    protected override string DisplayName => "Sub Texture Renderer";

    protected override void DrawContent(SubTextureRendererComponent component, Entity entity)
    {
        string? texturePath = null;
        if (MultiField.TryUniform(entity,
                e => e.GetComponent<SubTextureRendererComponent>().TexturePath,
                (a, b) => string.Equals(a, b, StringComparison.Ordinal),
                out var uniformPath))
            texturePath = uniformPath;

        TextureDropTarget.Draw("Texture", relativePath =>
        {
            MultiField.WriteEach(entity, (Entity e, string path) =>
            {
                e.GetComponent<SubTextureRendererComponent>().TexturePath = path;
            }, relativePath);
        }, texturePath);

        propertyRenderer.DrawPropertyField("Sub texture coords", entity,
            e => e.GetComponent<SubTextureRendererComponent>().Coords,
            (e, v) => e.GetComponent<SubTextureRendererComponent>().Coords = v,
            UIPropertyRenderer.SameVector2);

        ImGui.Separator();
        ImGui.Text("Atlas Settings");

        propertyRenderer.DrawPropertyField("Cell Size", entity,
            e => e.GetComponent<SubTextureRendererComponent>().CellSize,
            (e, v) => e.GetComponent<SubTextureRendererComponent>().CellSize = v,
            UIPropertyRenderer.SameVector2);
        propertyRenderer.DrawPropertyField("Sprite Size", entity,
            e => e.GetComponent<SubTextureRendererComponent>().SpriteSize,
            (e, v) => e.GetComponent<SubTextureRendererComponent>().SpriteSize = v,
            UIPropertyRenderer.SameVector2);

        ImGui.EndDisabled();
    }
}
