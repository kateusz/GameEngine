using ECS;
using Editor.ComponentEditors.Core;
using Editor.UI.Elements;
using SceneComponents.Rendering;

namespace Editor.ComponentEditors;

public class ModelRendererComponentEditor(UIPropertyRenderer propertyRenderer) : IComponentEditor
{
    public void DrawComponent(Entity entity)
    {
        ComponentEditorRegistry.DrawComponent<ModelRendererComponent>("Model Renderer", entity, () =>
        {
            var modelRendererComponent = entity.GetComponent<ModelRendererComponent>();

            propertyRenderer.DrawPropertyField("Color", modelRendererComponent.Color,
                newValue => modelRendererComponent.Color = (System.Numerics.Vector4)newValue);
        });
    }
}
