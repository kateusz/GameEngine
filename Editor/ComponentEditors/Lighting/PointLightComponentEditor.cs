using ECS;
using Editor.ComponentEditors.Core;
using Editor.Features.History;
using Editor.UI.Drawers;
using Editor.UI.Elements;
using SceneComponents.Lighting;

namespace Editor.ComponentEditors.Lighting;

public class PointLightComponentEditor(UIPropertyRenderer propertyRenderer, IEditorHistory history)
    : ComponentEditor<PointLightComponent>(history)
{
    protected override string DisplayName => "Point Light";

    protected override void DrawContent(PointLightComponent component, Entity entity)
    {
        propertyRenderer.DrawPropertyField("Color", entity,
            e => e.GetComponent<PointLightComponent>().Color,
            (e, v) => e.GetComponent<PointLightComponent>().Color = v,
            UIPropertyRenderer.SameVector4);

        propertyRenderer.DrawPropertyField("Intensity", entity,
            e => e.GetComponent<PointLightComponent>().Intensity,
            (e, v) => e.GetComponent<PointLightComponent>().Intensity = v,
            MultiField.SameFloat);

        propertyRenderer.DrawPropertyField("Range", entity,
            e => e.GetComponent<PointLightComponent>().Range,
            (e, v) => e.GetComponent<PointLightComponent>().Range = v,
            MultiField.SameFloat);

        propertyRenderer.DrawPropertyField("Casts Shadow", entity,
            e => e.GetComponent<PointLightComponent>().CastsShadow,
            (e, v) => e.GetComponent<PointLightComponent>().CastsShadow = v);

        propertyRenderer.DrawPropertyField("Apply Offset", entity,
            e => e.GetComponent<PointLightComponent>().ApplyOffset,
            (e, v) => e.GetComponent<PointLightComponent>().ApplyOffset = v);

        if (MultiField.For(entity).All(e => e.GetComponent<PointLightComponent>().ApplyOffset))
        {
            LayoutDrawer.DrawIndentedSection(() =>
            {
                propertyRenderer.DrawPropertyField("Offset", entity,
                    e => e.GetComponent<PointLightComponent>().Offset,
                    (e, v) => e.GetComponent<PointLightComponent>().Offset = v,
                    UIPropertyRenderer.SameVector3);
            });
        }
    }
}
