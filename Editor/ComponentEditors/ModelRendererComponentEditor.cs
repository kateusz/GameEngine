using ECS;
using Editor.ComponentEditors.Core;
using Editor.UI.Elements;
using SceneComponents.Rendering;

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
        });
    }
}
