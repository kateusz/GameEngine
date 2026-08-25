using ECS;
using Editor.ComponentEditors.Core;
using Editor.Features.History;
using Editor.UI.Elements;
using SceneComponents.Lighting;

namespace Editor.ComponentEditors.Lighting;

public class SpotLightComponentEditor(UIPropertyRenderer propertyRenderer, IEditorHistory history)
    : ComponentEditor<SpotLightComponent>(history)
{
    protected override string DisplayName => "Spot Light";

    protected override void DrawContent(SpotLightComponent component, Entity entity)
    {
        propertyRenderer.DrawPropertyField("Color", component.Color,
            newValue => component.Color = (System.Numerics.Vector4)newValue);
        propertyRenderer.DrawPropertyField("Direction", component.Direction,
            newValue => component.Direction = (System.Numerics.Vector3)newValue);
        propertyRenderer.DrawPropertyField("Inner Cutoff", component.InnerCutoff,
            newValue => component.InnerCutoff = (float)newValue);
        propertyRenderer.DrawPropertyField("Outer Cutoff", component.OuterCutoff,
            newValue => component.OuterCutoff = (float)newValue);
        propertyRenderer.DrawPropertyField("Constant", component.Constant,
            newValue => component.Constant = (float)newValue);
        propertyRenderer.DrawPropertyField("Linear", component.Linear,
            newValue => component.Linear = (float)newValue);
        propertyRenderer.DrawPropertyField("Quadratic", component.Quadratic,
            newValue => component.Quadratic = (float)newValue);
    }
}
