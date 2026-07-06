using ECS;
using Editor.ComponentEditors.Core;
using Editor.UI.Elements;
using SceneComponents.Rendering;

namespace Editor.ComponentEditors.Rendering;

public class ModelRendererComponentEditor(UIPropertyRenderer propertyRenderer)
    : ComponentEditor<ModelRendererComponent>
{
    protected override string DisplayName => "Model Renderer";

    protected override void DrawContent(ModelRendererComponent component, Entity entity)
    {
        propertyRenderer.DrawPropertyField("Color", component.Color,
            newValue => component.Color = (System.Numerics.Vector4)newValue);
    }
}
