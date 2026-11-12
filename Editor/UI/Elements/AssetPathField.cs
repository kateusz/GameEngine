using System.Numerics;
using Editor.AssetPicker;
using Editor.UI.Constants;
using Editor.UI.Drawers;
using Engine.Project;
using ImGuiNET;

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
        Action<string?> onRelativePathChanged,
        Func<string, bool>? isValidDrop = null)
    {
        isValidDrop ??= path => DragDropDrawer.IsValidFile(PathBuilder.Resolve(path), kind.Extensions);

        UIPropertyRenderer.DrawPropertyRow(label, () =>
        {
            var hasPath = !string.IsNullOrEmpty(currentPath);
            // ponytail: audio clear is path-only; model clear runs ClearImportedModelCommand in the editor.
            var showClear = hasPath
                            && (ReferenceEquals(kind, AssetKind.Texture) || ReferenceEquals(kind, AssetKind.Model));

            var buttonLabel = hasPath ? Path.GetFileName(currentPath) : "Open...";
            var width = showClear
                ? ImGui.GetContentRegionAvail().X
                  - EditorUIConstants.SmallButtonSize
                  - ImGui.GetStyle().ItemSpacing.X
                : -1f;

            if (ImGui.Button(buttonLabel, new Vector2(width, EditorUIConstants.StandardButtonHeight)))
                assetBrowser.Open(kind, path => onRelativePathChanged(path), currentPath);
            LayoutDrawer.DrawTooltip(EditorUIConstants.SelectAssetTooltip);

            DragDropDrawer.HandleFileDropTarget(
                DragDropDrawer.ContentBrowserItemPayload,
                isValidDrop,
                path => onRelativePathChanged(PathBuilder.ToAssetRelativePath(path)));

            if (!showClear)
                return;

            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Text, EditorUIConstants.ErrorColor);
            var clearTooltip = ReferenceEquals(kind, AssetKind.Model) ? "Clear model" : "Clear texture";
            if (ButtonDrawer.DrawSmallButton($"×##clear_{label}", tooltip: clearTooltip))
                onRelativePathChanged(null);
            ImGui.PopStyleColor();
        });
    }
}
