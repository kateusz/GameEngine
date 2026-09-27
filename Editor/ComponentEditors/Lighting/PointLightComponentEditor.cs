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
        propertyRenderer.DrawPropertyField("Intensity", component.Intensity,
            newValue => component.Intensity = (float)newValue);
        propertyRenderer.DrawPropertyField("Range", component.Range,
            newValue => component.Range = (float)newValue);
        propertyRenderer.DrawPropertyField("Casts Shadow", component.CastsShadow,
            newValue => component.CastsShadow = (bool)newValue);
    }
}