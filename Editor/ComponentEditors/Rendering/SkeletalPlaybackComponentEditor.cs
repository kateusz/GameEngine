using ECS;
using Editor.ComponentEditors.Core;
using Editor.Features.History;
using Editor.UI.Drawers;
using Editor.UI.Elements;
using Engine.Core;
using SceneComponents.Rendering;

namespace Editor.ComponentEditors.Rendering;

public class SkeletalPlaybackComponentEditor(UIPropertyRenderer propertyRenderer, IEditorHistory history)
    : ComponentEditor<SkeletalPlaybackComponent>(history)
{
    private static readonly string[] SkeletonExtensions = [".skel"];
    private static readonly string[] ClipExtensions = [".anim3d"];

    protected override string DisplayName => "Skeletal Playback";

    protected override void DrawContent(SkeletalPlaybackComponent component, Entity entity)
    {
        DrawAssetPathDrop("Skeleton", SkeletonExtensions, path => component.SkeletonPath = path, component.SkeletonPath);
        DrawAssetPathDrop("Clip", ClipExtensions, path => component.ClipPath = path, component.ClipPath);

        propertyRenderer.DrawPropertyField("Clip Name", component.ClipName ?? string.Empty,
            newValue => component.ClipName = string.IsNullOrWhiteSpace((string)newValue) ? null : (string)newValue);
        propertyRenderer.DrawPropertyField("Playing", component.Playing,
            newValue => component.Playing = (bool)newValue);
        propertyRenderer.DrawPropertyField("Loop", component.Loop,
            newValue => component.Loop = (bool)newValue);
        propertyRenderer.DrawPropertyField("Speed", component.Speed,
            newValue => component.Speed = (float)newValue);
        propertyRenderer.DrawPropertyField("Time", component.Time,
            newValue => component.Time = System.Math.Max(0f, (float)newValue));
    }

    private static void DrawAssetPathDrop(
        string label,
        string[] extensions,
        Action<string> onPathChanged,
        string? currentPath)
    {
        UIPropertyRenderer.DrawPropertyRow(label, () =>
        {
            var buttonLabel = !string.IsNullOrEmpty(currentPath)
                ? Path.GetFileName(currentPath)
                : label;

            ButtonDrawer.DrawFullWidthButton(buttonLabel, () => { });

            DragDropDrawer.HandleFileDropTarget(
                DragDropDrawer.ContentBrowserItemPayload,
                path => DragDropDrawer.IsValidFile(PathBuilder.Resolve(path), extensions),
                path => onPathChanged(PathBuilder.ToAssetRelativePath(path)));
        });
    }
}
