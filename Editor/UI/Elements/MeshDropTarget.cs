using Editor.UI.Drawers;
using Engine.Core;

namespace Editor.UI.Elements;

public static class MeshDropTarget
{
    public static readonly string[] SupportedExtensions = [".mesh"];

    public static bool IsSupported(string filename) =>
        SupportedExtensions.Any(ext => filename.EndsWith(ext, StringComparison.OrdinalIgnoreCase));

    public static void Draw(string label, Action<string> onModelPathChanged, string? currentModelPath = null)
    {
        UIPropertyRenderer.DrawPropertyRow(label, () =>
        {
            var buttonLabel = !string.IsNullOrEmpty(currentModelPath)
                ? Path.GetFileName(currentModelPath)
                : label;

            ButtonDrawer.DrawFullWidthButton(buttonLabel, () => { });

            DragDropDrawer.HandleFileDropTarget(
                DragDropDrawer.ContentBrowserItemPayload,
                path => DragDropDrawer.IsValidFile(PathBuilder.Resolve(path), SupportedExtensions),
                path => onModelPathChanged(PathBuilder.ToAssetRelativePath(path)));
        });
    }
}
