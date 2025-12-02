using ECS;
using Editor.ComponentEditors.Core;
using Editor.UI.Elements;
using Engine.Core;
using Engine.Renderer.Textures;
using Engine.Scene.Components;

namespace Editor.ComponentEditors;

public class SpriteRendererComponentEditor(IAssetsManager assetsManager, ITextureFactory textureFactory)
    : IComponentEditor
{
    public void DrawComponent(Entity entity)
    {
        ComponentEditorRegistry.DrawComponent<SpriteRendererComponent>("Sprite Renderer", entity, () =>
        {
            var component = entity.GetComponent<SpriteRendererComponent>();

            UIPropertyRenderer.DrawPropertyField("Color", component.Color,
                newValue => component.Color = (System.Numerics.Vector4)newValue);
            TextureDropTarget.Draw("Texture", texture => component.Texture = texture, assetsManager, textureFactory);
            UIPropertyRenderer.DrawPropertyField("Tiling Factor", component.TilingFactor,
                newValue => component.TilingFactor = (float)newValue);
        });
    }
}