using Editor.Platform;
using Editor.UI.Drawers;
using Engine.Project;

namespace Editor.UI.Elements;

/// <summary>
/// Drag-and-drop target for texture files (.png, .jpg) from the content browser.
/// On Windows, clicking opens a native file dialog.
/// </summary>
public static class TextureDropTarget
{
    private static readonly string[] SupportedExtensions = [".png", ".jpg"];
    private const string FileFilter = "Images (*.png;*.jpg)|*.png;*.jpg|All files (*.*)|*.*";

    public static void Draw(string label, Action<string> onTexturePathChanged, string? currentTexturePath = null)
    {
        UIPropertyRenderer.DrawPropertyRow(label, () =>
        {
            var buttonLabel = !string.IsNullOrEmpty(currentTexturePath)
                ? Path.GetFileName(currentTexturePath)
                : FilePicker.IsAvailable ? "Open..." : "Drop texture here";

            if (ButtonDrawer.DrawFullWidthButton(buttonLabel) && FilePicker.IsAvailable)
            {
                string? initial = null;
                try
                {
                    initial = !string.IsNullOrEmpty(currentTexturePath)
                        ? PathBuilder.Resolve(currentTexturePath)
                        : PathBuilder.AssetsPath;
                }
                catch (InvalidOperationException)
                {
                    initial = Environment.CurrentDirectory;
                }

                var picked = FilePicker.PickFile("Open Texture", FileFilter, initial);
                if (!string.IsNullOrEmpty(picked)
                    && DragDropDrawer.HasValidExtension(picked, SupportedExtensions))
                {
                    onTexturePathChanged(PathBuilder.ToAssetRelativePath(picked));
                }
            }

            DragDropDrawer.HandleFileDropTarget(
                DragDropDrawer.ContentBrowserItemPayload,
                path => DragDropDrawer.IsValidFile(PathBuilder.Resolve(path), SupportedExtensions),
                path => onTexturePathChanged(PathBuilder.ToAssetRelativePath(path)));
        });
    }
}
