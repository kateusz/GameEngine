using ECS;
using Editor.Panels.Elements;
using Engine.Scene.Components;
using ImGuiNET;

namespace Editor.Panels.ComponentEditors;

public class ModelRendererComponentEditor : IComponentEditor
{
    public void DrawComponent(Entity e)
    {
        ComponentEditorRegistry.DrawComponent<ModelRendererComponent>("Model Renderer", e, entity =>
        {
            var modelRendererComponent = entity.GetComponent<ModelRendererComponent>();

            UIPropertyRenderer.DrawPropertyField("Color", modelRendererComponent.Color,
                newValue => modelRendererComponent.Color = (System.Numerics.Vector4)newValue);

            // Use specialized texture drop target for override texture
            ModelTextureDropTarget.Draw("Texture", texture => modelRendererComponent.OverrideTexture = texture);

            // Shadow options
            UIPropertyRenderer.DrawPropertyField("Cast Shadows", modelRendererComponent.CastShadows,
                newValue => modelRendererComponent.CastShadows = (bool)newValue);

            UIPropertyRenderer.DrawPropertyField("Receive Shadows", modelRendererComponent.ReceiveShadows,
                newValue => modelRendererComponent.ReceiveShadows = (bool)newValue);
        });
    }
}