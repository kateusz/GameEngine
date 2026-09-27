using System.Numerics;
using ECS;
using Editor.ComponentEditors.Core;
using Editor.Features.History;
using Editor.UI.Elements;
using SceneComponents.Rendering;

namespace Editor.ComponentEditors.Rendering;

public class VisibilityZoneComponentEditor(IEditorHistory history) : ComponentEditor<VisibilityZoneComponent>(history)
{
    protected override string DisplayName => "Visibility Zone";

    protected override void DrawContent(VisibilityZoneComponent component, Entity entity)
    {
        var min = component.Min;
        var max = component.Max;
        VectorPanel.DrawVec3Control("Min", ref min);
        VectorPanel.DrawVec3Control("Max", ref max);
        component.Min = min;
        component.Max = max;
    }
}
