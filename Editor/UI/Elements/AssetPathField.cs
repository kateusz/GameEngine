using Editor.AssetPicker;
using Editor.UI.Constants;
using Editor.UI.Drawers;
using Engine.Project;

namespace Editor.UI.Elements;

/// <summary>
/// Shared asset path control: Open… / Asset Browser + Content Browser drop.
/// </summary>
public class AssetPathField(AssetBrowserDialog assetBrowser)
{
    public void Draw(
        string label,
        AssetKind kind,
        string? currentPath,
        Action<string> onRelativePathChanged,
        Func<string, bool>? isValidDrop = null)
    {
        isValidDrop ??= path => DragDropDrawer.IsValidFile(PathBuilder.Resolve(path), kind.Extensions);

        UIPropertyRenderer.DrawPropertyRow(label, () =>
        {
            var buttonLabel = !string.IsNullOrEmpty(currentPath)
                ? Path.GetFileName(currentPath)
                : "Open...";
            if (ButtonDrawer.DrawFullWidthButton(buttonLabel, tooltip: EditorUIConstants.SelectAssetTooltip))
                assetBrowser.Open(kind, onRelativePathChanged, currentPath);

            DragDropDrawer.HandleFileDropTarget(
                DragDropDrawer.ContentBrowserItemPayload,
                isValidDrop,
                path => onRelativePathChanged(PathBuilder.ToAssetRelativePath(path)));
        });
    }
}
