using ECS;
using Editor.Panels.Elements;
using Engine;
using Engine.Scene.Components;

namespace Editor.Panels.ComponentEditors;

public class SpriteRendererComponentEditor : IComponentEditor
{
    private readonly IAssetsManager _assetsManager;

    public SpriteRendererComponentEditor(IAssetsManager assetsManager)
    {
        _assetsManager = assetsManager;
    }

    public void DrawComponent(Entity e)
    {
        ComponentEditorRegistry.DrawComponent<SpriteRendererComponent>("Sprite Renderer", e, entity =>
        {
            var component = entity.GetComponent<SpriteRendererComponent>();

            UIPropertyRenderer.DrawPropertyField("Color", component.Color,
                newValue => component.Color = (System.Numerics.Vector4)newValue);

            TextureDropTarget.Draw("Texture", texture => component.Texture = texture, _assetsManager);

            UIPropertyRenderer.DrawPropertyField("Tiling Factor", component.TilingFactor,
                newValue => component.TilingFactor = (float)newValue);
        });
    }
}