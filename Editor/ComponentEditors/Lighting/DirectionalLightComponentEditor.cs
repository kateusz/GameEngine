using ECS;
using Editor.ComponentEditors.Core;
using Editor.Features.History;
using Editor.UI.Elements;
using SceneComponents.Lighting;

namespace Editor.ComponentEditors.Lighting;

public class DirectionalLightComponentEditor(UIPropertyRenderer propertyRenderer, IEditorHistory history)
    : ComponentEditor<DirectionalLightComponent>(history)
{
    protected override string DisplayName => "Directional Light";

    protected override void DrawContent(DirectionalLightComponent component, Entity entity)
    {
        propertyRenderer.DrawPropertyField("Direction", entity,
            e => e.GetComponent<DirectionalLightComponent>().Direction,
            (e, v) => e.GetComponent<DirectionalLightComponent>().Direction = v,
            UIPropertyRenderer.SameVector3);
        propertyRenderer.DrawPropertyField("Color", entity,
            e => e.GetComponent<DirectionalLightComponent>().Color,
            (e, v) => e.GetComponent<DirectionalLightComponent>().Color = v,
            UIPropertyRenderer.SameVector4);
        propertyRenderer.DrawPropertyField("Intensity", entity,
            e => e.GetComponent<DirectionalLightComponent>().Intensity,
            (e, v) => e.GetComponent<DirectionalLightComponent>().Intensity = v,
            MultiField.SameFloat);
    }
}
