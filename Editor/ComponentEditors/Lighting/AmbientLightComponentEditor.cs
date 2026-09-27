using ECS;
using Editor.ComponentEditors.Core;
using Editor.Features.History;
using Editor.UI.Elements;
using SceneComponents.Lighting;

namespace Editor.ComponentEditors.Lighting;

public class AmbientLightComponentEditor(UIPropertyRenderer propertyRenderer, IEditorHistory history)
    : ComponentEditor<AmbientLightComponent>(history)
{
    protected override string DisplayName => "Ambient Light";

    protected override void DrawContent(AmbientLightComponent component, Entity entity)
    {
        propertyRenderer.DrawPropertyField("Color", entity,
            e => e.GetComponent<AmbientLightComponent>().Color,
            (e, v) => e.GetComponent<AmbientLightComponent>().Color = v,
            UIPropertyRenderer.SameVector4);
        propertyRenderer.DrawPropertyField("Strength", entity,
            e => e.GetComponent<AmbientLightComponent>().Strength,
            (e, v) => e.GetComponent<AmbientLightComponent>().Strength = v,
            MultiField.SameFloat);
    }
}
