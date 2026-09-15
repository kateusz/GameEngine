using Editor.UI.Drawers;
using Engine.Project;

namespace Editor.UI.Elements;

public static class TextureDropTarget
{
    private static readonly string[] SupportedExtensions = [".png", ".jpg"];

    public static void Draw(string label, Action<string> onTexturePathChanged, string? currentTexturePath = null)
    {
        UIPropertyRenderer.DrawPropertyRow(label, () =>
        {
            var buttonLabel = !string.IsNullOrEmpty(currentTexturePath)
                ? Path.GetFileName(currentTexturePath)
                : label;

            ButtonDrawer.DrawFullWidthButton(buttonLabel);

            DragDropDrawer.HandleFileDropTarget(
                DragDropDrawer.ContentBrowserItemPayload,
                path => DragDropDrawer.IsValidFile(PathBuilder.Resolve(path), SupportedExtensions),
                path => onTexturePathChanged(PathBuilder.ToAssetRelativePath(path)));
        });
    }
}
