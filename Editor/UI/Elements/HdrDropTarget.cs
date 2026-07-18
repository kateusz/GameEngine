using Editor.UI.Drawers;
using Engine.Core;
using Engine.Renderer.Textures;

namespace Editor.UI.Elements;

/// <summary>
/// Drag-and-drop target for Radiance .hdr environment maps.
/// </summary>
public static class HdrDropTarget
{
    private static readonly string[] SupportedExtensions = [".hdr"];

    public static void Draw(
        string label,
        Action<string> onHdrPathChanged,
        ITextureFactory textureFactory,
        string? currentHdrPath = null)
    {
        UIPropertyRenderer.DrawPropertyRow(label, () =>
        {
            var buttonLabel = !string.IsNullOrEmpty(currentHdrPath)
                ? Path.GetFileName(currentHdrPath)
                : label;

            ButtonDrawer.DrawFullWidthButton(buttonLabel, () => { });

            DragDropDrawer.HandleFileDropTarget(
                DragDropDrawer.ContentBrowserItemPayload,
                path => DragDropDrawer.IsValidFile(PathBuilder.Resolve(path), SupportedExtensions),
                path =>
                {
                    var resolved = PathBuilder.Resolve(path);
                    textureFactory.Create(resolved);
                    onHdrPathChanged(ToAssetRelativePath(path));
                });
        });
    }

    private static string ToAssetRelativePath(string path)
    {
        var resolved = PathBuilder.Resolve(path);
        var assetsPath = Path.GetFullPath(PathBuilder.AssetsPath);
        var relative = Path.GetRelativePath(assetsPath, resolved);
        if (!relative.StartsWith("..", StringComparison.Ordinal) && !Path.IsPathRooted(relative))
            return relative.Replace('\\', '/');

        return path.Replace('\\', '/');
    }
}
