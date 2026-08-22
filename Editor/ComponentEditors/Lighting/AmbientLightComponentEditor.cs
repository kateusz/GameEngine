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
        propertyRenderer.DrawPropertyField("Color", component.Color,
            newValue => component.Color = (System.Numerics.Vector4)newValue);
        propertyRenderer.DrawPropertyField("Strength", component.Strength,
            newValue => component.Strength = (float)newValue);
    }
}
