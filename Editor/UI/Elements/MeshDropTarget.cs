using Editor.UI.Drawers;
using Engine.Core;

namespace Editor.UI.Elements;

public static class MeshDropTarget
{
    private static readonly string[] SupportedExtensions = [".fbx", ".gltf", ".glb"];

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
                path => onModelPathChanged(ToAssetRelativePath(path)));
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
