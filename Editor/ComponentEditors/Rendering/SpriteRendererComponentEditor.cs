using ECS;
using Editor.ComponentEditors.Core;
using Editor.UI.Elements;
using Engine.Renderer.Textures;
using SceneComponents.Rendering;

namespace Editor.ComponentEditors.Rendering;

public class SpriteRendererComponentEditor(
    ITextureFactory textureFactory,
    UIPropertyRenderer propertyRenderer)
    : IComponentEditor
{
    public void DrawComponent(Entity entity)
    {
        ComponentEditorRegistry.DrawComponent<SpriteRendererComponent>("Sprite Renderer", entity, () =>
        {
            var component = entity.GetComponent<SpriteRendererComponent>();

            propertyRenderer.DrawPropertyField("Color", component.Color,
                newValue => component.Color = (System.Numerics.Vector4)newValue);
            TextureDropTarget.Draw("Texture", relativePath =>
            {
                component.TexturePath = relativePath;
            }, textureFactory, component.TexturePath);
            propertyRenderer.DrawPropertyField("Tiling Factor", component.TilingFactor,
                newValue => component.TilingFactor = (float)newValue);
        });
    }
}