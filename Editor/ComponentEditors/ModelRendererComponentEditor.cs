using ECS;
using Editor.ComponentEditors.Core;
using Editor.UI.Elements;
using Engine.Core;
using Engine.Renderer.Textures;
using Engine.Scene.Components;

namespace Editor.ComponentEditors;

public class ModelRendererComponentEditor(IAssetsManager assetsManager, ITextureFactory textureFactory)
    : IComponentEditor
{
    public void DrawComponent(Entity e)
    {
        ComponentEditorRegistry.DrawComponent<ModelRendererComponent>("Model Renderer", e, entity =>
        {
            var modelRendererComponent = entity.GetComponent<ModelRendererComponent>();

            UIPropertyRenderer.DrawPropertyField("Color", modelRendererComponent.Color,
                newValue => modelRendererComponent.Color = (System.Numerics.Vector4)newValue);

            // Use specialized texture drop target for override texture
            ModelTextureDropTarget.Draw("Texture", texture => modelRendererComponent.OverrideTexture = texture, assetsManager, textureFactory);

            // Shadow options
            UIPropertyRenderer.DrawPropertyField("Cast Shadows", modelRendererComponent.CastShadows,
                newValue => modelRendererComponent.CastShadows = (bool)newValue);

            UIPropertyRenderer.DrawPropertyField("Receive Shadows", modelRendererComponent.ReceiveShadows,
                newValue => modelRendererComponent.ReceiveShadows = (bool)newValue);
        });
    }
}