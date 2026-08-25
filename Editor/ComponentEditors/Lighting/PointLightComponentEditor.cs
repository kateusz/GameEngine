using ECS;
using Editor.ComponentEditors.Core;
using Editor.Features.History;
using Editor.UI.Elements;
using SceneComponents.Lighting;

namespace Editor.ComponentEditors.Lighting;

public class PointLightComponentEditor(UIPropertyRenderer propertyRenderer, IEditorHistory history)
    : ComponentEditor<PointLightComponent>(history)
{
    protected override string DisplayName => "Point Light";

    protected override void DrawContent(PointLightComponent component, Entity entity)
    {
        propertyRenderer.DrawPropertyField("Color", component.Color,
            newValue => component.Color = (System.Numerics.Vector4)newValue);
        propertyRenderer.DrawPropertyField("Constant", component.Constant,
            newValue => component.Constant = (float)newValue);
        propertyRenderer.DrawPropertyField("Linear", component.Linear,
            newValue => component.Linear = (float)newValue);
        propertyRenderer.DrawPropertyField("Quadratic", component.Quadratic,
            newValue => component.Quadratic = (float)newValue);
    }
}
