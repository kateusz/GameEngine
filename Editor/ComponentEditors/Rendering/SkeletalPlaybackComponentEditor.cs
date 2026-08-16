using ECS;
using Editor.ComponentEditors.Core;
using Editor.Features.History;
using Editor.UI.Elements;
using SceneComponents.Rendering;

namespace Editor.ComponentEditors.Rendering;

public class SkeletalPlaybackComponentEditor(UIPropertyRenderer propertyRenderer, IEditorHistory history)
    : ComponentEditor<SkeletalPlaybackComponent>(history)
{
    protected override string DisplayName => "Skeletal Playback";

    protected override void DrawContent(SkeletalPlaybackComponent component, Entity entity)
    {
        MeshDropTarget.Draw("Mesh", path => component.MeshPath = path, component.MeshPath);
        propertyRenderer.DrawPropertyField("Clip Name", component.ClipName ?? "",
            newValue => component.ClipName = (string)newValue);
        propertyRenderer.DrawPropertyField("Playing", component.Playing,
            newValue => component.Playing = (bool)newValue);
        propertyRenderer.DrawPropertyField("Loop", component.Loop,
            newValue => component.Loop = (bool)newValue);
        propertyRenderer.DrawPropertyField("Speed", component.Speed,
            newValue => component.Speed = (float)newValue);
        propertyRenderer.DrawPropertyField("Time", component.Time,
            newValue => component.Time = (float)newValue);
    }
}
