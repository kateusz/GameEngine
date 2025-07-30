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
            
            var newColor = modelRendererComponent.Color;
            UIPropertyRenderer.DrawPropertyRow("Color", () => ImGui.ColorEdit4("##ModelColor", ref newColor));
            if (modelRendererComponent.Color != newColor)
                modelRendererComponent.Color = newColor;

            // Use specialized texture drop target for override texture
            ModelTextureDropTarget.Draw("Texture", texture => modelRendererComponent.OverrideTexture = texture);

            // Shadow options
            bool castShadows = modelRendererComponent.CastShadows;
            UIPropertyRenderer.DrawPropertyRow("Cast Shadows", () => ImGui.Checkbox("##CastShadows", ref castShadows));
            if (modelRendererComponent.CastShadows != castShadows)
                modelRendererComponent.CastShadows = castShadows;

            bool receiveShadows = modelRendererComponent.ReceiveShadows;
            UIPropertyRenderer.DrawPropertyRow("Receive Shadows", () => ImGui.Checkbox("##ReceiveShadows", ref receiveShadows));
            if (modelRendererComponent.ReceiveShadows != receiveShadows)
                modelRendererComponent.ReceiveShadows = receiveShadows;
        });
    }
}