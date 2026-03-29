using ECS;
using Editor.ComponentEditors.Core;
using Editor.UI.Elements;
using Engine.Scene.Components;

namespace Editor.ComponentEditors;

public class ModelRendererComponentEditor : IComponentEditor
{
    public void DrawComponent(Entity entity)
    {
        ComponentEditorRegistry.DrawComponent<ModelRendererComponent>("Model Renderer", entity, () =>
        {
            var modelRendererComponent = entity.GetComponent<ModelRendererComponent>();

            UIPropertyRenderer.DrawPropertyField("Color", modelRendererComponent.Color,
                newValue => modelRendererComponent.Color = (System.Numerics.Vector4)newValue);
            UIPropertyRenderer.DrawPropertyField("Cast Shadows", modelRendererComponent.CastShadows,
                newValue => modelRendererComponent.CastShadows = (bool)newValue);
            UIPropertyRenderer.DrawPropertyField("Receive Shadows", modelRendererComponent.ReceiveShadows,
                newValue => modelRendererComponent.ReceiveShadows = (bool)newValue);
        });
    }
}
