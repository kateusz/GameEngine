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
        propertyRenderer.DrawPropertyField("Direction", component.Direction,
            newValue => component.Direction = (System.Numerics.Vector3)newValue);
        propertyRenderer.DrawPropertyField("Color", component.Color,
            newValue => component.Color = (System.Numerics.Vector4)newValue);
        propertyRenderer.DrawPropertyField("Ortho Size", component.OrthoSize,
            newValue => component.OrthoSize = (float)newValue);
    }
}
