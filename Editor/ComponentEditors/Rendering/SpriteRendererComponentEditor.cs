using ECS;
using Editor.ComponentEditors.Core;
using Editor.Features.History;
using Editor.UI.Elements;
using SceneComponents.Rendering;

namespace Editor.ComponentEditors.Rendering;

public class SpriteRendererComponentEditor(
    UIPropertyRenderer propertyRenderer,
    TextureDropTarget textureDropTarget,
    IEditorHistory history) : ComponentEditor<SpriteRendererComponent>(history)
{
    protected override string DisplayName => "Sprite Renderer";

    protected override void DrawContent(SpriteRendererComponent component, Entity entity)
    {
        propertyRenderer.DrawPropertyField("Color", entity,
            e => e.GetComponent<SpriteRendererComponent>().Color,
            (e, v) => e.GetComponent<SpriteRendererComponent>().Color = v,
            UIPropertyRenderer.SameVector4);

        textureDropTarget.Draw("Texture",
            relativePath => MultiField.WriteEach(entity, (Entity e, string path) =>
            {
                e.GetComponent<SpriteRendererComponent>().TexturePath = path;
            }, relativePath),
            MultiField.UniformPath(entity, e => e.GetComponent<SpriteRendererComponent>().TexturePath));

        propertyRenderer.DrawPropertyField("Tiling Factor", entity,
            e => e.GetComponent<SpriteRendererComponent>().TilingFactor,
            (e, v) => e.GetComponent<SpriteRendererComponent>().TilingFactor = v,
            MultiField.SameFloat);
    }
}
