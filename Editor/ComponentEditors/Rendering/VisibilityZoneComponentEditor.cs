using ECS;
using Editor.ComponentEditors.Core;
using Editor.Features.History;
using Editor.UI.Elements;
using SceneComponents.Rendering;

namespace Editor.ComponentEditors.Rendering;

public class VisibilityZoneComponentEditor(UIPropertyRenderer propertyRenderer, IEditorHistory history)
    : ComponentEditor<VisibilityZoneComponent>(history)
{
    protected override string DisplayName => "Visibility Zone";

    protected override void DrawContent(VisibilityZoneComponent component, Entity entity)
    {
        propertyRenderer.DrawPropertyField("Min", entity,
            e => e.GetComponent<VisibilityZoneComponent>().Min,
            (e, v) => e.GetComponent<VisibilityZoneComponent>().Min = v,
            UIPropertyRenderer.SameVector3);
        propertyRenderer.DrawPropertyField("Max", entity,
            e => e.GetComponent<VisibilityZoneComponent>().Max,
            (e, v) => e.GetComponent<VisibilityZoneComponent>().Max = v,
            UIPropertyRenderer.SameVector3);
    }
}
