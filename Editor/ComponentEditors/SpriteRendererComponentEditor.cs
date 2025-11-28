using ECS;
using Editor.ComponentEditors.Core;
using Editor.UI.Elements;
using Engine;
using Engine.Renderer.Textures;
using Engine.Scene.Components;

namespace Editor.ComponentEditors;

public class SpriteRendererComponentEditor : IComponentEditor
{
    private readonly IAssetsManager _assetsManager;
    private readonly ITextureFactory _textureFactory;

    public SpriteRendererComponentEditor(IAssetsManager assetsManager, ITextureFactory textureFactory)
    {
        _assetsManager = assetsManager;
        _textureFactory = textureFactory;
    }

    public void DrawComponent(Entity e)
    {
        ComponentEditorRegistry.DrawComponent<SpriteRendererComponent>("Sprite Renderer", e, entity =>
        {
            var component = entity.GetComponent<SpriteRendererComponent>();

            UIPropertyRenderer.DrawPropertyField("Color", component.Color,
                newValue => component.Color = (System.Numerics.Vector4)newValue);

            TextureDropTarget.Draw("Texture", texture => component.Texture = texture, _assetsManager, _textureFactory);

            UIPropertyRenderer.DrawPropertyField("Tiling Factor", component.TilingFactor,
                newValue => component.TilingFactor = (float)newValue);
        });
    }
}