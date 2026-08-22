using ECS;
using Editor.ComponentEditors.Core;
using Editor.Features.History;
using Editor.UI.Elements;
using Engine.Renderer.Textures;
using SceneComponents.Rendering;

namespace Editor.ComponentEditors.Rendering;

public class ModelRendererComponentEditor(
    ITextureFactory textureFactory,
    UIPropertyRenderer propertyRenderer,
    IEditorHistory history) : ComponentEditor<ModelRendererComponent>(history)
{
    protected override string DisplayName => "Model Renderer";

    protected override void DrawContent(ModelRendererComponent component, Entity entity)
    {
        propertyRenderer.DrawPropertyField("Color", component.Color,
            newValue => component.Color = (System.Numerics.Vector4)newValue);
        TextureDropTarget.Draw("Texture", relativePath =>
        {
            component.TexturePath = relativePath;
        }, textureFactory, component.TexturePath);
        propertyRenderer.DrawPropertyField("Tiling Factor", component.TilingFactor,
            newValue => component.TilingFactor = (float)newValue);
    }
}
